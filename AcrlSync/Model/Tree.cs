using System.Collections.Generic;
using WinSCP;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using GalaSoft.MvvmLight.Messaging;

namespace AcrlSync.Model
{
    /// <summary>
    /// tree model and viewmodel all together...
    /// </summary>
    public class Tree
    {

        readonly List<Tree> _children = new List<Tree>();
        public IList<Tree> Children { get { return _children; } }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Parent { get; set; }

        private async void GetTree(string root)
        {
            List<RemoteFileInfo> fileData = await GetSeasons("/"+root);

            if (fileData == null)
            {
                Messenger.Default.Send<NotificationMessage<string>>(new NotificationMessage<string>(root,"Connection Failure"));
                return;
            }

            Name = root;
            FullName = root;
            Tree parent;
            Tree parent2 = null;

            foreach (var dir in fileData)
            {
                // if (dir.Count > 1)
                // {
                //     Children.Add(new Tree(dir[0].Name, dir[0].FullName,Name));
                //     dir.Remove(dir[0]);
                //     parent = Children.Last();
                //     foreach (var dir2 in dir)
                //     {
                //         if (parent2 != null && dir2.FullName.StartsWith(parent2.FullName))
                //         {
                //             parent2.Children.Add(new Tree(dir2.Name, dir2.FullName, parent2.Name));
                //         }
                //         else
                //         {
                //             parent.Children.Add(new Tree(dir2.Name, dir2.FullName, parent.Name));
                //             parent2 = parent.Children.Last();
                //         }
                //     }
                // }
            }
            Messenger.Default.Send<NotificationMessage<string>>(new NotificationMessage<string>(root,"Tree Loaded"));
        }

        private Task<List<RemoteFileInfo>> GetSeasons(string root)
        {
            Task<List<RemoteFileInfo>> t = new Task<List<RemoteFileInfo>>(() => 
            {
                using (Session session = new Session())
                {
                    try
                    { session.Open(ConnectionSettings.Options); }
                    catch(WinSCP.SessionRemoteException e)
                    {
                        System.Console.WriteLine(e.Message);
                        return null;
                    }
                    string remotePath = root;

                    List<RemoteFileInfo> seasons = new List<RemoteFileInfo>();

                    // Get list of files in the directory
                    RemoteDirectoryInfo directoryInfo = session.ListDirectory(remotePath);

                    foreach (RemoteFileInfo item in directoryInfo.Files)
                    {
                        if (item.IsDirectory && item.Name != "..")
                        {
                            RemoteDirectoryInfo directoryInfoTwo = session.ListDirectory(item.FullName);

                            foreach (RemoteFileInfo itemTwo in directoryInfoTwo.Files)
                            {
                                if (itemTwo.IsDirectory && itemTwo.Name != "..")
                                {
                                    RemoteDirectoryInfo directoryInfoThree = session.ListDirectory(itemTwo.FullName);
                                    foreach (RemoteFileInfo itemThree in directoryInfoThree.Files)
                                    {
                                        if (itemThree.IsDirectory && itemThree.Name != "..")
                                        {
                                            seasons.Add(itemThree);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return(seasons);
                }
            });
            t.Start();
            return t;
        }


        public Tree(string root)
        {
            GetTree(root);
        }

        public Tree(string name, string fullName, string parent)
        {
            Name = name;
            FullName = fullName;
            Parent = parent;
        }
    }
}
