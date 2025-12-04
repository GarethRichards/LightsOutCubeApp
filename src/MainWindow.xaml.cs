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
using System.Windows.Threading;

namespace LightsOutCube
{
    public partial class MainWindow : Window
    {
        private double _cubeScale = 0.14;
        private CubeViewModel _viewModel;
        private Dictionary<GeometryModel3D, int> _cubes = new Dictionary<GeometryModel3D, int>();
        private Dictionary<int, GeometryModel3D> _cubesByIndex = new Dictionary<int, GeometryModel3D>();
        // map index -> translate transform for animation
        private readonly Dictionary<int, TranslateTransform3D> cubeTranslateByIndex = new Dictionary<int, TranslateTransform3D>();

        private int[] model = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 39, 36, 33, 38, 35, 32, 37, 34, 31, 53, 52, 51, 56, 55, 54, 59, 58, 57, 23, 26, 29, 22, 25, 28, 21, 24, 27, 49, 46, 43, 48, 45, 42, 47, 44, 41, 11, 12, 13, 14, 15, 16, 17, 18, 19 };

        private Trackball _trackball;

        private Material _yellowMaterial;
        private Material _defaultMaterial;

        // store handlers so we can unsubscribe on unload
        private readonly Dictionary<int, PropertyChangedEventHandler> _cellHandlers = new Dictionary<int, PropertyChangedEventHandler>();

        // how far a button moves toward the center when pressed (tweak as needed)
        private const double _pressInset = 0.12;

        private readonly Random _rand = new Random();

        // Guard to prevent the solved celebration being started multiple times concurrently
        private bool _celebrationRunning = false;

        // Keep MediaPlayer as a field so it is not garbage-collected while playing
        private MediaPlayer _startupPlayer;

        // Solution display members
        private Material _solutionMaterial;
        private readonly Dictionary<int, DispatcherTimer> _solutionTimers = new Dictionary<int, DispatcherTimer>();
        private readonly Dictionary<int, Material> _originalMaterials = new Dictionary<int, Material>();
        private readonly TimeSpan _flashInterval = TimeSpan.FromMilliseconds(300);

        // new flag: do not show solution by default
        private bool _showSolution = false;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new CubeViewModel();
            DataContext = _viewModel;

            // listen to ViewModel property changes for solved notification and puzzle changes
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

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
            // use the overlay border as the EventSource so mouse moves are detected consistently
            _trackball = new Trackball(myTransformGroup)
            {
                EventSource = EventSourceBorder
            };

            _yellowMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Yellow));
            _defaultMaterial = (Material)System.Windows.Application.Current.Resources["myFunkyMaterial"];

            // solution material (distinct, can tweak color)
            _solutionMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0x00, 0xCC, 0xFF)));

            _cubes.Add(myCube, -1);
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
            _viewModel.InitializeCells(_cubesByIndex.Keys);
            InitializeCellVisuals();
            StartUpEffect();
            PlayStartupSound();

            // show the startup splash overlay
            ShowStartupSplash();
        }
        void InitializeCellVisuals()
        {

            // Map VM cells to visuals: subscribe to property changes and set materials in the View (UI)
            if (_viewModel.CellsByIndex != null)
            {
                foreach (var kvp in _cubesByIndex)
                {
                    int index = kvp.Key;
                    var model3D = kvp.Value;

                    if (!_viewModel.CellsByIndex.TryGetValue(index, out var cell))
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
        }
        

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // guard to avoid multiple celebrations
            if (e.PropertyName == nameof(_viewModel.Solved) && _viewModel.Solved && !_celebrationRunning)
            {
                // Ensure any solution highlight is hidden when solved (ViewModel will have set ShowSolution=false)
                Application.Current.Dispatcher.Invoke(() => ShowSolvedEffects());
            }

            // When a new puzzle is selected, display its solution only if user requested it
            if (e.PropertyName == nameof(_viewModel.SelectedPuzzle))
            {
                if (_viewModel.ShowSolution)
                {
                    Dispatcher.BeginInvoke(new Action(() => DisplaySolutionForCurrentPuzzle()), DispatcherPriority.Background);
                }
                else
                {
                    // always cancel any previous solution display when puzzle changes and user isn't showing solution
                    CancelSolutionDisplay();
                }
            }

            // When the show-solution flag changes in the ViewModel, start/stop the display
            if (e.PropertyName == nameof(_viewModel.ShowSolution))
            {
                try
                {
                    if (_viewModel.ShowSolution)
                    {
                        Dispatcher.BeginInvoke(new Action(() => DisplaySolutionForCurrentPuzzle()), DispatcherPriority.Background);
                    }
                    else
                    {
                        CancelSolutionDisplay();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error handling ShowSolution change: {ex}");
                }
            }
        }

        // Button click toggles showing solution
        private void ShowSolutionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _showSolution = !_showSolution;

                if (_showSolution)
                {
                    ShowSolutionButton.Content = "Hide Solution";
                    // display solution for the current puzzle
                    Dispatcher.BeginInvoke(new Action(() => DisplaySolutionForCurrentPuzzle()), DispatcherPriority.Background);
                }
                else
                {
                    ShowSolutionButton.Content = "Show Solution";
                    // hide solution highlights
                    CancelSolutionDisplay();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowSolutionButton_Click error: {ex}");
            }
        }

        private void ShowStartupSplash()
        {
            try
            {
                // enable blocking hit-test while splash is visible
                StartupSplash.IsHitTestVisible = true;
                StartupSplash.Visibility = Visibility.Visible;
                StartupSplash.Opacity = 0;

                var fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(300)));
                var hold = new DoubleAnimation(1, 1, new Duration(TimeSpan.FromMilliseconds(900))) { BeginTime = TimeSpan.FromMilliseconds(300) };
                var fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(300))) { BeginTime = TimeSpan.FromMilliseconds(1200) };

                var sb = new Storyboard();
                sb.Children.Add(fadeIn);
                sb.Children.Add(hold);
                sb.Children.Add(fadeOut);

                Storyboard.SetTarget(fadeIn, StartupSplash);
                Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
                Storyboard.SetTarget(hold, StartupSplash);
                Storyboard.SetTargetProperty(hold, new PropertyPath("Opacity"));
                Storyboard.SetTarget(fadeOut, StartupSplash);
                Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));

                sb.Completed += (s, e) =>
                {
                    StartupSplash.Visibility = Visibility.Collapsed;
                    StartupSplash.IsHitTestVisible = false;
                };

                sb.Begin();
            }
            catch
            {
                try
                {
                    StartupSplash.Visibility = Visibility.Collapsed;
                    StartupSplash.IsHitTestVisible = false;
                }
                catch { }
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
                        _viewModel.PuzzleSolved();
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
            var model3d = myCube.Clone();

            modelGroup.Children.Add(model3d);
            extraCube.Content = modelGroup;
            Transform3DGroup tg = new Transform3DGroup();
            extraCube.Transform = tg;

            tg.Children.Add(new ScaleTransform3D(_cubeScale, _cubeScale, _cubeScale));

            // use the provided TranslateTransform3D instance and keep a reference for animations
            tg.Children.Add(TT);
            mvModel.Children.Add(extraCube);

            AddCube(model3d, Index, TT);
        }

        private void AddCube(GeometryModel3D Model3D, int i, TranslateTransform3D tt)
        {
            int key = model[i];
            _cubes.Add(Model3D, key);
            _cubesByIndex.Add(key, Model3D);

            // store transform for later animation
            cubeTranslateByIndex[key] = tt;
        }

        void OnClick(Object sender, MouseButtonEventArgs args)
        {
            System.Windows.Point mouseposition = args.GetPosition(CubeViewport);
            PointHitTestParameters pointparams = new PointHitTestParameters(mouseposition);

            VisualTreeHelper.HitTest(CubeViewport, null, HTResult, pointparams);

            // removed click-driven puzzle advance — puzzle now advances automatically after the solved celebration
        }

        // Stop flashing for a single solution index and restore the appropriate material.
        private void StopFlashForModel(int index)
        {
            try
            {
                if (_solutionTimers.TryGetValue(index, out var timer))
                {
                    try { timer.Stop(); } catch { }
                    _solutionTimers.Remove(index);
                }

                // Determine final material based on current logical state (on->yellow, off->default)
                Material final = _defaultMaterial;
                if (_viewModel?.CellsByIndex != null && _viewModel.CellsByIndex.TryGetValue(index, out var cell))
                {
                    final = cell.IsOn ? _yellowMaterial : _defaultMaterial;
                }

                if (_cubesByIndex.TryGetValue(index, out var model3D))
                {
                    model3D.Material = final;
                }

                // remove stored original as we've restored appropriate material
                if (_originalMaterials.ContainsKey(index))
                    _originalMaterials.Remove(index);
            }
            catch
            {
                // best-effort cleanup
                try
                {
                    if (_solutionTimers.TryGetValue(index, out var t2))
                    {
                        t2.Stop();
                        _solutionTimers.Remove(index);
                    }
                    if (_originalMaterials.ContainsKey(index))
                        _originalMaterials.Remove(index);
                }
                catch { }
            }
        }

        public HitTestResultBehavior HTResult(System.Windows.Media.HitTestResult rawresult)
        {
            if (!(rawresult is RayHitTestResult rayResult))
                return HitTestResultBehavior.Stop;

            if (!(rayResult is RayMeshGeometry3DHitTestResult meshResult))
                return HitTestResultBehavior.Stop;

            if (!(meshResult.ModelHit is GeometryModel3D hitgeo))
                return HitTestResultBehavior.Stop;

            if (!_cubes.TryGetValue(hitgeo, out int iButton))
                return HitTestResultBehavior.Stop;

            try
            {
                // animate press visual feedback, then toggle model state and let VM property-change handler update material
                AnimatePress(iButton, () =>
                {
                    // toggle after animation completes
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            _viewModel.Toggle(iButton);
                            _viewModel.SetCube();

                            // If this button was part of the currently displayed solution, stop its flashing.
                            // ViewModel.SolutionMask contains the solution mask; stop flashing if bit set.
                            try
                            {
                                if ((_viewModel?.SolutionMask ?? 0L & (1L << iButton)) != 0)
                                {
                                    StopFlashForModel(iButton);
                                }
                            }
                            catch { /* ignore */ }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error toggling button {iButton}: {ex}");
                        }
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

            double tx = ox + nx * _pressInset;
            double ty = oy + ny * _pressInset;
            double tz = oz + nz * _pressInset;

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

        private void PlayStartupSound()
        {
            try
            {
                CleanupStartupPlayer();

                // Extract to temp file first (for embedded Resource)
                var resourceStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/Intro.wav", UriKind.Absolute));
                if (resourceStream == null)
                {
                    System.Diagnostics.Debug.WriteLine("Intro.wav resource not found");
                    return;
                }

                var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "LightsOutCube_Intro.wav");
                using (var fileStream = System.IO.File.Create(tempPath))
                {
                    resourceStream.Stream.CopyTo(fileStream);
                }

                _startupPlayer = new MediaPlayer();
                _startupPlayer.MediaOpened += StartupPlayer_MediaOpened;
                _startupPlayer.MediaEnded += StartupPlayer_MediaEnded;
                _startupPlayer.MediaFailed += StartupPlayer_MediaFailed;
                
                _startupPlayer.Open(new Uri(tempPath, UriKind.Absolute));
                _startupPlayer.Volume = 0.75;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlayStartupSound exception: {ex.Message}");
                CleanupStartupPlayer();
            }
        }

        private void StartupPlayer_MediaOpened(object sender, EventArgs e)
        {
            try { _startupPlayer?.Play(); } catch { }
        }

        private void StartupPlayer_MediaEnded(object sender, EventArgs e)
        {
            CleanupStartupPlayer();
        }

        private void StartupPlayer_MediaFailed(object sender, ExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Startup sound failed: {e.ErrorException?.Message}");
            CleanupStartupPlayer();
        }

        private void CleanupStartupPlayer()
        {
            if (_startupPlayer != null)
            {
                try
                {
                    // Unsubscribe event handlers
                    _startupPlayer.MediaOpened -= StartupPlayer_MediaOpened;
                    _startupPlayer.MediaEnded -= StartupPlayer_MediaEnded;
                    _startupPlayer.MediaFailed -= StartupPlayer_MediaFailed;
                    
                    _startupPlayer.Stop();
                    _startupPlayer.Close();
                }
                catch { }
                finally
                {
                    _startupPlayer = null;
                }
            }
        }
        // Add this method inside the MainWindow class (near other event handlers)
        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var about = new AboutWindow { Owner = this };
                about.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Showing About failed: {ex}");
            }
        }

        // -------------------- Solution display logic --------------------

        // Build a long-state mask from ViewModel.CellsByIndex (1L << index)
        private long BuildCurrentMaskFromViewModel()
        {
            long mask = 0L;
            if (_viewModel?.CellsByIndex == null) return mask;

            foreach (var kvp in _viewModel.CellsByIndex)
            {
                int idx = kvp.Key;
                var cell = kvp.Value;
                if (cell.IsOn)
                    mask |= (1L << idx);
            }
            return mask;
        }

        // Public entry - called when a new puzzle is set
        private void DisplaySolutionForCurrentPuzzle()
        {
            try
            {
                // cancel previous display first
                CancelSolutionDisplay();

                // prepare solver
                var solver = new LightsOutCube.Model.LightsOutCubeSolver();

                // set current state
                var currentMask = BuildCurrentMaskFromViewModel();
                solver.SetCurrent(currentMask);

                // If your solver requires masks (bot/mid/top) they must be initialized here.
                // The converted solver has setters SetBotMasks/SetMidMasks/SetTopMasks which you can call
                // if you have those masks available from LightsOutCubeModel. If not needed, skip.

                var found = solver.Solve();
                if (!found)
                    return;

                var solMask = solver.Solution;
                // For each cell that should be pressed, start a flash timer
                foreach (var kvp in _cubesByIndex)
                {
                    int index = kvp.Key;
                    var model3D = kvp.Value;
                    if ((solMask & (1L << index)) != 0)
                    {
                        // remember original material
                        if (!_originalMaterials.ContainsKey(index))
                            _originalMaterials[index] = model3D.Material;

                        StartFlashForModel(index, model3D);
                    }
                }
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"DisplaySolutionForCurrentPuzzle error: {ex}");
            }
        }

        private void StartFlashForModel(int index, GeometryModel3D model3D)
        {
            if (model3D == null) return;

            // Ensure existing timer for this index is stopped
            if (_solutionTimers.TryGetValue(index, out var existing))
            {
                try { existing.Stop(); } catch { }
                _solutionTimers.Remove(index);
            }

            // Remember original material
            if (!_originalMaterials.ContainsKey(index))
                _originalMaterials[index] = model3D.Material;

            bool lit = true;
            // Start with solution material visible immediately
            model3D.Material = _solutionMaterial;

            var timer = new DispatcherTimer(_flashInterval, DispatcherPriority.Normal, (s, e) =>
            {
                try
                {
                    // toggle material each tick
                    model3D.Material = lit ? _originalMaterials[index] : _solutionMaterial;
                    lit = !lit;
                }
                catch
                {
                    try { ((DispatcherTimer)s).Stop(); } catch { }
                    // restore safe fallback
                    model3D.Material = _originalMaterials.ContainsKey(index) ? _originalMaterials[index] : _defaultMaterial;
                    _solutionTimers.Remove(index);
                }
            }, Application.Current.Dispatcher);

            // Keep reference so it can be stopped later; timer runs indefinitely until CancelSolutionDisplay is called
            _solutionTimers[index] = timer;
            timer.Start();
        }

        private void CancelSolutionDisplay()
        {
            foreach (var kvp in _solutionTimers)
            {
                try { kvp.Value.Stop(); } catch { }
            }
            _solutionTimers.Clear();

            // restore materials
            foreach (var kvp in _originalMaterials)
            {
                try
                {
                    if (_cubesByIndex.TryGetValue(kvp.Key, out var model3D))
                        model3D.Material = kvp.Value;
                }
                catch { }
            }
            _originalMaterials.Clear();
        }

        // Add this method inside the MainWindow class
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Unsubscribe all cell property changed handlers to avoid memory leaks
            foreach (var kvp in _cellHandlers)
            {
                if (_viewModel?.CellsByIndex != null && _viewModel.CellsByIndex.TryGetValue(kvp.Key, out var cell))
                {
                    cell.PropertyChanged -= kvp.Value;
                }
            }
            _cellHandlers.Clear();

            // Cancel any solution display timers
            CancelSolutionDisplay();

            // Optionally, stop any media playback
            try
            {
                if (_startupPlayer != null)
                {
                    _startupPlayer.Stop();
                    _startupPlayer.Close();
                    _startupPlayer = null;
                }
            }
            catch { }
        }
    }
}
