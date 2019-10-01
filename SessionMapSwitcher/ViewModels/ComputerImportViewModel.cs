﻿using SessionMapSwitcher.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace SessionMapSwitcher.ViewModels
{
    public class ComputerImportViewModel : ViewModelBase
    {

        private bool _isZipFileImport;
        private bool _isImporting;
        private string _userMessage;
        private string _pathInput;
        private string SessionPath { get; set; }

        public string PathToSessionContent
        {
            get
            {
                return $"{SessionPath}\\SessionGame\\Content";
            }
        }

        public string PathToTempUnzipFolder
        {
            get
            {
                return $"{SessionPath}\\SessionGame\\Temp_Unzipped";
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
                NotifyPropertyChanged(nameof(ButtonVisibility));
            }
        }

        public bool IsNotImporting
        {
            get
            {
                return !IsImporting;
            }
        }

        public string PathLabel
        {
            get
            {
                if (IsZipFileImport)
                {
                    return "Path To Zip File:";
                }
                else
                {
                    return "Folder Path To Map Files:";
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

                return PathInput;
            }
        }

        public Visibility ButtonVisibility
        {
            get
            {
                if (IsImporting)
                {
                    return Visibility.Hidden;
                }
                return Visibility.Visible;
            }
        }

        public ComputerImportViewModel(string sessionPath)
        {
            this.UserMessage = "";
            this.IsZipFileImport = false;
            this.PathInput = "";
            this.SessionPath = sessionPath;
        }

        internal void BrowseForFolderOrFile()
        {
            if (IsZipFileImport)
            {
                using (OpenFileDialog fileBrowserDialog = new OpenFileDialog())
                {
                    fileBrowserDialog.Filter = "Zip files (*.zip)|*.zip|All files (*.*)|*.*";
                    fileBrowserDialog.Title = "Select .zip File Containing Session Map";
                    DialogResult result = fileBrowserDialog.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        PathInput = fileBrowserDialog.FileName;
                    }
                }
            }
            else
            {
                using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
                {
                    folderBrowserDialog.ShowNewFolderButton = false;
                    folderBrowserDialog.Description = "Select Folder Containing Session Map Files";
                    DialogResult result = folderBrowserDialog.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        PathInput = folderBrowserDialog.SelectedPath;
                    }
                }
            }
        }

        internal void ImportMapAsync()
        {
            UserMessage = "Importing Map ...";

            if (IsZipFileImport)
            {
                UserMessage += " Unzipping and copying can take a couple of minutes depending on the .zip file size.";
            }
            else
            {
                UserMessage += " Copying can take a couple of minutes depending on the amount of files to copy.";
            }

            IsImporting = true;

            bool didExtract = false;

            Task task = Task.Factory.StartNew(() =>
            {
                string sourceFolderToCopy;

                if (IsZipFileImport)
                {
                    if (File.Exists(PathToFileOrFolder) == false)
                    {
                        System.Windows.MessageBox.Show("File does not exist", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // extract files first before copying
                    Directory.CreateDirectory(PathToTempUnzipFolder);
                    didExtract = DownloadUtils.ExtractZipFile(PathToFileOrFolder, PathToTempUnzipFolder);

                    if (didExtract == false)
                    {
                        return;
                    }

                    sourceFolderToCopy = PathToTempUnzipFolder;
                }
                else
                {
                    if (Directory.Exists(PathToFileOrFolder) == false)
                    {
                        System.Windows.MessageBox.Show("Folder does not exist", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    sourceFolderToCopy = PathToFileOrFolder;
                }

                FileUtils.CopyDirectoryRecursively(sourceFolderToCopy, PathToSessionContent, true);

                if (IsZipFileImport && Directory.Exists(PathToTempUnzipFolder))
                {
                    // remove unzipped temp files
                    Directory.Delete(PathToTempUnzipFolder, true);
                }
            });

            task.ContinueWith((antecedent) =>
            {
                IsImporting = false;

                if (IsZipFileImport && didExtract == false)
                {
                    UserMessage = "Failed to extract .zip file.";
                    return;
                }

                UserMessage = "Map Imported!";
            });


        }
    }
}