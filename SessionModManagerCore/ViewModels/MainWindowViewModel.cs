using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.Classes.Interfaces;
using SessionMapSwitcherCore.Utils;
using SessionModManagerCore.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace SessionModManagerCore.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #region Data Members And Properties

        private string _userMessage;

        private bool _flashStatusBar;

        public string UserMessage
        {
            get { return _userMessage; }
            set
            {
                Logger.Info($"UserMessage = {value}");
                _userMessage = value;
                NotifyPropertyChanged();
            }
        }


        public bool FlashStatusBar
        {
            get { return _flashStatusBar; }
            set
            {
                _flashStatusBar = value;
                if (_flashStatusBar)
                {
                    Task.Factory.StartNew(async () =>
                    {
                        await Task.Delay(3000);
                        FlashStatusBar = false;
                    });
                }
                NotifyPropertyChanged();
            }
        }


        #endregion

        public MainWindowViewModel()
        {
            UserMessage = "";
        }


    }

}
