namespace DashRL.Models;

public class Player
{
    public string Name { get; set; } = "";
    public string PrimaryId { get; set; } = "";

    public int Shortcut { get; set; }
    public int TeamNum { get; set; }

    public int Score { get; set; }
    public int Goals { get; set; }
    public int Shots { get; set; }
    public int Assists { get; set; }
    public int Saves { get; set; }
    public int Touches { get; set; }
    public int CarTouches { get; set; }
    public int Demos { get; set; }

    public bool bOnGround { get; set; }
    public bool bHasCar { get; set; }

    public double Speed { get; set; }
    public int Boost { get; set; }

    public string[] Loadout { get; set; } = [];
}