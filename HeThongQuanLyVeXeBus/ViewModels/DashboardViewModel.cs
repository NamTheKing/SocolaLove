using System.Windows.Input;
using HeThongQuanLyVeXeBus.Models;
using HeThongQuanLyVeXeBus.Services;
using HeThongQuanLyVeXeBus.ViewModels.Base;

namespace HeThongQuanLyVeXeBus.ViewModels;

/// <summary>Màn "Tổng quan": các con số tổng hợp.</summary>
public sealed class DashboardViewModel : ViewModelBase
{
    private int _routeCount;
    private int _activeBusCount;
    private int _upcomingTripCount;
    private int _validTicketCount;
    private decimal _validRevenue;

    public DashboardViewModel(IAppShell shell, IDialogService dialogs) : base(shell, dialogs)
    {
    }

    public int RouteCount { get => _routeCount; private set => SetProperty(ref _routeCount, value); }
    public int ActiveBusCount { get => _activeBusCount; private set => SetProperty(ref _activeBusCount, value); }
    public int UpcomingTripCount { get => _upcomingTripCount; private set => SetProperty(ref _upcomingTripCount, value); }
    public int ValidTicketCount { get => _validTicketCount; private set => SetProperty(ref _validTicketCount, value); }
    public decimal ValidRevenue { get => _validRevenue; private set => SetProperty(ref _validRevenue, value); }

    public ICommand RefreshCommand => Shell.RefreshCommand;

    public void Load(IReadOnlyList<BusRoute> routes, IReadOnlyList<Bus> buses,
        IReadOnlyList<Trip> trips, IReadOnlyList<Ticket> tickets)
    {
        var validTickets = tickets.Where(t => t.Status is TicketStatus.Paid or TicketStatus.CheckedIn).ToList();
        RouteCount = routes.Count;
        ActiveBusCount = buses.Count(b => b.IsActive);
        UpcomingTripCount = trips.Count(t =>
            t.Status is TripStatus.Scheduled or TripStatus.Boarding && t.DepartureTime > DateTime.Now);
        ValidTicketCount = validTickets.Count;
        ValidRevenue = validTickets.Sum(t => t.Price);
    }
}
