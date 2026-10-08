using Microsoft.EntityFrameworkCore;

namespace HeThongQuanLyVeXeBus.Data;

/// <summary>Tạo một DbContext ngắn hạn cho mỗi thao tác tải/lưu dữ liệu.</summary>
public sealed class AppDbContextFactory : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext() => new();
}
