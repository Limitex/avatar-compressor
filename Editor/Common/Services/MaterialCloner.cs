using System.Collections.Generic;
using nadena.dev.ndmf;
using UnityEngine;

namespace dev.limitex.avatar.compressor.common
{
    /// <summary>
    /// Service for cloning materials to avoid modifying original assets.
    /// </summary>
    public static class MaterialCloner
    {
        /// <summary>
        /// Clones all materials in the hierarchy to allow safe modification.
        /// </summary>
        /// <param name="root">Root GameObject of the hierarchy</param>
        /// <returns>Dictionary mapping original materials to cloned materials</returns>
        public static Dictionary<Material, Material> CloneMaterials(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var clonedMaterials = new Dictionary<Material, Material>();

            foreach (var renderer in renderers)
            {
                var originalMaterials = renderer.sharedMaterials;
                var newMaterials = new Material[originalMaterials.Length];

                for (int i = 0; i < originalMaterials.Length; i++)
                {
                    var originalMat = originalMaterials[i];
                    if (originalMat == null)
                    {
                        newMaterials[i] = null;
                        continue;
                    }

                    if (!clonedMaterials.TryGetValue(originalMat, out var clonedMat))
                    {
                        clonedMat = Object.Instantiate(originalMat);
                        clonedMat.name = originalMat.name + "_clone";
                        // Register the material replacement in ObjectRegistry so that subsequent NDMF plugins
                        // can track which original material was cloned. This maintains proper reference
                        // tracking across the build pipeline for tools like TexTransTool and Avatar Optimizer.
                        ObjectRegistry.RegisterReplacedObject(originalMat, clonedMat);
                        clonedMaterials[originalMat] = clonedMat;
                    }

                    newMaterials[i] = clonedMat;
                }

                renderer.sharedMaterials = newMaterials;
            }

            return clonedMaterials;
        }
    }
}
