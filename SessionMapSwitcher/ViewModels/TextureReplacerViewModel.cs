using SessionMapSwitcher.Classes;
using SessionMapSwitcher.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SessionMapSwitcher.ViewModels
{
    public class TextureReplacerViewModel : ViewModelBase
    {
        private string _pathToFile;
        private const string _tempZipFolder = "Temp_Texture_Unzipped";

        public delegate void MessageChange(string message);

        public event MessageChange MessageChanged;

        private List<TexturePathInfo> _texturePaths;

        public List<TexturePathInfo> TexturePaths
        {
            get
            {
                if (_texturePaths == null)
                    InitTexturePaths();

                return _texturePaths;
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

        public string PathToTempFolder
        {
            get
            {
                return $"{SessionPath.ToSessionGame}\\{_tempZipFolder}";
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

        internal void BrowseForFiles()
        {
            using (OpenFileDialog fileBrowserDialog = new OpenFileDialog())
            {
                fileBrowserDialog.Filter = "uasset file (*.uasset)|*.uasset|Zip files (*.zip)|*.zip|Rar files (*.rar)|*.rar|All files (*.*)|*.*";
                fileBrowserDialog.Title = "Select .uasset Texture File, .zip, or .rar File Containing Texture Files";
                DialogResult result = fileBrowserDialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    PathToFile = fileBrowserDialog.FileName;
                }
            }
        }

        internal void ReplaceTextures()
        {
            InitTexturePaths();

            if (IsPathValid() == false)
            {
                return;
            }


            if (IsPathToCompressedFile)
            {
                ReplaceTexturesFromCompressedFile();
                return;
            }


            FileInfo textureFileInfo = null;
            textureFileInfo = new FileInfo(PathToFile);

            string textureFileName = textureFileInfo.NameWithoutExtension();

            // find which folder to copy to based on file name
            string originalTextureName = GetTextureNameFromFile(textureFileInfo);


            string targetFolder = TexturePaths.Where(t => t.TextureName == originalTextureName).Select(t => t.RelativePath).FirstOrDefault();

            if (String.IsNullOrEmpty(targetFolder))
            {
                // could not find folder path based on name in cooked asset file so look up based on file name
                TexturePaths.Where(t => t.TextureName == textureFileName).Select(t => t.RelativePath).FirstOrDefault();
            }

            if (String.IsNullOrEmpty(targetFolder))
            {
                MessageChanged?.Invoke($"Failed to find path to original texture: {textureFileInfo.Name}");
                return;
            }

            targetFolder = $"{SessionPath.ToContent}\\{targetFolder}";

            try
            {
                DeleteCurrentTextureFiles(originalTextureName, targetFolder);

                // find and copy files in source dir that match the .uasset name
                CopyNewTextureFilesToGame(textureFileInfo, targetFolder, originalTextureName);
            }
            catch (Exception e)
            {
                MessageChanged?.Invoke($"Failed to copy texture files: {e.Message}");
                return;
            }

            MessageChanged?.Invoke($"Successfully replaced textures for {textureFileInfo.Name}!");
        }

        private void ReplaceTexturesFromCompressedFile()
        {
            FileInfo textureFileInfo = null;
            DeleteTempZipFolder();
            Directory.CreateDirectory(PathToTempFolder);


            // extract to temp location
            BoolWithMessage didExtract = FileUtils.ExtractCompressedFile(PathToFile, PathToTempFolder);
            if (didExtract.Result == false)
            {
                MessageChanged?.Invoke($"Failed to extract file: {didExtract.Message}");
                return;
            }

            List<string> foundTextures = new List<string>();

            string foundTextureName = FindTextureFileInUnzippedTempFolder(dirToSearch: PathToTempFolder, filesToExclude: foundTextures);

            // validate at least one texture file is in the zip
            if (foundTextureName == "")
            {
                MessageChanged?.Invoke($"Failed to find a .uasset file inside the extracted folders.");
                return;
            }

            do
            {
                foundTextures.Add(foundTextureName);

                textureFileInfo = new FileInfo(foundTextureName);
                string textureFileName = textureFileInfo.NameWithoutExtension();


                // find which folder to copy to based on file name
                string originalTextureName = GetTextureNameFromFile(textureFileInfo);

                string targetFolder = TexturePaths.Where(t => t.TextureName == originalTextureName).Select(t => t.RelativePath).FirstOrDefault();

                if (String.IsNullOrEmpty(targetFolder))
                {
                    // could not find folder path based on name in cooked asset file so look up based on file name
                    targetFolder = TexturePaths.Where(t => t.TextureName == textureFileName).Select(t => t.RelativePath).FirstOrDefault();
                }

                if (String.IsNullOrEmpty(targetFolder))
                {
                    foundTextureName = FindTextureFileInUnzippedTempFolder(dirToSearch: PathToTempFolder, filesToExclude: foundTextures);
                    break;
                }


                targetFolder = $"{SessionPath.ToContent}\\{targetFolder}";

                try
                {
                    DeleteCurrentTextureFiles(originalTextureName, targetFolder);

                    // find and copy files in source dir that match the .uasset name
                    CopyNewTextureFilesToGame(textureFileInfo, targetFolder, originalTextureName);
                }
                catch (Exception e)
                {
                    MessageChanged?.Invoke($"Failed to copy texture files: {e.Message}");
                    return;
                }

                MessageChanged?.Invoke($"Successfully replaced textures for {textureFileInfo.Name}!");


                foundTextureName = FindTextureFileInUnzippedTempFolder(dirToSearch: PathToTempFolder, filesToExclude: foundTextures);
            } while (foundTextureName != "");


            try
            {
                // copy other files to Content
                if (UnzippedTempFolderHasOtherFolders())
                {
                    CopyOtherSubfoldersInTempDir(filesToExclude: foundTextures);
                }

                DeleteTempZipFolder();
            }
            catch (Exception e)
            {
                MessageChanged?.Invoke($"Failed to copy texture files: {e.Message}");
                return;
            }
        }

        private void DeleteTempZipFolder()
        {
            // delete temp folder with unzipped files
            if (Directory.Exists(PathToTempFolder))
            {
                Directory.Delete(PathToTempFolder, true);
            }
        }

        /// <summary>
        /// Copies other folders (not stock game folders) from unzipped temp folder into games Content folder
        /// </summary>
        private void CopyOtherSubfoldersInTempDir(List<string> filesToExclude)
        {
            foreach (string folder in Directory.GetDirectories(PathToTempFolder))
            {
                DirectoryInfo folderInfo = new DirectoryInfo(folder);

                if (ComputerImportViewModel.AllStockFoldersToExclude.Contains(folderInfo.Name) == false)
                {
                    List<string> fileNames = filesToExclude.Select(s =>
                    {
                        int index = s.LastIndexOf('\\');
                        return s.Substring(index + 1).Replace(".uasset" , "");
                    }).ToList();


                    FileUtils.CopyDirectoryRecursively(folder, $"{SessionPath.ToContent}\\{folderInfo.Name}", filesToExclude: fileNames, foldersToExclude: null, doContainsSearch: true);
                }
            }
        }

        /// <summary>
        /// Return true if unzipped temp folder has subfolders other than the games stock folders e.g. 'Customization' folder
        /// </summary>
        private bool UnzippedTempFolderHasOtherFolders()
        {
            foreach (string folder in Directory.GetDirectories(PathToTempFolder))
            {
                DirectoryInfo folderInfo = new DirectoryInfo(folder);

                if (ComputerImportViewModel.AllStockFoldersToExclude.Contains(folderInfo.Name) == false)
                {
                    return true;
                }
            }

            return false;
        }

        private string GetTextureNameFromFile(FileInfo textureFile)
        {
            try
            {
                string pathInFile = GetPathFromTextureFile(textureFile);

                int index = pathInFile.LastIndexOf("\\");
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

        private string GetFolderPathToTextureFromFile(FileInfo textureFile)
        {
            string foundPath = "";

            try
            {
                string pathInFile = GetPathFromTextureFile(textureFile);

                int index = pathInFile.LastIndexOf("\\");
                if (index < 0)
                {
                    return "";
                }
                pathInFile = pathInFile.Substring(0, index);


                foundPath = $"{SessionPath.ToContent}{pathInFile}";
                return foundPath;
            }
            catch (Exception)
            {
                return "";
            }
        }

        private string GetPathFromTextureFile(FileInfo textureFile)
        {
            try
            {
                string fileContents = File.ReadAllText(textureFile.FullName);

                int index = fileContents.IndexOf("/Game");

                if (index < 0)
                {
                    return "";
                }
                fileContents = fileContents.Substring("/Game".Length + index);


                index = fileContents.IndexOf('\0');
                if (index < 0)
                {
                    return "";
                }
                string relativeFolderPath = fileContents.Substring(0, index);


                return relativeFolderPath.Replace("/", "\\");
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// Loop over all files in the folder that contains the <paramref name="newTexture"/> .uasset file and copy all other files related to texture (.uexp and .ubulk files) to the <paramref name="targetFolder"/>
        /// </summary>
        private static void CopyNewTextureFilesToGame(FileInfo newTexture, string targetFolder, string textureName)
        {
            string textureSourceDir = Path.GetDirectoryName(newTexture.FullName);
            string textureFileName = newTexture.NameWithoutExtension();

            if (Directory.Exists(targetFolder) == false)
            {
                Directory.CreateDirectory(targetFolder);
            }

            foreach (string file in Directory.GetFiles(textureSourceDir))
            {
                FileInfo info = new FileInfo(file);

                if (info.NameWithoutExtension() == textureFileName)
                {
                    string targetPath = Path.Combine(targetFolder, $"{textureName}{info.Extension}");
                    File.Copy(file, targetPath, overwrite: true);
                }
            }
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
                if (existingFile.Contains(textureFileName))
                {
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

        private void InitTexturePaths()
        {
            if (_texturePaths != null)
                return;

            _texturePaths = new List<TexturePathInfo>();

            _texturePaths.Add(new TexturePathInfo() { TextureName = "ABP_CustomizationCharacter", RelativePath = "Customization" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "DATASS_DatabaseCompanies", RelativePath = "Customization\\BrandAssets" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_BRN_eS_Accel_ShoeBox_A", RelativePath = "Customization\\BrandAssets\\eSFootwear\\Staticmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_BRN_eS_Square3_ShoeProp_L", RelativePath = "Customization\\BrandAssets\\eSFootwear\\Staticmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_BRN_eS_Square3_ShoeProp_R", RelativePath = "Customization\\BrandAssets\\eSFootwear\\Staticmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_eS_RedSticker_Decal", RelativePath = "Customization\\BrandAssets\\eSFootwear\\Stickers\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_eS_Footwear_RED_Logo", RelativePath = "Customization\\BrandAssets\\eSFootwear\\Stickers\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_eS_Footwear_RED_Logo_NM", RelativePath = "Customization\\BrandAssets\\eSFootwear\\Stickers\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_eS_Footwear_RED_Logo_RG", RelativePath = "Customization\\BrandAssets\\eSFootwear\\Stickers\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_BRN_eS_ShoesBox_A", RelativePath = "Customization\\BrandAssets\\eSFootwear\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_eS_ShoesBox_BC", RelativePath = "Customization\\BrandAssets\\eSFootwear\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_Anatomy", RelativePath = "Customization\\BrandAssets\\HeroinSkateboards\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_BigEgg_Pruple_Custom", RelativePath = "Customization\\BrandAssets\\HeroinSkateboards\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_BigEgg_Yellow_Custom", RelativePath = "Customization\\BrandAssets\\HeroinSkateboards\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_BlackEye_Custom", RelativePath = "Customization\\BrandAssets\\HeroinSkateboards\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_Shovel_Custom", RelativePath = "Customization\\BrandAssets\\HeroinSkateboards\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_Spiral_Custom", RelativePath = "Customization\\BrandAssets\\HeroinSkateboards\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_TheEgg_Custom", RelativePath = "Customization\\BrandAssets\\HeroinSkateboards\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_ToxicEye_800", RelativePath = "Customization\\BrandAssets\\HeroinSkateboards\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_ToxicEye_825", RelativePath = "Customization\\BrandAssets\\HeroinSkateboards\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_ToxicEye_CustomShape", RelativePath = "Customization\\BrandAssets\\HeroinSkateboards\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_VeryBigEgg_Custom", RelativePath = "Customization\\BrandAssets\\HeroinSkateboards\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_HeroinSkateboard_Logo", RelativePath = "Customization\\BrandAssets\\HeroinSkateboards\\Logos" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "Heroin_Deck_01", RelativePath = "Customization\\BrandAssets\\HeroinSkateboards\\NonCustomizationAssets\\Basemesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "Heroin_Deck_02", RelativePath = "Customization\\BrandAssets\\HeroinSkateboards\\NonCustomizationAssets\\Basemesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "Heroin_Deck_03", RelativePath = "Customization\\BrandAssets\\HeroinSkateboards\\NonCustomizationAssets\\Basemesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_DG_HeroinSkateboard_01", RelativePath = "Customization\\BrandAssets\\HeroinSkateboards\\NonCustomizationAssets\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_DG_Heroin_Eye1", RelativePath = "Customization\\BrandAssets\\HeroinSkateboards\\NonCustomizationAssets\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_DG_Heroin_Eye2", RelativePath = "Customization\\BrandAssets\\HeroinSkateboards\\NonCustomizationAssets\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_DG_Heroin_Eye3", RelativePath = "Customization\\BrandAssets\\HeroinSkateboards\\NonCustomizationAssets\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_HeroinSkateboard_01_BC", RelativePath = "Customization\\BrandAssets\\HeroinSkateboards\\NonCustomizationAssets\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_JenkemBooks", RelativePath = "Customization\\BrandAssets\\JenkemMag\\BrandAssets\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_HUB_JenkemBook_Vol1", RelativePath = "Customization\\BrandAssets\\JenkemMag\\BrandAssets\\StaticMeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_HUB_JenkemBook_Vol2", RelativePath = "Customization\\BrandAssets\\JenkemMag\\BrandAssets\\StaticMeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_Jenkem_Books", RelativePath = "Customization\\BrandAssets\\JenkemMag\\BrandAssets\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_Jenkem_Logo", RelativePath = "Customization\\BrandAssets\\JenkemMag\\Logos" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_NoComply_Custom_01", RelativePath = "Customization\\BrandAssets\\NoComply\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_NoComply_Custom_02", RelativePath = "Customization\\BrandAssets\\NoComply\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_NoComply_Custom_03", RelativePath = "Customization\\BrandAssets\\NoComply\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_NoComply_Custom_04", RelativePath = "Customization\\BrandAssets\\NoComply\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_NoComply_Custom_05", RelativePath = "Customization\\BrandAssets\\NoComply\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_NoComply_CustomizationLogo", RelativePath = "Customization\\BrandAssets\\NoComply\\Logos" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_Roger_DogBalls_01_BC", RelativePath = "Customization\\BrandAssets\\RogerSkateCo\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_Roger_Doodler_01_BC", RelativePath = "Customization\\BrandAssets\\RogerSkateCo\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_Roger_GhostBoner_01_BC", RelativePath = "Customization\\BrandAssets\\RogerSkateCo\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_Roger_HalfRoger_01_BC", RelativePath = "Customization\\BrandAssets\\RogerSkateCo\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_Roger_SkateSwitch_01_BC", RelativePath = "Customization\\BrandAssets\\RogerSkateCo\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_Roger_SwordSwallower_01_BC", RelativePath = "Customization\\BrandAssets\\RogerSkateCo\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_Roger_WeedCobra_01_BC", RelativePath = "Customization\\BrandAssets\\RogerSkateCo\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_CusLogo_RogerSkateCo_BC", RelativePath = "Customization\\BrandAssets\\RogerSkateCo\\Logos" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_RumCom_BlackLarge_Hoodie", RelativePath = "Customization\\BrandAssets\\RumCom\\UpperBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_RumCom_BlackHoodieLarge_BC", RelativePath = "Customization\\BrandAssets\\RumCom\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_AllRed_01_BC", RelativePath = "Customization\\BrandAssets\\Schlaudie\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_OG_01_BC", RelativePath = "Customization\\BrandAssets\\Schlaudie\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_RoofTop_01_BC", RelativePath = "Customization\\BrandAssets\\Schlaudie\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_CusLogo_SchlaudieSkateCo_BC", RelativePath = "Customization\\BrandAssets\\Schlaudie\\Logos" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AFXX_Body", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AFXX_Body_Underwear", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AFXX_EyeRefractive", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AFXX_Hair", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AFXX_Head", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AFXX_Hair", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AFXX_Feet_Base", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AFXX_Head_Base", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AFXX_LowerBody_Base", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AFXX_UpperBody_Base", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Body_AO", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Body_BC", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Body_GL", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Body_NM", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Body_RG", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Body_Underwear_AO", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Body_Underwear_BC", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Body_Underwear_NM", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Body_Underwear_RG", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Cap_AO", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Cap_BC", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Cap_MT", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Cap_NM", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Cap_RG", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Eye_BC", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Hair_AL", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Hair_BC", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Hair_GL", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Hair_NM", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Hair_SP", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Head_AO", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Head_BC", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Head_NM", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Head_RG", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Jeans_AO", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Jeans_BC", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Jeans_MT", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Jeans_NM", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Jeans_RG", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoe_AO", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoe_BC", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoe_MT", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoe_NM", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoe_RG", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoe_Square3_01_BC", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoe_Square3_01_MG", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoe_Square3_01_NM", RelativePath = "Customization\\Characters\\AFXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_Shoes_Vul_A_eS_Square3_R_BlackGrey", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_Shoes_Vul_A_eS_Square3_R_BlackGum", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_Shoes_Vul_A_eS_Square3_R_BlackLeather", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_Shoes_Vul_A_eS_Square3_R_BlackWhite", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_Shoes_Vul_A_eS_Square3_R_Bone", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_Shoes_Vul_A_eS_Square3_R_Brown", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_Shoes_Vul_A_eS_Square3_R_Charcoal", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_Shoes_Vul_A_eS_Square3_R_Green", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_Shoes_Vul_A_eS_Square3_R_Navy", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_Shoes_Vul_A_eS_Square3_R_Red", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_R_BlackGrey", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_R_BlackGum", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_R_BlackLeather", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_R_BlackWhite", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_R_Bone", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_R_Brown", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_R_Charcoal", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_R_Green", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_R_Navy", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_R_Red", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_BlackGrey_R_BC", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_BlackGum_R_BC", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_BlackLeather_R_BC", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_BlackLeather_R_MG", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_BlackLeather_R_NM", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_BlackWhite_R_BC", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_Bone_R_BC", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_Brown_R_BC", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_Burgundy_R_BC", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_Charcoal_R_BC", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_Green_R_BC", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_Navy_R_BC", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_Red_R_BC", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_Red_R_MG", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shoes_Vul_A_eS_Square3_Red_R_NM", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Brands\\eS\\eS_Square3\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_FT_Shoes_Cup_A", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_FT_Shoes_Mid_Top_Cup_A", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_FT_Shoes_Mid_Top_Cup_A_02", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_FT_Shoes_Mid_Top_Cup_A_03", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_FT_Shoes_Mid_Top_Cup_A_04", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_FT_Shoes_Vulc_A", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_FT_Shoes_Vulc_A_02", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_FT_Shoes_Vulc_A_03", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_FT_Shoes_Vulc_A_04", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_FT_Shoes_Vulc_A_05", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_FT_Shoes_Vulc_B", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_FT_Shoes_Vulc_B_02", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_FT_Shoes_Vulc_B_03", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_FT_Shoes_Vulc_B_04", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AFXX_Shoes_03", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AFXX_Shoes_04", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AFXX_Shoes_05", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AFXX_Shoes", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AFXX_Shoes_02", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AFXX_GEN_FT_Shoes_Cup_A", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AFXX_GEN_FT_Shoes_MidTop_Cup_A", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AFXX_GEN_FT_Shoes_Vul_A", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AFXX_GEN_FT_Shoes_Vul_B", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AFXX_Shoes_AO", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AFXX_Shoes_BC", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AFXX_Shoes_ID", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AFXX_Shoes_NM", RelativePath = "Customization\\Characters\\AFXX\\Feet\\Generic\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AFXX_Hat_04", RelativePath = "Customization\\Characters\\AFXX\\Head\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AFXX_Hat_05", RelativePath = "Customization\\Characters\\AFXX\\Head\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AFXX_Hairs", RelativePath = "Customization\\Characters\\AFXX\\Head\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AFXX_Hat", RelativePath = "Customization\\Characters\\AFXX\\Head\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AFXX_Hat_02", RelativePath = "Customization\\Characters\\AFXX\\Head\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AFXX_Hat_03", RelativePath = "Customization\\Characters\\AFXX\\Head\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AFXX_GEN_HD_Cap_A", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AFXX_GEN_HD_Cap_B", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AFXX_GEN_HD_Cap_NoHair_A", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AFXX_GEN_HD_Cap_NoHair_B", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AFXX_GEN_HD_Hair_A", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AFXX_GEN_HD_Hat_A", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AFXX_GEN_HD_Hat_NoHair_A", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_HD_Cap_A", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_HD_Cap_A_02", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_HD_Cap_A_03", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_HD_Cap_A_04", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_HD_Cap_A_05", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_HD_Cap_B", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_HD_Cap_B_02", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_HD_Cap_B_03", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_HD_Cap_B_04", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_HD_Cap_No_Hair_A", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_HD_Cap_No_Hair_A_02", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_HD_Cap_No_Hair_A_03", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_HD_Cap_No_Hair_A_04", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_HD_Cap_No_Hair_A_05", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_HD_Cap_No_Hair_B", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_HD_Cap_No_Hair_B_02", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_HD_Cap_No_Hair_B_03", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_HD_Cap_No_Hair_B_04", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_HD_Hair_A", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_HD_Hat_A", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_HD_Hat_No_Hair_A", RelativePath = "Customization\\Characters\\AFXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Cap_02_AO", RelativePath = "Customization\\Characters\\AFXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Cap_02_BC", RelativePath = "Customization\\Characters\\AFXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Cap_02_NM", RelativePath = "Customization\\Characters\\AFXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Cap_AO", RelativePath = "Customization\\Characters\\AFXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Cap_BC", RelativePath = "Customization\\Characters\\AFXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Cap_ID", RelativePath = "Customization\\Characters\\AFXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Cap_NM", RelativePath = "Customization\\Characters\\AFXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Hat_02_AO", RelativePath = "Customization\\Characters\\AFXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Hat_02_BC", RelativePath = "Customization\\Characters\\AFXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Hat_02_NM", RelativePath = "Customization\\Characters\\AFXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Hat_02_RG", RelativePath = "Customization\\Characters\\AFXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AFXX_Pants_03", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AFXX_Pants_04", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AFXX_Socks_02", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AFXX_Socks_03", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AFXX_Socks_04", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AFXX_Socks_05", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AFXX_Pants", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AFXX_Pants_02", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AFXX_Pants_05", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AFXX_Short", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AFXX_Short_02", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AFXX_Short_03", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AFXX_Socks", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AFXX_GEN_LB_Jeans_A", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AFXX_GEN_LB_Shorts_A", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AFXX_GEN_LB_Shorts_Socks_A", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_LB_Jeans_A", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_LB_Jeans_A_01", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_LB_Jeans_A_02", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_LB_Jeans_A_03", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_LB_Jeans_A_04", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_LB_Shorts_A", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_LB_Shorts_A_02", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_LB_Shorts_A_03", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_LB_Shorts_Socks_A", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_LB_Shorts_Socks_A_02", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_LB_Shorts_Socks_A_03", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_LB_Shorts_Socks_A_04", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_LB_Shorts_Socks_A_05", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_LB_Shorts_Socks_A_06", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_LB_Shorts_Socks_A_07", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_LB_Shorts_Socks_A_08", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_LB_Shorts_Socks_A_09", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_LB_Shorts_Socks_A_10", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_LB_Shorts_Socks_A_11", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Pants_02_BC", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Pants_AO", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Pants_BC", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Pants_NM", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Short_02_BC", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Short_AO", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Short_BC", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Short_NM", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Socks_BC", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Socks_NM", RelativePath = "Customization\\Characters\\AFXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_Roger_TShirt_A", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Brands\\Roger\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_Roger_TShirt_A_02", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Brands\\Roger\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_Roger_TShirt_A_03", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Brands\\Roger\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_Roger_TShirt_A_04", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Brands\\Roger\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_Roger_TShirt_A_05", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Brands\\Roger\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_Roger_TShirt_A_06", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Brands\\Roger\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_Roger_TShirt_A_07", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Brands\\Roger\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_Roger_TShirt_A_08", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Brands\\Roger\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_Schlaudie_TShirt_A", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Brands\\Schlaudie\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AFXX_GEN_LongShirt_A", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Materials\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AFXX_GEN_LongShirt_B", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Materials\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AFXX_GEN_LongShirt_C", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Materials\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AFXX_GEN_Shirt_A", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Materials\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AFXX_GEN_Shirt_B", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Materials\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AFXX_GEN_Shirt_C", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Materials\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AFXX_TShirt", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Materials\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AFXX_GEN_LongShirt", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Materials\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AFXX_GEN_Shirt", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Materials\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AFXX_GEN_Tanktop", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Materials\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AFXX_GEN_UB_Hoodie_A", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Skelmesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AFXX_GEN_UB_Shirt_A", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Skelmesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AFXX_GEN_UB_TankTop_A", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Skelmesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AFXX_GEN_UB_TShirt_A", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Skelmesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_UB_Hoodie_A", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Skelmesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_UB_Hoodie_B", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Skelmesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_UB_Hoodie_C", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Skelmesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_UB_Hoodie_D", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Skelmesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_UB_Shirt_A", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Skelmesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_UB_Shirt_A_02", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Skelmesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_UB_Shirt_A_03", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Skelmesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_UB_Shirt_A_04", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Skelmesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_UB_TankTop_A", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Skelmesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_UB_TankTop_A_02", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Skelmesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_UB_TankTop_A_03", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Skelmesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_UB_TankTop_A_04", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Skelmesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_UB_TankTop_A_05", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Skelmesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_UB_Tshirt_A", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Skelmesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_UB_Tshirt_A_02", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Skelmesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AFXX_GEN_UB_Tshirt_A_03", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\Skelmesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Gen_TShirt_BC", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shirt_AO", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shirt_BC", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shirt_ID", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_Shirt_NM", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_TankTop_AO", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_TankTop_B_BC", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_TankTop_BC", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_TankTop_NM", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AFXX_TankTop_RG", RelativePath = "Customization\\Characters\\AFXX\\UpperBody\\textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Body", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_EyeRefractive_", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Hair", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Head", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Hair", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_NoHair", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Feet_Base", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Head_Base", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_LowerBody_Base", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_UpperBody_Base", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Hair_BC", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Hair_GL", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Hair_GR", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Hair_NM", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Hair_SP", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_Body_AO", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_Body_BC", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_Body_GL", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_Body_NM", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_Body_Rough", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_Hair_AL", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_Hair_BC", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_Head_AO", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_Head_BC", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_Head_GL", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_Head_NM", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_Head_RG", RelativePath = "Customization\\Characters\\AMXX\\Body_Base\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_GEN_CupSole_Shoes_A", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_GEN_CupSole_Shoes_B", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_GEN_CupSole_Shoes_C", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_GEN_CupSole_Shoes_D", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_GEN_CupSole_Shoes_E", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_GEN_Vulc_Shoes_2_A", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_GEN_Vulc_Shoes_2_B", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_GEN_Vulc_Shoes_2_C", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_GEN_Vulc_Shoes_2_D", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_GEN_VulcShoes_A", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_GEN_VulcShoes_B", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_GEN_CupSole_Shoes", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_GEN_CupSole_Shoes_2", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_GEN_CupSole_Shoes_3", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_GEN_CupSole_Shoes_4", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_GEN_Vulc_Shoes", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "NewMaterial1", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_GEN_FT_Shoes_MidTop_Cup_A", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_GEN_FT_Shoes_Vul_A", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_FT_Shoes_MidTop_Cup", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_FT_Shoes_MidTop_Cup_01", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_FT_Shoes_MidTop_Cup_02", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_FT_Shoes_MidTop_Cup_03", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_FT_Shoes_MidTop_Cup_04", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_FT_Shoes_MidTop_Cup_05", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_FT_Shoes_MidTop_Cup_06", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_FT_Shoes_Vul_A", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_FT_Shoes_Vul_A_01", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_FT_Shoes_Vul_A_02", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_FT_Shoes_Vul_A_03", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_FT_Shoes_Vul_A_04", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_FT_Shoes_Vul_A_05", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_CupSole_Shoes_02_AO", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_CupSole_Shoes_02_BC", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_CupSole_Shoes_02_ID", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_CupSole_Shoes_02_MT", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_CupSole_Shoes_02_NM", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_CupSole_Shoes_02_RG", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_CupSole_Shoes_03_BC", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_CupSole_Shoes_04_BC", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_CupSole_Shoes_crea-ture_BC", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Shoes_AO", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Shoes_D", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Shoes_Gloss", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Shoes_ID", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Shoes_NM", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_ShoesLightGrey_D", RelativePath = "Customization\\Characters\\AMXX\\Feet\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_GEN_HD_Cap_Jenkem_A", RelativePath = "Customization\\Characters\\AMXX\\Head\\Brands\\Jenkem" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_GEN_HD_Cap_Jenkem_No_Hair_A", RelativePath = "Customization\\Characters\\AMXX\\Head\\Brands\\Jenkem" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_Jenkem_Cap_A", RelativePath = "Customization\\Characters\\AMXX\\Head\\Brands\\Jenkem\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_Jenkem_Cap_B", RelativePath = "Customization\\Characters\\AMXX\\Head\\Brands\\Jenkem\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_Jenkem_Cap_C", RelativePath = "Customization\\Characters\\AMXX\\Head\\Brands\\Jenkem\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_Jenkem_Cap_No_Hair_A", RelativePath = "Customization\\Characters\\AMXX\\Head\\Brands\\Jenkem\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_Jenkem_Cap_No_Hair_B", RelativePath = "Customization\\Characters\\AMXX\\Head\\Brands\\Jenkem\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_Jenkem_Cap_No_Hair_C", RelativePath = "Customization\\Characters\\AMXX\\Head\\Brands\\Jenkem\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Jenkem_Cap_01", RelativePath = "Customization\\Characters\\AMXX\\Head\\Brands\\Jenkem\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Jenkem_Cap_02", RelativePath = "Customization\\Characters\\AMXX\\Head\\Brands\\Jenkem\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Jenkem_Cap_03", RelativePath = "Customization\\Characters\\AMXX\\Head\\Brands\\Jenkem\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_JenkemCap_Black_BC", RelativePath = "Customization\\Characters\\AMXX\\Head\\Brands\\Jenkem\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_JenkemCap_Green_BC", RelativePath = "Customization\\Characters\\AMXX\\Head\\Brands\\Jenkem\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_JenKemCap_NM", RelativePath = "Customization\\Characters\\AMXX\\Head\\Brands\\Jenkem\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_JenkemCap_Yellow_BC", RelativePath = "Customization\\Characters\\AMXX\\Head\\Brands\\Jenkem\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Gen_Beanie_C", RelativePath = "Customization\\Characters\\AMXX\\Head\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Gen_Beanie_D", RelativePath = "Customization\\Characters\\AMXX\\Head\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Gen_Beanie_F", RelativePath = "Customization\\Characters\\AMXX\\Head\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Gen_Beanie_G", RelativePath = "Customization\\Characters\\AMXX\\Head\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Gen_Cap_05", RelativePath = "Customization\\Characters\\AMXX\\Head\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Gen_Cap_06", RelativePath = "Customization\\Characters\\AMXX\\Head\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Gen_Cap_07", RelativePath = "Customization\\Characters\\AMXX\\Head\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Gen_Cap_08", RelativePath = "Customization\\Characters\\AMXX\\Head\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Gen_Beanie", RelativePath = "Customization\\Characters\\AMXX\\Head\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Gen_Beanie_B", RelativePath = "Customization\\Characters\\AMXX\\Head\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Gen_Cap_01", RelativePath = "Customization\\Characters\\AMXX\\Head\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Gen_Cap_02", RelativePath = "Customization\\Characters\\AMXX\\Head\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Gen_Cap_03", RelativePath = "Customization\\Characters\\AMXX\\Head\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Gen_Cap_04", RelativePath = "Customization\\Characters\\AMXX\\Head\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Hairs", RelativePath = "Customization\\Characters\\AMXX\\Head\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_GEN_HD_Beanie_A", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_GEN_HD_Beanie_NoHair_A", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_GEN_HD_Cap_A", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_GEN_HD_Cap_NoHair_A", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_GEN_HD_Hair_A", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_Beanie_A", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_Beanie_A_02", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_Beanie_A_03", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_Beanie_A_04", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_Beanie_A_05", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_Beanie_A_06", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_Beanie_NoHair_A", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_Beanie_NoHair_A_02", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_Beanie_NoHair_A_03", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_Beanie_NoHair_A_04", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_Beanie_NoHair_A_05", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_Beanie_NoHair_A_06", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_Cap_A", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_Cap_A_01", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_Cap_A_02", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_Cap_A_03", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_Cap_A_04", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_Cap_A_05", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_Cap_A_06", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_CapNo_Hair_A", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_CapNo_Hair_A_02", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_CapNo_Hair_A_03", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_CapNo_Hair_A_04", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_CapNo_Hair_A_05", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_CapNo_Hair_A_06", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_CapNo_Hair_A_07", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_HD_Hair_A", RelativePath = "Customization\\Characters\\AMXX\\Head\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cap_02_AO", RelativePath = "Customization\\Characters\\AMXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cap_02_BC", RelativePath = "Customization\\Characters\\AMXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cap_02_NM", RelativePath = "Customization\\Characters\\AMXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cap_03_AO", RelativePath = "Customization\\Characters\\AMXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cap_03_BC", RelativePath = "Customization\\Characters\\AMXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cap_03_NM", RelativePath = "Customization\\Characters\\AMXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cap_04_AO", RelativePath = "Customization\\Characters\\AMXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cap_04_BC", RelativePath = "Customization\\Characters\\AMXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cap_04_NM", RelativePath = "Customization\\Characters\\AMXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cap_AO", RelativePath = "Customization\\Characters\\AMXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cap_BC", RelativePath = "Customization\\Characters\\AMXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cap_ID", RelativePath = "Customization\\Characters\\AMXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cap_NM", RelativePath = "Customization\\Characters\\AMXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cap_RG", RelativePath = "Customization\\Characters\\AMXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Tuque_AO", RelativePath = "Customization\\Characters\\AMXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Tuque_C_RG", RelativePath = "Customization\\Characters\\AMXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Tuque_D", RelativePath = "Customization\\Characters\\AMXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Tuque_D_D1", RelativePath = "Customization\\Characters\\AMXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Tuque_E", RelativePath = "Customization\\Characters\\AMXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Tuque_E_ID", RelativePath = "Customization\\Characters\\AMXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Tuque_Gloss", RelativePath = "Customization\\Characters\\AMXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Tuque_NM", RelativePath = "Customization\\Characters\\AMXX\\Head\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_LB_NoComply_Cargo_Pants_A", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Brands\\NoComply\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_LB_NoComply_Cargo_Pants_A_02", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Brands\\NoComply\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_NoComply_CargoPants", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Brands\\NoComply\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_NoComply_CargoPants_02", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Brands\\NoComply\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_UB_NoComply_CargoPants_01_MG", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Brands\\NoComply\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_UB_NoComply_CargoPants_02_MG", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Brands\\NoComply\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_LB_Cargo_Pants_A", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_LB_Cargo_Pants_A_02", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_LB_Cargo_Pants_A_03", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_LB_Cargo_Pants_A_04", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_LB_Cargo_Pants_A_05", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_LB_Cargo_Pants_A_06", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_LB_Cargo_Pants_A_07", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_LB_Cargo_Pants_A_08", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_LB_StraightCut_Pants_A", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_LB_StraightCut_Pants_A_02", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_LB_StraightCut_Pants_A_03", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_LB_StraightCut_Pants_A_04", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Pants", RelativePath = "Customization\\Characters\\AMXX\\LowerBody" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Gen_Cargo_Pants_Burgundy", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Gen_Cargo_Pants_Gray", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Gen_Cargo_Pants_Green", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Gen_BlackCargo_Pants", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Gen_Cargo_Pants_01", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Gen_Cargo_Pants_Beige", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Gen_Cargo_Pants_Beige_02", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Gen_Cargo_Pants_Jean", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Gen_StraightCut_Black", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Gen_StraightCut_Burgundy", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Gen_StraightCut_Jean", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Gen_StraightCut_Tan", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_GEN_LB_Cargo_Pants_A", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_GEN_LB_StraightCut_Pants_A", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cargo_Pants_01_AO", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cargo_Pants_01_BC", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cargo_Pants_01_NM", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cargo_Pants_01_RG", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cargo_Pants_02_AO", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cargo_Pants_02_D", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cargo_Pants_02_NM", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cargo_Pants_03_AO", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cargo_Pants_03_D", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cargo_Pants_03_NM", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Cargo_Pants_04_D", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_StraightCut_02_AO", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_StraightCut_02_D", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_StraightCut_02_NM", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_StraightCut_03_AO", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_StraightCut_03_D", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_StraightCut_03_NM", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_StraightCut_AO", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_StraightCut_D", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_StraightCut_Gloss", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_StraightCut_NM", RelativePath = "Customization\\Characters\\AMXX\\LowerBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_Hoodie_A", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Jenkem\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_Jenkem_TShirt_A", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Jenkem\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_Jenkem_TShirt_A_2", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Jenkem\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_HoodieJenkem_Black", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Jenkem\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_ShirtJenkem_Black", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Jenkem\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_ShirtJenkem_White", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Jenkem\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_JenkemHoodie_Black_BC", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Jenkem\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_UB_Jenkem_TShirt_01_BC", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Jenkem\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_UB_Jenkem_TShirt_01_MG", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Jenkem\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_UB_Jenkem_TShirtBlack_01_BC", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Jenkem\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_UB_Jenkem_TShirtBlack_01_RN", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Jenkem\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_NoComply_TShirt_A", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_NoComply_TShirt_A_01", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_NoComply_TShirt_A_02", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_NoComply_TShirt_A_03", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_NoComply_TShirt_A_04", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_NoComply_TShirt_A_05", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_NoComply_TShirt_A_07", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_NoComply_TShirt_A_08", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_NoComply_TShirt_A_10", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_NoComply_TShirt_01", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_NoComply_TShirt_02", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_NoComply_TShirt_03", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_NoComply_TShirt_04", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_NoComply_TShirt_05", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_NoComply_TShirt_06", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_NoComply_TShirt_07", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_NoComply_TShirt_08", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_NoComply_TShirt_10", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_NoComply_TShirt", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_ComplyShirt_Black_02_BC", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_ComplyShirt_Black_BC", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_ComplyShirt_Blue_BC", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_ComplyShirt_BrightBlue_BC", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_ComplyShirt_BrightYellow_BC", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_ComplyShirt_Green_BC", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_ComplyShirt_Pink_BC", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_ComplyShirt_White_BC", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_ComplyShirt_Yellow_BC", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\No-Comply\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_Roger_TShirt_A", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_Roger_TShirt_A_02", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_Roger_TShirt_A_03", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_Roger_TShirt_A_04", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_Roger_TShirt_A_05", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_Roger_TShirt_A_06", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_Roger_TShirt_A_07", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_Roger_TShirt_A_08", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Roger_TShirt_02", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Roger_TShirt_03", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Roger_TShirt_04", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Roger_TShirt_05", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Roger_TShirt_06", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Roger_TShirt_07", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Roger_TShirt_08", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Roger_TShirt", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_RogerShirt_Black_BC", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_RogerShirt_Doodle_BC", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_RogerShirt_SausageBlue_BC", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_RogerShirt_SausageDarkBlue_BC", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_RogerShirt_SausageGray_BC", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_RogerShirt_SausageGrayBlue_BC", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_RogerShirt_Snake_BC", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_RogerShirt_Sword_BC", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Roger\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_Schlaudie_TShirt_A", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Schlaudie\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Schlaudie_TShirt", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Schlaudie\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_SchlaudieShirt_Black_BC", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Brands\\Schlaudie\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_Hoodie_00", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_Hoodie_01", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_Hoodie_02", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_Hoodie_03", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_Hoodie_04", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_Hoodie_05", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_Hoodie_06", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_Jacket_01", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_Jacket_02", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_Jacket_03", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_TShirt_01", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_TShirt_02", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_TShirt_03", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_TShirt_04", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Generic\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Hoodie_C_Beige", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Hoodie_C_Black", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Hoodie_C_Yellow", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Hoodie_Green", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Hoodie_Red", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Jacket_Green", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_Jacket_Red", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_TShirt_02", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_TShirt_03", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_TShirt_04", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_AMXX_TShirt_A_01", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Basic_Tshirt", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_HeatherGrey_Hoodie", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Hoodie", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Hoodie_Beige", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Hoodie_Black", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Hoodie_C_White", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Jacket_Black", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Jacket_White", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_GEN_UB_Hoodie_A", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_GEN_UB_Sweather_A", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_GEN_UB_Tshirt_A", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_GEN_UB_WindBreaker_A", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_UB_Hoodie_A", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_AMXX_GEN_UB_Tshirt_A", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Skelmeshes" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Hoodie_AO", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Hoodie_B", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Hoodie_B_D", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Hoodie_B_NM", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Hoodie_Black_BC", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Hoodie_C", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Hoodie_C_D", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Hoodie_C_ID", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Hoodie_C_NM", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Hoodie_D", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Hoodie_Gloss", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Hoodie_NM", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Jacket_02_D", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Jacket_AO", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Jacket_D", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Jacket_ID", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Gen_Jacket_NM", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Session_Hoodie_Heather_Grey_D", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_BaseMesh_BoxArtShirt_BC", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_BaseMesh_Shirt_Specular", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_Gen_TShirt_BC_01", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_Gen_TShirt_BC_02", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_Gen_TShirt_BC_03", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_Gen_TShirt_BC_04", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_Gen_TShirt_NM", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_AMXX_Gen_TShirt_RMO", RelativePath = "Customization\\Characters\\AMXX\\UpperBody\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Gen_RibsmanCargo_Pants", RelativePath = "Customization\\Characters\\Uniques\\Ribsman\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Ribsman_Costume", RelativePath = "Customization\\Characters\\Uniques\\Ribsman\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_AMXX_Ribsman_Mask", RelativePath = "Customization\\Characters\\Uniques\\Ribsman\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Ribsman_Body_AO", RelativePath = "Customization\\Characters\\Uniques\\Ribsman\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Ribsman_Body_BC", RelativePath = "Customization\\Characters\\Uniques\\Ribsman\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Ribsman_Body_NM", RelativePath = "Customization\\Characters\\Uniques\\Ribsman\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Ribsman_Face_AO", RelativePath = "Customization\\Characters\\Uniques\\Ribsman\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Ribsman_Face_BC", RelativePath = "Customization\\Characters\\Uniques\\Ribsman\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "AMXX_Ribsman_Face_NM", RelativePath = "Customization\\Characters\\Uniques\\Ribsman\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_NONE", RelativePath = "Customization" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "DATASS_DatabaseCategories", RelativePath = "Customization" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "DATASS_DatabaseCustomization", RelativePath = "Customization" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "PBP_CharacterCustomization", RelativePath = "Customization" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "PBP_CustomizationCharacter", RelativePath = "Customization" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_Default_Blank725_01", RelativePath = "Customization\\Skateboards\\Basemesh\\Decks\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_Default_Blank750_01", RelativePath = "Customization\\Skateboards\\Basemesh\\Decks\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_Default_Blank775_01", RelativePath = "Customization\\Skateboards\\Basemesh\\Decks\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_Default_Blank800_01", RelativePath = "Customization\\Skateboards\\Basemesh\\Decks\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_Default_Blank8125_01", RelativePath = "Customization\\Skateboards\\Basemesh\\Decks\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_Default_Blank8250_01", RelativePath = "Customization\\Skateboards\\Basemesh\\Decks\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DG_Default_Blank850_01", RelativePath = "Customization\\Skateboards\\Basemesh\\Decks\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Deck_Default_7250", RelativePath = "Customization\\Skateboards\\Basemesh\\Decks\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Deck_Default_7500", RelativePath = "Customization\\Skateboards\\Basemesh\\Decks\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Deck_Default_7750", RelativePath = "Customization\\Skateboards\\Basemesh\\Decks\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Deck_Default_8000", RelativePath = "Customization\\Skateboards\\Basemesh\\Decks\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Deck_Default_8125", RelativePath = "Customization\\Skateboards\\Basemesh\\Decks\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Deck_Default_8125_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Decks\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Deck_Default_8250", RelativePath = "Customization\\Skateboards\\Basemesh\\Decks\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Deck_Default_8500", RelativePath = "Customization\\Skateboards\\Basemesh\\Decks\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Truck_BasePlate_Default_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Trucks\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Truck_BasePlate_Default_B", RelativePath = "Customization\\Skateboards\\Basemesh\\Trucks\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Truck_Hanger_Default_8125_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Trucks\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Truck_Hanger_Default_8125_B", RelativePath = "Customization\\Skateboards\\Basemesh\\Trucks\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_CLassic_Default_46_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_CLassic_Default_48_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_CLassic_Default_50_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_CLassic_Default_51_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_CLassic_Default_52_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_CLassic_Default_53_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_CLassic_Default_54_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_CLassicWide_Default_46_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_CLassicWide_Default_48_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_CLassicWide_Default_50_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_CLassicWide_Default_51_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_CLassicWide_Default_52_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_CLassicWide_Default_53_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_CLassicWide_Default_54_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_ConicWide_Default_46_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_ConicWide_Default_48_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_ConicWide_Default_50_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_ConicWide_Default_51_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_ConicWide_Default_52_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_ConicWide_Default_53_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_ConicWide_Default_54_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_Default_RadialWide_46_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_Default_RadialWide_48_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_Default_RadialWide_50_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_Default_RadialWide_52_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_Default_RadialWide_54_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Wheel_Default_RadialWide_56_A", RelativePath = "Customization\\Skateboards\\Basemesh\\Wheels\\Default" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_BR_Default_A_01", RelativePath = "Customization\\Skateboards\\Bearings\\Default\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_BR_Default_A_01_BC", RelativePath = "Customization\\Skateboards\\Bearings\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_BR_Default_A_01_MG", RelativePath = "Customization\\Skateboards\\Bearings\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_BR_Default_A_01_NM", RelativePath = "Customization\\Skateboards\\Bearings\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_DG_Default_01", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Default\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_DG_Default_EMPTY", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Default\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Default_01_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Default_EMPTY_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "Heroin_Deck_03", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Basemesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "Heroin_Deck_Anatomy", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Basemesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "Heroin_Deck_BigEgg_Purple", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Basemesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "Heroin_Deck_BigEgg_Yellow", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Basemesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "Heroin_Deck_BlackEye_01", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Basemesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "Heroin_Deck_Fish", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Basemesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "Heroin_Deck_Shovel_01", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Basemesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "Heroin_Deck_Spiral_01", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Basemesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "Heroin_Deck_TheEgg", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Basemesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "Heroin_Deck_VeryBigEgg", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Basemesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_DG_Heroin_01", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_DG_Heroin_02", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_DG_Heroin_03", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_DG_Heroin_04", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_DG_Heroin_05", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_DG_Heroin_Anatomy", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_DG_Heroin_VeryBigEgg", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_Heroin_GID_BlackEye", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_Heroin_GID_Eye1", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_Heroin_GID_Eye2", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_Heroin_GID_Eye3", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_WB_Heroin_01", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_WB_Heroin_02", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Heroin_Anatomy_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Heroin_BlackEye_01_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Heroin_BlackEye_01_ID", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Heroin_Eye1_01_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Heroin_Eye1_01_ID", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Heroin_Eye2_01_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Heroin_Eye2_01_ID", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Heroin_Eye3_01_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Heroin_Eye3_01_ID", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Heroin_ShovelHead_01_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Heroin_Spiral_01_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Heroin_TheEgg_01_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Heroin_TheEgg_02_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Heroin_VeryBigEgg_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\Heroin\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Deck_NoComply_8000", RelativePath = "Customization\\Skateboards\\DeckGraphics\\NoComply\\Basemesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Deck_NoComply_8125", RelativePath = "Customization\\Skateboards\\DeckGraphics\\NoComply\\Basemesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Deck_NoComply_8250", RelativePath = "Customization\\Skateboards\\DeckGraphics\\NoComply\\Basemesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Deck_NoComply_8500", RelativePath = "Customization\\Skateboards\\DeckGraphics\\NoComply\\Basemesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "STM_SKXX_Deck_NoComply_8501", RelativePath = "Customization\\Skateboards\\DeckGraphics\\NoComply\\Basemesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_DG_NoComply_01", RelativePath = "Customization\\Skateboards\\DeckGraphics\\NoComply\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_WB_NoComply_01", RelativePath = "Customization\\Skateboards\\DeckGraphics\\NoComply\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_WB_NoComply_02", RelativePath = "Customization\\Skateboards\\DeckGraphics\\NoComply\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_WB_NoComply_03", RelativePath = "Customization\\Skateboards\\DeckGraphics\\NoComply\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_WB_NoComply_04", RelativePath = "Customization\\Skateboards\\DeckGraphics\\NoComply\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_WB_NoComply_05", RelativePath = "Customization\\Skateboards\\DeckGraphics\\NoComply\\Material" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_NoComply_01_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\NoComply\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_NoComply_02_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\NoComply\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_GhostBoner_01_BC_Scratched", RelativePath = "Customization\\Skateboards\\DeckGraphics\\RogerSkateCo\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_GhostBoner_01_Scratched_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\RogerSkateCo\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Roger_DogBalls_01_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\RogerSkateCo\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Roger_Doodler_01_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\RogerSkateCo\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Roger_GhostBoner_01_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\RogerSkateCo\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Roger_HalfRoger_01_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\RogerSkateCo\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Roger_SkateSwitch_01_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\RogerSkateCo\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Roger_SwordSwallower_01_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\RogerSkateCo\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Roger_WeedCobra_01_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\RogerSkateCo\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Schlaudie_RedLogo_01_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\SchlaudieSkateCo" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Schlaudie_Rooftop_01_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\SchlaudieSkateCo" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DG_Schlaudie_Rooftop_02_BC", RelativePath = "Customization\\Skateboards\\DeckGraphics\\SchlaudieSkateCo" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_DL_DeckLayers_01", RelativePath = "Customization\\Skateboards\\DeckLayers\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DL_Default_01_BC", RelativePath = "Customization\\Skateboards\\DeckLayers\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DL_Default_01_MG", RelativePath = "Customization\\Skateboards\\DeckLayers\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DL_Default_02_BC1", RelativePath = "Customization\\Skateboards\\DeckLayers\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DL_Default_02_MG", RelativePath = "Customization\\Skateboards\\DeckLayers\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DL_Default_03_MG", RelativePath = "Customization\\Skateboards\\DeckLayers\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_DL_Default_04_MG", RelativePath = "Customization\\Skateboards\\DeckLayers\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DL_Default_01_BC", RelativePath = "Customization\\Skateboards\\DeckLayers\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DL_Default_01_MG", RelativePath = "Customization\\Skateboards\\DeckLayers\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DL_Default_01_NM", RelativePath = "Customization\\Skateboards\\DeckLayers\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DL_Default_02_BC", RelativePath = "Customization\\Skateboards\\DeckLayers\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DL_Default_03_BC", RelativePath = "Customization\\Skateboards\\DeckLayers\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DL_Default_04_BC", RelativePath = "Customization\\Skateboards\\DeckLayers\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DL_Default_05_BC", RelativePath = "Customization\\Skateboards\\DeckLayers\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_DL_Default_06_BC", RelativePath = "Customization\\Skateboards\\DeckLayers\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_GT_Default_01_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_GT_Default_02_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_GT_Default_03_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_GT_Default_04_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_GT_Default_05_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_GT_Default_06_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_GT_Default_07_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_GT_Default_08_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_GT_Default_09_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_GT_Default_10_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_GT_Default_11_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_GT_Default_12_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_GT_Default_13_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_GT_Default_01", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_GT_Default_02", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_GT_Default_03", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_GT_Default_04", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_GT_Default_05", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_GT_Default_06", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_GT_Default_07", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_GT_Default_08", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_GT_Default_09", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_GT_Default_10", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_GT_Default_11", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_GT_Default_12", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_GT_Default_13", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_GT_crea-ture_Scratched_01_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_GT_Default_01_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_GT_Default_01_NM", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_GT_GripDesign_01_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_GT_GripDesign_02_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_GT_GripDesign_03_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_GT_GripDesign_04_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_GT_GripDesign_05_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_GT_GripDesign_06_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_GT_GripDesign_07_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_GT_GripDesign_08_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_GT_GripDesign_09_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_GT_SessionOG_Clean_01_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_GT_SessionOG_Scratched_01_BC", RelativePath = "Customization\\Skateboards\\GripTapes\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "SKXX_Customization_Base", RelativePath = "Customization\\Skateboards\\Skelmesh" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_TK_NT_Default_02", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Axle\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_Axle_BC", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Axle\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_Axle_MG", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Axle\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_Axle_NM", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Axle\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_TK_BP_Default_01", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Baseplate\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_TK_BP_Default_02", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Baseplate\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_TK_BP_Default_01_BC", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Baseplate\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_TK_BP_Default_01_MG", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Baseplate\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_Baseplate_BC", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Baseplate\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_Baseplate_MG", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Baseplate\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_Baseplate_NM", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Baseplate\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_TK_BP_Default_01_BC", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Baseplate\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_TK_BP_Default_01_MG", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Baseplate\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_TK_BP_Default_01_NM", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Baseplate\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_TK_BG_Default_01", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Bushing\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_TK_BG_Default_02", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Bushing\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_Bushing_BC", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Bushing\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_Bushing_MG", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Bushing\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_Bushing_NM", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Bushing\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_TK_BG_Default_01_BC", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Bushing\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_TK_BG_Default_01_MG", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Bushing\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_TK_BG_Default_01_NM", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Bushing\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_TK_HG_Default_01", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Hanger\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_TK_HG_Default_02", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Hanger\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_Hanger_BC", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Hanger\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_Hanger_MG", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Hanger\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_Hanger_NM", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Hanger\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_TK_HG_Default_01_BC", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Hanger\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_TK_HG_Default_01_MG", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Hanger\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_TK_HG_Default_01_NM", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Hanger\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_TK_NT_Default_01", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Nuts\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_TK_NT_Default_01_BC", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Nuts\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_TK_NT_Default_01_MG", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Nuts\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_TK_NT_Default_01_NM", RelativePath = "Customization\\Skateboards\\Truck\\Default\\Nuts\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_Classic46_01_BC2", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_Classic48_01_BC1", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_Classic50_01_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_Classic50_02_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_Classic50_03_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_Classic50_04_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_Classic51_01_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_Classic52_01_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_Classic53_01_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_Classic54_01_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_ClassicWide46_01", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_ClassicWide48_01_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_ClassicWide50_01_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_ClassicWide50_02_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_ClassicWide50_03_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_ClassicWide50_04_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_ClassicWide51_01_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_ClassicWide52_01_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_ClassicWide53_01_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_ClassicWide54_01_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_ConicWide46_01_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_ConicWide48_01_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_ConicWide50_01_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_ConicWide50_02_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_ConicWide50_03_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_ConicWide50_04_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_ConicWide51_01_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_ConicWide52_01_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_ConicWide53_01_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_ConicWide54_01_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_RadialWide46_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_RadialWide48_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_RadialWide50_02BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_RadialWide50_03BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_RadialWide50_04BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_RadialWide50_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_RadialWide52_01_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_RadialWide54_01_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CIT_WL_Default_RadialWide56_01_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\CIT" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_WL_Default_01", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_WL_Default_02", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_WL_Default_03", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAI_WL_Default_04", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_WL_Default_01_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_WL_Default_01_MG", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_WL_Default_01_NM", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_WL_Default_02_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_WL_Default_Blank_01_BC", RelativePath = "Customization\\Skateboards\\Wheel\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_WB_Default_01", RelativePath = "Customization\\Skateboards\\Wood\\Default\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "MAT_WT_Default_01", RelativePath = "Customization\\Skateboards\\Wood\\Default\\Materials" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_WD_Default_Clean_BC", RelativePath = "Customization\\Skateboards\\Wood\\Default\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "TEX_WT_RogerBurger_BC", RelativePath = "Customization\\Skateboards\\Wood\\RogerSkateCo" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "PBP_CustomizationGrid_Widget", RelativePath = "Customization\\UI" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "PBP_CustomizationGridItem_Widget", RelativePath = "Customization\\UI" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "PBP_InputController_Widget", RelativePath = "Customization\\UI" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "CRT_CustomizationCharacter", RelativePath = "Customization\\UI\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "GridItem_HatImage", RelativePath = "Customization\\UI\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "GridItem_NoneImage", RelativePath = "Customization\\UI\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "GridItem_PantsImage", RelativePath = "Customization\\UI\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "GridItem_ShirtImage", RelativePath = "Customization\\UI\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "GridItem_ShoesImage", RelativePath = "Customization\\UI\\Textures" });
            _texturePaths.Add(new TexturePathInfo() { TextureName = "Mat_RenderTarget", RelativePath = "Customization\\UI\\Textures" });
        }
    }

    public class TexturePathInfo
    {
        public string TextureName { get; set; }
        public string RelativePath { get; set; }


    }
}
