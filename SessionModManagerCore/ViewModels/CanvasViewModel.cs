using Newtonsoft.Json;
using SessionMapSwitcherCore.Classes;
using SessionModManagerCore.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SessionModManagerCore.ViewModels
{
    public class CanvasViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        UAssetEditor _assetEditor = new UAssetEditor();
        private int _floorWidth = 5000;
        private int _floorHeight = 5000;
        private int _canvasWidth = 1000;
        private int _canvasHeight = 1000;
        private int _currentFloorLayer = 0;

        private string _floorWidthText = "5000";
        private string _floorHeightText = "5000";   

        public const int LayerHeight = 500;

        public List<ParkObjBase> ParkObjs { get; set; } = new List<ParkObjBase>() { };

        public int ActiveCatalogIndex { get; set; }
        public List<ParkObjBase> ObjectCatalog { get; set; } = new List<ParkObjBase>() {
            new("SM_Bank_01")
            {
                UnrealScale = new ObjVector(300, 500, 150),
                AnchorPoint = new ObjVector(0, 0, 0)
            },
            new("SM_Bank_02")
            {
                UnrealScale = new ObjVector(300, 230, 100),
                AnchorPoint = new ObjVector(0, 0, 0)
            },
            new("SM_Bank_03")
            {
                UnrealScale = new ObjVector(300, 230, 50),
                AnchorPoint = new ObjVector(0, 0, 0)
            },
            new("SM_Bench_01")
            {
                UnrealScale = new ObjVector(215, 40, 50),
                AnchorPoint = new ObjVector(0.5, 0.5, 0)
            },
            new("SM_Table_Benches_01")
            {
                UnrealScale = new ObjVector(225, 200, 85),
                AnchorPoint = new ObjVector(0.5, 0.5, 0)
            },
            new("SM_Concrete_Spine_02")
            {
                UnrealScale = new ObjVector(800, 340, 150),
                AnchorPoint = new ObjVector(0, 0, 0)
            },
            new("SM_Funbox_Floor_01_3m")
            {
                UnrealScale = new ObjVector(300, 300, 100),
                AnchorPoint = new ObjVector(1, 1, 0)
            },
            new("SM_Funbox_Wall_01")
            {
                UnrealScale = new ObjVector(50, 300, 150),
                AnchorPoint = new ObjVector(0, 0, 0)
            },
            new("SM_Funbox_Wall_02")
            {
                UnrealScale = new ObjVector(50, 300, 150),
                AnchorPoint = new ObjVector(0, 0, 0)
            },
            new("SM_Halfpipe_01_Middle")
            {
                UnrealScale = new ObjVector(400, 300, 20),
                AnchorPoint = new ObjVector(0, 0, 0)
            },
            new("SM_Halfpipe_01_Ramp")
            {
                UnrealScale = new ObjVector(400, 300, 170),
                AnchorPoint = new ObjVector(0, 0, 0)
            },
            new("SM_Pyramid_01")
            {
                UnrealScale = new ObjVector(900, 600, 100),
                AnchorPoint = new ObjVector(0.5, 0, 0)
            },
            new("SM_Pyramid_02")
            {
                UnrealScale = new ObjVector(450, 600, 100),
                AnchorPoint = new ObjVector(1, 0, 0)
            },
            new("SM_Rail_01_2m")
            {
                UnrealScale = new ObjVector(200, 20, 55),
                AnchorPoint = new ObjVector(0, 0.5, 0)
            },
            new("SM_Rail_01_3m_Angled")
            {
                UnrealScale = new ObjVector(320, 20, 55),
                AnchorPoint = new ObjVector(0, 0.5, 0)
            },
            new("SM_Rail_01_4m")
            {
                UnrealScale = new ObjVector(400, 20, 55),
                AnchorPoint = new ObjVector(0, 0.5, 0)
            },
            new("SM_Rail_01_End")
            {
                UnrealScale = new ObjVector(60, 20, 55),
                AnchorPoint = new ObjVector(1, 0.5, 0)
            },
            new("SM_Rail_01_Funbox_Wall_02")
            {
                UnrealScale = new ObjVector(410, 20, 205),
                AnchorPoint = new ObjVector(0, 0.5, 0.5)
            },
            new("SM_Rail_01_Pyramid_01")
            {
                UnrealScale = new ObjVector(425, 20, 155),
                AnchorPoint = new ObjVector(0, 0.5, 0.5)
            },
            new("SM_Rail_01_Stairs_01")
            {
                UnrealScale = new ObjVector(250, 20, 155),
                AnchorPoint = new ObjVector(0, 0.5, 0.5)
            },
            new("SM_Rail_01_Corner_45_1m")
            {
                UnrealScale = new ObjVector(100, 60, 55),
                AnchorPoint = new ObjVector(0, 1, 0)
            },
            new("SM_Rail_01_Corner_90_1m")
            {
                UnrealScale = new ObjVector(105, 105, 55),
                AnchorPoint = new ObjVector(0, 1, 0)
            },
            new("SM_Rail_01_Corner_90_2m")
            {
                UnrealScale = new ObjVector(205, 205, 55),
                AnchorPoint = new ObjVector(0, 1, 0)
            },
            new("SM_Rail_02_2m")
            {
                UnrealScale = new ObjVector(200, 20, 55),
                AnchorPoint = new ObjVector(0, 0.5, 0)
            },
            new("SM_Rail_02_4m")
            {
                UnrealScale = new ObjVector(400, 20, 55),
                AnchorPoint = new ObjVector(0, 0.5, 0)
            },
            new("SM_Rail_02_Corner_45_1m")
            {
                UnrealScale = new ObjVector(100, 60, 55),
                AnchorPoint = new ObjVector(0, 1, 0)
            },
            new("SM_Rail_02_Corner_90_1m")
            {
                UnrealScale = new ObjVector(105, 105, 55),
                AnchorPoint = new ObjVector(0, 1, 0)
            },
            new("SM_Rail_02_Corner_90_2m")
            {
                UnrealScale = new ObjVector(205, 205, 55),
                AnchorPoint = new ObjVector(0, 1, 0)
            },
            new("SM_Roll_Ramp_01")
            {
                UnrealScale = new ObjVector(300, 300, 100),
                AnchorPoint = new ObjVector(0, 0, 0)
            },
            new("SM_Spine_01")
            {
                UnrealScale = new ObjVector(300, 300, 100),
                AnchorPoint = new ObjVector(0, 0, 0)
            },
            new("SM_Stairs_Small_01")
            {
                UnrealScale = new ObjVector(100, 150, 100),
                AnchorPoint = new ObjVector(0, 0, 0)
            },
            new("SM_Stairs_Small_02")
            {
                UnrealScale = new ObjVector(300, 150, 100),
                AnchorPoint = new ObjVector(0, 0, 0)
            },
            new("SM_Wave_Ramp_01")
            {
                UnrealScale = new ObjVector(200, 400, 40),
                AnchorPoint = new ObjVector(0, 0, 0)
            },
        };

        public int FloorWidth
        {
            get { return _floorWidth; }
            set
            {
                _floorWidth = value;
                NotifyPropertyChanged();
            }
        }

        public int FloorHeight
        {
            get { return _floorHeight; }
            set
            {
                _floorHeight = value;
                NotifyPropertyChanged();
            }
        }

        public string FloorWidthText
        {
            get { return _floorWidthText; }
            set
            {
                _floorWidthText = value;
                NotifyPropertyChanged();
            }
        }

        public string FloorHeightText
        {
            get { return _floorHeightText; }
            set
            {
                _floorHeightText = value;
                NotifyPropertyChanged();
            }
        }

        public int CanvasWidth
        {
            get { return _canvasWidth; }
            set
            {
                _canvasWidth = value;
                NotifyPropertyChanged();
            }
        }

        public int CanvasHeight
        {
            get { return _canvasHeight; }
            set
            {
                _canvasHeight = value;
                NotifyPropertyChanged();
            }
        }

        public int CurrentFloorLayer
        {
            get { return _currentFloorLayer; }
            set
            {
                _currentFloorLayer = value;
                NotifyPropertyChanged();
            }
        }

        public void Build()
        {
            try
            {
                _assetEditor.TestBuild(ParkObjs, FloorWidth, FloorHeight);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                MessageService.Instance.ShowMessage("Failed to build custom skatepark.");
            }
        }

        public BoolWithMessage SavePark(string filePath, List<ParkItemData> parkItems)
        {
            var json = JsonConvert.SerializeObject(new ParkSaveData()
            {
                CanvasHeight = CanvasHeight,
                CanvasWidth = CanvasWidth,
                FloorHeight = FloorHeight,
                FloorWidth = FloorWidth,
                ParkObjs = parkItems,
            }, Formatting.Indented);
            File.WriteAllText(filePath, json);
            return BoolWithMessage.True();
        }

        public List<ParkItemData> LoadPark(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return [];
            }

            var json = File.ReadAllText(filePath);
            var parkSave = JsonConvert.DeserializeObject<ParkSaveData>(json);
            ParkObjs = parkSave.ParkObjs.Select(p => (ParkObjBase)p).ToList();

            return parkSave.ParkObjs;
        }

        /// <summary>
        /// validates the floor size text inputs, returns a BoolWithMessage indicating if the input is valid.
        /// </summary>
        public static BoolWithMessage ValidateFloorSize(string sizeText)
        {
            if (int.TryParse(sizeText, out int parsedAmount) == false)
            {
                return BoolWithMessage.False("Invalid floor size.");
            }

            if (parsedAmount <= 2500 || parsedAmount > 30000)
            {
                return BoolWithMessage.False("Floor size must be between 2500 and 30000.");
            }

            return BoolWithMessage.True();
        }
    }
}
