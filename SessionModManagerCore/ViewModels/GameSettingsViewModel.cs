using SessionMapSwitcherCore.Classes;
using SessionModManagerCore.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SessionModManagerCore.ViewModels
{
    public class GameSettingsViewModel : ViewModelBase
    {
        private string _objectCountText;
        private bool _skipMovieIsChecked;
        private bool _dBufferIsChecked;

        private string _shadowQualityText;
        private string _antiAliasingText;
        private string _texturesQualityText;
        private string _viewDistanceQualityText;
        private string _foliageQualityText;
        private string _effectsQualityText;
        private string _shadingQualityText;
        private string _postProcessingText;
        private string _resolutionText;
        private string _customResolutionText;
        private string _frameRateLimitText;
        private string _fullScreenMode;
        private bool _isVsyncEnabled;

        public string ShadowQualityText
        {
            get { return _shadowQualityText; }
            set
            {
                _shadowQualityText = value;
                NotifyPropertyChanged();
            }
        }

        public string AntiAliasingText
        {
            get { return _antiAliasingText; }
            set
            {
                _antiAliasingText = value;
                NotifyPropertyChanged();
            }
        }

        public string TexturesQualityText
        {
            get { return _texturesQualityText; }
            set
            {
                _texturesQualityText = value;
                NotifyPropertyChanged();
            }
        }

        public string ViewDistanceQualityText
        {
            get { return _viewDistanceQualityText; }
            set
            {
                _viewDistanceQualityText = value;
                NotifyPropertyChanged();
            }
        }

        public string ShadingQualityText
        {
            get { return _shadingQualityText; }
            set
            {
                _shadingQualityText = value;
                NotifyPropertyChanged();
            }
        }

        public string FoliageQualityText
        {
            get { return _foliageQualityText; }
            set
            {
                _foliageQualityText = value;
                NotifyPropertyChanged();
            }
        }

        public string EffectsQualityText
        {
            get { return _effectsQualityText; }
            set
            {
                _effectsQualityText = value;
                NotifyPropertyChanged();
            }
        }

        public string PostProcessingText
        {
            get { return _postProcessingText; }
            set
            {
                _postProcessingText = value;
                NotifyPropertyChanged();
            }
        }

        public string FullScreenMode
        {
            get { return _fullScreenMode; }
            set
            {
                _fullScreenMode = value;
                NotifyPropertyChanged();
            }
        }

        public string ResolutionText
        {
            get { return _resolutionText; }
            set
            {
                _resolutionText = value;
                if (_resolutionText == "Custom" && string.IsNullOrWhiteSpace(CustomResolutionText))
                {
                    CustomResolutionText = "1920x1080";
                }
                NotifyPropertyChanged();
            }
        }

        public string CustomResolutionText
        {
            get { return _customResolutionText; }
            set
            {
                _customResolutionText = value;
                NotifyPropertyChanged();
            }
        }

        public string FrameRateLimitText
        {
            get { return _frameRateLimitText; }
            set
            {
                _frameRateLimitText = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsVsyncEnabled
        {
            get { return _isVsyncEnabled; }
            set
            {
                _isVsyncEnabled = value;
                NotifyPropertyChanged();
            }
        }


        public string ObjectCountText
        {
            get { return _objectCountText; }
            set
            {
                _objectCountText = value;
                NotifyPropertyChanged();
            }
        }

        public bool SkipMovieIsChecked
        {
            get { return _skipMovieIsChecked; }
            set
            {
                _skipMovieIsChecked = value;
                NotifyPropertyChanged();
            }
        }

        public bool DBufferIsChecked
        {
            get { return _dBufferIsChecked; }
            set
            {
                _dBufferIsChecked = value;
                NotifyPropertyChanged();
            }
        }


        private List<string> _videoDropdown;
        public List<string> VideoSettingsDropdownOptions
        {
            get
            {
                if (_videoDropdown == null)
                {
                    _videoDropdown = new List<string>()
                    {
                        VideoSettingsOptions.Low.ToString(),
                        VideoSettingsOptions.Medium.ToString(),
                        VideoSettingsOptions.High.ToString(),
                        VideoSettingsOptions.Epic.ToString(),
                    };
                }
                return _videoDropdown;
            }
        }

        private List<string> _resolutionDropdownOptions;
        public List<string> ResolutionDropdownOptions
        {
            get
            {
                if (_resolutionDropdownOptions == null)
                {
                    _resolutionDropdownOptions = new List<string>()
                    {
                        "320x200",
                        "320x240",
                        "400x300",
                        "512x384",
                        "640x400",
                        "640x480",
                        "800x600",
                        "1024x768",
                        "1152x864",
                        "1280x600",
                        "1280x720",
                        "1280x768",
                        "1280x800",
                        "1280x960",
                        "1280x1024",
                        "1360x768",
                        "1366x768",
                        "1400x1050",
                        "1440x900",
                        "1600x900",
                        "1680x1050",
                        "1920x1080",
                        "Match Desktop Resolution",
                        "Custom"
                    };
                }
                return _resolutionDropdownOptions;
            }
        }

        private List<string> _fullscreenDropdownOptions;
        public List<string> FullscreenDropdownOptions
        {
            get
            {
                if (_fullscreenDropdownOptions == null)
                {
                    _fullscreenDropdownOptions = new List<string>()
                    {
                        "Fullscreen",
                        "Windowed Fullscreen",
                        "Windowed"
                    };
                }
                return _fullscreenDropdownOptions;
            }
        }

        public GameSettingsViewModel()
        {
            RefreshGameSettings();
        }

        public void RefreshGameSettings()
        {
            BoolWithMessage result = GameSettingsManager.RefreshGameSettingsFromIniFiles();

            if (result.Result == false)
            {
                MessageService.Instance.ShowMessage(result.Message);
            }

            ObjectCountText = GameSettingsManager.ObjectCount.ToString();
            SkipMovieIsChecked = GameSettingsManager.SkipIntroMovie;
            DBufferIsChecked = GameSettingsManager.EnableDBuffer;

            ShadowQualityText = ((VideoSettingsOptions) GameSettingsManager.ShadowQuality).ToString();
            AntiAliasingText = ((VideoSettingsOptions)GameSettingsManager.AntiAliasingQuality).ToString();
            TexturesQualityText = ((VideoSettingsOptions)GameSettingsManager.TextureQuality).ToString();
            ViewDistanceQualityText = ((VideoSettingsOptions)GameSettingsManager.ViewDistanceQuality).ToString();
            ShadingQualityText = ((VideoSettingsOptions)GameSettingsManager.ShadowQuality).ToString();
            FoliageQualityText = ((VideoSettingsOptions)GameSettingsManager.FoliageQuality).ToString();
            EffectsQualityText = ((VideoSettingsOptions)GameSettingsManager.EffectsQuality).ToString();
            PostProcessingText = ((VideoSettingsOptions)GameSettingsManager.PostProcessQuality).ToString();
            FullScreenMode = FullscreenDropdownOptions[GameSettingsManager.FullscreenMode];


            if (string.IsNullOrEmpty(GameSettingsManager.ResolutionSizeX) && string.IsNullOrEmpty(GameSettingsManager.ResolutionSizeY))
            {
                ResolutionText = "Match Desktop Resolution";
            }
            else if (ResolutionDropdownOptions.Any(r => r == $"{GameSettingsManager.ResolutionSizeX}x{GameSettingsManager.ResolutionSizeY}"))
            {
                ResolutionText = $"{GameSettingsManager.ResolutionSizeX}x{GameSettingsManager.ResolutionSizeY}";
                CustomResolutionText = $"{GameSettingsManager.ResolutionSizeX}x{GameSettingsManager.ResolutionSizeY}";
            }
            else
            {
                ResolutionText = "Custom";
                CustomResolutionText = $"{GameSettingsManager.ResolutionSizeX}x{GameSettingsManager.ResolutionSizeY}";
            }

            FrameRateLimitText = GameSettingsManager.FrameRateLimit.ToString();
            IsVsyncEnabled = GameSettingsManager.EnableVsync;

        }

        public bool UpdateGameSettings()
        {
            string returnMessage = "";

            BoolWithMessage didSetSettings = GameSettingsManager.UpdateGameSettings(SkipMovieIsChecked, DBufferIsChecked);
            BoolWithMessage didSetObjCount = BoolWithMessage.True(); // set to true by default in case the user does not have the file to modify

            BoolWithMessage didSetVideoSettings = ValidateAndUpdateVideoSettings();


            if (GameSettingsManager.DoesInventorySaveFileExist())
            {
                didSetObjCount = GameSettingsManager.ValidateAndUpdateObjectCount(ObjectCountText);

                if (didSetObjCount.Result == false)
                {
                    returnMessage += didSetObjCount.Message;
                }
            }

            if (didSetSettings.Result == false)
            {
                returnMessage += didSetSettings.Message;
            }

            if (didSetVideoSettings.Result == false)
            {
                returnMessage += didSetVideoSettings.Message;
            }

            if (!didSetVideoSettings.Result || !didSetSettings.Result)
            {
                MessageService.Instance.ShowMessage(returnMessage);
                return false;
            }


            returnMessage = "Game settings updated!";

            if (GameSettingsManager.DoesInventorySaveFileExist() == false)
            {
                returnMessage += " Object count cannot be changed until a .sav file exists.";
            }

            if (SessionPath.IsSessionRunning())
            {
                returnMessage += " Restart the game for changes to take effect.";
            }

            MessageService.Instance.ShowMessage(returnMessage);



            return didSetSettings.Result && didSetObjCount.Result && didSetVideoSettings.Result;
        }

        private BoolWithMessage ValidateAndUpdateVideoSettings()
        {
            if (!int.TryParse(FrameRateLimitText, out int frameRate))
            {
                return BoolWithMessage.False("Frame Rate must be a valid number between 10 and 999");
            }

            if (frameRate < 10 || frameRate > 999)
            {
                return BoolWithMessage.False("Frame Rate must be a valid number between 10 and 999");
            }

            if (ResolutionText == "Match Desktop Resolution")
            {
                GameSettingsManager.ResolutionSizeX = null;
                GameSettingsManager.ResolutionSizeY = null;
            }
            else if (ResolutionText == "Custom")
            {
                string[] res = CustomResolutionText.Split(new char[] { 'x' }, StringSplitOptions.RemoveEmptyEntries);

                if (res.Length != 2)
                {
                    return BoolWithMessage.False("Custom resolution invalid. Must be in format '1920x1080'");
                }

                if (!int.TryParse(res[0], out int customResX) || !int.TryParse(res[1], out int customResY))
                {
                    return BoolWithMessage.False("Custom resolution invalid. Must be in format '1920x1080'");
                }

                if (customResX <= 0 || customResY <= 0)
                {
                    return BoolWithMessage.False("Custom resolution invalid. Must be in format '1920x1080'");
                }

                GameSettingsManager.ResolutionSizeX = res[0];
                GameSettingsManager.ResolutionSizeY = res[1];
            }
            else
            {
                string[] res = ResolutionText.Split(new char[] { 'x' }, StringSplitOptions.RemoveEmptyEntries);
                GameSettingsManager.ResolutionSizeX = res[0];
                GameSettingsManager.ResolutionSizeY = res[1];
            }

            GameSettingsManager.EnableVsync = IsVsyncEnabled;
            GameSettingsManager.FrameRateLimit = frameRate;

            Enum.TryParse(AntiAliasingText, out VideoSettingsOptions setting);
            GameSettingsManager.AntiAliasingQuality = (int)setting;

            Enum.TryParse(ShadowQualityText, out setting);
            GameSettingsManager.ShadowQuality = (int)setting;

            Enum.TryParse(ShadingQualityText, out setting);
            GameSettingsManager.ShadingQuality = (int)setting;

            Enum.TryParse(TexturesQualityText, out setting);
            GameSettingsManager.TextureQuality = (int)setting;

            Enum.TryParse(ViewDistanceQualityText, out setting);
            GameSettingsManager.ViewDistanceQuality = (int)setting;

            Enum.TryParse(FoliageQualityText, out setting);
            GameSettingsManager.FoliageQuality = (int)setting;

            Enum.TryParse(EffectsQualityText, out setting);
            GameSettingsManager.EffectsQuality = (int)setting;

            Enum.TryParse(PostProcessingText, out setting);
            GameSettingsManager.PostProcessQuality = (int)setting;

            GameSettingsManager.FullscreenMode = FullscreenDropdownOptions.IndexOf(FullScreenMode);

            return GameSettingsManager.SaveVideoSettingsToDisk();
        }
    }
}
