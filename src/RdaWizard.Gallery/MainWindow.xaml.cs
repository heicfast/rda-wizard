// Design Gallery — developer-only surface. Live theme switching, dynamic token swatch
// enumeration (gallery always reflects the actual dictionaries — no hand-maintained
// lists), icon grid, motion demos, dialog shells.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using RdaWizard.Controls;

namespace RdaWizard.Gallery;

public partial class MainWindow : Window
{
    private const string LightPack = "pack://application:,,,/RdaWizard.Design;component/Themes/Theme.Light.xaml";
    private const string DarkPack = "pack://application:,,,/RdaWizard.Design;component/Themes/Theme.Dark.xaml";
    private const string HcPack = "pack://application:,,,/RdaWizard.Design;component/Themes/Theme.HighContrast.xaml";

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => BuildDynamicSections();
    }

    // ---------- theme switching (same merge pattern as App ThemeService) ----------
    private void OnThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
        {
            return;
        }
        var pack = (ThemeSelector.SelectedIndex) switch
        {
            1 => DarkPack,
            2 => HcPack,
            _ => LightPack,
        };
        var merged = Application.Current.Resources.MergedDictionaries;
        merged[^1] = new ResourceDictionary { Source = new Uri(pack) };
        // Swatch hex labels are baked at build time; rebuild for the active theme.
        if (IsLoaded)
        {
            BuildDynamicSections();
        }
    }

    // ---------- dynamic sections (tokens + icons enumerated from live dictionaries) ----------
    private void BuildDynamicSections()
    {
        BuildColorSwatches();
        BuildIconGrid(ChromeIconPanel, "Icon.Nav.", "Icon.Window.", "Icon.Scan.", "Icon.Action.");
        BuildIconGrid(ChromeIconPanel, "Icon.Health.", "Icon.Status.", "Icon.Tree.", "Icon.Category.", "Icon.Device.", "Icon.File.");
    }

    private void BuildColorSwatches()
    {
        ColorSwatchPanel.Children.Clear();
        // The theme dictionary is the LAST merged entry; enumerate its Brush.* keys.
        var theme = Application.Current.Resources.MergedDictionaries[^1];
        foreach (var keyObj in theme.Keys)
        {
            if (keyObj is not string key || !key.StartsWith("Brush.", StringComparison.Ordinal))
            {
                continue;
            }
            var value = theme[keyObj];
            string hex;
            if (value is SolidColorBrush s)
            {
                var c = s.Color;
                hex = $"#{(c.A == 255 ? "" : c.A.ToString("X2", System.Globalization.CultureInfo.InvariantCulture))}{c.R:X2}{c.G:X2}{c.B:X2}";
            }
            else if (value is GradientBrush)
            {
                hex = "gradient";
            }
            else
            {
                continue;
            }
            var swatch = new Border
            {
                Width = 128,
                Height = 68,
                Margin = new Thickness(6),
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                BorderBrush = (Brush)FindResource("Brush.Border.Hairline"),
                Background = value as Brush,
            };
            var label = new TextBlock
            {
                Text = key["Brush.".Length..] + "\n" + hex,
                FontSize = 10,
                Foreground = Brushes.White,
                Margin = new Thickness(6, 4, 0, 0),
                TextWrapping = TextWrapping.NoWrap,
            };
            // Readable label on light swatches.
            if (value is SolidColorBrush sc && sc.Color.R + sc.Color.G + sc.Color.B > 480)
            {
                label.Foreground = (Brush)FindResource("Brush.Text.Primary");
            }
            var host = new Grid();
            host.Children.Add(swatch);
            swatch.Child = label;
            ColorSwatchPanel.Children.Add(host);
        }
    }

    private void BuildIconGrid(Panel panel, params string[] prefixes)
    {
        foreach (var prefix in prefixes)
        {
            foreach (var keyObj in Application.Current.Resources.Keys)
            {
                if (keyObj is not string key || !key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }
                if (Application.Current.Resources[key] is not Geometry)
                {
                    continue;
                }
                var chrome = key.StartsWith("Icon.Nav.", StringComparison.Ordinal)
                    || key.StartsWith("Icon.Window.", StringComparison.Ordinal)
                    || key.StartsWith("Icon.Scan.", StringComparison.Ordinal)
                    || key.StartsWith("Icon.Action.", StringComparison.Ordinal)
                    || key.StartsWith("Icon.Health.", StringComparison.Ordinal);
                var cell = new StackPanel
                {
                    Width = 84,
                    Margin = new Thickness(4),
                    ToolTip = key,
                };
                cell.Children.Add(new Icon
                {
                    IconName = key,
                    IconKind = chrome ? IconKind.Chrome : IconKind.Solid,
                    IconSize = 24,
                    HorizontalAlignment = HorizontalAlignment.Center,
                });
                cell.Children.Add(new TextBlock
                {
                    Text = key["Icon.".Length..],
                    FontSize = 9,
                    Foreground = (Brush)FindResource("Brush.Text.Tertiary"),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    ToolTip = key,
                });
                panel.Children.Add(cell);
            }
        }
    }

    // ---------- motion demos ----------
    private static bool ReducedMotion => !SystemParameters.ClientAreaAnimation;

    private void OnPlayPageTransition(object sender, RoutedEventArgs e)
    {
        if (ReducedMotion)
        {
            return;
        }
        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        var slide = new ThicknessAnimation(new Thickness(0, 12, 0, 0), new Thickness(0),
            TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        MotionStage.BeginAnimation(OpacityProperty, fade);
        MotionStage.BeginAnimation(MarginProperty, slide);
    }

    private void OnPlayPanel(object sender, RoutedEventArgs e)
    {
        if (ReducedMotion)
        {
            return;
        }
        var open = new DoubleAnimation(0.2, 1, TimeSpan.FromMilliseconds(250))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        var scaleUp = new DoubleAnimation(0.96, 1, TimeSpan.FromMilliseconds(250))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        MotionStage.RenderTransform = new ScaleTransform(1, 1);
        MotionStage.BeginAnimation(OpacityProperty, open);
        MotionStage.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUp);
        MotionStage.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUp);
    }

    private void OnPlayPress(object sender, RoutedEventArgs e)
    {
        if (ReducedMotion || sender is not Button b)
        {
            return;
        }
        b.RenderTransform = new ScaleTransform(1, 1);
        var down = new DoubleAnimation(1, 0.98, TimeSpan.FromMilliseconds(80));
        var up = new DoubleAnimation(0.98, 1, TimeSpan.FromMilliseconds(80)) { BeginTime = TimeSpan.FromMilliseconds(80) };
        b.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, down);
        b.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, up);
        b.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, down);
        b.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, up);
    }

    private void OnShowToast(object sender, RoutedEventArgs e)
    {
        var toast = new Toast
        {
            Severity = ToastSeverity.Info,
            Title = "AI Recommend available.",
            Message = "Switch to the recommended view to find more wanted files.",
            ActionLabel = "View Details",
            Duration = TimeSpan.FromSeconds(5),
            Margin = new Thickness(0, 0, 0, 8),
        };
        toast.Expired += (_, _) => ToastHost.Children.Remove(toast);
        ToastHost.Children.Add(toast);
        toast.StartLifecycle();
    }

    // ---------- dialog shells ----------
    private void OnShowStopDialog(object sender, RoutedEventArgs e)
    {
        ShowModal("Stop Searching for Lost Data?",
            "Remaining data can be rescanned and recovered anytime later, but continuing now saves you time.",
            "Stop", isDanger: true);
    }

    private void OnShowGateDialog(object sender, RoutedEventArgs e)
    {
        ShowModal("Save to the original drive?",
            "Data might be OVERWRITTEN and LOST PERMANENTLY if you save files to the original drive (where you lost data).",
            "Choose another drive", isDanger: true);
    }

    private void ShowModal(string title, string body, string primary, bool isDanger)
    {
        var scrim = new Border
        {
            Style = (Style)FindResource("Overlay.Scrim"),
        };
        var dialog = new Border
        {
            Style = (Style)FindResource("Dialog.Surface"),
            Width = 540,
        };
        var content = new StackPanel();
        content.Children.Add(new TextBlock
        {
            Text = title,
            Style = (Style)FindResource("Text.Header"),
            Margin = new Thickness(0, 0, 0, 8),
        });
        content.Children.Add(new TextBlock
        {
            Text = body,
            Style = (Style)FindResource("Text.Body"),
            Foreground = (Brush)FindResource("Brush.Text.Secondary"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20),
        });
        var footer = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        var cancel = new Button
        {
            Style = (Style)FindResource("Button.Secondary"),
            Content = "Continue",
            MinWidth = 90,
            Margin = new Thickness(0, 0, 8, 0),
        };
        var confirm = new Button
        {
            Style = (Style)FindResource(isDanger ? "Button.Danger" : "Button.Primary"),
            Content = primary,
            MinWidth = 110,
        };
        void Close()
        {
            var host = (Grid)Content;
            host.Children.Remove(scrim);
            host.Children.Remove(dialog);
        }
        cancel.Click += (_, _) => Close();
        confirm.Click += (_, _) => Close();
        scrim.MouseLeftButtonDown += (_, _) => { /* modal swallows outside clicks (atlas §12.10) */ };
        footer.Children.Add(cancel);
        footer.Children.Add(confirm);
        content.Children.Add(footer);
        dialog.Child = content;

        var hostGrid = (Grid)Content;
        scrim.SetValue(Panel.ZIndexProperty, 10);
        dialog.SetValue(Panel.ZIndexProperty, 11);
        dialog.HorizontalAlignment = HorizontalAlignment.Center;
        dialog.VerticalAlignment = VerticalAlignment.Center;
        hostGrid.Children.Add(scrim);
        hostGrid.Children.Add(dialog);
    }
}
