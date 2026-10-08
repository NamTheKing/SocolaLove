using System.Globalization;
using System.Runtime.CompilerServices;
using HeThongQuanLyVeXeBus.Services;

namespace HeThongQuanLyVeXeBus.ViewModels.Base;

/// <summary>
/// Lớp cơ sở cho ViewModel của từng màn hình: báo lỗi/thông báo thống nhất
/// và các hàm đọc dữ liệu nhập (bắt buộc, số tiền, ngày giờ).
/// </summary>
public abstract class ViewModelBase : ObservableObject, IFocusRequester
{
    protected ViewModelBase(IAppShell shell, IDialogService dialogs)
    {
        Shell = shell;
        Dialogs = dialogs;
    }

    /// <summary>Dành cho MainViewModel: chính nó là shell.</summary>
    private protected ViewModelBase(IDialogService dialogs)
    {
        Shell = this as IAppShell
            ?? throw new InvalidOperationException("ViewModel gốc phải cài đặt IAppShell.");
        Dialogs = dialogs;
    }

    public event Action<string>? FocusRequested;

    protected IAppShell Shell { get; }

    protected IDialogService Dialogs { get; }

    protected void Notify(string message) => Shell.ShowStatus(message);

    protected void RequestFocus(string propertyName) => FocusRequested?.Invoke(propertyName);

    /// <summary>Chạy thao tác của người dùng; lỗi hiện ở thanh trạng thái và hộp thoại.</summary>
    protected void Run(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            var text = ex.InnerException is null
                ? ex.Message
                : ex.Message + "\n" + ex.GetBaseException().Message;
            Notify("Lỗi: " + text);
            Dialogs.ShowError(text, "Không thể thực hiện");
        }
    }

    /// <summary>Lấy chuỗi đã bỏ khoảng trắng; nếu trống thì đặt con trỏ vào ô đó và báo lỗi.</summary>
    protected string Required(string? value, string label,
        [CallerArgumentExpression(nameof(value))] string propertyName = "")
    {
        var text = value?.Trim() ?? string.Empty;
        if (text.Length == 0)
        {
            RequestFocus(propertyName);
            throw new ArgumentException("Vui lòng nhập " + label + ".");
        }
        return text;
    }

    protected decimal PositiveMoney(string? value, string label,
        [CallerArgumentExpression(nameof(value))] string propertyName = "")
    {
        var text = Required(value, label, propertyName);
        if ((!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var result) &&
             !decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out result)) ||
            result <= 0m)
            throw new ArgumentException(label + " phải là số tiền lớn hơn 0.");
        return result;
    }

    protected double PositiveDouble(string? value, string label,
        [CallerArgumentExpression(nameof(value))] string propertyName = "")
    {
        var text = Required(value, label, propertyName);
        if ((!double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out var result) &&
             !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result)) ||
            !double.IsFinite(result) || result <= 0.0)
            throw new ArgumentException(label + " phải là số lớn hơn 0.");
        return result;
    }

    /// <summary>
    /// Hiện số tiền trong ô nhập, bỏ số 0 thừa sau dấu thập phân
    /// (SQLite trả 280000 thành 280000.0).
    /// </summary>
    protected static string EditableNumber(decimal value) => value.ToString("G29", CultureInfo.CurrentCulture);

    /// <summary>Ghép ngày (DatePicker) với giờ dạng HH:mm.</summary>
    protected static DateTime EnteredDateTime(DateTime? date, string? time, string label)
    {
        if (date is { } day &&
            TimeSpan.TryParseExact(time?.Trim(), "hh\\:mm", CultureInfo.InvariantCulture, out var clock) &&
            clock >= TimeSpan.Zero && clock < TimeSpan.FromDays(1))
            return day.Date.Add(clock);
        throw new ArgumentException("Vui lòng nhập " + label + " đúng ngày và giờ HH:mm.");
    }
}
