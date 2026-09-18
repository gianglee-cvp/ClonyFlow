using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    [Serializable] public sealed class BoxQueueJsonData { public BoxQueueJsonQueue[] queues; }
    [Serializable] public sealed class BoxQueueJsonQueue { public BoxQueueJsonBox[] boxes; }
    [Serializable] public sealed class BoxQueueJsonBox { public int colorId; public int antCount; }

    public static class BoxQueueJsonLoader
    {
        // queues are left-to-right; boxes are front-to-back. Never sort the authored data.
        public static DemoBoxData[] Load(string json, MapModel map, int maximumQueues)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new FormatException("Box queue JSON is empty.");
            BoxQueueJsonData data;
            try { data = JsonUtility.FromJson<BoxQueueJsonData>(json); }
            catch (ArgumentException ex) { throw new FormatException("Invalid Box queue JSON.", ex); }
            if (data == null || data.queues == null || data.queues.Length > maximumQueues)
                throw new FormatException($"Box queue JSON must contain queues (at most {maximumQueues}).");
            var result = new List<DemoBoxData>();
            for (int q = 0; q < data.queues.Length; q++)
            {
                if (data.queues[q] == null || data.queues[q].boxes == null)
                    throw new FormatException($"Queue {q} must contain a boxes array.");
                for (int b = 0; b < data.queues[q].boxes.Length; b++)
                {
                    var box = data.queues[q].boxes[b];
                    if (box == null || box.colorId <= 0 || box.antCount <= 0)
                        throw new FormatException($"Queue {q}, Box {b}: colorId and antCount must be positive.");
                    try { map.GetColor(box.colorId); }
                    catch (KeyNotFoundException) { throw new FormatException($"Queue {q}, Box {b}: unknown colorId {box.colorId}."); }
                    result.Add(new DemoBoxData { queueIndex = q, colorId = box.colorId, antCount = box.antCount });
                }
            }
            return result.ToArray();
        }
    }
}
