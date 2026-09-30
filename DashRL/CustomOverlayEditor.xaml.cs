using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using DashRL.Models;
using DashRL.Services;

namespace DashRL;

public partial class CustomOverlayEditor : Window
{
    private readonly CustomOverlay _overlay;
    private readonly CustomOverlayService _overlayService = new();

    private CustomOverlayElement? _selectedElement;
    private FrameworkElement? _selectedVisual;

    private bool _isDragging;
private Point _dragStart;
private double _elementStartX;
private double _elementStartY;

private bool _isUpdatingProperties;
private readonly Dictionary<FrameworkElement, SelectionAdorner>
    _selectionAdorners = new();
private Button? _draggedLayerButton;
private Point _layerDragStart;
private bool _isLayerDragging;

private AdornerLayer? _layerDragAdornerLayer;
private LayerDragAdorner? _layerDragAdorner;
private Point _layerDragMouseOffset;
private int _layerPendingInsertIndex = -1;
private LayerInsertionAdorner? _layerInsertionAdorner;

private const double MinCanvasZoom = 0.10;
private const double MaxCanvasZoom = 2.00;
private const double CanvasWorkspacePadding = 96;

private double _canvasZoom = 1.0;
private bool _isUpdatingCanvasZoom;
private bool _canvasZoomWasChangedManually;

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
                Width = 400,
                Height = 150
            };

        Loaded +=
            CustomOverlayEditor_Loaded;
        PreviewMouseLeftButtonUp +=
    CustomOverlayEditor_PreviewMouseLeftButtonUp;

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

        PreviewButton.Click +=
            PreviewButton_Click;

        SaveButton.Click +=
            SaveButton_Click;

        PreviewKeyDown +=
            CustomOverlayEditor_PreviewKeyDown;

        PreviewMouseWheel +=
            CustomOverlayEditor_PreviewMouseWheel;


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


    private void CustomOverlayEditor_PreviewMouseWheel(
        object sender,
        MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
            return;

        _canvasZoomWasChangedManually =
            true;

        double step =
            e.Delta > 0
                ? 0.10
                : -0.10;

        SetCanvasZoom(
            _canvasZoom + step
        );

        e.Handled =
            true;
    }


    // ================================================================
    // DELETE ELEMENT
    // ================================================================

    private void CustomOverlayEditor_PreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Delete)
            return;

        // Laisse la touche Suppr fonctionner normalement
        // quand l'utilisateur édite un champ texte.
        if (Keyboard.FocusedElement is TextBox)
            return;

        if (_selectedElement == null)
            return;

        DeleteSelectedElement();
        e.Handled = true;
    }


    private void DeleteSelectedElement()
    {
        if (_selectedElement == null)
            return;

        CustomOverlayElement elementToDelete =
            _selectedElement;

        _selectedElement = null;
        _selectedVisual = null;

        _overlay.Elements.Remove(
            elementToDelete
        );

        RenderOverlay();
        RenderLayers();
        UpdatePropertiesPanel();
    }


    // ================================================================
    // SAVE / PREVIEW
    // ================================================================

    private void PreviewButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        CustomOverlayPreviewWindow preview =
            new(_overlay)
            {
                Owner = this
            };

        preview.ShowDialog();
    }


    private void SaveButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_overlay.Name) ||
            _overlay.Name == "New Overlay")
        {
            string? name =
                ShowOverlayNameDialog(
                    _overlay.Name == "New Overlay"
                        ? ""
                        : _overlay.Name
                );

            if (name == null)
                return;

            _overlay.Name = name;
        }

        try
        {
            string path =
                _overlayService.Save(
                    _overlay
                );

            MessageBox.Show(
                this,
                $"Overlay saved successfully.\n\n{path}",
                "DashRL",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"Unable to save the overlay.\n\n{exception.Message}",
                "DashRL",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }


    private string? ShowOverlayNameDialog(
        string currentName)
    {
        Window dialog =
            new()
            {
                Title = "Save overlay",
                Width = 390,
                Height = 180,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = GetBrush("#0D0F14"),
                Foreground = Brushes.White
            };


        Grid grid =
            new()
            {
                Margin = new Thickness(20)
            };

        grid.RowDefinitions.Add(
            new RowDefinition
            {
                Height = GridLength.Auto
            }
        );

        grid.RowDefinitions.Add(
            new RowDefinition
            {
                Height = GridLength.Auto
            }
        );

        grid.RowDefinitions.Add(
            new RowDefinition
            {
                Height = GridLength.Auto
            }
        );


        TextBlock label =
            new()
            {
                Text = "Overlay name",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            };


        TextBox nameTextBox =
            new()
            {
                Text = currentName,
                Height = 34,
                Padding = new Thickness(10, 6, 10, 6),
                Background = GetBrush("#11141B"),
                Foreground = Brushes.White,
                BorderBrush = GetBrush("#292E39"),
                BorderThickness = new Thickness(1)
            };

        Grid.SetRow(
            nameTextBox,
            1
        );


        StackPanel buttons =
            new()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 14, 0, 0)
            };

        Grid.SetRow(
            buttons,
            2
        );


        Button cancelButton =
            new()
            {
                Content = "Cancel",
                Width = 82,
                Height = 32,
                Margin = new Thickness(0, 0, 8, 0),
                IsCancel = true
            };


        Button saveButton =
            new()
            {
                Content = "Save",
                Width = 82,
                Height = 32,
                IsDefault = true
            };


        saveButton.Click +=
            (_, _) =>
            {
                string name =
                    nameTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show(
                        dialog,
                        "Enter a name for the overlay.",
                        "DashRL",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );

                    return;
                }

                dialog.Tag = name;
                dialog.DialogResult = true;
            };


        buttons.Children.Add(
            cancelButton
        );

        buttons.Children.Add(
            saveButton
        );

        grid.Children.Add(
            label
        );

        grid.Children.Add(
            nameTextBox
        );

        grid.Children.Add(
            buttons
        );

        dialog.Content = grid;

        dialog.Loaded +=
            (_, _) =>
            {
                nameTextBox.Focus();
                nameTextBox.SelectAll();
            };


        bool? result =
            dialog.ShowDialog();

        return result == true
            ? dialog.Tag as string
            : null;
    }


    // ================================================================
    // LOADING
    // ================================================================

    private void CustomOverlayEditor_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        _overlay.Width =
            Math.Clamp(
                _overlay.Width,
                MinCanvasWidth,
                MaxCanvasWidth
            );

        _overlay.Height =
            Math.Clamp(
                _overlay.Height,
                MinCanvasHeight,
                MaxCanvasHeight
            );

        OverlayCanvas.Width =
            _overlay.Width;

        OverlayCanvas.Height =
            _overlay.Height;

        CanvasWidthTextBox.Text =
            Math.Round(_overlay.Width).ToString();

        CanvasHeightTextBox.Text =
            Math.Round(_overlay.Height).ToString();

        UpdateCanvasSizeText();

        RenderOverlay();
        RenderLayers();

        Dispatcher.BeginInvoke(
            () => FitCanvasToViewport()
        );
    }


    // ================================================================
    // CANVAS SIZE
    // ================================================================

    private const double MinCanvasWidth = 50;
    private const double MinCanvasHeight = 50;
    private const double MaxCanvasWidth = 400;
    private const double MaxCanvasHeight = 400;

    private void CanvasSizeTextBox_KeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        ApplyCanvasSize();
        Keyboard.ClearFocus();
        e.Handled = true;
    }

    private void CanvasSizeTextBox_LostFocus(
        object sender,
        RoutedEventArgs e)
    {
        ApplyCanvasSize();
    }

    private void ApplyCanvasSize()
    {
        double width = _overlay.Width;
        double height = _overlay.Height;

        if (double.TryParse(CanvasWidthTextBox.Text, out double parsedWidth))
            width = Math.Clamp(parsedWidth, MinCanvasWidth, MaxCanvasWidth);

        if (double.TryParse(CanvasHeightTextBox.Text, out double parsedHeight))
            height = Math.Clamp(parsedHeight, MinCanvasHeight, MaxCanvasHeight);

        _overlay.Width = width;
        _overlay.Height = height;

        OverlayCanvas.Width = width;
        OverlayCanvas.Height = height;

        // Si le canvas devient plus petit, garde chaque élément à l'intérieur.
        foreach (CustomOverlayElement element in _overlay.Elements)
        {
            element.Width = Math.Min(element.Width, width);
            element.Height = Math.Min(element.Height, height);

            element.X = Math.Clamp(
                element.X,
                0,
                Math.Max(0, width - element.Width));

            element.Y = Math.Clamp(
                element.Y,
                0,
                Math.Max(0, height - element.Height));
        }

        CanvasWidthTextBox.Text = Math.Round(width).ToString();
        CanvasHeightTextBox.Text = Math.Round(height).ToString();

        UpdateCanvasSizeText();

        RenderOverlay();
        RenderLayers();

        if (_selectedElement != null)
            SelectElement(_selectedElement);
        else
            UpdatePropertiesPanel();

        // Comme Canva : changer le format ne redimensionne jamais la fenêtre.
        // On recalcule simplement le zoom pour que la page reste visible.
        Dispatcher.BeginInvoke(
            () => FitCanvasToViewport()
        );
    }


    // ================================================================
    // CANVAS WORKSPACE / ZOOM
    // ================================================================

    private void CanvasViewport_SizeChanged(
        object sender,
        SizeChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        // Tant que l'utilisateur n'a pas choisi son propre zoom,
        // le canvas reste automatiquement ajusté à la zone de travail.
        if (!_canvasZoomWasChangedManually)
        {
            Dispatcher.BeginInvoke(
                () => FitCanvasToViewport()
            );
        }
    }


    private void CanvasZoomSlider_ValueChanged(
        object sender,
        RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded || _isUpdatingCanvasZoom)
            return;

        _canvasZoomWasChangedManually = true;

        SetCanvasZoom(
            e.NewValue / 100.0,
            updateSlider: false
        );
    }


    private void CanvasZoomOutButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _canvasZoomWasChangedManually = true;

        SetCanvasZoom(
            _canvasZoom - 0.10
        );
    }


    private void CanvasZoomInButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _canvasZoomWasChangedManually = true;

        SetCanvasZoom(
            _canvasZoom + 0.10
        );
    }


    private void CanvasFitButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _canvasZoomWasChangedManually = false;
        FitCanvasToViewport();
    }


    private void FitCanvasToViewport()
    {
        if (CanvasViewport == null ||
            OverlayCanvas == null ||
            CanvasScaleTransform == null)
        {
            return;
        }

        double availableWidth =
            Math.Max(
                1,
                CanvasViewport.ActualWidth -
                CanvasWorkspacePadding
            );

        double availableHeight =
            Math.Max(
                1,
                CanvasViewport.ActualHeight -
                CanvasWorkspacePadding
            );

        double widthScale =
            availableWidth /
            Math.Max(
                1,
                _overlay.Width
            );

        double heightScale =
            availableHeight /
            Math.Max(
                1,
                _overlay.Height
            );

        double fitZoom =
            Math.Min(
                widthScale,
                heightScale
            );

        // Un petit canvas peut être agrandi comme sur Canva,
        // mais on garde un plafond raisonnable à 200 %.
        fitZoom =
            Math.Clamp(
                fitZoom,
                MinCanvasZoom,
                MaxCanvasZoom
            );

        SetCanvasZoom(
            fitZoom
        );
    }


    private void SetCanvasZoom(
        double zoom,
        bool updateSlider = true)
    {
        zoom =
            Math.Clamp(
                zoom,
                MinCanvasZoom,
                MaxCanvasZoom
            );

        _canvasZoom =
            zoom;

        CanvasScaleTransform.ScaleX =
            zoom;

        CanvasScaleTransform.ScaleY =
            zoom;

        int zoomPercent =
            (int)Math.Round(
                zoom * 100
            );

        if (CanvasZoomText != null)
        {
            CanvasZoomText.Text =
                $"{zoomPercent}%";
        }

        if (updateSlider &&
            CanvasZoomSlider != null)
        {
            _isUpdatingCanvasZoom =
                true;

            try
            {
                CanvasZoomSlider.Value =
                    zoomPercent;
            }
            finally
            {
                _isUpdatingCanvasZoom =
                    false;
            }
        }

        UpdateCanvasSizeText();
    }


    private void UpdateCanvasSizeText()
    {
        if (CanvasSizeText == null)
            return;

        CanvasSizeText.Text =
            $"{Math.Round(_overlay.Width)} × {Math.Round(_overlay.Height)}";
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
RenderLayers();

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
// LAYERS
// ================================================================

private void RenderLayers()
{
    LayersPanel.Children.Clear();

    for (int i = _overlay.Elements.Count - 1;
         i >= 0;
         i--)
    {
        CustomOverlayElement element =
            _overlay.Elements[i];

        Button layerButton =
            new()
            {
                Tag = element,
                Style =
                    (Style)FindResource(
                        "LayerButtonStyle"
                    ),
                ClipToBounds = false
            };

        StackPanel content =
            new()
            {
                Orientation =
                    Orientation.Horizontal
            };

        TextBlock handle =
            new()
            {
                Text = "☰",
                Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(
                            115,
                            122,
                            137
                        )
                    ),
                FontSize = 15,
                Margin =
                    new Thickness(
                        0,
                        0,
                        10,
                        0
                    ),
                VerticalAlignment =
                    VerticalAlignment.Center
            };

        TextBlock name =
            new()
            {
                Text =
                    GetLayerName(
                        element
                    ),
                Foreground =
                    Brushes.White,
                FontSize = 12,
                FontWeight =
                    FontWeights.SemiBold,
                VerticalAlignment =
                    VerticalAlignment.Center
            };

        content.Children.Add(handle);
        content.Children.Add(name);

        layerButton.Content =
            content;

        layerButton.PreviewMouseLeftButtonDown +=
            LayerButton_PreviewMouseLeftButtonDown;

        layerButton.PreviewMouseMove +=
            LayerButton_PreviewMouseMove;

        layerButton.PreviewMouseLeftButtonUp +=
            LayerButton_PreviewMouseLeftButtonUp;

        Grid layerWrapper =
            new()
            {
                Tag = element,
                Margin =
                    new Thickness(
                        0,
                        8,
                        8,
                        0
                    ),
                ClipToBounds = false
            };

        layerWrapper.Children.Add(
            layerButton
        );

        Border deleteButton =
            new()
            {
                Tag = element,
                Width = 20,
                Height = 20,
                CornerRadius =
                    new CornerRadius(10),
                Background =
                    new SolidColorBrush(
                        Color.FromRgb(
                            239,
                            35,
                            60
                        )
                    ),
                BorderBrush =
                    Brushes.White,
                BorderThickness =
                    new Thickness(1.5),
                Cursor =
                    Cursors.Hand,
                HorizontalAlignment =
                    HorizontalAlignment.Right,
                VerticalAlignment =
                    VerticalAlignment.Top,
                Margin =
                    new Thickness(
                        0,
                        -8,
                        -8,
                        0
                    ),
                Child =
                    new TextBlock
                    {
                        Text = "×",
                        Foreground =
                            Brushes.White,
                        FontSize = 15,
                        FontWeight =
                            FontWeights.Bold,
                        HorizontalAlignment =
                            HorizontalAlignment.Center,
                        VerticalAlignment =
                            VerticalAlignment.Center,
                        TextAlignment =
                            TextAlignment.Center,
                        Margin =
                            new Thickness(
                                0,
                                -2,
                                0,
                                0
                            )
                    }
            };

        Panel.SetZIndex(
            deleteButton,
            10
        );

        deleteButton.PreviewMouseLeftButtonDown +=
            LayerDeleteButton_PreviewMouseLeftButtonDown;

        layerWrapper.Children.Add(
            deleteButton
        );

        LayersPanel.Children.Add(
            layerWrapper
        );
    }

    UpdateLayerSelection();
}

private void LayerDeleteButton_PreviewMouseLeftButtonDown(
    object sender,
    MouseButtonEventArgs e)
{
    if (sender is not Border deleteButton ||
        deleteButton.Tag is not CustomOverlayElement element)
    {
        return;
    }

    EndLayerDrag();

    if (ReferenceEquals(_selectedElement, element))
    {
        _selectedElement = null;
        _selectedVisual = null;
    }

    _overlay.Elements.Remove(element);

    RenderOverlay();
    RenderLayers();
    UpdatePropertiesPanel();

    e.Handled = true;
}


private static string GetLayerName(
    CustomOverlayElement element)
{
    return element.Type switch
    {
        CustomOverlayElementType.Wins =>
            "Wins",

        CustomOverlayElementType.Losses =>
            "Losses",

        CustomOverlayElementType.Streak =>
            "Streak",

        CustomOverlayElementType.Text =>
            string.IsNullOrWhiteSpace(
                element.Text
            )
                ? "Text"
                : element.Text,

        CustomOverlayElementType.Container =>
            "Container",

        _ =>
            "Element"
    };
}


private void LayerButton_PreviewMouseLeftButtonDown(
    object sender,
    MouseButtonEventArgs e)
{
    if (sender is not Button button)
        return;


    _draggedLayerButton =
        button;

    _layerDragStart =
        e.GetPosition(
            LayersPanel
        );

    _layerDragMouseOffset =
        e.GetPosition(
            button
        );

    _isLayerDragging =
        false;

    _layerPendingInsertIndex =
        -1;
}


private void LayerButton_PreviewMouseMove(
    object sender,
    MouseEventArgs e)
{
    if (_draggedLayerButton == null ||
        e.LeftButton != MouseButtonState.Pressed)
    {
        return;
    }


    Point currentPosition =
        e.GetPosition(
            LayersPanel
        );


    if (!_isLayerDragging)
    {
        double distance =
            Math.Abs(
                currentPosition.X -
                _layerDragStart.X
            );


        if (distance <
            SystemParameters.MinimumHorizontalDragDistance)
        {
            return;
        }


        _isLayerDragging =
            true;

        StartLayerDragVisual();

        _draggedLayerButton.Opacity =
            0.20;

        _draggedLayerButton.CaptureMouse();
    }


    UpdateLayerDragVisual(
        e.GetPosition(this)
    );


    if (_draggedLayerButton.Tag
        is not CustomOverlayElement draggedElement)
    {
        return;
    }


    // Pendant le drag on ne recrée PAS les boutons.
    // On calcule seulement la future position.
    // Cela évite de perdre la capture souris, notamment
    // avec le dernier calque situé tout à droite.

    List<CustomOverlayElement> displayOrder =
        new();

    for (int i = _overlay.Elements.Count - 1;
         i >= 0;
         i--)
    {
        CustomOverlayElement element =
            _overlay.Elements[i];

        if (!ReferenceEquals(
                element,
                draggedElement))
        {
            displayOrder.Add(
                element
            );
        }
    }


    int insertIndex =
        displayOrder.Count;


    foreach (UIElement child
             in LayersPanel.Children)
    {
        Button? button =
            child is Button directButton
                ? directButton
                : (child as Panel)?
                    .Children
                    .OfType<Button>()
                    .FirstOrDefault();

        if (button == null ||
            button.Tag is not CustomOverlayElement element ||
            ReferenceEquals(
                element,
                draggedElement
            ))
        {
            continue;
        }


        Point buttonPosition =
            button.TranslatePoint(
                new Point(0, 0),
                LayersPanel
            );


        double buttonCenter =
            buttonPosition.X +
            button.ActualWidth / 2;


        if (currentPosition.X <
            buttonCenter)
        {
            int index =
                displayOrder.IndexOf(
                    element
                );

            if (index >= 0)
            {
                insertIndex =
                    index;

                break;
            }
        }
    }


    _layerPendingInsertIndex =
        Math.Clamp(
            insertIndex,
            0,
            displayOrder.Count
        );


    UpdateLayerInsertionVisual(
        draggedElement,
        _layerPendingInsertIndex
    );
}


private void LayerButton_PreviewMouseLeftButtonUp(
    object sender,
    MouseButtonEventArgs e)
{
    if (sender is not Button button)
        return;


    bool wasDragging =
        _isLayerDragging;


    if (wasDragging &&
        button.Tag is CustomOverlayElement draggedElement)
    {
        CommitLayerDrag(
            draggedElement
        );
    }


    EndLayerDrag();


    if (!wasDragging &&
        button.Tag is CustomOverlayElement element)
    {
        SelectElement(
            element
        );
    }


    e.Handled =
        true;
}


private void CommitLayerDrag(
    CustomOverlayElement draggedElement)
{
    List<CustomOverlayElement> displayOrder =
        new();

    for (int i = _overlay.Elements.Count - 1;
         i >= 0;
         i--)
    {
        CustomOverlayElement element =
            _overlay.Elements[i];

        if (!ReferenceEquals(
                element,
                draggedElement))
        {
            displayOrder.Add(
                element
            );
        }
    }


    int insertIndex =
        _layerPendingInsertIndex;


    if (insertIndex < 0)
    {
        Point mousePosition =
            Mouse.GetPosition(
                LayersPanel
            );

        insertIndex =
            displayOrder.Count;

        foreach (UIElement child
                 in LayersPanel.Children)
        {
            if (child is not Button button ||
                button.Tag is not CustomOverlayElement element ||
                ReferenceEquals(
                    element,
                    draggedElement
                ))
            {
                continue;
            }


            Point buttonPosition =
                button.TranslatePoint(
                    new Point(0, 0),
                    LayersPanel
                );


            if (mousePosition.X <
                buttonPosition.X +
                button.ActualWidth / 2)
            {
                int index =
                    displayOrder.IndexOf(
                        element
                    );

                if (index >= 0)
                {
                    insertIndex =
                        index;

                    break;
                }
            }
        }
    }


    insertIndex =
        Math.Clamp(
            insertIndex,
            0,
            displayOrder.Count
        );


    displayOrder.Insert(
        insertIndex,
        draggedElement
    );


    _overlay.Elements.Clear();


    for (int i = displayOrder.Count - 1;
         i >= 0;
         i--)
    {
        _overlay.Elements.Add(
            displayOrder[i]
        );
    }


    RenderOverlay();
    RenderLayers();

    SelectElement(
        draggedElement
    );
}


private void CustomOverlayEditor_PreviewMouseLeftButtonUp(
    object sender,
    MouseButtonEventArgs e)
{
    if (!_isLayerDragging)
        return;


    if (_draggedLayerButton?.Tag
        is CustomOverlayElement draggedElement)
    {
        CommitLayerDrag(
            draggedElement
        );
    }


    EndLayerDrag();

    e.Handled =
        true;
}

private void StartLayerDragVisual()
{
    if (_draggedLayerButton == null)
        return;


    _layerDragAdornerLayer =
        AdornerLayer.GetAdornerLayer(
            LayersPanel
        );


    if (_layerDragAdornerLayer == null)
        return;


    _layerDragAdorner =
        new LayerDragAdorner(
            LayersPanel,
            _draggedLayerButton
        );


    _layerDragAdornerLayer.Add(
        _layerDragAdorner
    );


    _layerInsertionAdorner =
        new LayerInsertionAdorner(
            LayersPanel
        );

    _layerDragAdornerLayer.Add(
        _layerInsertionAdorner
    );


    UpdateLayerDragVisual(
        Mouse.GetPosition(this)
    );
}


private void UpdateLayerDragVisual(
    Point mousePosition)
{
    if (_layerDragAdorner == null)
        return;


    Point layersPosition =
        LayersPanel.TranslatePoint(
            new Point(0, 0),
            this
        );


    double x =
        mousePosition.X -
        layersPosition.X -
        _layerDragMouseOffset.X;

    double y =
        mousePosition.Y -
        layersPosition.Y -
        _layerDragMouseOffset.Y;


    _layerDragAdorner.SetPosition(
        x,
        y
    );
}


private void UpdateLayerInsertionVisual(
    CustomOverlayElement draggedElement,
    int insertIndex)
{
    if (_layerInsertionAdorner == null)
        return;


    List<Button> buttons =
        new();


    foreach (UIElement child
             in LayersPanel.Children)
    {
        Button? button =
            child is Button directButton
                ? directButton
                : (child as Panel)?
                    .Children
                    .OfType<Button>()
                    .FirstOrDefault();

        if (button != null &&
            button.Tag is CustomOverlayElement element &&
            !ReferenceEquals(
                element,
                draggedElement
            ))
        {
            buttons.Add(
                button
            );
        }
    }


    double x;


    if (buttons.Count == 0)
    {
        x = 4;
    }
    else if (insertIndex <= 0)
    {
        Button first =
            buttons[0];

        Point position =
            first.TranslatePoint(
                new Point(0, 0),
                LayersPanel
            );

        x =
            position.X - 5;
    }
    else if (insertIndex >= buttons.Count)
    {
        Button last =
            buttons[
                buttons.Count - 1
            ];

        Point position =
            last.TranslatePoint(
                new Point(0, 0),
                LayersPanel
            );

        x =
            position.X +
            last.ActualWidth +
            5;
    }
    else
    {
        Button right =
            buttons[
                insertIndex
            ];

        Point position =
            right.TranslatePoint(
                new Point(0, 0),
                LayersPanel
            );

        x =
            position.X - 5;
    }


    double height =
        48;


    double y =
        Math.Max(
            0,
            (LayersPanel.ActualHeight - height) / 2
        );


    _layerInsertionAdorner.SetIndicator(
        x,
        y,
        height
    );
}


private void StopLayerDragVisual()
{
    if (_layerDragAdornerLayer != null &&
        _layerDragAdorner != null)
    {
        _layerDragAdornerLayer.Remove(
            _layerDragAdorner
        );
    }


    if (_layerDragAdornerLayer != null &&
        _layerInsertionAdorner != null)
    {
        _layerDragAdornerLayer.Remove(
            _layerInsertionAdorner
        );
    }


    _layerDragAdorner =
        null;

    _layerInsertionAdorner =
        null;

    _layerDragAdornerLayer =
        null;
}


private void EndLayerDrag()
{
    StopLayerDragVisual();


    if (_draggedLayerButton != null)
    {
        _draggedLayerButton.Opacity =
            1.0;

        if (_draggedLayerButton.IsMouseCaptured)
        {
            _draggedLayerButton.ReleaseMouseCapture();
        }
    }


    _draggedLayerButton =
        null;

    _isLayerDragging =
        false;

    _layerPendingInsertIndex =
        -1;
}

private void UpdateLayerSelection()
{
    foreach (UIElement child
             in LayersPanel.Children)
    {
        Button? button =
            child is Button directButton
                ? directButton
                : (child as Panel)?
                    .Children
                    .OfType<Button>()
                    .FirstOrDefault();

        if (button == null)
            continue;


        bool selected =
            ReferenceEquals(
                button.Tag,
                _selectedElement
            );


        button.BorderBrush =
            selected
                ? new SolidColorBrush(
                    Color.FromRgb(
                        124,
                        108,
                        255
                    )
                )
                : new SolidColorBrush(
                    Color.FromRgb(
                        41,
                        46,
                        57
                    )
                );

        button.BorderThickness =
            selected
                ? new Thickness(2)
                : new Thickness(1);
    }
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
        UpdateLayerSelection();
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
    if (_selectedElement.Type ==
    CustomOverlayElementType.Text)
{
    RenderLayers();
}
}


private void TextColorPaletteButton_Click(
    object sender,
    RoutedEventArgs e)
{
    if (sender is not Button button ||
        button.Tag is not string color)
    {
        return;
    }

    TextColorTextBox.Text =
        color;
}


private void BackgroundColorPaletteButton_Click(
    object sender,
    RoutedEventArgs e)
{
    if (sender is not Button button ||
        button.Tag is not string color)
    {
        return;
    }

    BackgroundColorTextBox.Text =
        color;
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

internal sealed class LayerInsertionAdorner : Adorner
{
    private double _x;
    private double _y;
    private double _height;


    public LayerInsertionAdorner(
        UIElement adornedElement)
        : base(adornedElement)
    {
        IsHitTestVisible =
            false;
    }


    public void SetIndicator(
        double x,
        double y,
        double height)
    {
        _x = x;
        _y = y;
        _height = height;

        InvalidateVisual();
    }


    protected override void OnRender(
        DrawingContext drawingContext)
    {
        base.OnRender(
            drawingContext
        );


        if (_height <= 0)
            return;


        Brush brush =
            new SolidColorBrush(
                Color.FromRgb(
                    124,
                    108,
                    255
                )
            );


        Pen pen =
            new(
                brush,
                4
            )
            {
                StartLineCap =
                    PenLineCap.Round,

                EndLineCap =
                    PenLineCap.Round
            };


        drawingContext.DrawLine(
            pen,
            new Point(
                _x,
                _y
            ),
            new Point(
                _x,
                _y + _height
            )
        );
    }
}


internal sealed class LayerDragAdorner : Adorner
{
    private readonly VisualBrush _visualBrush;
    private readonly double _width;
    private readonly double _height;

    private double _x;
    private double _y;


    public LayerDragAdorner(
        UIElement adornedElement,
        FrameworkElement source)
        : base(adornedElement)
    {
        IsHitTestVisible =
            false;

        _width =
            source.ActualWidth;

        _height =
            source.ActualHeight;

        _visualBrush =
            new VisualBrush(
                source
            )
            {
                Opacity = 0.92,
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            };
    }


    public void SetPosition(
        double x,
        double y)
    {
        _x = x;
        _y = y;

        InvalidateVisual();
    }


    protected override void OnRender(
        DrawingContext drawingContext)
    {
        base.OnRender(
            drawingContext
        );


        Rect rect =
            new(
                new Point(
                    _x,
                    _y
                ),
                new Size(
                    _width,
                    _height
                )
            );


        drawingContext.DrawRoundedRectangle(
            _visualBrush,
            null,
            rect,
            8,
            8
        );
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