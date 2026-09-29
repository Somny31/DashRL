namespace DashRL.Models;

public class Game
{
    public Team[] Teams { get; set; } = [];

    public int PlaylistId { get; set; }
    public int TimeSeconds { get; set; }

    public bool bOvertime { get; set; }

    public Ball Ball { get; set; } = new();

    public bool bReplay { get; set; }
    public bool bHasWinner { get; set; }

    public string Winner { get; set; } = "";
    public string Arena { get; set; } = "";

    public bool bHasTarget { get; set; }

    public Target? Target { get; set; }
}