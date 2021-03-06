/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocatorTemplate xmlns:vm="clr-namespace:AcrlSync.ViewModel"
                                   x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"
*/

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using AcrlSync.Model;

namespace AcrlSync.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// <para>
    /// See http://www.mvvmlight.net
    /// </para>
    /// </summary>
    public class ViewModelLocator
    {
        static ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            SimpleIoc.Default.Register<IFtpService, FtpService>();
            SimpleIoc.Default.Register<MainViewModel>();
            SimpleIoc.Default.Register<JobVM>();
            SimpleIoc.Default.Register<UploadSkinVM>();
            SimpleIoc.Default.Register<DownloadVM>();
        }

        /// <summary>
        /// Gets the Main property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public MainViewModel Main
        {
            get
            {
                return ServiceLocator.Current.GetInstance<MainViewModel>();
            }
        }
        public JobVM JobVM
        {
            get
            {
                return ServiceLocator.Current.GetInstance<JobVM>();
            }
        }
        public UploadSkinVM UploadVM
        {
            get
            {
                return ServiceLocator.Current.GetInstance<UploadSkinVM>();
            }
        }
        public DownloadVM DownloadVM
        {
            get
            {
                return ServiceLocator.Current.GetInstance<DownloadVM>();
            }
        }
        /// <summary>
        /// Cleans up all the resources.
        /// </summary>
        public static void Cleanup()
        {
        }
    }
}