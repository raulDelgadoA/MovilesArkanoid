using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Cyan
{
    public class Blit : ScriptableRendererFeature
    {
        public class BlitPass : ScriptableRenderPass
        {
            public Material blitMaterial = null;
            public FilterMode filterMode { get; set; }

            private BlitSettings settings;

            // En Unity 6 usamos RTHandle en lugar de RenderTargetIdentifier/Handle
            private RTHandle sourceHandle;
            private RTHandle destinationHandle;
            private RTHandle m_TemporaryColorTexture;
            private RTHandle m_DestinationTexture;

            string m_ProfilerTag;

            public BlitPass(RenderPassEvent renderPassEvent, BlitSettings settings, string tag)
            {
                this.renderPassEvent = renderPassEvent;
                this.settings = settings;
                blitMaterial = settings.blitMaterial;
                m_ProfilerTag = tag;
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                // Configurar inputs necesarios (Normales, etc)
                if (settings.requireDepthNormals)
                    ConfigureInput(ScriptableRenderPassInput.Normal);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (blitMaterial == null) return;

                CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

                // Obtener el descriptor de la cámara para saber resolución y formato
                RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0; // No necesitamos buffer de profundidad para el blit de color

                var renderer = renderingData.cameraData.renderer;

                // 1. Configurar SOURCE (Origen)
                if (settings.srcType == Target.CameraColor)
                {
                    sourceHandle = renderer.cameraColorTargetHandle;
                }
                else if (settings.srcType == Target.TextureID)
                {
                    // Nota: Manejar texturas globales externas con RTHandle requiere que ya existan como RTHandle
                    // Para compatibilidad simple, intentamos obtenerlo si es una global conocida, 
                    // pero idealmente deberías pasar un RTHandle directo.
                    // Aquí asumimos que sourceHandle se asignará dinámicamente o se usará RTHandles.Alloc si es externo.
                    // Para este fix rápido, usaremos el color de cámara si falla, o crearemos uno temporal.
                    sourceHandle = RTHandles.Alloc(settings.srcTextureId);
                }
                else if (settings.srcType == Target.RenderTextureObject && settings.srcTextureObject != null)
                {
                    sourceHandle = RTHandles.Alloc(settings.srcTextureObject);
                }

                // 2. Configurar DESTINATION (Destino)
                // Si el destino es la cámara, usamos el handle de la cámara
                bool isCameraDst = (settings.dstType == Target.CameraColor);

                if (isCameraDst)
                {
                    destinationHandle = renderer.cameraColorTargetHandle;
                }
                else if (settings.dstType == Target.TextureID)
                {
                    if (settings.overrideGraphicsFormat)
                        desc.graphicsFormat = settings.graphicsFormat;

                    // Reasignar la textura de destino si es necesario (sistema nuevo de memoria)
                    RenderingUtils.ReAllocateIfNeeded(ref m_DestinationTexture, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: settings.dstTextureId);
                    destinationHandle = m_DestinationTexture;
                }
                else if (settings.dstType == Target.RenderTextureObject && settings.dstTextureObject != null)
                {
                    destinationHandle = RTHandles.Alloc(settings.dstTextureObject);
                }

                // 3. Matriz Inversa (Opcional)
                if (settings.setInverseViewMatrix)
                {
                    Shader.SetGlobalMatrix("_InverseView", renderingData.cameraData.camera.cameraToWorldMatrix);
                }

                // 4. Ejecutar el BLIT
                // En Unity 6 / URP 17+, usamos la API Blitter

                if (sourceHandle == destinationHandle || (settings.srcType == settings.dstType && settings.srcType == Target.CameraColor))
                {
                    // Si origen y destino son lo mismo (ej. Cámara -> Cámara), necesitamos una textura temporal
                    RenderingUtils.ReAllocateIfNeeded(ref m_TemporaryColorTexture, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TemporaryColorTexture");

                    // Paso 1: Origen -> Temporal (aplicando el material)
                    Blitter.BlitCameraTexture(cmd, sourceHandle, m_TemporaryColorTexture, blitMaterial, settings.blitMaterialPassIndex);

                    // Paso 2: Temporal -> Destino (copia simple)
                    Blitter.BlitCameraTexture(cmd, m_TemporaryColorTexture, destinationHandle);
                }
                else
                {
                    // Blit directo
                    Blitter.BlitCameraTexture(cmd, sourceHandle, destinationHandle, blitMaterial, settings.blitMaterialPassIndex);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public void Dispose()
            {
                m_TemporaryColorTexture?.Release();
                m_DestinationTexture?.Release();
            }
        }

        [System.Serializable]
        public class BlitSettings
        {
            public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;

            public Material blitMaterial = null;
            public int blitMaterialPassIndex = 0;
            public bool setInverseViewMatrix = false;
            public bool requireDepthNormals = false;

            public Target srcType = Target.CameraColor;
            public string srcTextureId = "_CameraColorTexture";
            public RenderTexture srcTextureObject;

            public Target dstType = Target.CameraColor;
            public string dstTextureId = "_BlitPassTexture";
            public RenderTexture dstTextureObject;

            public bool overrideGraphicsFormat = false;
            public UnityEngine.Experimental.Rendering.GraphicsFormat graphicsFormat;

            public bool canShowInSceneView = true;
        }

        public enum Target
        {
            CameraColor,
            TextureID,
            RenderTextureObject
        }

        public BlitSettings settings = new BlitSettings();
        public BlitPass blitPass;

        public override void Create()
        {
            var passIndex = settings.blitMaterial != null ? settings.blitMaterial.passCount - 1 : 1;
            settings.blitMaterialPassIndex = Mathf.Clamp(settings.blitMaterialPassIndex, -1, passIndex);

            // Crear el pase
            blitPass = new BlitPass(settings.Event, settings, name);

            if (settings.graphicsFormat == UnityEngine.Experimental.Rendering.GraphicsFormat.None)
            {
                settings.graphicsFormat = SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.LDR);
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.isPreviewCamera) return;
            if (!settings.canShowInSceneView && renderingData.cameraData.isSceneViewCamera) return;

            if (settings.blitMaterial == null)
            {
                Debug.LogWarningFormat("Missing Blit Material. {0} blit pass will not execute.", GetType().Name);
                return;
            }

            // Ya no es necesario el parche de AfterPostProcessTexture en Unity 6, 
            // el sistema de RenderGraph y RTHandles lo gestiona mejor, 
            // pero mantenemos la lógica simple aquí.

            renderer.EnqueuePass(blitPass);
        }

        protected override void Dispose(bool disposing)
        {
            blitPass?.Dispose();
        }
    }
}