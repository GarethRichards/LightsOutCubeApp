using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace LightsOutCube.Utilities
{
    public static class Effects
    {
        public static void StartConfetti(FrameworkElement owner, Action<UIElement> addElement = null, Action<UIElement> removeElement = null)
        {
            try
            {
                var rand = new Random();
                double width = Math.Max(400, owner.ActualWidth);
                double height = Math.Max(180, owner.ActualHeight);

                int pieces = 28;
                var colors = new[] { Colors.Yellow, Colors.Orange, Colors.Red, Colors.LimeGreen, Colors.Cyan, Colors.Magenta, Colors.Gold };

                for (int i = 0; i < pieces; i++)
                {
                    var size = rand.Next(8, 18);
                    var rect = new Rectangle
                    {
                        Width = size,
                        Height = size,
                        Fill = new SolidColorBrush(colors[rand.Next(colors.Length)]),
                        RenderTransform = new RotateTransform(rand.NextDouble() * 360)
                    };

                    double startX = rand.NextDouble() * width;
                    Canvas.SetLeft(rect, startX);
                    Canvas.SetTop(rect, -rand.Next(10, 120));

                    if (addElement == null)
                        continue;

                    addElement(rect);

                    var fall = new DoubleAnimation
                    {
                        From = Canvas.GetTop(rect),
                        To = height + rand.Next(40, 180),
                        Duration = TimeSpan.FromMilliseconds(1200 + rand.Next(0, 1000)),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                    };

                    var sway = new DoubleAnimation
                    {
                        From = startX,
                        To = startX + (rand.NextDouble() - 0.5) * 160,
                        Duration = TimeSpan.FromMilliseconds(1000 + rand.Next(0, 1200)),
                        AutoReverse = false
                    };

                    var rot = new DoubleAnimation
                    {
                        From = 0,
                        To = 360 * (rand.Next(1, 4)),
                        Duration = TimeSpan.FromMilliseconds(1000 + rand.Next(0, 1200))
                    };

                    var tt = new TranslateTransform();
                    rect.RenderTransform = new TransformGroup
                    {
                        Children = new TransformCollection { new RotateTransform(0), tt }
                    };

                    var topAnim = new DoubleAnimation
                    {
                        From = Canvas.GetTop(rect),
                        To = height + 200,
                        Duration = fall.Duration,
                        EasingFunction = fall.EasingFunction
                    };

                    Storyboard.SetTarget(topAnim, rect);
                    Storyboard.SetTargetProperty(topAnim, new PropertyPath(path: "(Canvas.Top)"));

                    Storyboard.SetTarget(sway, rect);
                    Storyboard.SetTargetProperty(sway, new PropertyPath(path: "(Canvas.Left)"));

                    Storyboard.SetTarget(rot, rect);
                    Storyboard.SetTargetProperty(rot, new PropertyPath(path: "RenderTransform.Children[0].Angle"));

                    var sb = new Storyboard();
                    sb.Children.Add(topAnim);
                    sb.Children.Add(sway);
                    sb.Children.Add(rot);

                    sb.BeginTime = TimeSpan.FromMilliseconds(rand.Next(0, 400));
                    sb.Completed += (s, e) => { try { removeElement?.Invoke(rect); } catch { /* ignore */   } };
                    sb.Begin();
                }
            }
            catch { /* best-effort */ }
        }

        public static void StartRibbons(FrameworkElement owner, Action<UIElement> addElement)
        {
            try
            {
                var rand = new Random();
                int ribbons = 3;
                var colors = new[] { Colors.DeepSkyBlue, Colors.LightPink, Colors.Gold };
                double width = Math.Max(400, owner.ActualWidth);
                double height = Math.Max(160, owner.ActualHeight);

                for (int r = 0; r < ribbons; r++)
                {
                    var poly = new Polyline
                    {
                        Stroke = new SolidColorBrush(colors[r % colors.Length]),
                        StrokeThickness = 6,
                        Opacity = 0.0,
                        StrokeLineJoin = PenLineJoin.Round
                    };

                    var points = Enumerable.Range(0, 7).Select(i =>
                    {
                        double x = (i / 6.0) * width;
                        double y = height * 0.2 + Math.Sin((i + r) * 1.2) * (20 + r * 8);
                        return new Point(x, y);
                    }).ToArray();

                    foreach (var p in points) poly.Points.Add(p);
                    if (addElement == null) continue;
                    addElement(poly);

                    var fadeIn = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(400)) { BeginTime = TimeSpan.FromMilliseconds(200 + r * 120) };
                    var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(800)) { BeginTime = TimeSpan.FromMilliseconds(1600 + r * 200) };

                    var sb = new Storyboard();
                    Storyboard.SetTarget(fadeIn, poly);
                    Storyboard.SetTargetProperty(fadeIn, new PropertyPath(path: "Opacity"));
                    Storyboard.SetTarget(fadeOut, poly);
                    Storyboard.SetTargetProperty(fadeOut, new PropertyPath(path: "Opacity"));
                    sb.Children.Add(fadeIn);
                    sb.Children.Add(fadeOut);
                    sb.Begin();
                }
            }
            catch { /* Ignore errors */ }
        }

        // Simple particle-style fireworks: several bursts at or near center that emit particles
        public static void StartFireworks(FrameworkElement owner, Action<UIElement> addElement = null, Action<UIElement> removeElement = null, int bursts = 3)
        {
            try
            {
                var rand = new Random();
                double width = Math.Max(400, owner.ActualWidth);
                double height = Math.Max(180, owner.ActualHeight);

                // center of bursts (slightly above center)
                double cx = width * 0.5;
                double cy = height * 0.35;

                var colors = new[] { Colors.Gold, Colors.OrangeRed, Colors.Cyan, Colors.Magenta, Colors.LimeGreen, Colors.SkyBlue };

                for (int b = 0; b < bursts; b++)
                {
                    int particles = 18 + rand.Next(12);
                    for (int i = 0; i < particles; i++)
                    {
                        // particle visual
                        var ellipse = new Ellipse
                        {
                            Width = 6,
                            Height = 6,
                            Fill = new SolidColorBrush(colors[rand.Next(colors.Length)]),
                            Opacity = 1.0,
                            RenderTransform = new TranslateTransform()
                        };

                        // start at center
                        Canvas.SetLeft(ellipse, cx - ellipse.Width / 2);
                        Canvas.SetTop(ellipse, cy - ellipse.Height / 2);

                        if (addElement == null) continue;
                        addElement(ellipse);

                        // direction
                        double angle = rand.NextDouble() * Math.PI * 2.0;
                        double speed = 80 + rand.NextDouble() * 140; // pixels

                        double tx = cx + Math.Cos(angle) * speed;
                        double ty = cy + Math.Sin(angle) * speed;

                        // animations: X (Canvas.Left), Y (Canvas.Top), scale (via Width/Height) and fade
                        var leftAnim = new DoubleAnimation(Canvas.GetLeft(ellipse), tx - ellipse.Width / 2, TimeSpan.FromMilliseconds(600 + rand.Next(400))) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
                        var topAnim = new DoubleAnimation(Canvas.GetTop(ellipse), ty - ellipse.Height / 2, TimeSpan.FromMilliseconds(600 + rand.Next(400))) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
                        var fade = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(700 + rand.Next(400))) { BeginTime = TimeSpan.FromMilliseconds(100) };
                        var scaleW = new DoubleAnimation(ellipse.Width, ellipse.Width * 0.6, TimeSpan.FromMilliseconds(700 + rand.Next(300)));
                        var scaleH = new DoubleAnimation(ellipse.Height, ellipse.Height * 0.6, TimeSpan.FromMilliseconds(700 + rand.Next(300)));

                        // apply animations via storyboard
                        Storyboard.SetTarget(leftAnim, ellipse);
                        Storyboard.SetTargetProperty(leftAnim, new PropertyPath(path: "(Canvas.Left)"));
                        Storyboard.SetTarget(topAnim, ellipse);
                        Storyboard.SetTargetProperty(topAnim, new PropertyPath(path: "(Canvas.Top)"));
                        Storyboard.SetTarget(fade, ellipse);
                        Storyboard.SetTargetProperty(fade, new PropertyPath(path: "Opacity"));

                        // Width/Height animations require targeting the element itself
                        Storyboard.SetTarget(scaleW, ellipse);
                        Storyboard.SetTargetProperty(scaleW, new PropertyPath(path: "Width"));
                        Storyboard.SetTarget(scaleH, ellipse);
                        Storyboard.SetTargetProperty(scaleH, new PropertyPath(path: "Height"));

                        var sb = new Storyboard();
                        sb.Children.Add(leftAnim);
                        sb.Children.Add(topAnim);
                        sb.Children.Add(fade);
                        sb.Children.Add(scaleW);
                        sb.Children.Add(scaleH);

                        // Stagger particle start times per-burst without blocking the UI thread.
                        var burstOffset = TimeSpan.FromMilliseconds(b * 180);
                        var particleOffset = TimeSpan.FromMilliseconds(rand.Next(0, 300));
                        sb.BeginTime = burstOffset + particleOffset;
                        sb.Completed += (s, e) => { try { removeElement?.Invoke(ellipse); } catch { /* Ignore */ } };
                        sb.Begin();
                    }

                    // next burst will be offset by burstOffset above
                }
            }
            catch { /* best-effort */ }
        }
    }
}
