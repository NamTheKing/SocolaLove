using System.Globalization;
using HeThongQuanLyVeXeBus.Models;
using HeThongQuanLyVeXeBus.Services;
using HeThongQuanLyVeXeBus.ViewModels.Base;

namespace HeThongQuanLyVeXeBus.ViewModels;

/// <summary>Màn "Chuyến xe": lịch chuyến, bộ lọc, biểu mẫu và đổi trạng thái.</summary>
public sealed class TripsViewModel : ViewModelBase
{
    private const string DefaultDepartureTime = "08:00";
    private const string DefaultArrivalTime = "15:00";

    private readonly TripService _tripService;
    private IReadOnlyList<Trip> _allTrips = [];
    private IReadOnlyList<TripRow> _tripRows = [];
    private IReadOnlyList<BusRoute> _routes = [];
    private IReadOnlyList<Bus> _buses = [];
    private TripRow? _selectedTripRow;
    private BusRoute? _tripRoute;
    private Bus? _tripBus;
    private DateTime? _departureDate = DateTime.Today.AddDays(1);
    private string _departureTimeText = DefaultDepartureTime;
    private DateTime? _arrivalDate = DateTime.Today.AddDays(1);
    private string _arrivalTimeText = DefaultArrivalTime;
    private string _driverName = string.Empty;
    private string _driverPhone = string.Empty;
    private string _ticketPriceText = string.Empty;
    private DateTime? _filterDate;
    private StatusChoice? _filterStatus;
    private StatusChoice? _newStatus;
    private bool _isLoading;

    public TripsViewModel(IAppShell shell, IDialogService dialogs, TripService tripService)
        : base(shell, dialogs)
    {
        _tripService = tripService;
        FilterChoices = [new StatusChoice(null, "Tất cả"), .. TripStatusChoices.All];
        _filterStatus = FilterChoices[0];
        SaveCommand = new RelayCommand(Save);
        NewCommand = new RelayCommand(New);
        ClearFilterCommand = new RelayCommand(ClearFilter);
        ChangeStatusCommand = new RelayCommand(ChangeStatus);
    }

    public IReadOnlyList<TripRow> TripRows { get => _tripRows; private set => SetProperty(ref _tripRows, value); }
    public IReadOnlyList<BusRoute> Routes { get => _routes; private set => SetProperty(ref _routes, value); }
    public IReadOnlyList<Bus> Buses { get => _buses; private set => SetProperty(ref _buses, value); }
    public IReadOnlyList<StatusChoice> StatusChoices => TripStatusChoices.All;
    public IReadOnlyList<StatusChoice> FilterChoices { get; }

    /// <summary>Dòng đang chọn trong bảng; chọn dòng sẽ đổ dữ liệu vào biểu mẫu.</summary>
    public TripRow? SelectedTripRow
    {
        get => _selectedTripRow;
        set
        {
            if (SetProperty(ref _selectedTripRow, value) && value is not null)
                FillForm(value.Trip);
        }
    }

    /// <summary>Tuyến trong biểu mẫu; đổi tuyến sẽ gợi ý giá vé bằng giá cơ bản của tuyến.</summary>
    public BusRoute? TripRoute
    {
        get => _tripRoute;
        set
        {
            if (SetProperty(ref _tripRoute, value) && value is not null && !_isLoading)
                TicketPriceText = EditableNumber(value.BasePrice);
        }
    }

    public Bus? TripBus { get => _tripBus; set => SetProperty(ref _tripBus, value); }
    public DateTime? DepartureDate { get => _departureDate; set => SetProperty(ref _departureDate, value); }
    public string DepartureTimeText { get => _departureTimeText; set => SetProperty(ref _departureTimeText, value); }
    public DateTime? ArrivalDate { get => _arrivalDate; set => SetProperty(ref _arrivalDate, value); }
    public string ArrivalTimeText { get => _arrivalTimeText; set => SetProperty(ref _arrivalTimeText, value); }
    public string DriverName { get => _driverName; set => SetProperty(ref _driverName, value); }
    public string DriverPhone { get => _driverPhone; set => SetProperty(ref _driverPhone, value); }
    public string TicketPriceText { get => _ticketPriceText; set => SetProperty(ref _ticketPriceText, value); }

    public DateTime? FilterDate
    {
        get => _filterDate;
        set
        {
            if (SetProperty(ref _filterDate, value))
                ApplyFilter();
        }
    }

    public StatusChoice? FilterStatus
    {
        get => _filterStatus;
        set
        {
            if (SetProperty(ref _filterStatus, value))
                ApplyFilter();
        }
    }

    /// <summary>Trạng thái mới sẽ lưu cho chuyến đang chọn.</summary>
    public StatusChoice? NewStatus { get => _newStatus; set => SetProperty(ref _newStatus, value); }

    public RelayCommand SaveCommand { get; }
    public RelayCommand NewCommand { get; }
    public RelayCommand ClearFilterCommand { get; }
    public RelayCommand ChangeStatusCommand { get; }

    public void Load(IReadOnlyList<Trip> trips, IReadOnlyList<BusRoute> routes, IReadOnlyList<Bus> buses)
    {
        var selectedTripId = SelectedTripRow?.Trip.Id;
        var routeId = TripRoute?.Id;
        var busId = TripBus?.Id;
        _isLoading = true;
        try
        {
            _allTrips = trips;
            Routes = routes;
            Buses = buses;
            TripRoute = routes.FirstOrDefault(r => r.Id == routeId);
            TripBus = buses.FirstOrDefault(b => b.Id == busId);
            ApplyFilter();
        }
        finally
        {
            _isLoading = false;
        }
        SelectedTripRow = TripRows.FirstOrDefault(row => row.Trip.Id == selectedTripId);
    }

    private void ApplyFilter()
    {
        var date = FilterDate?.Date;
        var status = FilterStatus?.Value;
        TripRows = _allTrips
            .Where(t => (date is null || t.DepartureTime.Date == date) && (status is null || t.Status == status))
            .Select(t => new TripRow(t, TripStatusChoices.Of(t.Status).Label))
            .ToList();
    }

    private void FillForm(Trip trip)
    {
        TripRoute = Routes.FirstOrDefault(r => r.Id == trip.RouteId);
        TripBus = Buses.FirstOrDefault(b => b.Id == trip.BusId);
        DepartureDate = trip.DepartureTime.Date;
        DepartureTimeText = trip.DepartureTime.ToString("HH:mm", CultureInfo.InvariantCulture);
        ArrivalDate = trip.EstimatedArrivalTime.Date;
        ArrivalTimeText = trip.EstimatedArrivalTime.ToString("HH:mm", CultureInfo.InvariantCulture);
        DriverName = trip.DriverName;
        DriverPhone = trip.DriverPhone;
        TicketPriceText = EditableNumber(trip.TicketPrice);
        NewStatus = TripStatusChoices.Of(trip.Status);
    }

    private void Save() => Run(() =>
    {
        if (TripRoute is not { } route || TripBus is not { } bus)
            throw new ArgumentException("Chọn tuyến và xe.");
        var values = new Trip
        {
            Id = SelectedTripRow?.Trip.Id ?? 0,
            RouteId = route.Id,
            BusId = bus.Id,
            DepartureTime = EnteredDateTime(DepartureDate, DepartureTimeText, "giờ khởi hành"),
            EstimatedArrivalTime = EnteredDateTime(ArrivalDate, ArrivalTimeText, "giờ dự kiến đến"),
            DriverName = Required(DriverName, "tên tài xế"),
            DriverPhone = Required(DriverPhone, "SĐT tài xế"),
            TicketPrice = PositiveMoney(TicketPriceText, "Giá vé")
        };
        var saved = _tripService.Save(values);
        Shell.ReloadData();
        SelectedTripRow = TripRows.FirstOrDefault(row => row.Trip.Id == saved.Id);
        Notify("Đã lưu chuyến " + saved.TripCode + ".");
    });

    private void New()
    {
        SelectedTripRow = null;
        TripRoute = null;
        TripBus = null;
        DepartureDate = DateTime.Today.AddDays(1);
        ArrivalDate = DateTime.Today.AddDays(1);
        DepartureTimeText = DefaultDepartureTime;
        ArrivalTimeText = DefaultArrivalTime;
        DriverName = string.Empty;
        DriverPhone = string.Empty;
        TicketPriceText = string.Empty;
        RequestFocus(nameof(TripRoute));
    }

    private void ClearFilter()
    {
        FilterDate = null;
        FilterStatus = FilterChoices[0];
    }

    private void ChangeStatus() => Run(() =>
    {
        if (SelectedTripRow is not { } row || NewStatus?.Value is not { } value)
            throw new ArgumentException("Chọn chuyến và trạng thái cần lưu.");
        _tripService.EnsureStatusChangeAllowed(row.Trip.Id, value);
        if (value == TripStatus.Cancelled &&
            !Dialogs.Confirm("Hủy chuyến sẽ hủy tất cả vé chưa lên xe. Xác nhận?", "Hủy chuyến"))
            return;
        _tripService.ChangeStatus(row.Trip.Id, value);
        Shell.ReloadData();
        Notify("Đã cập nhật trạng thái chuyến.");
    });
}
