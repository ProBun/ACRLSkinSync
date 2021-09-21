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
    public class JobVM : ViewModelBase
    {
        private List<string> _ExistingNames;

        public RelayCommand SelectClick { get; set; }
        public RelayCommand SaveClick { get; set; }
        public RelayCommand CancelClick { get; set; }
        public RelayCommand<Tree> TreeSelectionChange { get; set; }

        private bool _showErrors;
        public bool ShowErrors
        {
            get { return _showErrors; }
            set
            {
                _showErrors = value;
                RaisePropertyChanged(() => ShowErrors);
            }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                if (string.IsNullOrWhiteSpace(value))
                    NameErr = "Name is required.";
                else if (_ExistingNames.Contains(value))
                    NameErr = "Job already exists with this name.";
                else
                    NameErr = null;
                RaisePropertyChanged(() => Name);
            }
        }

        private string _nameErr;
        public string NameErr
        {
            get { return _nameErr; }
            set
            {
                _nameErr = value;
                RaisePropertyChanged(() => NameErr);
            }
        }

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

        private string _acPath;
        public string AcPath
        {
            get { return _acPath; }
            set
            {
                _acPath = value;
                if (string.IsNullOrWhiteSpace(value))
                    AcErr = "AC cars directory is required.";
                else
                    AcErr = null;
                RaisePropertyChanged(() => AcPath);
            }
        }

        private string _acErr;
        public string AcErr
        {
            get { return _acErr; }
            set
            {
                _acErr = value;
                RaisePropertyChanged(() => AcErr);
            }
        }

        private string _selected;
        public string Selected
        {
            get { return _selected; }
            set
            {
                _selected = value;
                if (string.IsNullOrWhiteSpace(value))
                    SelectedErr = "Sync folder must be selected.";
                else
                    SelectedErr = null;
                RaisePropertyChanged(() => Selected);
            }
        }

        private string _selectedErr;
        public string SelectedErr
        {
            get { return _selectedErr; }
            set
            {
                _selectedErr = value;
                RaisePropertyChanged(() => SelectedErr);
            }
        }
        
        private JobItem _editItem;

        /// <summary>
        /// Initializes a new instance of the jobVM class.
        /// </summary>
        public JobVM()
        {
            _ExistingNames = new List<string>();
            _name = "";
            _selected = "";
            _acPath = "";
            _showErrors = false;

            SelectClick = new RelayCommand(SelectACfolder);
            SaveClick = new RelayCommand(Save);
            CancelClick = new RelayCommand(Cancel);
            TreeSelectionChange = new RelayCommand<Tree>(TreeChange);

            _seasons = new List<Tree>();
            var item = new Tree("Download");
            _seasons.Add(item);

            Messenger.Default.Register<NotificationMessage<JobItem>>(this, (message) => {
                if (message.Notification == "job")
                {
                    if (message.Content!=null)
                    {
                        _editItem = message.Content;
                        Name = message.Content.Name;
                        //selected = message.Content.ftpPath;
                        ShowErrors = false;
                    }
                    else
                    {
                        _editItem = null;
                        Name = "";
                        Selected = "";
                        AcPath = "";
                        ShowErrors = false;
                    }
                }
            });
        }

        public void SelectACfolder()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                ShowNewFolderButton = false
            };
            DialogResult dr = dialog.ShowDialog();
            if (dr == DialogResult.OK)
                AcPath = dialog.SelectedPath;
        }

        public void TreeChange(Tree selectedTree)
        {
            Selected = selectedTree.FullName;
            if (selectedTree.Children.Count > 0)
                SelectedErr = "Selected Sync folder is not bottom level";
            else
                SelectedErr = null;

        }

        public void Save()
        {
            ShowErrors = (NameErr != null || AcErr != null || SelectedErr != null);

            if (ShowErrors)
                return;

            //do save and exit
            JobItem newJob = new JobItem()
            {
                Name = this.Name,
                //ftpPath = selected
            };

            Messenger.Default.Send<NotificationMessage<JobItem>>(new NotificationMessage<JobItem>(newJob, "closeJob"));
        }

        public void Cancel()
        {
            Messenger.Default.Send<NotificationMessage<JobItem>>(new NotificationMessage<JobItem>(_editItem, "closeJob"));
        }
    }
}