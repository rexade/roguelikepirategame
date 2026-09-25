using System.Collections.Generic;
using PirateGame.Gameplay.Combat;
using PirateGame.Presentation.Ships;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace PirateGame.Presentation.Combat
{
    // Read-only combat visuals: shots, muzzle flashes, splashes, the brace ring
    // and sinking hulls. It observes CombatWorld state and never applies effects.
    public sealed class CombatPresenter : MonoBehaviour
    {
        public Material friendlyShot, hostileShot, flash, smoke, splash, braceRing;
        [Min(0.05f)] public float shotScale = 0.42f;
        public CombatWorld World { get; private set; }

        private sealed class ShotView { public GameObject Root; public TrailRenderer Trail; public bool WasActive; public long Generation; public Vector3 Last; public float Remaining; }
        private sealed class Puff { public Transform Root; public float Age, Duration, From, To; public Vector3 Drift; }
        private sealed class Sinking { public CombatTarget Target; public Transform Model; public Vector3 Start; public Quaternion StartRotation; public float Age; public bool Done; }

        private readonly Dictionary<SweptProjectile, ShotView> shots = new Dictionary<SweptProjectile, ShotView>();
        private readonly List<Puff> puffs = new List<Puff>();
        private readonly Stack<Transform> pool = new Stack<Transform>();
        private readonly List<Sinking> sinking = new List<Sinking>();
        private readonly HashSet<CombatTarget> tracked = new HashSet<CombatTarget>();
        private LineRenderer ring;

        public void Bind(CombatWorld world)
        {
            Unbind();
            World = world;
            if (world == null) return;
            world.ShotFired += OnShotFired;
            foreach (var enemy in world.Enemies) Track(enemy.Target, true);
        }

        public void Unbind()
        {
            if (World != null) World.ShotFired -= OnShotFired;
            World = null;
            foreach (var view in shots.Values) if (view.Root != null) Destroy(view.Root);
            shots.Clear();
            foreach (var entry in sinking) if (entry.Model != null && entry.Target != null) Destroy(entry.Model.gameObject);
            sinking.RemoveAll(entry => entry.Target != null || entry.Model == null);
            tracked.Clear();
            if (ring != null) ring.gameObject.SetActive(false);
        }

        private void OnDestroy() => Unbind();

        // Restored defeated hulls vanish at once; fresh defeats animate.
        private void Track(CombatTarget target, bool initial)
        {
            if (target == null || !tracked.Add(target)) return;
            if (!initial || !target.Defeated) return;
            SetRenderers(target.transform, false);
            StopWake(target);
            sinking.Add(new Sinking { Target = target, Done = true });
        }

        private void OnShotFired(SweptProjectile shot)
        {
            var owner = FindOwner(shot.Owner);
            var origin = owner != null && owner.motor != null && owner.motor.weaponOrigin != null ? owner.motor.weaponOrigin.position : shot.Position;
            Spawn(origin, flash, 0.35f, 1.3f, 0.09f, Vector3.zero);
            Spawn(origin + shot.Direction * 0.6f, smoke, 0.5f, 1.8f, 0.55f, Vector3.up * 0.8f + shot.Direction * 0.4f);
        }

        private CombatTarget FindOwner(string key)
        {
            if (World == null) return null;
            if (World.Player != null && World.Player.Key == key) return World.Player;
            foreach (var enemy in World.Enemies) if (enemy.Target.Key == key) return enemy.Target;
            return null;
        }

        private void LateUpdate()
        {
            if (World == null) { UpdateSinking(); UpdatePuffs(); return; }
            foreach (var shot in World.Projectiles)
            {
                if (!shots.TryGetValue(shot, out var view)) { view = CreateShot(shot); shots.Add(shot, view); }
                bool relaunched = shot.Generation != view.Generation;
                if (view.WasActive && (!shot.Active || relaunched))
                {
                    // Retired: out of range means a splash on open water; otherwise it struck something.
                    if (view.Remaining <= 0.05f || (!relaunched && shot.Remaining <= 0.05f))
                        Spawn(Surface(view.Last), splash, 0.4f, 2.4f, 0.45f, Vector3.up * 0.6f);
                    else
                    {
                        Spawn(view.Last, flash, 0.5f, 2.2f, 0.16f, Vector3.zero);
                        Spawn(view.Last, smoke, 0.6f, 2.6f, 0.8f, Vector3.up * 1.2f);
                    }
                }
                if (shot.Active && (!view.WasActive || relaunched))
                {
                    view.Root.transform.position = shot.Position;
                    view.Trail.Clear();
                    view.Root.GetComponent<Renderer>().sharedMaterial = shot.Team == 0 ? friendlyShot : hostileShot;
                    view.Trail.sharedMaterial = view.Root.GetComponent<Renderer>().sharedMaterial;
                }
                view.Root.SetActive(shot.Active);
                if (shot.Active)
                {
                    view.Root.transform.position = shot.Position;
                    view.Last = shot.Position; view.Remaining = shot.Remaining;
                }
                view.WasActive = shot.Active; view.Generation = shot.Generation;
            }
            UpdateRing();
            foreach (var enemy in World.Enemies)
            {
                Track(enemy.Target, false);
                if (enemy.Target.Defeated && !IsSinking(enemy.Target)) BeginSinking(enemy.Target);
            }
            UpdateSinking();
            UpdatePuffs();
        }

        private static Vector3 Surface(Vector3 p) => new Vector3(p.x, 0.05f, p.z);

        private ShotView CreateShot(SweptProjectile shot)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.name = "Shot";
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * shotScale;
            var material = shot.Team == 0 ? friendlyShot : hostileShot;
            go.GetComponent<Renderer>().sharedMaterial = material;
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var trail = go.AddComponent<TrailRenderer>();
            trail.sharedMaterial = material; trail.time = 0.16f; trail.minVertexDistance = 0.2f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0, shotScale * 0.8f), new Keyframe(1, 0));
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.SetActive(false);
            return new ShotView { Root = go, Trail = trail };
        }

        private void UpdateRing()
        {
            bool braced = World.BraceRemaining > 0 && World.Player != null;
            if (ring == null && braced)
            {
                ring = new GameObject("Brace ring").AddComponent<LineRenderer>();
                ring.transform.SetParent(transform, false);
                ring.sharedMaterial = braceRing; ring.useWorldSpace = false; ring.loop = true; ring.widthMultiplier = 0.16f;
                ring.positionCount = 56; ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                for (int i = 0; i < 56; i++) { float a = i * Mathf.PI * 2 / 56; ring.SetPosition(i, new Vector3(Mathf.Cos(a) * 3.2f, 0.5f, Mathf.Sin(a) * 3.2f)); }
            }
            if (ring == null) return;
            ring.gameObject.SetActive(braced);
            if (braced)
            {
                ring.transform.position = World.Player.transform.position;
                ring.transform.rotation = Quaternion.Euler(0, Time.time * 40, 0);
            }
        }

        private bool IsSinking(CombatTarget target)
        {
            foreach (var entry in sinking) if (entry.Target == target) return true;
            return false;
        }

        private void BeginSinking(CombatTarget target)
        {
            // Animate a visual copy; the (disabled) gameplay hull is untouched.
            var model = ModelOf(target.transform);
            if (model == null) { sinking.Add(new Sinking { Target = target, Done = true }); return; }
            var copy = Instantiate(model.gameObject, model.position, model.rotation, transform);
            foreach (var bob in copy.GetComponentsInChildren<ShipBobbing>()) Destroy(bob);
            // A sinking copy must not cut holes in the water surface.
            foreach (var excluder in copy.GetComponentsInChildren<WaterExcluder>(true)) Destroy(excluder.gameObject);
            foreach (var behaviour in copy.GetComponentsInChildren<MonoBehaviour>()) behaviour.enabled = false;
            SetRenderers(target.transform, false);
            StopWake(target);
            sinking.Add(new Sinking { Target = target, Model = copy.transform, Start = copy.transform.position, StartRotation = copy.transform.rotation });
            Spawn(model.position + Vector3.up, flash, 1.2f, 4.5f, 0.25f, Vector3.zero);
            Spawn(model.position + Vector3.up, smoke, 1.5f, 5f, 1.6f, Vector3.up * 1.5f);
        }

        // Presentation-only wreck of the player's hull at the place it went down.
        public void SinkCopy(Transform ship, Vector3 at)
        {
            var model = ModelOf(ship);
            if (model == null) return;
            var copy = Instantiate(model.gameObject, at + (model.position - ship.position), model.rotation, transform);
            foreach (var bob in copy.GetComponentsInChildren<ShipBobbing>()) Destroy(bob);
            foreach (var excluder in copy.GetComponentsInChildren<WaterExcluder>(true)) Destroy(excluder.gameObject);
            foreach (var behaviour in copy.GetComponentsInChildren<MonoBehaviour>()) behaviour.enabled = false;
            foreach (var renderer in copy.GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;
            sinking.Add(new Sinking { Model = copy.transform, Start = copy.transform.position, StartRotation = copy.transform.rotation });
            Spawn(at + Vector3.up, flash, 1.2f, 4.5f, 0.25f, Vector3.zero);
            Spawn(at + Vector3.up, smoke, 1.5f, 5f, 1.6f, Vector3.up * 1.5f);
        }

        private void UpdateSinking()
        {
            foreach (var entry in sinking)
            {
                if (entry.Done || entry.Model == null) continue;
                entry.Age += Time.deltaTime;
                float t = Mathf.Clamp01(entry.Age / 3f);
                entry.Model.position = entry.Start + Vector3.down * (t * t * 3.2f);
                entry.Model.rotation = entry.StartRotation * Quaternion.Euler(t * 18, 0, t * 38);
                if (t >= 1) { entry.Done = true; entry.Model.gameObject.SetActive(false); }
            }
        }

        private static Transform ModelOf(Transform ship)
        {
            var bob = ship.GetComponentInChildren<ShipBobbing>(true);
            if (bob != null) return bob.transform;
            foreach (Transform child in ship) if (child.GetComponentInChildren<Renderer>() != null) return child;
            return null;
        }

        private static void StopWake(CombatTarget target)
        {
            foreach (var wake in target.GetComponentsInChildren<ShipWake>(true))
            {
                wake.enabled = false;
                if (wake.wake != null) wake.wake.surfaceFoamDimmer = 0;
            }
        }

        private static void SetRenderers(Transform root, bool value)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.enabled = value;
        }

        private void Spawn(Vector3 position, Material material, float from, float to, float duration, Vector3 drift)
        {
            if (material == null) return;
            Transform puff = pool.Count > 0 ? pool.Pop() : null;
            if (puff == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(go.GetComponent<Collider>());
                go.name = "Effect";
                go.transform.SetParent(transform, false);
                go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                puff = go.transform;
            }
            puff.gameObject.SetActive(true);
            puff.position = position;
            puff.localScale = Vector3.one * from;
            puff.GetComponent<Renderer>().sharedMaterial = material;
            puffs.Add(new Puff { Root = puff, Duration = duration, From = from, To = to, Drift = drift });
        }

        private void UpdatePuffs()
        {
            for (int i = puffs.Count - 1; i >= 0; i--)
            {
                var puff = puffs[i];
                puff.Age += Time.deltaTime;
                float t = Mathf.Clamp01(puff.Age / puff.Duration);
                // Grow quickly, then collapse: reads as a burst without transparency.
                float size = Mathf.Lerp(puff.From, puff.To, Mathf.Sin(t * Mathf.PI * 0.5f)) * (1 - t * t);
                puff.Root.localScale = new Vector3(size, size * 0.8f, size);
                puff.Root.position += puff.Drift * Time.deltaTime;
                if (t >= 1) { puff.Root.gameObject.SetActive(false); pool.Push(puff.Root); puffs.RemoveAt(i); }
            }
        }
    }
}
