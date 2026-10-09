using WeekPlan;

namespace WeekPlan.App.Ui;

public sealed class LineChartView : GraphicsView
{
    private readonly ChartDrawable _drawable = new();
    private bool _listens;

    public LineChartView()
    {
        Drawable = _drawable;
        BackgroundColor = Colors.Transparent;
        HeightRequest = 180;
    }

    public event Action<int>? PointChosen;

    public void EnablePointTap()
    {
        if (_listens)
            return;
        _listens = true;
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, args) =>
        {
            var position = args.GetPosition(this);
            if (position is null || Width <= 0 || Height <= 0)
                return;
            var index = _drawable.Hit(position.Value.X, position.Value.Y, Width, Height);
            if (index >= 0)
                PointChosen?.Invoke(index);
        };
        GestureRecognizers.Add(tap);
    }

    public void SetPoints(
        IReadOnlyList<double> values,
        string minLabel,
        string maxLabel,
        string startLabel,
        string endLabel,
        Color line)
    {
        if (_drawable.Same(values, minLabel, maxLabel, startLabel, endLabel, line))
            return;
        _drawable.Set(values, minLabel, maxLabel, startLabel, endLabel, line);
        Invalidate();
    }

    private sealed class ChartDrawable : IDrawable
    {
        private double[] _values = Array.Empty<double>();
        private string _min = "";
        private string _max = "";
        private string _start = "";
        private string _end = "";
        private Color _line = Color.FromArgb("#22C55E");

        public bool Same(
            IReadOnlyList<double> values,
            string minLabel,
            string maxLabel,
            string startLabel,
            string endLabel,
            Color line)
        {
            if (_min != minLabel || _max != maxLabel || _start != startLabel || _end != endLabel || !SameColor(_line, line))
                return false;
            if (_values.Length != values.Count)
                return false;
            for (var index = 0; index < _values.Length; index++)
            {
                if (Math.Abs(_values[index] - values[index]) > 0.0000001)
                    return false;
            }

            return true;
        }

        private static bool SameColor(Color left, Color right) =>
            Math.Abs(left.Red - right.Red) < 0.001
            && Math.Abs(left.Green - right.Green) < 0.001
            && Math.Abs(left.Blue - right.Blue) < 0.001
            && Math.Abs(left.Alpha - right.Alpha) < 0.001;

        public void Set(
            IReadOnlyList<double> values,
            string minLabel,
            string maxLabel,
            string startLabel,
            string endLabel,
            Color line)
        {
            _values = values.ToArray();
            _min = minLabel;
            _max = maxLabel;
            _start = startLabel;
            _end = endLabel;
            _line = line;
        }

        public int Hit(double x, double y, double width, double height)
        {
            var anchors = ChartGeometry.Layout(width, height, _values);
            return ChartGeometry.HitIndex(anchors, x, y, 22);
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (_values.Length == 0)
                return;

            var anchors = ChartGeometry.Layout(dirtyRect.Width, dirtyRect.Height, _values);
            var first = anchors[0];
            var last = anchors[^1];
            canvas.StrokeColor = Color.FromArgb("#E6E8EE");
            canvas.StrokeSize = 1;
            canvas.DrawLine((float)first.X, dirtyRect.Height - (float)ChartGeometry.Bottom, (float)last.X, dirtyRect.Height - (float)ChartGeometry.Bottom);

            canvas.StrokeColor = _line;
            canvas.StrokeSize = 2;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Round;
            for (var index = 1; index < anchors.Count; index++)
            {
                var previous = anchors[index - 1];
                var current = anchors[index];
                canvas.DrawLine((float)previous.X, (float)previous.Y, (float)current.X, (float)current.Y);
            }

            var radius = _values.Length > 40 ? 3f : 5f;
            canvas.FillColor = _line;
            foreach (var anchor in anchors)
                canvas.FillCircle((float)anchor.X, (float)anchor.Y, radius);

            canvas.FontColor = Color.FromArgb("#8E95A3");
            canvas.FontSize = 11;
            canvas.DrawString(_max, 2, 0, 40, 16, HorizontalAlignment.Left, VerticalAlignment.Center);
            canvas.DrawString(_min, 2, dirtyRect.Height - (float)ChartGeometry.Bottom - 16, 40, 16, HorizontalAlignment.Left, VerticalAlignment.Center);
            if (anchors.Count == 1)
            {
                canvas.DrawString(_start, (float)first.X - 40, dirtyRect.Height - 22, 80, 18, HorizontalAlignment.Center, VerticalAlignment.Center);
                return;
            }

            canvas.DrawString(_start, (float)first.X, dirtyRect.Height - 22, 88, 18, HorizontalAlignment.Left, VerticalAlignment.Center);
            canvas.DrawString(_end, (float)last.X - 88, dirtyRect.Height - 22, 88, 18, HorizontalAlignment.Right, VerticalAlignment.Center);
        }
    }
}
