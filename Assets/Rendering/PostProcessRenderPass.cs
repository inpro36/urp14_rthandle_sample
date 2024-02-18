using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal class PostProcessRenderPass : ScriptableRenderPass
{
    private ProfilingSampler _profilingSampler = new ProfilingSampler("PostProcess");
    private MaterialLibrary m_Materials;
    private ColorAberration m_ColorAberration;
    private static readonly int IntensityShaderId = Shader.PropertyToID("_AberrationIntensity");
    private RenderTextureDescriptor m_Descriptor;
    
    public PostProcessRenderPass(Shader uberPost)
    {
        m_Materials = new MaterialLibrary(uberPost);
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }
    
    public void Cleanup() => m_Materials.Cleanup();

    public void Setup(in RenderTextureDescriptor baseDescriptor)
    {
        m_Descriptor = baseDescriptor;
        m_Descriptor.useMipMap = false;
        m_Descriptor.autoGenerateMips = false;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        // ConfigureTargetはConfigure内で呼ぶこと
        // Configure内でSetRenderTargetによる任意のRenderTarget指定をしてはいけない
        // 今回は何もしない。
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (m_Materials == null)
        {
            Debug.LogError("PostProcessRenderPass.Execute: Material is null.");
            return;
        }

        if (renderingData.cameraData.cameraType == CameraType.SceneView || renderingData.cameraData.cameraType == CameraType.Preview)
        {
            return;
        }
        
        var stack = VolumeManager.instance.stack;
        m_ColorAberration = stack.GetComponent<ColorAberration>();
        
        // ObjectPoolからCommandBufferを取り出す.
        CommandBuffer cmd = CommandBufferPool.Get();

        using (new ProfilingScope(cmd, _profilingSampler))
        {
            bool colorAberrationActive = m_ColorAberration.IsActive();
            if (colorAberrationActive)
            {
                Render(cmd, ref renderingData);
            }
        }
        
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        CommandBufferPool.Release(cmd);
    }

    void Render(CommandBuffer cmd, ref RenderingData renderingData)
    {
        // NOTE: URP標準PostProcessPassでのSwap処理について(PostProcessPass.Swap参照)
        // URP標準PostProcessPassでは、m_UseSwapBuffer(SwapBufferを使用するかどうかのフラグ)が常にtrueになっており、
        // 特に理由がない限り、RTHandleが確保しているFrontBufferとBackBufferのSwapで実現している。
        // SwapBufferを使用しない場合は、TempRTHandleを経由してBlitしている。
        // ScriptableRenderPass.Blit内でBlit処理とSwap処理をおこなっている。
        if (m_Materials.uber != null)
        {
            m_Materials.uber.SetFloat(IntensityShaderId, m_ColorAberration.intensity.value);
            Blit(cmd, ref renderingData, m_Materials.uber, 0);
        }

        // RenderingUtils.ReAllocateIfNeededによるリアロケート処理は、必要な時にしかリアロケートしないので、同じRTHandleを使っていればアロケートは発生しない
        // Load、Storeの可否を明示的に指定することができる。URP標準PostProcessPassではLoad、Storeを明示的に指定している。
        // また、Load、Store処理が正常に動作するかはモバイル端末(Tile-Based Rendering)でのみ確認可能。
    }
    
    class MaterialLibrary
    {
        public readonly Material uber;
    
        public MaterialLibrary(Shader uberPost)
        {
            uber = Load(uberPost);
        }
        
        Material Load(Shader shader)
        {
            if (shader == null)
            {
                Debug.LogErrorFormat($"Missing shader. {GetType().DeclaringType.Name} render pass will not execute. Check for missing reference in the renderer resources.");
                return null;
            }
            else if (!shader.isSupported)
            {
                return null;
            }

            return CoreUtils.CreateEngineMaterial(shader);
        }

        internal void Cleanup()
        {
            CoreUtils.Destroy(uber);
        }
    }
}