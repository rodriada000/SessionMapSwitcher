using SessionMapSwitcherCore.Classes;
using SessionModManagerCore.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SessionModManagerCore.ViewModels
{
    public class CanvasViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        UAssetEditor _assetEditor = new UAssetEditor();

        public List<ParkObjBase> ParkObjs { get; set; } = new List<ParkObjBase>() { };

        public int ActiveCatalogIndex { get; set; }
        public List<ParkObjBase> ObjectCatalog { get; set; } = new List<ParkObjBase>() {
            new("SM_Pyramid_01")
            {
                AnchorPointX = 0.5,
                AnchorPointY = 0,
                UnrealScale = new ObjVector(900, 600, 100)
            },
            new("SM_Pyramid_02")
            {
                AnchorPointX = 1,
                AnchorPointY = 0,
                UnrealScale = new ObjVector(450, 600, 100)
            },
            new("SM_Wave_Ramp_01")
            {
                AnchorPointX = 0,
                AnchorPointY = 0,
                UnrealScale = new ObjVector(200, 400, 40)
            },
            new("SM_Roll_Ramp_01")
            {
                AnchorPointX = 0,
                AnchorPointY = 0,
                UnrealScale = new ObjVector(300, 300, 100)
            },
            new("SM_Spine_01")
            {
                AnchorPointX = 0,
                AnchorPointY = 0,
                UnrealScale = new ObjVector(300, 300, 100)
            },
            new("SM_Bank_03")
            {
                AnchorPointX = 0,
                AnchorPointY = 0,
                UnrealScale = new ObjVector(300, 233, 50)
            },
            new("SM_Bank_02")
            {
                AnchorPointX = 0,
                AnchorPointY = 0,
                UnrealScale = new ObjVector(300, 233, 100)
            },
            new("SM_Rail_02_4m")
            {
                AnchorPointX = 0,
                AnchorPointY = 0.5,
                UnrealScale = new ObjVector(400, 25, 53)
            },
            new("SM_Funbox_Floor_01_3m")
            {
                AnchorPointX = 1,
                AnchorPointY = 1,
                UnrealScale = new ObjVector(300, 300, 100)
            },
        };

        public void Build()
        {
            try
            {
                _assetEditor.TestBuild(ParkObjs);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                MessageService.Instance.ShowMessage("Failed to build custom skatepark.");
            }
        }
    }

    public class ParkObjBase
    {
        public ParkObjBase()
        {
            Position = new ObjVector();
            Scale = new ObjVector(1, 1, 1);
            Rotation = new ObjVector(0, 0, 0);
        }
        public ParkObjBase(string name)
        {
            Position = new ObjVector();
            Scale = new ObjVector(1, 1, 1);
            Rotation = new ObjVector(0, 0, 0);
            Name = name;
        }

        public ParkObjBase Clone()
        {
            return new ParkObjBase()
            {
                AnchorPointX = AnchorPointX,
                AnchorPointY = AnchorPointY,
                Rotation = new ObjVector(Rotation),
                Name = Name,
                Position = new ObjVector(Position),
                Scale = new ObjVector(Scale),
                UnrealScale = new ObjVector(UnrealScale),
            };
        }


        public ObjVector Position { get; set; }
        public ObjVector Scale { get; set; }
        public ObjVector Rotation { get; set; }


        public string Name { get; set; }
        public string ImagePath { get => Path.Combine(SessionPath.ToApplicationResourcesFolder, "ParkPieces" , $"{Name}.png"); }

        public double AnchorPointX { get; set; }
        public double AnchorPointY { get; set; }
        public ObjVector UnrealScale { get; set; }


        public override string ToString()
        {
            return $"{Name}, P: {Position}, S: {Scale}, R: {Rotation}";
        }
    }

    public class ObjVector
    {
        public ObjVector()
        {
        }

        public ObjVector(ObjVector copy)
        {
            if (copy == null)
            {
                return;
            }

            X = copy.X; Y = copy.Y; Z = copy.Z;
        }

        public ObjVector(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public override string ToString()
        {
            return $"{{{X},{Y},{Z}}}";
        }
    }
}
