using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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

private bool _isUpdatingProperties;
private readonly Dictionary<FrameworkElement, SelectionAdorner>
    _selectionAdorners = new();

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


// Mise à jour des propriétés de l'élément sélectionné.

PositionXTextBox.TextChanged +=
    PropertyTextBox_TextChanged;

PositionYTextBox.TextChanged +=
    PropertyTextBox_TextChanged;

WidthTextBox.TextChanged +=
    PropertyTextBox_TextChanged;

HeightTextBox.TextChanged +=
    PropertyTextBox_TextChanged;

ElementTextBox.TextChanged +=
    PropertyTextBox_TextChanged;

FontSizeTextBox.TextChanged +=
    PropertyTextBox_TextChanged;

TextColorTextBox.TextChanged +=
    PropertyTextBox_TextChanged;

BackgroundColorTextBox.TextChanged +=
    PropertyTextBox_TextChanged;
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
        ClearSelectionAdorners();
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
        Dispatcher.BeginInvoke(
    () => UpdateSelectionAdorners()
);
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
        UpdateSelectionAdorners();
    }


    private void UpdatePropertiesPanel()
{
    if (_selectedElement == null)
    {
        SelectedElementText.Text =
            "No element selected";

        return;
    }

    UpdatePropertiesVisibility();

    _isUpdatingProperties = true;

    try
    {
        SelectedElementText.Text =
            _selectedElement.Type switch
            {
                CustomOverlayElementType.Wins =>
                    "Wins",

                CustomOverlayElementType.Losses =>
                    "Losses",

                CustomOverlayElementType.Streak =>
                    "Streak",

                CustomOverlayElementType.Text =>
                    "Text",

                CustomOverlayElementType.Container =>
                    "Container",

                _ =>
                    "Element"
            };


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
            _selectedElement.TextColor;

        BackgroundColorTextBox.Text =
            _selectedElement.BackgroundColor;
    }
    finally
    {
        _isUpdatingProperties = false;
    }
}

private void UpdatePropertiesVisibility()
{
    if (_selectedElement == null)
        return;


    bool isText =
        _selectedElement.Type ==
        CustomOverlayElementType.Text;

    bool isContainer =
        _selectedElement.Type ==
        CustomOverlayElementType.Container;


    // Seul un véritable élément Text
    // peut modifier son contenu.

    TextPropertyPanel.Visibility =
        isText
            ? Visibility.Visible
            : Visibility.Collapsed;


    // Le Font Size est disponible sur tous
    // les éléments texte, sauf Container.

    FontSizePropertyPanel.Visibility =
    isContainer
        ? Visibility.Collapsed
        : Visibility.Visible;


TextColorPropertyPanel.Visibility =
    isContainer
        ? Visibility.Collapsed
        : Visibility.Visible;
}

// ================================================================
// PROPERTIES
// ================================================================

private void PropertyTextBox_TextChanged(
    object sender,
    TextChangedEventArgs e)
{
    if (_isUpdatingProperties ||
        _selectedElement == null)
    {
        return;
    }


    CustomOverlayElement element =
        _selectedElement;


    // POSITION X

    if (!string.IsNullOrWhiteSpace(
            PositionXTextBox.Text) &&
        double.TryParse(
            PositionXTextBox.Text,
            out double x))
    {
        element.X =
            Math.Clamp(
                x,
                0,
                Math.Max(
                    0,
                    OverlayCanvas.Width -
                    element.Width
                )
            );
    }


    // POSITION Y

    if (!string.IsNullOrWhiteSpace(
            PositionYTextBox.Text) &&
        double.TryParse(
            PositionYTextBox.Text,
            out double y))
    {
        element.Y =
            Math.Clamp(
                y,
                0,
                Math.Max(
                    0,
                    OverlayCanvas.Height -
                    element.Height
                )
            );
    }


    // WIDTH

    if (!string.IsNullOrWhiteSpace(
            WidthTextBox.Text) &&
        double.TryParse(
            WidthTextBox.Text,
            out double width))
    {
        element.Width =
            Math.Clamp(
                width,
                10,
                OverlayCanvas.Width
            );
    }


    // HEIGHT

    if (!string.IsNullOrWhiteSpace(
            HeightTextBox.Text) &&
        double.TryParse(
            HeightTextBox.Text,
            out double height))
    {
        element.Height =
            Math.Clamp(
                height,
                10,
                OverlayCanvas.Height
            );
    }


    // FONT SIZE

    if (!string.IsNullOrWhiteSpace(
            FontSizeTextBox.Text) &&
        double.TryParse(
            FontSizeTextBox.Text,
            out double fontSize))
    {
        element.FontSize =
            Math.Clamp(
                fontSize,
                6,
                100
            );
    }


    // TEXT

    element.Text =
        ElementTextBox.Text;


    // TEXT COLOR

    if (IsValidColor(
            TextColorTextBox.Text))
    {
        element.TextColor =
            TextColorTextBox.Text;
    }


    // BACKGROUND COLOR

    if (IsValidColor(
            BackgroundColorTextBox.Text))
    {
        element.BackgroundColor =
            BackgroundColorTextBox.Text;
    }


    UpdateSelectedVisual();
}


private static bool IsValidColor(
    string color)
{
    try
    {
        object? converted =
            ColorConverter.ConvertFromString(
                color
            );

        return converted is Color;
    }
    catch
    {
        return false;
    }
}

private void UpdateSelectedVisual()
{
    if (_selectedElement == null ||
        _selectedVisual == null)
    {
        return;
    }


    CustomOverlayElement element =
        _selectedElement;


    _selectedVisual.Width =
        element.Width;

    _selectedVisual.Height =
        element.Height;


    Canvas.SetLeft(
        _selectedVisual,
        element.X
    );

    Canvas.SetTop(
        _selectedVisual,
        element.Y
    );


    _selectedVisual.Opacity =
        element.Opacity;


    if (_selectedVisual is not Border border)
        return;


    border.Background =
        GetBrush(
            element.BackgroundColor
        );

    border.BorderBrush =
        GetBrush(
            element.BorderColor
        );

    border.BorderThickness =
        new Thickness(
            element.BorderThickness
        );

    border.CornerRadius =
        new CornerRadius(
            element.CornerRadius
        );


    if (border.Child is not TextBlock text)
        return;


    text.Text =
        GetDisplayText(
            element
        );

    text.FontSize =
        element.FontSize;

    text.Foreground =
        GetBrush(
            element.TextColor
        );

    text.FontWeight =
        element.Bold
            ? FontWeights.Bold
            : FontWeights.Normal;
}

// ================================================================
// SELECTION VISUAL
// ================================================================

private void UpdateSelectionAdorners()
{
    ClearSelectionAdorners();


    foreach (UIElement child in OverlayCanvas.Children)
    {
        if (child is not FrameworkElement visual)
            continue;

        AdornerLayer? layer =
            AdornerLayer.GetAdornerLayer(
                visual
            );

        if (layer == null)
            continue;


        bool isSelected =
            ReferenceEquals(
                visual,
                _selectedVisual
            );


        SelectionAdorner adorner =
            new(
                visual,
                isSelected
            );


        layer.Add(
            adorner
        );

        _selectionAdorners[visual] =
            adorner;
    }
}


private void ClearSelectionAdorners()
{
    foreach (
        KeyValuePair<FrameworkElement, SelectionAdorner> item
        in _selectionAdorners)
    {
        AdornerLayer? layer =
            AdornerLayer.GetAdornerLayer(
                item.Key
            );

        layer?.Remove(
            item.Value
        );
    }


    _selectionAdorners.Clear();
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


        _isUpdatingProperties = true;

try
{
    PositionXTextBox.Text =
        Math.Round(
            newX
        ).ToString();

    PositionYTextBox.Text =
        Math.Round(
            newY
        ).ToString();
}
finally
{
    _isUpdatingProperties = false;
}
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

internal sealed class SelectionAdorner : Adorner
{
    private readonly bool _isSelected;


    public SelectionAdorner(
        UIElement adornedElement,
        bool isSelected)
        : base(adornedElement)
    {
        _isSelected =
            isSelected;

        IsHitTestVisible =
            false;
    }


    protected override void OnRender(
        DrawingContext drawingContext)
    {
        base.OnRender(
            drawingContext
        );


        Brush brush;

        double thickness;


        if (_isSelected)
        {
            brush =
                new SolidColorBrush(
                    Color.FromRgb(
                        124,
                        108,
                        255
                    )
                );

            thickness =
                2;
        }
        else
        {
            brush =
                new SolidColorBrush(
                    Color.FromArgb(
                        55,
                        180,
                        185,
                        200
                    )
                );

            thickness =
                1;
        }


        Pen pen =
            new(
                brush,
                thickness
            );


        Rect rectangle =
            new(
                new Point(
                    0,
                    0
                ),
                AdornedElement.RenderSize
            );


        drawingContext.DrawRectangle(
            null,
            pen,
            rectangle
        );
    }
}