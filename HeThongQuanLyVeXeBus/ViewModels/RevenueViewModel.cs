using System.Globalization;
using HeThongQuanLyVeXeBus.Models;
using HeThongQuanLyVeXeBus.Services;
using HeThongQuanLyVeXeBus.ViewModels.Base;

namespace HeThongQuanLyVeXeBus.ViewModels;

/// <summary>Màn "Doanh thu": lọc kỳ báo cáo, số liệu tổng hợp, biểu đồ và xuất Excel.</summary>
public sealed class RevenueViewModel : ViewModelBase
{
    private const double TrendBarMaxHeight = 122;
    private const double BreakdownBarMaxWidth = 240;
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    private readonly RevenueReportService _reportService;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private IReadOnlyList<ReportRouteChoice> _routeChoices = [AllRoutes];
    private ReportRouteChoice? _selectedRouteChoice = AllRoutes;
    private string _periodText = string.Empty;
    private RevenueSummary _summary = RevenueSummary.Empty;
    private IReadOnlyList<TrendBar> _trendBars = [];
    private IReadOnlyList<BreakdownBar> _routeBars = [];
    private IReadOnlyList<BreakdownBar> _paymentBars = [];
    private IReadOnlyList<RevenueTrip> _trips = [];
    private IReadOnlyList<RevenueTicket> _tickets = [];
    private bool _isTrendEmpty;
    private bool _isRouteChartEmpty;
    private bool _isPaymentChartEmpty;

    public RevenueViewModel(IAppShell shell, IDialogService dialogs, RevenueReportService reportService)
        : base(shell, dialogs)
    {
        _reportService = reportService;
        ResetPeriod();
        ApplyCommand = new RelayCommand(() => Run(RefreshReport));
        ResetCommand = new RelayCommand(Reset);
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

    public string PeriodText { get => _periodText; private set => SetProperty(ref _periodText, value); }
    public RevenueSummary Summary { get => _summary; private set => SetProperty(ref _summary, value); }
    public IReadOnlyList<TrendBar> TrendBars { get => _trendBars; private set => SetProperty(ref _trendBars, value); }
    public IReadOnlyList<BreakdownBar> RouteBars { get => _routeBars; private set => SetProperty(ref _routeBars, value); }
    public IReadOnlyList<BreakdownBar> PaymentBars { get => _paymentBars; private set => SetProperty(ref _paymentBars, value); }
    public IReadOnlyList<RevenueTrip> Trips { get => _trips; private set => SetProperty(ref _trips, value); }
    public IReadOnlyList<RevenueTicket> Tickets { get => _tickets; private set => SetProperty(ref _tickets, value); }
    public bool IsTrendEmpty { get => _isTrendEmpty; private set => SetProperty(ref _isTrendEmpty, value); }
    public bool IsRouteChartEmpty { get => _isRouteChartEmpty; private set => SetProperty(ref _isRouteChartEmpty, value); }
    public bool IsPaymentChartEmpty { get => _isPaymentChartEmpty; private set => SetProperty(ref _isPaymentChartEmpty, value); }

    public RelayCommand ApplyCommand { get; }
    public RelayCommand ResetCommand { get; }
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
        if (Shell.IsReady)
            Display(LoadReport());
    }

    private void ResetPeriod()
    {
        FromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        ToDate = DateTime.Today;
    }

    private void Reset()
    {
        ResetPeriod();
        SelectedRouteChoice = RouteChoices[0];
        Run(RefreshReport);
    }

    private RevenueReport LoadReport()
    {
        if (!Shell.IsReady)
            throw new InvalidOperationException("Dữ liệu chưa được tải.");
        if (FromDate is not { } from || ToDate is not { } to)
            throw new ArgumentException("Chọn ngày bắt đầu và ngày kết thúc báo cáo.");
        return _reportService.Generate(from, to, SelectedRouteChoice?.Id);
    }

    private void Display(RevenueReport report)
    {
        PeriodText = $"Ngày bán vé: {report.From:dd/MM/yyyy} – {report.To:dd/MM/yyyy} · {report.TrendGranularity}. " +
                     "Số liệu theo trạng thái vé hiện tại; không phải đối soát ngân hàng.";
        Summary = new RevenueSummary(
            Money(report.Revenue),
            report.ActiveCount.ToString("N0"),
            report.CheckedInCount.ToString("N0"),
            report.TicketCount.ToString("N0"),
            report.CancelledCount.ToString("N0"),
            Money(report.CancelledValue),
            report.RefundedCount.ToString("N0"),
            report.PendingRefundCount.ToString("N0"),
            Money(report.RefundedValue),
            Money(report.PendingRefundValue));

        var trendMax = report.Trend.Count == 0 ? 0 : report.Trend.Max(point => point.Revenue);
        TrendBars = report.Trend.Select(point => new TrendBar(
            point.Label, ChartMoney(point.Revenue),
            trendMax == 0 ? 0 : (double)(point.Revenue / trendMax) * TrendBarMaxHeight,
            $"{point.Label}: {Money(point.Revenue)} · {point.ActiveTickets} vé hợp lệ · {point.CancelledTickets} vé hủy"))
            .ToList();
        IsTrendEmpty = report.TicketCount == 0;

        var routeMax = report.Routes.Count == 0 ? 0 : report.Routes.Max(route => route.Revenue);
        RouteBars = report.Routes.Select(route => new BreakdownBar(
            route.RouteName, ChartMoney(route.Revenue),
            routeMax == 0 ? 0 : (double)(route.Revenue / routeMax) * BreakdownBarMaxWidth,
            $"{route.RouteCode} · {route.RouteName}: {Money(route.Revenue)} · {route.ActiveTickets} vé hợp lệ · {route.CancelledTickets} vé hủy"))
            .ToList();
        IsRouteChartEmpty = report.Routes.Count == 0;

        var paymentMax = report.Payments.Count == 0 ? 0 : report.Payments.Max(method => method.Revenue);
        PaymentBars = report.Payments.Select(method => new BreakdownBar(
            method.PaymentMethod, ChartMoney(method.Revenue),
            paymentMax == 0 ? 0 : (double)(method.Revenue / paymentMax) * BreakdownBarMaxWidth,
            $"{method.PaymentMethod}: {Money(method.Revenue)} · {method.ActiveTickets} vé hợp lệ"))
            .ToList();
        IsPaymentChartEmpty = report.Payments.Count == 0;

        Trips = report.Trips;
        Tickets = report.Tickets;
        Notify("Đã cập nhật báo cáo doanh thu.");
    }

    private void ExportExcel() => Run(() =>
    {
        var report = LoadReport();
        Display(report);
        var routeLabel = SelectedRouteChoice?.Label ?? AllRoutes.Label;
        var path = Dialogs.AskSaveFilePath("Xuất báo cáo doanh thu", "Sổ Excel (*.xlsx)|*.xlsx", ".xlsx",
            $"DoanhThu_{report.From:yyyyMMdd}_{report.To:yyyyMMdd}.xlsx");
        if (path is null)
            return;
        RevenueExcelExporter.Export(report, routeLabel, path);
        Notify("Đã xuất báo cáo doanh thu ra Excel: " + path);
    });

    private static string Money(decimal amount) => amount.ToString("N0", Vietnamese) + " ₫";

    private static string ChartMoney(decimal amount)
    {
        if (amount >= 1_000_000_000m)
            return $"{amount / 1_000_000_000m:0.#} tỷ";
        if (amount >= 1_000_000m)
            return $"{amount / 1_000_000m:0.#} tr";
        if (amount >= 1_000m)
            return $"{amount / 1_000m:0.#} nghìn";
        return amount.ToString("N0", Vietnamese);
    }
}
