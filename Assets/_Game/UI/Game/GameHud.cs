using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PirateGame.UI.Game
{
    // Plain read model filled by composition each frame. The HUD never queries
    // gameplay objects or issues commands; it only renders these values.
    public sealed class HudModel
    {
        public double Health, MaxHealth;
        public int CargoUsed, CargoCapacity;
        public string CargoDetail = "";
        public string WeaponName = "", WeaponKey = "LMB";
        public double WeaponCooldown, WeaponCooldownMax = 1;
        public string AbilityName = "", AbilityKey = "RMB";
        public double AbilityCooldown, AbilityCooldownMax = 1, AbilityActive;
        public string Prompt = "";
        public bool PromptWarning;
        public string Region = "", Condition = "", Objective = "";
    }

    public struct HudMarker
    {
        public Vector2 Screen;      // Screen pixels, origin bottom-left (Camera.WorldToScreenPoint).
        public float Fill;          // Health fraction for bars.
        public string Label;        // Optional caption (e.g. distance).
    }

    public enum ToastKind { Info, Gain, Warning }

    // The voyage HUD in the engraved-chart style: parchment tags with ink text,
    // hatched ink meters, and two ceremonial overlays driven by composition:
    // lore inscriptions (landmarks) and zone titles (entering a new sea).
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameHud : MonoBehaviour
    {
        public StyleSheet stylesheet;
        private VisualElement root, hullFill, cargoFill, weaponFill, abilityFill, weaponSlot, abilitySlot, markers, toasts, homeArrow;
        private VisualElement inscription, zoneTitle;
        private Label hullValue, cargoValue, cargoDetail, weaponName, abilityName, weaponKey, abilityKey, prompt, region, condition, objective, homeLabel;
        private Label inscriptionTitle, inscriptionLine, zoneName, zoneMood;
        private IVisualElementScheduledItem inscriptionHide, zoneHide;
        private readonly List<VisualElement> bars = new List<VisualElement>();
        public VisualElement Root => root;
        public bool Visible { get; private set; }

        private void OnEnable() => Build();
        private void Start() { if (root == null) Build(); }

        private void Build()
        {
            var document = GetComponent<UIDocument>();
            var docRoot = document.rootVisualElement;
            if (docRoot == null) return;
            docRoot.Clear();
            root = new VisualElement { name = "hud", pickingMode = PickingMode.Ignore };
            root.AddToClassList("hud");
            if (stylesheet != null) root.styleSheets.Add(stylesheet);
            docRoot.Add(root);

            markers = Layer("markers");
            homeArrow = new VisualElement { name = "home-arrow", pickingMode = PickingMode.Ignore };
            homeArrow.AddToClassList("home-arrow");
            homeLabel = new Label { name = "home-label", pickingMode = PickingMode.Ignore };
            homeArrow.Add(new HomeGlyph().WithClass("home-glyph"));
            homeArrow.Add(homeLabel);
            markers.Add(homeArrow);

            var vitals = Panel("vitals", root);
            hullFill = Meter(vitals, "HULL", "hull", out hullValue);
            cargoFill = Meter(vitals, "HOLD", "cargo", out cargoValue);
            cargoDetail = new Label { name = "cargo-detail", pickingMode = PickingMode.Ignore }.WithClass("detail");
            vitals.Add(cargoDetail);

            var header = Panel("voyage", root);
            region = new Label { name = "region", pickingMode = PickingMode.Ignore }.WithClass("region");
            condition = new Label { name = "condition", pickingMode = PickingMode.Ignore }.WithClass("condition");
            objective = new Label { name = "objective", pickingMode = PickingMode.Ignore }.WithClass("objective");
            header.Add(region); header.Add(new SunRule { name = "voyage-rule" }); header.Add(condition); header.Add(objective);

            toasts = new VisualElement { name = "toasts", pickingMode = PickingMode.Ignore };
            toasts.AddToClassList("toasts");
            root.Add(toasts);

            inscription = new VisualElement { name = "inscription", pickingMode = PickingMode.Ignore }.WithClass("inscription");
            inscriptionTitle = new Label { name = "inscription-title", pickingMode = PickingMode.Ignore }.WithClass("inscription-title");
            inscriptionLine = new Label { name = "inscription-line", pickingMode = PickingMode.Ignore }.WithClass("inscription-line");
            inscription.Add(inscriptionTitle); inscription.Add(inscriptionLine);
            root.Add(inscription);

            zoneTitle = new VisualElement { name = "zone-title", pickingMode = PickingMode.Ignore }.WithClass("zone-title");
            zoneName = new Label { name = "zone-name", pickingMode = PickingMode.Ignore }.WithClass("zone-name");
            zoneMood = new Label { name = "zone-mood", pickingMode = PickingMode.Ignore }.WithClass("zone-mood");
            zoneTitle.Add(zoneName); zoneTitle.Add(new SunRule { name = "zone-rule" }.WithClass("zone-rule")); zoneTitle.Add(zoneMood);
            root.Add(zoneTitle);

            var promptRow = new VisualElement { name = "prompt-row", pickingMode = PickingMode.Ignore }.WithClass("prompt-row");
            prompt = new Label { name = "prompt", pickingMode = PickingMode.Ignore };
            prompt.AddToClassList("prompt");
            promptRow.Add(prompt);
            root.Add(promptRow);

            var slots = new VisualElement { name = "slots", pickingMode = PickingMode.Ignore };
            slots.AddToClassList("slots");
            root.Add(slots);
            weaponSlot = Slot(slots, "weapon", out weaponKey, out weaponName, out weaponFill);
            abilitySlot = Slot(slots, "ability", out abilityKey, out abilityName, out abilityFill);
            SetVisible(Visible);
        }

        private VisualElement Layer(string name)
        {
            var layer = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            layer.AddToClassList("layer");
            root.Add(layer);
            return layer;
        }

        private static VisualElement Panel(string name, VisualElement parent)
        {
            var panel = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            panel.AddToClassList("hud-panel");
            parent.Add(panel);
            return panel;
        }

        private static VisualElement Meter(VisualElement parent, string caption, string name, out Label value)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore }.WithClass("meter-row");
            row.Add(new Label(caption) { pickingMode = PickingMode.Ignore }.WithClass("meter-caption"));
            var bar = new VisualElement { name = name + "-bar", pickingMode = PickingMode.Ignore }.WithClass("meter");
            var fill = new VisualElement { name = name + "-fill", pickingMode = PickingMode.Ignore }.WithClass("meter-fill").WithClass(name + "-fill");
            bar.Add(fill); row.Add(bar);
            value = new Label { name = name + "-value", pickingMode = PickingMode.Ignore }.WithClass("meter-value");
            row.Add(value);
            parent.Add(row);
            return fill;
        }

        private static VisualElement Slot(VisualElement parent, string name, out Label key, out Label title, out VisualElement fill)
        {
            var slot = new VisualElement { name = name + "-slot", pickingMode = PickingMode.Ignore }.WithClass("slot");
            fill = new VisualElement { name = name + "-cooldown", pickingMode = PickingMode.Ignore }.WithClass("slot-cooldown");
            slot.Add(fill);
            key = new Label { pickingMode = PickingMode.Ignore }.WithClass("slot-key");
            title = new Label { name = name + "-name", pickingMode = PickingMode.Ignore }.WithClass("slot-name");
            slot.Add(key); slot.Add(title);
            parent.Add(slot);
            return slot;
        }

        public void SetVisible(bool value)
        {
            Visible = value;
            if (root != null) root.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Render(HudModel model)
        {
            if (root == null || model == null) return;
            float health = model.MaxHealth > 0 ? Mathf.Clamp01((float)(model.Health / model.MaxHealth)) : 0;
            hullFill.style.width = Length.Percent(health * 100);
            hullFill.EnableInClassList("critical", health <= 0.3f);
            hullValue.text = Math.Ceiling(model.Health).ToString("0") + " / " + model.MaxHealth.ToString("0");
            float cargo = model.CargoCapacity > 0 ? Mathf.Clamp01((float)model.CargoUsed / model.CargoCapacity) : 1;
            cargoFill.style.width = Length.Percent(cargo * 100);
            cargoFill.EnableInClassList("full", model.CargoUsed >= model.CargoCapacity);
            cargoValue.text = model.CargoUsed + " / " + model.CargoCapacity;
            cargoDetail.text = model.CargoDetail;
            region.text = model.Region;
            condition.text = model.Condition;
            condition.style.display = string.IsNullOrEmpty(model.Condition) ? DisplayStyle.None : DisplayStyle.Flex;
            objective.text = model.Objective;
            weaponKey.text = model.WeaponKey; weaponName.text = model.WeaponName;
            abilityKey.text = model.AbilityKey; abilityName.text = model.AbilityName;
            float weapon = model.WeaponCooldownMax > 0 ? Mathf.Clamp01((float)(model.WeaponCooldown / model.WeaponCooldownMax)) : 0;
            weaponFill.style.height = Length.Percent(weapon * 100);
            weaponSlot.EnableInClassList("ready", weapon <= 0);
            bool active = model.AbilityActive > 0;
            float ability = active ? 1 : model.AbilityCooldownMax > 0 ? Mathf.Clamp01((float)(model.AbilityCooldown / model.AbilityCooldownMax)) : 0;
            abilityFill.style.height = Length.Percent(ability * 100);
            abilitySlot.EnableInClassList("ready", !active && ability <= 0);
            abilitySlot.EnableInClassList("active", active);
            prompt.text = model.Prompt;
            prompt.style.display = string.IsNullOrEmpty(model.Prompt) ? DisplayStyle.None : DisplayStyle.Flex;
            prompt.EnableInClassList("warning", model.PromptWarning);
        }

        // Enemy health bars at screen positions; extra bars are hidden, not destroyed.
        public void RenderBars(IReadOnlyList<HudMarker> values)
        {
            if (markers == null) return;
            while (bars.Count < values.Count)
            {
                var bar = new VisualElement { pickingMode = PickingMode.Ignore }.WithClass("enemy-bar");
                bar.Add(new VisualElement { pickingMode = PickingMode.Ignore }.WithClass("enemy-bar-fill"));
                markers.Add(bar); bars.Add(bar);
            }
            for (int i = 0; i < bars.Count; i++)
            {
                bool shown = i < values.Count;
                bars[i].style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
                if (!shown) continue;
                var p = ToPanel(values[i].Screen);
                bars[i].style.left = p.x - 30; bars[i].style.top = p.y - 4;
                bars[i][0].style.width = Length.Percent(Mathf.Clamp01(values[i].Fill) * 100);
            }
        }

        // Points toward an off-screen destination (the nearest lit beacon) from the screen edge.
        public void RenderHome(bool visible, Vector2 screen, float degrees, string label)
        {
            if (homeArrow == null) return;
            homeArrow.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible) return;
            var p = ToPanel(screen);
            homeArrow.style.left = p.x - 26; homeArrow.style.top = p.y - 26;
            homeArrow[0].style.rotate = new Rotate(new Angle(degrees, AngleUnit.Degree));
            homeLabel.text = label;
        }

        public void Toast(string text, ToastKind kind = ToastKind.Info, float seconds = 3.2f)
        {
            if (toasts == null || string.IsNullOrEmpty(text)) return;
            var toast = new Label(text) { pickingMode = PickingMode.Ignore }.WithClass("toast");
            toast.AddToClassList(kind == ToastKind.Gain ? "gain" : kind == ToastKind.Warning ? "warning" : "info");
            toasts.Add(toast);
            while (toasts.childCount > 4) toasts.RemoveAt(0);
            toast.schedule.Execute(() => toast.RemoveFromHierarchy()).StartingIn((long)(seconds * 1000));
        }

        // Lore banner at the top centre: an engraved title and one italic line.
        public void Inscription(string title, string line, float seconds = 6)
        {
            if (inscription == null) return;
            inscriptionTitle.text = title ?? "";
            inscriptionLine.text = line ?? "";
            inscriptionLine.style.display = string.IsNullOrEmpty(line) ? DisplayStyle.None : DisplayStyle.Flex;
            inscriptionHide = Reveal(inscription, inscriptionHide, seconds);
        }

        // Large fading zone name when the ship enters a new sea, with its mood below.
        public void ZoneTitle(string name, string mood, float seconds = 4.5f)
        {
            if (zoneTitle == null) return;
            zoneName.text = (name ?? "").ToUpperInvariant();
            zoneMood.text = mood ?? "";
            zoneMood.style.display = string.IsNullOrEmpty(mood) ? DisplayStyle.None : DisplayStyle.Flex;
            zoneHide = Reveal(zoneTitle, zoneHide, seconds);
        }

        public bool InscriptionShown => inscription != null && inscription.ClassListContains("shown");
        public bool ZoneTitleShown => zoneTitle != null && zoneTitle.ClassListContains("shown");

        // Fades in via the USS transition on `.shown`, then out after `seconds`.
        private static IVisualElementScheduledItem Reveal(VisualElement element, IVisualElementScheduledItem pending, float seconds)
        {
            pending?.Pause();
            element.AddToClassList("shown");
            return element.schedule.Execute(() => element.RemoveFromClassList("shown")).StartingIn((long)(Mathf.Max(0.5f, seconds) * 1000));
        }

        public Vector2 ToPanel(Vector2 screen)
        {
            var panel = root?.panel;
            if (panel == null) return screen;
            return RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screen.x, Screen.height - screen.y));
        }

        // Gilded needle pointing up (rotated by RenderHome) with a small sun at its heel.
        private sealed class HomeGlyph : VisualElement
        {
            public HomeGlyph()
            {
                pickingMode = PickingMode.Ignore;
                generateVisualContent += Draw;
            }

            private void Draw(MeshGenerationContext context)
            {
                var box = contentRect;
                if (box.width < 4) return;
                var paint = context.painter2D;
                var c = box.center;
                float s = Mathf.Min(box.width, box.height) * 0.5f;
                paint.BeginPath();
                paint.MoveTo(c + new Vector2(0, -s));
                paint.LineTo(c + new Vector2(s * 0.42f, s * 0.2f));
                paint.LineTo(c + new Vector2(0, s * 0.02f));
                paint.LineTo(c + new Vector2(-s * 0.42f, s * 0.2f));
                paint.ClosePath();
                paint.fillColor = Ink.GoldLight; paint.Fill();
                paint.strokeColor = Ink.Black; paint.lineWidth = 1.6f; paint.Stroke();
                Ink.Sun(paint, c + new Vector2(0, s * 0.5f), s * 0.18f, 8, s * 0.14f, Ink.Gold, Ink.Black, 1f);
            }
        }
    }

    internal static class ElementExtensions
    {
        public static T WithClass<T>(this T element, string className) where T : VisualElement
        {
            element.AddToClassList(className);
            return element;
        }
    }
}
