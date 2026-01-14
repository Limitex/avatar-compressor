using nadena.dev.ndmf;

[assembly: ExportsPlugin(typeof(dev.limitex.avatar.compressor.texture.TextureCompressorPlugin))]

namespace dev.limitex.avatar.compressor.texture
{
    /// <summary>
    /// NDMF plugin that integrates TextureCompressorService into the avatar build pipeline.
    /// </summary>
    public class TextureCompressorPlugin : Plugin<TextureCompressorPlugin>
    {
        public override string DisplayName => "LAC Texture Compressor";
        public override string QualifiedName => "dev.limitex.avatar-compressor.texture";

        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming)
                .AfterPlugin("nadena.dev.modular-avatar")
                .Run(TextureCompressorPass.Instance);
        }
    }
}
