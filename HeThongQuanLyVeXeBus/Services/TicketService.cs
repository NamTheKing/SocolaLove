using HeThongQuanLyVeXeBus.Data;
using HeThongQuanLyVeXeBus.Models;
using Microsoft.EntityFrameworkCore;

namespace HeThongQuanLyVeXeBus.Services;

/// <summary>
/// Các thao tác vé dùng ở giao diện. Mỗi thao tác mở một DbContext riêng
/// rồi giao cho <see cref="BookingService"/> xử lý quy tắc bán/soát/hủy.
/// </summary>
public sealed class TicketService(IDbContextFactory<AppDbContext> dbFactory)
{
    public List<Ticket> GetAll()
    {
        using var db = dbFactory.CreateDbContext();
        return db.Tickets.AsNoTracking()
            .Include(t => t.Trip).ThenInclude(t => t.Route)
            .OrderByDescending(t => t.BookedAt)
            .ToList();
    }

    public Ticket Book(int tripId, string name, string phone, string? email,
        string pickup, string dropoff, string payment)
    {
        using var db = dbFactory.CreateDbContext();
        return new BookingService(db).Book(tripId, name, phone, email, pickup, dropoff, payment);
    }

    public Ticket CheckIn(string ticketCode)
    {
        using var db = dbFactory.CreateDbContext();
        return new BookingService(db).CheckIn(ticketCode);
    }

    public Ticket Cancel(string ticketCode, bool refunded)
    {
        using var db = dbFactory.CreateDbContext();
        return new BookingService(db).Cancel(ticketCode, refunded);
    }

    public Ticket MarkRefunded(string ticketCode)
    {
        using var db = dbFactory.CreateDbContext();
        return new BookingService(db).MarkRefunded(ticketCode);
    }

    public TicketValidationResult Validate(string ticketCode)
    {
        using var db = dbFactory.CreateDbContext();
        return new BookingService(db).Validate(ticketCode);
    }

    /// <summary>Lấy vé kèm chuyến, tuyến và xe để xuất PDF; vé đã hủy không được xuất.</summary>
    public Ticket GetForPdf(string ticketCode)
    {
        var code = ticketCode.Trim().ToUpperInvariant();
        using var db = dbFactory.CreateDbContext();
        var ticket = db.Tickets.AsNoTracking()
            .Include(t => t.Trip).ThenInclude(t => t.Route)
            .Include(t => t.Trip).ThenInclude(t => t.Bus)
            .SingleOrDefault(t => t.TicketCode == code)
            ?? throw new ArgumentException("Không tìm thấy mã vé cần xuất PDF.");
        if (ticket.Status == TicketStatus.Cancelled)
            throw new InvalidOperationException("Không thể xuất vé đã hủy thành PDF.");
        return ticket;
    }
}
