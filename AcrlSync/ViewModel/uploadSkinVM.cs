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
    public class UploadSkinVM : ViewModelBase
    {
        private readonly ObservableCollection<Tree> _seriesList;
        public ObservableCollection<Tree> SeriesList
        {
            get { return _seriesList; }
        }

        private readonly ObservableCollection<Tree> _carList;
        public ObservableCollection<Tree> CarList
        {
            get { return _carList; }
        }

        private readonly ObservableCollection<Tree> _skinList;
        public ObservableCollection<Tree> SkinList
        {
            get { return _skinList; }
        }

        private Tree _selectedSeries;
        public Tree SelectedSeries
        {
            get { return _selectedSeries; }
            set
            {
                _selectedSeries = value;
                RaisePropertyChanged(() => SelectedSeries);
                CarList.Clear();
                SkinList.Clear();
                if (value != null)
                {
                    foreach (Tree child in value.Children)
                    {
                        CarList.Add(child);
                    }
                }
            }
        }

        private Tree _selectedCar;
        public Tree SelectedCar
        {
            get { return _selectedCar; }
            set
            {
                _selectedCar = value;
                RaisePropertyChanged(() => SelectedCar);
                SkinList.Clear();
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
                        SkinList.Add(new Tree(dir, path + dir, path));
                    }
                }
            }
        }

        private Tree _selectedSkin;
        public Tree SelectedSkin
        {
            get { return _selectedSkin; }
            set
            {
                _selectedSkin = value;
                FilesToUpload.Clear();
                RaisePropertyChanged(() => SelectedCar);
                RaisePropertyChanged(() => EvaluateAllowed);

                if (value != null)
                    EvaluateCarFolder();
            }
        }

        public bool EvaluateAllowed
        {
            get { return treeLoaded && _selectedSkin != null; }
        }

        private bool _uploadEnabled = true;
        public bool UploadEnabled
        {
            get { return _uploadEnabled; }
            set
            {
                _uploadEnabled = value;
                RaisePropertyChanged(() => UploadEnabled);
            }
        }

        public RelayCommand EvaluateClick { get; set; }
        public RelayCommand UploadClick { get; set; }
        public RelayCommand BackClick { get; set; }

        public ObservableCollection<UploadFiles> FilesToUpload { get; set; }

        private Tree tree;
        private bool treeLoaded = false;
        private bool _ftpLoaded = false;
        public bool FtpLoaded
        {
            get { return _ftpLoaded; }
            set
            {
                _ftpLoaded = value;
                RaisePropertyChanged(() => FtpLoaded);
            }
        }

        private string _log;
        public string Log
        {
            get { return _log; }
            set { _log = value; RaisePropertyChanged(() => Log); }
        }

        private string _acRoot;

        /// <summary>
        /// Initializes a new instance of the uploadSkinVM class.
        /// </summary>
        public UploadSkinVM()
        {

            FilesToUpload = new ObservableCollection<UploadFiles>();

            _seriesList = new ObservableCollection<Tree>();
            _carList = new ObservableCollection<Tree>();
            _skinList = new ObservableCollection<Tree>();

            EvaluateClick = new RelayCommand(EvaluateCarFolder);
            UploadClick = new RelayCommand(Upload);
            BackClick = new RelayCommand(Back);

            Messenger.Default.Register<NotificationMessage<string>>(this, (message) =>
            {

                if (message.Content == "Upload")
                {
                    if (message.Notification == "Tree Loaded")
                    {
                        treeLoaded = true;
                        FtpLoaded = true;

                        SeriesList.Clear();
                        CarList.Clear();
                        SkinList.Clear();

                        foreach (Tree child in tree.Children)
                        {
                            foreach (Tree child2 in child.Children)
                            {
                                SeriesList.Add(child2);
                            }
                        }
                    }
                    if (message.Notification == "Connection Failure")
                    {
                        treeLoaded = false;
                        FtpLoaded = true;
                        string errorMessage = "Could not connect to FTP: " + ConnectionSettings.Options.HostName;
                        System.Windows.MessageBox.Show(errorMessage, "Connection Failure", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
                if (message.Notification == "upload")
                {
                    _acRoot = message.Content;
                    treeLoaded = false;
                    FtpLoaded = false;
                    tree = new Tree("Upload");
                    SeriesList.Clear();
                    CarList.Clear();
                    SkinList.Clear();
                }
            });

        }

        private void EvaluateCarFolder()
        {
            FilesToUpload.Clear();
            string folder = SelectedSkin.FullName;
            string[] files = Directory.GetFiles(folder);
            foreach (string file in files)
            {
                FilesToUpload.Add(new UploadFiles(file));
            }
            CheckForFile(FilesToUpload, "preview.jpg");
            CheckForFile(FilesToUpload, "livery.png");
            CheckForFile(FilesToUpload, "ui_skin.json");
        }

        private void CheckForFile(ObservableCollection<UploadFiles> files, string file)
        {
            if (files.Any(x => string.Compare(x.Name, file, true) == 0) == false)
            {
                files.Add(new UploadFiles(file, true));
            }
        }

        private void Back()
        {
            Messenger.Default.Send<NotificationMessage>(new NotificationMessage("Switch View"));
        }

        private async void Upload()
        {
            UploadEnabled = false;
            Log += "***************************\n";
            Log += "****     UPLOADING     ****\n";
            Log += "***************************\n";
            await UploadToFtp(FilesToUpload);
            Log += "***************************\n";
            Log += "****     COMPLETED     ****\n";
            Log += "***************************\n";
            UploadEnabled = true;
        }

        private Task UploadToFtp(ObservableCollection<UploadFiles> files)
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
            string remotePath = SelectedCar.FullName + @"/skins/" + SelectedSkin.Name;

            using (Session session = new Session())
            {
                try
                { session.Open(ConnectionSettings.Options); }
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
                    if (file.Transfer == true)
                    {
                        //Console.WriteLine(string.Format("Uploading {0} to {1}", file.fullname, remotePath + @"/" + file.name));
                        transferResult = session.PutFiles(file.Fullname, remotePath+@"/"+file.Name, false, transferOptions);

                        //log results
                        if (transferResult.IsSuccess)
                        {
                            foreach (TransferEventArgs transfer in transferResult.Transfers)
                            {
                                reporter.ReportProgressAsync(() =>
                                {
                                    Log += string.Format("Uploaded: {0}\n", file.Name);
                                });
                            }
                        }
                        else
                        {
                            foreach (TransferEventArgs transfer in transferResult.Transfers)
                            {
                                reporter.ReportProgressAsync(() =>
                                {
                                    Log += string.Format("Error: {0}\n\t{1}\n", file.Name, transfer.Error);
                                });
                            }
                        }
                    }
                }
            }
        }
    }
}