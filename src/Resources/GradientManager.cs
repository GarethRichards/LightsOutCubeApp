using System;
using System.Diagnostics;
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

            host.Background = targetBrush.Clone();


        }

        // Convenience: pick a random gradient from resources and animate host to it
        public static void AnimateBackgroundToRandomGradient(Panel host, ResourceDictionary dictionary = null, int durationMs = 800)
        {
            try
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
            catch (Exception ex)
            {
                Debug.Write($"Failed to load new gradent {ex.Message} {ex.StackTrace}");
            }

        }
    }
}