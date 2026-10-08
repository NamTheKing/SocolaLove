# Quản lý vé xe liên tỉnh

Ứng dụng quầy vé WPF trên Windows (.NET 10), lưu dữ liệu cục bộ bằng SQLite. Chạy bằng `dotnet run --project HeThongQuanLyVeXeBus/HeThongQuanLyVeXeBus.csproj` hoặc mở `HeThongQuanLyVeXeBus.slnx` trong Visual Studio. Lần đầu khởi động tạo dữ liệu mẫu; tệp `bus_tickets.db` nằm cạnh tệp thực thi trong thư mục `bin/...`. Các lần chạy sau giữ dữ liệu đã nhập. Cần quyền ghi ở thư mục triển khai; sao lưu tệp database khi ứng dụng đã đóng.

Giao diện có menu bên trái: **Tổng quan**, **Tuyến xe**, **Đội xe**, **Chuyến xe**, **Bán vé**, **Vé / soát vé**, **Doanh thu**. Các màn quản lý gồm biểu mẫu bên trái và bảng dữ liệu bên phải: chọn một dòng để sửa, bấm **Thêm mới** để nhập bản ghi mới. Ngày tháng và số tiền hiển thị theo định dạng Việt Nam.

## Mở bằng Visual Studio Community 2026

1. Cài workload **.NET desktop development** (có sẵn .NET 10 SDK).
2. Mở `HeThongQuanLyVeXeBus.slnx` (**File → Open → Project/Solution**).
3. Bấm **F5**. Lần build đầu Visual Studio tự tải các gói NuGet.

Nếu build báo lỗi đường dẫn quá dài, hãy đặt thư mục dự án ở vị trí ngắn hơn (ví dụ `C:\Projects\HeThongQuanLyVeXeBus`).

## Kiến trúc MVVM

```
HeThongQuanLyVeXeBus/
├─ Models/        Thực thể dữ liệu (BusRoute, Bus, Trip, Ticket, ...)
├─ Data/          AppDbContext (EF Core + SQLite), DbInitializer, AppDbContextFactory
├─ Services/      Nghiệp vụ và truy cập dữ liệu: RouteService, BusService, TripService,
│                 TicketService, BookingService, RevenueReportService, xuất PDF/Excel,
│                 IDialogService/DialogService (hộp thoại)
├─ ViewModels/    Trạng thái và lệnh của từng màn hình
│  ├─ Base/       ObservableObject, RelayCommand, ViewModelBase, IAppShell
│  └─ MainViewModel + Dashboard/Routes/Buses/Trips/Booking/Tickets/RevenueViewModel
├─ Views/         XAML: MainWindow, các UserControl cho từng tab, RefundChoiceWindow,
│                 CameraScanWindow, Behaviors/FocusBehavior
└─ Resources/     Styles.xaml (kiểu dáng dùng chung)
```

- **View** chỉ chứa XAML; code-behind chỉ gọi `InitializeComponent()`. Dữ liệu hiển thị qua `{Binding}`, nút bấm gọi `Command`.
- **ViewModel** không tham chiếu control WPF: đọc/ghi thuộc tính, gọi Service, báo lỗi qua `IDialogService`.
- **Service** chứa quy tắc nghiệp vụ (ví dụ không cho sửa tuyến đã bán vé, không xếp một xe cho hai chuyến trùng giờ) và mỗi thao tác dùng một `DbContext` riêng.
- `App.xaml.cs` là nơi lắp ráp: tạo Service → `MainViewModel` → gán làm `DataContext` của `MainWindow`.
- `CameraScanWindow` là thành phần giao diện gắn với phần cứng (camera), được gọi qua `IDialogService.ScanQrCode()`.

## Quy trình quầy vé

1. Tạo tuyến (điểm đi, điểm đến, cự ly, thời gian, giá cơ bản), xe (biển số, **sức chứa**, số tầng), rồi tạo chuyến (tuyến, xe, giờ đi/đến, tài xế, giá vé). Không xếp cùng một xe cho các chuyến trùng thời gian.
2. Mở **Bán vé**, chọn chuyến còn hoạt động, nhập khách, điện thoại, điểm đón/trả; không chọn vị trí ghế hoặc xem số ghế trống. Với mỗi chuyến, tổng vé đã thanh toán và đã soát không được vượt quá sức chứa của xe (ví dụ xe 40 chỗ thì tối đa 40 hành khách); vé đã hủy không chiếm suất, chuyến khác dùng cùng xe có hạn mức riêng. Chỉ bấm xác nhận bán khi **đã nhận tiền mặt hoặc tự kiểm tra chuyển khoản**. Ứng dụng không kết nối ngân hàng và không tự xác minh giao dịch. Bấm **Lưu vé thành PDF** để lưu vé có mã QR. Muốn xuất lại, chọn vé tại **Vé / soát vé** rồi trở về **Bán vé** để lưu PDF; màn soát vé không có nút xuất PDF. Vé đã hủy không thể xuất lại thành vé đi xe. Vé PDF và báo cáo Excel không ghi vị trí ghế.
3. Tại **Vé / soát vé**, ô tìm kiếm lọc theo mã vé, tên khách, số điện thoại hoặc mã chuyến. Nút **Quét QR bằng camera** mở camera máy tính, chọn thiết bị nếu có nhiều camera và hướng mã QR trên vé PDF vào camera; ứng dụng giải mã, tìm đúng vé và hiển thị kết quả kiểm tra. Quét không tự soát: nhân viên bấm **Soát vé / lên xe** khi vé hợp lệ. Nếu Windows chặn camera hoặc camera đang bị ứng dụng khác sử dụng, kiểm tra quyền truy cập camera rồi thử lại. Vé đã hủy/đã soát không được soát lại.
4. Bấm **Hủy vé** và chọn **Chưa hoàn tiền** hoặc **Đã hoàn tiền** đúng với số tiền thực đã trả cho khách. Vé hủy trước khi khởi hành trả lại một suất đăng ký cho chuyến; vé đã soát không được hủy. Với vé hủy còn nợ tiền khách, sau khi thực hoàn tiền bấm **Xác nhận đã hoàn tiền**. Hủy cả chuyến đánh dấu các vé chưa soát là **chưa hoàn tiền**; nhân viên xử lý từng vé sau. Ứng dụng chỉ ghi nhận tình trạng, **không tự chuyển tiền**.
5. Nút **Xuất Excel** xuất đúng danh sách đang hiển thị theo ô tìm kiếm, gồm hai trang **Tất cả vé** và **Đã soát vé**; cột hoàn tiền/ngày hoàn cho phép theo dõi vé đã hủy. Tổng giá vé chưa hủy không phải báo cáo đối soát ngân hàng.

Ứng dụng dùng một tệp SQLite tại một máy quầy; dữ liệu mẫu và danh sách người dùng **không phải cơ chế đăng nhập/phân quyền**. Không đưa tệp database lên thư mục chia sẻ để nhiều máy cùng ghi, và không coi đây là hệ thống thanh toán online. Lần đầu chạy bản có tính năng hoàn tiền, ứng dụng tự thêm cột `RefundedAt` vào database cũ mà không xóa vé; vé đã hủy trước khi nâng cấp được coi là **chưa hoàn tiền** cho đến khi nhân viên xác nhận. Nên sao lưu database khi ứng dụng đã đóng trước mỗi lần nâng cấp.

## Báo cáo doanh thu

Mở tab **Doanh thu** (mặc định là tháng hiện tại, tất cả tuyến), chọn khoảng **ngày bán vé** và tuyến rồi bấm **Xem báo cáo**. Màn hình chỉ hiện 4 số liệu chính — **Doanh thu**, **Vé hợp lệ** (kèm số vé đã soát), **Vé đã hủy**, **Chờ hoàn tiền** — và một bảng **doanh thu theo tuyến**. Chỉ vé chưa hủy được tính vào doanh thu; trạng thái hoàn tiền được ghi nhận thủ công, không phải đối soát ngân hàng.

Bấm **Xuất Excel** để lưu báo cáo chi tiết `.xlsx` theo khoảng ngày bán vé và tuyến đang chọn (nút xuất cũng cập nhật báo cáo trên màn hình). File gồm các trang **Tổng hợp**, **Xu hướng**, **Theo tuyến**, **Thanh toán**, **Theo chuyến** và **Danh sách vé**; kỳ không có vé vẫn xuất được báo cáo với doanh thu bằng 0. Giá vé đã hủy vẫn có trong danh sách đối chiếu nhưng không được cộng vào doanh thu.

