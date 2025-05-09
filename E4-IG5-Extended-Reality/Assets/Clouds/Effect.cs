//Created by Paro.
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;

public class Effect : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        //future settings
        public Material material;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
        public Color color = new Color(1, 1, 1, 1);
        public Texture2D CloudShapeTexture;
        public int Steps = 15;
        public Vector3 BoundsMin = new Vector3(-100, -100, -100);
        public Vector3 BoundsMax = new Vector3(100, 100, 100);
        public float CloudScale = 1.0f;
        public float CloudSmooth = 5.0f;
        public Vector3 Wind = new Vector3(1, 0, 0);
        public float ContainerEdgeFadeDst = 45;
        public float DensityThreshold = 0.25f;
        public float DensityMultiplier = 1.0f;
        [Range(0, 1)]
        public float detailCloudWeight = 0.24f;
        public Texture3D DetailCloudNoiseTexture;
        public float DetailCloudScale = 1.0f;
        public Vector3 DetailCloudWind = new Vector3(0.5f, 0, 0);
    }

    public Settings settings = new Settings();
    class Pass : ScriptableRenderPass
    {
        public Settings settings;
        private RenderTargetIdentifier source;
        RenderTargetHandle tempTexture;

        private string profilerTag;

        public void Setup(RenderTargetIdentifier source)
        {
            this.source = source;
        }

        public Pass(string profilerTag)
        {
            this.profilerTag = profilerTag;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(tempTexture.id, cameraTextureDescriptor);
            ConfigureTarget(tempTexture.Identifier());
            ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
            cmd.Clear();

            //it is very important that if something fails our code still calls 
            //CommandBufferPool.Release(cmd) or we will have a HUGE memory leak
            if (settings.material == null) return;

            try
            {
                //here we set out material properties
                //...
                settings.material.SetColor("_color", settings.color);

                settings.material.SetVector("_BoundsMin", settings.BoundsMin);
                settings.material.SetVector("_BoundsMax", settings.BoundsMax);
                settings.material.SetTexture("_ShapeNoise", settings.CloudShapeTexture);
                settings.material.SetFloat("_NumSteps", (float)settings.Steps);
                settings.material.SetFloat("_CloudScale", Mathf.Abs(settings.CloudScale));
                settings.material.SetVector("_Wind", settings.Wind);
                settings.material.SetFloat("_containerEdgeFadeDst", Mathf.Abs(settings.ContainerEdgeFadeDst));
                settings.material.SetFloat("_DensityThreshold", settings.DensityThreshold);
                settings.material.SetFloat("_DensityMultiplier", Mathf.Abs(settings.DensityMultiplier));
                settings.material.SetFloat("_cloudSmooth", settings.CloudSmooth);
                settings.material.SetFloat("_detailNoiseScale", Mathf.Abs(settings.DetailCloudScale));
                settings.material.SetVector("_detailNoiseWind", settings.DetailCloudWind);
                settings.material.SetFloat("_detailNoiseWeight", settings.detailCloudWeight);
                settings.material.SetTexture("_DetailNoise", settings.DetailCloudNoiseTexture);

                //never use a Blit from source to source, as it only works with MSAA
                // enabled and the scene view doesnt have MSAA,
                // so the scene view will be pure black

                cmd.Blit(source, tempTexture.Identifier());
                cmd.Blit(tempTexture.Identifier(), source, settings.material, 0);

                context.ExecuteCommandBuffer(cmd);
            }
            catch
            {
                Debug.LogError("Error");
            }
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }

    Pass pass;
    RenderTargetHandle renderTextureHandle;
    public override void Create()
    {
        pass = new Pass("Effect");
        name = "Effect";
        pass.settings = settings;
        pass.renderPassEvent = settings.renderPassEvent;
    }
#if UNITY_2022_1_OR_NEWER
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        var cameraColorTargetIdent = renderer.cameraColorTarget;
        pass.Setup(cameraColorTargetIdent);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(pass);
    }
#else
    // called every frame once per camera
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var cameraColorTargetIdent = renderer.cameraColorTarget;
        pass.Setup(cameraColorTargetIdent);
        renderer.EnqueuePass(pass);
    }
#endif
}
