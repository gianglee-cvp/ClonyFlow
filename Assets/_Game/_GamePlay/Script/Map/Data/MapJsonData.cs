using System;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    [Serializable]
    public sealed class MapJsonData
    {
        public int rows;
        public int columns;
        public MapVectorData cameraPosition;
        public MapVectorData cameraRotation;
        public MapVectorData firstCellPosition;
        public MapSpacingData cellSpacing;
        public MapVectorData cellScale;
        public MapPaletteEntry[] palette;
        public int[] cells;
    }

    [Serializable]
    public sealed class MapVectorData
    {
        public float x;
        public float y;
        public float z;
        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    [Serializable]
    public sealed class MapSpacingData
    {
        public float x;
        public float z;
    }

    [Serializable]
    public sealed class MapPaletteEntry
    {
        public int id;
        public string hex;
    }
}
