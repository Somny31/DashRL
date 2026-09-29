using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DashRL.Models;
using DashRL.RocketLeague;
using DashRL.Services;

namespace DashRL;

public partial class MainWindow : Window
{
    private readonly RocketLeagueClient _rocketLeagueClient = new();

    private readonly SessionTracker _sessionTracker = new();

    private OverlayWindow? _overlayWindow;

    private string? _lastCountedMatchGuid;

    public MainWindow()
    {
        InitializeComponent();

        _rocketLeagueClient.MessageReceived +=
            OnRocketLeagueMessage;

        _sessionTracker.SessionChanged +=
            OnSessionChanged;

        Loaded += MainWindow_Loaded;

        UpdateSessionDisplay();

        ShowDashboardPage();
    }

    private async void MainWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        bool connected =
            await _rocketLeagueClient.ConnectAsync();

        if (connected)
        {
            ConnectionStatus.Text =
                "● Connecté à Rocket League";

            ConnectionStatus.Foreground =
                Brushes.LimeGreen;
        }
        else
        {
            ConnectionStatus.Text =
                "● Rocket League non connecté";

            ConnectionStatus.Foreground =
                Brushes.Red;
        }
    }

    // ================================================================
    // NAVIGATION
    // ================================================================

    private void DashboardButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ShowDashboardPage();
    }

    private void OverlayButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ShowOverlayPage();
    }

    private void ShowDashboardPage()
    {
        DashboardPage.Visibility =
            Visibility.Visible;

        OverlayPage.Visibility =
            Visibility.Collapsed;

        SetNavigationButtonState(
            DashboardButton,
            true
        );

        SetNavigationButtonState(
            OverlayButton,
            false
        );
    }

    private void ShowOverlayPage()
    {
        DashboardPage.Visibility =
            Visibility.Collapsed;

        OverlayPage.Visibility =
            Visibility.Visible;

        SetNavigationButtonState(
            DashboardButton,
            false
        );

        SetNavigationButtonState(
            OverlayButton,
            true
        );

        UpdateOverlayPreview();
        UpdateOverlayControls();
    }

    private static void SetNavigationButtonState(
        System.Windows.Controls.Button button,
        bool active)
    {
        if (active)
        {
            button.Background =
                new SolidColorBrush(
                    Color.FromRgb(
                        32,
                        38,
                        50
                    )
                );

            button.Foreground =
                Brushes.White;
        }
        else
        {
            button.Background =
                Brushes.Transparent;

            button.Foreground =
                new SolidColorBrush(
                    Color.FromRgb(
                        146,
                        153,
                        168
                    )
                );
        }
    }

    // ================================================================
    // OVERLAY
    // ================================================================

    private void ToggleOverlayButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_overlayWindow != null)
        {
            _overlayWindow.Close();
            return;
        }

        OpenOverlay();
    }

    private void OpenOverlay()
    {
        if (_overlayWindow != null)
            return;

        _overlayWindow =
            new OverlayWindow(
                _sessionTracker
            );

        _overlayWindow.Closed +=
            OverlayWindow_Closed;

        _overlayWindow.Show();

        UpdateOverlayControls();
    }

    private void OverlayWindow_Closed(
        object? sender,
        EventArgs e)
    {
        _overlayWindow = null;

        UpdateOverlayControls();
    }

    private void UpdateOverlayControls()
    {
        if (_overlayWindow != null)
        {
            ToggleOverlayButton.Content =
                "Masquer l'overlay";

            OverlayStatusText.Text =
                "L'overlay est actuellement affiché";

            OverlayStatusText.Foreground =
                Brushes.LimeGreen;
        }
        else
        {
            ToggleOverlayButton.Content =
                "Afficher l'overlay";

            OverlayStatusText.Text =
                "L'overlay est actuellement désactivé";

            OverlayStatusText.Foreground =
                new SolidColorBrush(
                    Color.FromRgb(
                        115,
                        122,
                        137
                    )
                );
        }
    }

    private void UpdateOverlayPreview()
    {
        PreviewWins.Text =
            $"{_sessionTracker.Wins} W";

        PreviewLosses.Text =
            $"{_sessionTracker.Losses} L";

        if (_sessionTracker.Streak > 0)
        {
            PreviewStreak.Text =
                $"+{_sessionTracker.Streak}";

            PreviewStreak.Foreground =
                Brushes.LimeGreen;
        }
        else if (_sessionTracker.Streak < 0)
        {
            PreviewStreak.Text =
                _sessionTracker.Streak.ToString();

            PreviewStreak.Foreground =
                Brushes.IndianRed;
        }
        else
        {
            PreviewStreak.Text = "0";

            PreviewStreak.Foreground =
                Brushes.White;
        }
    }

    // ================================================================
    // SESSION
    // ================================================================

    private void OnSessionChanged()
    {
        Dispatcher.Invoke(() =>
        {
            UpdateSessionDisplay();

            UpdateOverlayPreview();

            _overlayWindow?.UpdateSession();
        });
    }

    // ================================================================
    // ROCKET LEAGUE
    // ================================================================

    private void OnRocketLeagueMessage(
        string message)
    {
        MatchState? match =
            RocketLeagueParser.ParseMatchState(
                message
            );

        if (match == null)
            return;

        Dispatcher.Invoke(() =>
        {
            UpdateMatchDisplay(match);

            UpdateSessionTracker(match);
        });
    }

    private void UpdateMatchDisplay(
        MatchState match)
    {
        Team? blueTeam =
            match.Game.Teams.FirstOrDefault(
                team => team.TeamNum == 0
            );

        Team? orangeTeam =
            match.Game.Teams.FirstOrDefault(
                team => team.TeamNum == 1
            );

        if (blueTeam != null)
        {
            BlueTeamName.Text =
                blueTeam.Name;
        }

        if (orangeTeam != null)
        {
            OrangeTeamName.Text =
                orangeTeam.Name;
        }

        if (blueTeam != null &&
            orangeTeam != null)
        {
            ScoreText.Text =
                $"{blueTeam.Score} - {orangeTeam.Score}";
        }

        int totalSeconds =
            match.Game.TimeSeconds;

        int minutes =
            totalSeconds / 60;

        int seconds =
            totalSeconds % 60;

        TimeText.Text =
            $"{minutes:00}:{seconds:00}";

        Player? player = null;

        if (match.Game.bHasTarget &&
            match.Game.Target != null)
        {
            player =
                match.Players.FirstOrDefault(
                    p =>
                        p.Shortcut ==
                        match.Game.Target.Shortcut
                );
        }

        if (player == null)
        {
            PlayerName.Text =
                "Aucun joueur ciblé";

            PlayerScore.Text = "—";
            PlayerGoals.Text = "—";
            PlayerShots.Text = "—";
            PlayerAssists.Text = "—";
            PlayerSaves.Text = "—";
            PlayerBoost.Text = "—";

            return;
        }

        PlayerName.Text =
            player.Name;

        PlayerScore.Text =
            player.Score.ToString();

        PlayerGoals.Text =
            player.Goals.ToString();

        PlayerShots.Text =
            player.Shots.ToString();

        PlayerAssists.Text =
            player.Assists.ToString();

        PlayerSaves.Text =
            player.Saves.ToString();

        PlayerBoost.Text =
            $"{player.Boost}%";
    }

    private void UpdateSessionTracker(
        MatchState match)
    {
        if (!match.Game.bHasWinner)
            return;

        if (string.IsNullOrWhiteSpace(
            match.MatchGuid))
        {
            return;
        }

        if (_lastCountedMatchGuid ==
            match.MatchGuid)
        {
            return;
        }

        if (!match.Game.bHasTarget ||
            match.Game.Target == null)
        {
            return;
        }

        Player? player =
            match.Players.FirstOrDefault(
                p =>
                    p.Shortcut ==
                    match.Game.Target.Shortcut
            );

        if (player == null)
            return;

        Team? playerTeam =
            match.Game.Teams.FirstOrDefault(
                team =>
                    team.TeamNum ==
                    player.TeamNum
            );

        if (playerTeam == null)
            return;

        if (string.IsNullOrWhiteSpace(
            match.Game.Winner))
        {
            return;
        }

        _lastCountedMatchGuid =
            match.MatchGuid;

        bool isWin =
            string.Equals(
                playerTeam.Name,
                match.Game.Winner,
                StringComparison.OrdinalIgnoreCase
            );

        if (isWin)
        {
            _sessionTracker.AddWin();
        }
        else
        {
            _sessionTracker.AddLoss();
        }
    }

    private void UpdateSessionDisplay()
    {
        SessionWins.Text =
            _sessionTracker.Wins.ToString();

        SessionLosses.Text =
            _sessionTracker.Losses.ToString();

        if (_sessionTracker.Streak > 0)
        {
            SessionStreak.Text =
                $"+{_sessionTracker.Streak}";

            SessionStreak.Foreground =
                Brushes.LimeGreen;

            return;
        }

        if (_sessionTracker.Streak < 0)
        {
            SessionStreak.Text =
                _sessionTracker.Streak.ToString();

            SessionStreak.Foreground =
                Brushes.IndianRed;

            return;
        }

        SessionStreak.Text = "0";

        SessionStreak.Foreground =
            Brushes.White;
    }
}