using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using AcrlSync.ViewModel;
using AcrlSync.Model;
using System.Windows.Controls;

namespace AcrlSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            Messenger.Default.Register<NotificationMessage<JobItem>>(this, (message) => {
                if (message.Notification == "addJob Show")
                {
                    jobView dialog = new jobView();
                    Messenger.Default.Send<NotificationMessage<JobItem>>(new NotificationMessage<JobItem>(message.Content, "job"));
                    dialog.ShowDialog();
                }
            });
            Closing += (s, e) => ViewModelLocator.Cleanup();
        }    
    }

    public class ScrollingTextBox : TextBox
        {

            protected override void OnTextChanged(TextChangedEventArgs e)
            {
                base.OnTextChanged(e);
                CaretIndex = Text.Length;
                ScrollToEnd();
            }

        }
}