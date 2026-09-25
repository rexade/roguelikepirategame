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

    [RequireComponent(typeof(UIDocument))]
    public sealed class GameHud : MonoBehaviour
    {
        public StyleSheet stylesheet;
        private VisualElement root, hullFill, cargoFill, weaponFill, abilityFill, weaponSlot, abilitySlot, markers, toasts, homeArrow;
        private Label hullValue, cargoValue, cargoDetail, weaponName, abilityName, weaponKey, abilityKey, prompt, region, condition, objective, homeLabel;
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
            homeArrow.Add(new VisualElement { pickingMode = PickingMode.Ignore }.WithClass("home-glyph"));
            homeArrow.Add(homeLabel);
            markers.Add(homeArrow);

            var vitals = Panel("vitals", root);
            hullFill = Meter(vitals, "HULL", "hull", out hullValue);
            cargoFill = Meter(vitals, "HOLD", "cargo", out cargoValue);
            cargoDetail = new Label { name = "cargo-detail" }.WithClass("detail");
            vitals.Add(cargoDetail);

            var header = Panel("voyage", root);
            region = new Label { name = "region" }.WithClass("region");
            condition = new Label { name = "condition" }.WithClass("condition");
            objective = new Label { name = "objective" }.WithClass("objective");
            header.Add(region); header.Add(condition); header.Add(objective);

            toasts = new VisualElement { name = "toasts", pickingMode = PickingMode.Ignore };
            toasts.AddToClassList("toasts");
            root.Add(toasts);

            prompt = new Label { name = "prompt", pickingMode = PickingMode.Ignore };
            prompt.AddToClassList("prompt");
            root.Add(prompt);

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
            row.Add(new Label(caption).WithClass("meter-caption"));
            var bar = new VisualElement { name = name + "-bar", pickingMode = PickingMode.Ignore }.WithClass("meter");
            var fill = new VisualElement { name = name + "-fill", pickingMode = PickingMode.Ignore }.WithClass("meter-fill").WithClass(name + "-fill");
            bar.Add(fill); row.Add(bar);
            value = new Label { name = name + "-value" }.WithClass("meter-value");
            row.Add(value);
            parent.Add(row);
            return fill;
        }

        private static VisualElement Slot(VisualElement parent, string name, out Label key, out Label title, out VisualElement fill)
        {
            var slot = new VisualElement { name = name + "-slot", pickingMode = PickingMode.Ignore }.WithClass("slot");
            fill = new VisualElement { name = name + "-cooldown", pickingMode = PickingMode.Ignore }.WithClass("slot-cooldown");
            slot.Add(fill);
            key = new Label().WithClass("slot-key");
            title = new Label { name = name + "-name" }.WithClass("slot-name");
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

        // Points toward an off-screen destination (the home harbor) from the screen edge.
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

        public Vector2 ToPanel(Vector2 screen)
        {
            var panel = root?.panel;
            if (panel == null) return screen;
            return RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screen.x, Screen.height - screen.y));
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
