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
        public ViewModelBase currentVM
        {
            get { return _currentVM; }
            set
            {
                _currentVM = value;
                RaisePropertyChanged(() => currentVM);
            }
        }

        private DownloadVM _dLoadVM;
        private uploadSkinVM _uploadVM;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(IFtpService dataService)
        {
            _dataService = dataService;
            ViewModelLocator locator = (App.Current.Resources["Locator"] as ViewModelLocator);
            _dLoadVM = locator.downloadVM;
            _uploadVM = locator.uploadVM;

            _currentVM = _dLoadVM;

            Messenger.Default.Register<NotificationMessage<string>>(this, (message) =>
            {
                if (message.Notification == "uploadSkin Show")
                {
                    Messenger.Default.Send<NotificationMessage<string>>(new NotificationMessage<string>(message.Content, "upload"));
                    switchVM();
                }
            });

            Messenger.Default.Register<NotificationMessage>(this, (message) =>
            {
                if (message.Notification == "Switch View")
                {
                    switchVM();
                }
            });
        }

        private void switchVM()
        {
            if (currentVM == _dLoadVM)
                currentVM = _uploadVM;
            else
                currentVM = _dLoadVM;
        }
    }
}