namespace SessionModManagerCore.Classes
{
    /// <summary>
    /// Class representing a 3D Vector...
    /// </summary>
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
