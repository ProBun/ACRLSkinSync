using AcrlSync.Model;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Forms;

namespace AcrlSync.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class jobVM : ViewModelBase
    {
        private List<string> _ExistingNames;

        public RelayCommand selectClick { get; set; }
        public RelayCommand saveClick { get; set; }
        public RelayCommand cancelClick { get; set; }
        public RelayCommand<Tree> treeSelectionChange { get; set; }

        private bool _showErrors;
        public bool showErrors
        {
            get { return _showErrors; }
            set
            {
                _showErrors = value;
                RaisePropertyChanged(() => showErrors);
            }
        }

        private string _name;
        public string name
        {
            get { return _name; }
            set
            {
                _name = value;
                if (string.IsNullOrWhiteSpace(value))
                    nameErr = "Name is required.";
                else if (_ExistingNames.Contains(value))
                    nameErr = "Job already exists with this name.";
                else
                    nameErr = null;
                RaisePropertyChanged(() => name);
            }
        }

        private string _nameErr;
        public string nameErr
        {
            get { return _nameErr; }
            set
            {
                _nameErr = value;
                RaisePropertyChanged(() => nameErr);
            }
        }

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

        private string _acPath;
        public string acPath
        {
            get { return _acPath; }
            set
            {
                _acPath = value;
                if (string.IsNullOrWhiteSpace(value))
                    acErr = "AC cars directory is required.";
                else
                    acErr = null;
                RaisePropertyChanged(() => acPath);
            }
        }

        private string _acErr;
        public string acErr
        {
            get { return _acErr; }
            set
            {
                _acErr = value;
                RaisePropertyChanged(() => acErr);
            }
        }

        private string _selected;
        public string selected
        {
            get { return _selected; }
            set
            {
                _selected = value;
                if (string.IsNullOrWhiteSpace(value))
                    selectedErr = "Sync folder must be selected.";
                else
                    selectedErr = null;
                RaisePropertyChanged(() => selected);
            }
        }

        private string _selectedErr;
        public string selectedErr
        {
            get { return _selectedErr; }
            set
            {
                _selectedErr = value;
                RaisePropertyChanged(() => selectedErr);
            }
        }
        
        private JobItem _editItem;

        /// <summary>
        /// Initializes a new instance of the jobVM class.
        /// </summary>
        public jobVM()
        {
            _ExistingNames = new List<string>();
            _name = "";
            _selected = "";
            _acPath = "";
            _showErrors = false;

            selectClick = new RelayCommand(selectACfolder);
            saveClick = new RelayCommand(save);
            cancelClick = new RelayCommand(cancel);
            treeSelectionChange = new RelayCommand<Tree>(TreeChange);

            _seasons = new List<Tree>();
            var item = new Tree();
            _seasons.Add(item);

            Messenger.Default.Register<NotificationMessage<JobItem>>(this, (message) => {
                if (message.Notification == "job")
                {
                    if (message.Content!=null)
                    {
                        _editItem = message.Content;
                        name = message.Content.name;
                        //selected = message.Content.ftpPath;
                        acPath = message.Content.acCarsPath;
                        showErrors = false;
                    }
                    else
                    {
                        _editItem = null;
                        name = "";
                        selected = "";
                        acPath = "";
                        showErrors = false;
                    }
                }
            });
        }

        public void selectACfolder()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.ShowNewFolderButton = false;
            DialogResult dr = dialog.ShowDialog();
            if (dr == DialogResult.OK)
                acPath = dialog.SelectedPath;
        }

        public void TreeChange(Tree selectedTree)
        {
            selected = selectedTree.fullName;
            if (selectedTree.children.Count > 0)
                selectedErr = "Selected Sync folder is not bottom level";
            else
                selectedErr = null;

        }

        public void save()
        {
            showErrors = (nameErr != null || acErr != null || selectedErr != null);

            if (showErrors)
                return;

            //do save and exit
            JobItem newJob = new JobItem()
            {
                name = this.name,
                acCarsPath = acPath,
                //ftpPath = selected
            };

            Messenger.Default.Send<NotificationMessage<JobItem>>(new NotificationMessage<JobItem>(newJob, "closeJob"));
        }

        public void cancel()
        {
            Messenger.Default.Send<NotificationMessage<JobItem>>(new NotificationMessage<JobItem>(_editItem, "closeJob"));
        }
    }
}