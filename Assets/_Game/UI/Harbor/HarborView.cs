using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PirateGame.Core;
using PirateGame.Rules.Application;
using PirateGame.UI.Game;
using PirateGame.UI.Loadout;
using PirateGame.UI.Travel;
using UnityEngine;
using UnityEngine.UIElements;
using Words = PirateGame.UI.Lexicon.Lexicon;

namespace PirateGame.UI.Harbor
{
    // The beacon's ledger: stores, the ship, the beacon works (upgrades), beacon
    // paths (fast travel) and departure. It only issues session commands.
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
        // Production binding (a hub namer) uses the Drowned Sun's words; the T07
        // fixture keeps raw rule IDs so its assertions stay about the rules.
        private bool Themed => hubName != null;

        // hubName enables production naming and the beacon-path (fast-travel) section (T09).
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
            var ledger = new VisualElement { name = "ledger" }; ledger.AddToClassList("ledger"); root.Add(ledger);
            var header = new VisualElement(); header.AddToClassList("header"); ledger.Add(header);
            title = new Label("DAWNREST BEACON") { name = "title" }; header.Add(title);
            header.Add(new SunRule { name = "title-rule" });
            location = new Label { name = "location" }; header.Add(location);
            bank = new Label { name = "bank" }; ledger.Add(bank);
            actions = new VisualElement(); actions.AddToClassList("columns"); ledger.Add(actions);
            var equipment = Section(actions, "The Ship");
            loadout = new LoadoutView(session, definitions, ShowResult); equipment.Add(loadout);
            stats = new Label { name = "stats" }; equipment.Add(stats);
            var upgrades = Section(actions, "Beacon Works"); upgrades.AddToClassList("upgrades");
            // Tracks in tier order; each row: name and effect, a line of lore, cost, then the button.
            foreach (var upgrade in definitions.Upgrades.Values.OrderBy(u => u.TrackId, StringComparer.Ordinal).ThenBy(u => u.Tier))
            {
                var row = new VisualElement(); row.AddToClassList("upgrade"); upgrades.Add(row);
                var info = new VisualElement(); info.AddToClassList("upgrade-info"); row.Add(info);
                var heading = new VisualElement(); heading.AddToClassList("upgrade-heading"); info.Add(heading);
                var name = new Label(UpgradeName(upgrade.Id)) { name = "name-" + upgrade.Id }; name.AddToClassList("upgrade-name"); heading.Add(name);
                var effects = string.Join(", ", upgrade.Modifiers.Select(m =>
                    (m.Value >= 0 ? "+" : "") + (m.Operation == ModifierOperation.Percent ? (m.Value * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%" : m.Value.ToString("0.#", CultureInfo.InvariantCulture))
                    + " " + StatName(m.StatId, true)));
                var effect = new Label(effects) { name = "effect-" + upgrade.Id }; effect.AddToClassList("upgrade-effect"); heading.Add(effect);
                string lore = Themed ? Words.UpgradeLine(upgrade.Id) : "";
                if (lore.Length > 0) { var line = new Label(lore) { name = "lore-" + upgrade.Id }; line.AddToClassList("upgrade-line"); info.Add(line); }
                var cost = new Label(Themed ? Bundle(upgrade.Cost, " · ") : string.Join("  /  ", upgrade.Cost.Select(p => p.Value + " " + p.Key))) { name = "cost-" + upgrade.Id };
                // Cost above its button on the right keeps every row to two lines.
                var buy = new VisualElement(); buy.AddToClassList("upgrade-buy"); row.Add(buy);
                cost.AddToClassList("upgrade-cost"); buy.Add(cost);
                buy.Add(new Button(() => ShowResult(session.PurchaseUpgrade(Guid.NewGuid(), upgrade.Id)))
                    { text = "Purchase", name = "buy-" + upgrade.Id });
            }
            unlocks = new Label { name = "unlocks" }; upgrades.Add(unlocks);
            // Third column: beacon paths (production only) above the departure button.
            var side = new VisualElement(); side.AddToClassList("side"); actions.Add(side);
            travel = hubName != null ? new TravelView(session, definitions, hubName, ShowResult) : null;
            if (travel != null) side.Add(travel); else side.Add(new VisualElement());
            side.Add(new Button(Embark) { text = "Set sail", name = "embark" });
            var footer = new VisualElement(); footer.AddToClassList("footer"); ledger.Add(footer);
            status = new Label("Docked. Ready to refit.") { name = "status" }; footer.Add(status);
            retry = new Button(() => ShowResult(session.PendingSave != null ? session.RetrySave() : session.RetryArrival()))
                { text = "Retry", name = "retry" }; footer.Add(retry);
            root.schedule.Execute(() => root.Q<DropdownField>()?.Focus());
        }

        private static VisualElement Section(VisualElement parent, string title)
        {
            var section = new VisualElement(); section.AddToClassList("section"); parent.Add(section);
            var heading = new Label(title); heading.AddToClassList("heading"); section.Add(heading); return section;
        }
        private static string Title(string value) => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.Replace('-', ' '));
        private string UpgradeName(string id) => Themed ? Words.Upgrade(id) : Title(id);
        private string StatName(string id, bool lower) => Themed ? (lower ? Words.Stat(id).ToLowerInvariant() : Words.Stat(id)) : (lower ? id : Title(id));
        private string Bundle(IEnumerable<KeyValuePair<string, int>> bundle, string separator) =>
            string.Join(separator, bundle.Select(p => p.Value + " " + (Themed ? Words.Resource(p.Key) : p.Key)));

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
                case RuleError.ArrivalFailed:
                    return result.Detail != null && result.Detail.StartsWith("Charting") ? "Charting the way…" : "Arrival is not ready. Retry to continue.";
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
            bank.text = Themed
                ? "STORES     " + string.Join("     ·     ", definitions.ResourceWeights.Keys.Select(id => Values.Amount(campaign.Bank, id) + " " + Words.Resource(id)))
                : "BANK     " + string.Join("     ", definitions.ResourceWeights.Keys.Select(id => Values.Amount(campaign.Bank, id) + " " + id));
            location.text = Themed ? (session.Lifecycle == Lifecycle.Docked ? "Moored beneath the beacon" : "At sea") : Title(campaign.CurrentHub) + "  /  " + session.Lifecycle;
            if (hubName != null) title.text = hubName(campaign.CurrentHub).ToUpperInvariant();
            travel?.Refresh();
            stats.text = (Themed ? "" : "SHIP\n") + string.Join("\n", session.ShipStats().Select(p => StatName(p.Key, false) + "   " + p.Value.ToString("0.##", CultureInfo.InvariantCulture)));
            unlocks.text = (Themed ? "Granted: " : "Unlocks: ") + (campaign.Unlocks.Count == 0 ? "None" : string.Join(", ", campaign.Unlocks.Select(u => Themed ? Words.Unlock(u) : Title(u))));
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
