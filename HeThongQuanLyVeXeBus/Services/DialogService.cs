using System.Windows;
using HeThongQuanLyVeXeBus.ViewModels;
using HeThongQuanLyVeXeBus.Views;
using Microsoft.Win32;

namespace HeThongQuanLyVeXeBus.Services;

/// <summary>Cài đặt <see cref="IDialogService"/> bằng các hộp thoại WPF.</summary>
public sealed class DialogService : IDialogService
{
    private static Window? Owner => Application.Current?.MainWindow;

    public void ShowError(string message, string title)
    {
        if (Owner is { } owner)
            MessageBox.Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        else
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public bool Confirm(string message, string title)
    {
        var result = Owner is { } owner
            ? MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning)
            : MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
        return result == MessageBoxResult.Yes;
    }

    public string? AskSaveFilePath(string title, string filter, string defaultExtension, string fileName)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = filter,
            DefaultExt = defaultExtension,
            AddExtension = true,
            FileName = fileName
        };
        var accepted = Owner is { } owner ? dialog.ShowDialog(owner) : dialog.ShowDialog();
        return accepted == true ? dialog.FileName : null;
    }

    public bool TryChooseRefund(string ticketCode, out bool refunded)
    {
        var viewModel = new RefundChoiceViewModel(ticketCode);
        var window = new RefundChoiceWindow { Owner = Owner, DataContext = viewModel };
        var accepted = window.ShowDialog() == true;
        refunded = accepted && viewModel.IsRefunded;
        return accepted;
    }

    public string? ScanQrCode()
    {
        var scanner = new CameraScanWindow { Owner = Owner };
        return scanner.ShowDialog() == true ? scanner.ScannedCode : null;
    }
}
