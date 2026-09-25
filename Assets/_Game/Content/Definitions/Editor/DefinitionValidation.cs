using System;
using System.Collections.Generic;
using System.Linq;
using PirateGame.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PirateGame.Content.Definitions.Editor
{
    [InitializeOnLoad]
    public sealed class DefinitionValidation : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        static DefinitionValidation()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change != PlayModeStateChange.ExitingEditMode) return;
                try { ValidateAll(); }
                catch (Exception e) { Debug.LogError(e.Message); EditorApplication.isPlaying = false; }
            };
        }
        public void OnPreprocessBuild(BuildReport report)
        {
            try { ValidateAll(); }
            catch (Exception e) { throw new BuildFailedException(e.Message); }
        }

        [MenuItem("Pirate Game/Validate Definitions")]
        public static void ValidateAll()
        {
            var identities = new List<AuthoredIdentity>();
            foreach (string guid in AssetDatabase.FindAssets("t:DefinitionCatalogAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<DefinitionCatalogAsset>(path);
                try { asset.Freeze(); }
                catch (Exception e) { throw new ArgumentException(path + ": " + e.Message, e); }
                identities.AddRange(asset.worldIdentities.Select(row => new AuthoredIdentity(row.id, path + ": " + row.origin)));
            }
            var duplicates = identities.GroupBy(i => i.Id).Where(g => g.Count() > 1).ToArray();
            if (duplicates.Length != 0) throw new ArgumentException("Duplicate authored IDs: " + string.Join("; ",
                duplicates.Select(g => g.Key + " at " + string.Join(", ", g.Select(i => i.Origin)))));
        }
    }
}
