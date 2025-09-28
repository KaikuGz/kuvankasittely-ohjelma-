using System;
using System.Windows;
using System.Windows.Controls;

namespace kuvankasittely
{
    public partial class CanvasSizeWindow : Window
    {
        private Canvas _canvas;
        private System.Windows.Controls.Image _image;
        private MainWindow _main;
        private bool isUpdating = false;

        public CanvasSizeWindow(MainWindow mainWindow, Canvas canvas, System.Windows.Controls.Image image)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"XAML Load Error: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            _main = mainWindow;
            _canvas = canvas;
            _image = image;

            UpdateDisplayValues();
        }

        public void UpdateDisplayValues()
        {
            if (_canvas == null) return;

            isUpdating = true;

            WidthSlider.Value = _canvas.Width;
            HeightSlider.Value = _canvas.Height;
            WidthTextBox.Text = ((int)_canvas.Width).ToString();
            HeightTextBox.Text = ((int)_canvas.Height).ToString();

            isUpdating = false;
        }

        private void WidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (WidthTextBox == null || isUpdating) return;

            int width = (int)e.NewValue;
            WidthTextBox.Text = width.ToString();

            if (_canvas != null)
            {
                _canvas.Width = width;
                ResizeImage();
            }
        }

        private void HeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (HeightTextBox == null || isUpdating) return;

            int height = (int)e.NewValue;
            HeightTextBox.Text = height.ToString();

            if (_canvas != null)
            {
                _canvas.Height = height;
                ResizeImage();
            }
        }

        private void WidthTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (WidthSlider == null || isUpdating) return;

            if (int.TryParse(WidthTextBox.Text, out int val) && val >= 1)
            {
                // Extend slider range if needed
                if (val > WidthSlider.Maximum)
                    WidthSlider.Maximum = val + 100;

                isUpdating = true;
                WidthSlider.Value = val;
                _canvas.Width = val;
                ResizeImage();
                isUpdating = false;
            }
        }

        private void HeightTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (HeightSlider == null || isUpdating) return;

            if (int.TryParse(HeightTextBox.Text, out int val) && val >= 1)
            {
                // Extend slider range if needed
                if (val > HeightSlider.Maximum)
                    HeightSlider.Maximum = val + 100;

                isUpdating = true;
                HeightSlider.Value = val;
                _canvas.Height = val;
                ResizeImage();
                isUpdating = false;
            }
        }

        private void ResizeImage()
        {
            if (_image != null && _image.Source != null && _canvas != null)
            {
                _image.Width = _canvas.Width;
                _image.Height = _canvas.Height;
                Canvas.SetLeft(_image, 0);
                Canvas.SetTop(_image, 0);
            }

            _main?.ApplyZoom();
        }
    }
}