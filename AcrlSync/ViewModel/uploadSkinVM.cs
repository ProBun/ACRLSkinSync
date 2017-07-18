using AcrlSync.Model;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace AcrlSync.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class uploadSkinVM : ViewModelBase
    {
        private ObservableCollection<Tree> _seriesList;
        public ObservableCollection<Tree> seriesList
        {
            get { return _seriesList; }
        }

        private ObservableCollection<Tree> _carList;
        public ObservableCollection<Tree> carList
        {
            get { return _carList; }
        }

        private ObservableCollection<Tree> _skinList;
        public ObservableCollection<Tree> skinList
        {
            get { return _skinList; }
        }

        private Tree _selectedSeries;
        public Tree selectedSeries
        {
            get { return _selectedSeries; }
            set
            {
                _selectedSeries = value;
                RaisePropertyChanged(() => selectedSeries);
                carList.Clear();
                skinList.Clear();
                foreach (Tree child in value.children)
                {
                    carList.Add(child);
                }
            }
        }

        private Tree _selectedCar;
        public Tree selectedCar
        {
            get { return _selectedCar; }
            set
            {
                _selectedCar = value;
                RaisePropertyChanged(() => selectedCar);
                skinList.Clear();
                string path = _acRoot + @"\content\cars\" + value.Name + @"\skins\";
                if (!Directory.Exists(path))
                {
                    string errorMessage = "Could not find folder: " + path;
                    System.Windows.MessageBox.Show(errorMessage, "Error in AC Folder", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                
                var skinDirs = Directory.GetDirectories(path).Select(f => Path.GetFileName(f)); 
                foreach (string dir in skinDirs)
                {
                    skinList.Add(new Tree(dir,path+dir,path));
                }
            }
        }

        private Tree _selectedSkin;
        public Tree selectedSkin
        {
            get { return _selectedSkin; }
            set
            {
                _selectedSkin = value;
                RaisePropertyChanged(() => selectedCar);
                RaisePropertyChanged(() => evaluateAllowed);
            }
        }

        public bool evaluateAllowed
        {
            get { return treeLoaded && _selectedSkin != null; }
        }

        public RelayCommand evaluateClick { get; set; }

        public ObservableCollection<UploadFiles> filesToUpload { get; set; }

        private Tree tree;
        private bool treeLoaded = false;
        private bool ftpLoaded = false;

        private string _acRoot;

        /// <summary>
        /// Initializes a new instance of the uploadSkinVM class.
        /// </summary>
        public uploadSkinVM()
        {

            tree = new Tree("Upload");

            filesToUpload = new ObservableCollection<UploadFiles>();

            _seriesList = new ObservableCollection<Tree>();
            _carList = new ObservableCollection<Tree>();
            _skinList = new ObservableCollection<Tree>();

            evaluateClick = new RelayCommand(evaluateCarFolder);

            Messenger.Default.Register<NotificationMessage<string>>(this, (message) =>
            {

                if (message.Content == "Upload")
                {
                    if (message.Notification == "Tree Loaded")
                    {
                        treeLoaded = true;
                        ftpLoaded = true;

                        seriesList.Clear();
                        carList.Clear();
                        skinList.Clear();

                        foreach (Tree child in tree.children)
                        {
                            foreach (Tree child2 in child.children)
                            {
                                seriesList.Add(child2);
                            }
                        }
                    }
                    if (message.Notification == "Connection Failure")
                    {
                        treeLoaded = false;
                        ftpLoaded = true;
                        string errorMessage = "Could not connect to FTP: " + ConnectionSettings.options.HostName;
                        System.Windows.MessageBox.Show(errorMessage, "Connection Failure", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
                if (message.Notification == "upload")
                {
                    _acRoot = message.Content;
                }
            });

        }

        private void evaluateCarFolder()
        {
            filesToUpload.Clear();
            string folder = selectedSkin.fullName;
            string[] files= Directory.GetFiles(folder);
            foreach (string file in files)
            {
                filesToUpload.Add(new UploadFiles(file));
            }
        }
    }
}