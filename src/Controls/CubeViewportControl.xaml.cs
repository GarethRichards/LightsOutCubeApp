using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace LightsOutCube.Controls
{
    public partial class CubeViewportControl : UserControl
    {
        public static readonly DependencyProperty AxisProperty =
            DependencyProperty.Register(nameof(Axis), typeof(Vector3D), typeof(CubeViewportControl),
                new PropertyMetadata(new Vector3D(0, 1, 0), OnAxisChanged));

        public static readonly DependencyProperty DurationSecondsProperty =
            DependencyProperty.Register(nameof(DurationSeconds), typeof(double), typeof(CubeViewportControl),
                new PropertyMetadata(15.0, OnDurationChanged));

        public static readonly DependencyProperty ScaleProperty =
            DependencyProperty.Register(nameof(Scale), typeof(double), typeof(CubeViewportControl),
                new PropertyMetadata(1.0, OnScaleChanged));

        public Vector3D Axis
        {
            get => (Vector3D)GetValue(AxisProperty);
            set => SetValue(AxisProperty, value);
        }

        public double DurationSeconds
        {
            get => (double)GetValue(DurationSecondsProperty);
            set => SetValue(DurationSecondsProperty, value);
        }

        public double Scale
        {
            get => (double)GetValue(ScaleProperty);
            set => SetValue(ScaleProperty, value);
        }

        private DoubleAnimation _angleAnimation;

        public CubeViewportControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private static void OnAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CubeViewportControl ctl && ctl.PART_Rotation != null)
            {
                ctl.PART_Rotation.Axis = (Vector3D)e.NewValue;
            }
        }

        private static void OnDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CubeViewportControl ctl)
            {
                ctl.RestartAnimation();
            }
        }

        private static void OnScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CubeViewportControl ctl && ctl.PART_Scale != null)
            {
                var s = (double)e.NewValue;
                ctl.PART_Scale.ScaleX = s;
                ctl.PART_Scale.ScaleY = s;
                ctl.PART_Scale.ScaleZ = s;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // apply initial DP values to named parts
            PART_Rotation.Axis = Axis;
            PART_Scale.ScaleX = PART_Scale.ScaleY = PART_Scale.ScaleZ = Scale;

            // start rotation animation
            RestartAnimation();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // stop animation to avoid leaks
            PART_Rotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, null);
            _angleAnimation = null;
        }

        private void RestartAnimation()
        {
            // stop previous
            PART_Rotation?.BeginAnimation(AxisAngleRotation3D.AngleProperty, null);

            _angleAnimation = new DoubleAnimation(0.0, 360.0, TimeSpan.FromSeconds(Math.Max(0.1, DurationSeconds)))
            {
                RepeatBehavior = RepeatBehavior.Forever
            };

            PART_Rotation?.BeginAnimation(AxisAngleRotation3D.AngleProperty, _angleAnimation);
        }
    }
}