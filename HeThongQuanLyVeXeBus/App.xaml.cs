using System.Globalization;
using System.Windows;
using HeThongQuanLyVeXeBus.Data;
using HeThongQuanLyVeXeBus.Services;
using HeThongQuanLyVeXeBus.ViewModels;
using HeThongQuanLyVeXeBus.Views;

namespace HeThongQuanLyVeXeBus;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        var culture = CultureInfo.GetCultureInfo("vi-VN");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        // WPF đã gán sẵn văn hóa của Windows cho luồng giao diện trước khi OnStartup chạy,
        // nên phải đặt cả cho luồng hiện tại; nếu không, "280.000" bị đọc thành 280.
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        base.OnStartup(e);

        // Nơi lắp ráp ứng dụng: Service -> ViewModel -> View.
        var dbFactory = new AppDbContextFactory();
        var mainViewModel = new MainViewModel(
            new DatabaseService(dbFactory),
            new RouteService(dbFactory),
            new BusService(dbFactory),
            new TripService(dbFactory),
            new TicketService(dbFactory),
            new RevenueReportService(dbFactory),
            new DialogService());

        var window = new MainWindow { DataContext = mainViewModel };
        window.Loaded += (_, _) => mainViewModel.LoadCommand.Execute(null);
        MainWindow = window;
        window.Show();
    }
}
