using Ini.Net;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Threading;
using System;
using SessionMapSwitcher.Classes.Events;

namespace SessionMapSwitcher.ViewModels
{
    public class ProjectWatcherViewModel : ViewModelBase
    {
        private const int burstDuration = 2000;

        private ComputerImportViewModel _importViewModel;

        private FileSystemWatcher _projectWatcher;

        public delegate void OnMapImported(object sender, MapImportedEventArgs e);
        public event OnMapImported MapImported;
        protected virtual void RaiseMapImported(string mapName) => 
            MapImported?.Invoke(this, new MapImportedEventArgs(mapName));

        private string _pathToProject;
        private string _statusText;
        private Brush _statusColor;

        public string PathToProject
        {
            get { return _pathToProject; }
            set
            {
                _pathToProject = value;
                NotifyPropertyChanged();
            }
        }

        public string StatusText
        {
            get { return _statusText; }
            set
            {
                _statusText = value;
                NotifyPropertyChanged();
            }
        }

        public Brush StatusColor
        {
            get { return _statusColor; }
            set
            {
                _statusColor = value;
                NotifyPropertyChanged();
            }
        }

        public string ProjectName
        {
            get
            {
                var slashIndex = PathToProject.LastIndexOf(@"\") + 1;
                var dotIndex = PathToProject.LastIndexOf(@".");
                return PathToProject.Substring(slashIndex, dotIndex - slashIndex);
            }
        }

        public string BaseProjectFolder
        {
            get
            {
                return PathToProject.Substring(0, PathToProject.LastIndexOf(@"\"));
            }
        }

        public string PathToCooked
        {
            get
            {
                return $@"{BaseProjectFolder}\Saved\Cooked\";
            }
        }

        public string PathToCookedContent
        {
            get
            {
                return $@"{PathToCooked}\WindowsNoEditor\{ProjectName}\Content\";
            }
        }

        public bool IsValidProjectPath
        {
            get
            {
                return !string.IsNullOrEmpty(PathToProject);
            }
        }

        public string PathToDefaultEngineIni
        {
            get
            {
                return $@"{BaseProjectFolder}\Config\DefaultEngine.ini";
            }
        }

        public string MapName
        {
            get
            {
                IniFile engineFile = new IniFile(PathToDefaultEngineIni);
                var fullPath = engineFile.ReadString("/Script/EngineSettings.GameMapsSettings", "GameDefaultMap");
                // Get everything after the last slash
                var slashIndex = fullPath.LastIndexOf("/") + 1;
                fullPath = fullPath.Substring(slashIndex);
                // Get everything after the last dot, if it exists:
                var dotIndex = fullPath.LastIndexOf(".") + 1;
                return fullPath.Substring(dotIndex);
            }
        }

        public ProjectWatcherViewModel()
        {
            _importViewModel = new ComputerImportViewModel()
            {
                IsZipFileImport = false
            };
            SetDefaultStatus();
        }

        internal void BrowseForProject()
        {
            UnwatchProject();

            using (var projectFileBrowser = new OpenFileDialog())
            {
                projectFileBrowser.Filter = "Unreal Engine Projects (*.uproject)|*.uproject";
                projectFileBrowser.Title = "Select .uproject File";
                if (projectFileBrowser.ShowDialog() == DialogResult.OK)
                {
                    PathToProject = projectFileBrowser.FileName;
                }
            }

            StatusText = "Not watching project.";
        }

        private void SetDefaultStatus()
        {
            StatusText = "Please enter a valid project file.";
            StatusColor = Brushes.Red;
        }

        internal void WatchProject()
        {
            if (!IsValidProjectPath)
            {
                SetDefaultStatus();
                return;
            }

            StatusText = "Watching project.";
            StatusColor = Brushes.Green;

            _importViewModel.PathInput = PathToCookedContent;

            _projectWatcher = new FileSystemWatcher()
            {
                Path = PathToCooked,
                Filter = "*.*",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                IncludeSubdirectories = true
            };

            _projectWatcher.Changed += OnCooked;

            _projectWatcher.EnableRaisingEvents = true;
        }

        internal void UnwatchProject()
        {
            if (!IsValidProjectPath)
            {
                SetDefaultStatus();
                return;
            }

            _projectWatcher.Changed -= OnCooked;
            _projectWatcher?.Dispose();

            StatusText = "Not watching project.";
            StatusColor = Brushes.Red;
        }

        private void OnCooked(object source, FileSystemEventArgs e)
        {
            StatusText = "Waiting for project to finish cooking...";
            LastOfBurstDo(burstDuration, () =>
            {
                StatusText = "Importing map...";
                var importTask = Task.Run(() => _importViewModel.ImportMapAsync());

                importTask.ContinueWith((antecedent) =>
                {
                    if (antecedent.Result.Result)
                    {
                        StatusText = "Done importing map!";
                        RaiseMapImported(MapName);
                    }
                    else
                        StatusText = antecedent.Result.Message;
                });
            });
        }

        private object _burstLock = new object();
        private int _burstCount = 0;

        private void LastOfBurstDo(int burstDuration, Action doAction)
        {
            var burstTask = Task.Run(async () =>
            {
                int expected = _burstCount + 1;
                lock (_burstLock)
                {
                    _burstCount = expected;
                }

                await Task.Delay(burstDuration);

                return expected == _burstCount;
            });

            burstTask.ContinueWith(antecedent =>
            {
                if (antecedent.Result)
                    doAction();
            });
        }
    }
}
