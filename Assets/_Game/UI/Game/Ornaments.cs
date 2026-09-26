using UnityEngine;
using UnityEngine.UIElements;

namespace PirateGame.UI.Game
{
    // The Drowned Sun's paper palette: parchment, iron-gall ink, gold leaf.
    public static class Ink
    {
        public static readonly Color Parchment = new Color32(232, 220, 192, 255);
        public static readonly Color ParchmentDark = new Color32(206, 186, 146, 255);
        public static readonly Color Black = new Color32(43, 33, 24, 255);
        public static readonly Color Soft = new Color32(43, 33, 24, 150);
        public static readonly Color Faint = new Color32(43, 33, 24, 70);
        public static readonly Color Gold = new Color32(184, 137, 43, 255);
        public static readonly Color GoldLight = new Color32(226, 184, 96, 255);
        public static readonly Color Oxblood = new Color32(142, 42, 28, 255);
        public static readonly Color Sea = new Color32(46, 93, 102, 255);
        public static readonly Color SeaWash = new Color32(92, 150, 150, 60);
        public static readonly Color Deep = new Color32(11, 35, 48, 255);

        public static Color Alpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

        // A sun disc with triangular rays; the symbol of drowned Aurelia.
        public static void Sun(Painter2D paint, Vector2 center, float radius, int rays, float rayLength, Color fill, Color line, float lineWidth = 1.2f)
        {
            paint.lineJoin = LineJoin.Miter;
            for (int i = 0; i < rays; i++)
            {
                float a = (i + 0.5f) * Mathf.PI * 2 / rays - Mathf.PI / 2;
                float half = Mathf.PI / rays * 0.45f;
                paint.BeginPath();
                paint.MoveTo(center + Polar(a - half, radius * 1.02f));
                paint.LineTo(center + Polar(a, radius + rayLength));
                paint.LineTo(center + Polar(a + half, radius * 1.02f));
                paint.ClosePath();
                paint.fillColor = fill; paint.Fill();
                paint.strokeColor = line; paint.lineWidth = lineWidth * 0.8f; paint.Stroke();
            }
            paint.BeginPath();
            paint.Arc(center, radius, 0, 360);
            paint.ClosePath();
            paint.fillColor = fill; paint.Fill();
            paint.strokeColor = line; paint.lineWidth = lineWidth; paint.Stroke();
            // Inner engraved ring.
            paint.BeginPath();
            paint.Arc(center, radius * 0.62f, 0, 360);
            paint.ClosePath();
            paint.strokeColor = Alpha(line, line.a * 0.7f); paint.lineWidth = lineWidth * 0.7f; paint.Stroke();
        }

        public static Vector2 Polar(float angle, float length) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * length;

        public static void Line(Painter2D paint, Vector2 a, Vector2 b, Color color, float width)
        {
            paint.BeginPath(); paint.MoveTo(a); paint.LineTo(b);
            paint.strokeColor = color; paint.lineWidth = width; paint.Stroke();
        }

        public static void Diamond(Painter2D paint, Vector2 center, float radius, Color color)
        {
            paint.BeginPath();
            paint.MoveTo(center + new Vector2(0, -radius)); paint.LineTo(center + new Vector2(radius, 0));
            paint.LineTo(center + new Vector2(0, radius)); paint.LineTo(center + new Vector2(-radius, 0));
            paint.ClosePath(); paint.fillColor = color; paint.Fill();
        }
    }

    // A gilded rule with a small sun at its centre, used under titles and headings.
    public sealed class SunRule : VisualElement
    {
        public Color Color { get; set; } = Ink.Gold;
        public Color Line { get; set; } = Ink.Black;

        public SunRule()
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList("sun-rule");
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext context)
        {
            var box = contentRect;
            if (box.width < 4 || box.height < 4) return;
            var paint = context.painter2D;
            float y = box.height * 0.5f, cx = box.width * 0.5f, r = Mathf.Min(box.height * 0.26f, 7);
            float gap = r * 2.4f;
            Ink.Line(paint, new Vector2(6, y), new Vector2(cx - gap, y), Color, 1.4f);
            Ink.Line(paint, new Vector2(cx + gap, y), new Vector2(box.width - 6, y), Color, 1.4f);
            Ink.Line(paint, new Vector2(18, y + 3), new Vector2(cx - gap - 6, y + 3), Ink.Alpha(Line, 0.35f), 0.8f);
            Ink.Line(paint, new Vector2(cx + gap + 6, y + 3), new Vector2(box.width - 18, y + 3), Ink.Alpha(Line, 0.35f), 0.8f);
            Ink.Diamond(paint, new Vector2(5, y), 3, Color);
            Ink.Diamond(paint, new Vector2(box.width - 5, y), 3, Color);
            Ink.Sun(paint, new Vector2(cx, y), r, 10, r * 0.9f, Color, Line, 1f);
        }
    }

    // Title logo: the sun half sunk below a horizon line, its reflection broken
    // into ripples. Drawn as vectors so it stays crisp at any resolution.
    public sealed class SunLogo : VisualElement
    {
        public SunLogo()
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList("sun-logo");
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext context)
        {
            var box = contentRect;
            if (box.width < 10 || box.height < 10) return;
            var paint = context.painter2D;
            float horizon = box.height * 0.62f, cx = box.width * 0.5f;
            float radius = Mathf.Min(box.height * 0.36f, box.width * 0.16f);
            // Rays fan out above the horizon only.
            const int rays = 17;
            for (int i = 0; i < rays; i++)
            {
                float a = Mathf.PI + (i + 0.5f) * Mathf.PI / rays;
                float half = Mathf.PI / rays * 0.32f;
                float length = radius * (i % 2 == 0 ? 0.85f : 0.55f);
                paint.BeginPath();
                paint.MoveTo(new Vector2(cx, horizon) + Ink.Polar(a - half, radius * 1.1f));
                paint.LineTo(new Vector2(cx, horizon) + Ink.Polar(a, radius * 1.1f + length));
                paint.LineTo(new Vector2(cx, horizon) + Ink.Polar(a + half, radius * 1.1f));
                paint.ClosePath();
                paint.fillColor = i % 2 == 0 ? Ink.GoldLight : Ink.Gold; paint.Fill();
                paint.strokeColor = Ink.Black; paint.lineWidth = 1.2f; paint.Stroke();
            }
            // Upper half of the disc.
            paint.BeginPath();
            paint.Arc(new Vector2(cx, horizon), radius, 180, 360);
            paint.ClosePath();
            paint.fillColor = Ink.GoldLight; paint.Fill();
            paint.strokeColor = Ink.Black; paint.lineWidth = 2f; paint.Stroke();
            paint.BeginPath();
            paint.Arc(new Vector2(cx, horizon), radius * 0.64f, 180, 360);
            paint.strokeColor = Ink.Alpha(Ink.Black, 0.55f); paint.lineWidth = 1.2f; paint.Stroke();
            // Horizon rule running out to both edges.
            Ink.Line(paint, new Vector2(box.width * 0.04f, horizon), new Vector2(box.width * 0.96f, horizon), Ink.Black, 2.2f);
            Ink.Diamond(paint, new Vector2(box.width * 0.04f, horizon), 4, Ink.Gold);
            Ink.Diamond(paint, new Vector2(box.width * 0.96f, horizon), 4, Ink.Gold);
            // The drowned half: shortening ripples of reflected gold under the line.
            for (int i = 1; i <= 5; i++)
            {
                float y = horizon + i * radius * 0.2f;
                float half = radius * (1.05f - i * 0.16f);
                float wobble = (i % 2 == 0 ? 1 : -1) * radius * 0.06f;
                Ink.Line(paint, new Vector2(cx - half + wobble, y), new Vector2(cx + half + wobble, y), Ink.Alpha(Ink.Gold, 1 - i * 0.14f), 2.4f - i * 0.25f);
            }
        }
    }
}
