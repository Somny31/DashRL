using System;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DashRL.Models;
using DashRL.RocketLeague;
using DashRL.Services;

namespace DashRL;

public partial class MainWindow : Window
{
    private readonly RocketLeagueClient _rocketLeagueClient = new();
    private readonly SessionTracker _sessionTracker = new();

    private readonly OverlaySettings _overlaySettings = new();

    private OverlayWindow? _overlayWindow;

    private string? _lastCountedMatchGuid;

    private string _selectedOverlayStyle = "Minimal";

    private static readonly string OverlaySettingsFilePath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DashRL",
            "overlay-settings.json"
        );

    private sealed class SavedOverlaySettings
    {
        public string Style { get; set; } = "Minimal";
        public OverlayPosition Position { get; set; } = OverlayPosition.TopCenter;
        public double Scale { get; set; } = 1.0;
        public double Opacity { get; set; } = 1.0;
        public bool ShowWins { get; set; } = true;
        public bool ShowLosses { get; set; } = true;
        public bool ShowStreak { get; set; } = true;
        public bool ClickThrough { get; set; }
    }


    public MainWindow()
    {
        InitializeComponent();

        _rocketLeagueClient.MessageReceived +=
            OnRocketLeagueMessage;

        _sessionTracker.SessionChanged +=
            OnSessionChanged;

        _overlaySettings.SettingsChanged +=
            OverlaySettings_SettingsChanged;

        Loaded +=
            MainWindow_Loaded;

        StateChanged +=
            MainWindow_StateChanged;

        Closed +=
            MainWindow_Closed;

        LoadOverlaySettings();

        UpdateSessionDisplay();

        ShowDashboardPage();

        SelectOverlayStyle(_selectedOverlayStyle);

        UpdateMaximizeButton();

        UpdateOverlaySettingsControls();
    }


    private void SaveOverlaySettings()
    {
        try
        {
            string? directory = Path.GetDirectoryName(OverlaySettingsFilePath);

            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            SavedOverlaySettings data = new()
            {
                Style = _selectedOverlayStyle,
                Position = _overlaySettings.Position,
                Scale = _overlaySettings.Scale,
                Opacity = _overlaySettings.Opacity,
                ShowWins = _overlaySettings.ShowWins,
                ShowLosses = _overlaySettings.ShowLosses,
                ShowStreak = _overlaySettings.ShowStreak,
                ClickThrough = _overlaySettings.ClickThrough
            };

            string json = JsonSerializer.Serialize(
                data,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }
            );

            File.WriteAllText(OverlaySettingsFilePath, json);
        }
        catch
        {
            // Une erreur de sauvegarde ne doit pas empêcher DashRL de se fermer.
        }
    }


    private void LoadOverlaySettings()
    {
        try
        {
            if (!File.Exists(OverlaySettingsFilePath))
                return;

            string json = File.ReadAllText(OverlaySettingsFilePath);

            SavedOverlaySettings? data =
                JsonSerializer.Deserialize<SavedOverlaySettings>(json);

            if (data == null)
                return;

            _selectedOverlayStyle =
                data.Style switch
                {
                    "Competitive" => "Competitive",
                    "Cards" => "Cards",
                    "Pill" => "Pill",
                    _ => "Minimal"
                };

            _overlaySettings.Position = data.Position;
            _overlaySettings.Scale = data.Scale;
            _overlaySettings.Opacity = data.Opacity;
            _overlaySettings.ShowWins = data.ShowWins;
            _overlaySettings.ShowLosses = data.ShowLosses;
            _overlaySettings.ShowStreak = data.ShowStreak;
            _overlaySettings.ClickThrough = data.ClickThrough;
        }
        catch
        {
            _selectedOverlayStyle = "Minimal";
            _overlaySettings.Reset();
        }
    }


    // ================================================================
    // WINDOW
    // ================================================================

    private void TitleBar_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;

        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }

        if (WindowState == WindowState.Maximized)
        {
            Point mousePosition =
                e.GetPosition(this);

            double percent =
                mousePosition.X /
                ActualWidth;

            WindowState =
                WindowState.Normal;

            Left =
                mousePosition.X -
                (ActualWidth * percent);

            Top = 0;
        }

        DragMove();
    }


    private void MinimizeButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        WindowState =
            WindowState.Minimized;
    }


    private void MaximizeButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ToggleMaximize();
    }


    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }


    private void MainWindow_Closed(
        object? sender,
        EventArgs e)
    {
        SaveOverlaySettings();

        // Ferme l'overlay s'il est encore ouvert.
        if (_overlayWindow != null)
        {
            _overlayWindow.Close();
            _overlayWindow = null;
        }

        // Ferme complètement DashRL et toutes ses fenêtres.
        Application.Current.Shutdown();
    }


    private void ToggleMaximize()
    {
        WindowState =
            WindowState ==
            WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
    }


    private void MainWindow_StateChanged(
        object? sender,
        EventArgs e)
    {
        UpdateMaximizeButton();
    }


    private void UpdateMaximizeButton()
{
    if (MaximizeButton == null)
        return;

    MaximizeButton.ToolTip =
        WindowState == WindowState.Maximized
            ? "Restaurer"
            : "Agrandir";
}


    // ================================================================
    // CONNECTION
    // ================================================================

    private async void MainWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        bool connected =
            await _rocketLeagueClient
                .ConnectAsync();

        if (connected)
        {
            ConnectionStatus.Text =
                "Connecté";

            ConnectionStatus.Foreground =
                new SolidColorBrush(
                    Color.FromRgb(
                        88,
                        201,
                        133
                    )
                );
        }
        else
        {
            ConnectionStatus.Text =
                "Non connecté";

            ConnectionStatus.Foreground =
                new SolidColorBrush(
                    Color.FromRgb(
                        217,
                        86,
                        100
                    )
                );
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

        UpdateOverlaySettingsControls();
    }


    private static void SetNavigationButtonState(
        Button button,
        bool active)
    {
        if (active)
        {
            button.Background =
                new SolidColorBrush(
                    Color.FromRgb(
                        24,
                        27,
                        36
                    )
                );

            button.Foreground =
                new SolidColorBrush(
                    Color.FromRgb(
                        241,
                        242,
                        245
                    )
                );
        }
        else
        {
            button.Background =
                Brushes.Transparent;

            button.Foreground =
                new SolidColorBrush(
                    Color.FromRgb(
                        133,
                        139,
                        153
                    )
                );
        }
    }


    // ================================================================
    // OVERLAY STYLE
    // ================================================================

    private void MinimalPresetButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SelectOverlayStyle(
            "Minimal"
        );
    }


    private void CompetitivePresetButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SelectOverlayStyle(
            "Competitive"
        );
    }


    private void CardsPresetButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SelectOverlayStyle(
            "Cards"
        );
    }


    private void PillPresetButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SelectOverlayStyle(
            "Pill"
        );
    }


    private void SelectOverlayStyle(
        string style)
    {
        _selectedOverlayStyle =
            style;

        MinimalPreview.Visibility =
            Visibility.Collapsed;

        CompetitivePreview.Visibility =
            Visibility.Collapsed;

        CardsPreview.Visibility =
            Visibility.Collapsed;

        PillPreview.Visibility =
            Visibility.Collapsed;

        ResetPresetButtons();

        switch (style)
        {
            case "Competitive":

                CompetitivePreview.Visibility =
                    Visibility.Visible;

                SetSelectedPresetButton(
                    CompetitivePresetButton
                );

                break;


            case "Cards":

                CardsPreview.Visibility =
                    Visibility.Visible;

                SetSelectedPresetButton(
                    CardsPresetButton
                );

                break;


            case "Pill":

                PillPreview.Visibility =
                    Visibility.Visible;

                SetSelectedPresetButton(
                    PillPresetButton
                );

                break;


            default:

                MinimalPreview.Visibility =
                    Visibility.Visible;

                SetSelectedPresetButton(
                    MinimalPresetButton
                );

                break;
        }

        SelectedStyleText.Text =
            style;

        UpdateOverlayPreview();

        _overlayWindow?
            .SetOverlayStyle(
                _selectedOverlayStyle
            );
    }


    private void ResetPresetButtons()
    {
        Button[] buttons =
        {
            MinimalPresetButton,
            CompetitivePresetButton,
            CardsPresetButton,
            PillPresetButton
        };

        foreach (Button button in buttons)
        {
            button.Background =
                new SolidColorBrush(
                    Color.FromRgb(
                        19,
                        22,
                        29
                    )
                );

            button.BorderBrush =
                new SolidColorBrush(
                    Color.FromRgb(
                        34,
                        38,
                        48
                    )
                );

            button.BorderThickness =
                new Thickness(1);
        }
    }


    private static void SetSelectedPresetButton(
        Button button)
    {
        button.Background =
            new SolidColorBrush(
                Color.FromRgb(
                    27,
                    29,
                    40
                )
            );

        button.BorderBrush =
            new SolidColorBrush(
                Color.FromRgb(
                    124,
                    108,
                    255
                )
            );

        button.BorderThickness =
            new Thickness(1);
    }


    // ================================================================
    // OVERLAY SETTINGS
    // ================================================================

    private void OverlaySettings_SettingsChanged()
    {
        Dispatcher.Invoke(
            UpdateOverlaySettingsControls
        );
    }


    private void OverlayPositionButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.Tag is not string position)
            return;

        switch (position)
        {
            case "TopLeft":

                _overlaySettings.Position =
                    OverlayPosition.TopLeft;

                break;


            case "TopRight":

                _overlaySettings.Position =
                    OverlayPosition.TopRight;

                break;


            case "BottomLeft":

                _overlaySettings.Position =
                    OverlayPosition.BottomLeft;

                break;


            case "BottomCenter":

                _overlaySettings.Position =
                    OverlayPosition.BottomCenter;

                break;


            case "BottomRight":

                _overlaySettings.Position =
                    OverlayPosition.BottomRight;

                break;


            default:

                _overlaySettings.Position =
                    OverlayPosition.TopCenter;

                break;
        }
    }


    private void OverlayScaleSlider_ValueChanged(
        object sender,
        RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded)
            return;

        _overlaySettings.Scale =
            e.NewValue / 100.0;
    }


    private void OverlayOpacitySlider_ValueChanged(
        object sender,
        RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded)
            return;

        _overlaySettings.Opacity =
            e.NewValue / 100.0;
    }


    private void ShowWinsCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        _overlaySettings.ShowWins =
            ShowWinsCheckBox.IsChecked ==
            true;
    }


    private void ShowLossesCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        _overlaySettings.ShowLosses =
            ShowLossesCheckBox.IsChecked ==
            true;
    }


    private void ShowStreakCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        _overlaySettings.ShowStreak =
            ShowStreakCheckBox.IsChecked ==
            true;
    }


    private void ClickThroughCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        _overlaySettings.ClickThrough =
            ClickThroughCheckBox.IsChecked ==
            true;
    }


    private void ResetOverlaySettingsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _overlaySettings.Reset();

        UpdateOverlaySettingsControls();
    }


    private void UpdateOverlaySettingsControls()
    {
        if (OverlayScaleSlider == null)
            return;

        OverlayScaleSlider.Value =
            _overlaySettings.Scale *
            100;

        OverlayOpacitySlider.Value =
            _overlaySettings.Opacity *
            100;

        ScaleValueText.Text =
            $"{Math.Round(_overlaySettings.Scale * 100)}%";

        OpacityValueText.Text =
            $"{Math.Round(_overlaySettings.Opacity * 100)}%";

        ShowWinsCheckBox.IsChecked =
            _overlaySettings.ShowWins;

        ShowLossesCheckBox.IsChecked =
            _overlaySettings.ShowLosses;

        ShowStreakCheckBox.IsChecked =
            _overlaySettings.ShowStreak;

        ClickThroughCheckBox.IsChecked =
            _overlaySettings.ClickThrough;

        UpdatePositionButtons();
    }


    private void UpdatePositionButtons()
    {
        if (TopLeftPositionButton == null)
            return;

        Button[] buttons =
        {
            TopLeftPositionButton,
            TopCenterPositionButton,
            TopRightPositionButton,
            BottomLeftPositionButton,
            BottomCenterPositionButton,
            BottomRightPositionButton
        };

        foreach (Button button in buttons)
        {
            button.Background =
                new SolidColorBrush(
                    Color.FromRgb(
                        23,
                        26,
                        34
                    )
                );

            button.BorderBrush =
                new SolidColorBrush(
                    Color.FromRgb(
                        42,
                        47,
                        58
                    )
                );
        }

        Button selected =
            _overlaySettings.Position switch
            {
                OverlayPosition.TopLeft =>
                    TopLeftPositionButton,

                OverlayPosition.TopRight =>
                    TopRightPositionButton,

                OverlayPosition.BottomLeft =>
                    BottomLeftPositionButton,

                OverlayPosition.BottomCenter =>
                    BottomCenterPositionButton,

                OverlayPosition.BottomRight =>
                    BottomRightPositionButton,

                _ =>
                    TopCenterPositionButton
            };

        selected.Background =
            new SolidColorBrush(
                Color.FromRgb(
                    42,
                    39,
                    68
                )
            );

        selected.BorderBrush =
            new SolidColorBrush(
                Color.FromRgb(
                    124,
                    108,
                    255
                )
            );
    }


    // ================================================================
    // OVERLAY WINDOW
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
                _sessionTracker,
                _selectedOverlayStyle,
                _overlaySettings
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


    // ================================================================
    // PREVIEW
    // ================================================================

    private void UpdateOverlayPreview()
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
    // SESSION
    // ================================================================

    private void OnSessionChanged()
    {
        Dispatcher.Invoke(() =>
        {
            UpdateSessionDisplay();

            UpdateOverlayPreview();

            _overlayWindow?
                .UpdateSession();
        });
    }


    // ================================================================
    // ROCKET LEAGUE
    // ================================================================

    private void OnRocketLeagueMessage(
        string message)
    {
        MatchState? match =
            RocketLeagueParser
                .ParseMatchState(
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
            match.Game.Teams
                .FirstOrDefault(
                    team =>
                        team.TeamNum == 0
                );

        Team? orangeTeam =
            match.Game.Teams
                .FirstOrDefault(
                    team =>
                        team.TeamNum == 1
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


        if (
            blueTeam != null &&
            orangeTeam != null
        )
        {
            ScoreText.Text =
                $"{blueTeam.Score} : {orangeTeam.Score}";
        }


        int totalSeconds =
            match.Game.TimeSeconds;

        int minutes =
            totalSeconds / 60;

        int seconds =
            totalSeconds % 60;


        TimeText.Text =
            $"{minutes:00}:{seconds:00}";


        Player? player =
            null;


        if (
            match.Game.bHasTarget &&
            match.Game.Target != null
        )
        {
            player =
                match.Players
                    .FirstOrDefault(
                        p =>
                            p.Shortcut ==
                            match.Game.Target.Shortcut
                    );
        }


        if (player == null)
        {
            PlayerName.Text =
                "Aucun joueur ciblé";

            PlayerScore.Text =
                "—";

            PlayerGoals.Text =
                "—";

            PlayerShots.Text =
                "—";

            PlayerAssists.Text =
                "—";

            PlayerSaves.Text =
                "—";

            PlayerBoost.Text =
                "—";

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


        if (
            string.IsNullOrWhiteSpace(
                match.MatchGuid
            )
        )
        {
            return;
        }


        if (
            _lastCountedMatchGuid ==
            match.MatchGuid
        )
        {
            return;
        }


        if (
            !match.Game.bHasTarget ||
            match.Game.Target == null
        )
        {
            return;
        }


        Player? player =
            match.Players
                .FirstOrDefault(
                    p =>
                        p.Shortcut ==
                        match.Game.Target.Shortcut
                );


        if (player == null)
            return;


        Team? playerTeam =
            match.Game.Teams
                .FirstOrDefault(
                    team =>
                        team.TeamNum ==
                        player.TeamNum
                );


        if (playerTeam == null)
            return;


        if (
            string.IsNullOrWhiteSpace(
                match.Game.Winner
            )
        )
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


    // ================================================================
    // SESSION DISPLAY
    // ================================================================

    private void UpdateSessionDisplay()
    {
        SessionWins.Text =
            _sessionTracker
                .Wins
                .ToString();

        SessionLosses.Text =
            _sessionTracker
                .Losses
                .ToString();


        if (_sessionTracker.Streak > 0)
        {
            SessionStreak.Text =
                $"+{_sessionTracker.Streak}";

            SessionStreak.Foreground =
                new SolidColorBrush(
                    Color.FromRgb(
                        88,
                        201,
                        133
                    )
                );

            return;
        }


        if (_sessionTracker.Streak < 0)
        {
            SessionStreak.Text =
                _sessionTracker
                    .Streak
                    .ToString();

            SessionStreak.Foreground =
                new SolidColorBrush(
                    Color.FromRgb(
                        217,
                        86,
                        100
                    )
                );

            return;
        }


        SessionStreak.Text =
            "0";

        SessionStreak.Foreground =
            new SolidColorBrush(
                Color.FromRgb(
                    241,
                    242,
                    245
                )
            );
    }
}