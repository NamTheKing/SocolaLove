using System.Windows.Input;

namespace HeThongQuanLyVeXeBus.ViewModels.Base;

/// <summary>Những gì các ViewModel con cần từ ViewModel của cửa sổ chính.</summary>
public interface IAppShell
{
    /// <summary>Database đã được khởi tạo và dữ liệu đã tải.</summary>
    bool IsReady { get; }

    /// <summary>Lệnh "Làm mới dữ liệu" dùng chung cho nhiều màn hình.</summary>
    ICommand RefreshCommand { get; }

    /// <summary>Tải lại toàn bộ danh sách từ database và giữ nguyên dòng đang chọn.</summary>
    void ReloadData();

    /// <summary>Hiện thông báo ở thanh trạng thái cuối cửa sổ.</summary>
    void ShowStatus(string message);
}
