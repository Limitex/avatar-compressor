using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture.integrations
{
    /// <summary>
    /// Bridges to lilToon's official unused-texture remover
    /// (<c>lilToon.lilMaterialUtils.RemoveUnusedTexture</c>) through reflection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// lilToon is an optional, external package and is not a declared dependency of this project,
    /// so it cannot be referenced at compile time. Reflection keeps the build green whether or not
    /// lilToon is installed: when its types are absent, <see cref="IsAvailable"/> is false and every
    /// call is a no-op (the unused-slot feature simply passes through).
    /// </para>
    /// <para>
    /// Delegating to lilToon's own API means the toggle → texture map is always lilToon's
    /// authoritative, maintained logic rather than a copy that can drift. <c>RemoveUnusedTexture</c>
    /// self-guards on the shader name, so it is safe to call on every material and only acts on
    /// lilToon ones.
    /// </para>
    /// <para>
    /// Behavioral details of the lilToon API to be aware of: it also strips all serialized
    /// properties not declared by the material's current shader (a bigger mutation than just
    /// clearing texture slots — safe here because it only ever runs on build-time clones), and it
    /// consults <c>animatedProperties</c> only for feature <em>toggles</em>. A slot whose texture
    /// binding is animated but whose toggle is statically off is still cleared here; the texture
    /// itself is kept in the build by <see cref="UnusedSlotPruner"/>, which restores slots holding
    /// animation-referenced textures. The one inverted case (<c>_AudioLinkMask</c>) is guarded by
    /// <see cref="ShouldPreserveAudioLinkMask"/>.
    /// </para>
    /// </remarks>
    public sealed class LilToonUnusedSlotOptimizer : IUnusedSlotOptimizer
    {
        private const string TypeName = "lilToon.lilMaterialUtils";
        private const string MethodName = "RemoveUnusedTexture";

        private const string AudioLinkMaskProperty = "_AudioLinkMask";
        private const string AudioLinkToggleProperty = "_UseAudioLink";
        private const string AudioLinkUvModeProperty = "_AudioLinkUVMode";

        // Cleared on the first invocation failure so a broken lilToon API disables the feature
        // for the rest of the build instead of throwing or spamming warnings.
        private MethodInfo _removeUnusedTexture;

        public LilToonUnusedSlotOptimizer()
        {
            Type type = FindType(TypeName);
            if (type == null)
                return; // lilToon not installed: documented silent pass-through

            _removeUnusedTexture = ResolveMethod(type);
            if (_removeUnusedTexture == null)
            {
                // Distinguish "installed but incompatible" from "not installed": the user has the
                // feature toggle on and lilToon present, so a silent no-op would look like success.
                Debug.LogWarning(
                    $"[LAC Texture Compressor] lilToon is installed, but {TypeName}.{MethodName}"
                        + "(Material, params string[]) was not found (requires lilToon 1.8.0 or "
                        + "newer). Unused texture slot removal is skipped."
                );
            }
        }

        public bool IsAvailable => _removeUnusedTexture != null;

        public void ClearUnusedSlots(
            Material material,
            IReadOnlyCollection<string> animatedProperties
        )
        {
            if (_removeUnusedTexture == null || material == null || material.shader == null)
                return;

            string[] animated =
                animatedProperties != null ? animatedProperties.ToArray() : Array.Empty<string>();

            Texture audioLinkMask = ShouldPreserveAudioLinkMask(material, animated)
                ? material.GetTexture(AudioLinkMaskProperty)
                : null;

            try
            {
                // RemoveUnusedTexture(Material material, params string[] animatedProps)
                _removeUnusedTexture.Invoke(null, new object[] { material, animated });
            }
            catch (Exception ex)
            {
                _removeUnusedTexture = null; // disable for the rest of this build
                Debug.LogWarning(
                    "[LAC Texture Compressor] lilToon unused-texture removal failed: "
                        + $"{(ex.InnerException ?? ex).Message}. Unused-slot detection is disabled "
                        + "for the rest of this build."
                );
            }

            if (audioLinkMask != null && material.GetTexture(AudioLinkMaskProperty) == null)
                material.SetTexture(AudioLinkMaskProperty, audioLinkMask);
        }

        /// <summary>
        /// lilToon's <c>RemoveUnusedTexture</c> keeps <c>_AudioLinkMask</c> only when
        /// <c>_AudioLinkUVMode</c> is statically 3 (Mask), but the shader samples the mask in
        /// modes 3 <em>and</em> 4 (Spectrum Mask), and it also treats an <em>animated</em> UV mode
        /// as a reason to clear the mask — the inverse of every other slot, where presence in
        /// <c>animatedProps</c> protects the texture. So the mask must be preserved whenever the
        /// AudioLink feature can be active (toggle statically on, or animated) and the mask can be
        /// sampled at runtime (mode statically 4, or the mode is animated and can land on 3/4).
        /// Static mode 3 needs no preservation: lilToon keeps it itself.
        /// </summary>
        public static bool ShouldPreserveAudioLinkMask(
            Material material,
            string[] animatedProperties
        )
        {
            if (material == null)
                return false;

            if (
                !material.HasProperty(AudioLinkMaskProperty)
                || !material.HasProperty(AudioLinkToggleProperty)
                || !material.HasProperty(AudioLinkUvModeProperty)
            )
                return false;

            bool featureCanBeActive =
                material.GetFloat(AudioLinkToggleProperty) != 0f
                || IsAnimated(animatedProperties, AudioLinkToggleProperty);
            if (!featureCanBeActive)
                return false;

            return material.GetFloat(AudioLinkUvModeProperty) == 4f
                || IsAnimated(animatedProperties, AudioLinkUvModeProperty);
        }

        private static bool IsAnimated(string[] animatedProperties, string property)
        {
            return animatedProperties != null && Array.IndexOf(animatedProperties, property) >= 0;
        }

        private static MethodInfo ResolveMethod(Type type)
        {
            return type.GetMethod(
                MethodName,
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(Material), typeof(string[]) },
                modifiers: null
            );
        }

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName, throwOnError: false);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
}
