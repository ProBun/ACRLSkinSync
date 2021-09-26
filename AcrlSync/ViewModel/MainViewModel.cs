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

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            ViewModelLocator locator = (App.Current.Resources["Locator"] as ViewModelLocator);
            downloadVM = locator.DownloadVM;

            _currentVM = downloadVM;
        }
    }
}