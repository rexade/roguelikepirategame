using System.Collections.Generic;
using PirateGame.Gameplay.Combat;
using UnityEngine;

namespace PirateGame.Tests.T05
{
    // Fixture-only presentation; it reads combat state and never applies effects.
    public sealed class CombatView : MonoBehaviour
    {
        public CombatWorld world;
        private readonly Dictionary<SweptProjectile, GameObject> visuals = new Dictionary<SweptProjectile, GameObject>();
        private Material friendly, hostile, shieldMaterial;
        private GameObject shield;
        private static Material Material(Color color)
        {
            var m = new Material(Shader.Find("HDRP/Unlit"));
            m.SetColor("_UnlitColor", color);
            return m;
        }
        private void Start()
        {
            friendly = Material(new Color(1, 0.9f, 0.3f));
            hostile = Material(new Color(1, 0.18f, 0.12f));
            shieldMaterial = Material(new Color(0.1f, 1, 0.8f));
            shield = new GameObject("Brace ring");
            var line = shield.AddComponent<LineRenderer>();
            line.sharedMaterial = shieldMaterial; line.useWorldSpace = false; line.loop = true; line.widthMultiplier = 0.12f;
            line.positionCount = 48;
            for (int i = 0; i < 48; i++) { float a = i * Mathf.PI * 2 / 48; line.SetPosition(i, new Vector3(Mathf.Cos(a) * 3, 0.4f, Mathf.Sin(a) * 3)); }
        }
        private void LateUpdate()
        {
            var stale = new List<SweptProjectile>();
            foreach (var pair in visuals)
            {
                bool retained = false;
                foreach (var shot in world.Projectiles) if (ReferenceEquals(shot, pair.Key)) { retained = true; break; }
                if (!retained) { Destroy(pair.Value); stale.Add(pair.Key); }
            }
            foreach (var shot in stale) visuals.Remove(shot);
            foreach (var old in visuals.Values) old.SetActive(false);
            foreach (var shot in world.Projectiles)
            {
                if (!visuals.TryGetValue(shot, out var visual))
                {
                    visual = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(visual.GetComponent<Collider>());
                    visual.name = "Combat shot"; visuals.Add(shot, visual);
                }
                visual.SetActive(shot.Active);
                if (!shot.Active) continue;
                visual.transform.position = shot.Position;
                visual.transform.rotation = Quaternion.LookRotation(shot.Direction);
                visual.transform.localScale = new Vector3(0.32f, 0.32f, 1.1f);
                visual.GetComponent<Renderer>().sharedMaterial = shot.Team == 0 ? friendly : hostile;
            }
            shield.SetActive(world.BraceRemaining > 0);
            shield.transform.position = world.Simulation.motor.Body.position;
            foreach (var enemy in world.Enemies)
                foreach (var renderer in enemy.Target.GetComponentsInChildren<Renderer>()) renderer.enabled = !enemy.Target.Defeated;
        }
        private void OnGUI()
        {
            if (world == null) return;
            var session = world.Simulation.Session;
            float hudX = Screen.width - 295;
            GUI.Box(new Rect(hudX, 20, 275, 94), "");
            GUI.Label(new Rect(hudX + 12, 27, 250, 22), "HULL  " + world.Player.Health.ToString("0") + (world.Player.Defeated ? "  DEFEATED" : ""));
            GUI.Label(new Rect(hudX + 12, 51, 250, 22), world.Weapon.Id.ToUpperInvariant() + "  " + (world.WeaponCooldown > 0 ? world.WeaponCooldown.ToString("0.0") : "READY"));
            GUI.Label(new Rect(hudX + 12, 75, 250, 22), "BRACE  " + (world.BraceRemaining > 0 ? "ACTIVE" : world.AbilityCooldown > 0 ? world.AbilityCooldown.ToString("0.0") : "READY"));
            foreach (var enemy in world.Enemies)
            {
                if (enemy.Target.Defeated) continue;
                var p = Camera.main.WorldToScreenPoint(enemy.Target.transform.position + Vector3.up * 3);
                if (p.z <= 0) continue;
                var rect = new Rect(p.x - 45, Screen.height - p.y, 90, 7);
                GUI.color = Color.black; GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = new Color(1, 0.35f, 0.25f); rect.width *= (float)(enemy.Target.Health / enemy.Target.MaximumHealth);
                GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = Color.white;
            }
            if (session.IsPaused) GUI.Label(new Rect(Screen.width / 2 - 50, 25, 180, 30), session.Snapshot.Expedition == null ? "EXPEDITION ENDED" : "PAUSED");
        }
        private void OnDestroy()
        {
            foreach (var visual in visuals.Values) if (visual != null) Destroy(visual);
            Destroy(shield); Destroy(friendly); Destroy(hostile); Destroy(shieldMaterial);
        }
    }
}
