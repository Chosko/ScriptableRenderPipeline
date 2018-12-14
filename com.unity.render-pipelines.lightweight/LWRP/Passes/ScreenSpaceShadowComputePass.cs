using UnityEngine.Experimental.VoxelizedShadowMap;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class ScreenSpaceShadowComputePass : ScriptableRenderPass
    {
        private static class VxShadowConstantBuffer
        {
            public static int _InvViewProjMatrixID;
            public static int _ScreenSizeID;

            public static int _VoxelResolutionID;
            public static int _VoxelBiasID;
            public static int _MaxScaleID;
            public static int _WorldToShadowMatrixID;

            public static int _VxShadowMapBufferID;
            public static int _ScreenSpaceShadowOutputID;
        }

        public class Resources : ScriptableObject
        {
            ComputeShader computeShader;
        }

        static readonly int TileSize = 8;
        static readonly int TileAdditive = TileSize - 1;

        RenderTextureFormat m_ColorFormat;
        ComputeShader m_ComputeShader;

        public ScreenSpaceShadowComputePass(LightweightForwardRenderer renderer) : base(renderer)
        {
            VxShadowConstantBuffer._InvViewProjMatrixID = Shader.PropertyToID("_InvViewProjMatrix");
            VxShadowConstantBuffer._ScreenSizeID = Shader.PropertyToID("_ScreenSize");

            VxShadowConstantBuffer._VoxelResolutionID = Shader.PropertyToID("_VoxelResolution");
            VxShadowConstantBuffer._VoxelBiasID = Shader.PropertyToID("_VoxelBias");
            VxShadowConstantBuffer._MaxScaleID = Shader.PropertyToID("_MaxScale");
            VxShadowConstantBuffer._WorldToShadowMatrixID = Shader.PropertyToID("_WorldToShadowMatrix");

            VxShadowConstantBuffer._VxShadowMapBufferID = Shader.PropertyToID("_VxShadowMapBuffer");
            VxShadowConstantBuffer._ScreenSpaceShadowOutputID = Shader.PropertyToID("_ScreenSpaceShadowOutput");

            bool R8_UNorm = SystemInfo.IsFormatSupported(GraphicsFormat.R8_UNorm, FormatUsage.LoadStore);
            bool R8_SNorm = SystemInfo.IsFormatSupported(GraphicsFormat.R8_SNorm, FormatUsage.LoadStore);
            bool R8_UInt  = SystemInfo.IsFormatSupported(GraphicsFormat.R8_UInt,  FormatUsage.LoadStore);
            bool R8_SInt  = SystemInfo.IsFormatSupported(GraphicsFormat.R8_SInt,  FormatUsage.LoadStore);
            
            bool R8 = R8_UNorm || R8_SNorm || R8_UInt || R8_SInt;
            
            m_ColorFormat = R8 ? RenderTextureFormat.R8 : RenderTextureFormat.RFloat;

            Debug.Log("Format = " + m_ColorFormat);

            m_ComputeShader = renderer.screenSpaceShadowComputeShader;
        }

        private RenderTargetHandle colorAttachmentHandle { get; set; }
        private RenderTextureDescriptor descriptor { get; set; }

        public void Setup(
            RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle colorAttachmentHandle)
        {
            this.colorAttachmentHandle = colorAttachmentHandle;

            baseDescriptor.autoGenerateMips = false;
            baseDescriptor.useMipMap = false;
            baseDescriptor.sRGB = false;
            baseDescriptor.depthBufferBits = 0;
            baseDescriptor.colorFormat = m_ColorFormat;
            baseDescriptor.enableRandomWrite = true;
            descriptor = baseDescriptor;
        }

        public override void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData)
        {
            // note: this pass must be executed, not related to DirectionalShadowsPass
            //if (renderingData.shadowData.renderedDirectionalShadowQuality == LightShadows.None)
                //return;
            if (renderingData.shadowData.directionalVxShadowMap == null)
                return;

            int kernel = -1;

            if (m_ComputeShader != null)
            {
                if (renderingData.shadowData.renderedDirectionalShadowQuality != LightShadows.None)
                    kernel = m_ComputeShader.FindKernel("ScreenSpaceShadowWithDynamicShadowsBiFiltering");
                    //kernel = m_ComputeShader.FindKernel("ScreenSpaceShadowWithDynamicShadowsNoFiltering");
                else
                    kernel = m_ComputeShader.FindKernel("ScreenSpaceShadowWithoutDynamicShadowsBiFiltering");
                    //kernel = m_ComputeShader.FindKernel("ScreenSpaceShadowWithoutDynamicShadowsNoFiltering");
            }

            if (kernel == -1)
                return;

            CommandBuffer cmd = CommandBufferPool.Get("Collect Shadows");

            cmd.GetTemporaryRT(colorAttachmentHandle.id, descriptor, FilterMode.Bilinear);
            SetupVxShadowReceiverConstants(cmd, kernel, ref renderingData.cameraData.camera, ref renderingData.shadowData.directionalVxShadowMap);

            int x = (renderingData.cameraData.camera.pixelWidth + TileAdditive) / TileSize;
            int y = (renderingData.cameraData.camera.pixelHeight + TileAdditive) / TileSize;

            cmd.DispatchCompute(m_ComputeShader, kernel, x, y, 1);

            if (renderingData.cameraData.isStereoEnabled)
            {
                Camera camera = renderingData.cameraData.camera;
                context.StartMultiEye(camera);
                context.ExecuteCommandBuffer(cmd);
                context.StopMultiEye(camera);
            }
            else
            {
                context.ExecuteCommandBuffer(cmd);
            }
            CommandBufferPool.Release(cmd);

            if (renderingData.shadowData.renderedDirectionalShadowQuality == LightShadows.None)
                renderingData.shadowData.renderedDirectionalShadowQuality = LightShadows.Hard;
        }

        public override void Dispose(CommandBuffer cmd)
        {
            if (colorAttachmentHandle != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(colorAttachmentHandle.id);
                colorAttachmentHandle = RenderTargetHandle.CameraTarget;
            }
        }

        void SetupVxShadowReceiverConstants(CommandBuffer cmd, int kernel, ref Camera camera, ref DirectionalVxShadowMap directionalVxShadowMap)
        {
            float screenSizeX = (float)camera.pixelWidth;
            float screenSizeY = (float)camera.pixelHeight;

            Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
            Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
            Matrix4x4 viewProjMatrix = projMatrix * viewMatrix;

            cmd.SetComputeMatrixParam(m_ComputeShader, VxShadowConstantBuffer._InvViewProjMatrixID, viewProjMatrix.inverse);
            cmd.SetComputeVectorParam(m_ComputeShader, VxShadowConstantBuffer._ScreenSizeID, new Vector4(screenSizeX, screenSizeY, 1.0f / screenSizeX, 1.0f / screenSizeY));

            cmd.SetComputeIntParam(m_ComputeShader, VxShadowConstantBuffer._VoxelResolutionID, (int)directionalVxShadowMap.voxelResolution);
            cmd.SetComputeIntParam(m_ComputeShader, VxShadowConstantBuffer._VoxelBiasID, directionalVxShadowMap.voxelBias);
            cmd.SetComputeIntParam(m_ComputeShader, VxShadowConstantBuffer._MaxScaleID, directionalVxShadowMap.maxScale);
            cmd.SetComputeMatrixParam(m_ComputeShader, VxShadowConstantBuffer._WorldToShadowMatrixID, directionalVxShadowMap.worldToShadowMatrix);

            cmd.SetComputeBufferParam(m_ComputeShader, kernel, VxShadowConstantBuffer._VxShadowMapBufferID, directionalVxShadowMap.computeBuffer);
            cmd.SetComputeTextureParam(m_ComputeShader, kernel, VxShadowConstantBuffer._ScreenSpaceShadowOutputID, colorAttachmentHandle.Identifier());
        }
    }
}
