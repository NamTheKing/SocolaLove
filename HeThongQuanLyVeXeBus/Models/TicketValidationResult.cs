namespace HeThongQuanLyVeXeBus.Models;

public class TicketValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public string StatusBadgeColor { get; set; } = "#EF4444"; // Red or Green or Amber
    public Ticket? Ticket { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.Now;
    public bool CanCheckIn { get; set; }
}
