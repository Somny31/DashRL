using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DashRL.Models;

namespace DashRL;

public partial class CustomOverlayPreviewWindow : Window
{
    private readonly CustomOverlay _overlay;

    public CustomOverlayPreviewWindow(
        CustomOverlay overlay)
    {
        InitializeComponent();

        _overlay = overlay;

        Width = _overlay.Width;
        Height = _overlay.Height;

        PreviewCanvas.Width = _overlay.Width;
        PreviewCanvas.Height = _overlay.Height;

        Loaded +=
            CustomOverlayPreviewWindow_Loaded;
    }

    private void CustomOverlayPreviewWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        RenderOverlay();
    }

    private void RenderOverlay()
    {
        PreviewCanvas.Children.Clear();

        foreach (
            CustomOverlayElement element
            in _overlay.Elements)
        {
            FrameworkElement visual =
                CreateVisual(
                    element
                );

            Canvas.SetLeft(
                visual,
                element.X
            );

            Canvas.SetTop(
                visual,
                element.Y
            );

            PreviewCanvas.Children.Add(
                visual
            );
        }
    }

    private static FrameworkElement CreateVisual(
        CustomOverlayElement element)
    {
        Border border =
            new()
            {
                Width =
                    Math.Max(
                        1,
                        element.Width
                    ),

                Height =
                    Math.Max(
                        1,
                        element.Height
                    ),

                Background =
                    GetBrush(
                        element.BackgroundColor
                    ),

                BorderBrush =
                    GetBrush(
                        element.BorderColor
                    ),

                BorderThickness =
                    new Thickness(
                        Math.Max(
                            0,
                            element.BorderThickness
                        )
                    ),

                CornerRadius =
                    new CornerRadius(
                        Math.Max(
                            0,
                            element.CornerRadius
                        )
                    ),

                Opacity =
                    Math.Clamp(
                        element.Opacity,
                        0,
                        1
                    )
            };

        if (
            element.Type ==
            CustomOverlayElementType.Container)
        {
            return border;
        }

        TextBlock text =
            new()
            {
                Text =
                    GetDisplayText(
                        element
                    ),

                Foreground =
                    GetBrush(
                        element.TextColor,
                        Brushes.White
                    ),

                FontSize =
                    Math.Max(
                        1,
                        element.FontSize
                    ),

                FontWeight =
                    element.Bold
                        ? FontWeights.Bold
                        : FontWeights.Normal,

                VerticalAlignment =
                    VerticalAlignment.Center,

                HorizontalAlignment =
                    HorizontalAlignment.Center,

                TextAlignment =
                    TextAlignment.Center,

                TextWrapping =
                    TextWrapping.Wrap
            };

        border.Child =
            text;

        return border;
    }

    private static string GetDisplayText(
        CustomOverlayElement element)
    {
        return element.Type switch
        {
            CustomOverlayElementType.Wins =>
                "0 W",

            CustomOverlayElementType.Losses =>
                "0 L",

            CustomOverlayElementType.Streak =>
                "+0",

            CustomOverlayElementType.Text =>
                element.Text ?? string.Empty,

            _ =>
                string.Empty
        };
    }

    private static Brush GetBrush(
        string? color,
        Brush? fallback = null)
    {
        fallback ??=
            Brushes.Transparent;

        if (
            string.IsNullOrWhiteSpace(
                color
            ))
        {
            return fallback;
        }

        try
        {
            object? converted =
                ColorConverter.ConvertFromString(
                    color
                );

            if (converted is Color parsedColor)
            {
                return new SolidColorBrush(
                    parsedColor
                );
            }
        }
        catch
        {
            // Couleur invalide : on utilise la couleur de secours.
        }

        return fallback;
    }

    private void Window_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (
            e.ButtonState ==
            MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Window_KeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }

    private void ClosePreviewButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}
