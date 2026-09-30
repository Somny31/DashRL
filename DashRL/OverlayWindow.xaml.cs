using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using DashRL.Models;
using DashRL.Services;

namespace DashRL;

public partial class OverlayWindow : Window
{
    private readonly SessionTracker _sessionTracker;
    private readonly OverlaySettings _settings;
    private readonly DispatcherTimer _rocketLeagueWindowTimer;

    private string _overlayStyle;
    private CustomOverlay? _customOverlay;
    private bool _overlayVisible;
    private Rect? _lastRocketLeagueMonitor;

    private const int GWL_EXSTYLE = -20;

    private const int WS_EX_TRANSPARENT = 0x00000020;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(
        IntPtr hWnd,
        int nIndex
    );

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(
        IntPtr hWnd,
        int nIndex,
        int dwNewLong
    );

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(
        IntPtr hwnd,
        uint dwFlags
    );

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(
        IntPtr hMonitor,
        ref MONITORINFO lpmi
    );

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }


    public OverlayWindow(
        SessionTracker sessionTracker,
        string overlayStyle,
        OverlaySettings settings,
        CustomOverlay? customOverlay = null)
    {
        InitializeComponent();

        _sessionTracker = sessionTracker;
        _overlayStyle = overlayStyle;
        _settings = settings;
        _customOverlay = customOverlay;

        UpdateSession();
        SetOverlayStyle(_overlayStyle);

        MouseLeftButtonDown +=
            OverlayWindow_MouseLeftButtonDown;

        _settings.SettingsChanged +=
            Settings_SettingsChanged;

        _rocketLeagueWindowTimer =
            new DispatcherTimer
            {
                Interval =
                    TimeSpan.FromMilliseconds(250)
            };

        _rocketLeagueWindowTimer.Tick +=
            RocketLeagueWindowTimer_Tick;

        _rocketLeagueWindowTimer.Start();

        Closed +=
            OverlayWindow_Closed;

        Loaded +=
            OverlayWindow_Loaded;
    }


    // ================================================================
    // LOADING
    // ================================================================

    private void OverlayWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        UpdateScreenBounds(
            forceUpdate: true
        );

        ApplySettings();

        UpdateOverlayVisibility();
    }


    // ================================================================
    // SETTINGS
    // ================================================================

    private void Settings_SettingsChanged()
    {
        Dispatcher.Invoke(
            ApplySettings
        );
    }


    private void ApplySettings()
    {
        OverlayScaleTransform.ScaleX =
            _settings.Scale;

        OverlayScaleTransform.ScaleY =
            _settings.Scale;

        ApplyPosition();

        ApplyVisibilitySettings();

        ApplyClickThrough();
    }


    // ================================================================
    // POSITION
    // ================================================================

    private void ApplyPosition()
    {
        if (OverlayPositionContainer == null)
            return;

        const double margin = 15;

        OverlayPositionContainer.Margin =
            new Thickness(margin);

        // Décalage fin relatif à la position d'ancrage choisie.
        // X positif = vers la droite, X négatif = vers la gauche.
        // Y positif = vers le bas, Y négatif = vers le haut.
        OverlayPositionTransform.X =
            _settings.OffsetX;

        OverlayPositionTransform.Y =
            _settings.OffsetY;

        switch (_settings.Position)
        {
            case OverlayPosition.TopLeft:

                OverlayPositionContainer.HorizontalAlignment =
                    HorizontalAlignment.Left;

                OverlayPositionContainer.VerticalAlignment =
                    VerticalAlignment.Top;

                OverlayPositionContainer.RenderTransformOrigin =
                    new Point(0, 0);

                break;


            case OverlayPosition.TopCenter:

                OverlayPositionContainer.HorizontalAlignment =
                    HorizontalAlignment.Center;

                OverlayPositionContainer.VerticalAlignment =
                    VerticalAlignment.Top;

                OverlayPositionContainer.RenderTransformOrigin =
                    new Point(0.5, 0);

                break;


            case OverlayPosition.TopRight:

                OverlayPositionContainer.HorizontalAlignment =
                    HorizontalAlignment.Right;

                OverlayPositionContainer.VerticalAlignment =
                    VerticalAlignment.Top;

                OverlayPositionContainer.RenderTransformOrigin =
                    new Point(1, 0);

                break;


            case OverlayPosition.Center:

                OverlayPositionContainer.HorizontalAlignment =
                    HorizontalAlignment.Center;

                OverlayPositionContainer.VerticalAlignment =
                    VerticalAlignment.Center;

                OverlayPositionContainer.RenderTransformOrigin =
                    new Point(0.5, 0.5);

                break;


            case OverlayPosition.BottomLeft:

                OverlayPositionContainer.HorizontalAlignment =
                    HorizontalAlignment.Left;

                OverlayPositionContainer.VerticalAlignment =
                    VerticalAlignment.Bottom;

                OverlayPositionContainer.RenderTransformOrigin =
                    new Point(0, 1);

                break;


            case OverlayPosition.BottomCenter:

                OverlayPositionContainer.HorizontalAlignment =
                    HorizontalAlignment.Center;

                OverlayPositionContainer.VerticalAlignment =
                    VerticalAlignment.Bottom;

                OverlayPositionContainer.RenderTransformOrigin =
                    new Point(0.5, 1);

                break;


            case OverlayPosition.BottomRight:

                OverlayPositionContainer.HorizontalAlignment =
                    HorizontalAlignment.Right;

                OverlayPositionContainer.VerticalAlignment =
                    VerticalAlignment.Bottom;

                OverlayPositionContainer.RenderTransformOrigin =
                    new Point(1, 1);

                break;
        }
    }


    // ================================================================
    // VISIBILITY SETTINGS
    // ================================================================

    private void ApplyVisibilitySettings()
    {
        MinimalWinsContainer.Visibility =
            _settings.ShowWins
                ? Visibility.Visible
                : Visibility.Collapsed;

        MinimalLossesContainer.Visibility =
            _settings.ShowLosses
                ? Visibility.Visible
                : Visibility.Collapsed;

        MinimalStreakContainer.Visibility =
            _settings.ShowStreak
                ? Visibility.Visible
                : Visibility.Collapsed;


        CompetitiveWins.Visibility =
            _settings.ShowWins
                ? Visibility.Visible
                : Visibility.Collapsed;

        CompetitiveLosses.Visibility =
            _settings.ShowLosses
                ? Visibility.Visible
                : Visibility.Collapsed;

        CompetitiveStreakContainer.Visibility =
            _settings.ShowStreak
                ? Visibility.Visible
                : Visibility.Collapsed;


        CardsWinsContainer.Visibility =
            _settings.ShowWins
                ? Visibility.Visible
                : Visibility.Collapsed;

        CardsLossesContainer.Visibility =
            _settings.ShowLosses
                ? Visibility.Visible
                : Visibility.Collapsed;

        CardsStreakContainer.Visibility =
            _settings.ShowStreak
                ? Visibility.Visible
                : Visibility.Collapsed;


        PillWinsContainer.Visibility =
            _settings.ShowWins
                ? Visibility.Visible
                : Visibility.Collapsed;

        PillLossesContainer.Visibility =
            _settings.ShowLosses
                ? Visibility.Visible
                : Visibility.Collapsed;

        PillStreakContainer.Visibility =
            _settings.ShowStreak
                ? Visibility.Visible
                : Visibility.Collapsed;

        if (_customOverlay != null)
            RenderCustomOverlay();
    }


    // ================================================================
    // CLICK THROUGH
    // ================================================================

    private void ApplyClickThrough()
    {
        if (!IsLoaded)
            return;

        IntPtr handle =
            new WindowInteropHelper(this).Handle;

        if (handle == IntPtr.Zero)
            return;

        int extendedStyle =
            GetWindowLong(
                handle,
                GWL_EXSTYLE
            );

        if (_settings.ClickThrough)
        {
            extendedStyle |=
                WS_EX_TRANSPARENT;
        }
        else
        {
            extendedStyle &=
                ~WS_EX_TRANSPARENT;
        }

        SetWindowLong(
            handle,
            GWL_EXSTYLE,
            extendedStyle
        );
    }


    // ================================================================
    // ROCKET LEAGUE MONITOR
    // ================================================================

    private void RocketLeagueWindowTimer_Tick(
        object? sender,
        EventArgs e)
    {
        UpdateScreenBounds();
        UpdateOverlayVisibility();
    }


    private void UpdateScreenBounds(
        bool forceUpdate = false)
    {
        if (!IsLoaded)
            return;

        IntPtr handle =
            new WindowInteropHelper(this).Handle;

        if (handle == IntPtr.Zero)
            return;

        IntPtr monitorHandle =
            MonitorFromWindow(
                handle,
                MONITOR_DEFAULTTONEAREST
            );

        if (monitorHandle == IntPtr.Zero)
            return;

        MONITORINFO monitorInfo =
            new()
            {
                cbSize =
                    Marshal.SizeOf<MONITORINFO>()
            };

        if (!GetMonitorInfo(
            monitorHandle,
            ref monitorInfo
        ))
        {
            return;
        }

        // Win32 donne des pixels physiques. WPF travaille en DIP.
        // On convertit donc les coordonnées de l'écran vers les unités WPF
        // pour que les coins restent exacts, même avec un scaling Windows.
        var source =
            PresentationSource.FromVisual(this);

        Matrix fromDevice =
            source?.CompositionTarget?.TransformFromDevice
            ?? Matrix.Identity;

        Point topLeft =
            fromDevice.Transform(
                new Point(
                    monitorInfo.rcMonitor.Left,
                    monitorInfo.rcMonitor.Top
                )
            );

        Point bottomRight =
            fromDevice.Transform(
                new Point(
                    monitorInfo.rcMonitor.Right,
                    monitorInfo.rcMonitor.Bottom
                )
            );

        Rect monitor =
            new(
                topLeft.X,
                topLeft.Y,
                bottomRight.X - topLeft.X,
                bottomRight.Y - topLeft.Y
            );

        if (!forceUpdate &&
            _lastRocketLeagueMonitor != null &&
            _lastRocketLeagueMonitor.Value == monitor)
        {
            return;
        }

        _lastRocketLeagueMonitor =
            monitor;

        ApplyMonitorBounds(
            monitor
        );
    }


    private void ApplyMonitorBounds(
        Rect monitor)
    {
        Left =
            monitor.Left;

        Top =
            monitor.Top;

        Width =
            monitor.Width;

        Height =
            monitor.Height;

        ApplyPosition();
    }


    // ================================================================
    // OVERLAY VISIBILITY
    // ================================================================

    private void UpdateOverlayVisibility()
    {
        bool dashRlFocused =
            Application.Current.MainWindow?.IsActive == true;

        bool rocketLeagueFocused =
            RocketLeagueWindowService.ShouldShowOverlay();

        if (dashRlFocused || rocketLeagueFocused)
            ShowOverlay();
        else
            HideOverlay();
    }


    private void ShowOverlay()
    {
        if (_overlayVisible &&
            Math.Abs(
                Opacity -
                _settings.Opacity
            ) < 0.001)
        {
            return;
        }

        _overlayVisible =
            true;

        Opacity =
            _settings.Opacity;

        IsHitTestVisible =
            true;
    }


    private void HideOverlay()
    {
        if (!_overlayVisible &&
            Opacity == 0)
        {
            return;
        }

        _overlayVisible =
            false;

        Opacity = 0;

        IsHitTestVisible =
            false;
    }


    // ================================================================
    // STYLE
    // ================================================================

    public void SetOverlayStyle(
        string style)
    {
        _overlayStyle =
            style;

        MinimalOverlay.Visibility =
            Visibility.Collapsed;

        CompetitiveOverlay.Visibility =
            Visibility.Collapsed;

        CardsOverlay.Visibility =
            Visibility.Collapsed;

        PillOverlay.Visibility =
            Visibility.Collapsed;

        CustomOverlayCanvas.Visibility =
            Visibility.Collapsed;


        switch (_overlayStyle)
        {
            case "Competitive":

                CompetitiveOverlay.Visibility =
                    Visibility.Visible;

                break;


            case "Cards":

                CardsOverlay.Visibility =
                    Visibility.Visible;

                break;


            case "Pill":

                PillOverlay.Visibility =
                    Visibility.Visible;

                break;


            case "Custom":

                if (_customOverlay != null)
                {
                    CustomOverlayCanvas.Visibility =
                        Visibility.Visible;

                    RenderCustomOverlay();
                }

                break;


            default:

                MinimalOverlay.Visibility =
                    Visibility.Visible;

                break;
        }

        ApplyVisibilitySettings();

        UpdateSession();
    }


    // ================================================================
    // SESSION
    // ================================================================

    public void UpdateSession()
    {
        string wins =
            $"{_sessionTracker.Wins} W";

        string losses =
            $"{_sessionTracker.Losses} L";

        string streak;

        Brush streakColor;


        if (_sessionTracker.Streak > 0)
        {
            streak =
                $"+{_sessionTracker.Streak}";

            streakColor =
                Brushes.LimeGreen;
        }
        else if (_sessionTracker.Streak < 0)
        {
            streak =
                _sessionTracker
                    .Streak
                    .ToString();

            streakColor =
                Brushes.IndianRed;
        }
        else
        {
            streak =
                "0";

            streakColor =
                Brushes.White;
        }


        MinimalWins.Text =
            wins;

        MinimalLosses.Text =
            losses;

        MinimalStreak.Text =
            streak;

        MinimalStreak.Foreground =
            streakColor;


        CompetitiveWins.Text =
            wins;

        CompetitiveLosses.Text =
            losses;

        CompetitiveStreak.Text =
            streak;

        CompetitiveStreak.Foreground =
            streakColor;


        CardsWins.Text =
            _sessionTracker
                .Wins
                .ToString();

        CardsLosses.Text =
            _sessionTracker
                .Losses
                .ToString();

        CardsStreak.Text =
            streak;

        CardsStreak.Foreground =
            streakColor;


        PillWins.Text =
            wins;

        PillLosses.Text =
            losses;

        PillStreak.Text =
            streak;

        PillStreak.Foreground =
            streakColor;

        if (_customOverlay != null)
            RenderCustomOverlay();
    }


    // ================================================================
    // CUSTOM OVERLAY
    // ================================================================

    public void SetCustomOverlay(
        CustomOverlay overlay)
    {
        _customOverlay = overlay;
        _overlayStyle = "Custom";

        SetOverlayStyle(
            "Custom"
        );
    }


    private void RenderCustomOverlay()
    {
        if (
            CustomOverlayCanvas == null ||
            _customOverlay == null
        )
        {
            return;
        }

        CustomOverlayCanvas.Children.Clear();

        // En jeu, on positionne la zone réellement occupée par les éléments
        // et non les 500x150 px complets du canvas de l'éditeur.
        // Ainsi TopRight / BottomRight / BottomLeft, etc. collent bien
        // l'overlay visible au coin choisi.
        double minX = 0;
        double minY = 0;
        double maxX = 1;
        double maxY = 1;

        if (_customOverlay.Elements.Count > 0)
        {
            minX =
                _customOverlay.Elements.Min(
                    element => element.X
                );

            minY =
                _customOverlay.Elements.Min(
                    element => element.Y
                );

            maxX =
                _customOverlay.Elements.Max(
                    element =>
                        element.X +
                        Math.Max(
                            1,
                            element.Width
                        )
                );

            maxY =
                _customOverlay.Elements.Max(
                    element =>
                        element.Y +
                        Math.Max(
                            1,
                            element.Height
                        )
                );
        }

        CustomOverlayCanvas.Width =
            Math.Max(
                1,
                maxX - minX
            );

        CustomOverlayCanvas.Height =
            Math.Max(
                1,
                maxY - minY
            );

        foreach (
            CustomOverlayElement element
            in _customOverlay.Elements
        )
        {
            if (
                element.Type == CustomOverlayElementType.Wins &&
                !_settings.ShowWins
            )
            {
                continue;
            }

            if (
                element.Type == CustomOverlayElementType.Losses &&
                !_settings.ShowLosses
            )
            {
                continue;
            }

            if (
                element.Type == CustomOverlayElementType.Streak &&
                !_settings.ShowStreak
            )
            {
                continue;
            }

            FrameworkElement visual =
                CreateCustomOverlayVisual(
                    element
                );

            Canvas.SetLeft(
                visual,
                element.X - minX
            );

            Canvas.SetTop(
                visual,
                element.Y - minY
            );

            CustomOverlayCanvas
                .Children
                .Add(
                    visual
                );
        }
    }


    private FrameworkElement CreateCustomOverlayVisual(
        CustomOverlayElement element)
    {
        Border border =
            new()
            {
                Width =
                    Math.Max(
                        1,
                        element.Width
                    ),
                Height =
                    Math.Max(
                        1,
                        element.Height
                    ),
                Opacity =
                    Math.Clamp(
                        element.Opacity,
                        0,
                        1
                    ),
                Background =
                    GetCustomBrush(
                        element.BackgroundColor
                    ),
                BorderBrush =
                    GetCustomBrush(
                        element.BorderColor
                    ),
                BorderThickness =
                    new Thickness(
                        Math.Max(
                            0,
                            element.BorderThickness
                        )
                    ),
                CornerRadius =
                    new CornerRadius(
                        Math.Max(
                            0,
                            element.CornerRadius
                        )
                    )
            };

        if (
            element.Type ==
            CustomOverlayElementType.Container
        )
        {
            return border;
        }

        TextBlock text =
            new()
            {
                Text =
                    GetCustomOverlayText(
                        element
                    ),
                Foreground =
                    GetCustomBrush(
                        element.TextColor,
                        Brushes.White
                    ),
                FontSize =
                    Math.Max(
                        1,
                        element.FontSize
                    ),
                FontWeight =
                    element.Bold
                        ? FontWeights.Bold
                        : FontWeights.Normal,
                HorizontalAlignment =
                    HorizontalAlignment.Center,
                VerticalAlignment =
                    VerticalAlignment.Center,
                TextAlignment =
                    TextAlignment.Center,
                TextWrapping =
                    TextWrapping.Wrap
            };

        border.Child =
            text;

        return border;
    }


    private string GetCustomOverlayText(
        CustomOverlayElement element)
    {
        return element.Type switch
        {
            CustomOverlayElementType.Wins =>
                $"{_sessionTracker.Wins} W",

            CustomOverlayElementType.Losses =>
                $"{_sessionTracker.Losses} L",

            CustomOverlayElementType.Streak =>
                _sessionTracker.Streak > 0
                    ? $"+{_sessionTracker.Streak}"
                    : _sessionTracker.Streak.ToString(),

            CustomOverlayElementType.Text =>
                element.Text ?? string.Empty,

            _ =>
                string.Empty
        };
    }


    private static Brush GetCustomBrush(
        string? color,
        Brush? fallback = null)
    {
        fallback ??=
            Brushes.Transparent;

        if (
            string.IsNullOrWhiteSpace(
                color
            )
        )
        {
            return fallback;
        }

        try
        {
            object? converted =
                ColorConverter.ConvertFromString(
                    color
                );

            if (converted is Color parsedColor)
            {
                return new SolidColorBrush(
                    parsedColor
                );
            }
        }
        catch
        {
        }

        return fallback;
    }


    // ================================================================
    // MANUAL DRAG
    // ================================================================

    private void OverlayWindow_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        // La fenêtre fait maintenant toute la taille de l'écran.
        // On ne fait donc plus DragMove(), sinon toute la surface
        // de l'overlay serait déplacée hors du moniteur.
    }


    // ================================================================
    // CLOSE
    // ================================================================

    private void OverlayWindow_Closed(
        object? sender,
        EventArgs e)
    {
        _rocketLeagueWindowTimer.Stop();

        _rocketLeagueWindowTimer.Tick -=
            RocketLeagueWindowTimer_Tick;

        _settings.SettingsChanged -=
            Settings_SettingsChanged;
    }
}