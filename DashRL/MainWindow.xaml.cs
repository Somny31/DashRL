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
    }

    // =========================================================
    // CONNEXION ROCKET LEAGUE
    // =========================================================

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

    // =========================================================
    // BOUTON OVERLAY
    // =========================================================

    private void OverlayButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_overlayWindow != null)
        {
            if (_overlayWindow.IsVisible)
            {
                _overlayWindow.Activate();
                return;
            }
        }

        _overlayWindow =
            new OverlayWindow(_sessionTracker);

        _overlayWindow.Closed +=
            OverlayWindow_Closed;

        _overlayWindow.Show();
    }

    private void OverlayWindow_Closed(
        object? sender,
        EventArgs e)
    {
        _overlayWindow = null;
    }

    // =========================================================
    // CHANGEMENT SESSION
    // =========================================================

    private void OnSessionChanged()
    {
        Dispatcher.Invoke(() =>
        {
            UpdateSessionDisplay();

            _overlayWindow?.UpdateSession();
        });
    }

    // =========================================================
    // RÉCEPTION ROCKET LEAGUE
    // =========================================================

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

    // =========================================================
    // DASHBOARD MATCH
    // =========================================================

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

        // -----------------------------------------------------
        // CHRONOMÈTRE
        // -----------------------------------------------------

        int totalSeconds =
            match.Game.TimeSeconds;

        int minutes =
            totalSeconds / 60;

        int seconds =
            totalSeconds % 60;

        TimeText.Text =
            $"{minutes:00}:{seconds:00}";

        // -----------------------------------------------------
        // JOUEUR
        // -----------------------------------------------------

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

    // =========================================================
    // DÉTECTION WIN / LOSS
    // =========================================================

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

    // =========================================================
    // AFFICHAGE SESSION DASHBOARD
    // =========================================================

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