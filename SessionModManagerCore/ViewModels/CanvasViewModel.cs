using SessionModManagerCore.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SessionModManagerCore.ViewModels
{
    public class CanvasViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        UAssetEditor _assetEditor = new UAssetEditor();

        public List<ParkObjBase> ParkObjs { get; set; } = new List<ParkObjBase>() { new ParkObjBase() { Position = new ObjVector(100,-600,0), Rotation = new ObjVector(0,0,90), Name = "SM_Rail_01_3m_Angled" }  };

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
            Scale = new ObjVector(1,1,1);
            Rotation = new ObjVector(0,0,0);
        }
        public ParkObjBase(string name)
        {
            Position = new ObjVector();
            Scale = new ObjVector(1, 1, 1);
            Rotation = new ObjVector(0, 0, 0);
            Name = name;
        }


        public ObjVector Position { get; set; }
        public ObjVector Scale { get; set; }
        public ObjVector Rotation { get; set; }

        public string Name { get; set; }
    }

    public class ObjVector
    {
        public ObjVector()
        {
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
    }
}
