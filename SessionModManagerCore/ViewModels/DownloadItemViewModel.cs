using SessionMapSwitcherCore.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SessionModManagerCore.ViewModels
{
    public enum DownloadMode
    {
        Download,
        Install,
    }

    public enum DownloadType
    {
        Asset,
        Catalog,
        Image
    }

    public class DownloadItemViewModel : ViewModelBase
    {
        private string _itemName;
        private double _percentComplete;
        private bool _isSelected;
        private string _downloadSpeed;

        public DownloadType DownloadType { get; set; }

        public Guid UniqueId { get; set; }

        public DateTime LastCalc { get; set; }
        public long LastBytes { get; set; }

        public Action PerformCancel { get; set; }
        public Action OnCancel { get; set; }

        public Action<Exception> OnError { get; set; }
        public Action OnComplete { get; set; }

        public string DownloadUrl { get; set; }

        public string SaveFilePath { get; set; }

        public string ItemName
        {
            get
            {
                return _itemName;
            }
            set
            {
                _itemName = value;
                NotifyPropertyChanged();
            }
        }

        public double PercentComplete
        {
            get
            {
                return _percentComplete;
            }
            set
            {
                _percentComplete = value;
                NotifyPropertyChanged();
            }
        }

        public string DownloadSpeed
        {
            get
            {
                return _downloadSpeed;
            }
            set
            {
                _downloadSpeed = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsStarted { get; set; }
        public bool IsCanceled { get; set; }

        public DownloadItemViewModel()
        {
            LastCalc = DateTime.Now;
            UniqueId = Guid.NewGuid();
            PercentComplete = 0;
            IsSelected = false;
            IsStarted = false;
            IsCanceled = false;
        }

    }
}
