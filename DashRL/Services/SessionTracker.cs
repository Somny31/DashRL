using System;

namespace DashRL.Services;

public class SessionTracker
{
    public int Wins { get; private set; }

    public int Losses { get; private set; }

    public int Streak { get; private set; }

    // Permet au Dashboard et à l'Overlay
    // d'être prévenus lorsqu'une statistique change.
    public event Action? SessionChanged;

    // =========================================================
    // VICTOIRE
    // =========================================================

    public void AddWin()
    {
        Wins++;

        if (Streak >= 0)
        {
            Streak++;
        }
        else
        {
            Streak = 1;
        }

        SessionChanged?.Invoke();
    }

    // =========================================================
    // DÉFAITE
    // =========================================================

    public void AddLoss()
    {
        Losses++;

        if (Streak <= 0)
        {
            Streak--;
        }
        else
        {
            Streak = -1;
        }

        SessionChanged?.Invoke();
    }

    // =========================================================
    // RESET
    // =========================================================

    public void Reset()
    {
        Wins = 0;
        Losses = 0;
        Streak = 0;

        SessionChanged?.Invoke();
    }
}