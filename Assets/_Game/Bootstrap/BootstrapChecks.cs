using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PirateGame.Bootstrap
{
    public static class BootstrapChecks
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CheckPlayerDependencies()
        {
            if (!Debug.isDebugBuild) return;
            var original = new Probe { label = "harbor", count = 7 };
            var restored = JsonConvert.DeserializeObject<Probe>(JsonConvert.SerializeObject(original));
            if (restored == null || restored.label != original.label || restored.count != original.count)
                throw new InvalidOperationException("T01 player JSON round-trip failed.");
            Debug.Log($"T01 player JSON round-trip passed. Unity={Application.unityVersion}; " +
                $"GPU={SystemInfo.graphicsDeviceName}; API={SystemInfo.graphicsDeviceType}; " +
                $"resolution={Screen.width}x{Screen.height}; scene={SceneManager.GetActiveScene().name}");
        }

        [Serializable]
        private sealed class Probe
        {
            public string label;
            public int count;
        }
    }
}
