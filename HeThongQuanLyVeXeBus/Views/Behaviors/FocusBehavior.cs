using System.Windows;
using HeThongQuanLyVeXeBus.ViewModels.Base;

namespace HeThongQuanLyVeXeBus.Views.Behaviors;

/// <summary>
/// Gắn tên thuộc tính của ViewModel vào một ô nhập: khi ViewModel gọi RequestFocus
/// với đúng tên đó, ô nhập sẽ nhận con trỏ. Ví dụ:
/// <c>&lt;TextBox Text="{Binding RouteCode}" b:FocusBehavior.Key="RouteCode"/&gt;</c>
/// </summary>
public static class FocusBehavior
{
    public static readonly DependencyProperty KeyProperty = DependencyProperty.RegisterAttached(
        "Key", typeof(string), typeof(FocusBehavior), new PropertyMetadata(null, OnKeyChanged));

    private static readonly DependencyProperty HandlerProperty = DependencyProperty.RegisterAttached(
        "Handler", typeof(Action<string>), typeof(FocusBehavior));

    public static string? GetKey(DependencyObject element) => (string?)element.GetValue(KeyProperty);

    public static void SetKey(DependencyObject element, string? value) => element.SetValue(KeyProperty, value);

    private static void OnKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;
        element.DataContextChanged -= OnDataContextChanged;
        element.DataContextChanged += OnDataContextChanged;
        Subscribe(element, element.DataContext);
    }

    private static void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var element = (FrameworkElement)sender;
        if (e.OldValue is IFocusRequester old && element.GetValue(HandlerProperty) is Action<string> handler)
            old.FocusRequested -= handler;
        Subscribe(element, e.NewValue);
    }

    private static void Subscribe(FrameworkElement element, object? dataContext)
    {
        if (dataContext is not IFocusRequester requester)
            return;
        if (element.GetValue(HandlerProperty) is not Action<string> handler)
        {
            handler = propertyName =>
            {
                if (propertyName == GetKey(element))
                    element.Focus();
            };
            element.SetValue(HandlerProperty, handler);
        }
        requester.FocusRequested -= handler;
        requester.FocusRequested += handler;
    }
}
