using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;
using AcrlSync.Model;
using System.Collections.ObjectModel;
using System;
using System.Linq;
using ByteSizeLib;
using WinSCP;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Ookii.Dialogs.Wpf;

namespace AcrlSync.ViewModel
{
    public class optionItem : ViewModelBase
    {
        public string name
        {
            get { return _name; }
            set
            {
                _name = value;
                RaisePropertyChanged(() => name);
            }
        }
        public string dlPath
        {
            get { return _dlPath; }
            set
            {
                _dlPath = value;
                RaisePropertyChanged(() => dlPath);
            }
        }
        public bool? isChecked
        {
            get { return _isChecked; }
            set
            {
                _isChecked = value;
                RaisePropertyChanged(() => isChecked);
            }
        }

        private string _name;
        private string _dlPath;
        private bool? _isChecked;

        public optionItem()
        {
            _name = "";
            _dlPath = "";
        }
    }

    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// See http://www.mvvmlight.net
    /// </para>
    /// </summary>
    public class DownloadVM : ViewModelBase
    {
        private readonly IFtpService _dataService;

        private CancellationTokenSource cancellationTokenSource;

        private ObservableCollection<JobItem> _jobs;
        public ObservableCollection<JobItem> jobs
        {
            get { return _jobs; }
            set { _jobs = value; RaisePropertyChanged(() => jobs); }
        }

        private JobItem _selectedJob;
        public JobItem selectedJob
        {
            get { return _selectedJob; }
            set { _selectedJob = value; RaisePropertyChanged(() => selectedJob); }
        }

        private string _log;
        public string log
        {
            get { return _log; }
            set { _log = value; RaisePropertyChanged(() => log); }
        }

        private string _skins;
        public string skins
        {
            get { return _skins; }
            set { _skins = value; RaisePropertyChanged(() => skins); }
        }

        private string _analysisText;
        public string analysisText
        {
            get { return _analysisText; }
            set { _analysisText = value; RaisePropertyChanged(() => analysisText); }
        }

        private string _runText;
        public string runText
        {
            get { return _runText; }
            set { _runText = value; RaisePropertyChanged(() => runText); }
        }

        private string _size;
        public string size
        {
            get { return _size; }
            set { _size = value; RaisePropertyChanged(() => size); }
        }

        private string _files;
        public string files
        {
            get { return _files; }
            set { _files = value; RaisePropertyChanged(() => files); }
        }

        private bool _treeLoaded;
        public bool treeLoaded
        {
            get { return _treeLoaded; }
            set { _treeLoaded = value; RaisePropertyChanged(() => treeLoaded); }
        }

        private string _loading;
        public string loading
        {
            get { return _loading; }
            set { _loading = value; RaisePropertyChanged(() => loading); }
        }

        private string _ftpAddress;
        public string ftpAddress
        {
            get { return _ftpAddress; }
            set
            {
                _ftpAddress = value;
                RaisePropertyChanged(() => ftpAddress);

                //check valid before this and redo tree loading
                if (value != null)
                    ConnectionSettings.setHost(value);
            }
        }

        private string _ftpError;
        public string ftpError
        {
            get { return _ftpError; }
            set { _ftpError = value; RaisePropertyChanged(() => ftpError); }
        }

        private bool _ftpLoaded;
        public bool ftpLoaded
        {
            get { return _ftpLoaded; }
            set { _ftpLoaded = value; RaisePropertyChanged(() => ftpLoaded); }
        }

        private string _acPath;
        public string acPath
        {
            get { return _acPath; }
            set
            {
                _acPath = value;
                RaisePropertyChanged(() => acPath);

                //check valid before this and redo tree loading
                Jobs.setPath(value);
            }
        }

        private bool _analyseInProgress;
        private bool _runInProgress;

        private JobItem _analysedjob;
        // private JobItem _editJob;
        // private int _editIndex;

        private AnalysisItem analysis;

        public RelayCommand ftpClick { get; set; }
        public RelayCommand editClick { get; set; }
        public RelayCommand addClick { get; set; }
        public RelayCommand analyseClick { get; set; }
        public RelayCommand runClick { get; set; }
        public RelayCommand findClick { get; set; }
        public RelayCommand uploadClick { get; set; }

        // private jobVM _jobvm;
        private List<Tree> _seasons;
        public List<Tree> seasons
        {
            get { return _seasons; }
            set
            {
                _seasons = value;
                RaisePropertyChanged(() => seasons);
            }
        }



        private List<optionItem> _options;
        public List<optionItem> options
        {
            get { return _options; }
            set
            {
                _options = value;
                RaisePropertyChanged(() => options);

            }
        }

        /// <summary>
        /// Initializes a new instance of the DownloadVM class.
        /// </summary>
        public DownloadVM(IFtpService dataService)
        {
            _ftpAddress = ConnectionSettings.options.HostName;
            _acPath = Jobs.acCarsPath;

            loadConnectionJson();
            string path = AppDomain.CurrentDomain.BaseDirectory;
            string json = System.IO.File.ReadAllText(path + "/acPath.json");
            _acPath = JsonConvert.DeserializeObject<String>(json);

            //find jobVM so it can be intialised and start async call to populate tree
            //ViewModelLocator vmLoc = new ViewModelLocator();
            //_jobvm = vmLoc.jobVM;

            _seasons = new List<Tree>();
            var item = new Tree("Download");
            _seasons.Add(item);

            _options = new List<optionItem>();

            //editClick = new RelayCommand(editJob);
            //addClick = new RelayCommand(addJob);
            analyseClick = new RelayCommand(analyseJob);
            runClick = new RelayCommand(runJob);
            ftpClick = new RelayCommand(reinitialise);
            findClick = new RelayCommand(findPath);
            uploadClick = new RelayCommand(switchToUpload);

            _dataService = dataService;
            _log = string.Empty;

            _loading = "Loading data from FTP";
            _treeLoaded = false;
            _ftpLoaded = false;
            _ftpError = "Connecting";

            _analyseInProgress = false;
            _runInProgress = false;

            _analysisText = "A_nalyse";
            _runText = "_Run";

            //_editJob = null;

            /*
            _jobs = new ObservableCollection<JobItem>();
            loadJson();
            _selectedJob = _jobs.FirstOrDefault();

            Messenger.Default.Register<NotificationMessage<JobItem>>(this, (message) => {
                if (message.Notification == "closeJob")
                {
                    if (message.Content != null)
                    {
                        if (_editJob == null)
                            jobs.Add(message.Content);
                        else
                            jobs.Insert(_editIndex, message.Content);
                        selectedJob = message.Content;
                        saveJson();
                        _editJob = null;
                    }
                }
            });
            */

            Messenger.Default.Register<NotificationMessage<string>>(this, (message) =>
            {
                /*
                if (message.Notification == "closeJob")
                {
                    if (_editJob != null)
                    {
                        jobs.Insert(_editIndex, _editJob);
                        selectedJob = _editJob;
                        _editJob = null;
                    }
                }
                */
                if (message.Content == "Download")
                {
                    if (message.Notification == "Tree Loaded")
                    {
                        loading = "";
                        treeLoaded = true;
                        ftpLoaded = true;
                        ftpError = "";

                        List<optionItem> list = new List<optionItem>();
                        foreach (Tree s in _seasons)
                        {
                            flattenTree(s, "", list);
                        }
                        options = list;
                    }
                    if (message.Notification == "Connection Failure")
                    {
                        loading = "";
                        treeLoaded = false;
                        ftpLoaded = true;
                        ftpError = "Could not Connect";
                        string errorMessage = "Could not connect to FTP: " + ConnectionSettings.options.HostName;
                        System.Windows.MessageBox.Show(errorMessage, "Connection Failure", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            });
            //_dataService.GetDirectoryDetails("Download");
        }

        private void flattenTree(Tree input, string parent, List<optionItem> output, int level = 0)
        {
            int maxLevel = 2;

            if (level == maxLevel)
            {
                optionItem item = new optionItem();
                item.name = String.Format("{0} \u2013 {1}", parent, input.Name);
                item.dlPath = input.fullName;
                item.isChecked = false;
                output.Add(item);
            }
            else
            {
                foreach (Tree child in input.children)
                    flattenTree(child, input.Name, output, level + 1);
            }
        }

        private void reinitialise()
        {
            saveConnectionJson();
            loading = "Loading data from FTP";
            treeLoaded = false;
            ftpLoaded = false;
            ftpError = "Connecting";

            options = new List<optionItem>();

            //_jobvm.seasons = new List<Tree>();
            _seasons = new List<Tree>();
            var item = new Tree("Download");
            //_jobvm.seasons.Add(item);
            _seasons.Add(item);

            analysisText = "A_nalyse";
            runText = "_Run";
        }

        /*
        private void editJob()
        {
            if (selectedJob==null)
            {
                System.Windows.MessageBox.Show("A job must be selected","Select a Job",System.Windows.MessageBoxButton.OK,System.Windows.MessageBoxImage.Exclamation);
                return;
            }
            _editJob = selectedJob;
            _editIndex = jobs.IndexOf(_editJob);
            jobs.Remove(selectedJob);
            Messenger.Default.Send<NotificationMessage<JobItem>>(new NotificationMessage<JobItem>(_editJob,"addJob Show"));  
        }
        */
        /*
        private void saveJson()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            string json = JsonConvert.SerializeObject(jobs, Formatting.Indented);
            System.IO.File.WriteAllText(path+"/jobs.json", json);
            string json2 = JsonConvert.SerializeObject(jobs, Formatting.Indented);
            System.IO.File.WriteAllText(path + "/jobs.json", json2);
        }

        private void loadJson()
        {
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory;
                string json = System.IO.File.ReadAllText(path + "/jobs.json");
                _jobs = JsonConvert.DeserializeObject<ObservableCollection<JobItem>>(json);
            }
            catch
            {
                _jobs = new ObservableCollection<JobItem>();
            }
        }
        */
        private void saveConnectionJson()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            string json = JsonConvert.SerializeObject(ConnectionSettings.options, Formatting.Indented);
            System.IO.File.WriteAllText(path + "/connection.json", json);
        }
        private void loadConnectionJson()
        {
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory;
                string json = System.IO.File.ReadAllText(path + "/connection.json");
                SessionOptions temp = JsonConvert.DeserializeObject<SessionOptions>(json);
                ConnectionSettings.options.HostName = temp.HostName;
                ConnectionSettings.options.Protocol = temp.Protocol;
                ConnectionSettings.options.UserName = temp.UserName;
                ConnectionSettings.options.Password = temp.Password;
                _ftpAddress = temp.HostName;
            }
            catch
            {
                //dont need to do anything
            }
        }

        private async void analyseJob()
        {

            if (_runInProgress)
                return;

            if (_analyseInProgress)
            {
                cancellationTokenSource.Cancel();
                return;
            }

            selectedJob = new JobItem();
            selectedJob.acCarsPath = acPath;
            foreach (optionItem item in options)
            {
                if (item.isChecked == true)
                {
                    selectedJob.ftpPath.Add(item.dlPath);
                }
            }
            if (selectedJob.ftpPath.Count < 1)
            {
                System.Windows.MessageBox.Show("No series selected", "Check some series", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation);
                return;
            }

            if (selectedJob == null)
            {
                System.Windows.MessageBox.Show("A job must be selected", "Select a Job", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation);
                return;
            }

            ftpLoaded = false;

            analysisText = "_Cancel";
            _analyseInProgress = true;
            skins = "  0";
            files = "  0";
            size = "0 b";
            log += "*******************************************************************\n";
            log += "****                     ANALYSIS  STARTED                     ****\n";
            log += "*******************************************************************\n";
            _analysedjob = selectedJob;
            analysis = await analyseFtp(selectedJob);
            if (analysis == null)
            {
                log += "*******************************************************************\n";
                log += "****                     ANALYSIS  FAILED                      ****\n";
                log += "*******************************************************************\n";
                Messenger.Default.Send<NotificationMessage>(new NotificationMessage("Connection Failure"));
                analysisText = "A_nalyse";
                _analyseInProgress = false;
                ftpLoaded = true;
                return;
            }
            skins = analysis.skinCount.ToString().PadLeft(3);
            files = analysis.files.ToString().PadLeft(3);
            if (analysis.size == 0)
                size = "0 b";
            else
                size = ByteSize.FromBytes(analysis.size).ToString();
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                log += "*******************************************************************\n";
                log += "****                     ANALYSIS COMPLETE                     ****\n";
                log += "*******************************************************************\n";
            }
            else
            {
                log += "*******************************************************************\n";
                log += "****                     ANALYSIS  ABORTED                     ****\n";
                log += "*******************************************************************\n";
            }
            analysisText = "A_nalyse";
            _analyseInProgress = false;
            ftpLoaded = true;
        }

        private Task<AnalysisItem> analyseFtp(JobItem job)
        {
            var progressReporter = new ProgressReporter();
            Task<AnalysisItem> t = new Task<AnalysisItem>(() => BackgroundAnalyse(job, progressReporter));
            t.Start();
            return t;
        }

        private Task<AnalysisItem> runFtp(AnalysisItem data, JobItem job)
        {
            var progressReporter = new ProgressReporter();
            Task<AnalysisItem> t = new Task<AnalysisItem>(() => BackgroundRun(data, job, progressReporter));
            t.Start();
            return t;
        }

        private AnalysisItem BackgroundAnalyse(JobItem job, ProgressReporter reporter)
        {
            AnalysisItem data = new AnalysisItem();
            data.skinCount = 0;
            data.files = 0;
            data.size = 0;
            this.cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = this.cancellationTokenSource.Token;

            using (Session session = new Session())
            {
                try
                { session.Open(ConnectionSettings.options); }
                catch (WinSCP.SessionRemoteException e)
                {
                    System.Console.WriteLine(e.Message);
                    return null;
                }

                string localPath = job.acCarsPath;
                foreach (string remotePath in job.ftpPath)
                {
                    RemoteDirectoryInfo carInfo = session.ListDirectory(remotePath);
                    foreach (RemoteFileInfo car in carInfo.Files)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return data;
                        if (car.IsDirectory && car.Name != "..")
                        {
                            reporter.ReportProgressAsync(() =>
                            {
                                log += string.Format("Car: {0}\n", car.Name);
                            });
                            localPath = Path.Combine(job.acCarsPath, car.Name, "skins");
                            RemoteDirectoryInfo skinInfo = session.ListDirectory(car.FullName + "/skins");
                            foreach (RemoteFileInfo skinDir in skinInfo.Files)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                    return data;
                                if (skinDir.IsDirectory && skinDir.Name != "..")
                                {
                                    reporter.ReportProgressAsync(() =>
                                    {
                                        log += string.Format("\tSkin: {0,-40}", skinDir.Name);
                                    });
                                    localPath = Path.Combine(job.acCarsPath, car.Name, "skins", skinDir.Name);
                                    if (!Directory.Exists(localPath))
                                    {
                                        reporter.ReportProgressAsync(() =>
                                        {
                                            log += string.Format("Not found in cars folder\n");
                                        });
                                        Skin skin = new Skin();
                                        skin.name = skinDir.Name;
                                        skin.car = car.Name;
                                        data.skinCount += 1;
                                        RemoteDirectoryInfo skinFiles = session.ListDirectory(skinDir.FullName);
                                        List<RemoteFileInfo> files = new List<RemoteFileInfo>();
                                        foreach (RemoteFileInfo file in skinFiles.Files)
                                        {
                                            if (cancellationToken.IsCancellationRequested)
                                                return data;
                                            if (!file.IsDirectory && file.Name != "..")
                                            {
                                                files.Add(file);
                                                data.files += 1;
                                                data.size += file.Length;

                                                reporter.ReportProgressAsync(() =>
                                                {
                                                    this.skins = data.skinCount.ToString().PadLeft(3);
                                                    this.files = data.files.ToString().PadLeft(3);
                                                    if (data.size == 0)
                                                        this.size = "0 b";
                                                    else
                                                        this.size = ByteSize.FromBytes(data.size).ToString();
                                                });
                                            }
                                        }
                                        skin.files = files;
                                        data.skins.Add(skin);
                                    }
                                    else
                                    {
                                        RemoteDirectoryInfo skinFiles = session.ListDirectory(skinDir.FullName);
                                        List<RemoteFileInfo> files = new List<RemoteFileInfo>();
                                        foreach (RemoteFileInfo file in skinFiles.Files)
                                        {
                                            if (cancellationToken.IsCancellationRequested)
                                                return data;
                                            if (!file.IsDirectory && file.Name != "..")
                                            {
                                                localPath = Path.Combine(job.acCarsPath, car.Name, "skins", skinDir.Name, file.Name);
                                                bool required = false;
                                                if (!File.Exists(localPath))
                                                {
                                                    required = true;
                                                }

                                                else
                                                {
                                                    DateTime remoteWriteTime = file.LastWriteTime;
                                                    DateTime localWriteTime = File.GetLastWriteTime(localPath);
                                                    if (remoteWriteTime > localWriteTime)
                                                    {
                                                        required = true;
                                                    }
                                                }

                                                if (required)
                                                {
                                                    files.Add(file);
                                                    data.files += 1;
                                                    data.size += file.Length;
                                                }
                                            }
                                        }
                                        if (files.Count > 0)
                                        {
                                            reporter.ReportProgressAsync(() =>
                                            {
                                                log += string.Format("has new or updated files\n");
                                                this.skins = data.skinCount.ToString().PadLeft(3);
                                                this.files = data.files.ToString().PadLeft(3);
                                                if (data.size == 0)
                                                    this.size = "0 b";
                                                else
                                                    this.size = ByteSize.FromBytes(data.size).ToString();
                                            });
                                            Skin skin = new Skin();
                                            skin.name = skinDir.Name;
                                            skin.car = car.Name;
                                            data.skinCount += 1;
                                            skin.files = files;
                                            data.skins.Add(skin);
                                        }
                                        else
                                        {
                                            reporter.ReportProgressAsync(() =>
                                            {
                                                log += string.Format("\n");
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return data;
        }

        private async void runJob()
        {
            if (_analyseInProgress || _analysedjob == null)
                return;

            if (_runInProgress)
            {
                cancellationTokenSource.Cancel();
                return;
            }

            if (analysis == null)
            {
                return;
            }

            runText = "_Cancel";
            _runInProgress = true;
            ftpLoaded = false;

            log += "*******************************************************************\n";
            log += "****                     DOWNLOAD  STARTED                     ****\n";
            log += "*******************************************************************\n";
            analysis = await runFtp(analysis, _analysedjob);
            skins = analysis.skinCount.ToString().PadLeft(3);
            files = analysis.files.ToString().PadLeft(3);
            if (analysis.size == 0)
                size = "0 b";
            else
                size = ByteSize.FromBytes(analysis.size).ToString();
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                log += "*******************************************************************\n";
                log += "****                     DOWNLOAD COMPLETE                     ****\n";
                log += "*******************************************************************\n";
            }
            else
            {
                log += "*******************************************************************\n";
                log += "****                     DOWNLOAD  ABORTED                     ****\n";
                log += "*******************************************************************\n";
            }
            runText = "_Run";
            _runInProgress = false;
            ftpLoaded = true;
        }

        private AnalysisItem BackgroundRun(AnalysisItem data, JobItem job, ProgressReporter reporter)
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = this.cancellationTokenSource.Token;

            using (Session session = new Session())
            {
                session.Open(ConnectionSettings.options);

                string lastCar = null;
                Queue<Skin> skinQueue = new Queue<Skin>(data.skins);

                while (skinQueue.Count > 0)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return data;

                    Skin skin = skinQueue.Dequeue();
                    if (skin.car != lastCar)
                        reporter.ReportProgressAsync(() =>
                        {
                            log += string.Format("Car: {1}\n\tSkin: {0,-40}\n", skin.name, skin.car);
                        });
                    else
                        reporter.ReportProgressAsync(() =>
                        {
                            log += string.Format("\tSkin: {0,-40}\n", skin.name);
                        });

                    string localPath = Path.Combine(job.acCarsPath, skin.car, "skins", skin.name);
                    Directory.CreateDirectory(localPath);
                    Queue<RemoteFileInfo> fileQueue = new Queue<RemoteFileInfo>(skin.files);
                    while (fileQueue.Count > 0)
                    {
                        var file = fileQueue.Dequeue();
                        if (cancellationToken.IsCancellationRequested)
                            return data;

                        reporter.ReportProgressAsync(() =>
                        {
                            log += string.Format("\t\tDownloading: {0,-40}", file.Name);
                        });

                        //do the download here
                        string remotePath = file.FullName;
                        session.GetFiles(session.EscapeFileMask(remotePath), Path.Combine(localPath, file.Name)).Check();
                        data.files -= 1;
                        data.size -= file.Length;
                        skin.files.Remove(file);
                        reporter.ReportProgressAsync(() =>
                        {
                            log += string.Format("Done\n");
                            this.skins = data.skinCount.ToString().PadLeft(3);
                            this.files = data.files.ToString().PadLeft(3);
                            if (data.size == 0)
                                this.size = "0 b";
                            else
                                this.size = ByteSize.FromBytes(data.size).ToString();
                        });

                    }
                    data.skins.Remove(skin);
                    data.skinCount -= 1;
                    reporter.ReportProgressAsync(() =>
                    {
                        this.skins = data.skinCount.ToString().PadLeft(3);
                        this.files = data.files.ToString().PadLeft(3);
                        if (data.size == 0)
                            this.size = "0 b";
                        else
                            this.size = ByteSize.FromBytes(data.size).ToString();
                    });
                }
            }
            return data;
        }

        private void addJob()
        {
            Messenger.Default.Send<NotificationMessage<JobItem>>(new NotificationMessage<JobItem>(null, "addJob Show"));
        }

        private void findPath()
        {
            var dialog = new VistaFolderBrowserDialog();
            dialog.ShowNewFolderButton = false;
            dialog.SelectedPath = acPath;
            bool? dr = dialog.ShowDialog();
            if (dr == true)
                acPath = dialog.SelectedPath;

            string path = AppDomain.CurrentDomain.BaseDirectory;
            string json = JsonConvert.SerializeObject(acPath, Formatting.Indented);
            System.IO.File.WriteAllText(path + "/acPath.json", json);
        }

        private void switchToUpload()
        {
            var files = new List<string>();
            DirectoryInfo tPath = Directory.GetParent(acPath);
            string path = tPath.Parent.FullName;

            Messenger.Default.Send<NotificationMessage<string>>(new NotificationMessage<string>(path, "uploadSkin Show"));
        }

        ////public override void Cleanup()
        ////{
        ////    // Clean up if needed

        ////    base.Cleanup();
        ////}
    }
}