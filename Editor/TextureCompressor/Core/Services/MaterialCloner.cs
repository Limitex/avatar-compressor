using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Service for cloning materials and updating references.
    /// </summary>
    public static class MaterialCloner
    {
        /// <summary>
        /// Clones materials from the given references and updates Renderer references.
        /// </summary>
        /// <param name="references">Material references to process</param>
        /// <returns>Dictionary mapping original materials to cloned materials</returns>
        public static Dictionary<Material, Material> CloneAndReplace(
            IEnumerable<MaterialReference> references
        )
        {
            var clonedMaterials = new Dictionary<Material, Material>();
            var referenceList = references.ToList();

            // First pass: clone all unique materials
            foreach (var reference in referenceList)
            {
                if (reference?.Material == null)
                    continue;
                GetOrCloneMaterial(reference.Material, clonedMaterials);
            }

            // Second pass: update Renderer references
            UpdateRendererReferences(referenceList, clonedMaterials);

            return clonedMaterials;
        }

        private static void UpdateRendererReferences(
            IEnumerable<MaterialReference> references,
            Dictionary<Material, Material> clonedMaterials
        )
        {
            // Group references by Renderer for efficient batch updates
            var rendererGroups = references
                .Where(r =>
                    r != null
                    && r.SourceType == MaterialSourceType.Renderer
                    && r.SourceObject is Renderer
                )
                .GroupBy(r => (Renderer)r.SourceObject);

            foreach (var group in rendererGroups)
            {
                var renderer = group.Key;
                if (renderer == null)
                    continue;

                var originalMaterials = renderer.sharedMaterials;
                var newMaterials = new Material[originalMaterials.Length];
                bool hasChanges = false;

                for (int i = 0; i < originalMaterials.Length; i++)
                {
                    var originalMat = originalMaterials[i];
                    if (
                        originalMat != null
                        && clonedMaterials.TryGetValue(originalMat, out var clonedMat)
                    )
                    {
                        newMaterials[i] = clonedMat;
                        hasChanges = true;
                    }
                    else
                    {
                        newMaterials[i] = originalMat;
                    }
                }

                if (hasChanges)
                {
                    renderer.sharedMaterials = newMaterials;
                }
            }
        }

        private static Material GetOrCloneMaterial(
            Material originalMat,
            Dictionary<Material, Material> clonedMaterials
        )
        {
            if (clonedMaterials.TryGetValue(originalMat, out var clonedMat))
            {
                return clonedMat;
            }

            clonedMat = Object.Instantiate(originalMat);
            clonedMat.name = originalMat.name + "_clone";

            // Register the material replacement in ObjectRegistry so that subsequent NDMF plugins
            // can track which original material was cloned. This maintains proper reference
            // tracking across the build pipeline for tools like TexTransTool and Avatar Optimizer.
            ObjectRegistry.RegisterReplacedObject(originalMat, clonedMat);
            clonedMaterials[originalMat] = clonedMat;

            return clonedMat;
        }
    }
}
