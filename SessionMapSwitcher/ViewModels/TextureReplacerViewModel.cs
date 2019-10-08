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
            if (IsPathValid() == false)
            {
                return;
            }

            FileInfo textureFileInfo = null;

            if (IsPathToCompressedFile)
            {
                Directory.CreateDirectory(PathToTempFolder);

                // extract to temp location
                BoolWithMessage didExtract = FileUtils.ExtractCompressedFile(PathToFile, PathToTempFolder);
                if (didExtract.Result == false)
                {
                    MessageChanged?.Invoke($"Failed to extract file: {didExtract.Message}");
                    return;
                }

                string foundTextureName = FindTextureFileInUnzippedTempFolder();

                if (foundTextureName == "")
                {
                    MessageChanged?.Invoke($"Failed to find a .uasset file inside the extracted folders.");
                    return;
                }

                textureFileInfo = new FileInfo(foundTextureName);
            }
            else
            {
                textureFileInfo = new FileInfo(PathToFile);
            }

            string textureFileName = textureFileInfo.NameWithoutExtension();

            // find which folder to copy to based on file name
            string targetFolder = GetFolderPathToTexture(textureFileName);

            if (targetFolder == "")
            {
                MessageChanged?.Invoke($"Failed to find path to original texture: {textureFileInfo.Name}");
                return;
            }

            try
            {
                DeleteCurrentTextureFiles(textureFileName, targetFolder);

                // find and copy files in source dir that match the .uasset name
                CopyNewTextureFilesToGame(textureFileInfo, targetFolder);

                // delete temp folder with unzipped files
                if (IsPathToCompressedFile)
                {
                    if (Directory.Exists(PathToTempFolder))
                    {
                        Directory.Delete(PathToTempFolder, true);
                    }
                }
            }
            catch (Exception e)
            {
                MessageChanged?.Invoke($"Failed to copy texture files: {e.Message}");
                return;
            }

            MessageChanged?.Invoke($"Successfully replaced textures for {textureFileInfo.Name}!");
        }

        /// <summary>
        /// Loop over all files in the folder that contains the <paramref name="newTexture"/> .uasset file and copy all other files related to texture (.uexp and .ubulk files) to the <paramref name="targetFolder"/>
        /// </summary>
        private static void CopyNewTextureFilesToGame(FileInfo newTexture, string targetFolder)
        {
            string textureSourceDir = Path.GetDirectoryName(newTexture.FullName);
            string textureFileName = newTexture.NameWithoutExtension();

            foreach (string file in Directory.GetFiles(textureSourceDir))
            {
                FileInfo info = new FileInfo(file);

                if (info.NameWithoutExtension() == textureFileName)
                {
                    string targetPath = Path.Combine(targetFolder, info.Name);
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

        private string FindTextureFileInUnzippedTempFolder(string dirToSearch = null)
        {
            if (dirToSearch == null)
            {
                dirToSearch = PathToTempFolder;
            }

            foreach (string fileName in Directory.GetFiles(dirToSearch))
            {
                if (fileName.EndsWith(".uasset"))
                {
                    return fileName;
                }
            }

            foreach (string folder in Directory.GetDirectories(dirToSearch))
            {
                string fileName = FindTextureFileInUnzippedTempFolder(folder);
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
    }
}
