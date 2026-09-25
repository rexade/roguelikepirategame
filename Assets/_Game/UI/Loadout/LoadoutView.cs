using System;
using System.Collections.Generic;
using System.Linq;
using PirateGame.Core;
using PirateGame.Rules.Application;
using UnityEngine.UIElements;

namespace PirateGame.UI.Loadout
{
    public sealed class LoadoutView : VisualElement
    {
        private readonly CampaignSession session;
        private readonly Dictionary<string, string> draft;
        private readonly Dictionary<string, DropdownField> fields = new Dictionary<string, DropdownField>();
        private readonly Label validation = new Label();
        public RuleResult Validation => session.ValidateLoadout(draft);
        public bool Dirty => draft.Any(p => !session.Snapshot.Campaign.Loadout.TryGetValue(p.Key, out var value) || value != p.Value);

        public LoadoutView(CampaignSession session, DefinitionCatalog definitions, Action<RuleResult> completed)
        {
            this.session = session;
            draft = Values.Copy(session.Snapshot.Campaign.Loadout);
            var owned = session.Snapshot.Campaign.OwnedEquipment;
            var instances = owned.Keys.ToList();
            foreach (var slot in definitions.Hulls[session.Snapshot.Campaign.HullId].Slots)
            {
                var field = new DropdownField(slot.Key.Replace('-', ' '), instances, instances.IndexOf(draft[slot.Key]),
                    id => owned[id].Replace('-', ' '), id => owned[id].Replace('-', ' '));
                field.name = "slot-" + slot.Key;
                field.RegisterValueChangedCallback(e => { draft[slot.Key] = e.newValue; RefreshValidation(); });
                fields.Add(slot.Key, field); Add(field);
            }
            validation.name = "loadout-validation"; validation.AddToClassList("muted"); Add(validation);
            Add(new Button(() => { var result = session.SetLoadout(Guid.NewGuid(), draft); completed(result); RefreshValidation(); })
                { text = "Apply equipment", name = "apply-loadout" });
            Add(new Button(ResetDraft) { text = "Reset selection", name = "reset-loadout" });
            RefreshValidation();
        }

        public void RefreshValidation()
        {
            validation.text = !Validation.IsSuccess ? "Invalid loadout: each slot needs a different, compatible owned item."
                : Dirty ? "Equipment changes pending" : "Equipment ready";
            validation.EnableInClassList("error", !Validation.IsSuccess);
        }

        public void ResetDraft()
        {
            foreach (var pair in session.Snapshot.Campaign.Loadout) { draft[pair.Key] = pair.Value; fields[pair.Key].SetValueWithoutNotify(pair.Value); }
            RefreshValidation();
        }
    }
}
