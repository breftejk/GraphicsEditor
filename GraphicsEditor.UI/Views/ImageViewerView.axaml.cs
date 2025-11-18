using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Media;
using Avalonia.Threading;
using GraphicsEditor.UI.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GraphicsEditor.UI.Views;

public partial class ImageViewerView : UserControl
{
    private ImageViewerViewModel? ViewModel => DataContext as ImageViewerViewModel;
    private double _currentZoom = 1.0;
    private ScaleTransform? _scaleTransform;
    private byte[]? _currentPixelData;
    private int _currentWidth;
    private int _currentHeight;
    private bool _isRendering = false;
    
    // Panning state
    private bool _isPanning = false;
    private Point _panStartPoint;
    private Point _scrollStartOffset;

    public ImageViewerView()
    {
        InitializeComponent();
        
        // Subscribe to DataContext changes to wire up PropertyChanged
        this.DataContextChanged += OnDataContextChanged;
        
        // Ensure the ImageCanvas has a ScaleTransform for zooming
        var canvasInit = this.FindControl<Canvas>("ImageCanvas");
        if (canvasInit != null && canvasInit.RenderTransform is not ScaleTransform)
        {
            canvasInit.RenderTransform = new ScaleTransform(_currentZoom, _currentZoom);
        }
        
        // Add pinch gesture support for touchpad zoom
        var canvas = this.FindControl<Canvas>("ImageCanvas");
        if (canvas != null)
        {
            // Enable gesture recognition
            Gestures.PinchEvent.AddClassHandler<Canvas>(OnPinchGesture);
        }
        
        // Subscribe to scroll changes to update RGB overlay when panning
        var scrollViewer = this.FindControl<ScrollViewer>("ImageScrollViewer");
        if (scrollViewer != null)
        {
            scrollViewer.PropertyChanged += ScrollViewer_PropertyChanged;
        }
    }

    private void ScrollViewer_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // Update RGB overlay when scroll position changes (user is panning)
        if (e.Property.Name == nameof(ScrollViewer.Offset))
        {
            var canvas = this.FindControl<Canvas>("ImageCanvas");
            if (canvas != null && _currentPixelData != null && _currentWidth > 0 && _currentHeight > 0)
            {
                // Only update if we're zoomed in enough to show RGB values
                double pixelSizeOnScreen = _currentZoom;
                if (pixelSizeOnScreen >= 30.0)
                {
                    UpdateRgbOverlay(canvas);
                }
            }
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (ViewModel != null)
        {
            // Hook up to zoom commands
            ViewModel.PropertyChanged += (s, args) =>
            {
                System.Diagnostics.Debug.WriteLine($"PropertyChanged: {args.PropertyName}");
                
                if (args.PropertyName == nameof(ViewModel.ZoomLevel))
                {
                    ApplyZoom(ViewModel.ZoomLevel);
                }
                else if (args.PropertyName == nameof(ViewModel.ImageData) && ViewModel.ImageData != null)
                {
                    // Preserve scroll offset around re-render
                    var scrollViewer = this.FindControl<ScrollViewer>("ImageScrollViewer");
                    Vector? oldOffset = null;
                    if (scrollViewer != null)
                    {
                        oldOffset = scrollViewer.Offset;
                    }
                    
                    // Re-render when ImageData changes (e.g., processing)
                    System.Diagnostics.Debug.WriteLine($"ImageData changed, rendering: {ViewModel.ImageWidth}x{ViewModel.ImageHeight}");
                    RenderImage(ViewModel.ImageData, ViewModel.ImageWidth, ViewModel.ImageHeight);
                    
                    // Restore previous offset if available (clamped to new extent)
                    if (scrollViewer != null && oldOffset.HasValue)
                    {
                        var extent = scrollViewer.Extent;
                        var viewport = scrollViewer.Viewport;
                        double newX = Math.Max(0, Math.Min(oldOffset.Value.X, Math.Max(0, extent.Width - viewport.Width)));
                        double newY = Math.Max(0, Math.Min(oldOffset.Value.Y, Math.Max(0, extent.Height - viewport.Height)));
                        scrollViewer.Offset = new Vector(newX, newY);
                    }
                    
                    // Re-apply current zoom to ensure transform matches new content
                    ApplyZoom(ViewModel.ZoomLevel);
                }
                else if (args.PropertyName == nameof(ViewModel.HistogramData))
                {
                    // Update histogram visualization
                    System.Diagnostics.Debug.WriteLine("HistogramData changed, drawing histogram");
                    DrawHistogram();
                }
            };
        }
    }

    private void DrawHistogram()
    {
        var canvas = this.FindControl<Canvas>("HistogramCanvas");
        if (canvas == null || ViewModel?.HistogramData == null)
        {
            System.Diagnostics.Debug.WriteLine("DrawHistogram: canvas or data is null");
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                canvas.Children.Clear();

                int[] histogram = ViewModel.HistogramData;
                double canvasWidth = canvas.Bounds.Width;
                double canvasHeight = canvas.Bounds.Height;

                System.Diagnostics.Debug.WriteLine($"Drawing histogram: canvas size {canvasWidth}x{canvasHeight}");

                if (canvasWidth <= 0 || canvasHeight <= 0)
                {
                    // Canvas not yet measured, try again after layout
                    System.Diagnostics.Debug.WriteLine("Canvas not measured yet, scheduling redraw");
                    canvas.LayoutUpdated += OnHistogramCanvasLayoutUpdated;
                    return;
                }

                // Find max value for scaling
                int maxValue = histogram.Max();
                if (maxValue == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Histogram max value is 0");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Histogram max value: {maxValue}");

                // Draw bars
                double barWidth = canvasWidth / 256.0;
                
                for (int i = 0; i < 256; i++)
                {
                    double barHeight = (histogram[i] / (double)maxValue) * (canvasHeight - 2);
                    
                    if (barHeight > 0)
                    {
                        var rect = new Avalonia.Controls.Shapes.Rectangle
                        {
                            Width = Math.Max(1, barWidth),
                            Height = barHeight,
                            Fill = new SolidColorBrush(Color.FromRgb((byte)i, (byte)i, (byte)i))
                        };

                        Canvas.SetLeft(rect, i * barWidth);
                        Canvas.SetTop(rect, canvasHeight - barHeight);

                        canvas.Children.Add(rect);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Histogram drawn: {canvas.Children.Count} bars");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error drawing histogram: {ex.Message}");
            }
        });
    }

    private void OnHistogramCanvasLayoutUpdated(object? sender, EventArgs e)
    {
        var canvas = sender as Canvas;
        if (canvas != null)
        {
            canvas.LayoutUpdated -= OnHistogramCanvasLayoutUpdated;
            System.Diagnostics.Debug.WriteLine("Canvas layout updated, redrawing histogram");
            DrawHistogram();
        }
    }

    private void ApplyZoom(double zoom)
    {
        var canvas = this.FindControl<Canvas>("ImageCanvas");
        var scrollViewer = this.FindControl<ScrollViewer>("ImageScrollViewer");
        
        if (canvas?.RenderTransform is ScaleTransform transform && scrollViewer != null)
        {
            // Calculate the center point of the current viewport in image coordinates
            double oldZoom = _currentZoom;
            double viewportCenterX = (scrollViewer.Offset.X + scrollViewer.Viewport.Width / 2) / oldZoom;
            double viewportCenterY = (scrollViewer.Offset.Y + scrollViewer.Viewport.Height / 2) / oldZoom;
            
            // Apply new zoom
            transform.ScaleX = zoom;
            transform.ScaleY = zoom;
            _currentZoom = zoom;
            
            // Update container size to make ScrollViewer aware of zoomed size
            var container = this.FindControl<Border>("CanvasContainer");
            if (container != null && canvas.Width > 0 && canvas.Height > 0)
            {
                container.Width = canvas.Width * zoom;
                container.Height = canvas.Height * zoom;
            }
            
            // Adjust scroll position to keep the same center point
            double newScrollX = viewportCenterX * zoom - scrollViewer.Viewport.Width / 2;
            double newScrollY = viewportCenterY * zoom - scrollViewer.Viewport.Height / 2;
            
            // Clamp to valid scroll range
            newScrollX = Math.Max(0, Math.Min(newScrollX, scrollViewer.Extent.Width - scrollViewer.Viewport.Width));
            newScrollY = Math.Max(0, Math.Min(newScrollY, scrollViewer.Extent.Height - scrollViewer.Viewport.Height));
            
            scrollViewer.Offset = new Vector(newScrollX, newScrollY);
            
            System.Diagnostics.Debug.WriteLine($"Zoom applied: {zoom}x, center: ({viewportCenterX:F1}, {viewportCenterY:F1}), scroll: ({newScrollX:F1}, {newScrollY:F1})");
            
            // Update RGB overlay without full re-render
            if (!_isRendering && _currentPixelData != null && _currentWidth > 0 && _currentHeight > 0)
            {
                UpdateRgbOverlay(canvas);
            }
        }
    }

    private void UpdateRgbOverlay(Canvas canvas)
    {
        System.Diagnostics.Debug.WriteLine($"UpdateRgbOverlay called: _currentPixelData={_currentPixelData != null}, width={_currentWidth}, height={_currentHeight}, zoom={_currentZoom}");
        
        if (_currentPixelData == null || _currentWidth == 0 || _currentHeight == 0)
        {
            System.Diagnostics.Debug.WriteLine("UpdateRgbOverlay: No data to display");
            return;
        }
            
        // Remove old RGB overlays (keep only the Image control)
        var childrenToRemove = canvas.Children
            .Where(c => c is TextBlock || c is Border)
            .ToList();
        System.Diagnostics.Debug.WriteLine($"Removing {childrenToRemove.Count} old overlay elements");
        foreach (var child in childrenToRemove)
        {
            canvas.Children.Remove(child);
        }
        
        // Add new RGB overlay based on current zoom
        System.Diagnostics.Debug.WriteLine($"Calling AddRgbValueOverlay with zoom={_currentZoom}");
        AddRgbValueOverlay(canvas, _currentPixelData, _currentWidth, _currentHeight);
    }

    private async void LoadImageButton_Click(object? sender, RoutedEventArgs e)
    {
        await LoadImageAsync();
    }

    private async void SaveImageButton_Click(object? sender, RoutedEventArgs e)
    {
        await SaveImageAsJpegAsync();
    }

    private async void SaveImageAsPngButton_Click(object? sender, RoutedEventArgs e)
    {
        await SaveImageAsPngAsync();
    }

    private async Task SaveImageAsJpegAsync()
    {
        if (ViewModel?.ImageData == null)
        {
            if (ViewModel != null)
            {
                ViewModel.StatusMessage = "No image to save";
            }
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save JPEG Image",
            DefaultExtension = "jpg",
            SuggestedFileName = "output.jpg",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("JPEG Image")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg" }
                }
            }
        });

        if (file != null)
        {
            try
            {
                var filePath = file.Path.LocalPath;
                var jpegWriter = new IO.JpegWriter();
                jpegWriter.Write(filePath, ViewModel.ImageData, ViewModel.ImageWidth, ViewModel.ImageHeight, ViewModel.JpegQuality);
                ViewModel.StatusMessage = $"✅ Saved JPEG (quality {ViewModel.JpegQuality}%) to: {System.IO.Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                ViewModel.StatusMessage = $"❌ Error saving JPEG: {ex.Message}";
            }
        }
    }

    private async Task SaveImageAsPngAsync()
    {
        if (ViewModel?.ImageData == null)
        {
            if (ViewModel != null)
            {
                ViewModel.StatusMessage = "No image to save";
            }
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save PNG Image",
            DefaultExtension = "png",
            SuggestedFileName = "output.png",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("PNG Image")
                {
                    Patterns = new[] { "*.png" }
                }
            }
        });

        if (file != null)
        {
            try
            {
                var filePath = file.Path.LocalPath;
                var pngWriter = new IO.PngWriter();
                pngWriter.WriteRgb(filePath, ViewModel.ImageData, ViewModel.ImageWidth, ViewModel.ImageHeight);
                ViewModel.StatusMessage = $"✅ Saved PNG to: {System.IO.Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                ViewModel.StatusMessage = $"❌ Error saving PNG: {ex.Message}";
            }
        }
    }

    private async Task LoadImageAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Image File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Image Files")
                {
                    Patterns = new[] { "*.ppm", "*.jpg", "*.jpeg", "*.png" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        if (files.Count > 0 && ViewModel != null)
        {
            var filePath = files[0].Path.LocalPath;
            
            // Use ViewModel's async method to load image in background
            await ViewModel.LoadImageFromPathAsync(filePath);
            
            // After ViewModel loads the data, render it
            if (ViewModel.ImageData != null && ViewModel.ImageWidth > 0 && ViewModel.ImageHeight > 0)
            {
                // Reset zoom to 1.0 when loading a NEW image
                if (ViewModel != null)
                {
                    ViewModel.ZoomLevel = 1.0;
                    _currentZoom = 1.0;
                }
                
                RenderImage(ViewModel.ImageData, ViewModel.ImageWidth, ViewModel.ImageHeight);
            }
        }
    }

    private void RenderImage(byte[] pixelData, int width, int height)
    {
        System.Diagnostics.Debug.WriteLine($"RenderImage called: {width}x{height}, _isRendering={_isRendering}");
        
        if (_isRendering) 
        {
            System.Diagnostics.Debug.WriteLine("RenderImage: Already rendering, skipping");
            return; // Prevent recursive rendering
        }
        
        // Validate dimensions
        if (width <= 0 || height <= 0)
        {
            System.Diagnostics.Debug.WriteLine($"RenderImage: Invalid dimensions {width}x{height}, skipping");
            return;
        }
        
        if (pixelData == null || pixelData.Length < width * height * 3)
        {
            System.Diagnostics.Debug.WriteLine($"RenderImage: Invalid pixel data (length={pixelData?.Length}, expected={width * height * 3}), skipping");
            return;
        }
        
        try
        {
            _isRendering = true;
            
            System.Diagnostics.Debug.WriteLine($"RenderImage executing: {width}x{height}, data length: {pixelData.Length}");
            
            // Store current image data for re-rendering when zoom changes
            _currentPixelData = pixelData;
            _currentWidth = width;
            _currentHeight = height;
            
            // Create a WriteableBitmap and copy pixel data
            var bitmap = new WriteableBitmap(
                new PixelSize(width, height),
                new Vector(96, 96),
                PixelFormat.Rgba8888,
                AlphaFormat.Opaque
            );

        using (var buffer = bitmap.Lock())
        {
            unsafe
            {
                var ptr = (byte*)buffer.Address;
                
                // Convert RGB to RGBA
                for (int i = 0; i < width * height; i++)
                {
                    int srcIndex = i * 3;  // RGB
                    int dstIndex = i * 4;  // RGBA
                    
                    ptr[dstIndex + 0] = pixelData[srcIndex + 0]; // R
                    ptr[dstIndex + 1] = pixelData[srcIndex + 1]; // G
                    ptr[dstIndex + 2] = pixelData[srcIndex + 2]; // B
                    ptr[dstIndex + 3] = 255;                     // A (opaque)
                }
            }
        }

        // Display the bitmap on canvas
        var canvas = this.FindControl<Canvas>("ImageCanvas");
        var placeholder = this.FindControl<TextBlock>("PlaceholderText");
        
        System.Diagnostics.Debug.WriteLine($"Canvas found: {canvas != null}");
        
        if (canvas != null)
        {
            // Hide placeholder
            if (placeholder != null)
            {
                placeholder.IsVisible = false;
            }
            
            canvas.Children.Clear();
            canvas.Width = width;
            canvas.Height = height;
            
            var image = new Avalonia.Controls.Image
            {
                Source = bitmap,
                Width = width,
                Height = height,
                Stretch = Stretch.None
            };
            
            Canvas.SetLeft(image, 0);
            Canvas.SetTop(image, 0);
            
            canvas.Children.Add(image);
            
            // Add RGB values overlay if pixels are large enough
            AddRgbValueOverlay(canvas, pixelData, width, height);
            
            System.Diagnostics.Debug.WriteLine($"Image added to canvas. Canvas children count: {canvas.Children.Count}");
            
            // PRESERVE current zoom level - don't reset!
            // The zoom is already set in _currentZoom and ApplyZoom will handle it
            System.Diagnostics.Debug.WriteLine($"Preserving zoom level: {_currentZoom}x");
        }
        }
        finally
        {
            _isRendering = false;
        }
    }

    private void ImageCanvas_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var canvas = sender as Canvas;
        if (canvas == null) return;
        
        var properties = e.GetCurrentPoint(canvas).Properties;
        
        // Check if left mouse button or primary touch
        if (properties.IsLeftButtonPressed)
        {
            // Start panning
            var scrollViewer = this.FindControl<ScrollViewer>("ImageScrollViewer");
            if (scrollViewer != null)
            {
                _isPanning = true;
                _panStartPoint = e.GetPosition(scrollViewer);
                _scrollStartOffset = new Point(scrollViewer.Offset.X, scrollViewer.Offset.Y);
                
                // Capture pointer so we get events even if cursor leaves canvas
                e.Pointer.Capture(canvas);
                
                System.Diagnostics.Debug.WriteLine($"Pan started at {_panStartPoint}");
            }
        }
    }

    private void ImageCanvas_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPanning) return;
        
        var scrollViewer = this.FindControl<ScrollViewer>("ImageScrollViewer");
        if (scrollViewer == null) return;
        
        // Calculate delta from start position
        var currentPoint = e.GetPosition(scrollViewer);
        var delta = currentPoint - _panStartPoint;
        
        // Update scroll position (subtract delta because we're dragging the content)
        scrollViewer.Offset = new Vector(
            _scrollStartOffset.X - delta.X,
            _scrollStartOffset.Y - delta.Y
        );
    }

    private void ImageCanvas_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var canvas = sender as Canvas;
        if (canvas == null) return;

        var scrollViewer = this.FindControl<ScrollViewer>("ImageScrollViewer");
        if (scrollViewer == null) return;

        // Check if this was a click (not much panning)
        var currentPoint = e.GetPosition(scrollViewer);
        double distance = Math.Sqrt(Math.Pow(currentPoint.X - _panStartPoint.X, 2) + Math.Pow(currentPoint.Y - _panStartPoint.Y, 2));
        bool wasClick = !_isPanning || (_isPanning && distance < 5);
        
        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            System.Diagnostics.Debug.WriteLine("Pan ended");
        }
        
        // Handle pixel inspection on click
        if (wasClick && ViewModel != null && _currentPixelData != null && _currentWidth > 0 && _currentHeight > 0)
        {
            // Get position relative to scrollviewer (viewport coordinates)
            var viewportPos = e.GetPosition(scrollViewer);
            
            // Convert viewport position to image coordinates
            // Account for scroll offset and zoom
            double imageX = (scrollViewer.Offset.X + viewportPos.X) / _currentZoom;
            double imageY = (scrollViewer.Offset.Y + viewportPos.Y) / _currentZoom;
            
            int pixelX = (int)Math.Floor(imageX);
            int pixelY = (int)Math.Floor(imageY);
            
            System.Diagnostics.Debug.WriteLine($"Click: viewport=({viewportPos.X:F1},{viewportPos.Y:F1}), scroll=({scrollViewer.Offset.X:F1},{scrollViewer.Offset.Y:F1}), zoom={_currentZoom:F2}, pixel=({pixelX},{pixelY})");
            
            // Validate coordinates
            if (pixelX >= 0 && pixelX < _currentWidth && pixelY >= 0 && pixelY < _currentHeight)
            {
                // Get pixel color
                int pixelIndex = (pixelY * _currentWidth + pixelX) * 3;
                if (pixelIndex + 2 < _currentPixelData.Length)
                {
                    byte r = _currentPixelData[pixelIndex];
                    byte g = _currentPixelData[pixelIndex + 1];
                    byte b = _currentPixelData[pixelIndex + 2];
                    
                    // Update ViewModel
                    var color = new GraphicsEditor.Core.Models.RgbColor(r, g, b);
                    ViewModel.SelectedPixelColor = color;
                    ViewModel.PixelInfo = $"Pixel ({pixelX}, {pixelY}): R={r}, G={g}, B={b}";
                    
                    System.Diagnostics.Debug.WriteLine($"Pixel clicked: ({pixelX}, {pixelY}) = RGB({r}, {g}, {b})");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Pixel coordinates out of bounds: ({pixelX}, {pixelY}), image size: {_currentWidth}x{_currentHeight}");
            }
        }
    }

    private void ImageCanvas_OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (ViewModel == null) return;

        var scrollViewer = this.FindControl<ScrollViewer>("ImageScrollViewer");
        var canvas = sender as Canvas;
        if (scrollViewer == null || canvas == null) return;

        // Get scroll delta (positive = scroll up = zoom in, negative = scroll down = zoom out)
        var delta = e.Delta.Y;
        
        // Use adaptive zoom factor based on current zoom level
        // At low zoom (e.g., 0.5x): use smaller steps (1.05) for fine control
        // At high zoom (e.g., 100x): use larger steps (1.15) for faster navigation
        double baseZoomFactor;
        if (_currentZoom < 1.0)
        {
            baseZoomFactor = 1.05; // Very small steps when zoomed out
        }
        else if (_currentZoom < 5.0)
        {
            baseZoomFactor = 1.08; // Small steps at normal zoom
        }
        else if (_currentZoom < 20.0)
        {
            baseZoomFactor = 1.10; // Medium steps
        }
        else
        {
            baseZoomFactor = 1.12; // Larger steps at high zoom
        }
        
        // Get mouse position relative to ScrollViewer
        var mousePos = e.GetPosition(scrollViewer);
        
        // Calculate mouse position in image coordinates (before zoom)
        double oldZoom = _currentZoom;
        double mouseImageX = (scrollViewer.Offset.X + mousePos.X) / oldZoom;
        double mouseImageY = (scrollViewer.Offset.Y + mousePos.Y) / oldZoom;
        
        double newZoom = oldZoom;
        
        if (delta > 0)
        {
            // Zoom in
            newZoom = oldZoom * baseZoomFactor;
            newZoom = Math.Min(newZoom, 500.0);
        }
        else if (delta < 0)
        {
            // Zoom out
            newZoom = oldZoom / baseZoomFactor;
            newZoom = Math.Max(newZoom, 0.1);
        }
        
        // Update ViewModel which will trigger ApplyZoom, but we need to adjust scroll after
        ViewModel.ZoomLevel = newZoom;
        
        // After zoom is applied, adjust scroll so mouse position stays at same image point
        // This needs to happen after the zoom transform is applied
        Dispatcher.UIThread.Post(() =>
        {
            double newScrollX = mouseImageX * newZoom - mousePos.X;
            double newScrollY = mouseImageY * newZoom - mousePos.Y;
            
            // Clamp to valid range
            newScrollX = Math.Max(0, Math.Min(newScrollX, scrollViewer.Extent.Width - scrollViewer.Viewport.Width));
            newScrollY = Math.Max(0, Math.Min(newScrollY, scrollViewer.Extent.Height - scrollViewer.Viewport.Height));
            
            scrollViewer.Offset = new Vector(newScrollX, newScrollY);
        }, DispatcherPriority.Background);
        
        // Mark event as handled so it doesn't scroll the ScrollViewer
        e.Handled = true;
    }

    private void OnPinchGesture(Canvas canvas, PinchEventArgs e)
    {
        if (ViewModel == null) return;
        
        // e.Scale shows the pinch scale (>1 = zoom in, <1 = zoom out)
        // Apply the scale delta to current zoom
        double scaleDelta = e.Scale;
        double newZoom = ViewModel.ZoomLevel * scaleDelta;
        
        // Clamp to valid range
        newZoom = Math.Clamp(newZoom, 0.1, 500.0);
        ViewModel.ZoomLevel = newZoom;
        
        e.Handled = true;
    }

    private void ShowRgbValuesCheckBox_Click(object? sender, RoutedEventArgs e)
    {
        // Re-render to show/hide RGB values
        var canvas = this.FindControl<Canvas>("ImageCanvas");
        if (canvas != null && _currentPixelData != null && _currentWidth > 0 && _currentHeight > 0)
        {
            System.Diagnostics.Debug.WriteLine($"ShowRgbValuesCheckBox clicked, updating overlay");
            UpdateRgbOverlay(canvas);
        }
    }

    private void AddRgbValueOverlay(Canvas canvas, byte[] pixelData, int width, int height)
    {
        // Check if RGB values should be shown
        var showRgbCheckBox = this.FindControl<CheckBox>("ShowRgbValuesCheckBox");
        bool shouldShow = showRgbCheckBox?.IsChecked == true;
        
        System.Diagnostics.Debug.WriteLine($"AddRgbValueOverlay: shouldShow={shouldShow}, zoom={_currentZoom}");
        
        if (!shouldShow) return;

        // Calculate effective pixel size after zoom (in screen pixels)
        // 1 image pixel = 1 unit, after zoom transform becomes _currentZoom pixels on screen
        double pixelSizeOnScreen = _currentZoom;
        
        System.Diagnostics.Debug.WriteLine($"Pixel size on screen: {pixelSizeOnScreen}px (zoom={_currentZoom}x)");
        
        // Only show RGB values when pixels are large enough to read text
        // At least 30px on screen to fit readable text like "255,255,255"
        double minPixelSize = 30.0;
        if (pixelSizeOnScreen < minPixelSize)
        {
            System.Diagnostics.Debug.WriteLine($"Pixel size {pixelSizeOnScreen}px too small, minimum is {minPixelSize}px");
            return;
        }

        // Calculate font size - IMPORTANT: fontSize is in IMAGE coordinates, will be scaled by zoom
        // Target: readable text that fits within a pixel
        // For 30px pixel: use ~8px font on screen
        // For 100px pixel: use ~12px font on screen
        double targetScreenFontSize = Math.Max(7, Math.Min(14, pixelSizeOnScreen / 3.5));
        double fontSize = targetScreenFontSize / _currentZoom;
        
        System.Diagnostics.Debug.WriteLine($"Font size: {fontSize} image units (= {targetScreenFontSize:F1}px on screen)");
        
        // To avoid performance issues, limit the number of pixels we annotate
        // Only show labels for pixels visible in the viewport
        var scrollViewer = this.FindControl<ScrollViewer>("ImageScrollViewer");
        int startX = 0, startY = 0, endX = width, endY = height;
        
        if (scrollViewer != null)
        {
            // Calculate visible region based on scroll position and viewport size
            double scrollX = scrollViewer.Offset.X;
            double scrollY = scrollViewer.Offset.Y;
            double viewportWidth = scrollViewer.Viewport.Width;
            double viewportHeight = scrollViewer.Viewport.Height;
            
            // Convert viewport coordinates to image coordinates
            startX = Math.Max(0, (int)(scrollX / pixelSizeOnScreen));
            startY = Math.Max(0, (int)(scrollY / pixelSizeOnScreen));
            endX = Math.Min(width, (int)Math.Ceiling((scrollX + viewportWidth) / pixelSizeOnScreen) + 1);
            endY = Math.Min(height, (int)Math.Ceiling((scrollY + viewportHeight) / pixelSizeOnScreen) + 1);
            
            System.Diagnostics.Debug.WriteLine($"Visible region: X={startX}-{endX}, Y={startY}-{endY} (total: {width}x{height})");
        }

        // Additional safety: limit total labels to prevent UI freeze
        int maxLabelsToShow = 2000;
        int visiblePixels = (endX - startX) * (endY - startY);
        int step = 1;
        
        if (visiblePixels > maxLabelsToShow)
        {
            step = (int)Math.Ceiling(Math.Sqrt((double)visiblePixels / maxLabelsToShow));
            System.Diagnostics.Debug.WriteLine($"Too many visible pixels ({visiblePixels}), using step={step}");
        }

        System.Diagnostics.Debug.WriteLine($"Starting to add RGB labels: visible region {startX},{startY} to {endX},{endY}, step={step}");

        int labelsAdded = 0;
        // Add text for each pixel in visible region
        for (int y = startY; y < endY; y += step)
        {
            for (int x = startX; x < endX; x += step)
            {
                int pixelIndex = (y * width + x) * 3;
                if (pixelIndex + 2 >= pixelData.Length) continue;

                byte r = pixelData[pixelIndex];
                byte g = pixelData[pixelIndex + 1];
                byte b = pixelData[pixelIndex + 2];

                // Create a text block with RGB values
                var textBlock = new TextBlock
                {
                    FontSize = fontSize,
                    FontWeight = FontWeight.Bold,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                };

                // Use contrasting color for text (white or black based on brightness)
                double brightness = (r + g + b) / 3.0;
                var textColor = brightness > 128 ? Colors.Black : Colors.White;
                textBlock.Foreground = new SolidColorBrush(textColor);

                // Format: R,G,B on single line for small pixels, or stacked for large pixels
                if (pixelSizeOnScreen < 60)
                {
                    textBlock.Text = $"{r},{g},{b}";
                }
                else
                {
                    textBlock.Text = $"R:{r}\nG:{g}\nB:{b}";
                }

                // Wrap in a border for positioning (1 pixel = 1 unit in image coordinates)
                var border = new Border
                {
                    Width = 1.0,
                    Height = 1.0,
                    Child = textBlock,
                    BorderBrush = new SolidColorBrush(Colors.Gray),
                    BorderThickness = new Thickness(1.0 / _currentZoom), // Border thickness in image coordinates, will scale with zoom
                    // No background - let the pixel color show through
                };

                // Position at pixel location
                Canvas.SetLeft(border, x);
                Canvas.SetTop(border, y);

                canvas.Children.Add(border);
                labelsAdded++;
                
                // Debug first few labels
                if (labelsAdded <= 3)
                {
                    System.Diagnostics.Debug.WriteLine($"  Label {labelsAdded}: pos=({x},{y}), text='{textBlock.Text}', fontSize={fontSize}, border={1.0/_currentZoom}");
                }
            }
        }

        System.Diagnostics.Debug.WriteLine($"=== FINISHED: Added {labelsAdded} RGB labels to canvas. Total children: {canvas.Children.Count} ===");
    }

    private async void CopyError_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.ErrorMessage != null)
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(ViewModel.ErrorMessage);
            }
        }
    }
}
