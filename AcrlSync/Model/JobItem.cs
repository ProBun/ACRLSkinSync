using System.Collections.Generic;
using System.Windows.Forms.VisualStyles;
using WinSCP;

namespace AcrlSync.Model
{
    static public class ConnectionSettings
    {
        static private SessionOptions sessionOptions = new SessionOptions
        {
            Protocol = Protocol.Ftp,
            HostName = "host",
            UserName = "user",
            Password = "pword",
        };

        static public SessionOptions Options { get { return sessionOptions; } }
        static public void SetHost(string ip)
        {
            sessionOptions.HostName = ip;
        }
    }

    public class GeneralSettings
    {
        private string _AcCarsDirectory;
        private string _AccCarsDirectory;
        private string[] _ExcludedSkins;

        public string AcCarsDirectory { get { return _AcCarsDirectory; } set { _AcCarsDirectory = value; } }
        public string AccCarsDirectory { get { return _AccCarsDirectory; } set { _AccCarsDirectory = value; } }
        public string[] ExcludedSkins { get { return _ExcludedSkins; } set { _ExcludedSkins = value; } }

        public GeneralSettings()
        {
            _AcCarsDirectory = "";
            _ExcludedSkins = null;
        }

        public GeneralSettings(string acCarDir, string accCarDir, string ExcludedStr)
        {
            _AcCarsDirectory = acCarDir;
            _AccCarsDirectory = accCarDir;
            _ExcludedSkins = ExcludedStr.Split(':');
        }
    }

    static public class Jobs
    {
        static public string acCarsPath = "acCarPath";
        static public string accCarsPath = "accCarPath";
        static public void SetCarsPath(string path)
        {
            acCarsPath = path;
        }
        static public string acPath = "acPath";
        static public void SetAcPath(string path)
        {
            acPath = path;
        }
        static public string accPath = "accPath";
        static public void SetAccPath(string path)
        {
            accPath = path;
        }
    }

    public class JobItem
    {
        public string Name { get; set; }
        public List<Item> Items { get; }

        public JobItem()
        {
            Items = new List<Item>();
        }
    }

    public class Item
    {
        public string FTPPath { get; set; }
        public string Game { get; set; }

        public Item(string ftpPath, string game)
        {
            FTPPath = ftpPath;
            Game = game;
        }
    }

    public class AnalysisItem
    {
        public int SkinCount { get; set; }
        public int Files { get; set; }
        public long Size { get; set; }

        public List<Skin> Skins { get; set; }
        public AnalysisItem()
        {
            Skins = new List<Skin>();
        }
    }

    public class Skin
    {
        public string Name { get; set; }
        public string Car { get; set; }
        public string Game { get; set; }
        public List<RemoteFileInfo> Files {get; set;}

        public Skin()
        {
            Files = new List<RemoteFileInfo>();
        }
    }
}