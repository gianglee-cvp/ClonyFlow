using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public sealed class MapModel
    {
        private readonly Cell[,] cells;
        private readonly Dictionary<int, Color> palette;

        public int Rows { get; }
        public int Columns { get; }
        public int CellCount => Rows * Columns;
        public int ColoredCellCount { get; private set; }
        public Vector3 CameraPosition { get; }
        public Vector3 CameraRotation { get; }
        public Vector3 CellScale { get; private set; }
        public Vector2 CellSpacing { get; private set; }

        public void LayoutToCard(Bounds cardBounds, float padding, float heightMultiplier = 1, float topReserve = 0)
        {
            float availableWidth = cardBounds.size.x - padding * 2;
            if (availableWidth <= 0) throw new InvalidOperationException("Card width must exceed map padding.");
            float cellWidth = Mathf.Min(availableWidth / Columns,
                (cardBounds.size.z - padding * 2 - topReserve) / Rows);
            if (cellWidth <= 0) throw new InvalidOperationException("Card height is too small for the map.");
            float thicknessRatio = CellScale.y / CellScale.x * heightMultiplier;
            CellSpacing = Vector2.one * cellWidth;
            CellScale = new Vector3(cellWidth, cellWidth * thicknessRatio, cellWidth);
            float firstX = cardBounds.center.x - (Columns - 1) * cellWidth * .5f;
            float firstZ = cardBounds.center.z - topReserve * .5f + (Rows - 1) * cellWidth * .5f;
            foreach (var cell in EnumerateCells())
                cell.Position = new Vector3(firstX + cell.Column * cellWidth, 0,
                    firstZ - cell.Row * cellWidth);
        }

        internal MapModel(MapJsonData data, Dictionary<int, Color> colors)
        {
            Rows = data.rows;
            Columns = data.columns;
            CameraPosition = data.cameraPosition.ToVector3();
            CameraRotation = data.cameraRotation.ToVector3();
            CellSpacing = new Vector2(data.cellSpacing.x, data.cellSpacing.z);
            bool hasCustomScale = data.cellScale != null && data.cellScale.x > 0 && data.cellScale.y > 0 && data.cellScale.z > 0;
            CellScale = hasCustomScale ? data.cellScale.ToVector3() : new Vector3(data.cellSpacing.x, 0.45f * (data.cellSpacing.x / 1.1f), data.cellSpacing.z);
            palette = new Dictionary<int, Color>(colors);
            cells = new Cell[Rows, Columns];
            var origin = data.firstCellPosition.ToVector3();
            int coloredCount = 0;
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    int id = data.cells[row * Columns + column];
                    var position = new Vector3(origin.x + column * data.cellSpacing.x,
                        origin.y, origin.z - row * data.cellSpacing.z);
                    cells[row, column] = new Cell(row, column, id, position);
                    if (id != 0) coloredCount++;
                }
            }
            ColoredCellCount = coloredCount;
        }

        public Cell GetCell(int row, int column)
        {
            if (row < 0 || row >= Rows) throw new ArgumentOutOfRangeException(nameof(row));
            if (column < 0 || column >= Columns) throw new ArgumentOutOfRangeException(nameof(column));
            return cells[row, column];
        }

        public bool TryCollect(Cell cell)
        {
            if (cell == null || cell.IsEmpty || cell.Row < 0 || cell.Row >= Rows ||
                cell.Column < 0 || cell.Column >= Columns || cells[cell.Row, cell.Column] != cell) return false;
            cell.ColorId = 0;
            ColoredCellCount--;
            return true;
        }

        public Color GetColor(int colorId) => palette[colorId];

        public IEnumerable<Cell> EnumerateCells()
        {
            for (int row = 0; row < Rows; row++)
                for (int column = 0; column < Columns; column++)
                    yield return cells[row, column];
        }
    }
}
