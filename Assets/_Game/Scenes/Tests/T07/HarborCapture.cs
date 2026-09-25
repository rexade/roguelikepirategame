using System.Collections;
using System.IO;
using PirateGame.Core;
using PirateGame.Rules.Application;
using UnityEngine;

namespace PirateGame.Tests.T07
{
    // Opt-in final-state evidence. Record the visible window externally to include UI.
    public sealed class HarborCapture : MonoBehaviour
    {
        public CampaignSession session;
        private IEnumerator Start()
        {
            string directory = Path.GetFullPath("docs/evidence/T07/interaction");
            Directory.CreateDirectory(directory);
            UnityEngine.Application.targetFrameRate = 60;
            for (int frame = 0; frame < 1200; frame++)
            {
                yield return null;
                if (session.Lifecycle == Lifecycle.AtSea && !session.InputLocked)
                {
                    File.WriteAllText(Path.Combine(directory, "final-state.txt"),
                        "Resolution: " + Screen.width + "x" + Screen.height + "\nRevision: " + session.Snapshot.Revision +
                        "\nWood: " + Values.Amount(session.Snapshot.Campaign.Bank, "wood") +
                        "\nIron: " + Values.Amount(session.Snapshot.Campaign.Bank, "iron") +
                        "\nHealth: " + session.Snapshot.Expedition.Health + "\nCargo capacity: " + session.ShipStats()["cargo"]);
                    yield return new WaitForSecondsRealtime(2); UnityEngine.Application.Quit(); yield break;
                }
                yield return new WaitForSecondsRealtime(0.5f);
            }
            UnityEngine.Application.Quit();
        }
    }
}
