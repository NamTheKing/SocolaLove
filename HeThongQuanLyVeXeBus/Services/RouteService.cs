using HeThongQuanLyVeXeBus.Data;
using HeThongQuanLyVeXeBus.Models;
using Microsoft.EntityFrameworkCore;

namespace HeThongQuanLyVeXeBus.Services;

/// <summary>Quy tắc nghiệp vụ của tuyến xe.</summary>
public sealed class RouteService(IDbContextFactory<AppDbContext> dbFactory)
{
    public List<BusRoute> GetAll()
    {
        using var db = dbFactory.CreateDbContext();
        return db.Routes.AsNoTracking().OrderBy(r => r.RouteCode).ToList();
    }

    /// <summary>Thêm mới (Id = 0) hoặc cập nhật tuyến; trả về Id của tuyến đã lưu.</summary>
    public int Save(BusRoute values)
    {
        if (string.Equals(values.Origin, values.Destination, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Điểm đi và điểm đến phải khác nhau.");

        var id = values.Id;
        var code = values.RouteCode;
        using var db = dbFactory.CreateDbContext();
        if (db.Routes.Any(r => r.Id != id && r.RouteCode.ToUpper() == code.ToUpper()))
            throw new InvalidOperationException("Mã tuyến đã tồn tại.");

        var isNew = id == 0;
        var route = isNew ? new BusRoute() : db.Routes.Single(r => r.Id == id);
        if (!isNew && db.Tickets.Any(t => t.Trip.RouteId == id) &&
            (route.RouteCode != values.RouteCode || route.RouteName != values.RouteName ||
             route.Origin != values.Origin || route.Destination != values.Destination ||
             route.DistanceKm != values.DistanceKm || route.EstimatedHours != values.EstimatedHours ||
             route.BasePrice != values.BasePrice))
            throw new InvalidOperationException("Tuyến đã có vé bán: chỉ được thay đổi trạng thái hoạt động.");

        route.RouteCode = values.RouteCode;
        route.RouteName = values.RouteName;
        route.Origin = values.Origin;
        route.Destination = values.Destination;
        route.DistanceKm = values.DistanceKm;
        route.EstimatedHours = values.EstimatedHours;
        route.BasePrice = values.BasePrice;
        route.IsActive = values.IsActive;
        if (isNew)
            db.Routes.Add(route);
        db.SaveChanges();
        return route.Id;
    }
}
