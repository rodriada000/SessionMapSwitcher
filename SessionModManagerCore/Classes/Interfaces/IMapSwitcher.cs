using System.Collections.Generic;

namespace SessionMapSwitcherCore.Classes.Interfaces
{
    /// <summary>
    /// Interface to represent a map switcher for EzPz and Unpacked versions of the game
    /// </summary>
    interface IMapSwitcher
    {
        List<MapListItem> GetDefaultSessionMaps();

        MapListItem GetFirstLoadedMap();

        BoolWithMessage LoadMap(MapListItem map);

        BoolWithMessage LoadDefaultMap(MapListItem map);

        bool CopyMapFilesToNYCFolder(MapListItem map);

        void DeleteMapFilesFromNYCFolder();

        string GetGameDefaultMapSetting();

        bool SetGameDefaultMapSetting(string defaultMapValue, string defaultGameModeValue);
    }
}
