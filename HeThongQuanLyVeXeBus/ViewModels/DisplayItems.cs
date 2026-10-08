using HeThongQuanLyVeXeBus.Models;

namespace HeThongQuanLyVeXeBus.ViewModels;

// Các kiểu dữ liệu chỉ phục vụ hiển thị (dòng bảng, mục ComboBox).

public sealed record StatusChoice(TripStatus? Value, string Label);

public sealed record TripRow(Trip Trip, string StatusText);

public sealed record ReportRouteChoice(int? Id, string Label);

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
