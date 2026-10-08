namespace HeThongQuanLyVeXeBus.ViewModels.Base;

/// <summary>
/// ViewModel báo cho View nên đặt con trỏ vào ô nhập nào (theo tên thuộc tính),
/// ví dụ ô bắt buộc còn trống. View xử lý qua Views.Behaviors.FocusBehavior.
/// </summary>
public interface IFocusRequester
{
    event Action<string>? FocusRequested;
}
