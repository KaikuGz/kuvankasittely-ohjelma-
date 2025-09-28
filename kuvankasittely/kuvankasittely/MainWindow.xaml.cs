using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MediaColor = System.Windows.Media.Color;
using System.Configuration;

// Remove this line: using System.Windows.Forms;
// We'll use fully qualified names instead

namespace kuvankasittely
{
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.ColorDialog colorDialog;

        private CanvasSizeWindow canvasSizeWindow;
        private double zoomFactor = 1.0;
        System.Windows.Point start;
        private Polyline currentStroke;
        private PointCollection currentPoints;
        bool canDraw = false;
        bool isDrawing = false;
        bool brushPreviewControl = false;

        private Stack<Polyline> undoStack = new Stack<Polyline>();
        private Ellipse brushPreview;

        private int StrokeThickness = 10;

        private ColorPicker colorPickerWindow;
        private MediaColor currentBrushColor = Colors.Black;
        public MainWindow()
        {
            InitializeComponent();

            LoadSettings();
            BrushSize.Text = StrokeThickness.ToString();
            BrushPreviewReload();

            MainCanvas.Children.Add(brushPreview);

            ApplyZoom();
            CanvasScrollViewer.PreviewMouseWheel += CanvasScrollViewer_PreviewMouseWheel;
            InitializeColorPicker();
        }

        private void LoadSettings()
        {
            string previewSetting = ConfigurationManager.AppSettings["BrushPreview"];
            bool previewEnabled = false;

            if (!string.IsNullOrEmpty(previewSetting))
                Boolean.TryParse(previewSetting, out previewEnabled);

            BrushPreviewMenuItem.IsChecked = previewEnabled;
            brushPreviewControl = previewEnabled;
        }
        private void SaveSettings()
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["BrushPreview"].Value = brushPreviewControl.ToString().ToLower();
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        private void BrushPreviewReload()
        {
            brushPreview = new Ellipse
            {
                Width = StrokeThickness, 
                Height = StrokeThickness,
                Stroke = System.Windows.Media.Brushes.Gray,
                StrokeThickness = 1,
                Fill = System.Windows.Media.Brushes.Transparent,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };
        }

        private void Canvas_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && canDraw)
            {
                System.Windows.Point mousePos = e.GetPosition(MainCanvas); // Explicitly use WPF Point
                
                // if not over canvas
                if (mousePos.X >= 0 && mousePos.X <= MainCanvas.ActualWidth &&
                    mousePos.Y >= 0 && mousePos.Y <= MainCanvas.ActualHeight)
                {
                    isDrawing = true;
                    start = mousePos;

                    currentStroke = new Polyline();
                    currentStroke.Stroke = new SolidColorBrush(currentBrushColor); // Use selected color
                    currentStroke.StrokeThickness = StrokeThickness;
                    currentStroke.StrokeLineJoin = PenLineJoin.Round;
                    currentStroke.StrokeStartLineCap = PenLineCap.Round;
                    currentStroke.StrokeEndLineCap = PenLineCap.Round;

                    currentPoints = new PointCollection();
                    currentPoints.Add(mousePos);
                    currentStroke.Points = currentPoints;

                    MainCanvas.Children.Add(currentStroke);
                    MainCanvas.CaptureMouse();
                }
            }
        }

        private void InitializeColorPicker()
        {
            colorDialog = new System.Windows.Forms.ColorDialog();
            colorDialog.AllowFullOpen = true;
            colorDialog.AnyColor = true;
            colorDialog.FullOpen = true;
        }

        private void ColorSquare_Click(object sender, MouseButtonEventArgs e)
        {
            if (colorPickerWindow == null)
            {
                colorPickerWindow = new ColorPicker();

                colorPickerWindow.ColorCanvas.SelectedColor = currentBrushColor;
                colorPickerWindow.ColorCanvas.SelectedColorChanged += ColorCanvas_SelectedColorChanged;

                colorPickerWindow.Closed += (s, args) => colorPickerWindow = null;
            }

            colorPickerWindow.Show();
            colorPickerWindow.Activate();
        }

        private void ColorCanvas_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<MediaColor?> e)
        {
            if (e.NewValue.HasValue)
            {
                currentBrushColor = e.NewValue.Value;
                ColorSquare.Fill = new SolidColorBrush(currentBrushColor);
            }
        }

        private void BrushPreviewMenu_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                brushPreviewControl = menuItem.IsChecked;
            }
        }


        private void Canvas_MouseMove_1(object sender, System.Windows.Input.MouseEventArgs e) 
        {
            System.Windows.Point currentPos = e.GetPosition(MainCanvas); 
            if (canDraw && brushPreviewControl)
            {
                // centered on cursor
                double brushSize = StrokeThickness; 
                brushPreview.Width = brushSize;
                brushPreview.Height = brushSize;

                Canvas.SetLeft(brushPreview, currentPos.X - brushSize / 2);
                Canvas.SetTop(brushPreview, currentPos.Y - brushSize / 2);

                brushPreview.Visibility = Visibility.Visible;
            }
            else
            {
                brushPreview.Visibility = Visibility.Collapsed;
            }

            if (isDrawing && e.LeftButton == MouseButtonState.Pressed && currentStroke != null && canDraw)
            {
                // not over
                double constrainedX = Math.Max(0, Math.Min(MainCanvas.ActualWidth, currentPos.X));
                double constrainedY = Math.Max(0, Math.Min(MainCanvas.ActualHeight, currentPos.Y));
                System.Windows.Point constrainedPos = new System.Windows.Point(constrainedX, constrainedY); // Explicitly use WPF Point
                currentPoints.Add(constrainedPos);

                // smooth
                if (currentPoints.Count > 2)
                {
                    System.Windows.Point lastPoint = currentPoints[currentPoints.Count - 1]; // Explicitly use WPF Point
                    System.Windows.Point secondLastPoint = currentPoints[currentPoints.Count - 2]; // Explicitly use WPF Point

                    double distance = Math.Sqrt(Math.Pow(lastPoint.X - secondLastPoint.X, 2) +
                                              Math.Pow(lastPoint.Y - secondLastPoint.Y, 2));

                    if (distance < 2.0)
                    {
                        currentPoints.RemoveAt(currentPoints.Count - 1);
                    }
                }
            }
        }
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Z)
            {
                UndoLastStroke();
            }
        }
        private void UndoLastStroke_Click(object sender, RoutedEventArgs e)
        {
            UndoLastStroke();
        }
        private void UndoLastStroke()
        {
            if (undoStack.Count > 0)
            {
                Polyline lastStroke = undoStack.Pop();
                MainCanvas.Children.Remove(lastStroke);
            }
        }

        private void Canvas_MouseUp_1(object sender, MouseButtonEventArgs e)
        {
            if (isDrawing && canDraw)
            {
                isDrawing = false;

                if (currentStroke != null)
                {
                    // save for undo
                    undoStack.Push(currentStroke);
                }

                currentStroke = null;
                currentPoints = null;
                MainCanvas.ReleaseMouseCapture();
            }
        }

        private void CanvasSize_Click(object sender, RoutedEventArgs e)
        {
            if (MainCanvas == null)
            {
                System.Windows.MessageBox.Show("Canvas not initialized.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // if open
            if (canvasSizeWindow != null && canvasSizeWindow.IsVisible)
            {
                // Bring existing window to front
                canvasSizeWindow.Activate();
                canvasSizeWindow.Focus();
                return;
            }

            // new wd
            canvasSizeWindow = new CanvasSizeWindow(this, MainCanvas, LoadedImage);
            canvasSizeWindow.Owner = this;

            canvasSizeWindow.Closed += (s, args) => canvasSizeWindow = null;
            canvasSizeWindow.Show();
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            zoomFactor *= 1.25;
            ApplyZoom();
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            zoomFactor *= 0.8;
            ApplyZoom();
        }

        public void ApplyZoom()
        {
            var scale = CanvasScaleTransform;
            scale.ScaleX = zoomFactor;
            scale.ScaleY = zoomFactor;
            ZoomTextBlock.Text = $"Canvas: {MainCanvas.Width} x {MainCanvas.Height} | Zoom: {zoomFactor:P0}";

            tittle.Title = $"KK - Canvas: {MainCanvas.Width} x {MainCanvas.Height} | Zoom: {zoomFactor:P0}";
            canvasSizeWindow?.UpdateDisplayValues();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var bitmap = new BitmapImage(new Uri(openFileDialog.FileName));

                    LoadedImage.Source = bitmap;

                    // Resize canvas to match image dimensions
                    MainCanvas.Width = bitmap.PixelWidth;
                    MainCanvas.Height = bitmap.PixelHeight;

                    ResizeImageToCanvas();
                    ApplyZoom();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to load image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            Rect bounds = VisualTreeHelper.GetDescendantBounds(MainCanvas);

            if (bounds.IsEmpty)
            {
                System.Windows.MessageBox.Show("Canvas appears empty!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create a DrawingVisual to render the canvas with offset
            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(MainCanvas);
                dc.DrawRectangle(vb, null, new Rect(new System.Windows.Point(), bounds.Size));
            }

            // Render to bitmap using bounds size
            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)Math.Ceiling(bounds.Width),
                (int)Math.Ceiling(bounds.Height),
                96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);

            PngBitmapEncoder png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(rtb));

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "CanvasImage",
                DefaultExt = ".png",
                Filter = "PNG Image (.png)|*.png"
            };

            if (dlg.ShowDialog() == true)
            {
                using (var fs = new FileStream(dlg.FileName, FileMode.Create))
                {
                    png.Save(fs);
                }
            }
        }


        private void FitToWindow_Click(object sender, RoutedEventArgs e)
        {
            FitToWindow();
        }

        private void FitToWindow()
        {
            if (LoadedImage.Source == null) return;

            double Width = CanvasScrollViewer.ViewportWidth;
            double Height = CanvasScrollViewer.ViewportHeight;

            if (Width == 0) Width = CanvasScrollViewer.ActualWidth - 20;
            if (Height == 0) Height = CanvasScrollViewer.ActualHeight - 20;

            // originaali
            double imageWidth = MainCanvas.Width;
            double imageHeight = MainCanvas.Height;

            double scaleX = Width / imageWidth;
            double scaleY = Width / imageHeight;

            zoomFactor = Math.Min(scaleX, scaleY);
            zoomFactor = Math.Max(0.1, Math.Min(zoomFactor, 10.0));

            ApplyZoom();
        }

        private void ResizeImageToCanvas()
        {
            if (LoadedImage.Source != null)
            {
                LoadedImage.Width = MainCanvas.Width;
                LoadedImage.Height = MainCanvas.Height;
                Canvas.SetLeft(LoadedImage, 0);
                Canvas.SetTop(LoadedImage, 0);
            }
        }

        private void CanvasScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            double delta = e.Delta > 0 ? 1.1 : 0.9;
            zoomFactor *= delta;

            zoomFactor = Math.Max(0.1, Math.Min(zoomFactor, 10.0));

            ApplyZoom();
            e.Handled = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            canvasSizeWindow?.Close();
            SaveSettings();
            base.OnClosed(e);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (Brush.IsChecked == true)
            {
                canDraw = true;
            }
            if (Brush.IsChecked == false)
            {
                canDraw = false;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                StrokeThickness = Int32.Parse(BrushSize.Text);

                if (MainCanvas.Children.Contains(brushPreview))
                {
                    MainCanvas.Children.Remove(brushPreview);
                }

                BrushPreviewReload();

                MainCanvas.Children.Add(brushPreview);
            } catch { }
        }
    }
}