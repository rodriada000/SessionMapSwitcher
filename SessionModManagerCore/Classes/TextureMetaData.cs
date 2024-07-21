using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SessionModManagerCore.Classes
{
    public class TextureMetaData
    {
        /// <summary>
        /// List of absolute paths to files that were copied for the texture
        /// </summary>
        public List<string> FilePaths { get; set; }

        /// <summary>
        /// Name of the asset file that this texture file came from
        /// </summary>
        public string AssetName { get; set; }

        /// <summary>
        /// Returns <see cref="AssetName"/> without the .zip or .rar extension
        /// </summary>
        [JsonIgnore]
        public string AssetNameWithoutExtension
        {
            get
            {
                if (string.IsNullOrWhiteSpace(AssetName))
                {
                    return AssetName;
                }

                return AssetName.Replace(".zip", "").Replace(".rar", "");
            }
        }

        /// <summary>
        /// Display name of the asset from the asset store
        /// </summary>
        public string Name { get; set; }

        public string Category { get; set; }

        public string PathToImage { get; set; }

        public TextureMetaData()
        {
            FilePaths = new List<string>();
            AssetName = "";
            Name = "";
            PathToImage = "";
        }

        public TextureMetaData(Asset assetToInstall)
        {
            FilePaths = new List<string>();

            if (assetToInstall == null)
            {
                return;
            }

            AssetName = assetToInstall.ID;
            Name = assetToInstall.Name;
            Category = assetToInstall.Category;
        }
    }
}
