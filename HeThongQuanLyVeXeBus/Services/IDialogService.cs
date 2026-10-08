namespace HeThongQuanLyVeXeBus.Services;

/// <summary>
/// Hộp thoại mà ViewModel cần. ViewModel chỉ gọi interface này nên không phụ thuộc
/// vào Window, MessageBox hay SaveFileDialog (dễ thay thế khi kiểm thử).
/// </summary>
public interface IDialogService
{
    void ShowError(string message, string title);

    /// <summary>Hỏi Có/Không; trả về true khi người dùng chọn Có.</summary>
    bool Confirm(string message, string title);

    /// <summary>Hỏi nơi lưu tệp; trả về null nếu người dùng bỏ qua.</summary>
    string? AskSaveFilePath(string title, string filter, string defaultExtension, string fileName);

    /// <summary>Hỏi tình trạng hoàn tiền khi hủy vé; trả về false nếu người dùng quay lại.</summary>
    bool TryChooseRefund(string ticketCode, out bool refunded);

    /// <summary>Mở camera quét QR; trả về nội dung mã hoặc null nếu đóng cửa sổ.</summary>
    string? ScanQrCode();
}
