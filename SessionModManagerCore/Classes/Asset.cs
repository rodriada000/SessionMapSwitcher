using System;
using System.IO;
using Newtonsoft.Json;
using SessionMapSwitcherCore.ViewModels;

namespace SessionModManagerCore.Classes
{
    /// <summary>
    /// Class containing an Asset object
    /// </summary>
    public class Asset
    {
        /// <summary>
        /// A unique string to identify assets
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Returns <see cref="ID"/> without the .zip or .rar extension
        /// </summary>
        [JsonIgnore]
        public string IDWithoutExtension
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ID))
                {
                    return ID;
                }

                return ID.Replace(".zip", "").Replace(".rar", "");
            }
        }

        public string PathToDownloadedImage
        {
            get
            {
                return Path.Combine(AssetStoreViewModel.AbsolutePathToThumbnails, IDWithoutExtension);
            }
        }
        

        /// <summary>
        /// The display name of the asset
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// A short description of the asset
        /// </summary>
        public string Description { get; set; }

        public double Version { get; set; }
        /// <summary>
        /// The author of the asset
        /// </summary>
        public string Author { get; set; }
        /// <summary>
        /// The url to download in format smm://GDrive//googledriveID
        /// </summary>
        public string DownloadLink { get; set; }
        /// <summary>
        /// The url to preview image
        /// </summary>
        public string PreviewImage { get; set; }
        /// <summary>
        /// The category of the asset as a string
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// The date (UTC) the asset was last uploaded/edited.
        /// </summary>
        public DateTime UpdatedDate { get; set; }

        [JsonIgnore]
        internal AssetCategory assetCategory
        {
            get
            {
                if (string.IsNullOrEmpty(Category))
                    return null;

                return AssetCategory.FromString(Category);
            }
        }

        [JsonIgnore]
        internal string ConvertError{ get;  }

        /// <summary>
        /// Default constructor
        /// </summary>
        [JsonConstructor]
        public Asset(string id, string Name, string Description, string Author, string AssetName, string Thumbnail, string Category, string UpdatedDate, double version)
        {
            this.ID = id;
            this.Name = Name;
            this.Description = Description;
            this.Version = version;
            this.Author = Author;
            this.DownloadLink = AssetName;
            this.PreviewImage = Thumbnail;
            this.Category = Category;
            this.UpdatedDate = UpdatedDate == null ? DateTime.MinValue : DateTime.Parse(UpdatedDate);
        }

        public Asset(string ConvertError)
        {
            this.ConvertError = ConvertError;
        }

        public Asset()
        {
        }

        /// <summary>
        /// Dumps a json string of the asset
        /// </summary>
        /// <returns>JSON string</returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public int SearchRelevance(string text)
        {
            if (Name.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) >= 0) return 200;
            if (Description.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) >= 0) return 100;
            if (Author.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) >= 0) return 50;
            if (Category?.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) >= 0) return 25;
            return 0;
        }

        /// <summary>
        /// Compares properties to another asset to see if 
        /// name, description, category, author, or link is different.
        /// </summary>
        /// <param name="other"></param>
        /// <returns> true if properties are different; false if they are equal. </returns>
        internal bool HasChanges(Asset other)
        {
            return (this.Name != other.Name ||
                    this.Description != other.Description ||
                    this.Category != other.Category ||
                    this.Author != other.Author ||
                    this.DownloadLink != other.DownloadLink);
        }

    }

    /// <summary>
    /// Class to replace an enum to get string values.
    /// </summary>
    public class AssetCategory
    {
        public string Value { get; set; }
        public AssetCategory(string value) { Value = value; }

        public static AssetCategory Maps { get { return new AssetCategory("session-maps"); } }
        public static AssetCategory Griptapes { get { return new AssetCategory("session-griptapes"); } }
        public static AssetCategory Hats { get { return new AssetCategory("session-hats"); } }
        public static AssetCategory Shirts { get { return new AssetCategory("session-shirts"); } }
        public static AssetCategory Pants { get { return new AssetCategory("session-pants"); } }
        public static AssetCategory Shoes { get { return new AssetCategory("session-shoes"); } }
        public static AssetCategory Decks { get { return new AssetCategory("session-decks"); } }
        public static AssetCategory Trucks { get { return new AssetCategory("session-trucks"); } }
        public static AssetCategory Wheels { get { return new AssetCategory("session-wheels"); } }
        public static AssetCategory Meshes { get { return new AssetCategory("session-meshes"); } }
        public static AssetCategory Characters { get { return new AssetCategory("session-characters"); } }

        /// <summary>
        /// Converts a string to an AssetCategory
        /// </summary>
        /// <param name="category">Name of the category</param>
        /// <returns>AssetCategory object</returns>
        public static AssetCategory FromString(string category)
        {
            switch(category)
            {
                case ("session-maps"):
                    return Maps;
                case ("session-griptapes"):
                    return Griptapes;
                case ("session-hats"):
                    return Hats;
                case ("session-shirts"):
                    return Shirts;
                case ("session-pants"):
                    return Pants;
                case ("session-shoes"):
                    return Shoes;
                case ("session-decks"):
                    return Decks;
                case ("session-trucks"):
                    return Trucks;
                case ("session-wheels"):
                    return Wheels;
                case ("session-meshes"):
                    return Meshes;
                case ("session-characters"):
                    return Characters;
                default:
                    throw new Exception($"Invalid category provided: {category}");
            }
        }
    }
}
