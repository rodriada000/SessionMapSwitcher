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

        public bool IsPathToZip
        {
            get
            {
                if (PathToFile == null)
                    return false;

                return PathToFile.EndsWith(".zip");
            }
        }

        internal void BrowseForFiles()
        {
            using (OpenFileDialog fileBrowserDialog = new OpenFileDialog())
            {
                fileBrowserDialog.Filter = "uasset file (*.uasset)|*.uasset|Zip files (*.zip)|*.zip|All files (*.*)|*.*";
                fileBrowserDialog.Title = "Select .uasset Texture File or .zip File Containing Texture Files";
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

            if (IsPathToZip)
            {
                Directory.CreateDirectory(PathToTempFolder);

                // extract to temp location
                BoolWithMessage didExtract = FileUtils.ExtractZipFile(PathToFile, PathToTempFolder);
                if (didExtract.Result == false)
                {
                    MessageChanged?.Invoke($"Failed to extract .zip file: {didExtract.Message}");
                    return;
                }

                string foundTextureName = FindTextureFileInUnzippedTempFolder();

                if (foundTextureName == "")
                {
                    MessageChanged?.Invoke($"Failed to find a .uasset file inside the .zip");
                    return;
                }

                textureFileInfo = new FileInfo(foundTextureName);
            }
            else
            {
                textureFileInfo = new FileInfo(PathToFile);
            }

            // find which folder to copy to based on file name
            string targetFolder = GetFolderPathToTexture(textureFileInfo.NameWithoutExtension());

            if (targetFolder == "")
            {
                MessageChanged?.Invoke($"Failed to find path to original texture: {textureFileInfo.Name}");
                return;
            }

            try
            {
                // find and copy files that match the .uasset name
                string textureDirectory = Path.GetDirectoryName(textureFileInfo.FullName);

                foreach (string file in Directory.GetFiles(textureDirectory))
                {
                    if (file.Contains(textureFileInfo.NameWithoutExtension()))
                    {
                        FileInfo foundFile = new FileInfo(file);
                        string targetPath = Path.Combine(targetFolder, foundFile.Name);
                        File.Copy(file, targetPath, overwrite: true);
                    }
                }

                // delete temp folder with unzipped files
                if (IsPathToZip)
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

            if (PathToFile.EndsWith(".zip") == false && PathToFile.EndsWith(".uasset") == false)
            {
                MessageChanged?.Invoke("The selected file is invalid. You must choose a .zip or .uasset file.");
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
