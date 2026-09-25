using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace ColonyFlow.Gameplay
{
    public sealed class QueueOutlineFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader outlineShader;
        private Material material;
        private OutlinePass pass;

        public override void Create()
        {
            CoreUtils.Destroy(material);
            if (outlineShader == null) return;
            material = CoreUtils.CreateEngineMaterial(outlineShader);
            pass = new OutlinePass(material) { renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (material == null || BoxActor.OutlineBoxes.Count == 0 ||
                renderingData.cameraData.cameraType != CameraType.Game) return;
            var settings = GameplayOutlineSettings.Active;
            if (settings != null && !settings.ShowOutline) return;
            material.SetFloat("_WidthPixels", settings != null ? settings.WidthPixels : 3f);
            material.SetColor("_OutlineColor", settings != null ? settings.OutlineColor : Color.white);
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing) => CoreUtils.Destroy(material);

        private sealed class OutlinePass : ScriptableRenderPass
        {
            private readonly Material material;
            private readonly List<Renderer> targets = new();
            private sealed class MaskData
            {
                public Material material;
                public List<Renderer> targets;
            }
            private sealed class CompositeData
            {
                public Material material;
                public TextureHandle mask;
            }

            public OutlinePass(Material material)
            {
                this.material = material;
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                targets.Clear();
                var camera = frameData.Get<UniversalCameraData>();
                foreach (var box in BoxActor.OutlineBoxes)
                    if (box != null && box.isActiveAndEnabled) box.CollectOutlineRenderers(targets);
                targets.RemoveAll(r => (camera.camera.cullingMask & (1 << r.gameObject.layer)) == 0);
                if (targets.Count == 0) return;

                var resources = frameData.Get<UniversalResourceData>();
                var descriptor = camera.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0;
                descriptor.msaaSamples = 1;
                descriptor.graphicsFormat = GraphicsFormat.R8_UNorm;
                var mask = UniversalRenderer.CreateRenderGraphTexture(graph, descriptor, "Queue silhouette", true);
                using (var builder = graph.AddRasterRenderPass<MaskData>("Queue silhouette mask", out var data))
                {
                    data.material = material;
                    data.targets = targets;
                    builder.SetRenderAttachment(mask, 0, AccessFlags.Write);
                    builder.UseTexture(resources.cameraDepthTexture, AccessFlags.Read);
                    builder.UseAllGlobalTextures(true);
                    builder.SetRenderFunc((MaskData d, RasterGraphContext context) =>
                    {
                        foreach (var target in d.targets)
                        {
                            if (target == null) continue;
                            int submeshes = target is SkinnedMeshRenderer skin && skin.sharedMesh != null
                                ? skin.sharedMesh.subMeshCount
                                : target.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null
                                    ? filter.sharedMesh.subMeshCount : 1;
                            for (int i = 0; i < submeshes; i++)
                                context.cmd.DrawRenderer(target, d.material, i, 0);
                        }
                    });
                }
                using (var builder = graph.AddRasterRenderPass<CompositeData>("Queue white outline", out var data))
                {
                    data.material = material;
                    data.mask = mask;
                    builder.UseTexture(mask, AccessFlags.Read);
                    builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                    builder.SetRenderFunc((CompositeData d, RasterGraphContext context) =>
                        Blitter.BlitTexture(context.cmd, d.mask, new Vector4(1, 1, 0, 0), d.material, 1));
                }
            }
        }
    }
}
