using AcrlSync.Model;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinSCP;

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
                if (value != null)
                {
                    foreach (Tree child in value.children)
                    {
                        carList.Add(child);
                    }
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
                if (value != null)
                {
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
                        skinList.Add(new Tree(dir, path + dir, path));
                    }
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
                filesToUpload.Clear();
                RaisePropertyChanged(() => selectedCar);
                RaisePropertyChanged(() => evaluateAllowed);

                if (value != null)
                    evaluateCarFolder();
            }
        }

        public bool evaluateAllowed
        {
            get { return treeLoaded && _selectedSkin != null; }
        }

        private bool _uploadEnabled = true;
        public bool uploadEnabled
        {
            get { return _uploadEnabled; }
            set
            {
                _uploadEnabled = value;
                RaisePropertyChanged(() => uploadEnabled);
            }
        }

        public RelayCommand evaluateClick { get; set; }
        public RelayCommand uploadClick { get; set; }
        public RelayCommand backClick { get; set; }

        public ObservableCollection<UploadFiles> filesToUpload { get; set; }

        private Tree tree;
        private bool treeLoaded = false;
        private bool _ftpLoaded = false;
        public bool ftpLoaded
        {
            get { return _ftpLoaded; }
            set
            {
                _ftpLoaded = value;
                RaisePropertyChanged(() => ftpLoaded);
            }
        }

        private string _log;
        public string log
        {
            get { return _log; }
            set { _log = value; RaisePropertyChanged(() => log); }
        }

        private string _acRoot;

        /// <summary>
        /// Initializes a new instance of the uploadSkinVM class.
        /// </summary>
        public uploadSkinVM()
        {

            filesToUpload = new ObservableCollection<UploadFiles>();

            _seriesList = new ObservableCollection<Tree>();
            _carList = new ObservableCollection<Tree>();
            _skinList = new ObservableCollection<Tree>();

            evaluateClick = new RelayCommand(evaluateCarFolder);
            uploadClick = new RelayCommand(upload);
            backClick = new RelayCommand(back);

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
                    treeLoaded = false;
                    ftpLoaded = false;
                    tree = new Tree("Upload");
                    seriesList.Clear();
                    carList.Clear();
                    skinList.Clear();
                }
            });

        }

        private void evaluateCarFolder()
        {
            filesToUpload.Clear();
            string folder = selectedSkin.fullName;
            string[] files = Directory.GetFiles(folder);
            foreach (string file in files)
            {
                filesToUpload.Add(new UploadFiles(file));
            }
            checkForFile(filesToUpload, "preview.jpg");
            checkForFile(filesToUpload, "livery.png");
            checkForFile(filesToUpload, "ui_skin.json");
        }

        private void checkForFile(ObservableCollection<UploadFiles> files, string file)
        {
            if (files.Any(x => string.Compare(x.name, file, true) == 0) == false)
            {
                files.Add(new UploadFiles(file, true));
            }
        }

        private void back()
        {
            Messenger.Default.Send<NotificationMessage>(new NotificationMessage("Switch View"));
        }

        private async void upload()
        {
            uploadEnabled = false;
            log += "***************************\n";
            log += "****     UPLOADING     ****\n";
            log += "***************************\n";
            await uploadToFtp(filesToUpload);
            log += "***************************\n";
            log += "****     COMPLETED     ****\n";
            log += "***************************\n";
            uploadEnabled = true;
        }

        private Task uploadToFtp(ObservableCollection<UploadFiles> files)
        {
            var progressReporter = new ProgressReporter();
            Task t = new Task(() => BackgroundUpload(files, progressReporter));
            t.Start();
            return t;
        }

        private void BackgroundUpload(ObservableCollection<UploadFiles> files, ProgressReporter reporter)
        {
            if (files.Count < 1)
                return;
            string remotePath = selectedCar.fullName + @"/skins/" + selectedSkin.Name;

            using (Session session = new Session())
            {
                try
                { session.Open(ConnectionSettings.options); }
                catch (WinSCP.SessionRemoteException e)
                {
                    System.Console.WriteLine(e.Message);
                    return;
                }

                TransferOptions transferOptions = new TransferOptions();
                List<TransferOperationResult> transferResults = new List<TransferOperationResult>();
                TransferOperationResult transferResult;
                foreach (UploadFiles file in files)
                {
                    if (file.transfer == true)
                    {
                        //Console.WriteLine(string.Format("Uploading {0} to {1}", file.fullname, remotePath + @"/" + file.name));
                        transferResult = session.PutFiles(file.fullname, remotePath+@"/"+file.name, false, transferOptions);

                        //log results
                        if (transferResult.IsSuccess)
                        {
                            foreach (TransferEventArgs transfer in transferResult.Transfers)
                            {
                                reporter.ReportProgressAsync(() =>
                                {
                                    log += string.Format("Uploaded: {0}\n", file.name);
                                });
                            }
                        }
                        else
                        {
                            foreach (TransferEventArgs transfer in transferResult.Transfers)
                            {
                                reporter.ReportProgressAsync(() =>
                                {
                                    log += string.Format("Error: {0}\n\t{1}\n", file.name, transfer.Error);
                                });
                            }
                        }
                    }
                }
            }
        }
    }
}