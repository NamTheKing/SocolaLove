using HeThongQuanLyVeXeBus.Models;

namespace HeThongQuanLyVeXeBus.ViewModels;

// Các kiểu dữ liệu chỉ phục vụ hiển thị (dòng bảng, mục ComboBox, cột biểu đồ).

public sealed record StatusChoice(TripStatus? Value, string Label);

public sealed record TripRow(Trip Trip, string StatusText);

public sealed record ReportRouteChoice(int? Id, string Label);

public sealed record TrendBar(string Label, string AmountText, double BarHeight, string TooltipText);

public sealed record BreakdownBar(string Label, string AmountText, double BarWidth, string TooltipText);

/// <summary>Các ô số liệu tổng hợp trên màn Doanh thu (đã định dạng sẵn).</summary>
public sealed record RevenueSummary(
    string Revenue, string ActiveCount, string CheckedInCount, string TicketCount,
    string CancelledCount, string CancelledAmount, string RefundedCount, string PendingRefundCount,
    string RefundedAmount, string PendingRefundAmount)
{
    public static RevenueSummary Empty { get; } = new("0", "0", "0", "0", "0", "0", "0", "0", "0", "0");
}

public static class TripStatusChoices
{
    public static IReadOnlyList<StatusChoice> All { get; } =
    [
        new(TripStatus.Scheduled, "Sắp chạy"),
        new(TripStatus.Boarding, "Đang đón khách"),
        new(TripStatus.Departed, "Đã xuất bến"),
        new(TripStatus.Completed, "Hoàn thành"),
        new(TripStatus.Cancelled, "Đã hủy")
    ];

    public static StatusChoice Of(TripStatus status) => All.First(choice => choice.Value == status);
}
