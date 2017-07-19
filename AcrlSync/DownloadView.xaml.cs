using System.Windows.Controls;

namespace AcrlSync
{
    /// <summary>
    /// Description for DownloadView.
    /// </summary>
    public partial class DownloadView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the DownloadView class.
        /// </summary>
        public DownloadView()
        {
            InitializeComponent();

            //Messenger.Default.Register<NotificationMessage<JobItem>>(this, (message) =>
            //{
            //    if (message.Notification == "addJob Show")
            //    {
            //        jobView dialog = new jobView();
            //        Messenger.Default.Send<NotificationMessage<JobItem>>(new NotificationMessage<JobItem>(message.Content, "job"));
            //        dialog.ShowDialog();
            //    }
            //});
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