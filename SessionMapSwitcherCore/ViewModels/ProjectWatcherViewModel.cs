using IniParser;
using IniParser.Model;
using System.IO;
using System.Threading.Tasks;
using System;
using SessionMapSwitcher.Classes.Events;
using SessionMapSwitcherCore.Utils;

namespace SessionMapSwitcherCore.ViewModels
{
    public class ProjectWatcherViewModel : ViewModelBase
    {
        private const int burstDuration = 3000;

        private ComputerImportViewModel _importViewModel;

        private FileSystemWatcher _projectWatcher;

        public delegate void OnMapImported(object sender, MapImportedEventArgs e);
        public event OnMapImported MapImported;
        protected virtual void RaiseMapImported(string mapName) =>
            MapImported?.Invoke(this, new MapImportedEventArgs(mapName));

        private string _pathToProject;
        private string _statusText;
        private string _statusColor;

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

        public string StatusColor
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
                var parser = new FileIniDataParser();
                parser.Parser.Configuration.AllowDuplicateKeys = true;
                IniData engineFile = parser.ReadFile(PathToDefaultEngineIni);
                var fullPath = engineFile["/Script/EngineSettings.GameMapsSettings"]["GameDefaultMap"];
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
            PathToProject = AppSettingsUtil.GetAppSetting(SettingKey.ProjectWatcherPath);

            UnwatchProject();
        }

        private void SetDefaultStatus()
        {
            StatusText = "Please enter a valid project file.";
            StatusColor = "Red";
        }

        public void WatchProject()
        {
            if (!IsValidProjectPath)
            {
                SetDefaultStatus();
                return;
            }

            StatusText = "Watching project.";
            StatusColor = "Green";

            _importViewModel.PathInput = PathToCookedContent;

            if (!Directory.Exists(PathToCookedContent))
            {
                Directory.CreateDirectory(PathToCookedContent);
            }

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

        public void UnwatchProject()
        {
            if (!IsValidProjectPath)
            {
                SetDefaultStatus();
                return;
            }

            if (_projectWatcher != null)
            {
                _projectWatcher.Changed -= OnCooked;
                _projectWatcher?.Dispose();
            }

            StatusText = "Not watching project.";
            StatusColor = "Red";
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
