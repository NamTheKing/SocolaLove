using HeThongQuanLyVeXeBus.Data;
using Microsoft.EntityFrameworkCore;

namespace HeThongQuanLyVeXeBus.Services;

/// <summary>Tạo/nâng cấp database SQLite và nạp dữ liệu mẫu lần đầu.</summary>
public sealed class DatabaseService(IDbContextFactory<AppDbContext> dbFactory)
{
    public void Initialize()
    {
        using var db = dbFactory.CreateDbContext();
        DbInitializer.Initialize(db);
    }
}
