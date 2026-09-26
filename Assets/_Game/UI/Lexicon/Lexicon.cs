using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PirateGame.UI.Lexicon
{
    // Player-facing words for the Drowned Sun. Rule IDs never change (they live in
    // saves); only what the player reads does. Unknown IDs fall back to title case.
    //
    // Inside a PirateGame.UI.* namespace the name `Lexicon` resolves to this
    // namespace, so UI code uses an alias: `using Words = PirateGame.UI.Lexicon.Lexicon;`.
    public static class Lexicon
    {
        private static readonly Dictionary<string, string> Resources = new Dictionary<string, string>
        { ["wood"] = "timber", ["iron"] = "bronze" };

        private static readonly Dictionary<string, string> Upgrades = new Dictionary<string, string>
        {
            ["harbor-storehouse"] = "Beacon Storehouse",
            ["shipwright-slip"] = "Shipwright's Slip",
            ["reinforced-hull"] = "Reinforced Hull",
            ["iron-bound-guns"] = "Bronze-bound Guns",
            ["navigators-charts"] = "Beacon Paths"
        };

        private static readonly Dictionary<string, string> UpgradeLines = new Dictionary<string, string>
        {
            ["harbor-storehouse"] = "Room below the beacon for a larger hold.",
            ["shipwright-slip"] = "A slipway to trim the hull for speed.",
            ["reinforced-hull"] = "Bronze ribs against wrecker shot.",
            ["iron-bound-guns"] = "Guns bound in salvaged bronze strike harder.",
            ["navigators-charts"] = "Sail straight to any beacon you have relit."
        };

        private static readonly Dictionary<string, string> Unlocks = new Dictionary<string, string>
        { ["storehouse"] = "Beacon Storehouse", ["fast-travel"] = "Beacon Paths" };

        private static readonly Dictionary<string, string> Stats = new Dictionary<string, string>
        { ["health"] = "Hull", ["cargo"] = "Hold", ["speed"] = "Speed", ["damage-scale"] = "Gun power" };

        private static readonly Dictionary<string, string> Enemies = new Dictionary<string, string>
        { ["raider"] = "Wrecker skiff", ["gunner"] = "Eclipse gunboat", ["corsair"] = "Black corsair" };

        private static readonly Dictionary<string, string> Equipment = new Dictionary<string, string>
        { ["cannon"] = "Cannon", ["repeater"] = "Repeater", ["brace"] = "Brace" };

        // Resource display order: the rules' order (timber, bronze), then anything else.
        private static readonly string[] ResourceOrder = { "wood", "iron" };

        public static string Resource(string id) => Lookup(Resources, id, Plain(id));
        public static string ResourceTitle(string id) => Capitalize(Resource(id));
        public static string Upgrade(string id) => Lookup(Upgrades, id, Title(id));
        public static string UpgradeLine(string id) => Lookup(UpgradeLines, id, "");
        public static string Unlock(string id) => Lookup(Unlocks, id, Title(id));
        public static string Stat(string id) => Lookup(Stats, id, Title(id));
        public static string Enemy(string id) => Lookup(Enemies, id, Title(id));
        public static string EquipmentName(string id) => Lookup(Equipment, id, Title(id));

        // Salvage kinds as the player names them: relics (formerly barrels) and wrecks.
        public static string Salvage(string definitionId) =>
            definitionId == "relic" || definitionId == "barrel" ? "relic" : definitionId == "wreck" ? "wreck" : Plain(definitionId);

        // "3 timber, 1 bronze", or "nothing" when the bundle is empty.
        public static string Describe(IReadOnlyDictionary<string, int> bundle)
        {
            if (bundle == null) return "nothing";
            var parts = bundle.Where(p => p.Value > 0)
                .OrderBy(p => Order(p.Key)).ThenBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => p.Value.ToString(CultureInfo.InvariantCulture) + " " + Resource(p.Key)).ToArray();
            return parts.Length == 0 ? "nothing" : string.Join(", ", parts);
        }

        // "iron-bound-guns" -> "Iron Bound Guns".
        public static string Title(string id) =>
            string.IsNullOrEmpty(id) ? "" : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Plain(id));

        private static int Order(string id)
        {
            int index = Array.IndexOf(ResourceOrder, id);
            return index < 0 ? ResourceOrder.Length : index;
        }

        private static string Plain(string id) => (id ?? "").Replace('-', ' ');
        private static string Capitalize(string text) => string.IsNullOrEmpty(text) ? "" : char.ToUpperInvariant(text[0]) + text.Substring(1);
        private static string Lookup(Dictionary<string, string> table, string id, string fallback) =>
            id != null && table.TryGetValue(id, out var value) ? value : fallback;
    }
}
