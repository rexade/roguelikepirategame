using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PirateGame.Content.Definitions;
using PirateGame.Core;
using PirateGame.Rules.Application;
using PirateGame.UI.Harbor;
using PirateGame.UI.Loadout;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace PirateGame.Tests.T07
{
    public sealed class HarborTests
    {
        private DefinitionCatalog definitions;
        private CampaignSession session;
        private MemoryStore store;
        private ReadyArrival arrival;
        private GameObject go;
        private HarborView view;
        [UnitySetUp] public IEnumerator Setup()
        {
            definitions = AssetDatabase.LoadAssetAtPath<DefinitionCatalogAsset>("Assets/_Game/Content/Progression/HarborCatalog.asset").Freeze();
            var initial = HarborFixture.Initial(definitions); store = new MemoryStore(initial); arrival = new ReadyArrival();
            session = new CampaignSession(definitions, initial, store, arrival); session.RetryArrival();
            go = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/UI/Harbor/Harbor.prefab"));
            view = go.GetComponent<HarborView>(); view.Bind(session, definitions, HarborFixture.Plan);
            yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup() { UnityEngine.Object.DestroyImmediate(go); yield return null; }
        private IEnumerator Click(string name)
        {
            yield return null;
            var button = view.Root.Q<Button>(name);
            Assert.That(button.panel, Is.Not.Null, "Runtime UI must be attached before input");
            button.Focus();
            yield return null;
            Assert.That(button.focusController.focusedElement, Is.SameAs(button));
            using (var ev = NavigationSubmitEvent.GetPooled()) { ev.target = button; button.SendEvent(ev); }
            yield return null;
        }
        [UnityTest] public IEnumerator PurchaseCommandsGrantOnceAndRefreshBothViews()
        {
            var second = new LoadoutView(session, definitions, _ => { });
            yield return Click("buy-harbor-storehouse");
            Assert.That(session.Snapshot.Campaign.Bank["wood"], Is.EqualTo(6));
            Assert.That(session.Snapshot.Campaign.Unlocks, Contains.Item("storehouse"));
            yield return Click("buy-reinforced-hull");
            Assert.That(session.Snapshot.Campaign.Bank["wood"], Is.Zero);
            Assert.That(session.ShipStats()["health"], Is.EqualTo(150));
            Assert.That(session.ShipStats()["cargo"], Is.EqualTo(15));
            Assert.That(view.Root.Q<Label>("stats").text, Does.Contain("150"));
            long revision = session.Snapshot.Revision; yield return Click("buy-reinforced-hull");
            Assert.That(session.Snapshot.Revision, Is.EqualTo(revision));
            Assert.That(view.Root.Q<Label>("status").text, Does.Contain("already owned"));
            Assert.That(second.Validation.IsSuccess, Is.True);
        }
        [UnityTest] public IEnumerator AC03_FiveCannotBuySixAndNothingChanges()
        {
            var initial = HarborFixture.Initial(definitions, 5);
            session = new CampaignSession(definitions, initial, new MemoryStore(initial), arrival); session.RetryArrival();
            view.Bind(session, definitions, HarborFixture.Plan); yield return Click("buy-reinforced-hull");
            Assert.That(session.Snapshot, Is.SameAs(initial));
            Assert.That(view.Root.Q<Label>("bank").text, Does.Contain("5 wood"));
            Assert.That(view.Root.Q<Label>("status").text, Does.Contain("Not enough"));
        }
        [UnityTest] public IEnumerator AC16_DuplicateDraftCannotApplyOrEmbark() => InvalidDraft("dash-01", "dash-01");
        [UnityTest] public IEnumerator AC16_IncompatibleDraftCannotApplyOrEmbark() => InvalidDraft("dash-01", "cannon-01");
        private IEnumerator InvalidDraft(string weapon, string ability)
        {
            var before = session.Snapshot;
            view.Root.Q<DropdownField>("slot-weapon").value = weapon;
            view.Root.Q<DropdownField>("slot-ability").value = ability;
            yield return Click("apply-loadout"); Assert.That(session.Snapshot, Is.SameAs(before));
            yield return Click("embark"); Assert.That(session.Snapshot, Is.SameAs(before));
            Assert.That(view.Root.Q<Label>("status").text, Does.Contain("Invalid loadout"));
        }
        [Test] public void UnownedEquipmentRejectedBySharedCommand()
        {
            var before = session.Snapshot;
            Assert.That(session.SetLoadout(Guid.NewGuid(), new Dictionary<string, string> { ["weapon"] = "unowned", ["ability"] = "dash-01" }).Error, Is.EqualTo(RuleError.InvalidLoadout));
            Assert.That(session.Snapshot, Is.SameAs(before));
        }
        [UnityTest] public IEnumerator PendingDraftRequiresApplyThenEmbarksWithSharedStats()
        {
            view.Root.Q<DropdownField>("slot-weapon").value = "heavy-01";
            yield return Click("embark"); Assert.That(session.Lifecycle, Is.EqualTo(Lifecycle.Docked));
            yield return Click("apply-loadout"); yield return Click("buy-reinforced-hull"); yield return Click("embark");
            Assert.That(session.Snapshot.Expedition.Health, Is.EqualTo(180));
            Assert.That(session.Snapshot.Expedition.Cargo.Count, Is.Zero);
            Assert.That(view.Root.Q<Label>("status").text, Is.EqualTo("Voyage started."));
            Assert.That(view.Root.Q<Button>("embark").enabledInHierarchy, Is.False);
        }
        [UnityTest] public IEnumerator SaveFailureShowsCommittedStateAndRetryGrantsExactlyOnce()
        {
            var before = session.Snapshot; store.Fail = true; yield return Click("buy-reinforced-hull");
            Assert.That(session.Snapshot, Is.SameAs(before));
            Assert.That(view.Root.Q<Label>("bank").text, Does.Contain("11 wood"));
            Assert.That(view.Root.Q<Button>("embark").enabledInHierarchy, Is.False);
            Assert.That(view.Root.Q<Label>("status").text, Does.Contain("Save failed"));
            store.Fail = false; yield return Click("retry");
            Assert.That(session.Snapshot.Campaign.Bank["wood"], Is.EqualTo(5));
            Assert.That(session.ShipStats()["health"], Is.EqualTo(150));
            Assert.That(session.DrainEvents().Count, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator ArrivalFailureKeepsEmbarkLockedUntilRetry()
        {
            arrival.Fail = true; yield return Click("embark");
            Assert.That(session.Lifecycle, Is.EqualTo(Lifecycle.AtSea)); Assert.That(session.InputLocked, Is.True);
            Assert.That(view.Root.Q<Label>("status").text, Does.Contain("Arrival"));
            arrival.Fail = false; yield return Click("retry"); Assert.That(session.InputLocked, Is.False);
            Assert.That(view.Root.Q<Button>("embark").enabledInHierarchy, Is.False);
        }
        [UnityTest] public IEnumerator TwoHarborViewsReadSameCampaignWithoutOwningCopies()
        {
            var other = UnityEngine.Object.Instantiate(go);
            try
            {
                var second = other.GetComponent<HarborView>(); second.Bind(session, definitions, HarborFixture.Plan);
                yield return Click("buy-harbor-storehouse"); second.Refresh();
                Assert.That(second.Root.Q<Label>("bank").text, Is.EqualTo(view.Root.Q<Label>("bank").text));
                Assert.That(second.Root.Q<Label>("unlocks").text, Does.Contain("Storehouse"));
                Assert.That(second.Root.Q<Button>("buy-harbor-storehouse").text, Is.EqualTo("Owned"));
            }
            finally { UnityEngine.Object.DestroyImmediate(other); }
        }
    }
}
