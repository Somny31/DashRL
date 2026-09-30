namespace DashRL.Models;

public enum CustomOverlayElementType
{
    Wins,
    Losses,
    Streak,
    Text,
    Container
}

public class CustomOverlayElement
{
    public string Id { get; set; } =
        System.Guid.NewGuid().ToString("N");

    public CustomOverlayElementType Type { get; set; }

    public double X { get; set; }

    public double Y { get; set; }

    public double Width { get; set; } = 100;

    public double Height { get; set; } = 40;

    public string Text { get; set; } = "";

    public double FontSize { get; set; } = 18;

    public string TextColor { get; set; } = "#FFFFFF";

    public string BackgroundColor { get; set; } = "#00000000";

    public string BorderColor { get; set; } = "#00000000";

    public double BorderThickness { get; set; }

    public double CornerRadius { get; set; }

    public double Opacity { get; set; } = 1.0;

    public bool Bold { get; set; }
}