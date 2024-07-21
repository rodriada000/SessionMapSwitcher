using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SessionModManagerCore.Classes
{
    public class InstalledTexturesMetaData
    {
        public List<TextureMetaData> InstalledTextures { get; set; }

        public InstalledTexturesMetaData()
        {
            InstalledTextures = new List<TextureMetaData>();
        }

        public void Add(TextureMetaData texture)
        {
            if (texture == null)
            {
                return;
            }

            InstalledTextures.Add(texture);
        }
        public void Remove(TextureMetaData texture)
        {
            if (texture == null)
            {
                return;
            }

            TextureMetaData toRemove = InstalledTextures.Where(t => t.AssetName == texture.AssetName).FirstOrDefault();

            InstalledTextures.Remove(toRemove);
        }

        public void Replace(TextureMetaData texture)
        {
            TextureMetaData existingTexture = InstalledTextures.Where(t => t.AssetName == texture.AssetName).FirstOrDefault();

            if (existingTexture == null)
            {
                Add(texture);
                return;
            }

            Remove(existingTexture);
            Add(texture);
        }


    }
}
