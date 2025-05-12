using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UAssetAPI.UnrealTypes;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using SessionModManagerCore.ViewModels;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;

namespace SessionModManagerCore.Classes
{
    public class UAssetEditor
    {
        private const string Path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Session\\SessionGame\\Content\\CustomMaps\\Ablazerod\\ModularPark\\Map\\ModularPark.umap";
        private const double _scale = 10;
        private const double _zScale = 1;


        public UAssetEditor() { }

        public void TestBuild(List<ParkObjBase> parkObjs)
        {
            UAsset myAsset = new UAsset(Path, EngineVersion.VER_UE4_27);
            NormalExport myExport = (NormalExport)myAsset.Exports.Where(e => e.ObjectName.Value.ToString() == "MapLayout").FirstOrDefault();

            if (myExport == null)
            {
                throw new Exception("Cant find MapLayout within .umap file");
            }

            var array = ((PropertyData[])myExport["ObjectTypes"].RawValue).ToList();
            var positions = ((PropertyData[])myExport["ObjectPos"].RawValue).ToList();
            var scales = ((PropertyData[])myExport["ObjectScale"].RawValue).ToList();
            var rotators = ((PropertyData[])myExport["ObjectRotation"].RawValue).ToList();

            array.Clear();
            positions.Clear();
            scales.Clear();
            rotators.Clear();

            foreach (var obj in parkObjs)
            {
                
                array.Add(new StrPropertyData() { Value = new FString(obj.Name) });
                
                //position
                StructPropertyData pos = new StructPropertyData() { Name = new FName(myAsset, "ObjectPos"), StructType = new FName(myAsset, "Vector") };
                pos.Value = [new VectorPropertyData() { Name = new FName(myAsset, "ObjectPos") }];
                pos.Value[0].RawValue = new FVector { X = obj.Position.X * _scale,  Y = obj.Position.Y * _scale, Z = obj.Position.Z * _zScale };

                positions.Add(pos);

                // scale
                StructPropertyData scale = new StructPropertyData() { Name = new FName(myAsset, "ObjectScale"), StructType = new FName(myAsset, "Vector") };
                scale.Value = [new VectorPropertyData() { Name = new FName(myAsset, "ObjectScale") }];
                scale.Value[0].RawValue = new FVector { X = obj.Scale.X, Y = obj.Scale.Y, Z = obj.Scale.Z };

                scales.Add(scale);

                //rotator
                StructPropertyData rot = new StructPropertyData() { Name = new FName(myAsset, "ObjectRotation"), StructType = new FName(myAsset, "Rotator") };
                rot.Value = [new RotatorPropertyData() { Name = new FName(myAsset, "ObjectRotation") }];
                rot.Value[0].RawValue = new FRotator { Roll = obj.Rotation.X, Pitch = obj.Rotation.Y, Yaw = obj.Rotation.Z };

                rotators.Add(rot);
            }

            myExport["ObjectTypes"].RawValue = array.ToArray();
            myExport["ObjectPos"].RawValue = positions.ToArray();
            myExport["ObjectScale"].RawValue = scales.ToArray();
            myExport["ObjectRotation"].RawValue = rotators.ToArray();
            myAsset.Write(Path);
        }
    }
}
