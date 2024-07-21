using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.Utils;
using SessionMapSwitcherCore.ViewModels;
using SessionModManagerCore.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SessionModManagerCore.ViewModels
{
    public class TextureReplacerViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private string _pathToFile;
        private const string _tempZipFolder = "Temp_Texture_Unzipped";
        private static readonly List<string> StockFoldersToExclude = new List<string>() { "Skeletons" };
        private List<InstalledTextureItemViewModel> _installedTextures;
        private InstalledTextureItemViewModel _selectedTexture;
        private Stream _modPreviewSource;
        private bool _isLoadingImage;
        private bool _isReplaceButtonEnabled = true;
        public List<InstalledTextureItemViewModel> InstalledTextures
        {
            get
            {
                if (_installedTextures == null)
                    _installedTextures = new List<InstalledTextureItemViewModel>();

                return _installedTextures;
            }
            set
            {
                _installedTextures = value;
                NotifyPropertyChanged();
            }
        }

        public InstalledTextureItemViewModel SelectedTexture
        {
            get
            {
                return _selectedTexture;
            }
            set
            {
                _selectedTexture = value;
                GetSelectedPreviewImageAsync();
                NotifyPropertyChanged();
            }
        }

        public Asset AssetToInstall { get; set; }

        public delegate void MessageChange(string message);

        public event MessageChange MessageChanged;

        public TextureReplacerViewModel()
        {
            PathToFile = "";
        }

        public string PathToFile
        {
            get { return _pathToFile; }
            set
            {
                _pathToFile = value;
                NotifyPropertyChanged();
            }
        }

        public string PathToTempFolder
        {
            get
            {
                return Path.Combine(SessionPath.ToSessionGame, _tempZipFolder);
            }
        }

        public bool IsPathToCompressedFile
        {
            get
            {
                if (PathToFile == null)
                    return false;

                return PathToFile.EndsWith(".zip") || PathToFile.EndsWith(".rar");
            }
        }

        public Stream ModPreviewSource
        {
            get { return _modPreviewSource; }
            set
            {
                _modPreviewSource = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsPreviewMissing));
            }
        }

        public bool IsLoadingImage
        {
            get
            {
                return _isLoadingImage;
            }
            set
            {
                _isLoadingImage = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsPreviewMissing));
            }
        }

        public bool IsPreviewMissing
        {
            get
            {
                return (ModPreviewSource == null && !IsLoadingImage);
            }
        }

        public bool IsReplaceButtonEnabled
        {
            get
            {
                return _isReplaceButtonEnabled;
            }
            set
            {
                _isReplaceButtonEnabled = value;
                NotifyPropertyChanged();
            }

        }

        public void ReplaceTextures()
        {
            if (IsPathValid() == false)
            {
                return;
            }

            if (IsPathToCompressedFile)
            {
                ReplaceTexturesFromCompressedFile();
            }
            else
            {
                Logger.Warn("Only .zip and .rar files are accepted");
            }
        }

        private void ReplaceTexturesFromCompressedFile()
        {
            FileInfo textureFileInfo = null;
            DeleteTempZipFolder();
            Directory.CreateDirectory(PathToTempFolder);

            Logger.Info($"Extracting {PathToFile}...");
            MessageChanged?.Invoke($"Extracting mod files 0% ...");

            string pathToFile = PathToFile;


            // extract to temp location
            try
            {
                IProgress<double> progress = new Progress<double>(percent => MessageChanged?.Invoke($"Extracting mod files {percent * 100:0.0}% ..."));
                FileUtils.ExtractCompressedFile(pathToFile, PathToTempFolder, progress);
            }
            catch (Exception e)
            {
                Logger.Warn($"... failed to extract: {e.Message}");
                MessageChanged?.Invoke($"Failed to extract file: {e.Message}");
                return;
            }

            string rootFolder = PathToTempFolder;
            // change root folder to be where 'Customization' folder starts in unzipped files
            foreach (string dir in Directory.GetDirectories(PathToTempFolder, "*", SearchOption.AllDirectories))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(dir);
                if (dirInfo.Name == "Customization")
                {
                    rootFolder = dirInfo.Parent.FullName;
                    break;
                }
            }

            List<string> foundTextures = new List<string>();

            string foundTextureName = FindTextureFileInUnzippedTempFolder(dirToSearch: rootFolder, filesToExclude: foundTextures);
            bool hasPakFile = Directory.GetFiles(rootFolder, "*.pak", SearchOption.AllDirectories).Length > 0;

            // validate at least one texture file is in the zip
            if (foundTextureName == "" && !hasPakFile)
            {
                Logger.Warn("... failed to find a .uasset or .pak file");
                MessageChanged?.Invoke($"Failed to find a .uasset or .pak file inside the extracted folders.");
                return;
            }

            TextureMetaData newTextureMetaData = new TextureMetaData(AssetToInstall);

            if (AssetToInstall == null)
            {
                // if not replacing from Asset Store then just use name of compressed file being used to replace textures
                newTextureMetaData.AssetName = Path.GetFileName(pathToFile);
                newTextureMetaData.Name = Path.GetFileNameWithoutExtension(pathToFile);
            }
            else
            {
                if (File.Exists(AssetToInstall.PathToDownloadedImage))
                {
                    newTextureMetaData.PathToImage = AssetToInstall.PathToDownloadedImage;
                }
            }

            while (foundTextureName != "")
            {
                Logger.Info($"... found texture {foundTextureName}");
                foundTextures.Add(foundTextureName);

                textureFileInfo = new FileInfo(foundTextureName);
                string textureFileName = textureFileInfo.NameWithoutExtension();

                //
                // copy file to folder with same folder structure as zip
                //

                int index = textureFileInfo.DirectoryName.IndexOf(rootFolder) + 1;
                string targetFolder = textureFileInfo.DirectoryName.Substring(index + rootFolder.Length);

                targetFolder = Path.Combine(SessionPath.ToContent, targetFolder);


                try
                {
                    DeleteCurrentTextureFiles(textureFileInfo.NameWithoutExtension(), targetFolder);
                    // find and copy files in source dir that match the .uasset name
                    List<string> filesCopied = CopyNewTextureFilesToGame(textureFileInfo, targetFolder);
                    newTextureMetaData.FilePaths.AddRange(filesCopied);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    MessageChanged?.Invoke($"Failed to copy mod files: {e.Message}");
                    return;
                }

                MessageChanged?.Invoke($"Successfully imported mod file {textureFileInfo.Name}!");


                foundTextureName = FindTextureFileInUnzippedTempFolder(dirToSearch: rootFolder, filesToExclude: foundTextures);
            }


            try
            {
                // copy other files to Content
                if (UnzippedTempFolderHasOtherFolders(rootFolder))
                {
                    List<string> otherFilesCopied = CopyOtherSubfoldersInTempDir(rootFolder, filesToExclude: foundTextures);
                    newTextureMetaData.FilePaths.AddRange(otherFilesCopied);
                }

                if (string.IsNullOrWhiteSpace(newTextureMetaData.PathToImage))
                {
                    // check unzipped files for a preview img and copy over
                    foreach (string filePath in Directory.GetFiles(PathToTempFolder, "*", SearchOption.AllDirectories))
                    {
                        if (filePath.Contains("preview."))
                        {
                            string targetPath = Path.Combine(SessionPath.FullPathToMetaImagesFolder, newTextureMetaData.AssetNameWithoutExtension);

                            if (!Directory.Exists(SessionPath.FullPathToMetaImagesFolder))
                            {
                                Directory.CreateDirectory(SessionPath.FullPathToMetaImagesFolder);
                            }

                            File.Copy(filePath, targetPath, true);

                            newTextureMetaData.PathToImage = targetPath;
                            newTextureMetaData.FilePaths.Add(targetPath);
                            break;
                        }
                    }
                }

                DeleteTempZipFolder();
            }
            catch (Exception e)
            {
                Logger.Error(e);
                MessageChanged?.Invoke($"Failed to copy mod files: {e.Message}");
                return;
            }


            MetaDataManager.SaveTextureMetaData(newTextureMetaData);
            AssetToInstall = null; // texture asset replaced so nullify it since done with object

            MessageChanged?.Invoke($"Successfully finished importing mod {new FileInfo(pathToFile).NameWithoutExtension()}!");
        }

        private void DeleteTempZipFolder()
        {
            // delete temp folder with unzipped files
            if (Directory.Exists(PathToTempFolder))
            {
                Logger.Info("... deleting temp zip");
                Directory.Delete(PathToTempFolder, true);
            }
        }

        /// <summary>
        /// Copies other folders (not stock game folders) from unzipped temp folder into games Content folder
        /// </summary>
        private List<string> CopyOtherSubfoldersInTempDir(string rootFolder, List<string> filesToExclude)
        {
            List<string> filesCopied = new List<string>();

            foreach (string folder in Directory.GetDirectories(rootFolder))
            {
                DirectoryInfo folderInfo = new DirectoryInfo(folder);

                if (StockFoldersToExclude.Contains(folderInfo.Name) == false)
                {
                    List<string> fileNames = filesToExclude.Select(s =>
                    {
                        return Path.GetFileNameWithoutExtension(s);
                    }).ToList();



                    Logger.Info($"... copying folder {folder} -> {Path.Combine(SessionPath.ToContent, folderInfo.Name)}");

                    List<string> copied = FileUtils.CopyDirectoryRecursively(folder, Path.Combine(SessionPath.ToContent, folderInfo.Name), filesToExclude: fileNames, foldersToExclude: null, doContainsSearch: true);
                    filesCopied.AddRange(copied);
                }
            }

            return filesCopied;
        }

        /// <summary>
        /// Return true if unzipped temp folder has subfolders other than the games stock folders e.g. 'Customization' folder
        /// </summary>
        private bool UnzippedTempFolderHasOtherFolders(string rootFolder)
        {
            foreach (string folder in Directory.GetDirectories(rootFolder))
            {
                DirectoryInfo folderInfo = new DirectoryInfo(folder);

                if (StockFoldersToExclude.Contains(folderInfo.Name) == false)
                {
                    return true;
                }
            }

            return false;
        }

        public static string GetTextureNameFromFile(FileInfo textureFile)
        {
            try
            {
                string pathInFile = GetPathFromTextureFile(textureFile);

                int index = pathInFile.LastIndexOf(Path.DirectorySeparatorChar);
                if (index < 0)
                {
                    return "";
                }

                return pathInFile.Substring(index + 1);
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static string GetFolderPathToTextureFromFile(FileInfo textureFile)
        {
            string foundPath = "";

            try
            {
                string pathInFile = GetPathFromTextureFile(textureFile);

                // find index of last folder seperator '/' character to trim off the texture file name
                int index = pathInFile.LastIndexOf(Path.DirectorySeparatorChar);
                if (index < 0)
                {
                    return "";
                }
                pathInFile = pathInFile.Substring(0, index);

                // remove "\Game" before appending to absolute path to Content
                pathInFile = pathInFile.Replace($"{Path.DirectorySeparatorChar}Game", "");


                foundPath = $"{SessionPath.ToContent}{pathInFile}";
                return foundPath;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static string GetPathFromTextureFile(FileInfo textureFile)
        {
            try
            {
                string fileContents = File.ReadAllText(textureFile.FullName);
                string textureNameToFind = textureFile.NameWithoutExtension();

                Regex regex = new Regex($"\\/Game\\/Customization\\/[a-zA-Z\\/]+\\/{textureNameToFind}", RegexOptions.Multiline);

                Match found = regex.Match(fileContents);

                if (found.Success == false)
                {
                    return "";
                }

                string relativeFolderPath = found.Value;


                return relativeFolderPath.Replace('/', Path.DirectorySeparatorChar);
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// Loop over all files in the folder that contains the <paramref name="newTexture"/> .uasset file and copy all other files related to texture (.uexp and .ubulk files) to the <paramref name="targetFolder"/>
        /// </summary>
        private static List<string> CopyNewTextureFilesToGame(FileInfo newTexture, string targetFolder)
        {
            List<string> filesCopied = new List<string>();
            string textureSourceDir = Path.GetDirectoryName(newTexture.FullName);
            string textureFileName = newTexture.NameWithoutExtension();

            if (Directory.Exists(targetFolder) == false)
            {
                Logger.Info($"... creating missing directory {targetFolder}");
                Directory.CreateDirectory(targetFolder);
            }

            foreach (string file in Directory.GetFiles(textureSourceDir))
            {
                FileInfo info = new FileInfo(file);

                if (info.NameWithoutExtension() == textureFileName)
                {
                    string targetPath = Path.Combine(targetFolder, $"{textureFileName}{info.Extension}");

                    Logger.Info($"... copying {file} -> {targetPath}");
                    File.Copy(file, targetPath, overwrite: true);
                    filesCopied.Add(targetPath);
                }
            }

            return filesCopied;
        }

        /// <summary>
        /// Delete files in <paramref name="targetFolder"/> that contain the string <paramref name="textureFileName"/>
        /// </summary>
        /// <param name="textureFileName"> name of texture to delete (without file extension) </param>
        /// <param name="targetFolder"> folder to search in </param>
        private static void DeleteCurrentTextureFiles(string textureFileName, string targetFolder)
        {
            if (Directory.Exists(targetFolder) == false)
            {
                return;
            }

            foreach (string existingFile in Directory.GetFiles(targetFolder))
            {

                if (Path.GetFileNameWithoutExtension(existingFile) == textureFileName)
                {
                    Logger.Info($"... deleting current texture {existingFile}");
                    File.Delete(existingFile);
                }
            }
        }

        private bool IsPathValid()
        {
            if (String.IsNullOrWhiteSpace(PathToFile))
            {
                MessageChanged?.Invoke("Enter a path to a texture file or .zip file.");
                return false;
            }

            if (File.Exists(PathToFile) == false)
            {
                MessageChanged?.Invoke("File does not exist");
                return false;
            }

            if (!PathToFile.EndsWith(".zip") && !PathToFile.EndsWith(".uasset") && !PathToFile.EndsWith(".rar"))
            {
                MessageChanged?.Invoke("The selected file is invalid. You must choose a .rar, .zip or .uasset file.");
                return false;
            }

            return true;
        }

        private string FindTextureFileInUnzippedTempFolder(string dirToSearch = null, List<string> filesToExclude = null)
        {
            if (filesToExclude == null)
            {
                filesToExclude = new List<string>();
            }

            if (dirToSearch == null)
            {
                dirToSearch = PathToTempFolder;
            }

            foreach (string fileName in Directory.GetFiles(dirToSearch))
            {
                if (fileName.EndsWith(".uasset") && filesToExclude.Contains(fileName) == false)
                {
                    return fileName;
                }
            }

            foreach (string folder in Directory.GetDirectories(dirToSearch))
            {
                string fileName = FindTextureFileInUnzippedTempFolder(folder, filesToExclude);
                if (fileName != "")
                {
                    return fileName;
                }
            }

            return "";
        }

        internal string GetFolderPathToTexture(string textureName, string startingDir = null)
        {
            if (startingDir == null)
            {
                startingDir = SessionPath.ToContent;
            }

            foreach (string file in Directory.GetFiles(startingDir))
            {
                if (file.Contains(textureName))
                {
                    return Path.GetDirectoryName(file);
                }
            }

            foreach (string dir in Directory.GetDirectories(startingDir))
            {
                string path = GetFolderPathToTexture(textureName, dir);
                if (path != "")
                {
                    return path;
                }
            }

            return "";
        }

        /// <summary>
        /// Reads installed_textures.json meta data and initializes <see cref="InstalledTextures"/> with results
        /// </summary>
        public void LoadInstalledTextures()
        {
            InstalledTexturesMetaData installedMetaData = MetaDataManager.LoadTextureMetaData();

            List<InstalledTextureItemViewModel> textures = new List<InstalledTextureItemViewModel>();

            foreach (TextureMetaData item in installedMetaData.InstalledTextures)
            {
                textures.Add(new InstalledTextureItemViewModel(item));
            }

            // remember selected item or select first in list
            string assetName = "";
            if (SelectedTexture != null)
            {
                assetName = SelectedTexture.MetaData?.AssetName;
            }

            InstalledTextures = textures.OrderBy(t => t.TextureName).ToList();
            SetMissingImageFilePaths();

            if (assetName != "")
            {
                SelectedTexture = InstalledTextures.Where(t => t.MetaData?.AssetName == assetName).FirstOrDefault();
            }

            if (SelectedTexture == null)
            {
                SelectedTexture = InstalledTextures.FirstOrDefault();
            }
        }

        public void SetMissingImageFilePaths()
        {
            foreach (InstalledTextureItemViewModel item in InstalledTextures)
            {
                string pathToImage = Path.Combine(AssetStoreViewModel.AbsolutePathToThumbnails, item.MetaData.AssetNameWithoutExtension);
                bool hasChanged = false;

                if (!string.IsNullOrWhiteSpace(item.MetaData.PathToImage) && !File.Exists(item.MetaData.PathToImage))
                {
                    item.MetaData.PathToImage = "";
                    hasChanged = true;
                }

                if (string.IsNullOrWhiteSpace(item.MetaData.PathToImage) && File.Exists(pathToImage))
                {
                    item.MetaData.PathToImage = pathToImage;
                    hasChanged = true;
                }

                if (hasChanged)
                {
                    MetaDataManager.SaveTextureMetaData(item.MetaData);
                }
            }
        }

        public void RemoveSelectedMod()
        {
            InstalledTextureItemViewModel modToRemove = SelectedTexture;

            if (modToRemove == null || modToRemove.MetaData == null)
            {
                Logger.Warn("textureToRemove is null");
                return;
            }

            // check if removing RMS tools and delete loaded files before removing mod
            if (modToRemove.MetaData.FilePaths.Any(f => f.Contains(RMSToolsuiteLoader.PathToToolsuite)) && RMSToolsuiteLoader.IsLoaded())
            {
                RMSToolsuiteLoader.DeleteFilesInEnvFolder();
                AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.EnableRMSTools, ""); // clear out setting since not installed anymore
            }

            BoolWithMessage deleteResult = MetaDataManager.DeleteTextureFiles(modToRemove.MetaData);

            if (deleteResult.Result)
            {
                MessageService.Instance.ShowMessage($"Successfully removed {modToRemove.TextureName}!");
                LoadInstalledTextures();
            }
            else
            {
                MessageService.Instance.ShowMessage($"Failed to remove mod: {deleteResult.Message}");
            }
        }

        public void GetSelectedPreviewImageAsync()
        {
            if (SelectedTexture == null || string.IsNullOrWhiteSpace(SelectedTexture.MetaData.PathToImage))
            {
                ModPreviewSource = null;
                return;
            }

            IsLoadingImage = true;

            Task t = Task.Factory.StartNew(() =>
            {
                ModPreviewSource = new MemoryStream(File.ReadAllBytes(SelectedTexture.MetaData.PathToImage));
            });

            t.ContinueWith((taskResult) =>
            {
                if (taskResult.IsFaulted)
                {
                    MessageService.Instance.ShowMessage("Failed to get preview image.");
                    ModPreviewSource = null;
                    Logger.Error(taskResult.Exception);
                }

                IsLoadingImage = false;
            });

        }

    }
}
