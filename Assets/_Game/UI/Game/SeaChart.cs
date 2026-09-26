using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace PirateGame.UI.Game
{
    // Plain chart data filled by composition from authored geography and campaign
    // state. The chart only draws it; it never reads gameplay objects.
    public sealed class ChartModel
    {
        public sealed class Region { public string Name; public Rect Bounds; }
        public sealed class Island { public Vector2 Center, Size; }
        // Harbors are the beacons; Claimed = lit.
        public sealed class Harbor { public string Name; public Vector2 Position; public bool Claimed, Current; }
        public sealed class Zone { public string Name; public Vector2 Center; public string Mood; }
        public enum ShapeKind { Land, Shallows, Reef }
        // Closed outline in world XZ.
        public sealed class Shape { public Vector2[] Points; public ShapeKind Kind; }
        public enum LandmarkKind { Gate, Colossus, Hand, Arches, Crown }
        public sealed class Landmark { public string Name; public Vector2 Position; public LandmarkKind Kind; }

        public readonly List<Region> Regions = new List<Region>();
        public readonly List<Island> Islands = new List<Island>();
        public readonly List<Harbor> Harbors = new List<Harbor>();
        public readonly List<Zone> Zones = new List<Zone>();
        public readonly List<Shape> Shapes = new List<Shape>();
        public readonly List<Landmark> Landmarks = new List<Landmark>();
        public readonly List<Vector2> Road = new List<Vector2>();
        // The Veil: the charted limit of the sea (zero size = none).
        public Rect Limits;
        public Vector2 Ship;
        public float ShipYaw;
        public bool ShipVisible;
    }

    // The sea chart (M): an engraved map of the Aurelian reaches on aged paper.
    [RequireComponent(typeof(UIDocument))]
    public sealed class SeaChart : MonoBehaviour
    {
        public StyleSheet stylesheet;
        private VisualElement root;
        private ChartCanvas canvas;
        public bool IsOpen { get; private set; }

        private void OnEnable() => Build();
        private void Start() { if (root == null) Build(); }

        private void Build()
        {
            var docRoot = GetComponent<UIDocument>().rootVisualElement;
            if (docRoot == null) return;
            docRoot.Clear();
            root = new VisualElement { name = "chart", pickingMode = PickingMode.Ignore };
            root.AddToClassList("chart");
            if (stylesheet != null) root.styleSheets.Add(stylesheet);
            var frame = new VisualElement { name = "chart-frame" };
            frame.AddToClassList("chart-frame");
            canvas = new ChartCanvas { name = "chart-canvas" };
            canvas.AddToClassList("chart-canvas");
            var side = new VisualElement { name = "chart-side" };
            side.AddToClassList("chart-side");
            var title = new Label("THE AURELIAN REACHES") { name = "chart-title" };
            title.AddToClassList("chart-title");
            var subtitle = new Label("as charted by the last keepers of the sun") { name = "chart-subtitle" };
            subtitle.AddToClassList("chart-subtitle");
            side.Add(title); side.Add(new SunRule()); side.Add(subtitle);
            side.Add(new CompassRose { name = "chart-compass" });
            var legend = new VisualElement { name = "chart-legend" };
            legend.AddToClassList("chart-legend");
            Legend(legend, LegendIcon.Kind.Lit, "Beacon, relit");
            Legend(legend, LegendIcon.Kind.Unlit, "Beacon, dark");
            Legend(legend, LegendIcon.Kind.Ship, "Your ship");
            Legend(legend, LegendIcon.Kind.Road, "The drowned road");
            Legend(legend, LegendIcon.Kind.Veil, "The Veil: beyond, no one returns");
            side.Add(legend);
            var close = new Label("M or Esc closes the chart") { name = "chart-close" };
            close.AddToClassList("chart-close");
            side.Add(close);
            frame.Add(canvas); frame.Add(side);
            root.Add(frame);
            docRoot.Add(root);
            root.style.display = DisplayStyle.None;
            IsOpen = false;
        }

        private static void Legend(VisualElement parent, LegendIcon.Kind kind, string text)
        {
            var row = new VisualElement(); row.AddToClassList("legend-row");
            row.Add(new LegendIcon(kind));
            var label = new Label(text); label.AddToClassList("legend-text");
            row.Add(label);
            parent.Add(row);
        }

        public void Show(ChartModel model)
        {
            if (root == null) Build();
            if (root == null) return;
            root.style.display = DisplayStyle.Flex;
            IsOpen = true;
            canvas.SetModel(model, true);
        }

        // Cheap per-frame update while open: only the ship layer repaints.
        public void Refresh(ChartModel model)
        {
            if (IsOpen && canvas != null) canvas.SetModel(model, false);
        }

        public void Hide()
        {
            if (root != null) root.style.display = DisplayStyle.None;
            IsOpen = false;
        }

        // Static map layer (repainted on open or resize) plus a ship layer on top.
        private sealed class ChartCanvas : VisualElement
        {
            private static readonly Color LandFill = new Color32(214, 196, 150, 255), ShallowFill = new Color32(120, 168, 160, 70),
                ShipRed = new Color32(150, 34, 22, 255);
            private ChartModel model;
            private readonly VisualElement labels = new VisualElement { pickingMode = PickingMode.Ignore };
            private readonly VisualElement shipLayer = new VisualElement { pickingMode = PickingMode.Ignore };

            public ChartCanvas()
            {
                pickingMode = PickingMode.Ignore;
                generateVisualContent += Draw;
                foreach (var layer in new[] { labels, shipLayer })
                {
                    layer.style.position = Position.Absolute;
                    layer.style.left = 0; layer.style.top = 0; layer.style.right = 0; layer.style.bottom = 0;
                    Add(layer);
                }
                shipLayer.generateVisualContent += DrawShip;
                RegisterCallback<GeometryChangedEvent>(e =>
                {
                    if (e.target != this) return;
                    Relabel(); MarkDirtyRepaint(); shipLayer.MarkDirtyRepaint();
                });
            }

            public void SetModel(ChartModel value, bool full)
            {
                model = value;
                if (full) { Relabel(); MarkDirtyRepaint(); }
                shipLayer.MarkDirtyRepaint();
            }

            // Charted extent: the Veil (plus anything drawn beyond it, such as the
            // Crown), else every region's bounds.
            private Rect World()
            {
                if (model.Limits.width > 0 && model.Limits.height > 0)
                {
                    var world = model.Limits;
                    foreach (var mark in model.Landmarks)
                        world = Rect.MinMaxRect(Mathf.Min(world.xMin, mark.Position.x - 60), Mathf.Min(world.yMin, mark.Position.y - 60),
                            Mathf.Max(world.xMax, mark.Position.x + 60), Mathf.Max(world.yMax, mark.Position.y + 90));
                    float pad = Mathf.Max(world.width, world.height) * 0.04f;
                    return Rect.MinMaxRect(world.xMin - pad, world.yMin - pad, world.xMax + pad, world.yMax + pad);
                }
                var bounds = model.Regions.Count > 0 ? model.Regions[0].Bounds : new Rect(-100, -100, 200, 200);
                foreach (var region in model.Regions)
                    bounds = Rect.MinMaxRect(Mathf.Min(bounds.xMin, region.Bounds.xMin), Mathf.Min(bounds.yMin, region.Bounds.yMin),
                        Mathf.Max(bounds.xMax, region.Bounds.xMax), Mathf.Max(bounds.yMax, region.Bounds.yMax));
                return bounds;
            }

            // World XZ (north up the chart) to local coordinates, preserving aspect.
            private Vector2 Map(Vector2 p, out float scale)
            {
                var world = World();
                var box = contentRect;
                scale = Mathf.Min(box.width / world.width, box.height / world.height);
                float x0 = (box.width - world.width * scale) * 0.5f, y0 = (box.height - world.height * scale) * 0.5f;
                return new Vector2(x0 + (p.x - world.xMin) * scale, y0 + (world.yMax - p.y) * scale);
            }

            private Vector2 Map(Vector2 p) => Map(p, out _);
            private float Scale { get { Map(Vector2.zero, out float s); return s; } }

            private void Relabel()
            {
                labels.Clear();
                if (model == null || contentRect.width < 10) return;
                if (model.Zones.Count > 0)
                    foreach (var zone in model.Zones)
                    {
                        var at = Map(zone.Center);
                        Name(zone.Name.ToUpperInvariant(), at, "chart-zone", true);
                        if (!string.IsNullOrEmpty(zone.Mood)) Name(zone.Mood, at + new Vector2(0, 17), "chart-mood", true);
                    }
                else
                    foreach (var region in model.Regions)
                        Name(region.Name.ToUpperInvariant(), Map(new Vector2(region.Bounds.xMin, region.Bounds.yMax)) + new Vector2(10, 8), "chart-region", false);
                foreach (var mark in model.Landmarks)
                    Name(mark.Name, Map(mark.Position) + new Vector2(0, mark.Kind == ChartModel.LandmarkKind.Crown ? 26 : 13), "chart-landmark", true);
                foreach (var harbor in model.Harbors)
                    Name(harbor.Claimed ? harbor.Name : "Unlit beacon", Map(harbor.Position) + new Vector2(12, -9),
                        harbor.Claimed ? "chart-harbor" : "chart-uncharted", false);
                if (model.Limits.width > 0)
                    Name("THE VEIL", Map(new Vector2(model.Limits.center.x, model.Limits.yMin)) + new Vector2(0, 14), "chart-veil", true);
                var bar = ScaleBar(Scale);
                Name(ScaleMetres + " m", bar.end + new Vector2(6, -11), "chart-scale", false);
            }

            private const float ScaleMetres = 200;

            private (Vector2 start, Vector2 end) ScaleBar(float scale)
            {
                var start = new Vector2(14, contentRect.height - 16);
                return (start, start + new Vector2(ScaleMetres * scale, 0));
            }

            private void Name(string text, Vector2 at, string style, bool centred)
            {
                var label = new Label(text) { pickingMode = PickingMode.Ignore };
                label.AddToClassList("chart-name"); label.AddToClassList(style);
                label.style.position = Position.Absolute;
                label.style.left = at.x; label.style.top = at.y;
                // Centre on the point once the label knows its size.
                if (centred) label.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
                labels.Add(label);
            }

            private void Draw(MeshGenerationContext context)
            {
                if (model == null || contentRect.width < 10) return;
                var paint = context.painter2D;
                paint.lineJoin = LineJoin.Round; paint.lineCap = LineCap.Round;
                float scale = Scale;
                if (model.Limits.width > 0) DrawVeil(paint);
                foreach (var shape in model.Shapes.Where(s => s.Kind == ChartModel.ShapeKind.Shallows)) DrawShallows(paint, shape);
                foreach (var shape in model.Shapes.Where(s => s.Kind == ChartModel.ShapeKind.Land)) DrawRipples(paint, shape.Points, scale);
                foreach (var island in model.Islands) DrawRipples(paint, EllipsePoints(island.Center, island.Size * 0.5f), scale);
                foreach (var shape in model.Shapes.Where(s => s.Kind == ChartModel.ShapeKind.Reef)) DrawReef(paint, shape);
                foreach (var shape in model.Shapes.Where(s => s.Kind == ChartModel.ShapeKind.Land)) DrawLand(paint, shape.Points);
                foreach (var island in model.Islands) DrawLand(paint, EllipsePoints(island.Center, island.Size * 0.5f));
                if (model.Zones.Count == 0) DrawRegionBounds(paint);
                if (model.Road.Count > 1) DrawRoad(paint);
                foreach (var mark in model.Landmarks) DrawLandmark(paint, mark);
                foreach (var harbor in model.Harbors) DrawBeacon(paint, harbor);
                DrawScaleBar(paint, scale);
            }

            // The Veil: cross-hatched margin outside the charted sea, with a double rule.
            private void DrawVeil(Painter2D paint)
            {
                var a = Map(new Vector2(model.Limits.xMin, model.Limits.yMax));
                var b = Map(new Vector2(model.Limits.xMax, model.Limits.yMin));
                var box = contentRect;
                // Faint horizontal sea lines inside the limits.
                for (float y = a.y + 6; y < b.y; y += 9) Ink.Line(paint, new Vector2(a.x + 2, y), new Vector2(b.x - 2, y), Ink.Alpha(Ink.Sea, 0.09f), 0.8f);
                // Diagonal hatching everywhere outside the limits.
                float step = 7;
                for (float k = -box.height; k < box.width + box.height; k += step)
                {
                    var p0 = new Vector2(k, 0); var p1 = new Vector2(k - box.height, box.height);
                    HatchOutside(paint, p0, p1, a, b);
                }
                paint.BeginPath();
                paint.MoveTo(a); paint.LineTo(new Vector2(b.x, a.y)); paint.LineTo(b); paint.LineTo(new Vector2(a.x, b.y)); paint.ClosePath();
                paint.strokeColor = Ink.Black; paint.lineWidth = 2f; paint.Stroke();
                paint.BeginPath();
                paint.MoveTo(a + new Vector2(-4, -4)); paint.LineTo(new Vector2(b.x + 4, a.y - 4)); paint.LineTo(b + new Vector2(4, 4));
                paint.LineTo(new Vector2(a.x - 4, b.y + 4)); paint.ClosePath();
                paint.strokeColor = Ink.Soft; paint.lineWidth = 0.8f; paint.Stroke();
            }

            // Draws the parts of segment p0-p1 outside the rectangle a-b (grown by 5 px),
            // clipping exactly (Liang-Barsky) so the hatching stops cleanly at the rule.
            private static void HatchOutside(Painter2D paint, Vector2 p0, Vector2 p1, Vector2 a, Vector2 b)
            {
                var color = Ink.Alpha(Ink.Black, 0.16f);
                var d = p1 - p0;
                float t0 = 0, t1 = 1;
                bool Clip(float p, float q, ref float lo, ref float hi)
                {
                    if (Mathf.Abs(p) < 1e-6f) return q >= 0;
                    float r = q / p;
                    if (p < 0) { if (r > hi) return false; if (r > lo) lo = r; }
                    else { if (r < lo) return false; if (r < hi) hi = r; }
                    return true;
                }
                bool inside = Clip(-d.x, p0.x - (a.x - 5), ref t0, ref t1) && Clip(d.x, (b.x + 5) - p0.x, ref t0, ref t1)
                    && Clip(-d.y, p0.y - (a.y - 5), ref t0, ref t1) && Clip(d.y, (b.y + 5) - p0.y, ref t0, ref t1);
                if (!inside) { Ink.Line(paint, p0, p1, color, 0.8f); return; }
                if (t0 > 0) Ink.Line(paint, p0, p0 + d * t0, color, 0.8f);
                if (t1 < 1) Ink.Line(paint, p0 + d * t1, p1, color, 0.8f);
            }

            private void DrawShallows(Painter2D paint, ChartModel.Shape shape)
            {
                if (shape.Points == null || shape.Points.Length < 3) return;
                Path(paint, shape.Points);
                paint.fillColor = ShallowFill; paint.Fill();
                Dotted(paint, shape.Points, Ink.Alpha(Ink.Sea, 0.55f), 5, 1.1f);
            }

            // Engraved coastline: offset outlines that fade out to sea.
            private void DrawRipples(Painter2D paint, Vector2[] points, float scale)
            {
                if (points == null || points.Length < 3) return;
                var center = Centroid(points);
                for (int ring = 3; ring >= 1; ring--)
                {
                    float offset = ring * 5.5f / Mathf.Max(0.05f, scale);
                    var ripple = points.Select(p => p + (p - center).normalized * offset).ToArray();
                    Path(paint, ripple);
                    paint.strokeColor = Ink.Alpha(Ink.Sea, 0.34f - ring * 0.07f); paint.lineWidth = 0.9f; paint.Stroke();
                }
            }

            private void DrawLand(Painter2D paint, Vector2[] points)
            {
                if (points == null || points.Length < 3) return;
                Path(paint, points);
                paint.fillColor = LandFill; paint.Fill();
                paint.strokeColor = Ink.Black; paint.lineWidth = 1.3f; paint.Stroke();
            }

            // Reef: a chain of small crosses, the old charts' mark for rocks awash.
            private void DrawReef(Painter2D paint, ChartModel.Shape shape)
            {
                if (shape.Points == null || shape.Points.Length < 2) return;
                foreach (var p in Along(shape.Points.Select(Map).ToArray(), 9, true))
                {
                    Ink.Line(paint, p + new Vector2(-2.2f, 0), p + new Vector2(2.2f, 0), Ink.Soft, 1f);
                    Ink.Line(paint, p + new Vector2(0, -2.2f), p + new Vector2(0, 2.2f), Ink.Soft, 1f);
                }
            }

            private void DrawRegionBounds(Painter2D paint)
            {
                foreach (var region in model.Regions)
                {
                    var a = Map(new Vector2(region.Bounds.xMin, region.Bounds.yMax));
                    var b = Map(new Vector2(region.Bounds.xMax, region.Bounds.yMin));
                    paint.BeginPath(); paint.MoveTo(a); paint.LineTo(new Vector2(b.x, a.y)); paint.LineTo(b); paint.LineTo(new Vector2(a.x, b.y)); paint.ClosePath();
                    paint.strokeColor = Ink.Faint; paint.lineWidth = 1.2f; paint.Stroke();
                }
            }

            // The drowned road: a dashed line with small stones at its bends.
            private void DrawRoad(Painter2D paint)
            {
                var points = model.Road.Select(Map).ToArray();
                for (int i = 1; i < points.Length; i++)
                {
                    var a = points[i - 1]; var b = points[i];
                    float length = Vector2.Distance(a, b);
                    for (float t = 0; t < length; t += 11)
                        Ink.Line(paint, Vector2.Lerp(a, b, t / length), Vector2.Lerp(a, b, Mathf.Min(1, (t + 6) / length)), Ink.Soft, 1.6f);
                }
                foreach (var p in points) Ink.Diamond(paint, p, 2.6f, Ink.Soft);
            }

            private void DrawBeacon(Painter2D paint, ChartModel.Harbor harbor)
            {
                var p = Map(harbor.Position);
                if (harbor.Current)
                {
                    paint.BeginPath(); paint.Arc(p, 13, 0, 360); paint.ClosePath();
                    paint.strokeColor = Ink.Gold; paint.lineWidth = 1.4f; paint.Stroke();
                }
                if (harbor.Claimed) Ink.Sun(paint, p, 5, 12, 4.5f, Ink.GoldLight, Ink.Black, 1.1f);
                else Ink.Sun(paint, p, 4.2f, 8, 3, Ink.Parchment, Ink.Soft, 1f);
            }

            private void DrawLandmark(Painter2D paint, ChartModel.Landmark mark)
            {
                var p = Map(mark.Position);
                var ink = Ink.Black;
                switch (mark.Kind)
                {
                    case ChartModel.LandmarkKind.Gate:
                        // Two pillars and a broken lintel.
                        Block(paint, p + new Vector2(-6, -4), new Vector2(3, 10), ink);
                        Block(paint, p + new Vector2(6, -4), new Vector2(3, 10), ink);
                        Ink.Line(paint, p + new Vector2(-9, -10), p + new Vector2(1, -10), ink, 2f);
                        Ink.Line(paint, p + new Vector2(3, -9), p + new Vector2(9, -11), ink, 2f);
                        break;
                    case ChartModel.LandmarkKind.Colossus:
                        // A standing sun-king holding up a disc.
                        paint.BeginPath(); paint.Arc(p + new Vector2(0, -9), 2.2f, 0, 360); paint.ClosePath();
                        paint.fillColor = ink; paint.Fill();
                        paint.BeginPath();
                        paint.MoveTo(p + new Vector2(-3.5f, -6.5f)); paint.LineTo(p + new Vector2(3.5f, -6.5f));
                        paint.LineTo(p + new Vector2(2.5f, 2)); paint.LineTo(p + new Vector2(-2.5f, 2)); paint.ClosePath();
                        paint.fillColor = ink; paint.Fill();
                        Ink.Line(paint, p + new Vector2(-1.5f, 2), p + new Vector2(-2, 8), ink, 1.6f);
                        Ink.Line(paint, p + new Vector2(1.5f, 2), p + new Vector2(2, 8), ink, 1.6f);
                        Ink.Line(paint, p + new Vector2(3, -6), p + new Vector2(6, -12), ink, 1.4f);
                        Ink.Sun(paint, p + new Vector2(6.5f, -14), 2.2f, 6, 1.6f, Ink.Gold, ink, 0.8f);
                        break;
                    case ChartModel.LandmarkKind.Hand:
                        // An open hand rising from the sea, a disc on its palm.
                        paint.BeginPath();
                        paint.MoveTo(p + new Vector2(-4, 6)); paint.LineTo(p + new Vector2(-4, -3));
                        for (int f = 0; f < 4; f++)
                        {
                            float x = -4 + f * 2.6f;
                            paint.LineTo(p + new Vector2(x, -8 - (f == 1 || f == 2 ? 2 : 0)));
                            paint.LineTo(p + new Vector2(x + 1.8f, -8 - (f == 1 || f == 2 ? 2 : 0)));
                            paint.LineTo(p + new Vector2(x + 2.2f, -3));
                        }
                        paint.LineTo(p + new Vector2(4, 6)); paint.ClosePath();
                        paint.fillColor = ink; paint.Fill();
                        Ink.Sun(paint, p + new Vector2(0, -14), 2.6f, 8, 2, Ink.Gold, ink, 0.8f);
                        Ink.Line(paint, p + new Vector2(-8, 7), p + new Vector2(8, 7), Ink.Alpha(Ink.Sea, 0.6f), 1f);
                        break;
                    case ChartModel.LandmarkKind.Arches:
                        for (int k = -1; k <= 1; k++)
                        {
                            var c = p + new Vector2(k * 7, 0);
                            Block(paint, c + new Vector2(-3, 1), new Vector2(1.6f, 7), ink);
                            Block(paint, c + new Vector2(3, 1), new Vector2(1.6f, 7), ink);
                            paint.BeginPath(); paint.Arc(c + new Vector2(0, -2.5f), 3, 180, 360);
                            paint.strokeColor = ink; paint.lineWidth = 1.6f; paint.Stroke();
                        }
                        break;
                    case ChartModel.LandmarkKind.Crown:
                        // The Drowned Crown: black spires under a turning storm.
                        paint.BeginPath();
                        paint.MoveTo(p + new Vector2(-16, 8));
                        float[] spires = { -12, -18, -9, -22, -11, -16, -8 };
                        for (int s = 0; s < spires.Length; s++)
                        {
                            float x = -14 + s * 28f / (spires.Length - 1);
                            paint.LineTo(p + new Vector2(x - 1.8f, 0));
                            paint.LineTo(p + new Vector2(x, spires[s]));
                            paint.LineTo(p + new Vector2(x + 1.8f, 0));
                        }
                        paint.LineTo(p + new Vector2(16, 8)); paint.ClosePath();
                        paint.fillColor = ink; paint.Fill();
                        for (int turn = 0; turn < 3; turn++)
                        {
                            paint.BeginPath();
                            paint.Arc(p + new Vector2(0, -30), 8 + turn * 7, 200 + turn * 40, 470 + turn * 30);
                            paint.strokeColor = Ink.Alpha(ink, 0.55f - turn * 0.12f); paint.lineWidth = 1.2f; paint.Stroke();
                        }
                        Ink.Line(paint, p + new Vector2(9, -36), p + new Vector2(5, -27), Ink.Oxblood, 1.4f);
                        Ink.Line(paint, p + new Vector2(5, -27), p + new Vector2(10, -24), Ink.Oxblood, 1.4f);
                        break;
                }
            }

            private void DrawScaleBar(Painter2D paint, float scale)
            {
                var (start, end) = ScaleBar(scale);
                Ink.Line(paint, start, end, Ink.Black, 1.4f);
                for (int i = 0; i <= 4; i++)
                {
                    var tick = Vector2.Lerp(start, end, i / 4f);
                    Ink.Line(paint, tick, tick + new Vector2(0, -(i % 2 == 0 ? 6 : 3)), Ink.Black, 1.1f);
                }
            }

            private void DrawShip(MeshGenerationContext context)
            {
                if (model == null || !model.ShipVisible || contentRect.width < 10) return;
                var paint = context.painter2D;
                var p = Map(model.Ship);
                float yaw = model.ShipYaw * Mathf.Deg2Rad;
                Vector2 Point(float angle, float length) => p + new Vector2(Mathf.Sin(yaw + angle), -Mathf.Cos(yaw + angle)) * length;
                paint.fillColor = ShipRed; paint.strokeColor = Ink.Black; paint.lineWidth = 1.4f;
                paint.BeginPath(); paint.MoveTo(Point(0, 12)); paint.LineTo(Point(2.5f, 8)); paint.LineTo(Point(Mathf.PI, 3)); paint.LineTo(Point(-2.5f, 8)); paint.ClosePath();
                paint.Fill(); paint.Stroke();
            }

            private void Path(Painter2D paint, Vector2[] points)
            {
                paint.BeginPath();
                paint.MoveTo(Map(points[0]));
                for (int i = 1; i < points.Length; i++) paint.LineTo(Map(points[i]));
                paint.ClosePath();
            }

            private void Dotted(Painter2D paint, Vector2[] points, Color color, float spacing, float radius)
            {
                foreach (var p in Along(points.Select(Map).ToArray(), spacing, true))
                {
                    paint.BeginPath(); paint.Arc(p, radius * 0.5f, 0, 360); paint.ClosePath();
                    paint.fillColor = color; paint.Fill();
                }
            }

            // Evenly spaced points along a polyline (closed when `loop`), in canvas pixels.
            private static IEnumerable<Vector2> Along(Vector2[] points, float spacing, bool loop)
            {
                float carry = 0;
                int count = loop ? points.Length : points.Length - 1;
                for (int i = 0; i < count; i++)
                {
                    var a = points[i]; var b = points[(i + 1) % points.Length];
                    float length = Vector2.Distance(a, b);
                    if (length <= carry) { carry -= length; continue; }
                    for (float t = carry; t < length; t += spacing) yield return Vector2.Lerp(a, b, t / Mathf.Max(length, 0.0001f));
                    carry = spacing - ((length - carry) % spacing);
                    if (carry >= spacing) carry = 0;
                }
            }

            private static void Block(Painter2D paint, Vector2 center, Vector2 size, Color color)
            {
                paint.BeginPath();
                paint.MoveTo(center + new Vector2(-size.x / 2, -size.y / 2)); paint.LineTo(center + new Vector2(size.x / 2, -size.y / 2));
                paint.LineTo(center + new Vector2(size.x / 2, size.y / 2)); paint.LineTo(center + new Vector2(-size.x / 2, size.y / 2));
                paint.ClosePath(); paint.fillColor = color; paint.Fill();
            }

            private static Vector2[] EllipsePoints(Vector2 center, Vector2 radius)
            {
                var points = new Vector2[20];
                for (int i = 0; i < points.Length; i++)
                {
                    float a = i * Mathf.PI * 2 / points.Length;
                    points[i] = center + new Vector2(Mathf.Cos(a) * radius.x * 0.9f, Mathf.Sin(a) * radius.y * 0.9f);
                }
                return points;
            }

            private static Vector2 Centroid(Vector2[] points)
            {
                var sum = Vector2.zero;
                foreach (var p in points) sum += p;
                return sum / points.Length;
            }
        }

        // An engraved eight-point compass rose with its north mark.
        private sealed class CompassRose : VisualElement
        {
            public CompassRose()
            {
                pickingMode = PickingMode.Ignore;
                AddToClassList("chart-compass");
                generateVisualContent += Draw;
                var north = new Label("N") { pickingMode = PickingMode.Ignore };
                north.AddToClassList("compass-north");
                Add(north);
            }

            private void Draw(MeshGenerationContext context)
            {
                var box = contentRect;
                if (box.width < 10) return;
                var paint = context.painter2D;
                var c = box.center + new Vector2(0, 6);
                float r = Mathf.Min(box.width, box.height) * 0.36f;
                paint.BeginPath(); paint.Arc(c, r * 0.72f, 0, 360); paint.ClosePath();
                paint.strokeColor = Ink.Soft; paint.lineWidth = 1f; paint.Stroke();
                paint.BeginPath(); paint.Arc(c, r * 0.78f, 0, 360); paint.ClosePath();
                paint.strokeColor = Ink.Faint; paint.lineWidth = 0.8f; paint.Stroke();
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4 - Mathf.PI / 2;
                    float length = i % 2 == 0 ? r : r * 0.55f;
                    float width = i % 2 == 0 ? r * 0.16f : r * 0.11f;
                    var tip = c + Ink.Polar(a, length);
                    var left = c + Ink.Polar(a - Mathf.PI / 2, width);
                    var right = c + Ink.Polar(a + Mathf.PI / 2, width);
                    // Each point is half filled, half open, as engraved roses are.
                    paint.BeginPath(); paint.MoveTo(c); paint.LineTo(left); paint.LineTo(tip); paint.ClosePath();
                    paint.fillColor = i == 0 ? Ink.Oxblood : Ink.Black; paint.Fill();
                    paint.BeginPath(); paint.MoveTo(c); paint.LineTo(tip); paint.LineTo(right); paint.ClosePath();
                    paint.fillColor = Ink.Parchment; paint.Fill();
                    paint.strokeColor = Ink.Black; paint.lineWidth = 0.9f; paint.Stroke();
                }
                Ink.Sun(paint, c, r * 0.1f, 8, r * 0.06f, Ink.Gold, Ink.Black, 0.8f);
            }
        }

        private sealed class LegendIcon : VisualElement
        {
            public enum Kind { Lit, Unlit, Ship, Road, Veil }
            private readonly Kind kind;

            public LegendIcon(Kind kind)
            {
                this.kind = kind;
                pickingMode = PickingMode.Ignore;
                AddToClassList("legend-icon");
                generateVisualContent += Draw;
            }

            private void Draw(MeshGenerationContext context)
            {
                var box = contentRect;
                if (box.width < 4) return;
                var paint = context.painter2D;
                var c = box.center;
                switch (kind)
                {
                    case Kind.Lit: Ink.Sun(paint, c, 5, 12, 4.5f, Ink.GoldLight, Ink.Black, 1.1f); break;
                    case Kind.Unlit: Ink.Sun(paint, c, 4.2f, 8, 3, Ink.Parchment, Ink.Soft, 1f); break;
                    case Kind.Ship:
                        paint.fillColor = new Color32(150, 34, 22, 255); paint.strokeColor = Ink.Black; paint.lineWidth = 1.2f;
                        paint.BeginPath(); paint.MoveTo(c + new Vector2(0, -8)); paint.LineTo(c + new Vector2(5, 5));
                        paint.LineTo(c + new Vector2(0, 2)); paint.LineTo(c + new Vector2(-5, 5)); paint.ClosePath();
                        paint.Fill(); paint.Stroke();
                        break;
                    case Kind.Road:
                        for (float x = -10; x < 10; x += 7) Ink.Line(paint, c + new Vector2(x, 0), c + new Vector2(x + 4, 0), Ink.Soft, 1.6f);
                        break;
                    case Kind.Veil:
                        for (float x = -10; x <= 8; x += 4) Ink.Line(paint, c + new Vector2(x, 6), c + new Vector2(x + 6, -6), Ink.Alpha(Ink.Black, 0.5f), 0.9f);
                        break;
                }
            }
        }
    }
}
