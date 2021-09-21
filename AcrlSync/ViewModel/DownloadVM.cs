using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;
using AcrlSync.Model;
using System.Collections.ObjectModel;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using ByteSizeLib;
using WinSCP;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CSharp.RuntimeBinder;
using Ookii.Dialogs.Wpf;

namespace AcrlSync.ViewModel
{
    public class OptionItem : ViewModelBase
    {
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                RaisePropertyChanged(() => Name);
            }
        }
        public string DownloadPath
        {
            get { return _downloadPath; }
            set
            {
                _downloadPath = value;
                RaisePropertyChanged(() => DownloadPath);
            }
        }
        public bool? IsChecked
        {
            get { return _isChecked; }
            set
            {
                _isChecked = value;
                RaisePropertyChanged(() => IsChecked);
            }
        }

        public string Game
        {
            get
            {
                int pos = _downloadPath.LastIndexOf("/", StringComparison.Ordinal) + 1;
                if (pos != 0 && _downloadPath.Length > pos+3 && _downloadPath.Substring(pos, 3) == "ACC")
                {
                    return "ACC";
                }

                return "AC";
            }
        }

        private string _name;
        private string _downloadPath;
        private bool? _isChecked;

        public OptionItem()
        {
            _name = "";
            _downloadPath = "";
        }
    }

    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// See http://www.mvvmlight.net
    /// </para>
    /// </summary>
    public class DownloadVM : ViewModelBase, IDisposable
    {
        private readonly IFtpService _dataService;

        private CancellationTokenSource cancellationTokenSource;

        private ObservableCollection<JobItem> _jobs;

        public ObservableCollection<JobItem> Jobs
        {
            get { return _jobs; }
            set
            {
                _jobs = value;
                RaisePropertyChanged(() => Jobs);
            }
        }

        private JobItem _selectedJob;

        public JobItem SelectedJob
        {
            get { return _selectedJob; }
            set
            {
                _selectedJob = value;
                RaisePropertyChanged(() => SelectedJob);
            }
        }

        private string _log;

        public string Log
        {
            get { return _log; }
            set
            {
                _log = value;
                RaisePropertyChanged(() => Log);
            }
        }

        private string _skins;

        public string Skins
        {
            get { return _skins; }
            set
            {
                _skins = value;
                RaisePropertyChanged(() => Skins);
            }
        }

        private string _analysisText;

        public string AnalysisText
        {
            get { return _analysisText; }
            set
            {
                _analysisText = value;
                RaisePropertyChanged(() => AnalysisText);
            }
        }

        private string _runText;

        public string RunText
        {
            get { return _runText; }
            set
            {
                _runText = value;
                RaisePropertyChanged(() => RunText);
            }
        }

        private string _size;

        public string Size
        {
            get { return _size; }
            set
            {
                _size = value;
                RaisePropertyChanged(() => Size);
            }
        }

        private string _files;

        public string Files
        {
            get { return _files; }
            set
            {
                _files = value;
                RaisePropertyChanged(() => Files);
            }
        }

        private bool _treeLoaded;

        public bool TreeLoaded
        {
            get { return _treeLoaded; }
            set
            {
                _treeLoaded = value;
                RaisePropertyChanged(() => TreeLoaded);
            }
        }

        private string _loading;

        public string Loading
        {
            get { return _loading; }
            set
            {
                _loading = value;
                RaisePropertyChanged(() => Loading);
            }
        }

        private string _ftpAddress;

        public string FtpAddress
        {
            get { return _ftpAddress; }
            set
            {
                _ftpAddress = value;
                RaisePropertyChanged(() => FtpAddress);

                // Check valid before this and redo tree loading
                if (value != null)
                    ConnectionSettings.SetHost(value);
            }
        }

        private string _ftpError;

        public string FtpError
        {
            get { return _ftpError; }
            set
            {
                _ftpError = value;
                RaisePropertyChanged(() => FtpError);
            }
        }

        private bool _ftpLoaded;

        public bool FtpLoaded
        {
            get { return _ftpLoaded; }
            set
            {
                _ftpLoaded = value;
                RaisePropertyChanged(() => FtpLoaded);
            }
        }

        private Dictionary<string, string> _paths;

        private string getPath(string game)
        {
            if (!_paths.ContainsKey(game))
            {
                return "";
            }

            return _paths[game];
        }

        private void setPath(string game, string path)
        {
            if (_paths.ContainsKey(game))
            {
                _paths[game] = path;
            }
            else
            {
                _paths.Add(game, path);
            }

            RaisePropertyChanged(game + "Path");
        }

        public string ACPath
        {
            get => getPath("AC");
            set => setPath("AC", value);
        }

        public string ACCPath
        {
            get => getPath("ACC");
            set => setPath("ACC", value);
        }

        private string _exclusionString;
        public string ExclusionString
        {
            get => _exclusionString;
            set { _exclusionString = value; RaisePropertyChanged(() => ExclusionString); }
        }


        private bool _analyseInProgress;
        private bool _runInProgress;

        private JobItem _analysedjob;
        private AnalysisItem analysis;

        public RelayCommand FtpClick { get; set; }
        public RelayCommand EditClick { get; set; }
        public RelayCommand AddClick { get; set; }
        public RelayCommand AnalyseClick { get; set; }
        public RelayCommand RunClick { get; set; }
        public RelayCommand FindAcClick { get; }
        public RelayCommand FindAccClick { get; }

        private List<Tree> _seasons;
        public List<Tree> Seasons
        {
            get { return _seasons; }
            set
            {
                _seasons = value;
                RaisePropertyChanged(() => Seasons);
            }
        }

        private List<OptionItem> _options;
        public List<OptionItem> Options
        {
            get { return _options; }
            set
            {
                _options = value;
                RaisePropertyChanged(() => Options);

            }
        }

        private bool CheckCarsPath(string game)
        {
            bool exists = Directory.Exists(getPath(game));

            if (!exists)
            {
                System.Windows.Forms.MessageBox.Show(string.Format("The \"{0}\" Cars Path: \"{1}\" doesn't exist.\n\nPlease create the folder or update the path.", game, getPath(game)), "Cars folder not found.",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }

            return exists;
        }

        /// <summary>
        /// Initializes a new instance of the DownloadVM class.
        /// </summary>
        public DownloadVM(IFtpService dataService)
        {
            _ftpAddress = ConnectionSettings.Options.HostName;
            _paths = new Dictionary<string, string>();
            setPath("AC", Model.Jobs.acCarsPath);
            setPath("ACC", Model.Jobs.accCarsPath);

            LoadConnectionJson();
            LoadSettingsJson();

            _seasons = new List<Tree>();
            var item = new Tree("Download");
            _seasons.Add(item);

            _options = new List<OptionItem>();

            AnalyseClick = new RelayCommand(AnalyseJob);
            RunClick = new RelayCommand(RunJob);
            FtpClick = new RelayCommand(Reinitialise);
            FindAcClick = new RelayCommand(FindAcPath);
            FindAccClick = new RelayCommand(FindAccPath);

            _dataService = dataService;
            _log = string.Empty;

            _loading = "Loading data from FTP";
            _treeLoaded = false;
            _ftpLoaded = false;
            _ftpError = "Connecting";

            _analyseInProgress = false;
            _runInProgress = false;

            _analysisText = "_Analyse";
            _runText = "_Run";

            Messenger.Default.Register<NotificationMessage<string>>(this, (message) =>
            {
                if (message.Content == "Download")
                {
                    if (message.Notification == "Tree Loaded")
                    {
                        Loading = "";
                        TreeLoaded = true;
                        FtpLoaded = true;
                        FtpError = "";

                        List<OptionItem> list = new List<OptionItem>();
                        foreach (Tree s in _seasons)
                        {
                            FlattenTree(s, "", list);
                        }
                        Options = list;
                    }
                    if (message.Notification == "Connection Failure")
                    {
                        Loading = "";
                        TreeLoaded = false;
                        FtpLoaded = true;
                        FtpError = "Could not Connect";
                        string errorMessage = "Could not connect to FTP: " + ConnectionSettings.Options.HostName;
                        System.Windows.MessageBox.Show(errorMessage, "Connection Failure", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            });
        }

        ~DownloadVM()
        {
            // Write the setting to disk. No need to save connection that updates on edit.
            SaveSettingsJson();
            _paths = new Dictionary<string, string>();
        }

        private void FlattenTree(Tree input, string parent, List<OptionItem> output, int level = 0)
        {
            int maxLevel = 2;

            if (level == maxLevel)
            {
                OptionItem item = new OptionItem
                {
                    Name = String.Format("{0} \u2013 {1}", parent, input.Name),
                    DownloadPath = input.FullName,
                    IsChecked = false
                };
                output.Add(item);
            }
            else
            {
                foreach (Tree child in input.Children)
                    FlattenTree(child, input.Name, output, level + 1);
            }
        }

        private void Reinitialise()
        {
            SaveConnectionJson();
            Loading = "Loading data from FTP";
            TreeLoaded = false;
            FtpLoaded = false;
            FtpError = "Connecting";

            Options = new List<OptionItem>();

            _seasons = new List<Tree>();
            var item = new Tree("Download");
            _seasons.Add(item);

            AnalysisText = "A_nalyse";
            RunText = "_Run";
        }

        private void SaveConnectionJson()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            string json = JsonConvert.SerializeObject(ConnectionSettings.Options, Formatting.Indented);
            System.IO.File.WriteAllText(path + "/connection.json", json);
        }

        private void LoadConnectionJson()
        {
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory;
                string json = System.IO.File.ReadAllText(path + "/connection.json");
                SessionOptions temp = JsonConvert.DeserializeObject<SessionOptions>(json);
                ConnectionSettings.Options.HostName = temp.HostName;
                ConnectionSettings.Options.Protocol = temp.Protocol;
                ConnectionSettings.Options.UserName = temp.UserName;
                ConnectionSettings.Options.Password = temp.Password;
                _ftpAddress = temp.HostName;
            }
            catch (FileNotFoundException)
            {
                SaveConnectionJson();
            }
        }

        private void LoadSettingsJson()
        {
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory;
                string json = System.IO.File.ReadAllText(path + "/settings.json");
                GeneralSettings settings = JsonConvert.DeserializeObject<GeneralSettings>(json);
                setPath("AC", settings.AcCarsDirectory);
                setPath("ACC", settings.AccCarsDirectory);
                if (settings.ExcludedSkins.Length > 0)
                {
                    _exclusionString = string.Join(":", settings.ExcludedSkins);
                }
            }
            catch(FileNotFoundException)
            {
                _paths = new Dictionary<string, string>();
                _exclusionString = "";
                SaveSettingsJson();
            }
        }

        private void SaveSettingsJson()
        {
            var GeneralSettings = new GeneralSettings(getPath("AC"), getPath("ACC"), ExclusionString);
            string json = JsonConvert.SerializeObject(GeneralSettings, Formatting.Indented);
            string path = AppDomain.CurrentDomain.BaseDirectory;
            System.IO.File.WriteAllText(path + "/settings.json", json);
        }

        private async void AnalyseJob()
        {
            if (_runInProgress)
                return;

            if (_analyseInProgress)
            {
                cancellationTokenSource.Cancel();
                return;
            }

            SelectedJob = new JobItem();
            foreach (OptionItem item in Options)
            {
                if (item.IsChecked == true)
                {
                    if (!CheckCarsPath(item.Game))
                    {
                        return;
                    }
                    
                    SelectedJob.Items.Add(new Item(item.DownloadPath, item.Game));
                }
            }
            if (SelectedJob.Items.Count < 1)
            {
                System.Windows.MessageBox.Show("No series selected", "Check some series", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation);
                return;
            }

            if (SelectedJob == null)
            {
                System.Windows.MessageBox.Show("A job must be selected", "Select a Job", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation);
                return;
            }

            FtpLoaded = false;

            AnalysisText = "_Cancel";
            _analyseInProgress = true;
            Skins = "  0";
            Files = "  0";
            Size = "0 b";
            Log += "*******************************************************************\n";
            Log += "****                     ANALYSIS  STARTED                     ****\n";
            Log += "*******************************************************************\n";
            _analysedjob = SelectedJob;
            analysis = await AnalyseFtp(SelectedJob);
            if (analysis == null)
            {
                Log += "*******************************************************************\n";
                Log += "****                     ANALYSIS  FAILED                      ****\n";
                Log += "*******************************************************************\n";
                Messenger.Default.Send<NotificationMessage>(new NotificationMessage("Connection Failure"));
                AnalysisText = "A_nalyse";
                _analyseInProgress = false;
                FtpLoaded = true;
                return;
            }
            Skins = analysis.SkinCount.ToString().PadLeft(3);
            Files = analysis.Files.ToString().PadLeft(3);
            if (analysis.Size == 0)
                Size = "0 b";
            else
                Size = ByteSize.FromBytes(analysis.Size).ToString();
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                Log += "*******************************************************************\n";
                Log += "****                     ANALYSIS COMPLETE                     ****\n";
                Log += "*******************************************************************\n";
            }
            else
            {
                Log += "*******************************************************************\n";
                Log += "****                     ANALYSIS  ABORTED                     ****\n";
                Log += "*******************************************************************\n";
            }
            AnalysisText = "A_nalyse";
            _analyseInProgress = false;
            FtpLoaded = true;
        }

        private Task<AnalysisItem> AnalyseFtp(JobItem job)
        {
            var progressReporter = new ProgressReporter();
            Task<AnalysisItem> t = new Task<AnalysisItem>(() => BackgroundAnalyse(job, progressReporter));
            t.Start();
            return t;
        }

        private Task<AnalysisItem> RunFtp(AnalysisItem data, JobItem job)
        {
            var progressReporter = new ProgressReporter();
            Task<AnalysisItem> t = new Task<AnalysisItem>(() => BackgroundRun(data, job, progressReporter));
            t.Start();
            return t;
        }

        /// <summary>
        /// Creates an AnalysisItem with all the files that need to be downloaded.
        /// </summary>
        /// <param name="job">The selection of folders to analyse</param>
        /// <param name="reporter">Where we will post our output messages</param>
        /// <returns></returns>
        private AnalysisItem BackgroundAnalyse(JobItem job, ProgressReporter reporter)
        {
            AnalysisItem data = new AnalysisItem
            {
                SkinCount = 0,
                Files = 0,
                Size = 0
            };
            cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            RemoteDirectoryInfo carInfo; // WinSCP API object that represents a list of car folders.
            RemoteDirectoryInfo skinInfo; // WinSCP API object that represents the contents of the car/skins folder.
            RemoteDirectoryInfo skinFiles; // WinSCP API object that represents the contents of a skin folder.

            List<string> exclusionPatterns;
            if (String.IsNullOrWhiteSpace(ExclusionString)) {
                exclusionPatterns = new List<string>();
            } else {
                exclusionPatterns = ExclusionString.Split(':').ToList().ConvertAll(x => {
                    x = x.ToLowerInvariant().Replace('/', '\\'); // Make the user pattern lowercase and windows dir separated.
                    var regexParts = x.Split('*').ToList().ConvertAll(y => Regex.Escape(y)); // Split pattern into parts between wildcards and escape those parts.
                    return String.Join(".*", regexParts.ToArray()); // Reassemble the parts into a regex pattern with the proper regex wildcard.
                    });
            }

            using (Session session = new Session())
            {
                // Connect to the FTP.
                try
                { session.Open(ConnectionSettings.Options); }
                catch (WinSCP.SessionRemoteException e)
                {
                    System.Console.WriteLine(e.Message);
                    return null;
                }
                
                foreach (Item item in job.Items)
                {
                    string localPath;
                    // Open the folder for the series and get the list of car folders.
                    try {
                        carInfo = session.ListDirectory(item.FTPPath);
                    }
                    catch (WinSCP.SessionRemoteException e) {
                        Log += string.Format("{0}\n", e.Message);
                        System.Console.WriteLine(e.Message);
                        continue;
                    }
                    
                    // Iterate over the cars.
                    foreach (RemoteFileInfo car in carInfo.Files)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return data;
                        if (car.IsDirectory && car.Name != "..")
                        {
                            reporter.ReportProgressAsync(() =>
                            {
                                Log += string.Format("Car: {0}\n", car.Name);
                            });

                            // Navigate to the skins folder and get a list its contents
                            localPath = Path.Combine(getPath(item.Game), car.Name, "skins");
                            try {
                                skinInfo = session.ListDirectory(car.FullName + "/skins");
                            }
                            catch (WinSCP.SessionRemoteException e) {
                                // No Skins folder! Issue lies with the file struct on server.
                                // Tell user to inform a moderator.
                                Log += string.Format("Error: {0} could not access skins folder. Skipping car, please inform a moderator.\n", car.Name);
                                System.Console.WriteLine(e.Message);
                                continue;
                            }
                            
                            // Iterate over the skins.
                            foreach (RemoteFileInfo skinDir in skinInfo.Files)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                    return data;

                                // Only interested in the directories.
                                if (skinDir.IsDirectory && skinDir.Name != "..")
                                {
                                    reporter.ReportProgressAsync(() =>
                                    {
                                        Log += string.Format("\tSkin: {0,-40}", skinDir.Name);
                                    });
                                    localPath = Path.Combine(getPath(item.Game), car.Name, "skins", skinDir.Name);
                                    if (IsExcluded(exclusionPatterns, Path.Combine(car.Name, "skins", skinDir.Name)))
                                    {
                                        // Skin is Excluded! Skip it
                                        reporter.ReportProgressAsync(() =>
                                        {
                                            Log += string.Format("Skin has been excluded\n");
                                        });
                                    }
                                    else if (!Directory.Exists(localPath))
                                    {
                                        // Skin does not exist on users system, mark all files for download.
                                        reporter.ReportProgressAsync(() =>
                                        {
                                            Log += string.Format("Not found in cars folder\n");
                                        });
                                        Skin skin = new Skin
                                        {
                                            Name = skinDir.Name,
                                            Car = car.Name,
                                            Game = item.Game
                                        };
                                        data.SkinCount += 1;
                                        try { // Don't think this one can actually fail. As listing contents of dir we know exists.
                                            skinFiles = session.ListDirectory(skinDir.FullName);
                                        }
                                        catch (WinSCP.SessionRemoteException e) {
                                            System.Console.WriteLine(e.Message);
                                            continue;
                                        }
                                        List<RemoteFileInfo> remoteFiles = new List<RemoteFileInfo>();
                                        foreach (RemoteFileInfo file in skinFiles.Files)
                                        {
                                            if (cancellationToken.IsCancellationRequested)
                                                return data;
                                            if (!file.IsDirectory && file.Name != "..")
                                            {
                                                remoteFiles.Add(file);
                                                data.Files += 1;
                                                data.Size += file.Length;

                                                reporter.ReportProgressAsync(() =>
                                                {
                                                    Skins = data.SkinCount.ToString().PadLeft(3);
                                                    Files = data.Files.ToString().PadLeft(3);
                                                    if (data.Size == 0)
                                                        Size = "0 b";
                                                    else
                                                        Size = ByteSize.FromBytes(data.Size).ToString();
                                                });
                                            }
                                        }
                                        skin.Files = remoteFiles;
                                        data.Skins.Add(skin);
                                    }
                                    else
                                    {
                                        // Skin exists on users system check each file in turn.
                                        skinFiles = session.ListDirectory(skinDir.FullName);
                                        List<RemoteFileInfo> remoteFiles = new List<RemoteFileInfo>();
                                        foreach (RemoteFileInfo file in skinFiles.Files)
                                        {
                                            if (cancellationToken.IsCancellationRequested)
                                                return data;
                                            if (!file.IsDirectory && file.Name != "..")
                                            {
                                                localPath = Path.Combine(getPath(item.Game), car.Name, "skins", skinDir.Name, file.Name);
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
                                                    remoteFiles.Add(file);
                                                    data.Files += 1;
                                                    data.Size += file.Length;
                                                }
                                            }
                                        }
                                        if (remoteFiles.Count > 0)
                                        {
                                            reporter.ReportProgressAsync(() =>
                                            {
                                                Log += string.Format("Has new or updated files\n");
                                                Skins = data.SkinCount.ToString().PadLeft(3);
                                                Files = data.Files.ToString().PadLeft(3);
                                                if (data.Size == 0)
                                                    Size = "0 b";
                                                else
                                                    Size = ByteSize.FromBytes(data.Size).ToString();
                                            });
                                            Skin skin = new Skin
                                            {
                                                Name = skinDir.Name,
                                                Car = car.Name,
                                                Game = item.Game
                                            };
                                            data.SkinCount += 1;
                                            skin.Files = remoteFiles;
                                            data.Skins.Add(skin);
                                        }
                                        else
                                        {
                                            reporter.ReportProgressAsync(() =>
                                            {
                                                Log += string.Format("\n");
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

        private bool IsExcluded(List<string> exclusionPatterns, string path)
        {
            path = path.ToLowerInvariant();
            foreach(string pattern in exclusionPatterns)
            {
                if (Regex.IsMatch(path, pattern))
                    return true;
            }
            return false;
        }

        private async void RunJob()
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

            RunText = "_Cancel";
            _runInProgress = true;
            FtpLoaded = false;

            Log += "*******************************************************************\n";
            Log += "****                     DOWNLOAD  STARTED                     ****\n";
            Log += "*******************************************************************\n";
            analysis = await RunFtp(analysis, _analysedjob);
            Skins = analysis.SkinCount.ToString().PadLeft(3);
            Files = analysis.Files.ToString().PadLeft(3);
            if (analysis.Size == 0)
                Size = "0 b";
            else
                Size = ByteSize.FromBytes(analysis.Size).ToString();
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                Log += "*******************************************************************\n";
                Log += "****                     DOWNLOAD COMPLETE                     ****\n";
                Log += "*******************************************************************\n";
            }
            else
            {
                Log += "*******************************************************************\n";
                Log += "****                     DOWNLOAD  ABORTED                     ****\n";
                Log += "*******************************************************************\n";
            }
            RunText = "_Run";
            _runInProgress = false;
            FtpLoaded = true;
        }

        private AnalysisItem BackgroundRun(AnalysisItem data, JobItem job, ProgressReporter reporter)
        {
            cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            using (Session session = new Session())
            {
                session.Open(ConnectionSettings.Options);

                string lastCar = null;
                Queue<Skin> skinQueue = new Queue<Skin>(data.Skins);

                while (skinQueue.Count > 0)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return data;

                    Skin skin = skinQueue.Dequeue();
                    if (skin.Car != lastCar)
                        reporter.ReportProgressAsync(() =>
                        {
                            Log += string.Format("Car: {1}\n\tSkin: {0,-40}\n", skin.Name, skin.Car);
                        });
                    else
                        reporter.ReportProgressAsync(() =>
                        {
                            Log += string.Format("\tSkin: {0,-40}\n", skin.Name);
                        });

                    string localPath = Path.Combine(getPath(skin.Game), skin.Car, "skins", skin.Name);
                    Directory.CreateDirectory(localPath);
                    Queue<RemoteFileInfo> fileQueue = new Queue<RemoteFileInfo>(skin.Files);
                    while (fileQueue.Count > 0)
                    {
                        var file = fileQueue.Dequeue();
                        if (cancellationToken.IsCancellationRequested)
                            return data;

                        reporter.ReportProgressAsync(() =>
                        {
                            Log += string.Format("\t\tDownloading: {0,-40}", file.Name);
                        });

                        //do the download here
                        string remotePath = file.FullName;
                        session.GetFiles(session.EscapeFileMask(remotePath), Path.Combine(localPath, file.Name)).Check();
                        data.Files -= 1;
                        data.Size -= file.Length;
                        skin.Files.Remove(file);
                        reporter.ReportProgressAsync(() =>
                        {
                            Log += string.Format("Done\n");
                            Skins = data.SkinCount.ToString().PadLeft(3);
                            Files = data.Files.ToString().PadLeft(3);
                            if (data.Size == 0)
                                Size = "0 b";
                            else
                                Size = ByteSize.FromBytes(data.Size).ToString();
                        });

                    }
                    data.Skins.Remove(skin);
                    data.SkinCount -= 1;
                    reporter.ReportProgressAsync(() =>
                    {
                        Skins = data.SkinCount.ToString().PadLeft(3);
                        Files = data.Files.ToString().PadLeft(3);
                        if (data.Size == 0)
                            Size = "0 b";
                        else
                            Size = ByteSize.FromBytes(data.Size).ToString();
                    });
                }
            }
            return data;
        }

        private void AddJob()
        {
            Messenger.Default.Send<NotificationMessage<JobItem>>(new NotificationMessage<JobItem>(null, "addJob Show"));
        }

        private void FindAcPath()
        {
            var dialog = new VistaFolderBrowserDialog
            {
                ShowNewFolderButton = false,
                SelectedPath = getPath("AC")
            };
            bool? dr = dialog.ShowDialog();
            if (dr == true)
                setPath("AC", dialog.SelectedPath);
            SaveSettingsJson();
        }

        private void FindAccPath()
        {
            var dialog = new VistaFolderBrowserDialog
            {
                ShowNewFolderButton = false,
                SelectedPath = getPath("ACC")
            };
            bool? dr = dialog.ShowDialog();
            if (dr == true)
                setPath("ACC", dialog.SelectedPath);
            SaveSettingsJson();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool all)
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }
        }
    }
}