using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public static class MapJsonLoader
    {
        public static MapModel Load(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new FormatException("Map JSON is empty.");

            string document = json.Trim();
            if (!document.StartsWith("{", StringComparison.Ordinal) ||
                !document.EndsWith("}", StringComparison.Ordinal))
                throw new FormatException("Map JSON must be an object.");

            MapJsonData data;
            try
            {
                data = JsonUtility.FromJson<MapJsonData>(document);
            }
            catch (ArgumentException exception)
            {
                throw new FormatException("Cannot parse map JSON: " + exception.Message, exception);
            }

            if (data == null || data.rows <= 0 || data.columns <= 0)
                throw new FormatException("Map rows and columns must be positive.");

            long expectedCount = (long)data.rows * data.columns;
            if (expectedCount > int.MaxValue || data.cells == null || data.cells.Length != expectedCount)
                throw new FormatException("Map cells count must equal rows * columns.");

            ValidateVector(data.cameraPosition, "cameraPosition");
            ValidateVector(data.cameraRotation, "cameraRotation");
            ValidateVector(data.firstCellPosition, "firstCellPosition");
            if (data.cellScale != null && (data.cellScale.x != 0 || data.cellScale.y != 0 || data.cellScale.z != 0))
            {
                ValidateVector(data.cellScale, "cellScale");
                if (data.cellScale.x <= 0 || data.cellScale.y <= 0 || data.cellScale.z <= 0)
                    throw new FormatException("Map cellScale components must be positive.");
            }
            if (data.cellSpacing == null || !IsFinite(data.cellSpacing.x) ||
                !IsFinite(data.cellSpacing.z) || data.cellSpacing.x <= 0 || data.cellSpacing.z <= 0)
                throw new FormatException("Map cellSpacing.x and cellSpacing.z must be finite and positive.");

            // Check the furthest coordinate before allocating any scene objects.
            if (!IsFinite(data.firstCellPosition.x + (data.columns - 1) * data.cellSpacing.x) ||
                !IsFinite(data.firstCellPosition.z - (data.rows - 1) * data.cellSpacing.z))
                throw new FormatException("Map firstCellPosition/cellSpacing produce non-finite coordinates.");

            if (data.palette == null)
                throw new FormatException("Map palette is required (it can be empty for an empty map).");

            var colors = new Dictionary<int, Color>();
            for (int i = 0; i < data.palette.Length; i++)
            {
                var entry = data.palette[i];
                if (entry == null || entry.id <= 0 || colors.ContainsKey(entry.id))
                    throw new FormatException($"Map palette[{i}].id must be positive and unique; 0 is empty.");
                if (!IsHexColor(entry.hex) || !ColorUtility.TryParseHtmlString(entry.hex, out var color))
                    throw new FormatException($"Map palette[{i}].hex must be #RRGGBB or #RRGGBBAA.");
                colors.Add(entry.id, color);
            }

            for (int i = 0; i < data.cells.Length; i++)
            {
                int id = data.cells[i];
                if (id < 0 || (id != 0 && !colors.ContainsKey(id)))
                    throw new FormatException($"Map cells[{i}] has unknown colorId {id}.");
            }

            return new MapModel(data, colors);
        }

        private static bool IsHexColor(string value)
        {
            if (value == null || (value.Length != 7 && value.Length != 9) || value[0] != '#')
                return false;
            for (int i = 1; i < value.Length; i++)
            {
                char c = value[i];
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                    return false;
            }
            return true;
        }

        private static void ValidateVector(MapVectorData vector, string name)
        {
            if (vector == null || !IsFinite(vector.x) || !IsFinite(vector.y) || !IsFinite(vector.z))
                throw new FormatException($"Map {name} is required and must contain finite coordinates.");
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
