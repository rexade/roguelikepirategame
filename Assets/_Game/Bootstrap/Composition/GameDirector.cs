using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PirateGame.Content.Combat;
using PirateGame.Content.Definitions;
using PirateGame.Content.World;
using PirateGame.Core;
using PirateGame.Gameplay.AI;
using PirateGame.Gameplay.Combat;
using PirateGame.Gameplay.Input;
using PirateGame.Gameplay.Ships;
using PirateGame.Gameplay.World;
using PirateGame.Gameplay.World.Streaming;
using PirateGame.Persistence;
using PirateGame.Presentation.Audio;
using PirateGame.Presentation.Cameras;
using PirateGame.Presentation.Combat;
using PirateGame.Presentation.Weather;
using PirateGame.Rules.Application;
using PirateGame.UI.Game;
using PirateGame.UI.Harbor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using EntityId = PirateGame.Core.EntityId;

namespace PirateGame.Composition
{
    // Production composition root for the ocean world. It owns the only
    // CampaignSession, the durable store and the world arrival port; every other
    // component receives these references from here. Persistent changes only go
    // through session commands, and at-sea saves are taken at idle tick boundaries
    // with a fresh combat capture so every snapshot describes one simulation instant.
    public sealed class GameDirector : MonoBehaviour, IWorldArrival
    {
        [Header("Content")]
        public DefinitionCatalogAsset rules;
        public CombatCatalogAsset combatContent;
        public FirstRegionAsset region;
        // Every region of the archipelago (T10); the first is the home region.
        public FirstRegionAsset[] regions = Array.Empty<FirstRegionAsset>();
        public string homeHub = "home-harbor";
        public string startingHull = "cutter";
        public string regionTitle = "Homeward Reach";

        [Header("Player")]
        public ShipSimulation player;
        public ShipKeyboardMouse playerInput;
        public CombatTarget playerTarget;
        public SalvageInteraction interaction;

        [Header("World")]
        public SalvageRegion salvage;
        public SalvageRegion[] salvageRegions = Array.Empty<SalvageRegion>();
        public RegionStreamer streamer;
        public GameObject raiderPrefab, gunnerPrefab, corsairPrefab;
        public ShipFollowCamera followCamera;
        public Camera worldCamera;
        public CombatPresenter presenter;
        public AimMarker aimMarker;
        public GameAudio sound;
        public SeaConditions weather;

        [Header("UI")]
        public HarborView harbor;
        public GameHud hud;
        public GameMenus menus;
        public SeaChart chart;

        [Header("Voyage")]
        [Min(1)] public float checkpointSeconds = 10;

        public CampaignSession Session { get; private set; }
        public JsonSaveStore Store { get; private set; }
        public DefinitionCatalog Definitions { get; private set; }
        public CombatCatalog Combat { get; private set; }
        public CombatWorld CombatWorld { get; private set; }
        public IReadOnlyList<EnemyShip> Enemies => enemies;
        public ExpeditionPlanner Planner { get; private set; }
        public string LastNotice { get; private set; } = "";
        // A session exists and its saved location has fully arrived (regions loaded).
        public bool Ready => Session != null && !Session.ArrivalPending;
        public bool Holding => holding;
        // Diagnostics for automated runs: docking target, pending tick and last tick result.
        public string DockingState => (dockTarget ?? "-") + " req@" + dockRequestedTick + " tick " + (Session?.Tick ?? 0) +
            " last " + (player.LastTickResult?.Error.ToString() ?? "-") + " speed " + (Session?.Snapshot.Expedition?.Speed ?? 0).ToString("0.00");
        public bool MenuPaused => pausedByMenu;
        public bool CardOpen => resultsOpen && menus.IsOpen;
        public string CardTitle => menus.CurrentTitle;

        // Automation/test entry point equivalent to pressing Interact at sea.
        public void RequestInteract() => interactRequested = true;
        public int CheckpointCount { get; private set; }

        private readonly List<EnemyShip> enemies = new List<EnemyShip>();
        private readonly List<EnemyShip> pendingWrecks = new List<EnemyShip>();
        private readonly HudModel model = new HudModel();
        private readonly List<HudMarker> bars = new List<HudMarker>();
        private GameObject expeditionRoot;
        private bool interactRequested, pausedByMenu, resultsOpen, errorOpen, started;
        private string dockTarget;
        private long dockRequestedTick = -1;
        private long lastCheckpointTick;
        private IReadOnlyDictionary<string, int> voyageCargo = new Dictionary<string, int>();
        private Vector3 lastSeaPosition;
        private readonly HashSet<string> sighted = new HashSet<string>();
        private bool braced;
        public SeaCondition Weather => weather != null ? weather.Current : SeaCondition.Daylight;

        public IReadOnlyList<FirstRegionAsset> Regions => regions.Length > 0 ? regions : new[] { region };
        private IEnumerable<SalvageRegion> SalvageRegions => salvageRegions.Length > 0 ? salvageRegions : new[] { salvage };
        public string HubName(string id) => Regions.Select(r => r.HubName(id)).FirstOrDefault(n => n != id) ?? id;
        private float DockYaw(string hubId) => (Regions.FirstOrDefault(r => r.Hubs.Any(h => h.Id == hubId)) ?? region).DockYaw(hubId);
        private bool Loaded(string regionId) => streamer == null || streamer.IsReady(regionId);
        public string RegionIdAt(Vector3 position) => streamer != null ? streamer.RegionIdAt(position) : region.regionId;
        private FirstRegionAsset RegionAt(Vector3 position) => streamer != null ? streamer.RegionAt(position) : region;
        private GameObject PrefabFor(string enemy) =>
            enemy == "gunner" ? gunnerPrefab : enemy == "corsair" && corsairPrefab != null ? corsairPrefab : raiderPrefab;

        private bool arrivalWaiting, holding, fogWarned, chartPaused, focusLost;
        public bool ChartOpen => chart != null && chart.IsOpen;
        private Vector3 arrivalAt;
        private readonly Queue<string> regionsToPopulate = new Queue<string>();
        private readonly HashSet<string> populated = new HashSet<string>();
        private readonly Dictionary<EnemyShip, string> enemyRegion = new Dictionary<EnemyShip, string>();
        private string CurrentHarbor => HubName(Session.Snapshot.Campaign.CurrentHub);

        private static readonly Dictionary<string, string> StartingEquipment = new Dictionary<string, string>
        { ["cannon-1"] = "cannon", ["repeater-1"] = "repeater", ["brace-1"] = "brace" };
        private static readonly Dictionary<string, string> StartingLoadout = new Dictionary<string, string>
        { ["weapon"] = "cannon-1", ["ability"] = "brace-1" };

        private EntityId ContextId(Guid expedition) => EntityId.Generated(expedition, "combat-context");

        private void Start()
        {
            var rulesAsset = rules; var combatAsset = combatContent;
            if (GameBenchmark.Active)
            {
                // Benchmark voyages: a sturdier hull so every lap completes the route; the
                // stress run also fires much faster. Runtime copies only; assets are untouched.
                rulesAsset = Instantiate(rules);
                bool stress = GameBenchmark.Stress;
                foreach (var stat in rulesAsset.hulls.SelectMany(h => h.stats).Where(s => s.id == "health"))
                { stat.value = stress ? 500000 : 2000; stat.maximum = stress ? 1000000 : 10000; }
                combatAsset = Instantiate(combatContent);
                combatAsset.rules = rulesAsset;
                if (stress) foreach (var row in combatAsset.enemies) row.reloadScale *= 0.03f;
            }
            Definitions = rulesAsset.Freeze();
            Combat = combatAsset.Freeze();
            foreach (var r in Regions) r.Validate(Definitions);
            FirstRegionAsset.ValidateIdentities(Regions.SelectMany(r => r.Identities(r.name)));
            Planner = new ExpeditionPlanner(Regions, Combat);
            if (GameBenchmark.Stress) { Planner.ShipMultiplier = 4; Planner.ExtraBarrels = 100; }
            player.RegionOf = RegionIdAt;
            if (streamer != null) streamer.RegionReady += r => regionsToPopulate.Enqueue(r.regionId);
            // At-sea checkpoints and pickups are written off the main thread; transitions stay synchronous.
            Store = new JsonSaveStore(LaunchOptions.SaveDirectory, Definitions, backgroundWrites: true);
            interaction.collectOnInteractIntent = false;
            player.TickStarted += OnTickStarted;
            if (sound != null)
            {
                presenter.ShotRetired += (at, struck) => sound.Play(struck ? Sfx.Impact : Sfx.Splash, at, struck ? 0.9f : 0.55f);
                presenter.HullSank += at => sound.Play(Sfx.Sink, at, 0.9f);
            }
            hud.SetVisible(false);
            harbor.gameObject.SetActive(true);
            started = true;
            Launch(LaunchOptions.Mode);
        }

        private void OnDestroy()
        {
            if (player != null) player.TickStarted -= OnTickStarted;
            // Leaving the scene (e.g. exit to title) finishes any queued save first.
            Store?.Flush();
        }

        // ---------------------------------------------------------------- launch

        public void Launch(LaunchMode mode)
        {
            bool resume = mode == LaunchMode.Continue || (mode == LaunchMode.Auto && Store.HasSaveFiles);
            if (!resume) { NewCampaign(); return; }
            var load = Store.Load();
            if (load.HasCampaign)
            {
                Open(load.Snapshot);
                if (load.Status == SaveLoadStatus.RecoveredFromBackup)
                    ShowCard("Save recovered", "The latest save could not be read, so the previous one was restored.",
                        Lines(load.Detail, load.PreservedFiles.Count > 0 ? "Unreadable file kept at " + load.PreservedFiles[0] : null),
                        new MenuAction("Continue", "continue", CloseCard, true));
                return;
            }
            if (load.Status == SaveLoadStatus.NoSave) { NewCampaign(); return; }
            errorOpen = true;
            menus.Show("Save cannot be read", "Your existing save files were left untouched.", Lines(load.Detail),
                new MenuAction("Start a new campaign (keeps old files)", "new-campaign", () => { errorOpen = false; menus.Hide(); NewCampaign(); }),
                new MenuAction("Exit to title", "exit", ExitToTitle));
        }

        private void NewCampaign()
        {
            var snapshot = CampaignSession.NewCampaign(Definitions, homeHub, startingHull, StartingEquipment, StartingLoadout);
            var created = Store.Initialize(snapshot, out var preserved);
            if (!created.IsSuccess)
            {
                errorOpen = true;
                menus.Show("Could not create a save", "Check that the save folder is writable.", Lines(created.Detail, Store.Directory),
                    new MenuAction("Try again", "retry", () => { errorOpen = false; menus.Hide(); NewCampaign(); }, true),
                    new MenuAction("Exit to title", "exit", ExitToTitle));
                return;
            }
            Open(snapshot);
            ShowCard(HubName(homeHub), "A modest cutter, an empty storehouse, and a sea full of other people's cargo.", Lines(
                "Sail out, salvage barrels and wrecks, and sink raiders for their plunder.",
                "Cargo is only yours once you dock and bank it. If your ship goes down, the hold is lost; your bank, upgrades and equipment are kept.",
                "W/S sail  ·  A/D steer  ·  Space brake  ·  Mouse aim  ·  LMB fire  ·  RMB brace  ·  E salvage / dock  ·  M sea chart  ·  Esc pause",
                preserved.Count > 0 ? "Your previous save was kept as " + System.IO.Path.GetFileName(preserved[0]) + "." : null),
                new MenuAction("To the harbor", "continue", CloseCard, true));
        }

        private void Open(SessionSnapshot snapshot)
        {
            Session = new CampaignSession(Definitions, snapshot, Store, this);
            player.Bind(Session);
            harbor.Bind(Session, Definitions, PlanEmbark, HubName);
            var arrived = Session.RetryArrival();
            if (!arrived.IsSuccess && !arrivalWaiting) ShowRecovery();
        }

        public EmbarkPlan PlanEmbark()
        {
            int seed = LaunchOptions.Seed ?? new System.Random().Next();
            return Planner.Plan(Guid.NewGuid(), seed);
        }

        // ---------------------------------------------------------------- arrival

        public RuleResult EnsureReady(ArrivalRequest request)
        {
            try
            {
                // Collision and art for the destination must be loaded before the world is restored.
                var destination = new Vector3((float)request.Destination.X, 0, (float)request.Destination.Z);
                if (streamer != null && !streamer.Require(destination))
                {
                    arrivalWaiting = true; arrivalAt = destination;
                    return new RuleResult(RuleError.None, pending: true, detail: "Charting the destination waters.");
                }
                arrivalWaiting = false;
                return request.ExpeditionId == null ? ArriveDocked(request) : ArriveAtSea(request);
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                Debug.LogException(e);
                return new RuleResult(RuleError.ArrivalFailed, detail: e.Message);
            }
        }

        private RuleResult ArriveDocked(ArrivalRequest request)
        {
            if (!Definitions.Hubs.TryGetValue(request.HubId ?? "", out var hub)) return new RuleResult(RuleError.ArrivalFailed, detail: "Unknown harbor.");
            TeardownExpedition();
            Place(hub.Dock, DockYaw(hub.Id), 0);
            ApplyWeather(SeaCondition.Daylight);
            if (followCamera.FocusOverride == null) followCamera.Snap();
            return new RuleResult();
        }

        private RuleResult ArriveAtSea(ArrivalRequest request)
        {
            var voyage = Session.Snapshot.Expedition;
            if (voyage == null || voyage.Id != request.ExpeditionId)
                return new RuleResult(RuleError.ArrivalFailed, detail: "The saved voyage does not match the arrival request.");
            TeardownExpedition();
            // Upgrades bought in harbor change hull stats; rebind at each departure.
            player.Bind(Session);
            expeditionRoot = new GameObject("Voyage " + voyage.Id.ToString("N").Substring(0, 8));
            // Only loaded regions get views and ships; the ledger keeps the rest.
            foreach (var loaded in Regions.Where(r => Loaded(r.regionId)))
            {
                var populatedRegion = Populate(loaded.regionId);
                if (!populatedRegion.IsSuccess) return populatedRegion;
            }
            var world = expeditionRoot.AddComponent<CombatWorld>();
            world.Bind(player, Combat, playerTarget, enemies.Cast<ICombatEnemy>(), ContextId(voyage.Id));
            CombatWorld = world;
            world.ShotFired += OnShotFired;
            ApplyWeather(SeaConditions.ForSeed(voyage.Seed));
            if (voyage.Entities.ContainsKey(ContextId(voyage.Id)))
            {
                var restored = world.Restore(ForLoadedWorld(voyage));
                if (!restored.IsSuccess) return new RuleResult(RuleError.ArrivalFailed, detail: "Combat state: " + restored.Detail);
            }
            else Place(voyage.Position, DockYaw(Session.Snapshot.Campaign.CurrentHub), voyage.Speed);
            presenter.Bind(world);
            lastCheckpointTick = voyage.Tick;
            voyageCargo = voyage.Cargo;
            lastSeaPosition = player.motor.Body.position;
            sighted.Clear();
            followCamera.FocusOverride = null;
            followCamera.Snap();
            return new RuleResult();
        }

        // Builds views and ships for one loaded region from the saved ledger.
        private RuleResult Populate(string regionId)
        {
            var voyage = Session.Snapshot.Expedition;
            if (voyage == null || expeditionRoot == null || !Loaded(regionId) || populated.Contains(regionId)) return new RuleResult();
            var view = SalvageRegions.FirstOrDefault(s => s != null && s.content != null && s.content.regionId == regionId);
            if (view != null)
            {
                var rebuilt = view.Recreate(Session);
                if (!rebuilt.IsSuccess) return new RuleResult(RuleError.ArrivalFailed, detail: rebuilt.Detail);
            }
            foreach (var entity in voyage.Entities.Values.Where(e => Combat.Enemies.ContainsKey(e.DefinitionId) && e.Position.RegionId == regionId)
                         .OrderBy(e => e.Id.ToString(), StringComparer.Ordinal))
            {
                if (enemies.Any(e => e.Id.Equals(entity.Id))) continue;
                var ship = Instantiate(PrefabFor(entity.DefinitionId), new Vector3((float)entity.Position.X, 0, (float)entity.Position.Z),
                    Quaternion.identity, expeditionRoot.transform);
                ship.name = entity.DefinitionId + " " + (entity.Id.SpawnId ?? entity.Id.AuthoredId);
                var enemy = ship.GetComponent<EnemyShip>();
                enemy.Initialize(entity.Id, entity.Position.RegionId, Combat.Enemies[entity.DefinitionId]);
                var restored = enemy.Restore(entity);
                if (!restored.IsSuccess) return new RuleResult(RuleError.ArrivalFailed, detail: "Enemy " + entity.Id + ": " + restored.Detail);
                enemy.Target.Died += OnEnemyDied;
                enemies.Add(enemy);
                enemyRegion[enemy] = regionId;
                if (CombatWorld != null) { CombatWorld.Attach(enemy); presenter.Adopt(enemy.Target); }
            }
            populated.Add(regionId);
            RefreshSources();
            return new RuleResult();
        }

        // Captures a region's ships into the ledger, removes its views, then retires its scene.
        private bool Retire(FirstRegionAsset target)
        {
            var leaving = enemies.Where(e => enemyRegion.TryGetValue(e, out var r) && r == target.regionId).ToList();
            var ship = player.motor.Body.position;
            if (leaving.Any(e => !e.Target.Defeated && (e.transform.position - ship).sqrMagnitude < 60 * 60)) return false;
            if (Session.Snapshot.Expedition != null && CombatWorld != null && (leaving.Count > 0 || pendingWrecks.Count > 0))
            {
                var saved = Checkpoint();
                if (!saved.IsSuccess) return false;
            }
            foreach (var enemy in leaving)
            {
                CombatWorld?.Detach(enemy.Id);
                enemy.Target.Died -= OnEnemyDied;
                enemies.Remove(enemy); enemyRegion.Remove(enemy);
                Destroy(enemy.gameObject);
            }
            SalvageRegions.FirstOrDefault(s => s != null && s.content != null && s.content.regionId == target.regionId)?.Clear();
            populated.Remove(target.regionId);
            RefreshSources();
            streamer.Unload(target.regionId);
            return true;
        }

        private void RefreshSources() =>
            interaction.sources = SalvageRegions.Where(s => s != null).SelectMany(s => s.Sources).Where(s => s != null).ToArray();

        // Shots owned by ships outside the loaded world are dropped on restore (they were
        // retired when their region unloaded); everything else restores exactly.
        private ExpeditionState ForLoadedWorld(ExpeditionState voyage)
        {
            var id = ContextId(voyage.Id);
            if (!voyage.Entities.TryGetValue(id, out var context)) return voyage;
            var payload = JsonUtility.FromJson<CombatContext>(context.BehaviorState);
            if (payload?.shots == null) return voyage;
            var owners = new HashSet<string>(enemies.Select(e => e.Target.Key)) { CombatWorld.PlayerKey };
            var kept = payload.shots.Where(s => s != null && owners.Contains(s.owner ?? "")).ToArray();
            if (kept.Length == payload.shots.Length) return voyage;
            payload.shots = kept;
            var cleaned = new EntityState(context.Id, context.DefinitionId, context.Position, context.Health, context.Defeated,
                context.Loot, context.Cooldowns, JsonUtility.ToJson(payload));
            return new ExpeditionState(voyage.Id, voyage.Seed, voyage.RngState, voyage.Tick, voyage.Health, voyage.Speed, voyage.Position,
                voyage.Cargo, voyage.Modifiers, voyage.Cooldowns, voyage.Encounters, voyage.Entities.Values.Select(e => e.Id.Equals(id) ? cleaned : e));
        }

        private void TeardownExpedition()
        {
            presenter.Unbind();
            aimMarker.Hide();
            foreach (var enemy in enemies) if (enemy != null && enemy.Target != null) enemy.Target.Died -= OnEnemyDied;
            enemies.Clear(); pendingWrecks.Clear(); enemyRegion.Clear(); populated.Clear();
            if (expeditionRoot != null)
            {
                // Disable first so the combat world unsubscribes from the tick this frame.
                expeditionRoot.SetActive(false);
                Destroy(expeditionRoot);
            }
            expeditionRoot = null; CombatWorld = null;
            foreach (var view in SalvageRegions) view?.Clear();
            interaction.sources = Array.Empty<SalvageSource>();
            dockTarget = null; dockRequestedTick = -1; interactRequested = false;
            playerInput.ForceBrake = false;
        }

        private void Place(SeaPosition position, float yaw, double speed)
        {
            player.motor.Suspend(true);
            player.motor.Teleport(new Vector3((float)position.X, 0, (float)position.Z), yaw, (float)speed);
            Physics.SyncTransforms();
        }

        // ---------------------------------------------------------------- frame loop

        private void OnTickStarted(InputIntent intent)
        {
            if (intent.Interact) interactRequested = true;
        }

        // Players keep simulating in the background (Run In Background), so losing
        // window focus pauses the voyage like Esc does. The editor, batch runs and
        // the benchmark/capture automation keep running unfocused.
        private void OnApplicationFocus(bool focused)
        {
            focusLost = !focused && !Application.isEditor && !Application.isBatchMode &&
                !GameBenchmark.Active && LaunchOptions.Argument("-capture") == null;
        }

        private void OnShotFired(SweptProjectile shot)
        {
            if (sound == null) return;
            bool mine = CombatWorld != null && CombatWorld.Player != null && shot.Owner == CombatWorld.Player.Key;
            sound.Play(shot.Weapon.Id == "repeater" ? Sfx.Repeater : Sfx.Cannon, shot.Position, mine ? 0.8f : 0.6f);
        }

        private void ApplyWeather(SeaCondition condition)
        {
            if (weather != null) weather.Apply(condition);
            if (sound != null) sound.SetAmbience(condition == SeaCondition.Rough ? 0.55f : condition == SeaCondition.Dusk ? 0.3f : 0.35f);
        }

        private void OnEnemyDied(CombatTarget target)
        {
            var enemy = enemies.FirstOrDefault(e => e.Target == target);
            if (enemy != null && !pendingWrecks.Contains(enemy)) pendingWrecks.Add(enemy);
        }

        private void Update()
        {
            if (!started || Session == null) return;
            HandleEvents();
            Stream();
            WatchLocks();
            bool atSea = Session.Lifecycle == Lifecycle.AtSea && Session.Snapshot.Expedition != null;
            if (atSea && player.motor.Body != null) lastSeaPosition = player.motor.Body.position;
            bool idle = atSea && !Session.InputLocked && !player.HasPendingStep && !menus.IsOpen;
            if (idle && CombatWorld != null)
            {
                while (regionsToPopulate.Count > 0)
                {
                    var populatedRegion = Populate(regionsToPopulate.Dequeue());
                    if (!populatedRegion.IsSuccess) Debug.LogWarning("Region population failed: " + populatedRegion.Detail);
                }
                foreach (var far in streamer != null ? streamer.Unwanted(player.motor.Body.position).ToList() : new List<FirstRegionAsset>())
                    Retire(far);
            }
            else if (!atSea) regionsToPopulate.Clear();
            var keyboard = Keyboard.current;
            bool escape = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
            bool chartKey = keyboard != null && keyboard.mKey.wasPressedThisFrame;
            if (ChartOpen)
            {
                chart.Refresh(ChartData());
                if (escape || chartKey) CloseChart();
                UpdateViews(atSea);
                return;
            }
            if (chartKey && idle) OpenChart();
            else if (chartKey && !atSea && !menus.IsOpen && Session.Lifecycle == Lifecycle.Docked) OpenChart();
            else if (escape && pausedByMenu) Resume();
            else if (idle)
            {
                if (pendingWrecks.Count > 0) Checkpoint();
                if (escape || focusLost) { focusLost = false; Pause(); }
                else
                {
                    if (interactRequested) { interactRequested = false; Interact(); }
                    UpdateDocking();
                    if (Session.Snapshot.Expedition != null &&
                        Session.Tick - lastCheckpointTick >= (long)Math.Round(checkpointSeconds / Session.FixedDeltaSeconds))
                        Checkpoint();
                }
            }
            else if (escape && !atSea && !menus.IsOpen && Session.Lifecycle == Lifecycle.Docked) ShowHarborMenu();
            if (!atSea) interactRequested = false;
            UpdateViews(atSea);
        }

        private void OnApplicationQuit()
        {
            // Best-effort final checkpoint; a crash may still roll back to the last one.
            if (Session != null && Session.Lifecycle == Lifecycle.AtSea && !Session.InputLocked && !player.HasPendingStep && CombatWorld != null)
                Checkpoint();
            // Background writes would not survive process exit.
            Store?.Flush();
        }

        private void HandleEvents()
        {
            foreach (var committed in Session.DrainEvents())
            {
                var snapshot = committed.Snapshot;
                switch (committed.Command)
                {
                    case "Embark":
                        voyageCargo = new Dictionary<string, int>();
                        hud.Toast("Cast off from " + CurrentHarbor + ".", ToastKind.Info);
                        break;
                    case "ActivateHub":
                        var raised = snapshot.Campaign.Hubs.Where(h => h.Value.Activated).Select(h => h.Key)
                            .FirstOrDefault(id => id == dockTarget) ?? dockTarget;
                        hud.Toast("Your flag flies over " + HubName(raised) + ". Bank and upgrades are shared there.", ToastKind.Gain, 4.5f);
                        sound?.PlayUi(Sfx.Flag, 0.8f);
                        break;
                    case "FastTravel":
                        hud.Toast("Arrived at " + HubName(snapshot.Campaign.CurrentHub) + ".", ToastKind.Info);
                        LastNotice = "Travelled to " + HubName(snapshot.Campaign.CurrentHub) + ".";
                        break;
                    case "CollectLoot":
                        var cargo = snapshot.Expedition?.Cargo ?? new Dictionary<string, int>();
                        var gained = cargo.Where(p => p.Value > Values.Amount(voyageCargo, p.Key))
                            .Select(p => "+" + (p.Value - Values.Amount(voyageCargo, p.Key)) + " " + p.Key).ToArray();
                        voyageCargo = cargo;
                        if (gained.Length > 0) hud.Toast("Salvaged " + string.Join(", ", gained), ToastKind.Gain);
                        sound?.PlayUi(Sfx.Pickup, 0.7f);
                        break;
                    case "Checkpoint":
                        SyncGeneratedSalvage();
                        break;
                    case "Dock":
                        sound?.PlayUi(Sfx.Bell, 0.7f);
                        ShowCard("Safe harbor", "Docked at " + HubName(snapshot.Campaign.CurrentHub) + ". Cargo is banked and the voyage is over.",
                            Lines(voyageCargo.Count == 0 || voyageCargo.Values.All(v => v == 0) ? "Banked: nothing this time." : "Banked: " + Describe(voyageCargo),
                                "Bank now holds " + Describe(snapshot.Campaign.Bank) + "."),
                            new MenuAction("Enter the harbor", "continue", CloseCard, true));
                        voyageCargo = new Dictionary<string, int>();
                        break;
                    case "Sink":
                        sound?.Play(Sfx.Sink, lastSeaPosition, 1f, 0);
                        followCamera.FocusOverride = lastSeaPosition;
                        presenter.SinkCopy(player.transform, lastSeaPosition);
                        ShowCard("Your ship went down", "The crew is fished out and brought back to " + HubName(snapshot.Campaign.CurrentHub) + ".",
                            Lines(voyageCargo.Count == 0 || voyageCargo.Values.All(v => v == 0) ? "Lost cargo: none." : "Lost cargo: " + Describe(voyageCargo),
                                "Your bank, upgrades and equipment are safe. The ship is refitted at no cost."),
                            new MenuAction("Return to harbor", "continue", () => { followCamera.FocusOverride = null; followCamera.Snap(); CloseCard(); }, true));
                        voyageCargo = new Dictionary<string, int>();
                        break;
                    case "PurchaseUpgrade":
                        LastNotice = "Upgrade purchased.";
                        break;
                }
            }
        }

        // Keeps the regions around the ship (or the harbor) loaded, retries a waiting
        // arrival once its regions are ready, and holds the ship at the edge of any
        // region whose collision is not ready yet (it never sails into missing collision).
        private void Stream()
        {
            if (streamer == null) return;
            bool atSea = Session.Lifecycle == Lifecycle.AtSea && Session.Snapshot.Expedition != null;
            if (arrivalWaiting && Session.ArrivalPending)
            {
                if (streamer.AllReady(arrivalAt))
                {
                    arrivalWaiting = false;
                    var arrived = Session.RetryArrival();
                    if (!arrived.IsSuccess && !arrivalWaiting) ShowRecovery();
                }
                return;
            }
            if (Session.ArrivalPending) return;
            var here = player.motor.Body.position;
            streamer.Require(here);
            if (!atSea)
            {
                foreach (var far in streamer.Unwanted(here).ToList()) streamer.Unload(far.regionId);
                return;
            }
            bool ready = streamer.IsReady(RegionIdAt(here));
            if (!ready && !holding && !player.HasPendingStep && !Session.InputLocked)
            {
                holding = true; player.SetPaused(true);
                if (!fogWarned) { fogWarned = true; hud.Toast("Charting unknown waters…", ToastKind.Info, 2f); }
            }
            else if (ready && holding)
            {
                holding = false; fogWarned = false;
                if (!pausedByMenu) player.SetPaused(false);
            }
        }

        // Frozen saves or unfinished arrivals keep input locked until resolved.
        private void WatchLocks()
        {
            if (errorOpen || pausedByMenu || holding) return;
            bool loadFailed = arrivalWaiting && streamer != null && streamer.Wanted(arrivalAt).Any(r => streamer.StateOf(r.regionId) == RegionState.Failed);
            if (Session.PendingSave != null || (Session.ArrivalPending && (!arrivalWaiting || loadFailed))) ShowRecovery();
        }

        private void ShowRecovery()
        {
            if (errorOpen) return;
            errorOpen = true;
            bool save = Session.PendingSave != null;
            menus.Show(save ? "The voyage could not be saved" : "The world is not ready",
                save ? "Nothing is lost: your last save is intact and this change is waiting to be written."
                     : "The saved location could not be restored yet.",
                Lines(save ? Store.Directory : null),
                new MenuAction("Retry", "retry", () =>
                {
                    if (streamer != null) foreach (var r in Regions) streamer.Retry(r.regionId);
                    var result = Session.PendingSave != null ? Session.RetrySave() : Session.RetryArrival();
                    errorOpen = false; menus.Hide();
                    if (!result.IsSuccess && Session.InputLocked) ShowRecovery();
                }, true),
                new MenuAction("Exit to title", "exit", ExitToTitle));
        }

        // ---------------------------------------------------------------- at-sea actions

        public RuleResult Checkpoint()
        {
            var voyage = Session.Snapshot.Expedition;
            if (voyage == null || CombatWorld == null) return new RuleResult(RuleError.WrongLifecycle);
            if (Session.InputLocked || player.HasPendingStep) return new RuleResult(RuleError.Busy);
            var captured = CombatWorld.Capture(voyage.Id, voyage.Tick);
            var wrecks = new List<EntityState>();
            foreach (var enemy in pendingWrecks)
            {
                var spec = Combat.Enemies[enemy.Definition.Id];
                if (spec.WreckLoot.Values.All(v => v == 0)) continue;
                var id = EntityId.Generated(voyage.Id, "wreck/" + (enemy.Id.SpawnId ?? enemy.Id.AuthoredId));
                if (captured.Entities.ContainsKey(id)) continue;
                var at = enemy.Target.transform.position;
                wrecks.Add(new EntityState(id, CombatCatalog.WreckDefinition, new SeaPosition(RegionIdAt(at), at.x, at.z), 1, false,
                    spec.WreckLoot, new Dictionary<string, double>(), "salvage"));
            }
            if (wrecks.Count > 0)
                captured = new ExpeditionState(captured.Id, captured.Seed, captured.RngState, captured.Tick, captured.Health, captured.Speed,
                    captured.Position, captured.Cargo, captured.Modifiers, captured.Cooldowns, captured.Encounters, captured.Entities.Values.Concat(wrecks));
            var result = Session.Checkpoint(Guid.NewGuid(), captured);
            lastCheckpointTick = voyage.Tick;
            if (result.IsSuccess)
            {
                CheckpointCount++;
                if (pendingWrecks.Count > 0)
                    hud.Toast(pendingWrecks.Count == 1 ? "Enemy ship sunk — salvage its wreck!" : pendingWrecks.Count + " ships sunk — salvage the wrecks!", ToastKind.Gain);
                pendingWrecks.Clear();
            }
            else if (result.Error == RuleError.SaveFailed) ShowRecovery();
            else if (result.Error != RuleError.Busy) Debug.LogWarning("Checkpoint rejected: " + result.Error + " " + result.Detail);
            return result;
        }

        private void SyncGeneratedSalvage()
        {
            var voyage = Session.Snapshot.Expedition;
            if (voyage == null) return;
            var existing = new HashSet<EntityId>(SalvageRegions.Where(s => s != null).SelectMany(s => s.Sources).Where(s => s != null).Select(s => s.Id));
            bool added = false;
            foreach (var entity in voyage.Entities.Values.Where(e => e.Id.AuthoredId == null && SalvageRegion.IsSalvage(e.DefinitionId)))
            {
                if (existing.Contains(entity.Id) || !populated.Contains(entity.Position.RegionId)) continue;
                var view = SalvageRegions.FirstOrDefault(s => s != null && s.content != null && s.content.regionId == entity.Position.RegionId);
                if (view == null) continue;
                view.Add(Session, entity); added = true;
            }
            if (added) RefreshSources();
        }

        private void Interact()
        {
            var voyage = Session.Snapshot.Expedition;
            var hub = HubInReach(voyage.Position);
            if (hub != null)
            {
                if (!Session.Snapshot.Campaign.Hubs[hub.Id].Activated)
                {
                    // Raise the flag first (a coherent at-sea save), then moor as usual.
                    var boundary = Checkpoint();
                    if (!boundary.IsSuccess) return;
                    dockTarget = hub.Id;
                    var activated = Session.ActivateHub(Guid.NewGuid(), voyage.Id, hub.Id);
                    if (activated.Error == RuleError.SaveFailed) { ShowRecovery(); return; }
                    if (!activated.IsSuccess) { dockTarget = null; hud.Toast("Cannot claim this harbor: " + activated.Error, ToastKind.Warning); return; }
                }
                dockTarget = hub.Id; dockRequestedTick = -1;
                return;
            }
            var source = interaction.Nearest();
            if (source == null) { hud.Toast("Nothing to salvage within reach.", ToastKind.Info, 2f); return; }
            // Capture the scene first so the pickup's save describes one simulation instant.
            var saved = Checkpoint();
            if (!saved.IsSuccess) return;
            var result = interaction.TryCollect(Guid.NewGuid());
            if (result.Error == RuleError.CargoFull)
            {
                hud.Toast("The hold is too full for that. Bank your cargo in harbor.", ToastKind.Warning);
                sound?.PlayUi(Sfx.CargoFull, 0.7f);
            }
            else if (!result.IsSuccess && !result.IsPending && result.Error != RuleError.SaveFailed)
                hud.Toast("Could not salvage: " + result.Error, ToastKind.Warning);
            else if (result.Error == RuleError.SaveFailed) ShowRecovery();
        }

        // Any harbor whose berth contains the position; inactive ones can be claimed.
        private HubDefinition HubInReach(SeaPosition position)
        {
            foreach (var hub in Definitions.Hubs.Values)
            {
                if (hub.Dock.RegionId != position.RegionId) continue;
                double dx = position.X - hub.Dock.X, dz = position.Z - hub.Dock.Z;
                if (dx * dx + dz * dz <= hub.Radius * hub.Radius) return hub;
            }
            return null;
        }

        private void UpdateDocking()
        {
            if (dockTarget == null) { playerInput.ForceBrake = false; return; }
            var voyage = Session.Snapshot.Expedition;
            var hub = Definitions.Hubs[dockTarget];
            if (HubInReach(voyage.Position)?.Id != dockTarget)
            {
                dockTarget = null; playerInput.ForceBrake = false;
                hud.Toast("Docking cancelled.", ToastKind.Info, 2f);
                return;
            }
            playerInput.ForceBrake = true;
            if (dockRequestedTick >= 0 && voyage.Tick <= dockRequestedTick) return;
            if (voyage.Speed > hub.MaximumSpeed * 0.8) return;
            var result = Session.RequestDock(Guid.NewGuid(), voyage.Id, dockTarget);
            if (result.IsPending) dockRequestedTick = voyage.Tick;
            else if (result.Error != RuleError.Busy)
            {
                hud.Toast("Cannot dock: " + result.Error, ToastKind.Warning);
                dockTarget = null; playerInput.ForceBrake = false;
            }
        }

        private void Pause()
        {
            var saved = Checkpoint();
            if (!saved.IsSuccess && saved.Error == RuleError.SaveFailed) return;
            player.SetPaused(true);
            pausedByMenu = true;
            menus.Show("Paused", saved.IsSuccess ? "Voyage saved." : "", null,
                new MenuAction("Resume", "resume", Resume, true),
                new MenuAction("Save and exit to title", "exit", ExitToTitle),
                new MenuAction("Quit game", "quit", QuitGame));
        }

        public void Resume()
        {
            menus.Hide();
            pausedByMenu = false;
            if (!holding) player.SetPaused(false);
        }

        // The chart pauses a voyage at a checkpointed boundary, like the pause menu.
        public void OpenChart()
        {
            if (chart == null) return;
            if (Session.Lifecycle == Lifecycle.AtSea)
            {
                var saved = Checkpoint();
                if (!saved.IsSuccess && saved.Error == RuleError.SaveFailed) return;
                player.SetPaused(true);
                chartPaused = true;
            }
            chart.Show(ChartData());
        }

        public void CloseChart()
        {
            if (chart != null) chart.Hide();
            if (chartPaused)
            {
                chartPaused = false;
                if (!holding && !pausedByMenu) player.SetPaused(false);
            }
        }

        public ChartModel ChartData()
        {
            var data = new ChartModel();
            foreach (var r in Regions)
            {
                data.Regions.Add(new ChartModel.Region { Name = r.displayName, Bounds = r.bounds });
                foreach (var island in r.islands) data.Islands.Add(new ChartModel.Island { Center = island.position, Size = island.size });
                data.Islands.Add(new ChartModel.Island { Center = r.homeLandmass, Size = r.homeLandmassSize });
                foreach (var outpost in r.outposts) data.Islands.Add(new ChartModel.Island { Center = outpost.landmass, Size = outpost.landmassSize });
            }
            var campaign = Session.Snapshot.Campaign;
            foreach (var hub in Definitions.Hubs.Values)
                data.Harbors.Add(new ChartModel.Harbor
                {
                    Name = HubName(hub.Id), Position = new Vector2((float)hub.Dock.X, (float)hub.Dock.Z),
                    Claimed = campaign.Hubs[hub.Id].Activated,
                    Current = Session.Lifecycle == Lifecycle.Docked && campaign.CurrentHub == hub.Id
                });
            var body = player.motor.Body;
            data.ShipVisible = body != null;
            if (body != null) { data.Ship = new Vector2(body.position.x, body.position.z); data.ShipYaw = body.rotation.eulerAngles.y; }
            return data;
        }

        private void ShowHarborMenu()
        {
            ShowCard(CurrentHarbor, "Everything in harbor is already saved.", null,
                new MenuAction("Back to the harbor", "resume", CloseCard, true),
                new MenuAction("Exit to title", "exit", ExitToTitle),
                new MenuAction("Quit game", "quit", QuitGame));
        }

        public void ExitToTitle()
        {
            if (Session != null && Session.Lifecycle == Lifecycle.AtSea && !Session.InputLocked && !player.HasPendingStep) Checkpoint();
            pausedByMenu = false; errorOpen = false; resultsOpen = false;
            if (Application.CanStreamedLevelBeLoaded(LaunchOptions.TitleScene)) SceneManager.LoadScene(LaunchOptions.TitleScene);
            else QuitGame();
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowCard(string heading, string detail, IEnumerable<string> body, params MenuAction[] actions)
        {
            resultsOpen = true;
            menus.Show(heading, detail, body, actions);
        }

        private void CloseCard()
        {
            resultsOpen = false;
            menus.Hide();
            if (Session != null && Session.Lifecycle == Lifecycle.Docked)
                harbor.Root.schedule.Execute(() => harbor.Root.Q<DropdownField>()?.Focus());
        }

        // ---------------------------------------------------------------- views

        private void UpdateViews(bool atSea)
        {
            bool docked = Session.Lifecycle == Lifecycle.Docked && Session.Snapshot.Expedition == null;
            harbor.Root.style.display = docked ? DisplayStyle.Flex : DisplayStyle.None;
            hud.SetVisible(atSea);
            if (!atSea || CombatWorld == null) { aimMarker.Hide(); hud.RenderBars(Array.Empty<HudMarker>()); hud.RenderHome(false, default, 0, null); return; }
            var voyage = Session.Snapshot.Expedition;
            var stats = Session.ShipStats();
            model.Health = voyage.Health;
            model.MaxHealth = stats["health"];
            model.CargoUsed = ShipRules.Weight(Definitions, voyage.Cargo);
            model.CargoCapacity = (int)Math.Floor(stats["cargo"]);
            model.CargoDetail = voyage.Cargo.Values.All(v => v == 0) ? "Hold empty" : Describe(voyage.Cargo);
            model.WeaponName = Title(CombatWorld.Weapon.Id).ToUpperInvariant();
            model.WeaponCooldown = CombatWorld.WeaponCooldown; model.WeaponCooldownMax = CombatWorld.Weapon.Cooldown;
            model.AbilityName = Title(Combat.AbilityId).ToUpperInvariant();
            model.AbilityCooldown = CombatWorld.AbilityCooldown; model.AbilityCooldownMax = Combat.AbilityCooldown;
            model.AbilityActive = CombatWorld.BraceRemaining;
            if (CombatWorld.BraceRemaining > 0 && !braced) sound?.Play(Sfx.Brace, player.transform.position, 0.8f, 0.02f);
            braced = CombatWorld.BraceRemaining > 0;
            model.Region = (RegionAt(player.motor.Body.position)?.displayName ?? regionTitle).ToUpperInvariant();
            model.Condition = SeaConditions.Describe(Weather).ToUpperInvariant();
            bool full = model.CargoUsed >= model.CargoCapacity;
            model.Objective = full ? "Hold full: return to a harbor to bank it."
                : model.CargoUsed > 0 ? "Bring your cargo to a friendly harbor to bank it."
                : "Salvage barrels and wrecks. Sink raiders for plunder.";
            Sightings(voyage);
            Prompt(voyage);
            hud.Render(model);

            bars.Clear();
            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.Target.Defeated) continue;
                var screen = worldCamera.WorldToScreenPoint(enemy.transform.position + Vector3.up * 4f);
                if (screen.z <= 0) continue;
                bars.Add(new HudMarker { Screen = screen, Fill = (float)(enemy.Target.Health / enemy.Target.MaximumHealth) });
            }
            hud.RenderBars(bars);
            RenderHomeMarker(voyage);

            if (!Session.InputLocked && playerInput.HasAimPoint && !menus.IsOpen)
                aimMarker.Show(player.motor.weaponOrigin.position, playerInput.AimPoint, CombatWorld.Weapon.Range);
            else aimMarker.Hide();
        }

        private void Prompt(ExpeditionState voyage)
        {
            model.PromptWarning = false;
            if (voyage.Health <= 0) { model.Prompt = ""; return; }
            if (dockTarget != null) { model.Prompt = "Bringing her alongside…"; return; }
            var hub = HubInReach(voyage.Position);
            if (hub != null)
            {
                bool cargo = voyage.Cargo.Values.Any(v => v > 0);
                model.Prompt = !Session.Snapshot.Campaign.Hubs[hub.Id].Activated
                    ? "[E]  Raise your flag at " + HubName(hub.Id) + " and moor"
                    : "[E]  Dock at " + HubName(hub.Id) + (cargo ? "  —  bank " + Describe(voyage.Cargo) : "");
                return;
            }
            var source = interaction.Nearest();
            if (source == null) { model.Prompt = ""; return; }
            var loot = source.Capture().Loot;
            int weight = ShipRules.Weight(Definitions, loot);
            int free = (int)Math.Floor(Session.ShipStats()["cargo"]) - ShipRules.Weight(Definitions, voyage.Cargo);
            string what = source.Capture().DefinitionId == "wreck" ? "wreck" : "barrel";
            if (weight > free)
            {
                model.Prompt = "Hold too full for this " + what + " (" + Describe(loot) + ", needs " + weight + " space)";
                model.PromptWarning = true;
            }
            else model.Prompt = "[E]  Salvage " + what + "  —  " + Describe(loot);
        }

        private void Sightings(ExpeditionState voyage)
        {
            foreach (var hub in Definitions.Hubs.Values)
            {
                if (Session.Snapshot.Campaign.Hubs[hub.Id].Activated || sighted.Contains(hub.Id)) continue;
                double dx = voyage.Position.X - hub.Dock.X, dz = voyage.Position.Z - hub.Dock.Z;
                if (dx * dx + dz * dz > 45 * 45) continue;
                sighted.Add(hub.Id);
                hud.Toast("Land ho! " + HubName(hub.Id) + " — sail into its berth to raise your flag.", ToastKind.Info, 5f);
            }
        }

        private void RenderHomeMarker(ExpeditionState voyage)
        {
            var ship = player.motor.Body.position;
            var hub = Definitions.Hubs.Values.Where(h => Session.Snapshot.Campaign.Hubs[h.Id].Activated)
                .OrderBy(h => (h.Dock.X - ship.x) * (h.Dock.X - ship.x) + (h.Dock.Z - ship.z) * (h.Dock.Z - ship.z)).FirstOrDefault();
            if (hub == null) { hud.RenderHome(false, default, 0, null); return; }
            var target = new Vector3((float)hub.Dock.X, 0, (float)hub.Dock.Z);
            var screen = worldCamera.WorldToScreenPoint(target);
            bool onScreen = screen.z > 0 && screen.x > 0 && screen.x < Screen.width && screen.y > 0 && screen.y < Screen.height;
            if (onScreen) { hud.RenderHome(false, default, 0, null); return; }
            var direction = new Vector2(target.x - ship.x, target.z - ship.z);
            float distance = direction.magnitude;
            var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var dir = direction.normalized;
            // Keep clear of the HUD: ability slots below, vitals/voyage panels above.
            float halfX = Screen.width * 0.45f;
            float halfY = Screen.height * 0.24f;
            float scale = Mathf.Min(halfX / Mathf.Max(0.001f, Mathf.Abs(dir.x)), halfY / Mathf.Max(0.001f, Mathf.Abs(dir.y)));
            var edge = center + dir * scale;
            float degrees = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
            hud.RenderHome(true, edge, degrees, HubName(hub.Id) + " " + Mathf.RoundToInt(distance) + " m");
        }

        private string Describe(IReadOnlyDictionary<string, int> bundle)
        {
            var parts = Definitions.ResourceWeights.Keys.Where(k => Values.Amount(bundle, k) > 0)
                .Select(k => Values.Amount(bundle, k) + " " + k).ToArray();
            return parts.Length == 0 ? "nothing" : string.Join(", ", parts);
        }

        private static string Title(string id) => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(id.Replace('-', ' '));

        private static IEnumerable<string> Lines(params string[] values) => values.Where(v => !string.IsNullOrEmpty(v));
    }
}
