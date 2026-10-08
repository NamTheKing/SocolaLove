namespace HeThongQuanLyVeXeBus.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Nhân viên"; // "Quản trị viên", "Nhân viên bán vé", "Kiểm soát viên"
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
