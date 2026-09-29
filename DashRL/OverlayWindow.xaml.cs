using System;
using System.Runtime.InteropServices;
using System.Windows;
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


    public OverlayWindow(
        SessionTracker sessionTracker,
        string overlayStyle,
        OverlaySettings settings)
    {
        InitializeComponent();

        _sessionTracker = sessionTracker;
        _overlayStyle = overlayStyle;
        _settings = settings;

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
        UpdateRocketLeagueMonitor(
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
        UpdateRocketLeagueMonitor();

        UpdateOverlayVisibility();
    }


    private void UpdateRocketLeagueMonitor(
        bool forceUpdate = false)
    {
        Rect? monitor =
            RocketLeagueWindowService
                .GetRocketLeagueMonitorBounds();

        if (monitor == null)
            return;

        if (!forceUpdate &&
            _lastRocketLeagueMonitor != null &&
            _lastRocketLeagueMonitor.Value ==
            monitor.Value)
        {
            return;
        }

        _lastRocketLeagueMonitor =
            monitor;

        ApplyMonitorBounds(
            monitor.Value
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
        bool shouldShow =
            RocketLeagueWindowService
                .ShouldShowOverlay();

        if (shouldShow)
        {
            ShowOverlay();
        }
        else
        {
            HideOverlay();
        }
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