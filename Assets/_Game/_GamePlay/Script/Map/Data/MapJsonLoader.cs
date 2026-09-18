using System.Collections.Generic;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public static class MapJsonLoader
    {
        // Level JSON is authored data. Invalid field values return null.
        public static MapModel Load(string json)
        {
            var data = ReadData(json);
            if (!HasValidLayout(data)) return null;
            var colors = ReadPalette(data.palette);
            if (colors == null || !HasValidColors(data, colors)) return null;
            return new MapModel(data, colors);
        }

        private static MapJsonData ReadData(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            string document = json.Trim();
            if (!document.StartsWith("{") || !document.EndsWith("}")) return null;
            return JsonUtility.FromJson<MapJsonData>(document);
        }

        private static bool HasValidLayout(MapJsonData data)
        {
            if (data == null || data.rows <= 0 || data.columns <= 0) return false;
            if (data.cells == null || data.cells.LongLength != (long)data.rows * data.columns) return false;
            if (!IsFinite(data.cameraPosition) || !IsFinite(data.cameraRotation) || !IsFinite(data.firstCellPosition)) return false;
            if (data.cellSpacing == null || !IsPositive(data.cellSpacing.x) || !IsPositive(data.cellSpacing.z)) return false;
            if (data.cellScale != null && (data.cellScale.x != 0 || data.cellScale.y != 0 || data.cellScale.z != 0) &&
                (!IsPositive(data.cellScale.x) || !IsPositive(data.cellScale.y) || !IsPositive(data.cellScale.z))) return false;
            return IsFinite(data.firstCellPosition.x + (data.columns - 1) * data.cellSpacing.x) &&
                IsFinite(data.firstCellPosition.z - (data.rows - 1) * data.cellSpacing.z);
        }

        private static Dictionary<int, Color> ReadPalette(MapPaletteEntry[] palette)
        {
            if (palette == null) return null;
            var colors = new Dictionary<int, Color>();
            foreach (var entry in palette)
            {
                if (entry == null || entry.id <= 0 || colors.ContainsKey(entry.id) ||
                    !IsHexColor(entry.hex) || !ColorUtility.TryParseHtmlString(entry.hex, out var color)) return null;
                colors.Add(entry.id, color);
            }
            return colors;
        }

        private static bool HasValidColors(MapJsonData data, Dictionary<int, Color> colors)
        {
            foreach (int id in data.cells)
                if (id < 0 || id != 0 && !colors.ContainsKey(id)) return false;
            return HasValidQueues(data.queues, colors);
        }

        private static bool HasValidQueues(BoxQueueData[] queues, Dictionary<int, Color> colors)
        {
            if (queues == null) return false;
            foreach (var queue in queues)
            {
                if (queue?.boxes == null) return false;
                foreach (var box in queue.boxes)
                    if (box == null || box.antCount <= 0 || !colors.ContainsKey(box.colorId) ||
                        box.kind < BoxKind.Normal || box.kind > BoxKind.Stick) return false;
            }
            return true;
        }

        private static bool IsHexColor(string value)
        {
            if (value == null || (value.Length != 7 && value.Length != 9) || value[0] != '#') return false;
            for (int i = 1; i < value.Length; i++)
            {
                char c = value[i];
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))) return false;
            }
            return true;
        }

        private static bool IsFinite(MapVectorData value) => value != null && IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool IsPositive(float value) => IsFinite(value) && value > 0;
    }
}
