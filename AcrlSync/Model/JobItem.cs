using System.Collections.Generic;
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
        private string _CarsDirectory;
        private string[] _ExcludedSkins;

        public string CarsDirectory { get { return _CarsDirectory; } set { _CarsDirectory = value; } }
        public string[] ExcludedSkins { get { return _ExcludedSkins; } set { _ExcludedSkins = value; } }

        public GeneralSettings()
        {
            _CarsDirectory = "";
            _ExcludedSkins = null;
        }

        public GeneralSettings(string CarDir, string ExcludedStr)
        {
            _CarsDirectory = CarDir;
            _ExcludedSkins = ExcludedStr.Split(':');
        }
    }

    static public class Jobs
    {
        static public string acCarsPath = "acCarPath";
        static public void SetCarsPath(string path)
        {
            acCarsPath = path;
        }
        static public string acPath = "acPath";
        static public void SetPath(string path)
        {
            acPath = path;
        }
    }

    public class JobItem
    {
        public string Name { get; set; }
        public List<string> FtpPath { get; set; }
        public string AcCarsPath { get; set; }

        public JobItem()
        {
            FtpPath = new List<string>();
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
        public List<RemoteFileInfo> Files {get; set;}

        public Skin()
        {
            Files = new List<RemoteFileInfo>();
        }
    }
}