using HeThongQuanLyVeXeBus.Models;
using HeThongQuanLyVeXeBus.Services;
using HeThongQuanLyVeXeBus.ViewModels.Base;

namespace HeThongQuanLyVeXeBus.ViewModels;

/// <summary>Màn "Bán vé": bán vé tại quầy và lưu vé PDF.</summary>
public sealed class BookingViewModel : ViewModelBase
{
    private readonly TicketService _ticketService;
    private IReadOnlyList<Trip> _openTrips = [];
    private Trip? _selectedTrip;
    private string _customerName = string.Empty;
    private string _customerPhone = string.Empty;
    private string _customerEmail = string.Empty;
    private string _pickupPoint = string.Empty;
    private string _dropoffPoint = string.Empty;
    private string? _paymentMethod;
    private string _bookingResult = "Chưa có vé nào.";
    private string? _pdfTicketCode;
    private bool _pdfAllowed;

    public BookingViewModel(IAppShell shell, IDialogService dialogs, TicketService ticketService)
        : base(shell, dialogs)
    {
        _ticketService = ticketService;
        _paymentMethod = PaymentMethods[0];
        BookCommand = new RelayCommand(Book);
        ExportPdfCommand = new RelayCommand(ExportPdf, () => _pdfTicketCode is not null && _pdfAllowed);
    }

    /// <summary>Chuyến còn nhận khách: sắp chạy/đang đón, chưa khởi hành, tuyến và xe còn hoạt động.</summary>
    public IReadOnlyList<Trip> OpenTrips { get => _openTrips; private set => SetProperty(ref _openTrips, value); }

    public Trip? SelectedTrip
    {
        get => _selectedTrip;
        set
        {
            if (!SetProperty(ref _selectedTrip, value) || value is null)
                return;
            PickupPoint = value.Route.Origin;
            DropoffPoint = value.Route.Destination;
        }
    }

    public string CustomerName { get => _customerName; set => SetProperty(ref _customerName, value); }
    public string CustomerPhone { get => _customerPhone; set => SetProperty(ref _customerPhone, value); }
    public string CustomerEmail { get => _customerEmail; set => SetProperty(ref _customerEmail, value); }
    public string PickupPoint { get => _pickupPoint; set => SetProperty(ref _pickupPoint, value); }
    public string DropoffPoint { get => _dropoffPoint; set => SetProperty(ref _dropoffPoint, value); }

    public IReadOnlyList<string> PaymentMethods { get; } = ["Tiền mặt", "Chuyển khoản (đã xác nhận)"];
    public string? PaymentMethod { get => _paymentMethod; set => SetProperty(ref _paymentMethod, value); }

    /// <summary>Dòng tóm tắt vé vừa bán hoặc vé đang chọn ở màn soát vé.</summary>
    public string BookingResult { get => _bookingResult; private set => SetProperty(ref _bookingResult, value); }

    public RelayCommand BookCommand { get; }
    public RelayCommand ExportPdfCommand { get; }

    public void Load(IReadOnlyList<Trip> trips)
    {
        var selectedId = SelectedTrip?.Id;
        OpenTrips = trips
            .Where(t => t.Status is TripStatus.Scheduled or TripStatus.Boarding &&
                        t.DepartureTime > DateTime.Now && t.Route.IsActive && t.Bus.IsActive)
            .OrderBy(t => t.DepartureTime)
            .ToList();
        SelectedTrip = OpenTrips.FirstOrDefault(t => t.Id == selectedId);
    }

    /// <summary>Nhận vé được chọn ở màn "Vé / soát vé" để có thể xuất lại PDF.</summary>
    public void ShowTicket(Ticket ticket, string summary)
    {
        BookingResult = summary;
        SetPdfTicket(ticket.TicketCode, ticket.Status != TicketStatus.Cancelled);
    }

    private void SetPdfTicket(string code, bool allowed)
    {
        _pdfTicketCode = code;
        _pdfAllowed = allowed;
        ExportPdfCommand.NotifyCanExecuteChanged();
    }

    private void Book() => Run(() =>
    {
        if (SelectedTrip is not { } trip)
            throw new ArgumentException("Chọn chuyến xe đang nhận khách.");
        var name = Required(CustomerName, "tên hành khách");
        var phone = Required(CustomerPhone, "số điện thoại");
        var pickup = Required(PickupPoint, "điểm đón");
        var dropoff = Required(DropoffPoint, "điểm trả");
        var payment = PaymentMethod ?? throw new ArgumentException("Chọn phương thức thanh toán đã xác nhận.");
        var email = string.IsNullOrWhiteSpace(CustomerEmail) ? null : CustomerEmail.Trim();

        var ticket = _ticketService.Book(trip.Id, name, phone, email, pickup, dropoff, payment);
        Shell.ReloadData();
        CustomerName = string.Empty;
        CustomerPhone = string.Empty;
        CustomerEmail = string.Empty;
        SetPdfTicket(ticket.TicketCode, allowed: true);
        BookingResult = $"Đã bán vé {ticket.TicketCode} | {ticket.CustomerName} | {ticket.Price:N0} ₫";
        Notify("Đã bán vé " + ticket.TicketCode + ". Chọn 'Lưu vé thành PDF' để xuất file.");
    });

    private void ExportPdf() => Run(() =>
    {
        if (_pdfTicketCode is null)
            throw new ArgumentException("Chưa có vé để xuất PDF.");
        var ticket = _ticketService.GetForPdf(_pdfTicketCode);
        var path = Dialogs.AskSaveFilePath("Lưu vé hành khách", "Tài liệu PDF (*.pdf)|*.pdf", ".pdf",
            ticket.TicketCode + ".pdf");
        if (path is null)
            return;
        TicketPdfExporter.Export(ticket, path);
        Notify("Đã lưu vé PDF: " + path);
    });
}
