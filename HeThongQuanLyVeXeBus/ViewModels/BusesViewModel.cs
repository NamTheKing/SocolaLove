using System.Globalization;
using HeThongQuanLyVeXeBus.Models;
using HeThongQuanLyVeXeBus.Services;
using HeThongQuanLyVeXeBus.ViewModels.Base;

namespace HeThongQuanLyVeXeBus.ViewModels;

/// <summary>Màn "Đội xe": danh sách xe và biểu mẫu thêm/sửa.</summary>
public sealed class BusesViewModel : ViewModelBase
{
    private const string DefaultBusType = "Giường nằm";
    private const string DefaultCapacity = "36";
    private const int DefaultFloors = 2;

    private readonly BusService _busService;
    private IReadOnlyList<Bus> _buses = [];
    private Bus? _selectedBus;
    private string _plateNumber = string.Empty;
    private string _busModel = string.Empty;
    private string _busTypeName = DefaultBusType;
    private string _capacityText = DefaultCapacity;
    private int? _numberOfFloors = DefaultFloors;
    private bool _hasWifi = true;
    private bool _hasAirConditioner = true;
    private bool _isActive = true;

    public BusesViewModel(IAppShell shell, IDialogService dialogs, BusService busService)
        : base(shell, dialogs)
    {
        _busService = busService;
        SaveCommand = new RelayCommand(Save);
        NewCommand = new RelayCommand(New);
    }

    public IReadOnlyList<Bus> Buses { get => _buses; private set => SetProperty(ref _buses, value); }

    public IReadOnlyList<int> FloorOptions { get; } = [1, 2];

    /// <summary>Dòng đang chọn trong bảng; chọn dòng sẽ đổ dữ liệu vào biểu mẫu.</summary>
    public Bus? SelectedBus
    {
        get => _selectedBus;
        set
        {
            if (SetProperty(ref _selectedBus, value) && value is not null)
                FillForm(value);
        }
    }

    public string PlateNumber { get => _plateNumber; set => SetProperty(ref _plateNumber, value); }
    public string BusModel { get => _busModel; set => SetProperty(ref _busModel, value); }
    public string BusTypeName { get => _busTypeName; set => SetProperty(ref _busTypeName, value); }
    public string CapacityText { get => _capacityText; set => SetProperty(ref _capacityText, value); }
    public int? NumberOfFloors { get => _numberOfFloors; set => SetProperty(ref _numberOfFloors, value); }
    public bool HasWifi { get => _hasWifi; set => SetProperty(ref _hasWifi, value); }
    public bool HasAirConditioner { get => _hasAirConditioner; set => SetProperty(ref _hasAirConditioner, value); }
    public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }

    public RelayCommand SaveCommand { get; }
    public RelayCommand NewCommand { get; }

    public void Load(IReadOnlyList<Bus> buses)
    {
        var selectedId = SelectedBus?.Id;
        Buses = buses;
        SelectedBus = buses.FirstOrDefault(b => b.Id == selectedId);
    }

    private void FillForm(Bus bus)
    {
        PlateNumber = bus.PlateNumber;
        BusModel = bus.BusModel;
        BusTypeName = bus.BusTypeName;
        CapacityText = bus.TotalSeats.ToString(CultureInfo.CurrentCulture);
        NumberOfFloors = bus.NumberOfFloors;
        HasWifi = bus.HasWifi;
        HasAirConditioner = bus.HasAirConditioner;
        IsActive = bus.IsActive;
    }

    private void Save() => Run(() =>
    {
        var plate = Required(PlateNumber, "biển số");
        var model = Required(BusModel, "dòng xe");
        var type = Required(BusTypeName, "loại xe");
        if (!int.TryParse(Required(CapacityText, "số chỗ"), out var seats) || seats is <= 0 or > 500)
            throw new ArgumentException("Số chỗ phải từ 1 đến 500.");
        if (NumberOfFloors is not { } floors)
            throw new ArgumentException("Chọn số tầng của xe.");

        var savedId = _busService.Save(new Bus
        {
            Id = SelectedBus?.Id ?? 0,
            PlateNumber = plate,
            BusModel = model,
            BusTypeName = type,
            TotalSeats = seats,
            NumberOfFloors = floors,
            HasWifi = HasWifi,
            HasAirConditioner = HasAirConditioner,
            IsActive = IsActive
        });
        Shell.ReloadData();
        SelectedBus = Buses.FirstOrDefault(b => b.Id == savedId);
        Notify("Đã lưu xe.");
    });

    private void New()
    {
        SelectedBus = null;
        PlateNumber = string.Empty;
        BusModel = string.Empty;
        BusTypeName = DefaultBusType;
        CapacityText = DefaultCapacity;
        NumberOfFloors = DefaultFloors;
        HasWifi = true;
        HasAirConditioner = true;
        IsActive = true;
        RequestFocus(nameof(PlateNumber));
    }
}
