using System;
using System.Linq;
using PirateGame.Core;
using PirateGame.Rules.Application;
using PirateGame.UI.Loadout;
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
        private Label bank, stats, status, location, unlocks;
        private VisualElement actions;
        private Button retry;
        private LoadoutView loadout;
        private long displayedRevision = -1;
        private bool displayedLock;
        public VisualElement Root => GetComponent<UIDocument>().rootVisualElement;

        public void Bind(CampaignSession owner, DefinitionCatalog catalog, Func<EmbarkPlan> embarkPlan)
        {
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
            header.Add(new Label("HOMEWARD HARBOR") { name = "title" });
            location = new Label(); header.Add(location);
            bank = new Label { name = "bank" }; root.Add(bank);
            actions = new VisualElement(); actions.AddToClassList("columns"); root.Add(actions);
            var equipment = Section(actions, "Equipment");
            loadout = new LoadoutView(session, definitions, ShowResult); equipment.Add(loadout);
            stats = new Label { name = "stats" }; equipment.Add(stats);
            var upgrades = Section(actions, "Harbor & ship");
            foreach (var upgrade in definitions.Upgrades.Values)
            {
                var row = new VisualElement(); row.AddToClassList("upgrade"); upgrades.Add(row);
                row.Add(new Label(Title(upgrade.Id)) { name = "name-" + upgrade.Id });
                row.Add(new Label(string.Join("  /  ", upgrade.Cost.Select(p => p.Value + " " + p.Key))) { name = "cost-" + upgrade.Id });
                var effects = string.Join(", ", upgrade.Modifiers.Select(m =>
                    (m.Value >= 0 ? "+" : "") + (m.Operation == ModifierOperation.Percent ? (m.Value * 100).ToString("0.#") + "%" : m.Value.ToString("0.#")) + " " + m.StatId));
                row.Add(new Label(effects) { name = "effect-" + upgrade.Id });
                row.Add(new Button(() => ShowResult(session.PurchaseUpgrade(Guid.NewGuid(), upgrade.Id)))
                    { text = "Purchase", name = "buy-" + upgrade.Id });
            }
            unlocks = new Label { name = "unlocks" }; upgrades.Add(unlocks);
            actions.Add(new Button(Embark) { text = "Embark", name = "embark" });
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
            location.text = Title(campaign.CurrentHub) + "  /  " + session.Lifecycle;
            stats.text = "SHIP\n" + string.Join("\n", session.ShipStats().Select(p => Title(p.Key) + "   " + p.Value.ToString("0.##")));
            unlocks.text = "Unlocks: " + (campaign.Unlocks.Count == 0 ? "None" : string.Join(", ", campaign.Unlocks.Select(Title)));
            foreach (var upgrade in definitions.Upgrades.Values)
                Root.Q<Button>("buy-" + upgrade.Id).text = Values.Amount(campaign.Tiers, upgrade.TrackId) >= upgrade.Tier ? "Owned" : "Purchase";
            actions.SetEnabled(!session.InputLocked && session.Lifecycle == Lifecycle.Docked);
            retry.style.display = session.InputLocked ? DisplayStyle.Flex : DisplayStyle.None;
            loadout.RefreshValidation(); displayedRevision = session.Snapshot.Revision; displayedLock = session.InputLocked;
        }
    }
}
