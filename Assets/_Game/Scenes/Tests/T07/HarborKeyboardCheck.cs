using System;
using System.Collections;
using System.IO;
using PirateGame.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;

namespace PirateGame.Tests.T07
{
    public sealed class HarborKeyboardCheck : MonoBehaviour
    {
        public HarborFixture fixture;
        private Keyboard keyboard;
        private string log;
        private IEnumerator Start()
        {
            string directory = Path.GetFullPath("docs/evidence/T07");
            log = Path.Combine(directory, "keyboard-check.txt");
            File.WriteAllText(log, "Waiting for keyboard-start.txt; Input System device-state injection, not OS keys.\n");
            while (!File.Exists(Path.Combine(directory, "keyboard-start.txt"))) yield return null;
            Check(UnityEngine.Application.isFocused, "Application focused");
            keyboard = InputSystem.AddDevice<Keyboard>("T07VerificationKeyboard");
            var root = fixture.view.Root;
            root.Q<DropdownField>("slot-weapon").Focus();
            yield return Press(Key.Tab);
            Check(root.focusController.focusedElement == root.Q<DropdownField>("slot-ability"), "Tab advances focus");
            yield return Press(Key.LeftShift, Key.Tab);
            Check(root.focusController.focusedElement == root.Q<DropdownField>("slot-weapon"), "Shift+Tab reverses focus");
            yield return Press(Key.Enter); yield return Press(Key.DownArrow); yield return Press(Key.DownArrow); yield return Press(Key.Enter);
            Check(root.Q<DropdownField>("slot-weapon").value == "heavy-01", "Enter / Down / Down / Enter selects heavy cannon");
            yield return Press(Key.Tab); yield return Press(Key.Tab); yield return Press(Key.Enter);
            Check(fixture.Session.Snapshot.Campaign.Loadout["weapon"] == "heavy-01", "Keyboard applies loadout");
            yield return Press(Key.Tab); yield return Press(Key.Tab); yield return Press(Key.Enter);
            Check(Values.Amount(fixture.Session.Snapshot.Campaign.Tiers, "harbor") == 1, "Keyboard buys storehouse");
            yield return Press(Key.Enter);
            Check(root.Q<Label>("status").text.Contains("already owned"), "Keyboard repeated-tier error");
            yield return Press(Key.Tab); yield return Press(Key.Enter);
            Check(fixture.Session.ShipStats()["health"] == 180, "Keyboard buys hull; computed health 180");
            yield return Press(Key.Tab); yield return Press(Key.Enter);
            Check(fixture.Session.Lifecycle == Lifecycle.AtSea, "Keyboard embarks");
            File.AppendAllText(log, "PASS: keyboard route complete\n");
        }
        private IEnumerator Press(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            for (int frame = 0; frame < 3; frame++) yield return null;
            Check(keyboard[keys[keys.Length - 1]].isPressed, "Input received: " + keys[keys.Length - 1]);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSecondsRealtime(0.65f);
        }
        private void Check(bool condition, string description)
        {
            File.AppendAllText(log, (condition ? "PASS: " : "FAIL: ") + description + "\n");
            if (!condition) throw new InvalidOperationException(description);
        }
        private void OnDestroy() { if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard); }
    }
}
