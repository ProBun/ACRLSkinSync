using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using AcrlSync.Model;
using AcrlSync.ViewModel;
using System;

namespace AcrlSync.ViewModel
{

    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// See http://www.mvvmlight.net
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly IFtpService _dataService;

        private ViewModelBase _currentVM;
        public ViewModelBase CurrentVM
        {
            get { return _currentVM; }
            set
            {
                _currentVM = value;
                RaisePropertyChanged(() => CurrentVM);
            }
        }

        private readonly DownloadVM downloadVM;
        private readonly UploadSkinVM uploadVM;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(IFtpService dataService)
        {
            _dataService = dataService;
            ViewModelLocator locator = (App.Current.Resources["Locator"] as ViewModelLocator);
            downloadVM = locator.DownloadVM;
            uploadVM = locator.UploadVM;

            _currentVM = downloadVM;

            Messenger.Default.Register<NotificationMessage<string>>(this, (message) =>
            {
                if (message.Notification == "uploadSkin Show")
                {
                    Messenger.Default.Send<NotificationMessage<string>>(new NotificationMessage<string>(message.Content, "upload"));
                    SwitchVM();
                }
            });

            Messenger.Default.Register<NotificationMessage>(this, (message) =>
            {
                if (message.Notification == "Switch View")
                {
                    SwitchVM();
                }
            });
        }

        private void SwitchVM()
        {
            if (CurrentVM == downloadVM)
                CurrentVM = uploadVM;
            else
                CurrentVM = downloadVM;
        }
    }
}