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
    /// Two behavioral details of the lilToon API to be aware of: it also strips all serialized
    /// properties not declared by the material's current shader (a bigger mutation than just
    /// clearing texture slots — safe here because it only ever runs on build-time clones), and it
    /// consults <c>animatedProperties</c> only for feature <em>toggles</em>. A texture slot whose
    /// binding is animated but whose toggle is statically off is still cleared; that is harmless
    /// because the feature can never become visible.
    /// </para>
    /// </remarks>
    public sealed class LilToonUnusedSlotOptimizer : IUnusedSlotOptimizer
    {
        private const string TypeName = "lilToon.lilMaterialUtils";
        private const string MethodName = "RemoveUnusedTexture";

        // Cleared on the first invocation failure so a broken lilToon API disables the feature
        // for the rest of the build instead of throwing or spamming warnings.
        private MethodInfo _removeUnusedTexture;

        public LilToonUnusedSlotOptimizer()
        {
            _removeUnusedTexture = ResolveMethod();
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
        }

        private static MethodInfo ResolveMethod()
        {
            Type type = FindType(TypeName);
            return type?.GetMethod(
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
