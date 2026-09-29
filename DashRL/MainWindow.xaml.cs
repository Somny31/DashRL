using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DashRL.Models;
using DashRL.RocketLeague;

namespace DashRL;

public partial class MainWindow : Window
{
    private readonly RocketLeagueClient _rocketLeagueClient = new();

    public MainWindow()
    {
        InitializeComponent();

        _rocketLeagueClient.MessageReceived += OnRocketLeagueMessage;

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        bool connected = await _rocketLeagueClient.ConnectAsync();

        if (connected)
        {
            ConnectionStatus.Text = "● Connecté à Rocket League";
            ConnectionStatus.Foreground = Brushes.Green;
        }
        else
        {
            ConnectionStatus.Text = "● Rocket League non connecté";
            ConnectionStatus.Foreground = Brushes.Red;
        }
    }

    private void OnRocketLeagueMessage(string message)
    {
        MatchState? match = RocketLeagueParser.ParseMatchState(message);

        if (match == null)
            return;

        Dispatcher.Invoke(() =>
        {
            UpdateMatchDisplay(match);
        });
    }

    private void UpdateMatchDisplay(MatchState match)
    {
        // -------------------------
        // Équipes
        // -------------------------

        Team? blueTeam = match.Game.Teams
            .FirstOrDefault(team => team.TeamNum == 0);

        Team? orangeTeam = match.Game.Teams
            .FirstOrDefault(team => team.TeamNum == 1);

        if (blueTeam != null)
            BlueTeamName.Text = blueTeam.Name;

        if (orangeTeam != null)
            OrangeTeamName.Text = orangeTeam.Name;

        if (blueTeam != null && orangeTeam != null)
        {
            ScoreText.Text =
                $"{blueTeam.Score} - {orangeTeam.Score}";
        }

        // -------------------------
        // Chronomètre
        // -------------------------

        int totalSeconds = match.Game.TimeSeconds;

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        TimeText.Text = $"{minutes:00}:{seconds:00}";

        // -------------------------
        // Joueur
        // -------------------------

        Player? player = null;

if (match.Game.bHasTarget && match.Game.Target != null)
{
    player = match.Players.FirstOrDefault(p =>
        p.Shortcut == match.Game.Target.Shortcut
    );
}

if (player == null)
{
    PlayerName.Text = "Aucun joueur ciblé";

    PlayerScore.Text = "";
    PlayerGoals.Text = "";
    PlayerShots.Text = "";
    PlayerAssists.Text = "";
    PlayerSaves.Text = "";
    PlayerBoost.Text = "";

    return;
}

        PlayerName.Text = player.Name;

        PlayerScore.Text =
            $"Score : {player.Score}";

        PlayerGoals.Text =
            $"Buts : {player.Goals}";

        PlayerShots.Text =
            $"Tirs : {player.Shots}";

        PlayerAssists.Text =
            $"Passes : {player.Assists}";

        PlayerSaves.Text =
            $"Arrêts : {player.Saves}";

        PlayerBoost.Text =
            $"Boost : {player.Boost}%";
    }
}