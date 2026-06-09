using Newtonsoft.Json;
using SessionMapSwitcherCore.Classes;
using System.Collections.Generic;
using System.IO;

namespace SessionModManagerCore.Classes
{
    public class ParkSaveData
    {
        public ParkSaveData()
        {
        }

        public string Name { get; set; }

        public List<ParkItemData> ParkObjs { get; set; } = new List<ParkItemData>() { };

        public int FloorWidth { get; set; }

        public int FloorHeight { get; set; }

        [JsonIgnore]
        public int CanvasWidth { get => (int)(FloorWidth * 0.2); }

        [JsonIgnore]
        public int CanvasHeight { get => (int)(FloorHeight * 0.2); }

    }

    public class ParkItemData : ParkObjBase
    {
        public ParkItemData()
        {
        }

        public ParkItemData(ParkObjBase dataContext)
        {
            Name = dataContext.Name;
            AnchorPoint = dataContext.AnchorPoint;
            Position = dataContext.Position;
            Rotation = dataContext.Rotation;
            Scale = dataContext.Scale;
            UnrealScale = dataContext.UnrealScale;
            Layer = dataContext.Layer;
            IsPlayerStart = dataContext.IsPlayerStart;
        }

        [JsonIgnore]
        public new string ImagePath { get => Path.Combine(SessionPath.ToApplicationResourcesFolder, "ParkPieces", $"{Name}.png"); }

        public double Top { get; set; }
        public double Left { get; set; }
        public double CenterX { get; set; }
        public double CenterY { get; set; }

    }
}
