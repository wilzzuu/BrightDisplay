using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Forms;
using Color = System.Windows.Media.Color;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace BrightDisplay;

public partial class MainWindow : Window
{
    private static readonly Color WarmColor = Color.FromRgb(255, 226, 189);
    private static readonly Color NeutralColor = Colors.White;
    private static readonly Color CoolColor = Color.FromRgb(230, 241, 255);
    private bool _monitorsLoaded;

    public MainWindow()
    {
        InitializeComponent();
        UpdateDisplay();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var screens = Screen.AllScreens;
        if (screens.Length == 0)
        {
            throw new InvalidOperationException("Windows did not report any available monitors.");
        }

        var monitors = screens
            .Select((screen, index) => new MonitorOption(
                screen,
                $"Monitor {index + 1}{(screen.Primary ? " (Primary)" : string.Empty)} — " +
                $"{screen.Bounds.Width} × {screen.Bounds.Height}"))
            .ToArray();

        MonitorSelector.ItemsSource = monitors;
        var currentScreen = Screen.FromHandle(new WindowInteropHelper(this).Handle);
        MonitorSelector.SelectedItem = monitors.FirstOrDefault(
            monitor => monitor.Screen.DeviceName == currentScreen.DeviceName) ?? monitors[0];
        _monitorsLoaded = true;
    }

    private void MonitorSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_monitorsLoaded || MonitorSelector.SelectedItem is not MonitorOption monitor)
        {
            return;
        }

        WindowState = WindowState.Normal;
        try
        {
            var bounds = monitor.Screen.Bounds;
            var handle = new WindowInteropHelper(this).Handle;
            if (!SetWindowPos(
                    handle,
                    IntPtr.Zero,
                    bounds.Left,
                    bounds.Top,
                    bounds.Width,
                    bounds.Height,
                    SwpNoZOrder | SwpNoActivate | SwpFrameChanged))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    $"Could not move BrightDisplay to {monitor.Label}.");
            }
        }
        finally
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void MenuToggleButton_Click(object sender, RoutedEventArgs e)
    {
        var isOpen = AdjustmentMenu.Visibility != Visibility.Visible;
        AdjustmentMenu.Visibility = isOpen ? Visibility.Visible : Visibility.Collapsed;
        MenuToggleButton.Content = isOpen ? "Close settings" : "Adjust display";
    }

    private void WarmPreset_Click(object sender, RoutedEventArgs e)
    {
        TemperatureSlider.Value = 2700;
    }

    private void NeutralPreset_Click(object sender, RoutedEventArgs e)
    {
        TemperatureSlider.Value = 4500;
    }

    private void CoolPreset_Click(object sender, RoutedEventArgs e)
    {
        TemperatureSlider.Value = 6500;
    }

    private void BrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (IsInitialized)
        {
            UpdateDisplay();
        }
    }

    private void TemperatureSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (IsInitialized)
        {
            UpdateDisplay();
        }
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void UpdateDisplay()
    {
        var temperature = TemperatureSlider.Value;
        var color = temperature <= 4500
            ? Interpolate(WarmColor, NeutralColor, (temperature - 2700) / 1800)
            : Interpolate(NeutralColor, CoolColor, (temperature - 4500) / 2000);
        var brightness = BrightnessSlider.Value / 100;

        DisplaySurface.Background = new SolidColorBrush(Color.FromRgb(
            Scale(color.R, brightness),
            Scale(color.G, brightness),
            Scale(color.B, brightness)));
        BrightnessValue.Text = $"{BrightnessSlider.Value:0}%";
        TemperatureValue.Text = $"{TemperatureSlider.Value:0} K";
    }

    private static Color Interpolate(Color start, Color end, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromRgb(
            Blend(start.R, end.R, amount),
            Blend(start.G, end.G, amount),
            Blend(start.B, end.B, amount));
    }

    private static byte Blend(byte start, byte end, double amount)
    {
        return (byte)Math.Round(start + (end - start) * amount);
    }

    private static byte Scale(byte channel, double brightness)
    {
        return (byte)Math.Round(channel * brightness);
    }

    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint flags);

    private sealed record MonitorOption(Screen Screen, string Label);
}
