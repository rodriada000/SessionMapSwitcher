using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace SessionModManagerAvalonia.Classes
{
    public class GridBackground : Control
    {
        public double GridSpacing { get; set; } = 40.0;
        public IBrush GridBrush { get; set; } = Brushes.LightGray;
        public double GridThickness { get; set; } = 1.0;

        public override void Render(DrawingContext context)
        {
            var pen = new Pen(GridBrush, GridThickness);
            double width = Bounds.Width;
            double height = Bounds.Height;

            // Draw vertical lines
            for (double x = 0; x < width; x += GridSpacing)
            {
                context.DrawLine(pen, new Point(x, 0), new Point(x, height));
            }

            // Draw horizontal lines
            for (double y = 0; y < height; y += GridSpacing)
            {
                context.DrawLine(pen, new Point(0, y), new Point(width, y));
            }
        }
    }
}
