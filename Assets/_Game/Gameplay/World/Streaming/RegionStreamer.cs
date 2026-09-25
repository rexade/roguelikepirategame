using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PirateGame.Content.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PirateGame.Gameplay.World.Streaming
{
    public enum RegionState { Unloaded, Loading, Loaded, Failed }

    // Loads region art/collision scenes additively ahead of the ship and unloads
    // them on request. It never decides persistence: composition captures a
    // region's entities at an idle boundary before calling Unload.
    public sealed class RegionStreamer : MonoBehaviour
    {
        public FirstRegionAsset[] regions = Array.Empty<FirstRegionAsset>();
        [Min(1)] public float loadMargin = 70;
        [Min(1)] public float unloadMargin = 110;
        // Test hook: regions listed here never become ready (readiness-failure checks).
        public readonly HashSet<string> BlockedRegions = new HashSet<string>();

        public event Action<FirstRegionAsset> RegionReady;
        public IReadOnlyList<string> LoadLog => loadLog;

        private readonly Dictionary<string, RegionState> states = new Dictionary<string, RegionState>();
        private readonly Dictionary<string, Scene> scenes = new Dictionary<string, Scene>();
        private readonly List<string> loadLog = new List<string>();

        public RegionState StateOf(string regionId) => states.TryGetValue(regionId ?? "", out var state) ? state : RegionState.Unloaded;
        public bool IsReady(string regionId) => StateOf(regionId) == RegionState.Loaded;
        public FirstRegionAsset Region(string regionId) => regions.FirstOrDefault(r => r.regionId == regionId);

        // The owning region of a sea position: the one containing it, else the nearest.
        public FirstRegionAsset RegionAt(Vector3 position)
        {
            var xz = new Vector2(position.x, position.z);
            FirstRegionAsset best = null; float distance = float.MaxValue;
            foreach (var region in regions)
            {
                float d = region.DistanceTo(xz);
                if (d < distance) { best = region; distance = d; }
            }
            return best;
        }

        public string RegionIdAt(Vector3 position) => RegionAt(position)?.regionId;

        public IEnumerable<FirstRegionAsset> Wanted(Vector3 position)
        {
            var xz = new Vector2(position.x, position.z);
            var home = RegionAt(position);
            return regions.Where(r => r == home || r.DistanceTo(xz) <= loadMargin);
        }

        public IEnumerable<FirstRegionAsset> Unwanted(Vector3 position)
        {
            var xz = new Vector2(position.x, position.z);
            var home = RegionAt(position);
            return regions.Where(r => r != home && StateOf(r.regionId) == RegionState.Loaded && r.DistanceTo(xz) > unloadMargin);
        }

        public bool AllReady(Vector3 position) => Wanted(position).All(r => IsReady(r.regionId));

        // Starts loading every region needed around the position; returns true when all are ready.
        public bool Require(Vector3 position)
        {
            foreach (var region in Wanted(position)) Load(region);
            return AllReady(position);
        }

        public void Load(FirstRegionAsset region)
        {
            var state = StateOf(region.regionId);
            if (state == RegionState.Loading || state == RegionState.Loaded) return;
            if (string.IsNullOrEmpty(region.sceneName)) { MarkReady(region, default, 0); return; }
            if (BlockedRegions.Contains(region.regionId)) { states[region.regionId] = RegionState.Failed; return; }
            var existing = SceneManager.GetSceneByName(region.sceneName);
            if (existing.IsValid() && existing.isLoaded) { MarkReady(region, existing, 0); return; }
            states[region.regionId] = RegionState.Loading;
            var watch = Stopwatch.StartNew();
            var operation = SceneManager.LoadSceneAsync(region.sceneName, LoadSceneMode.Additive);
            if (operation == null) { states[region.regionId] = RegionState.Failed; Log(region, "failed to start", watch); return; }
            operation.completed += _ =>
            {
                var scene = SceneManager.GetSceneByName(region.sceneName);
                if (!scene.IsValid() || !scene.isLoaded) { states[region.regionId] = RegionState.Failed; Log(region, "failed", watch); return; }
                MarkReady(region, scene, watch.Elapsed.TotalMilliseconds);
            };
        }

        public void Unload(string regionId)
        {
            if (StateOf(regionId) != RegionState.Loaded) return;
            states[regionId] = RegionState.Unloaded;
            if (scenes.TryGetValue(regionId, out var scene) && scene.IsValid() && scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
            scenes.Remove(regionId);
            loadLog.Add(regionId + " unloaded");
        }

        // Clears failures so a later Require retries (used by the retry path).
        public void Retry(string regionId)
        {
            if (StateOf(regionId) == RegionState.Failed) states[regionId] = RegionState.Unloaded;
        }

        private void MarkReady(FirstRegionAsset region, Scene scene, double milliseconds)
        {
            states[region.regionId] = RegionState.Loaded;
            if (scene.IsValid()) scenes[region.regionId] = scene;
            string line = region.regionId + " ready in " + milliseconds.ToString("0.0") + " ms";
            loadLog.Add(line);
            UnityEngine.Debug.Log("[regions] " + line);
            RegionReady?.Invoke(region);
        }

        private void Log(FirstRegionAsset region, string what, Stopwatch watch)
        {
            string line = region.regionId + " " + what + " after " + watch.Elapsed.TotalMilliseconds.ToString("0.0") + " ms";
            loadLog.Add(line);
            UnityEngine.Debug.LogWarning("[regions] " + line);
        }
    }
}
