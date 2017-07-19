using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Navigation;

namespace AcrlSync
{
    /// <summary>
    /// Description for UploadView.
    /// </summary>
    public partial class UploadView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the UploadView class.
        /// </summary>
        public UploadView()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }

    public class DecorationConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object para, CultureInfo culture)
        {
            if ((bool)value == false)
                return TextDecorations.Strikethrough;
            else return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ColorConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object para, CultureInfo culture)
        {
            // Console.WriteLine(value.ToString());
            if ((bool)value == false)
                return "black";
            else return "red";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TransferConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object para, CultureInfo culture)
        {
            // Console.WriteLine(value.ToString());
            if ((bool)value == false)
                return "Hidden";
            else return "Visible";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}