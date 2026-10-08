using HeThongQuanLyVeXeBus.Data;
using HeThongQuanLyVeXeBus.Models;
using Microsoft.EntityFrameworkCore;

namespace HeThongQuanLyVeXeBus.Services;

/// <summary>Quy tắc nghiệp vụ của đội xe.</summary>
public sealed class BusService(IDbContextFactory<AppDbContext> dbFactory)
{
    public List<Bus> GetAll()
    {
        using var db = dbFactory.CreateDbContext();
        return db.Buses.AsNoTracking().OrderBy(b => b.PlateNumber).ToList();
    }

    /// <summary>Thêm mới (Id = 0) hoặc cập nhật xe; trả về Id của xe đã lưu.</summary>
    public int Save(Bus values)
    {
        if (values.TotalSeats is <= 0 or > 500)
            throw new ArgumentException("Số chỗ phải từ 1 đến 500.");
        if (values.NumberOfFloors is not (1 or 2))
            throw new ArgumentException("Chọn số tầng của xe.");

        var id = values.Id;
        var plate = values.PlateNumber;
        using var db = dbFactory.CreateDbContext();
        if (db.Buses.Any(b => b.Id != id && b.PlateNumber.ToUpper() == plate.ToUpper()))
            throw new InvalidOperationException("Biển số đã tồn tại.");

        var isNew = id == 0;
        var bus = isNew ? new Bus() : db.Buses.Single(b => b.Id == id);
        if (!isNew && db.Tickets.Any(t => t.Trip.BusId == id) &&
            (bus.PlateNumber != values.PlateNumber || bus.BusModel != values.BusModel ||
             bus.BusTypeName != values.BusTypeName || bus.TotalSeats != values.TotalSeats ||
             bus.NumberOfFloors != values.NumberOfFloors))
            throw new InvalidOperationException("Xe đã có vé bán: chỉ được thay đổi tiện ích và trạng thái hoạt động.");

        bus.PlateNumber = values.PlateNumber;
        bus.BusModel = values.BusModel;
        bus.BusTypeName = values.BusTypeName;
        bus.TotalSeats = values.TotalSeats;
        bus.NumberOfFloors = values.NumberOfFloors;
        bus.HasWifi = values.HasWifi;
        bus.HasAirConditioner = values.HasAirConditioner;
        bus.IsActive = values.IsActive;
        if (isNew)
            db.Buses.Add(bus);
        db.SaveChanges();
        return bus.Id;
    }
}
