namespace DashRL.Models;

public class MatchState
{
    public string MatchGuid { get; set; } = "";

    public Player[] Players { get; set; } = [];

    public Game Game { get; set; } = new();
}