using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    internal sealed class SharedColorMaterialCache : IDisposable
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private readonly Dictionary<int, Material> materials = new Dictionary<int, Material>();
        private readonly Material template;

        public SharedColorMaterialCache(Material materialTemplate)
        {
            template = materialTemplate;
        }

        public Material Get(int colorId, Color color)
        {
            if (materials.TryGetValue(colorId, out var material)) return material;
            if (template == null) return null;
            material = new Material(template)
            {
                name = template.name + " Color " + colorId,
                enableInstancing = true,
                hideFlags = HideFlags.DontSave
            };
            if (material.HasProperty(BaseColor)) material.SetColor(BaseColor, color);
            if (material.HasProperty(ColorProperty)) material.SetColor(ColorProperty, color);
            materials.Add(colorId, material);
            return material;
        }

        public void Dispose()
        {
            foreach (var material in materials.Values)
            {
                if (material == null) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(material);
                else UnityEngine.Object.DestroyImmediate(material);
            }
            materials.Clear();
        }
    }
}
