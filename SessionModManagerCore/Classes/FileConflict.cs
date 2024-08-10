namespace SessionModManagerCore.Classes
{
    public class FileConflict
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }

        /// <summary>
        /// name of asset that conflicts with <see cref="ExistingAssetName"/>
        /// </summary>
        public string AssetName { get; set; }

        /// <summary>
        /// name of asset that is already enabled
        /// </summary>
        public string ExistingAssetName { get; set; }
    }
}
