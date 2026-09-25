using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace PirateGame.Bootstrap.Tests
{
    public sealed class KeyboardNavigationTests
    {
        private Keyboard keyboard;

        [UnityTest]
        public IEnumerator KeyboardFocusAndSceneNavigation()
        {
            // Runtime UI intentionally ignores navigation in an unfocused application.
            float deadline = Time.realtimeSinceStartup + 30;
            while (!Application.isFocused && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Application.isFocused, Is.True, "Run in a focused Windows player or Game view");
            keyboard = InputSystem.AddDevice<Keyboard>("T01VerificationKeyboard");
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            for (int i = 0; i < 5; i++) yield return null;
            Assert.That(FocusedButton(), Is.EqualTo("Open ocean"), "Initial keyboard focus");

            yield return Press(Key.Tab);
            Assert.That(FocusedButton(), Is.EqualTo("Quit"), "Tab advances focus");
            yield return Press(Key.LeftShift, Key.Tab);
            Assert.That(FocusedButton(), Is.EqualTo("Open ocean"), "Shift+Tab reverses focus");
            yield return Press(Key.Enter);
            for (int i = 0; i < 10; i++) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("WaterTest"));
            Assert.That(FocusedButton(), Is.EqualTo("Back"), "Focus after opening ocean");

            yield return Press(Key.Enter);
            for (int i = 0; i < 10; i++) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Bootstrap"));
            Assert.That(FocusedButton(), Is.EqualTo("Open ocean"), "Focus after returning");
        }

        private IEnumerator Press(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            for (int i = 0; i < 3; i++) yield return null;
            Assert.That(keyboard[keys[keys.Length - 1]].isPressed, Is.True, "Injected keyboard event reached Input System");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            for (int i = 0; i < 3; i++) yield return null;
        }

        private static string FocusedButton()
        {
            var document = Object.FindFirstObjectByType<UIDocument>();
            Assert.That(document, Is.Not.Null);
            return (document.rootVisualElement.focusController.focusedElement as Button)?.text;
        }

        [TearDown]
        public void RemoveVerificationKeyboard()
        {
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        }
    }
}
