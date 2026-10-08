using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HeThongQuanLyVeXeBus.ViewModels.Base;

/// <summary>Cài đặt INotifyPropertyChanged để giao diện tự cập nhật khi thuộc tính thay đổi.</summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>Gán giá trị mới và báo cho View; trả về false nếu giá trị không đổi.</summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
