using System;

namespace DashRL.Models;

public enum OverlayPosition
{
    TopLeft,
    TopCenter,
    TopRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}

public class OverlaySettings
{
    private OverlayPosition _position = OverlayPosition.TopCenter;
    private double _scale = 1.0;
    private double _opacity = 1.0;
    private bool _showWins = true;
    private bool _showLosses = true;
    private bool _showStreak = true;
    private bool _clickThrough = false;

    public event Action? SettingsChanged;

    public OverlayPosition Position
    {
        get => _position;
        set
        {
            if (_position == value)
                return;

            _position = value;
            SettingsChanged?.Invoke();
        }
    }

    public double Scale
    {
        get => _scale;
        set
        {
            double newValue = Math.Clamp(value, 0.75, 1.50);

            if (Math.Abs(_scale - newValue) < 0.001)
                return;

            _scale = newValue;
            SettingsChanged?.Invoke();
        }
    }

    public double Opacity
    {
        get => _opacity;
        set
        {
            double newValue = Math.Clamp(value, 0.20, 1.0);

            if (Math.Abs(_opacity - newValue) < 0.001)
                return;

            _opacity = newValue;
            SettingsChanged?.Invoke();
        }
    }

    public bool ShowWins
    {
        get => _showWins;
        set
        {
            if (_showWins == value)
                return;

            _showWins = value;
            SettingsChanged?.Invoke();
        }
    }

    public bool ShowLosses
    {
        get => _showLosses;
        set
        {
            if (_showLosses == value)
                return;

            _showLosses = value;
            SettingsChanged?.Invoke();
        }
    }

    public bool ShowStreak
    {
        get => _showStreak;
        set
        {
            if (_showStreak == value)
                return;

            _showStreak = value;
            SettingsChanged?.Invoke();
        }
    }

    public bool ClickThrough
    {
        get => _clickThrough;
        set
        {
            if (_clickThrough == value)
                return;

            _clickThrough = value;
            SettingsChanged?.Invoke();
        }
    }

    public void Reset()
    {
        _position = OverlayPosition.TopCenter;
        _scale = 1.0;
        _opacity = 1.0;
        _showWins = true;
        _showLosses = true;
        _showStreak = true;
        _clickThrough = false;

        SettingsChanged?.Invoke();
    }
}