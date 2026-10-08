using HeThongQuanLyVeXeBus.ViewModels.Base;

namespace HeThongQuanLyVeXeBus.ViewModels;

/// <summary>Hộp thoại hủy vé: ghi nhận tiền đã thực trả cho khách hay chưa (không tự chuyển tiền).</summary>
public sealed class RefundChoiceViewModel(string ticketCode) : ObservableObject
{
    private bool _isRefunded;

    public string Heading { get; } = $"Hủy vé {ticketCode}?";

    public bool IsRefunded
    {
        get => _isRefunded;
        set
        {
            if (SetProperty(ref _isRefunded, value))
                OnPropertyChanged(nameof(IsPending));
        }
    }

    public bool IsPending
    {
        get => !_isRefunded;
        set
        {
            if (value)
                IsRefunded = false;
        }
    }
}
