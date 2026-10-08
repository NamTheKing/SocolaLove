using System.Globalization;
using HeThongQuanLyVeXeBus.Models;
using HeThongQuanLyVeXeBus.Services;
using HeThongQuanLyVeXeBus.ViewModels.Base;

namespace HeThongQuanLyVeXeBus.ViewModels;

/// <summary>Màn "Tuyến xe": danh sách tuyến và biểu mẫu thêm/sửa.</summary>
public sealed class RoutesViewModel : ViewModelBase
{
    private readonly RouteService _routeService;
    private IReadOnlyList<BusRoute> _routes = [];
    private BusRoute? _selectedRoute;
    private string _routeCode = string.Empty;
    private string _routeName = string.Empty;
    private string _origin = string.Empty;
    private string _destination = string.Empty;
    private string _distanceKmText = string.Empty;
    private string _estimatedHoursText = string.Empty;
    private string _basePriceText = string.Empty;
    private bool _isActive = true;

    public RoutesViewModel(IAppShell shell, IDialogService dialogs, RouteService routeService)
        : base(shell, dialogs)
    {
        _routeService = routeService;
        SaveCommand = new RelayCommand(Save);
        NewCommand = new RelayCommand(New);
    }

    public IReadOnlyList<BusRoute> Routes { get => _routes; private set => SetProperty(ref _routes, value); }

    /// <summary>Dòng đang chọn trong bảng; chọn dòng sẽ đổ dữ liệu vào biểu mẫu.</summary>
    public BusRoute? SelectedRoute
    {
        get => _selectedRoute;
        set
        {
            if (SetProperty(ref _selectedRoute, value) && value is not null)
                FillForm(value);
        }
    }

    public string RouteCode { get => _routeCode; set => SetProperty(ref _routeCode, value); }
    public string RouteName { get => _routeName; set => SetProperty(ref _routeName, value); }
    public string Origin { get => _origin; set => SetProperty(ref _origin, value); }
    public string Destination { get => _destination; set => SetProperty(ref _destination, value); }
    public string DistanceKmText { get => _distanceKmText; set => SetProperty(ref _distanceKmText, value); }
    public string EstimatedHoursText { get => _estimatedHoursText; set => SetProperty(ref _estimatedHoursText, value); }
    public string BasePriceText { get => _basePriceText; set => SetProperty(ref _basePriceText, value); }
    public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }

    public RelayCommand SaveCommand { get; }
    public RelayCommand NewCommand { get; }

    public void Load(IReadOnlyList<BusRoute> routes)
    {
        var selectedId = SelectedRoute?.Id;
        Routes = routes;
        SelectedRoute = routes.FirstOrDefault(r => r.Id == selectedId);
    }

    private void FillForm(BusRoute route)
    {
        RouteCode = route.RouteCode;
        RouteName = route.RouteName;
        Origin = route.Origin;
        Destination = route.Destination;
        DistanceKmText = route.DistanceKm.ToString(CultureInfo.CurrentCulture);
        EstimatedHoursText = route.EstimatedHours.ToString(CultureInfo.CurrentCulture);
        BasePriceText = EditableNumber(route.BasePrice);
        IsActive = route.IsActive;
    }

    private void Save() => Run(() =>
    {
        var values = new BusRoute
        {
            Id = SelectedRoute?.Id ?? 0,
            RouteCode = Required(RouteCode, "mã tuyến"),
            RouteName = Required(RouteName, "tên tuyến"),
            Origin = Required(Origin, "điểm đi"),
            Destination = Required(Destination, "điểm đến"),
            DistanceKm = PositiveDouble(DistanceKmText, "Quãng đường"),
            EstimatedHours = PositiveDouble(EstimatedHoursText, "Thời gian dự kiến"),
            BasePrice = PositiveMoney(BasePriceText, "Giá cơ bản"),
            IsActive = IsActive
        };
        var savedId = _routeService.Save(values);
        Shell.ReloadData();
        SelectedRoute = Routes.FirstOrDefault(r => r.Id == savedId);
        Notify("Đã lưu tuyến xe.");
    });

    private void New()
    {
        SelectedRoute = null;
        RouteCode = string.Empty;
        RouteName = string.Empty;
        Origin = string.Empty;
        Destination = string.Empty;
        DistanceKmText = string.Empty;
        EstimatedHoursText = string.Empty;
        BasePriceText = string.Empty;
        IsActive = true;
        RequestFocus(nameof(RouteCode));
    }
}
