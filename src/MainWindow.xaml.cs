using _3DTools;
using LightsOutCube.Resources;
using LightsOutCube.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace LightsOutCube
{
    public partial class MainWindow : Window
    {
        double CubeScale = 0.14;
        CubeViewModel ViewModel = new CubeViewModel();
        Dictionary<GeometryModel3D, int> cubes = new Dictionary<GeometryModel3D, int>();
        Dictionary<int, GeometryModel3D> cubesByIndex = new Dictionary<int, GeometryModel3D>();
        // map index -> translate transform for animation
        private readonly Dictionary<int, TranslateTransform3D> cubeTranslateByIndex = new Dictionary<int, TranslateTransform3D>();

        int[] model = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 39, 36, 33, 38, 35, 32, 37, 34, 31, 53, 52, 51, 56, 55, 54, 59, 58, 57, 23, 26, 29, 22, 25, 28, 21, 24, 27, 49, 46, 43, 48, 45, 42, 47, 44, 41, 11, 12, 13, 14, 15, 16, 17, 18, 19 };

        Trackball trackball;

        private Material _yellowMaterial;
        private Material _defaultMaterial;

        // store handlers so we can unsubscribe on unload
        private readonly Dictionary<int, PropertyChangedEventHandler> _cellHandlers = new Dictionary<int, PropertyChangedEventHandler>();

        // how far a button moves toward the center when pressed (tweak as needed)
        private const double PressInset = 0.12;

        private readonly Random _rand = new Random();

        // Guard to prevent the solved celebration being started multiple times concurrently
        private bool _celebrationRunning = false;

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new CubeViewModel();
            DataContext = ViewModel;

            // listen to ViewModel property changes for solved notification
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            // cleanup on unload to avoid leaks
            this.Unloaded += OnUnloaded;
        }

        private void StartUpEffect()
        {
            try
            {
                var startupAxis = new Vector3D(0, 0, 1);
                var startupRotation = new AxisAngleRotation3D(startupAxis, 0);
                var startupTransform = new RotateTransform3D(startupRotation, new Point3D(0, 0, 0));
                // insert the startup transform at the beginning so it composes with future transforms
                myTransformGroup.Children.Insert(0, startupTransform);

                var startupAnim = new DoubleAnimation(-90, new Duration(TimeSpan.FromMilliseconds(600)))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                startupRotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, startupAnim);
            }
            catch
            {
                // best-effort: ignore if transform group unavailable
            }
        }
        public void OnLoaded(Object sender, System.Windows.RoutedEventArgs args)
        {
            trackball = new Trackball(myTransformGroup);
            trackball.EventSource = CubeViewport;

            _yellowMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Yellow));
            _defaultMaterial = (Material)System.Windows.Application.Current.Resources["myFunkyMaterial"];

            cubes.Add(myCube, -1);
            // create a couple extra cubes
            double ti, tj;
            double cs = 0.47;

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    ti = (i - 1) * 0.27;
                    tj = (j - 1) * 0.27;

                    // create translate transform and pass it to CreateCube
                    CreateCube(i * 3 + j, new TranslateTransform3D(ti, tj, -1 * cs));
                    CreateCube(9 + i * 3 + j, new TranslateTransform3D(ti, tj, +cs));
                    CreateCube(18 + i * 3 + j, new TranslateTransform3D(ti, -1 * cs, tj));
                    CreateCube(27 + i * 3 + j, new TranslateTransform3D(ti, cs, tj));
                    CreateCube(36 + i * 3 + j, new TranslateTransform3D(-1 * cs, tj, ti));
                    CreateCube(45 + i * 3 + j, new TranslateTransform3D(cs, tj, ti));
                }
            }
            myCube.Transform = new ScaleTransform3D(0.1, 0.1, 0.1);
            ViewModel.InitializeCells(cubesByIndex.Keys);
            // Map VM cells to visuals: subscribe to property changes and set materials in the View (UI)
            if (ViewModel.CellsByIndex != null)
            {
                foreach (var kvp in cubesByIndex)
                {
                    int index = kvp.Key;
                    var model3D = kvp.Value;

                    if (!ViewModel.CellsByIndex.TryGetValue(index, out var cell))
                        continue;

                    // apply initial state
                    model3D.Material = cell.IsOn
                        ? _yellowMaterial
                        : _defaultMaterial;

                    // subscribe to changes and update material on UI thread
                    PropertyChangedEventHandler handler = (s, e) =>
                    {
                        if (e.PropertyName != nameof(cell.IsOn)) return;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            model3D.Material = cell.IsOn ? _yellowMaterial : _defaultMaterial;
                        });
                    };

                    cell.PropertyChanged += handler;
                    _cellHandlers[index] = handler;
                }
            }
            StartUpEffect();
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // guard to avoid multiple celebrations
            if (e.PropertyName == nameof(ViewModel.Solved) && ViewModel.Solved && !_celebrationRunning)
            {
                Application.Current.Dispatcher.Invoke(() => ShowSolvedEffects());
            }
        }

        // unsubscribes to avoid leaks when window closes/unloads
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel != null)
                    ViewModel.PropertyChanged -= ViewModel_PropertyChanged;

                if (ViewModel?.CellsByIndex != null)
                {
                    foreach (var kvp in ViewModel.CellsByIndex)
                    {
                        var index = kvp.Key;
                        var cell = kvp.Value;
                        if (_cellHandlers.TryGetValue(index, out var handler))
                        {
                            cell.PropertyChanged -= handler;
                        }
                    }
                    _cellHandlers.Clear();
                }
            }
            catch
            {
                // best effort cleanup
            }

            // detach trackball event source so Trackball can unregister events
            if (trackball != null)
            {
                trackball.EventSource = null;
            }
        }

        private void ShowSolvedEffects()
        {
            // prevent re-entrancy
            if (_celebrationRunning) return;
            _celebrationRunning = true;

            // banner text
            try
            {
                SolvedBanner.Text = $"Solved Level {ViewModel.SelectedPuzzle}";
                SolvedBanner.Visibility = Visibility.Visible;

                GradientManager.AnimateBackgroundToRandomGradient(MainGrid, durationMs: 800);
                var fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(300)));
                var stay = new DoubleAnimation(1, 1, new Duration(TimeSpan.FromMilliseconds(800))) { BeginTime = TimeSpan.FromMilliseconds(300) };
                var fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(400))) { BeginTime = TimeSpan.FromMilliseconds(1100) };

                var sb = new Storyboard();
                sb.Children.Add(fadeIn);
                sb.Children.Add(stay);
                sb.Children.Add(fadeOut);
                Storyboard.SetTarget(fadeIn, SolvedBanner);
                Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
                Storyboard.SetTarget(stay, SolvedBanner);
                Storyboard.SetTargetProperty(stay, new PropertyPath("Opacity"));
                Storyboard.SetTarget(fadeOut, SolvedBanner);
                Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));

                // Completed now advances to the next puzzle so the new puzzle appears without requiring a click
                sb.Completed += (s, e) =>
                {
                    try
                    {
                        SolvedBanner.Visibility = Visibility.Collapsed;
                        // advance to next puzzle after the celebration animation (only once)
                        ViewModel.PuzzleSolved();
                    }
                    finally
                    {
                        // ensure guard is reset even if PuzzleSolved throws
                        _celebrationRunning = false;
                    }
                };
                sb.Begin();
            }
            catch
            {
                // ignore banner errors, but ensure we clear the guard
                _celebrationRunning = false;
            }

            // random rotation of the cube (add a temporary RotateTransform3D to myTransformGroup)
            try
            {
                // random axis
                var rx = _rand.NextDouble() - 0.5;
                var ry = _rand.NextDouble() - 0.5;
                var rz = _rand.NextDouble() - 0.5;
                var axis = new Vector3D(rx, ry, rz);
                if (axis.Length == 0) axis = new Vector3D(0, 1, 0);
                axis.Normalize();

                var rotation = new AxisAngleRotation3D(axis, 0);
                var rTransform = new RotateTransform3D(rotation, new Point3D(0, 0, 0));
                myTransformGroup.Children.Add(rTransform);

                var revs = 1 + _rand.Next(2); // 1..2 revs
                var angleAnim = new DoubleAnimation(0, 360 * revs, new Duration(TimeSpan.FromMilliseconds(800 + _rand.Next(400))))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                angleAnim.Completed += (s, e) =>
                {
                    // remove temporary transform after animation completes
                    try
                    {
                        myTransformGroup.Children.Remove(rTransform);
                    }
                    catch { }
                };

                rotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, angleAnim);
            }
            catch
            {
                // ignore rotation errors
            }
        }

        private void CreateCube(int Index, TranslateTransform3D TT)
        {
            ModelVisual3D extraCube = new ModelVisual3D();
            Model3DGroup modelGroup = new Model3DGroup();
            GeometryModel3D model3d = new GeometryModel3D();
            model3d = myCube.Clone();

            MaterialGroup m = (MaterialGroup)System.Windows.Application.Current.Resources["myFunkyMaterial"];

            modelGroup.Children.Add(model3d);
            extraCube.Content = modelGroup;
            Transform3DGroup tg = new Transform3DGroup();
            extraCube.Transform = tg;

            tg.Children.Add(new ScaleTransform3D(CubeScale, CubeScale, CubeScale));

            // use the provided TranslateTransform3D instance and keep a reference for animations
            tg.Children.Add(TT);
            mvModel.Children.Add(extraCube);

            AddCube(model3d, Index, TT);
        }

        private void AddCube(GeometryModel3D Model3D, int i, TranslateTransform3D tt)
        {
            int key = model[i];
            cubes.Add(Model3D, key);
            cubesByIndex.Add(key, Model3D);

            // store transform for later animation
            cubeTranslateByIndex[key] = tt;
        }

        void OnClick(Object sender, MouseButtonEventArgs args)
        {
            System.Windows.Point mouseposition = args.GetPosition(CubeViewport);
            Point3D testpoint3D = new Point3D(mouseposition.X, mouseposition.Y, 0);
            Vector3D testdirection = new Vector3D(mouseposition.X, mouseposition.Y, 10);
            PointHitTestParameters pointparams = new PointHitTestParameters(mouseposition);
            RayHitTestParameters rayparams = new RayHitTestParameters(testpoint3D, testdirection);

            VisualTreeHelper.HitTest(CubeViewport, null, HTResult, pointparams);

            // removed click-driven puzzle advance — puzzle now advances automatically after the solved celebration
        }

        public HitTestResultBehavior HTResult(System.Windows.Media.HitTestResult rawresult)
        {
            var rayResult = rawresult as RayHitTestResult;
            if (rayResult == null)
                return HitTestResultBehavior.Stop;

            var meshResult = rayResult as RayMeshGeometry3DHitTestResult;
            if (meshResult == null)
                return HitTestResultBehavior.Stop;

            var hitgeo = meshResult.ModelHit as GeometryModel3D;
            if (hitgeo == null)
                return HitTestResultBehavior.Stop;

            if (!cubes.TryGetValue(hitgeo, out int iButton))
                return HitTestResultBehavior.Stop;

            try
            {
                // animate press visual feedback, then toggle model state and let VM property-change handler update material
                AnimatePress(iButton, () =>
                {
                    // toggle after animation completes
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ViewModel.Toggle(iButton);
                        ViewModel.SetCube();
                    });
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling button {iButton}: {ex}");
            }

            return HitTestResultBehavior.Stop;
        }

        // Animate a small cube a short distance toward the scene origin, then back (AutoReverse)
        // Calls onCompleted after the animation (including AutoReverse) finishes.
        private void AnimatePress(int index, Action onCompleted = null)
        {
            if (!cubeTranslateByIndex.TryGetValue(index, out var tt))
                return;

            // current offsets
            double ox = tt.OffsetX;
            double oy = tt.OffsetY;
            double oz = tt.OffsetZ;

            // direction toward face of main cube
            double dx = -ox;
            double dy = -oy;
            double dz = -oz;
            dx = Math.Abs(dx) < .3 ? 0 : dx;
            dy = Math.Abs(dy) < .3 ? 0 : dy;
            dz = Math.Abs(dz) < .3 ? 0 : dz;

            double len = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (len < 1e-6)
            {
                // fallback: move slightly along negative Z if at origin
                dz = -1;
                len = 1;
            }

            // normalize and scale by PressInset
            double nx = dx / len;
            double ny = dy / len;
            double nz = dz / len;

            double tx = ox + nx * PressInset;
            double ty = oy + ny * PressInset;
            double tz = oz + nz * PressInset;

            var durMs = 150; // single direction duration (tweak)
            var dur = new Duration(TimeSpan.FromMilliseconds(durMs));
            // create and start animations directly on the transform
            var ax = new DoubleAnimation(tx, dur) { AutoReverse = true, EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            var ay = new DoubleAnimation(ty, dur) { AutoReverse = true, EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            var az = new DoubleAnimation(tz, dur) { AutoReverse = true, EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            // apply animations
            tt.BeginAnimation(TranslateTransform3D.OffsetXProperty, ax);
            tt.BeginAnimation(TranslateTransform3D.OffsetYProperty, ay);
            tt.BeginAnimation(TranslateTransform3D.OffsetZProperty, az);

            // schedule onCompleted after the full AutoReverse round-trip
            if (onCompleted != null)
            {
                var totalMs = (durMs * 2) + 20; // AutoReverse doubles duration, add tiny margin
                var timer = new System.Windows.Threading.DispatcherTimer(
                    TimeSpan.FromMilliseconds(totalMs),
                    System.Windows.Threading.DispatcherPriority.Background,
                    (s, e) =>
                    {
                        try { onCompleted(); } catch { /* best-effort */ }
                        ((System.Windows.Threading.DispatcherTimer)s).Stop();
                    },
                    Application.Current.Dispatcher);
                timer.Start();
            }
        }
    }
}
