using dev.limitex.avatar.compressor.common;
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
                .BeforePlugin("nadena.dev.modular-avatar")
                .BeforePlugin("com.anatawa12.avatar-optimizer")
                .Run("Avatar Compressor: Compress Avatar Textures", ctx =>
                {
                    var components = ctx.AvatarRootObject.GetComponentsInChildren<TextureCompressor>(true);

                    if (components.Length == 0) return;

                    var config = components[0];
                    var service = new TextureCompressorService(config);

                    service.Compress(ctx.AvatarRootObject, config.EnableLogging);

                    CleanupComponents(components);
                });
        }

        private static void CleanupComponents(TextureCompressor[] components)
        {
            foreach (var component in components)
            {
                ComponentUtils.SafeDestroy(component);
            }
        }
    }
}
