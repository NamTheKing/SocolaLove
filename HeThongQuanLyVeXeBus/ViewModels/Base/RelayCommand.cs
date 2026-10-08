using System.Windows.Input;

namespace HeThongQuanLyVeXeBus.ViewModels.Base;

/// <summary>ICommand gọi một phương thức của ViewModel; nút bấm tự mờ đi khi CanExecute trả về false.</summary>
public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public void Execute(object? parameter)
    {
        if (CanExecute(parameter))
            execute();
    }

    /// <summary>Gọi khi điều kiện của CanExecute thay đổi để View cập nhật trạng thái nút.</summary>
    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
