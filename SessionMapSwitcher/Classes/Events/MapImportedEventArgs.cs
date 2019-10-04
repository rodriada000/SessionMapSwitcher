namespace SessionMapSwitcher.Classes.Events
{
    public class MapImportedEventArgs
    {
        public string MapName { get; set; }

        public MapImportedEventArgs(string mapName)
        {
            MapName = mapName;
        }

    }
}
