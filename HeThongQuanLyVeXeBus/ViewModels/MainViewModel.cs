using System.Windows.Input;
using HeThongQuanLyVeXeBus.Services;
using HeThongQuanLyVeXeBus.ViewModels.Base;

namespace HeThongQuanLyVeXeBus.ViewModels;

/// <summary>
/// ViewModel của cửa sổ chính: chứa ViewModel của từng tab, thanh trạng thái
/// và việc tải dữ liệu dùng chung cho mọi màn hình.
/// </summary>
public sealed class MainViewModel : ViewModelBase, IAppShell
{
    /// <summary>Vị trí tab "Doanh thu" trong TabControl.</summary>
    public const int RevenueTabIndex = 6;

    private readonly DatabaseService _databaseService;
    private readonly RouteService _routeService;
    private readonly BusService _busService;
    private readonly TripService _tripService;
    private readonly TicketService _ticketService;
    private string _statusMessage = string.Empty;
    private int _selectedTabIndex;
    private bool _isReady;

    public MainViewModel(DatabaseService databaseService, RouteService routeService, BusService busService,
        TripService tripService, TicketService ticketService, RevenueReportService revenueReportService,
        IDialogService dialogs)
        : base(dialogs)
    {
        _databaseService = databaseService;
        _routeService = routeService;
        _busService = busService;
        _tripService = tripService;
        _ticketService = ticketService;

        LoadCommand = new RelayCommand(Load);
        RefreshCommand = new RelayCommand(Refresh);

        Dashboard = new DashboardViewModel(this, dialogs);
        Routes = new RoutesViewModel(this, dialogs, routeService);
        Buses = new BusesViewModel(this, dialogs, busService);
        Trips = new TripsViewModel(this, dialogs, tripService);
        Booking = new BookingViewModel(this, dialogs, ticketService);
        Tickets = new TicketsViewModel(this, dialogs, ticketService);
        Revenue = new RevenueViewModel(this, dialogs, revenueReportService);

        // Chọn vé ở màn soát vé thì màn Bán vé có thể xuất lại PDF của vé đó.
        Tickets.TicketSelected += Booking.ShowTicket;
    }

    public DashboardViewModel Dashboard { get; }
    public RoutesViewModel Routes { get; }
    public BusesViewModel Buses { get; }
    public TripsViewModel Trips { get; }
    public BookingViewModel Booking { get; }
    public TicketsViewModel Tickets { get; }
    public RevenueViewModel Revenue { get; }

    public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }

    public bool IsReady { get => _isReady; private set => SetProperty(ref _isReady, value); }

    /// <summary>Tab đang mở; mở tab Doanh thu sẽ tự cập nhật báo cáo.</summary>
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetProperty(ref _selectedTabIndex, value) && value == RevenueTabIndex && IsReady)
                Run(Revenue.RefreshReport);
        }
    }

    /// <summary>Chạy một lần khi cửa sổ chính hiển thị.</summary>
    public ICommand LoadCommand { get; }

    public ICommand RefreshCommand { get; }

    public void ShowStatus(string message) => StatusMessage = message;

    public void ReloadData()
    {
        var routes = _routeService.GetAll();
        var buses = _busService.GetAll();
        var trips = _tripService.GetAll();
        var tickets = _ticketService.GetAll();

        Dashboard.Load(routes, buses, trips, tickets);
        Routes.Load(routes);
        Buses.Load(buses);
        Trips.Load(trips, routes, buses);
        Booking.Load(trips);
        Tickets.Load(tickets);
        Revenue.LoadRoutes(routes);
        if (SelectedTabIndex == RevenueTabIndex)
            Revenue.RefreshReport();
    }

    private void Load() => Run(() =>
    {
        InitializeDatabase();
        ReloadData();
        Notify("Đã tải dữ liệu.");
    });

    private void Refresh() => Run(() =>
    {
        if (!IsReady)
            InitializeDatabase();
        ReloadData();
        Notify("Đã cập nhật dữ liệu.");
    });

    private void InitializeDatabase()
    {
        _databaseService.Initialize();
        IsReady = true;
    }
}
