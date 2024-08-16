using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.Utils;
using SessionModManagerCore.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SessionModManagerCore.ViewModels
{
    public class MapImportViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static readonly List<string> FilesToExclude = new List<string>() { "DefaultEngine.ini", "DefaultGame.ini" };
        public static readonly List<string> StockFoldersToExclude = new List<string> { "Data" };


        private bool _isZipFileImport;
        private bool _isImporting;
        private double _importProgress;
        private string _userMessage;
        private string _pathInput;

        public string PathToTempUnzipFolder
        {
            get
            {
                return Path.Combine(SessionPath.ToSessionGame, "Temp_Unzipped");
            }
        }

        public bool IsZipFileImport
        {
            get { return _isZipFileImport; }
            set
            {
                _isZipFileImport = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(PathLabel));
            }
        }

        public bool IsImporting
        {
            get { return _isImporting; }
            set
            {
                _isImporting = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsNotImporting));
            }
        }

        public bool IsNotImporting
        {
            get
            {
                return !IsImporting;
            }
        }

        public double ImportProgress
        {
            get { return _importProgress; }
            set
            {
                _importProgress = value;
                NotifyPropertyChanged();
            }
        }

        public string PathLabel
        {
            get
            {
                if (IsZipFileImport)
                {
                    return "Path To Zip/Rar File";
                }
                else
                {
                    return "Folder Path To Map Files";
                }
            }
        }

        public string UserMessage
        {
            get { return _userMessage; }
            set
            {
                _userMessage = value;
                NotifyPropertyChanged();
            }
        }

        public string PathInput
        {
            get { return _pathInput; }
            set
            {
                _pathInput = value;
                NotifyPropertyChanged();
            }
        }

        public string PathToFileOrFolder
        {
            get
            {
                if (String.IsNullOrEmpty(PathInput))
                {
                    return "";
                }

                if (PathInput.EndsWith("\\"))
                {
                    PathInput = PathInput.TrimEnd('\\');
                }

                if (PathInput.EndsWith("/"))
                {
                    PathInput = PathInput.TrimEnd('/');
                }

                return PathInput;
            }
        }

        public Asset AssetToImport { get; set; }

        public MapImportViewModel()
        {
            this.UserMessage = "";
            this.IsZipFileImport = true; //usually a zip file to import so default to this
            this.PathInput = "";
        }

        public void BeginImportMapAsync()
        {
            UserMessage = "Importing Map ... this can take a few minutes for larger maps.";

            if (!IsZipFileImport)
            {
                PathInput = EnsurePathToMapFilesIsCorrect(PathToFileOrFolder);
            }

            IsImporting = true;

            Task<BoolWithMessage> task = ImportMapAsync();

            task.ContinueWith((antecedent) =>
            {
                IsImporting = false;

                if (antecedent.IsFaulted)
                {
                    Logger.Warn(antecedent.Exception, "import task uncaught exception");
                    UserMessage = "An error occurred importing the map.";
                    return;
                }

                if (antecedent.Result.Result)
                {
                    UserMessage = "Map Imported!";
                }
                else
                {
                    UserMessage = $"Failed to import map: {antecedent.Result.Message}";
                }
            });
        }

        /// <summary>
        /// This will check if the folder path to a map has the 'Content' folder and returns path to the maps 'Content folder if so
        /// </summary>
        /// <returns>
        /// "pathToMapFiles/Content" if Content folder exists; otherwise original pathToMapFiles string is returned
        /// </returns>
        private string EnsurePathToMapFilesIsCorrect(string pathToMapFiles)
        {
            string pathToContent = Path.Combine(pathToMapFiles, "Content");

            if (Directory.Exists(pathToContent))
            {
                return pathToContent;
            }

            return pathToMapFiles;
        }

        internal Task<BoolWithMessage> ImportMapAsync()
        {
            Task<BoolWithMessage> task = Task.Factory.StartNew(() =>
            {
                List<string> filesCopied;
                IProgress<double> progress = new Progress<double>(percent => this.ImportProgress = percent * 100);

                if (IsZipFileImport)
                {
                    // extract compressed file to correct location
                    if (File.Exists(PathToFileOrFolder) == false)
                    {
                        return BoolWithMessage.False($"{PathToFileOrFolder} does not exist.");
                    }

                    if (!FileUtils.CompressedFileHasFile(PathToFileOrFolder, ".umap", FileUtils.SearchType.EndsWith))
                    {
                        return BoolWithMessage.False($"{PathToFileOrFolder} does not contain a valid .umap file to import.");
                    }

                    this.ImportProgress = 0;

                    try
                    {
                        if (FileUtils.CompressedFileHasFile(PathToFileOrFolder, "Content/", FileUtils.SearchType.StartsWith) || FileUtils.CompressedFileHasFile(PathToFileOrFolder, "Content\\", FileUtils.SearchType.StartsWith))
                        {
                            // extract files to SessionGame/ instead if the zipped up root folder is 'Content/'
                            filesCopied = FileUtils.ExtractCompressedFile(PathToFileOrFolder, SessionPath.ToSessionGame, progress);
                        }
                        else
                        {
                            filesCopied = FileUtils.ExtractCompressedFile(PathToFileOrFolder, SessionPath.ToContent, progress);
                        }

                        string relativePathData = Path.Combine("Content", "Data");

                        if (filesCopied.Any(f => f.Contains(relativePathData)))
                        {
                            Logger.Info("Checking for files extracted to Data folder ...");
                            for (int i = filesCopied.Count-1; i >= 0; i--)
                            {
                                if (filesCopied[i].Contains(relativePathData) && File.Exists(filesCopied[i]))
                                {
                                    File.Delete(filesCopied[i]);
                                    filesCopied.RemoveAt(i);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "failed to extract file");
                        UserMessage = $"Failed to extract file: {e.Message}";
                        return BoolWithMessage.False($"Failed to extract: {e.Message}.");
                    }
                }
                else
                {
                    // validate folder exists and contains a valid map file
                    if (Directory.Exists(PathToFileOrFolder) == false)
                    {
                        return BoolWithMessage.False($"{PathToFileOrFolder} does not exist.");
                    }

                    if (!MetaDataManager.DoesValidMapExistInFolder(PathToFileOrFolder))
                    {
                        return BoolWithMessage.False($"{PathToFileOrFolder} does not contain a valid .umap file to import.");
                    }

                    filesCopied = FileUtils.CopyDirectoryRecursively(PathToFileOrFolder, SessionPath.ToContent, filesToExclude: FilesToExclude, foldersToExclude: StockFoldersToExclude, doContainsSearch: false, progress);
                }


                // create meta data for new map and save to disk
                MapMetaData metaData = GenerateMetaData(filesCopied);
                MetaDataManager.SaveMapMetaData(metaData);

                return BoolWithMessage.True();

            });

            return task;
        }

        private MapMetaData GenerateMetaData(List<string> filesCopied)
        {
            MapMetaData metaData = new MapMetaData()
            {
                AssetName = AssetToImport != null ? AssetToImport.ID : "",
                CustomName = "",
                IsHiddenByUser = false,
                FilePaths = filesCopied,
                OriginalImportPath = !IsZipFileImport ? PathToFileOrFolder : ""
            };

            // find valid map files
            foreach (string filePath in filesCopied.Where(f => f.EndsWith(".umap")))
            {
                if (MapListItem.HasGameMode(filePath))
                {
                    MapListItem map = new MapListItem(filePath);
                    metaData.MapFileDirectory = map.DirectoryPath;
                    metaData.MapName = map.MapName;
                    break;
                }
            }

            if (metaData.AssetName == "")
            {
                metaData.AssetName = metaData.MapName;
            }

            if (AssetToImport != null && File.Exists(AssetToImport?.PathToDownloadedImage))
            {
                metaData.PathToImage = AssetToImport.PathToDownloadedImage;
            }
            else if (filesCopied.Any(f => f.Contains("preview.")))
            {
                // check files copied for a preview image
                metaData.PathToImage = filesCopied.Where(f => f.Contains("preview.")).FirstOrDefault();
            }

            return metaData;
        }
    }
}
