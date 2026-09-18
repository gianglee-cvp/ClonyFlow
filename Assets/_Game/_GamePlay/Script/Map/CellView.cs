using System;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public sealed class CellView : MonoBehaviour
    {
        public Cell Cell { get; private set; }
        private MaterialPropertyBlock propertyBlock;

        internal static void ValidatePrefab(GameObject prefab)
        {
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException("Cell prefab must contain a Renderer.");
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterials.Length == 0)
                    throw new InvalidOperationException("Cell Renderer must have a material.");
                foreach (var material in renderer.sharedMaterials)
                    GetColorProperty(material);
            }
        }

        public void Initialize(Cell cell, Color color)
        {
            Cell = cell ?? throw new ArgumentNullException(nameof(cell));
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    propertyBlock.Clear();
                    renderer.GetPropertyBlock(propertyBlock, i);
                    propertyBlock.SetColor(GetColorProperty(materials[i]), color);
                    renderer.SetPropertyBlock(propertyBlock, i);
                }
            }
        }

        private static int GetColorProperty(Material material)
        {
            if (material == null)
                throw new InvalidOperationException("Cell Renderer has a missing material.");
            if (material.HasProperty("_BaseColor"))
                return Shader.PropertyToID("_BaseColor");
            if (material.HasProperty("_Color"))
                return Shader.PropertyToID("_Color");
            var shaderName = material.shader != null ? material.shader.name : "Missing shader";
            throw new InvalidOperationException(
                $"Cell material '{material.name}' (shader '{shaderName}') needs a _BaseColor or _Color shader property. " +
                "Check that its shader and required render pipeline package are installed.");
        }
    }
}
