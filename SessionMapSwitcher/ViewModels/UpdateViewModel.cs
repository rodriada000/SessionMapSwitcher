using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SessionMapSwitcher.ViewModels
{
    class UpdateViewModel : ViewModelBase
    {

        private string _headerMessage;
        private Visibility _browserVisibility;

        public string HeaderMessage
        {
            get => _headerMessage;
            set
            {
                _headerMessage = value;
                NotifyPropertyChanged();
            }
        }

        public Visibility BrowserVisibility
        {
            get => _browserVisibility;
            set
            {
                _browserVisibility = value;
                NotifyPropertyChanged();
            }
        }

        public UpdateViewModel()
        {
            HeaderMessage = "A new version of Session Map Switcher is available to download. You can view what's changed below.";
            BrowserVisibility = Visibility.Hidden;
        }
    }
}
