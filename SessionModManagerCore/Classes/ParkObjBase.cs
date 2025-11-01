using Newtonsoft.Json;
using SessionMapSwitcherCore.Classes;
using System.IO;

namespace SessionModManagerCore.Classes
{
    /// <summary>
    /// Class that defines properties of a placeable object
    /// </summary>
    public class ParkObjBase
    {

        public ParkObjBase()
        {
            Position = new ObjVector();
            Scale = new ObjVector(1, 1, 1);
            Rotation = new ObjVector(0, 0, 0);
            AnchorPoint = new ObjVector(0, 0, 0);
        }
        public ParkObjBase(string name)
        {
            Position = new ObjVector();
            Scale = new ObjVector(1, 1, 1);
            Rotation = new ObjVector(0, 0, 0);
            Name = name;
            AnchorPoint = new ObjVector(0, 0, 0);
        }

        public ParkObjBase Clone()
        {
            return new ParkObjBase()
            {
                Rotation = new ObjVector(Rotation),
                Name = Name,
                Position = new ObjVector(Position),
                Scale = new ObjVector(Scale),
                UnrealScale = new ObjVector(UnrealScale),
                AnchorPoint = new ObjVector(AnchorPoint)
            };
        }


        public ObjVector Position { get; set; }
        public ObjVector Scale { get; set; }
        public ObjVector Rotation { get; set; }
        public int Layer { get; set; }

        public string Name { get; set; }
        [JsonIgnore]
        public string ImagePath { get => Path.Combine(SessionPath.ToApplicationResourcesFolder, "ParkPieces", $"{Name}.png"); }

        public ObjVector UnrealScale { get; set; }
        public ObjVector AnchorPoint { get; set; }

        public override string ToString()
        {
            return $"{Name}, P: {Position}, S: {Scale}, R: {Rotation}, L: {Layer}";
        }
    }
}
