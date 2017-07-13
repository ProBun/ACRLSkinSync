using AcrlSync.Model;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Windows;

namespace AcrlSync
{
    /// <summary>
    /// Description for jobView.
    /// </summary>
    public partial class jobView : Window
    {
        private bool closeHandled;

        /// <summary>
        /// Initializes a new instance of the jobView class.
        /// </summary>
        public jobView()
        {
            InitializeComponent();
            closeHandled = false;

            Messenger.Default.Register<NotificationMessage<JobItem>>(this, (message) => {
                if (message.Notification == "closeJob")
                {
                    Messenger.Default.Unregister(this);
                    closeHandled = true;
                    Close();
                }
            });
        }

        private void jobWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (closeHandled == false)
                Messenger.Default.Send<NotificationMessage>(new NotificationMessage("closeJob"));
        }
    }
}