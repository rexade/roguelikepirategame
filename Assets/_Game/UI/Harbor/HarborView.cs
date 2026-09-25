using System;
using System.Linq;
using PirateGame.Core;
using PirateGame.Rules.Application;
using PirateGame.UI.Loadout;
using PirateGame.UI.Travel;
using UnityEngine;
using UnityEngine.UIElements;

namespace PirateGame.UI.Harbor
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class HarborView : MonoBehaviour
    {
        public StyleSheet stylesheet;
        private CampaignSession session;
        private DefinitionCatalog definitions;
        private Func<EmbarkPlan> plan;
        private Func<string, string> hubName;
        private TravelView travel;
        private Label title;
        private Label bank, stats, status, location, unlocks;
        private VisualElement actions;
        private Button retry;
        private LoadoutView loadout;
        private long displayedRevision = -1;
        private bool displayedLock;
        public VisualElement Root => GetComponent<UIDocument>().rootVisualElement;

        // hubName enables production naming and the fast-travel section (T09).
        public void Bind(CampaignSession owner, DefinitionCatalog catalog, Func<EmbarkPlan> embarkPlan, Func<string, string> hubName = null)
        {
            this.hubName = hubName;
            session = owner ?? throw new ArgumentNullException(nameof(owner));
            definitions = catalog ?? throw new ArgumentNullException(nameof(catalog));
            plan = embarkPlan ?? throw new ArgumentNullException(nameof(embarkPlan));
            Build(); Refresh();
        }

        private void Build()
        {
            var root = Root; root.Clear(); root.AddToClassList("harbor");
            if (stylesheet != null && !root.styleSheets.Contains(stylesheet)) root.styleSheets.Add(stylesheet);
            var header = new VisualElement(); header.AddToClassList("header"); root.Add(header);
            title = new Label("HOMEWARD HARBOR") { name = "title" }; header.Add(title);
            location = new Label(); header.Add(location);
            bank = new Label { name = "bank" }; root.Add(bank);
            actions = new VisualElement(); actions.AddToClassList("columns"); root.Add(actions);
            var equipment = Section(actions, "Equipment");
            loadout = new LoadoutView(session, definitions, ShowResult); equipment.Add(loadout);
            stats = new Label { name = "stats" }; equipment.Add(stats);
            var upgrades = Section(actions, "Harbor & ship"); upgrades.AddToClassList("upgrades");
            // Tracks in tier order; each row: name and effect, cost, then the button.
            foreach (var upgrade in definitions.Upgrades.Values.OrderBy(u => u.TrackId, StringComparer.Ordinal).ThenBy(u => u.Tier))
            {
                var row = new VisualElement(); row.AddToClassList("upgrade"); upgrades.Add(row);
                var info = new VisualElement(); info.AddToClassList("upgrade-info"); row.Add(info);
                var heading = new VisualElement(); heading.AddToClassList("upgrade-heading"); info.Add(heading);
                var name = new Label(Title(upgrade.Id)) { name = "name-" + upgrade.Id }; name.AddToClassList("upgrade-name"); heading.Add(name);
                var effects = string.Join(", ", upgrade.Modifiers.Select(m =>
                    (m.Value >= 0 ? "+" : "") + (m.Operation == ModifierOperation.Percent ? (m.Value * 100).ToString("0.#") + "%" : m.Value.ToString("0.#")) + " " + m.StatId));
                var effect = new Label(effects) { name = "effect-" + upgrade.Id }; effect.AddToClassList("upgrade-effect"); heading.Add(effect);
                var cost = new Label(string.Join("  /  ", upgrade.Cost.Select(p => p.Value + " " + p.Key))) { name = "cost-" + upgrade.Id };
                cost.AddToClassList("upgrade-cost"); info.Add(cost);
                row.Add(new Button(() => ShowResult(session.PurchaseUpgrade(Guid.NewGuid(), upgrade.Id)))
                    { text = "Purchase", name = "buy-" + upgrade.Id });
            }
            unlocks = new Label { name = "unlocks" }; upgrades.Add(unlocks);
            // Third column: fast travel (production only) above the departure button.
            var side = new VisualElement(); side.AddToClassList("side"); actions.Add(side);
            travel = hubName != null ? new TravelView(session, definitions, hubName, ShowResult) : null;
            if (travel != null) side.Add(travel); else side.Add(new VisualElement());
            side.Add(new Button(Embark) { text = "Embark", name = "embark" });
            status = new Label("Docked. Ready to refit.") { name = "status" }; root.Add(status);
            retry = new Button(() => ShowResult(session.PendingSave != null ? session.RetrySave() : session.RetryArrival()))
                { text = "Retry", name = "retry" }; root.Add(retry);
            root.schedule.Execute(() => root.Q<DropdownField>().Focus());
        }

        private static VisualElement Section(VisualElement parent, string title)
        {
            var section = new VisualElement(); section.AddToClassList("section"); parent.Add(section);
            var heading = new Label(title); heading.AddToClassList("heading"); section.Add(heading); return section;
        }
        private static string Title(string value) => System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.Replace('-', ' '));

        private void Embark()
        {
            var valid = loadout.Validation;
            if (!valid.IsSuccess) { ShowResult(valid); return; }
            if (loadout.Dirty) { status.text = "Apply or reset equipment changes before embarking."; status.AddToClassList("error"); return; }
            ShowResult(session.Embark(Guid.NewGuid(), plan()));
        }

        public void ShowResult(RuleResult result)
        {
            status.text = result.IsSuccess ? (session.Lifecycle == Lifecycle.AtSea ? "Voyage started." : "Changes committed.") : Message(result);
            status.EnableInClassList("error", !result.IsSuccess); Refresh();
        }

        private static string Message(RuleResult result)
        {
            switch (result.Error)
            {
                case RuleError.InsufficientBank: return "Not enough banked resources.";
                case RuleError.TierAlreadyOwned: return "This upgrade tier is already owned.";
                case RuleError.InvalidLoadout: return "Invalid loadout: choose different, compatible owned items.";
                case RuleError.SaveFailed: return "Save failed. Your last committed bank and equipment are unchanged. Retry to continue.";
                case RuleError.ArrivalFailed: return "Arrival is not ready. Retry to continue.";
                case RuleError.PrerequisiteMissing: return "Required upgrade or unlock is missing.";
                default: return result.Error + (string.IsNullOrEmpty(result.Detail) ? "" : ": " + result.Detail);
            }
        }

        private void Update()
        {
            if (session != null && (displayedRevision != session.Snapshot.Revision || displayedLock != session.InputLocked)) Refresh();
        }
        public void Refresh()
        {
            var campaign = session.Snapshot.Campaign;
            bank.text = "BANK     " + string.Join("     ", definitions.ResourceWeights.Keys.Select(id => Values.Amount(campaign.Bank, id) + " " + id));
            location.text = (hubName != null ? hubName(campaign.CurrentHub) : Title(campaign.CurrentHub)) + "  /  " + session.Lifecycle;
            if (hubName != null) title.text = hubName(campaign.CurrentHub).ToUpperInvariant();
            travel?.Refresh();
            stats.text = "SHIP\n" + string.Join("\n", session.ShipStats().Select(p => Title(p.Key) + "   " + p.Value.ToString("0.##")));
            unlocks.text = "Unlocks: " + (campaign.Unlocks.Count == 0 ? "None" : string.Join(", ", campaign.Unlocks.Select(Title)));
            foreach (var upgrade in definitions.Upgrades.Values)
            {
                int tier = Values.Amount(campaign.Tiers, upgrade.TrackId);
                bool owned = tier >= upgrade.Tier;
                bool locked = !owned && (tier != upgrade.Tier - 1 || upgrade.RequiredUnlocks.Any(u => !campaign.Unlocks.Contains(u)));
                bool affordable = upgrade.Cost.All(c => Values.Amount(campaign.Bank, c.Key) >= c.Value);
                var button = Root.Q<Button>("buy-" + upgrade.Id);
                button.text = owned ? "Owned" : locked ? "Locked" : "Purchase";
                button.EnableInClassList("owned", owned);
                button.EnableInClassList("unaffordable", !owned && !locked && !affordable);
                Root.Q<Label>("cost-" + upgrade.Id).EnableInClassList("unaffordable", !owned && !affordable);
            }
            actions.SetEnabled(!session.InputLocked && session.Lifecycle == Lifecycle.Docked);
            retry.style.display = session.InputLocked ? DisplayStyle.Flex : DisplayStyle.None;
            loadout.RefreshValidation(); displayedRevision = session.Snapshot.Revision; displayedLock = session.InputLocked;
        }
    }
}
