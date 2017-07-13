using System;
using System.Collections.Generic;
using WinSCP;

namespace AcrlSync.Model
{
    public interface IFtpService
    {
        void GetTree(string directory, Action<Exception, List<List<RemoteFileInfo>>> callback);
        void GetDirectoryDetails(string dir);
    }
}
