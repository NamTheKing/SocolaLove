using System.Windows;

namespace HeThongQuanLyVeXeBus.Views;

/// <summary>Hộp thoại chọn tình trạng hoàn tiền; lựa chọn nằm ở RefundChoiceViewModel.</summary>
public partial class RefundChoiceWindow : Window
{
    public RefundChoiceWindow()
    {
        InitializeComponent();
    }

    // Đóng hộp thoại là việc của View; ViewModel chỉ giữ lựa chọn.
    private void Confirm_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
