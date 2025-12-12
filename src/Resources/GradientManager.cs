using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LightsOutCube.Resources
{
    public static class GradientManager
    {
        private static readonly Random _rand = new Random();

        // Keys must match the keys in Resources/Gradients.xaml
        private static readonly string[] _gradientKeys =
        {
            "Gradient_DuskGlow",
            "Gradient_SunsetCoral",
            "Gradient_DeepOcean",
            "Gradient_PurpleCosmos",
            "Gradient_EmeraldTeal",
            "Gradient_WarmSunrise",
            "Gradient_AnimatedSky"
        };

        // Pick a random gradient from application resources (or optionally a provided ResourceDictionary)
        public static LinearGradientBrush PickRandomGradient(ResourceDictionary dictionary = null)
        {
            var dict = dictionary ?? Application.Current.Resources;
            var available = _gradientKeys.Where(k => dict.Contains(k)).ToArray();
            if (available.Length == 0)
                return null;

            var key = available[_rand.Next(available.Length)];
            return dict[key] as LinearGradientBrush;
        }

        // Animate the host Panel.Background gradient stops to the target brush colors.
        // If host.Background is not a LinearGradientBrush it will be replaced by a clone of targetBrush.
        public static void AnimateBackgroundToBrush(Panel host, LinearGradientBrush targetBrush, int durationMs = 800)
        {
            if (host == null || targetBrush == null)
                return;

            // Ensure we operate on a mutable clone
            var target = targetBrush.Clone();
            target.Freeze(); // safe to keep frozen; we only read colors from it

            if (!(host.Background is LinearGradientBrush current))
            {
                // No existing gradient — set target immediately (or set a clone so future animations can run)
                host.Background = targetBrush.Clone();
                return;
            }

            // animate matching gradient stops; if counts differ, add missing stops
            int min = Math.Min(current.GradientStops.Count, target.GradientStops.Count);

            var duration = new Duration(TimeSpan.FromMilliseconds(durationMs));
            var easing = new QuadraticEase { EasingMode = EasingMode.EaseOut };

            // Animate existing stops
            for (int i = 0; i < min; i++)
            {
                var toColor = target.GradientStops[i].Color;
                var anim = new ColorAnimation(toColor, duration) { EasingFunction = easing };
                current.GradientStops[i].BeginAnimation(GradientStop.ColorProperty, anim);
            }

            // If target has additional stops, append them and animate from last color
            if (target.GradientStops.Count > current.GradientStops.Count)
            {
                var lastColor = current.GradientStops[current.GradientStops.Count - 1].Color;
                for (int i = min; i < target.GradientStops.Count; i++)
                {
                    var gs = new GradientStop(lastColor, target.GradientStops[i].Offset);
                    current.GradientStops.Add(gs);
                    var anim = new ColorAnimation(target.GradientStops[i].Color, duration) { EasingFunction = easing };
                    gs.BeginAnimation(GradientStop.ColorProperty, anim);
                }
            }
            // If current has extra stops, animate them to the last target color
            else if (current.GradientStops.Count > target.GradientStops.Count)
            {
                var finalColor = target.GradientStops[current.GradientStops.Count - 1].Color;
                for (int i = min; i < current.GradientStops.Count; i++)
                {
                    var anim = new ColorAnimation(finalColor, duration) { EasingFunction = easing };
                    current.GradientStops[i].BeginAnimation(GradientStop.ColorProperty, anim);
                }
            }
        }

        // Convenience: pick a random gradient from resources and animate host to it
        public static void AnimateBackgroundToRandomGradient(Panel host, ResourceDictionary dictionary = null, int durationMs = 800)
        {
            var dict = dictionary ?? 
                (Application.Current.Resources["MergedGradients"] as ResourceDictionary).MergedDictionaries[0];
            var brush = PickRandomGradient(dict);
            if (brush == null)
                return;

            // Ensure we call on UI thread if not already
            if (!host.Dispatcher.CheckAccess())
            {
                host.Dispatcher.Invoke(() => AnimateBackgroundToBrush(host, brush, durationMs));
                return;
            }

            AnimateBackgroundToBrush(host, brush, durationMs);
        }
    }
}