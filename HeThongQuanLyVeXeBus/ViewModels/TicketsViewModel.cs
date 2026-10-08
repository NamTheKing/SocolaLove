using System.Windows.Input;
using HeThongQuanLyVeXeBus.Models;
using HeThongQuanLyVeXeBus.Services;
using HeThongQuanLyVeXeBus.ViewModels.Base;

namespace HeThongQuanLyVeXeBus.ViewModels;

/// <summary>Màn "Vé / soát vé": tra cứu, quét QR, soát vé, hủy vé và hoàn tiền.</summary>
public sealed class TicketsViewModel : ViewModelBase
{
    private const string NoTicketSelected = "Chọn một vé trong bảng để soát vé, hủy vé hoặc in lại.";

    private readonly TicketService _ticketService;
    private IReadOnlyList<Ticket> _allTickets = [];
    private IReadOnlyList<Ticket> _visibleTickets = [];
    private Ticket? _selectedTicket;
    private string _searchText = string.Empty;
    private string _ticketDetail = NoTicketSelected;

    public TicketsViewModel(IAppShell shell, IDialogService dialogs, TicketService ticketService)
        : base(shell, dialogs)
    {
        _ticketService = ticketService;
        ExportExcelCommand = new RelayCommand(ExportExcel);
        ScanQrCommand = new RelayCommand(ScanQr);
        CheckInCommand = new RelayCommand(CheckIn);
        CancelTicketCommand = new RelayCommand(CancelTicket);
        MarkRefundedCommand = new RelayCommand(MarkRefunded,
            () => SelectedTicket is { Status: TicketStatus.Cancelled, RefundedAt: null });
    }

    /// <summary>Báo cho màn Bán vé biết vé vừa chọn (kèm dòng tóm tắt).</summary>
    public event Action<Ticket, string>? TicketSelected;

    /// <summary>Danh sách sau khi lọc theo ô tìm kiếm; cũng là danh sách được xuất Excel.</summary>
    public IReadOnlyList<Ticket> VisibleTickets
    {
        get => _visibleTickets;
        private set => SetProperty(ref _visibleTickets, value);
    }

    /// <summary>Lọc theo mã vé, tên khách, số điện thoại hoặc mã chuyến.</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilter();
        }
    }

    public Ticket? SelectedTicket
    {
        get => _selectedTicket;
        set
        {
            if (!SetProperty(ref _selectedTicket, value))
                return;
            MarkRefundedCommand.NotifyCanExecuteChanged();
            if (value is null)
            {
                TicketDetail = NoTicketSelected;
                return;
            }
            TicketDetail = $"{value.TicketCode} | {value.CustomerName} - {value.CustomerPhone} | " +
                           $"{value.Trip.Route.RouteName} | {value.Trip.DepartureTime:dd/MM/yyyy HH:mm} | " +
                           $"{value.StatusText} | {value.PaymentMethod}";
            TicketSelected?.Invoke(value, TicketDetail);
        }
    }

    public string TicketDetail { get => _ticketDetail; private set => SetProperty(ref _ticketDetail, value); }

    public ICommand RefreshCommand => Shell.RefreshCommand;
    public RelayCommand ExportExcelCommand { get; }
    public RelayCommand ScanQrCommand { get; }
    public RelayCommand CheckInCommand { get; }
    public RelayCommand CancelTicketCommand { get; }
    public RelayCommand MarkRefundedCommand { get; }

    public void Load(IReadOnlyList<Ticket> tickets)
    {
        var selectedId = SelectedTicket?.Id;
        _allTickets = tickets;
        ApplyFilter();
        SelectedTicket = VisibleTickets.FirstOrDefault(t => t.Id == selectedId);
    }

    private void ApplyFilter()
    {
        var search = SearchText.Trim();
        VisibleTickets = _allTickets
            .Where(t => search.Length == 0 ||
                        t.TicketCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        t.CustomerName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        t.CustomerPhone.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        t.Trip.TripCode.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>Vé đang chọn; nếu chưa chọn thì dùng đúng nội dung ô tìm kiếm làm mã vé.</summary>
    private string SelectedTicketCode()
    {
        if (SelectedTicket is { } ticket)
            return ticket.TicketCode;
        var text = SearchText.Trim();
        if (text.Length == 0)
            throw new ArgumentException("Chọn vé hoặc nhập chính xác mã vé cần kiểm tra.");
        return text;
    }

    private void ExportExcel() => Run(() =>
    {
        var tickets = VisibleTickets.ToArray();
        if (tickets.Length == 0)
            throw new InvalidOperationException("Danh sách đang hiển thị không có vé để xuất.");
        var path = Dialogs.AskSaveFilePath("Xuất danh sách vé và soát vé", "Sổ Excel (*.xlsx)|*.xlsx", ".xlsx",
            $"DanhSachVe_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        if (path is null)
            return;
        TicketExcelExporter.Export(tickets, path);
        Notify($"Đã xuất {tickets.Length} vé ra Excel: {path}");
    });

    private void ScanQr() => Run(() =>
    {
        var scanned = Dialogs.ScanQrCode();
        if (string.IsNullOrWhiteSpace(scanned))
            return;
        var code = scanned.Trim().ToUpperInvariant();
        SearchText = code;
        SelectedTicket = VisibleTickets.FirstOrDefault(t => t.TicketCode == code);
        var result = _ticketService.Validate(code);
        TicketDetail = $"{result.Message} | Có thể soát vé: {(result.CanCheckIn ? "Có" : "Không")}";
        Notify(result.Message);
    });

    private void CheckIn() => Run(() =>
    {
        var ticket = _ticketService.CheckIn(SelectedTicketCode());
        Shell.ReloadData();
        Notify($"Đã soát vé {ticket.TicketCode}.");
    });

    private void CancelTicket() => Run(() =>
    {
        var code = SelectedTicketCode();
        if (!Dialogs.TryChooseRefund(code, out var refunded))
            return;
        var ticket = _ticketService.Cancel(code, refunded);
        Shell.ReloadData();
        Notify($"Đã hủy vé {ticket.TicketCode}; {ticket.StatusText}. Chuyến có thể nhận thêm một hành khách.");
    });

    private void MarkRefunded() => Run(() =>
    {
        var code = SelectedTicketCode();
        if (!Dialogs.Confirm($"Chỉ xác nhận đã hoàn tiền cho vé {code} nếu tiền đã thực trả cho khách.",
                "Xác nhận hoàn tiền"))
            return;
        var ticket = _ticketService.MarkRefunded(code);
        Shell.ReloadData();
        Notify($"Vé {ticket.TicketCode}: đã ghi nhận hoàn tiền.");
    });
}
