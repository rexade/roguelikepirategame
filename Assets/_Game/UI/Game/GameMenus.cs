using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PirateGame.UI.Game
{
    public readonly struct MenuAction
    {
        public string Label { get; }
        public string Name { get; }
        public Action Invoke { get; }
        public bool Primary { get; }
        public MenuAction(string label, string name, Action invoke, bool primary = false)
        { Label = label; Name = name; Invoke = invoke; Primary = primary; }
    }

    // Card mood: Plain for pause/results, Dire for "THE SEA CLAIMS YOU",
    // Radiant for "BEACON RELIT" and other triumphs.
    public enum CardTone { Plain, Dire, Radiant }

    // One modal parchment card at a time: pause, voyage results, save/arrival retry
    // and confirmations. Buttons only invoke composition callbacks.
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameMenus : MonoBehaviour
    {
        public StyleSheet stylesheet;
        private VisualElement root, card, lines, buttons;
        private Label title, subtitle;
        private readonly Dictionary<string, Action> handlers = new Dictionary<string, Action>();
        public bool IsOpen { get; private set; }
        public string CurrentTitle => IsOpen ? title.text : null;
        public CardTone CurrentTone { get; private set; }
        public VisualElement Root => root;

        private void OnEnable() => Build();
        private void Start() { if (root == null) Build(); }

        private void Build()
        {
            var docRoot = GetComponent<UIDocument>().rootVisualElement;
            if (docRoot == null) return;
            docRoot.Clear();
            root = new VisualElement { name = "menus" };
            root.AddToClassList("menus");
            if (stylesheet != null) root.styleSheets.Add(stylesheet);
            card = new VisualElement { name = "menu-card" };
            card.AddToClassList("menu-card");
            title = new Label { name = "menu-title" }; title.AddToClassList("menu-title");
            var rule = new SunRule { name = "menu-rule" }; rule.AddToClassList("menu-rule");
            subtitle = new Label { name = "menu-subtitle" }; subtitle.AddToClassList("menu-subtitle");
            lines = new VisualElement { name = "menu-lines" }; lines.AddToClassList("menu-lines");
            buttons = new VisualElement { name = "menu-buttons" }; buttons.AddToClassList("menu-buttons");
            card.Add(title); card.Add(rule); card.Add(subtitle); card.Add(lines); card.Add(buttons);
            root.Add(card);
            docRoot.Add(root);
            root.style.display = DisplayStyle.None;
            IsOpen = false;
        }

        public void Show(string heading, string detail, IEnumerable<string> body, params MenuAction[] actions) =>
            Show(CardTone.Plain, heading, detail, body, actions);

        public void Show(CardTone tone, string heading, string detail, IEnumerable<string> body, params MenuAction[] actions)
        {
            if (root == null) Build();
            if (root == null) return;
            CurrentTone = tone;
            card.EnableInClassList("dire", tone == CardTone.Dire);
            card.EnableInClassList("radiant", tone == CardTone.Radiant);
            title.text = heading ?? "";
            subtitle.text = detail ?? "";
            subtitle.style.display = string.IsNullOrEmpty(detail) ? DisplayStyle.None : DisplayStyle.Flex;
            lines.Clear();
            if (body != null)
                foreach (var line in body)
                {
                    var label = new Label(line); label.AddToClassList("menu-line");
                    lines.Add(label);
                }
            lines.style.display = lines.childCount == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            buttons.Clear();
            handlers.Clear();
            Button first = null;
            foreach (var action in actions)
            {
                var captured = action;
                var button = new Button(() => captured.Invoke?.Invoke()) { text = action.Label, name = action.Name };
                button.AddToClassList("menu-button");
                if (action.Primary) button.AddToClassList("primary");
                buttons.Add(button);
                if (!string.IsNullOrEmpty(action.Name)) handlers[action.Name] = action.Invoke;
                if (first == null) first = button;
            }
            root.style.display = DisplayStyle.Flex;
            IsOpen = true;
            if (first != null) root.schedule.Execute(() => first.Focus());
        }

        public void Hide()
        {
            if (root != null) root.style.display = DisplayStyle.None;
            IsOpen = false;
        }

        // Test/automation hook: runs a visible button's action as a click would.
        public bool Activate(string name)
        {
            if (!IsOpen || !handlers.TryGetValue(name, out var action)) return false;
            action?.Invoke();
            return true;
        }
    }
}
