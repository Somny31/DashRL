using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DashRL.Models;

namespace DashRL;

public partial class CustomOverlayEditor : Window
{
    private readonly CustomOverlay _overlay;

    private CustomOverlayElement? _selectedElement;
    private FrameworkElement? _selectedVisual;

    private bool _isDragging;
    private Point _dragStart;
    private double _elementStartX;
    private double _elementStartY;


    // ================================================================
    // CONSTRUCTOR
    // ================================================================

    public CustomOverlayEditor()
    {
        InitializeComponent();

        _overlay =
            new CustomOverlay
            {
                Name = "New Overlay",
                Width = 500,
                Height = 150
            };

        Loaded +=
            CustomOverlayEditor_Loaded;

        AddWinsButton.Click +=
            AddWinsButton_Click;

        AddLossesButton.Click +=
            AddLossesButton_Click;

        AddStreakButton.Click +=
            AddStreakButton_Click;

        AddTextButton.Click +=
            AddTextButton_Click;

        AddContainerButton.Click +=
            AddContainerButton_Click;
    }


    // ================================================================
    // LOADING
    // ================================================================

    private void CustomOverlayEditor_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        OverlayCanvas.Width =
            _overlay.Width;

        OverlayCanvas.Height =
            _overlay.Height;

        RenderOverlay();
    }


    // ================================================================
    // ADD ELEMENTS
    // ================================================================

    private void AddWinsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        AddElement(
            CustomOverlayElementType.Wins
        );
    }


    private void AddLossesButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        AddElement(
            CustomOverlayElementType.Losses
        );
    }


    private void AddStreakButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        AddElement(
            CustomOverlayElementType.Streak
        );
    }


    private void AddTextButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        AddElement(
            CustomOverlayElementType.Text
        );
    }


    private void AddContainerButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        AddElement(
            CustomOverlayElementType.Container
        );
    }


    private void AddElement(
        CustomOverlayElementType type)
    {
        CustomOverlayElement element =
            CreateElement(type);

        _overlay.Elements.Add(
            element
        );

        RenderOverlay();

        SelectElement(
            element
        );
    }


    private static CustomOverlayElement CreateElement(
        CustomOverlayElementType type)
    {
        CustomOverlayElement element =
            new()
            {
                Type = type,

                X = 20,
                Y = 20,

                Width = 100,
                Height = 40,

                FontSize = 18,

                TextColor =
                    "#FFFFFF",

                BackgroundColor =
                    "#00000000"
            };


        switch (type)
        {
            case CustomOverlayElementType.Wins:

                element.Text =
                    "0 W";

                element.TextColor =
                    "#72D69A";

                element.Bold =
                    true;

                break;


            case CustomOverlayElementType.Losses:

                element.Text =
                    "0 L";

                element.TextColor =
                    "#E87979";

                element.Bold =
                    true;

                break;


            case CustomOverlayElementType.Streak:

                element.Text =
                    "+0";

                element.TextColor =
                    "#A89CFF";

                element.Bold =
                    true;

                break;


            case CustomOverlayElementType.Text:

                element.Text =
                    "Text";

                break;


            case CustomOverlayElementType.Container:

                element.Width =
                    180;

                element.Height =
                    70;

                element.BackgroundColor =
                    "#CC11141B";

                element.BorderColor =
                    "#292E39";

                element.BorderThickness =
                    1;

                element.CornerRadius =
                    8;

                break;
        }

        return element;
    }


    // ================================================================
    // RENDER
    // ================================================================

    private void RenderOverlay()
    {
        OverlayCanvas.Children.Clear();

        foreach (
            CustomOverlayElement element
            in _overlay.Elements)
        {
            FrameworkElement visual =
                CreateVisual(
                    element
                );

            visual.Tag =
                element;

            visual.MouseLeftButtonDown +=
                Element_MouseLeftButtonDown;

            visual.MouseMove +=
                Element_MouseMove;

            visual.MouseLeftButtonUp +=
                Element_MouseLeftButtonUp;

            Canvas.SetLeft(
                visual,
                element.X
            );

            Canvas.SetTop(
                visual,
                element.Y
            );

            OverlayCanvas.Children.Add(
                visual
            );
        }
    }


    private FrameworkElement CreateVisual(
        CustomOverlayElement element)
    {
        if (element.Type ==
            CustomOverlayElementType.Container)
        {
            Border container =
                new()
                {
                    Width =
                        element.Width,

                    Height =
                        element.Height,

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
                            element.BorderThickness
                        ),

                    CornerRadius =
                        new CornerRadius(
                            element.CornerRadius
                        ),

                    Opacity =
                        element.Opacity,

                    Cursor =
                        Cursors.SizeAll
                };

            return container;
        }


        Border border =
            new()
            {
                Width =
                    element.Width,

                Height =
                    element.Height,

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
                        element.BorderThickness
                    ),

                CornerRadius =
                    new CornerRadius(
                        element.CornerRadius
                    ),

                Opacity =
                    element.Opacity,

                Cursor =
                    Cursors.SizeAll
            };


        TextBlock text =
            new()
            {
                Text =
                    GetDisplayText(
                        element
                    ),

                Foreground =
                    GetBrush(
                        element.TextColor
                    ),

                FontSize =
                    element.FontSize,

                FontWeight =
                    element.Bold
                        ? FontWeights.Bold
                        : FontWeights.Normal,

                VerticalAlignment =
                    VerticalAlignment.Center,

                HorizontalAlignment =
                    HorizontalAlignment.Center,

                TextAlignment =
                    TextAlignment.Center
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

            _ =>
                element.Text
        };
    }


    // ================================================================
    // SELECTION
    // ================================================================

    private void SelectElement(
        CustomOverlayElement element)
    {
        _selectedElement =
            element;

        _selectedVisual =
            null;


        foreach (
            UIElement child
            in OverlayCanvas.Children)
        {
            if (child is not FrameworkElement visual)
                continue;

            if (ReferenceEquals(
                    visual.Tag,
                    element))
            {
                _selectedVisual =
                    visual;

                break;
            }
        }


        UpdatePropertiesPanel();
    }


    private void UpdatePropertiesPanel()
    {
        if (_selectedElement == null)
            return;


        PositionXTextBox.Text =
            Math.Round(
                _selectedElement.X
            ).ToString();

        PositionYTextBox.Text =
            Math.Round(
                _selectedElement.Y
            ).ToString();


        WidthTextBox.Text =
            Math.Round(
                _selectedElement.Width
            ).ToString();

        HeightTextBox.Text =
            Math.Round(
                _selectedElement.Height
            ).ToString();


        ElementTextBox.Text =
            _selectedElement.Text;

        FontSizeTextBox.Text =
            _selectedElement
                .FontSize
                .ToString();

        TextColorTextBox.Text =
            _selectedElement
                .TextColor;

        BackgroundColorTextBox.Text =
            _selectedElement
                .BackgroundColor;
    }


    // ================================================================
    // DRAG
    // ================================================================

    private void Element_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement visual)
            return;

        if (visual.Tag is not CustomOverlayElement element)
            return;


        SelectElement(
            element
        );


        _isDragging =
            true;

        _dragStart =
            e.GetPosition(
                OverlayCanvas
            );

        _elementStartX =
            element.X;

        _elementStartY =
            element.Y;


        visual.CaptureMouse();

        e.Handled =
            true;
    }


    private void Element_MouseMove(
        object sender,
        MouseEventArgs e)
    {
        if (!_isDragging ||
            _selectedElement == null ||
            _selectedVisual == null)
        {
            return;
        }


        Point currentPosition =
            e.GetPosition(
                OverlayCanvas
            );


        double deltaX =
            currentPosition.X -
            _dragStart.X;

        double deltaY =
            currentPosition.Y -
            _dragStart.Y;


        double newX =
            _elementStartX +
            deltaX;

        double newY =
            _elementStartY +
            deltaY;


        // Empêche l'élément de sortir du canvas.

        newX =
            Math.Clamp(
                newX,
                0,
                Math.Max(
                    0,
                    OverlayCanvas.Width -
                    _selectedElement.Width
                )
            );

        newY =
            Math.Clamp(
                newY,
                0,
                Math.Max(
                    0,
                    OverlayCanvas.Height -
                    _selectedElement.Height
                )
            );


        _selectedElement.X =
            newX;

        _selectedElement.Y =
            newY;


        Canvas.SetLeft(
            _selectedVisual,
            newX
        );

        Canvas.SetTop(
            _selectedVisual,
            newY
        );


        PositionXTextBox.Text =
            Math.Round(
                newX
            ).ToString();

        PositionYTextBox.Text =
            Math.Round(
                newY
            ).ToString();
    }


    private void Element_MouseLeftButtonUp(
        object sender,
        MouseButtonEventArgs e)
    {
        if (!_isDragging)
            return;


        _isDragging =
            false;


        if (sender is FrameworkElement visual)
        {
            visual.ReleaseMouseCapture();
        }


        e.Handled =
            true;
    }


    // ================================================================
    // COLORS
    // ================================================================

    private static Brush GetBrush(
        string color)
    {
        try
        {
            object? converted =
                ColorConverter
                    .ConvertFromString(
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
            // Couleur invalide.
        }


        return Brushes.Transparent;
    }
}