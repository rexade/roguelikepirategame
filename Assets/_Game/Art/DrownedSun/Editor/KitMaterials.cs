using UnityEngine;

namespace PirateGame.Art.Editor
{
    // The Drowned Sun palette: bleached limestone, sun-gold and turquoise-friendly
    // sand in the south; obsidian and patina in the north. Colours are sRGB.
    public sealed class KitMaterials
    {
        public readonly Material Limestone, LimestoneShade, Stained, StainedShade, Gold, Patina, Jungle, JungleDark, Trunk,
            Sand, Seabed, Seagrass, ReefCoral, Rock, Obsidian, Coral, Wood, WoodDark, Hull, Iron, Terracotta, Brazier, Ash,
            Flame, FlameCore, Pillar, PillarGlow, Glint, SunSail, WreckerSail, GunboatSail, CorsairSail, Canvas;

        public KitMaterials()
        {
            KitAssets.Folders();
            Limestone = KitAssets.Lit("Limestone", new Color(0.79f, 0.72f, 0.59f), 0.1f);
            LimestoneShade = KitAssets.Lit("Limestone shade", new Color(0.62f, 0.54f, 0.42f), 0.1f);
            // Below the waterline stone carries a green-grey stain: the tide mark of a drowned empire.
            Stained = KitAssets.Lit("Stained limestone", new Color(0.46f, 0.52f, 0.42f), 0.15f);
            StainedShade = KitAssets.Lit("Stained shade", new Color(0.36f, 0.42f, 0.35f), 0.15f);
            // Half metallic: fully metallic gold mirrors the turquoise world and turns lime.
            Gold = KitAssets.Lit("Sun gold", new Color(1f, 0.66f, 0.18f), 0.48f, 0.55f, glow: new Color(0.22f, 0.12f, 0.02f));
            Patina = KitAssets.Lit("Patina bronze", new Color(0.30f, 0.56f, 0.48f), 0.35f, 0.45f);
            Jungle = KitAssets.Lit("Jungle", new Color(0.16f, 0.36f, 0.15f), 0.08f);
            JungleDark = KitAssets.Lit("Jungle dark", new Color(0.09f, 0.24f, 0.11f), 0.08f);
            Trunk = KitAssets.Lit("Palm trunk", new Color(0.56f, 0.43f, 0.29f), 0.08f);
            Sand = KitAssets.Lit("Beach sand", new Color(0.90f, 0.82f, 0.62f), 0.05f);
            Seabed = KitAssets.Lit("Seabed sand", new Color(0.88f, 0.86f, 0.76f), 0.05f);
            // Dark patches on the lagoon floor that read through the water.
            Seagrass = KitAssets.Lit("Seagrass", new Color(0.56f, 0.62f, 0.44f), 0.05f);
            ReefCoral = KitAssets.Lit("Reef coral", new Color(0.80f, 0.40f, 0.36f), 0.2f);
            Rock = KitAssets.Lit("Weathered rock", new Color(0.55f, 0.51f, 0.45f), 0.1f);
            Obsidian = KitAssets.Lit("Obsidian", new Color(0.07f, 0.07f, 0.10f), 0.72f);
            Coral = KitAssets.Lit("Obsidian coral", new Color(0.17f, 0.10f, 0.20f), 0.4f);
            Wood = KitAssets.Lit("Weathered wood", new Color(0.46f, 0.31f, 0.18f), 0.12f);
            WoodDark = KitAssets.Lit("Dark wood", new Color(0.24f, 0.16f, 0.10f), 0.12f);
            Hull = KitAssets.Lit("Hull teal", new Color(0.08f, 0.28f, 0.33f), 0.25f);
            Iron = KitAssets.Lit("Black iron", new Color(0.07f, 0.07f, 0.08f), 0.3f, 0.6f);
            Terracotta = KitAssets.Lit("Terracotta", new Color(0.70f, 0.36f, 0.20f), 0.15f);
            Brazier = KitAssets.Lit("Brazier bronze", new Color(0.46f, 0.30f, 0.14f), 0.4f, 0.45f);
            Ash = KitAssets.Lit("Ash", new Color(0.11f, 0.10f, 0.10f), 0.05f);
            Flame = KitAssets.Unlit("Flame", new Color(3.4f, 1.55f, 0.35f));
            FlameCore = KitAssets.Unlit("Flame core", new Color(4.2f, 3.3f, 1.4f));
            Glint = KitAssets.Unlit("Glint", new Color(6f, 5f, 2.6f), doubleSided: true);
            Pillar = KitAssets.Unlit("Light pillar", new Color(1.5f, 1.05f, 0.5f), additive: true, map: Textures.PillarGradient());
            PillarGlow = KitAssets.Unlit("Light pillar glow", new Color(0.45f, 0.3f, 0.13f), additive: true, map: Textures.PillarGradient());
            SunSail = KitAssets.Lit("Sun sail", Color.white, 0.1f, 0, Textures.Sail("Sun sail", SailStyle.Sun));
            WreckerSail = KitAssets.Lit("Wrecker sail", Color.white, 0.1f, 0, Textures.Sail("Wrecker sail", SailStyle.Wrecker), clip: true);
            GunboatSail = KitAssets.Lit("Gunboat sail", Color.white, 0.1f, 0, Textures.Sail("Gunboat sail", SailStyle.Gunboat), clip: true);
            CorsairSail = KitAssets.Lit("Corsair sail", Color.white, 0.12f, 0, Textures.Sail("Corsair sail", SailStyle.Corsair));
            Canvas = KitAssets.Lit("Canvas", new Color(0.88f, 0.82f, 0.68f), 0.08f);
        }

        // Stone below the waterline takes the stain; everything else keeps its material.
        public Material StainOf(Material material) =>
            material == Limestone ? Stained : material == LimestoneShade ? StainedShade : material == Gold ? Patina : null;
    }
}
