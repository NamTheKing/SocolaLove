using System.Globalization;
using HeThongQuanLyVeXeBus.Data;
using HeThongQuanLyVeXeBus.Models;
using Microsoft.EntityFrameworkCore;

namespace HeThongQuanLyVeXeBus.Services;

/// <summary>Quy tắc nghiệp vụ của lịch chuyến: phân xe, sửa chuyến và chuyển trạng thái.</summary>
public sealed class TripService(IDbContextFactory<AppDbContext> dbFactory)
{
    public List<Trip> GetAll()
    {
        using var db = dbFactory.CreateDbContext();
        return db.Trips.AsNoTracking()
            .Include(t => t.Route)
            .Include(t => t.Bus)
            .OrderByDescending(t => t.DepartureTime)
            .ToList();
    }

    /// <summary>Thêm mới (Id = 0) hoặc cập nhật chuyến; trả về chuyến đã lưu (có Id và mã chuyến).</summary>
    public Trip Save(Trip values)
    {
        var id = values.Id;
        var routeId = values.RouteId;
        var busId = values.BusId;
        var departure = values.DepartureTime;
        var arrival = values.EstimatedArrivalTime;
        if (arrival <= departure)
            throw new ArgumentException("Giờ đến phải sau giờ khởi hành.");

        using var db = dbFactory.CreateDbContext();
        var isNew = id == 0;
        var trip = isNew ? new Trip() : db.Trips.Single(t => t.Id == id);
        var scheduleChanged = isNew || trip.RouteId != routeId || trip.BusId != busId ||
            trip.DepartureTime != departure || trip.EstimatedArrivalTime != arrival ||
            trip.TicketPrice != values.TicketPrice;

        if (!isNew && trip.Status is not (TripStatus.Scheduled or TripStatus.Boarding))
            throw new InvalidOperationException("Chuyến đã xuất bến hoặc đã hủy không thể sửa.");
        if (!isNew && trip.Status == TripStatus.Boarding && scheduleChanged)
            throw new InvalidOperationException("Chuyến đang đón khách: chỉ được sửa tên và SĐT tài xế.");
        if (!isNew && scheduleChanged && db.Tickets.Any(t => t.TripId == id))
            throw new InvalidOperationException("Chuyến đã từng bán vé: chỉ được sửa tên và SĐT tài xế.");

        if (scheduleChanged)
        {
            if (departure <= DateTime.Now)
                throw new ArgumentException("Giờ khởi hành phải trong tương lai.");
            if (!db.Routes.Any(r => r.Id == routeId && r.IsActive) || !db.Buses.Any(b => b.Id == busId && b.IsActive))
                throw new InvalidOperationException("Tuyến hoặc xe không còn hoạt động.");
            if (db.Trips.Any(t => t.Id != id && t.BusId == busId && t.Status != TripStatus.Cancelled &&
                                  departure < t.EstimatedArrivalTime && arrival > t.DepartureTime))
                throw new InvalidOperationException("Xe đã được phân công chuyến khác trong khoảng thời gian này.");
        }

        if (isNew)
            trip.TripCode = NextTripCode(db, departure);
        trip.RouteId = routeId;
        trip.BusId = busId;
        trip.DepartureTime = departure;
        trip.EstimatedArrivalTime = arrival;
        trip.DriverName = values.DriverName;
        trip.DriverPhone = values.DriverPhone;
        trip.TicketPrice = values.TicketPrice;
        if (isNew)
            db.Trips.Add(trip);
        db.SaveChanges();
        return trip;
    }

    /// <summary>Kiểm tra trước (không ghi) để giao diện chỉ hỏi xác nhận khi thao tác hợp lệ.</summary>
    public void EnsureStatusChangeAllowed(int tripId, TripStatus value)
    {
        using var db = dbFactory.CreateDbContext();
        var trip = db.Trips.AsNoTracking().Single(t => t.Id == tripId);
        ValidateStatusChange(db, trip, value);
    }

    /// <summary>Đổi trạng thái chuyến; hủy chuyến sẽ hủy (chưa hoàn tiền) mọi vé chưa soát.</summary>
    public void ChangeStatus(int tripId, TripStatus value)
    {
        using var db = dbFactory.CreateDbContext();
        using var transaction = db.Database.BeginTransaction();
        var trip = db.Trips.Single(t => t.Id == tripId);
        ValidateStatusChange(db, trip, value);
        if (value == TripStatus.Cancelled)
        {
            var booking = new BookingService(db);
            var paidCodes = db.Tickets
                .Where(t => t.TripId == trip.Id && t.Status == TicketStatus.Paid)
                .Select(t => t.TicketCode)
                .ToList();
            foreach (var code in paidCodes)
                booking.Cancel(code);
        }
        trip.Status = value;
        db.SaveChanges();
        transaction.Commit();
    }

    private static void ValidateStatusChange(AppDbContext db, Trip trip, TripStatus value)
    {
        var allowed = trip.Status switch
        {
            TripStatus.Scheduled => value is TripStatus.Boarding or TripStatus.Cancelled,
            TripStatus.Boarding => value is TripStatus.Departed or TripStatus.Cancelled,
            TripStatus.Departed => value == TripStatus.Completed,
            _ => false
        };
        if (!allowed)
            throw new InvalidOperationException("Không thể chuyển trạng thái chuyến theo hướng này.");
        if (value == TripStatus.Cancelled &&
            db.Tickets.Any(t => t.TripId == trip.Id && t.Status == TicketStatus.CheckedIn))
            throw new InvalidOperationException("Không thể hủy chuyến đã có khách soát vé.");
    }

    private static string NextTripCode(AppDbContext db, DateTime departure)
    {
        var prefix = $"CX-{departure:yyyyMMdd}-";
        var number = 1;
        string code;
        do
        {
            code = prefix + number.ToString("D2", CultureInfo.InvariantCulture);
            number++;
        } while (db.Trips.Any(t => t.TripCode == code));
        return code;
    }
}
