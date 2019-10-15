using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SessionMapSwitcher.Classes.Interfaces
{
    /// <summary>
    /// Interface to represent a map switcher for EzPz and Unpacked versions of the game
    /// </summary>
    interface IMapSwitcher
    {
        MapListItem GetDefaultSessionMap();

        MapListItem GetFirstLoadedMap();

        BoolWithMessage LoadMap(MapListItem map);

        BoolWithMessage LoadOriginalMap();

        bool CopyMapFilesToNYCFolder(MapListItem map);

        void DeleteMapFilesFromNYCFolder();

        string GetGameDefaultMapSetting();

        bool SetGameDefaultMapSetting(string defaultMapValue);
    }
}
