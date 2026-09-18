using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public sealed class Cell
    {
        public int Row { get; }
        public int Column { get; }
        public int ColorId { get; internal set; }
        public Vector3 Position { get; internal set; }
        public bool IsEmpty => ColorId == 0;

        internal Cell(int row, int column, int colorId, Vector3 position)
        {
            Row = row;
            Column = column;
            ColorId = colorId;
            Position = position;
        }
    }
}
