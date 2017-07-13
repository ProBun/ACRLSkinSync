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

        static public SessionOptions options { get { return sessionOptions; } }
        static public void setHost(string ip)
        {
            sessionOptions.HostName = ip;
        }
    }
    
    static public class Jobs
    {
        static public string acCarsPath = "acPath";
        static public void setPath(string path)
        {
            acCarsPath = path;
        }
        //public IList<string> ftpPath { get; set; }
    }

    public class JobItem
    {
        public string name { get; set; }
        public List<string> ftpPath { get; set; }
        public string acCarsPath { get; set; }

        public JobItem()
        {
            ftpPath = new List<string>();
        }
    }

    public class AnalysisItem
    {
        public int skinCount { get; set; }
        public int files { get; set; }
        public long size { get; set; }

        public List<Skin> skins { get; set; }
        public AnalysisItem()
        {
            skins = new List<Skin>();
        }
    }

    public class Skin
    {
        public string name { get; set; }
        public string car { get; set; }
        public List<RemoteFileInfo> files {get; set;}

        public Skin()
        {
            files = new List<RemoteFileInfo>();
        }
    }
}