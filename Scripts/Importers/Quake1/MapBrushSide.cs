#if UNITY_EDITOR || RUNTIME_CSG

namespace Sabresaurus.SabreCSG.Importers.Quake1
{
    /// <summary>
    /// Represents a Quake 1 Brush Side.
    /// </summary>
    public class MapBrushSide
    {
        public MapPlane Plane;
        public string Material;
        public MapVector2 Offset;
        public float Rotation;
        public MapVector2 Scale;

        // gold source valve map format support:

        public MapVector3 UAxis = new MapVector3(0, 0, 0);
        public MapVector3 VAxis = new MapVector3(0, 0, 0);

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return "Quake 1 Brush Side " + " '" + Material + "' " + " " + Plane;
        }
    }
}

#endif