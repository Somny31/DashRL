using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DashRL.Services;

namespace DashRL;

public partial class OverlayWindow : Window
{
    private readonly SessionTracker _sessionTracker;

    public OverlayWindow(
        SessionTracker sessionTracker)
    {
        InitializeComponent();

        _sessionTracker = sessionTracker;

        UpdateSession();

        MouseLeftButtonDown +=
            OverlayWindow_MouseLeftButtonDown;
    }

    // =========================================================
    // ACTUALISATION
    // =========================================================

    public void UpdateSession()
    {
        OverlayWins.Text =
            $"{_sessionTracker.Wins} W";

        OverlayLosses.Text =
            $"{_sessionTracker.Losses} L";

        if (_sessionTracker.Streak > 0)
        {
            OverlayStreak.Text =
                $"+{_sessionTracker.Streak}";

            OverlayStreak.Foreground =
                Brushes.LimeGreen;
        }
        else if (_sessionTracker.Streak < 0)
        {
            OverlayStreak.Text =
                _sessionTracker.Streak.ToString();

            OverlayStreak.Foreground =
                Brushes.IndianRed;
        }
        else
        {
            OverlayStreak.Text = "0";

            OverlayStreak.Foreground =
                Brushes.White;
        }
    }

    // =========================================================
    // DÉPLACEMENT
    // =========================================================

    private void OverlayWindow_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.ButtonState ==
            MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}