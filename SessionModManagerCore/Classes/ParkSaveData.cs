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

        public int CanvasWidth { get; set; }

        public int CanvasHeight { get; set; }
        public ObjVector StartPosition { get; set; }

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
        }

        [JsonIgnore]
        public new string ImagePath { get => Path.Combine(SessionPath.ToApplicationResourcesFolder, "ParkPieces", $"{Name}.png"); }

        public double Top { get; set; }
        public double Left { get; set; }
        public double CenterX { get; set; }
        public double CenterY { get; set; }

    }
}
