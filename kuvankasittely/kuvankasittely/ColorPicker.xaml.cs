using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace kuvankasittely
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    using MediaColor = System.Windows.Media.Color;

    public partial class ColorPicker : Window
    {
        public MediaColor SelectedColor { get; private set; }

        public ColorPicker()
        {
            InitializeComponent();
            this.Topmost = true;
            ColorCanvas.SelectedColor = Colors.Black;
            SelectedColor = Colors.Black;
        }

        private void ColorCanvas_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<MediaColor?> e)
        {
            if (e.NewValue.HasValue)
                SelectedColor = e.NewValue.Value;
        }
    }

}
