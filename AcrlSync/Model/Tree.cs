using System.Collections.Generic;
using WinSCP;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;

namespace AcrlSync.Model
{
    /// <summary>
    /// tree model and viewmodel all together...
    /// </summary>
    public class Tree
    {

        readonly List<Tree> _children = new List<Tree>();
        public IList<Tree> children { get { return _children; } }
        public string Name { get; set; }
        public string fullName { get; set; }

        private async void GetTree()
        {
            List<List<RemoteFileInfo>> FileData = await backgroundGetTree();

            if (FileData == null)
            {
                Messenger.Default.Send<NotificationMessage>(new NotificationMessage("Connection Failure"));
                return;
            }

            this.Name = "Download";
            this.fullName = "Download";
            Tree parent;

            foreach (var dir in FileData)
            {
                if (dir.Count > 1)
                {
                    children.Add(new Tree(dir[0].Name, dir[0].FullName));
                    dir.Remove(dir[0]);
                    parent = children.Last();
                    foreach (var dir2 in dir)
                    {
                        parent.children.Add(new Tree(dir2.Name, dir2.FullName));
                    }
                }
            }
            Messenger.Default.Send<NotificationMessage>(new NotificationMessage("Tree Loaded"));
        }

        private Task<List<List<RemoteFileInfo>>> backgroundGetTree()
        {
            Task<List<List<RemoteFileInfo>>> t = new Task<List<List<RemoteFileInfo>>>(() => 
            {
                using (Session session = new Session())
                {
                    try
                    { session.Open(ConnectionSettings.options); }
                    catch(WinSCP.SessionRemoteException e)
                    {
                        System.Console.WriteLine(e.Message);
                        return null;
                    }
                    string remotePath = "/Download";

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
                    return(allFiles);
                }
            });
            t.Start();
            return t;
        }


        public Tree()
        {
            GetTree();
        }

        public Tree(string name, string fName)
        {
            Name = name;
            fullName = fName;
        }
    }
}
