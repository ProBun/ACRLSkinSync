using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using WinSCP;

namespace AcrlSync.Model
{
    public class FtpService : IFtpService
    {

        public void GetTree(string directory, Action<Exception, List<List<RemoteFileInfo>>> callback)
        {
            using (Session session = new Session())
            {
                session.Open(ConnectionSettings.Options);

                string remotePath = "/" + directory;
               
                List<List<RemoteFileInfo>> allFiles = new List<List<RemoteFileInfo>>();

                // Get list of files in the directory
                RemoteDirectoryInfo directoryInfo = session.ListDirectory(remotePath);

                foreach (RemoteFileInfo item in directoryInfo.Files)
                {
                    if (item.IsDirectory && item.Name != "..")
                    {
                        List<RemoteFileInfo> files = new List<RemoteFileInfo>();
                        RemoteDirectoryInfo directoryInfoTwo = session.ListDirectory(item.FullName);
                        files.Add(item);
                        foreach (RemoteFileInfo itemTwo in directoryInfoTwo.Files)
                        {
                            if (itemTwo.IsDirectory && itemTwo.Name != "..")
                            {
                                files.Add(itemTwo);
                            }
                        }
                        allFiles.Add(files);
                    }
                }

            callback(null, allFiles);
            }
        }

        public void GetDirectoryDetails(string directory)
        {
            using (Session session = new Session())
            {
                // Connect
                try
                { session.Open(ConnectionSettings.Options); }
                catch (WinSCP.SessionRemoteException e)
                {
                    System.Console.WriteLine(e.Message);

                    return;
                }

                string remotePath = "/"+directory;

                List<RemoteFileInfo> files = new List<RemoteFileInfo>();
                Queue<string> folders = new Queue<string>();
                folders.Enqueue(remotePath);

                while (folders.Count > 0)
                {
                    String fld = folders.Dequeue();

                    // Get list of files in the directory
                    RemoteDirectoryInfo directoryInfo = session.ListDirectory(fld);

                    foreach (RemoteFileInfo item in directoryInfo.Files)
                    {
                        if (item.IsDirectory && item.Name != "..")
                        {
                            folders.Enqueue(item.FullName);
                        }
                        else
                        {
                            files.Add(item);
                        }
                    }
                }

                // Download the selected file
                //localPath = @"C:\local\path\";
                //session.GetFiles(session.EscapeFileMask(remotePath + latest.Name), localPath).Check();
            }
        }
    }
}