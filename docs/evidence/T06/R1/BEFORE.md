# Before/after regression

Before.xml and Before.log record one failing startup test with a
NullReferenceException in SalvageFixture.HudText. The original OnGUI cargo/status
expression was extracted unchanged into HudText, and OnGUI was wired to use it.
The failing run had no null guard. To reproduce on this revision, temporarily
remove `result == null ? "" : ` from the status expression, run RunUnity.ps1
-Mode Before, then restore that guard before running -Mode Tests. Use a separate
evidence directory to preserve these logs.

The passing revision adds that null guard. Tests.xml includes the same startup
test, which loads the real scene twice, verifies LastResult remains null, checks
the displayed string, unchanged cargo/sources, and retained embark event.
This headless test is not claimed as visible repaint evidence.

Tests-initial.xml/log preserve the first expanded-suite run: startup passed;
the new cargo-status test failed because an interpolated Rigidbody teleport did
not immediately update the child interaction transform. The test now explicitly
synchronizes the transform at its test positions. No gameplay assertion was
removed or weakened. Final Tests.xml/log supersede that setup failure.
