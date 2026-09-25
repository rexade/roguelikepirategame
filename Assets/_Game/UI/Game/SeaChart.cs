using System;
using System.Collections.Generic;
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
        public sealed class Harbor { public string Name; public Vector2 Position; public bool Claimed, Current; }
        public readonly List<Region> Regions = new List<Region>();
        public readonly List<Island> Islands = new List<Island>();
        public readonly List<Harbor> Harbors = new List<Harbor>();
        public Vector2 Ship;
        public float ShipYaw;
        public bool ShipVisible;
    }

    // Parchment sea chart of the fixed archipelago, opened with M.
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
            var title = new Label("SEA CHART") { name = "chart-title" };
            title.AddToClassList("chart-title");
            canvas = new ChartCanvas { name = "chart-canvas" };
            canvas.AddToClassList("chart-canvas");
            var legend = new Label("Red arrow: your ship   ·   Orange: harbors flying your flag   ·   ?: uncharted harbor        M or Esc closes")
                { name = "chart-legend" };
            legend.AddToClassList("chart-legend");
            frame.Add(title); frame.Add(canvas); frame.Add(legend);
            root.Add(frame);
            docRoot.Add(root);
            root.style.display = DisplayStyle.None;
            IsOpen = false;
        }

        public void Show(ChartModel model)
        {
            if (root == null) Build();
            if (root == null) return;
            root.style.display = DisplayStyle.Flex;
            IsOpen = true;
            canvas.SetModel(model, true);
        }

        // Cheap per-frame update while open: only the ship marker moves.
        public void Refresh(ChartModel model)
        {
            if (IsOpen && canvas != null) canvas.SetModel(model, false);
        }

        public void Hide()
        {
            if (root != null) root.style.display = DisplayStyle.None;
            IsOpen = false;
        }

        private sealed class ChartCanvas : VisualElement
        {
            private static readonly Color Sea = new Color(0.8f, 0.74f, 0.58f), Ink = new Color(0.27f, 0.2f, 0.12f),
                Land = new Color(0.55f, 0.62f, 0.38f), Sand = new Color(0.88f, 0.8f, 0.57f), Claimed = new Color(0.85f, 0.45f, 0.1f),
                Uncharted = new Color(0.45f, 0.4f, 0.32f), Ship = new Color(0.75f, 0.12f, 0.08f);
            private ChartModel model;
            private readonly VisualElement labels = new VisualElement { pickingMode = PickingMode.Ignore };

            public ChartCanvas()
            {
                pickingMode = PickingMode.Ignore;
                generateVisualContent += Draw;
                labels.style.position = Position.Absolute;
                labels.style.left = 0; labels.style.top = 0; labels.style.right = 0; labels.style.bottom = 0;
                Add(labels);
                RegisterCallback<GeometryChangedEvent>(_ => Relabel());
            }

            public void SetModel(ChartModel value, bool relabel)
            {
                model = value;
                if (relabel) Relabel();
                MarkDirtyRepaint();
            }

            private Rect World()
            {
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
                scale = Mathf.Min(box.width / world.width, box.height / world.height) * 0.94f;
                float x0 = (box.width - world.width * scale) * 0.5f, y0 = (box.height - world.height * scale) * 0.5f;
                return new Vector2(x0 + (p.x - world.xMin) * scale, y0 + (world.yMax - p.y) * scale);
            }

            private Vector2 Map(Vector2 p) => Map(p, out _);

            private void Relabel()
            {
                labels.Clear();
                if (model == null || contentRect.width < 10) return;
                foreach (var region in model.Regions)
                    Name(region.Name.ToUpperInvariant(), Map(new Vector2(region.Bounds.xMin, region.Bounds.yMax)) + new Vector2(10, 8), "chart-region");
                foreach (var harbor in model.Harbors)
                    Name(harbor.Claimed ? harbor.Name : "?", Map(harbor.Position) + new Vector2(11, -10), harbor.Claimed ? "chart-harbor" : "chart-uncharted");
                Name("N", new Vector2(contentRect.width - 40, contentRect.height - 86), "chart-north");
            }

            private void Name(string text, Vector2 at, string style)
            {
                var label = new Label(text) { pickingMode = PickingMode.Ignore };
                label.AddToClassList("chart-name"); label.AddToClassList(style);
                label.style.position = Position.Absolute; label.style.left = at.x; label.style.top = at.y;
                labels.Add(label);
            }

            private void Draw(MeshGenerationContext context)
            {
                if (model == null || contentRect.width < 10) return;
                var paint = context.painter2D;
                int shade = 0;
                foreach (var region in model.Regions)
                {
                    var a = Map(new Vector2(region.Bounds.xMin, region.Bounds.yMax));
                    var b = Map(new Vector2(region.Bounds.xMax, region.Bounds.yMin));
                    paint.fillColor = new Color(Sea.r - 0.04f * shade, Sea.g - 0.03f * shade, Sea.b - 0.01f * shade, 1); shade++;
                    paint.BeginPath(); paint.MoveTo(a); paint.LineTo(new Vector2(b.x, a.y)); paint.LineTo(b); paint.LineTo(new Vector2(a.x, b.y)); paint.ClosePath();
                    paint.Fill();
                    paint.strokeColor = new Color(Ink.r, Ink.g, Ink.b, 0.5f); paint.lineWidth = 1.5f; paint.Stroke();
                }
                Map(Vector2.zero, out float scale);
                foreach (var island in model.Islands)
                {
                    var center = Map(island.Center);
                    Ellipse(paint, center, island.Size.x * 0.62f * scale, island.Size.y * 0.62f * scale, Sand);
                    Ellipse(paint, center, island.Size.x * 0.45f * scale, island.Size.y * 0.45f * scale, Land);
                }
                foreach (var harbor in model.Harbors)
                {
                    var p = Map(harbor.Position);
                    paint.strokeColor = harbor.Claimed ? Ink : Uncharted; paint.lineWidth = 2;
                    paint.BeginPath(); paint.Arc(p, harbor.Current ? 8 : 6, 0, 360); paint.ClosePath();
                    if (harbor.Claimed) { paint.fillColor = Claimed; paint.Fill(); }
                    paint.Stroke();
                }
                if (model.ShipVisible)
                {
                    var p = Map(model.Ship);
                    float yaw = model.ShipYaw * Mathf.Deg2Rad;
                    Vector2 Point(float angle, float length) => p + new Vector2(Mathf.Sin(yaw + angle), -Mathf.Cos(yaw + angle)) * length;
                    paint.fillColor = Ship; paint.strokeColor = Ink; paint.lineWidth = 1.5f;
                    paint.BeginPath(); paint.MoveTo(Point(0, 12)); paint.LineTo(Point(2.5f, 8)); paint.LineTo(Point(Mathf.PI, 3)); paint.LineTo(Point(-2.5f, 8)); paint.ClosePath();
                    paint.Fill(); paint.Stroke();
                }
                var rose = new Vector2(contentRect.width - 34, contentRect.height - 40);
                paint.strokeColor = Ink; paint.lineWidth = 1.5f;
                paint.BeginPath(); paint.MoveTo(rose + new Vector2(0, 18)); paint.LineTo(rose + new Vector2(0, -18)); paint.Stroke();
                paint.fillColor = Ink;
                paint.BeginPath(); paint.MoveTo(rose + new Vector2(0, -24)); paint.LineTo(rose + new Vector2(5, -12)); paint.LineTo(rose + new Vector2(-5, -12)); paint.ClosePath(); paint.Fill();
            }

            private static void Ellipse(Painter2D paint, Vector2 center, float rx, float ry, Color color)
            {
                paint.fillColor = color;
                paint.BeginPath();
                for (int i = 0; i <= 24; i++)
                {
                    float a = i * Mathf.PI * 2 / 24;
                    var point = center + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
                    if (i == 0) paint.MoveTo(point); else paint.LineTo(point);
                }
                paint.ClosePath(); paint.Fill();
            }
        }
    }
}
