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
        private List<FileConflict> _fileConflicts;
        private Stream _modPreviewSource;
        private bool _isLoadingImage;
        private bool _isReplaceButtonEnabled = true;
        private bool _allowModConflicts = false;

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
                NotifyPropertyChanged(nameof(MenuItemOpenSelectedText));
                IsShowingConflicts = false;
                FileConflicts = null;
            }
        }

        public Asset AssetToInstall { get; set; }

        public delegate void MessageChange(string message);

        public event MessageChange MessageChanged;

        private List<TexturePathInfo> _texturePaths;
        private bool _isShowingConflictWindow;

        public List<TexturePathInfo> TexturePaths
        {
            get
            {
                if (_texturePaths == null)
                    _texturePaths = TexturePathInfo.InitTexturePaths();

                return _texturePaths;
            }
        }

        public List<FileConflict> FileConflicts
        {
            get { return _fileConflicts; }
            set
            {
                _fileConflicts = value;
                NotifyPropertyChanged();
            }
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

        public string MenuItemOpenSelectedText
        {
            get
            {
                string shortName = SelectedTexture?.TextureName?.Length > 20 ? SelectedTexture?.TextureName.Substring(0, 20) : SelectedTexture?.TextureName;
                return SelectedTexture == null ? "Open Selected Mod Folder ..." : $"Open Mod Folder: {shortName} ...";
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

        public bool IsShowingConflicts
        {
            get
            {
                return _isShowingConflictWindow;
            }
            set
            {
                _isShowingConflictWindow = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsShowingModPreview));
            }
        }

        public bool AllowModConflicts
        {
            get
            {
                return _allowModConflicts;
            }
            set
            {
                if (_allowModConflicts != value)
                {
                    _allowModConflicts = value;
                    NotifyPropertyChanged();
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AllowModConflicts, _allowModConflicts.ToString());
                }
            }
        }

        public bool IsShowingModPreview
        {
            get { return !_isShowingConflictWindow; }
        }


        public TextureReplacerViewModel()
        {
            PathToFile = "";
            string allowConflicts = AppSettingsUtil.GetAppSetting(SettingKey.AllowModConflicts);
            if (!string.IsNullOrWhiteSpace(allowConflicts))
            {
                AllowModConflicts = allowConflicts.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
        }

        public void ImportTextureMod()
        {
            if (_texturePaths == null)
                _texturePaths = TexturePathInfo.InitTexturePaths();

            if (IsPathValid() == false)
            {
                return;
            }


            if (!IsPathToCompressedFile)
            {
                MessageChanged?.Invoke($"Path to file is not a valid .zip or .rar");
                return;
            }

            ImportTexturesFromCompressedFile();
        }

        private void ImportTexturesFromCompressedFile()
        {
            string pathToFile = PathToFile;
            Asset assetFromStore = AssetToInstall;
            string pathToMod = "";

            if (AssetToInstall != null)
            {
                pathToMod = Path.Combine(SessionPath.PathToInstalledModsFolder, $"{assetFromStore.IDWithoutExtension}");
            }
            else
            {
                pathToMod = Path.Combine(SessionPath.PathToInstalledModsFolder, Path.GetFileNameWithoutExtension(PathToFile));
            }

            Directory.CreateDirectory(pathToMod);

            Logger.Info($"Extracting {PathToFile}...");
            MessageChanged?.Invoke($"Extracting mod files 0% ...");

            // extract to download location
            try
            {
                IProgress<double> progress = new Progress<double>(percent => MessageChanged?.Invoke($"Extracting mod files {percent * 100:0.0}% ..."));
                FileUtils.ExtractCompressedFile(pathToFile, pathToMod, progress);
            }
            catch (Exception e)
            {
                Logger.Warn($"... failed to extract: {e.Message}");
                MessageChanged?.Invoke($"Failed to extract file: {e.Message}");
                return;
            }


            var metaData = CreateNewTextureMetaData(pathToFile, pathToMod, assetFromStore);

            var conflicts = GetConflicts(metaData);
            if (conflicts.Count != 0 && !AllowModConflicts)
            {
                MessageChanged?.Invoke($"finished importing mod {new FileInfo(pathToFile).NameWithoutExtension()}, but not enabled due to conflicts!");
                MetaDataManager.SaveTextureMetaData(metaData);
            }
            else
            {
                // enable mod if no conflicts or allow conflicts
                CopyModToSession(metaData);
                MessageChanged?.Invoke($"Successfully finished importing mod {new FileInfo(pathToFile).NameWithoutExtension()}!");
            }

            AssetToInstall = null; // texture asset replaced so nullify it since done with object
        }

        public List<FileConflict> GetConflicts(TextureMetaData metaData)
        {
            List<FileConflict> conflicts = new List<FileConflict>();
            FileInfo textureFileInfo = null;
            metaData.FilePaths = new List<string>();
            List<string> foundTextures = new List<string>();

            string rootFolder = metaData.FolderInstallPath;

            if (!Directory.Exists(rootFolder))
            {
                return conflicts; // directory doesn't exist any more so no files to compare to
            }

            // change root folder to be where 'Customization' folder starts in unzipped files
            foreach (string dir in Directory.GetDirectories(rootFolder, "*", SearchOption.AllDirectories))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(dir);
                if (dirInfo.Name == "Customization")
                {
                    rootFolder = dirInfo.Parent.FullName;
                    break;
                }
            }

            string foundTextureName = FindTextureFileInUnzippedTempFolder(dirToSearch: rootFolder, filesToExclude: foundTextures);
            bool hasPakFile = Directory.GetFiles(rootFolder, "*.pak", SearchOption.AllDirectories).Length > 0;

            // validate at least one texture file is in the zip
            if (foundTextureName == "" && !hasPakFile)
            {
                Logger.Warn("... failed to find a .uasset or .pak file");
                MessageChanged?.Invoke($"Failed to find a .uasset or .pak file inside the extracted folders.");
                return conflicts;
            }

            while (foundTextureName != "")
            {
                foundTextures.Add(foundTextureName);

                textureFileInfo = new FileInfo(foundTextureName);
                string textureFileName = textureFileInfo.NameWithoutExtension();

                //
                // find which folder to copy to based on file
                //

                // first try to get the path from the file contents and validate it is a valid texture path
                string absolutePathToTexture = GetFolderPathToTextureFromFile(textureFileInfo);

                bool isValidTexture = TexturePaths.Any(t => Path.Combine(SessionPath.ToContent, t.RelativePath) == absolutePathToTexture);

                string targetFolder = "";

                if (isValidTexture)
                {
                    targetFolder = absolutePathToTexture;
                }
                else
                {
                    // second try to get path to texture based on file name
                    targetFolder = TexturePaths.Where(t => t.TextureName == textureFileName).Select(t => t.RelativePath).FirstOrDefault();

                    if (String.IsNullOrEmpty(targetFolder) == false)
                    {
                        targetFolder = Path.Combine(SessionPath.ToContent, targetFolder);
                    }
                }


                if (String.IsNullOrEmpty(targetFolder))
                {
                    // a path to a game texture could not be found ...
                    // ... so assume the texture/material is custom; Copy to game directory with same folder structure as zip
                    int index = textureFileInfo.DirectoryName.IndexOf(rootFolder) + 1;
                    targetFolder = textureFileInfo.DirectoryName.Substring(index + rootFolder.Length);

                    targetFolder = Path.Combine(SessionPath.ToContent, targetFolder);
                }

                if (Directory.Exists(targetFolder))
                {
                    foreach (string file in Directory.GetFiles(targetFolder))
                    {
                        if (Path.GetFileNameWithoutExtension(file) == textureFileName && !conflicts.Any(t => t.FileName == textureFileName))
                        {
                            // look for the asset that is conflicting (if none then safe to overwrite)
                            var conflictingAsset = InstalledTextures.Where(t => t.MetaData.AssetName != metaData.AssetName && t.MetaData.FilePaths.Contains(file)).FirstOrDefault();

                            if (conflictingAsset != null)
                            {
                                Logger.Warn($"Conflict found for {textureFileName}: {file} already exists");
                                conflicts.Add(new FileConflict()
                                {
                                    FileName = textureFileName,
                                    FilePath = file,
                                    AssetName = metaData.AssetNameWithoutExtension,
                                    ExistingAssetName = conflictingAsset.MetaData.AssetNameWithoutExtension
                                });
                            }
                        }
                    }
                }

                foundTextureName = FindTextureFileInUnzippedTempFolder(dirToSearch: rootFolder, filesToExclude: foundTextures);
            }

            // check other files to Content
            if (FolderHasOtherFolders(rootFolder))
            {
                // TODO
            }


            return conflicts;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pathToFile"></param>
        /// <param name="rootFolder"></param>
        /// <param name="asset"></param>
        private TextureMetaData CreateNewTextureMetaData(string pathToFile, string rootFolder, Asset asset)
        {
            List<string> foundTextures = new List<string>();

            TextureMetaData newTextureMetaData = new TextureMetaData(asset)
            {
                FolderInstallPath = rootFolder
            };

            if (asset == null)
            {
                // if not replacing from Asset Store then just use name of compressed file being used to replace textures
                newTextureMetaData.AssetName = Path.GetFileName(pathToFile);
                newTextureMetaData.Name = Path.GetFileNameWithoutExtension(pathToFile);
            }
            else
            {
                if (File.Exists(asset.PathToDownloadedImage))
                {
                    // copy over asset store image as preview image and use that instead
                    var fileInfo = new FileInfo(asset.PathToDownloadedImage);
                    newTextureMetaData.PathToImage = Path.Combine(rootFolder, "preview_thumbnail");
                    File.Copy(asset.PathToDownloadedImage, newTextureMetaData.PathToImage, overwrite: true);
                    File.Delete(asset.PathToDownloadedImage);
                    if (ImageCache.Instance.CacheEntries.ContainsKey(asset.PathToDownloadedImage))
                    {
                        ImageCache.Instance.CacheEntries[asset.PathToDownloadedImage].FilePath = newTextureMetaData.PathToImage;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(newTextureMetaData.PathToImage))
            {
                // check unzipped files for a preview img
                foreach (string filePath in Directory.GetFiles(rootFolder, "*", SearchOption.AllDirectories))
                {
                    if (filePath.Contains("preview."))
                    {
                        newTextureMetaData.PathToImage = filePath;
                        break;
                    }
                }
            }

            return newTextureMetaData;
        }

        private bool CopyModToSession(TextureMetaData metaData)
        {
            FileInfo textureFileInfo = null;
            metaData.FilePaths = new List<string>();
            List<string> foundTextures = new List<string>();

            string rootFolder = metaData.FolderInstallPath;

            if (!Directory.Exists(rootFolder))
            {
                Logger.Warn($"... {metaData.AssetName} - FolderInstallPath does not exist {rootFolder}");
                MessageChanged?.Invoke($"Failed to enable because mod folder is missing. You may need to re-install the mod.");
                return false;
            }

            // change root folder to be where 'Customization' folder starts in unzipped files
            foreach (string dir in Directory.GetDirectories(rootFolder, "*", SearchOption.AllDirectories))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(dir);
                if (dirInfo.Name == "Customization")
                {
                    rootFolder = dirInfo.Parent.FullName;
                    break;
                }
            }

            string foundTextureName = FindTextureFileInUnzippedTempFolder(dirToSearch: rootFolder, filesToExclude: foundTextures);
            bool hasPakFile = Directory.GetFiles(rootFolder, "*.pak", SearchOption.AllDirectories).Length > 0;

            // validate at least one texture file is in the zip
            if (foundTextureName == "" && !hasPakFile)
            {
                Logger.Warn("... failed to find a .uasset or .pak file");
                MessageChanged?.Invoke($"Failed to find a .uasset or .pak file inside the extracted folders. You may need to re-install the mod.");
                return false;
            }

            while (foundTextureName != "")
            {
                Logger.Info($"... found texture {foundTextureName}");
                foundTextures.Add(foundTextureName);

                textureFileInfo = new FileInfo(foundTextureName);
                string textureFileName = textureFileInfo.NameWithoutExtension();

                //
                // find which folder to copy to based on file
                //

                // first try to get the path from the file contents and validate it is a valid texture path
                string absolutePathToTexture = GetFolderPathToTextureFromFile(textureFileInfo);

                bool isValidTexture = TexturePaths.Any(t => Path.Combine(SessionPath.ToContent, t.RelativePath) == absolutePathToTexture);

                string targetFolder = "";

                if (isValidTexture)
                {
                    targetFolder = absolutePathToTexture;
                }
                else
                {
                    // second try to get path to texture based on file name
                    targetFolder = TexturePaths.Where(t => t.TextureName == textureFileName).Select(t => t.RelativePath).FirstOrDefault();

                    if (String.IsNullOrEmpty(targetFolder) == false)
                    {
                        targetFolder = Path.Combine(SessionPath.ToContent, targetFolder);
                    }
                }


                if (String.IsNullOrEmpty(targetFolder))
                {
                    // a path to a game texture could not be found ...
                    // ... so assume the texture/material is custom; Copy to game directory with same folder structure as zip
                    int index = textureFileInfo.DirectoryName.IndexOf(rootFolder) + 1;
                    targetFolder = textureFileInfo.DirectoryName.Substring(index + rootFolder.Length);

                    targetFolder = Path.Combine(SessionPath.ToContent, targetFolder);
                }



                try
                {
                    DeleteCurrentTextureFiles(textureFileInfo.NameWithoutExtension(), targetFolder);
                    // find and copy files in source dir that match the .uasset name
                    List<string> filesCopied = CopyNewTextureFilesToGame(textureFileInfo, targetFolder);
                    metaData.FilePaths.AddRange(filesCopied);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    MessageChanged?.Invoke($"Failed to copy mod files: {e.Message}");
                    return false;
                }



                foundTextureName = FindTextureFileInUnzippedTempFolder(dirToSearch: rootFolder, filesToExclude: foundTextures);
            }


            try
            {
                // copy other files to Content
                if (FolderHasOtherFolders(rootFolder))
                {
                    List<string> otherFilesCopied = CopyOtherSubfoldersInDir(rootFolder, filesToExclude: foundTextures);
                    metaData.FilePaths.AddRange(otherFilesCopied);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                MessageChanged?.Invoke($"Failed to copy mod files: {e.Message}");
                return false;
            }

            metaData.Enabled = true;
            MetaDataManager.SaveTextureMetaData(metaData);

            MessageChanged?.Invoke($"Successfully enabled mod {metaData.AssetNameWithoutExtension}!");
            return true;
        }


        /// <summary>
        /// Copies other folders (not stock game folders) from unzipped temp folder into games Content folder
        /// </summary>
        private List<string> CopyOtherSubfoldersInDir(string rootFolder, List<string> filesToExclude)
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
        private bool FolderHasOtherFolders(string rootFolder)
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

            // convert old meta data file to new version to allow for enabling/disabling mods
            if (installedMetaData.SchemaVersion == "v1")
            {
                ConvertToV2Schema(ref installedMetaData);
            }
            else if (string.IsNullOrWhiteSpace(installedMetaData.SchemaVersion))
            {
                ConvertToV1Schema(ref installedMetaData);
            }

            List<InstalledTextureItemViewModel> textures = new List<InstalledTextureItemViewModel>();

            foreach (TextureMetaData item in installedMetaData.InstalledTextures)
            {
                var meta = new InstalledTextureItemViewModel(item);
                meta.IsEnabledChanged += EnableOrDisableMod;
                textures.Add(meta);
            }

            // remember selected item or select first in list
            string assetName = "";
            if (SelectedTexture != null)
            {
                assetName = SelectedTexture.MetaData?.AssetName;
            }


            InstalledTextures.ForEach(t => t.IsEnabledChanged -= EnableOrDisableMod);
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

        private void ConvertToV1Schema(ref InstalledTexturesMetaData installedMetaData)
        {
            try
            {
                foreach (TextureMetaData item in installedMetaData.InstalledTextures)
                {
                    item.Enabled = true;
                    CopyFilesToInstalledModsFolder(item);
                }

                MetaDataManager.SaveTextureMetaData(installedMetaData);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to convert installed_textures.json to v1 Schema");
            }
        }

        private static void CopyFilesToInstalledModsFolder(TextureMetaData item)
        {
            item.FolderInstallPath = Path.Combine(SessionPath.PathToInstalledModsFolder, item.AssetNameWithoutExtension);

            if (!Directory.Exists(item.FolderInstallPath))
            {
                Directory.CreateDirectory(item.FolderInstallPath);
            }

            foreach (var file in item.FilePaths)
            {
                int idx = file.IndexOf("Customization");

                if (idx == -1 || !File.Exists(file))
                {
                    continue; //skip file; it may have been deleted manually
                }

                string destPath = file.Substring(idx, file.Length - idx);
                destPath = Path.Combine(item.FolderInstallPath, destPath);

                FileInfo fileInfo = new FileInfo(destPath);
                if (!fileInfo.Directory.Exists)
                {
                    fileInfo.Directory.Create();
                }

                File.Copy(file, destPath, overwrite: true);
            }
        }

        /// <summary>
        /// same as v1 conversion but doesnt enable mod
        /// </summary>
        /// <param name="installedMetaData"></param>
        private void ConvertToV2Schema(ref InstalledTexturesMetaData installedMetaData)
        {
            try
            {
                foreach (TextureMetaData item in installedMetaData.InstalledTextures)
                {
                    CopyFilesToInstalledModsFolder(item);
                }

                MetaDataManager.SaveTextureMetaData(installedMetaData);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to convert installed_textures.json to v2 Schema");
            }
        }

        private void EnableOrDisableMod(bool isEnabled, TextureMetaData metaData)
        {
            if (!metaData.Enabled && isEnabled)
            {
                var conflicts = GetConflicts(metaData);
                if (conflicts.Any() && !AllowModConflicts)
                {
                    SelectedTexture.IsEnabled = false;
                    FileConflicts = conflicts;
                    IsShowingConflicts = true;
                    MessageService.Instance.ShowMessage($"Failed to enable mod due to conflicts! Disable conflicting mods first or change setting to allow conflicts.");
                }
                else
                {
                    FileConflicts = null;
                    IsShowingConflicts = false;
                    // copy files to game
                    if (!CopyModToSession(metaData))
                    {
                        SelectedTexture.IsEnabled = false; // failed to copy so leave it as not enabled
                    }
                }
            }
            else if (metaData.Enabled && !isEnabled)
            {
                DisableSelectedMod(metaData);
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

        public void DisableSelectedMod(TextureMetaData modMetaData)
        {
            if (modMetaData == null)
            {
                Logger.Warn("modMetaData is null");
                return;
            }

            BoolWithMessage deleteResult = MetaDataManager.DeleteTextureFiles(modMetaData);

            if (deleteResult.Result)
            {
                MessageService.Instance.ShowMessage($"Successfully disabled {modMetaData.AssetNameWithoutExtension}!");
                LoadInstalledTextures();
            }
            else
            {
                MessageService.Instance.ShowMessage($"Failed to disable mod: {deleteResult.Message}");
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


        public void DeleteSelectedMod()
        {
            InstalledTextureItemViewModel modToRemove = SelectedTexture;

            if (modToRemove == null || modToRemove.MetaData == null)
            {
                Logger.Warn("modToRemove is null");
                return;
            }

            if (modToRemove.IsEnabled)
            {
                BoolWithMessage deleteResult = MetaDataManager.DeleteTextureFiles(modToRemove.MetaData);

                if (!deleteResult.Result)
                {
                    Logger.Warn($"Failed to delete mod files: {deleteResult.Message}");
                }
            }

            // check if removing RMS tools and delete loaded files before removing mod
            if (modToRemove.MetaData.FilePaths.Any(f => f.Contains(RMSToolsuiteLoader.PathToToolsuite)) && RMSToolsuiteLoader.IsLoaded())
            {
                RMSToolsuiteLoader.DeleteFilesInEnvFolder();
                AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.EnableRMSTools, ""); // clear out setting since not installed anymore
            }

            // delete downloaded files
            try
            {
                if (Directory.Exists(modToRemove.MetaData.FolderInstallPath))
                {
                    Directory.Delete(modToRemove.MetaData.FolderInstallPath, true);
                }

                InstalledTexturesMetaData currentlyInstalledTextures = MetaDataManager.LoadTextureMetaData();
                currentlyInstalledTextures.Remove(modToRemove.MetaData);
                MetaDataManager.SaveTextureMetaData(currentlyInstalledTextures);

                LoadInstalledTextures();

                MessageService.Instance.ShowMessage($"Successfully removed {modToRemove.TextureName}!");
            }
            catch (Exception ex)
            {
                MessageService.Instance.ShowMessage($"Failed to remove mod: {ex.Message}");
            }

        }

    }
}