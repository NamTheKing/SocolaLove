using HeThongQuanLyVeXeBus.Models;
using HeThongQuanLyVeXeBus.Services;
using HeThongQuanLyVeXeBus.ViewModels.Base;

namespace HeThongQuanLyVeXeBus.ViewModels;

/// <summary>Màn "Doanh thu": chọn kỳ báo cáo, xem số liệu chính, bảng theo tuyến và xuất Excel.</summary>
public sealed class RevenueViewModel : ViewModelBase
{
    private readonly RevenueReportService _reportService;
    private DateTime? _fromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime? _toDate = DateTime.Today;
    private IReadOnlyList<ReportRouteChoice> _routeChoices = [AllRoutes];
    private ReportRouteChoice? _selectedRouteChoice = AllRoutes;
    private RevenueReport? _report;

    public RevenueViewModel(IAppShell shell, IDialogService dialogs, RevenueReportService reportService)
        : base(shell, dialogs)
    {
        _reportService = reportService;
        ApplyCommand = new RelayCommand(() => Run(RefreshReport));
        ExportExcelCommand = new RelayCommand(ExportExcel);
    }

    private static ReportRouteChoice AllRoutes { get; } = new(null, "Tất cả tuyến");

    public DateTime? FromDate { get => _fromDate; set => SetProperty(ref _fromDate, value); }
    public DateTime? ToDate { get => _toDate; set => SetProperty(ref _toDate, value); }

    public IReadOnlyList<ReportRouteChoice> RouteChoices
    {
        get => _routeChoices;
        private set => SetProperty(ref _routeChoices, value);
    }

    public ReportRouteChoice? SelectedRouteChoice
    {
        get => _selectedRouteChoice;
        set => SetProperty(ref _selectedRouteChoice, value);
    }

    /// <summary>Báo cáo đang hiển thị: doanh thu, số vé và bảng theo tuyến.</summary>
    public RevenueReport? Report
    {
        get => _report;
        private set
        {
            if (SetProperty(ref _report, value))
                OnPropertyChanged(nameof(IsEmpty));
        }
    }

    /// <summary>Kỳ đã chọn không có vé nào được bán.</summary>
    public bool IsEmpty => Report is { TicketCount: 0 };

    public RelayCommand ApplyCommand { get; }
    public RelayCommand ExportExcelCommand { get; }

    /// <summary>Cập nhật danh sách tuyến trong bộ lọc, giữ tuyến đang chọn.</summary>
    public void LoadRoutes(IReadOnlyList<BusRoute> routes)
    {
        var selectedId = SelectedRouteChoice?.Id;
        RouteChoices = [AllRoutes, .. routes.Select(route => new ReportRouteChoice(route.Id, route.RouteName))];
        SelectedRouteChoice = RouteChoices.FirstOrDefault(choice => choice.Id == selectedId);
    }

    /// <summary>Tạo lại báo cáo theo bộ lọc hiện tại (bỏ qua nếu dữ liệu chưa tải).</summary>
    public void RefreshReport()
    {
        if (!Shell.IsReady)
            return;
        Report = LoadReport();
        Notify("Đã cập nhật báo cáo doanh thu.");
    }

    private RevenueReport LoadReport()
    {
        if (!Shell.IsReady)
            throw new InvalidOperationException("Dữ liệu chưa được tải.");
        if (FromDate is not { } from || ToDate is not { } to)
            throw new ArgumentException("Chọn ngày bắt đầu và ngày kết thúc báo cáo.");
        return _reportService.Generate(from, to, SelectedRouteChoice?.Id);
    }

    private void ExportExcel() => Run(() =>
    {
        var report = LoadReport();
        Report = report;
        var routeLabel = SelectedRouteChoice?.Label ?? AllRoutes.Label;
        var path = Dialogs.AskSaveFilePath("Xuất báo cáo doanh thu", "Sổ Excel (*.xlsx)|*.xlsx", ".xlsx",
            $"DoanhThu_{report.From:yyyyMMdd}_{report.To:yyyyMMdd}.xlsx");
        if (path is null)
            return;
        RevenueExcelExporter.Export(report, routeLabel, path);
        Notify("Đã xuất báo cáo doanh thu ra Excel: " + path);
    });
}
