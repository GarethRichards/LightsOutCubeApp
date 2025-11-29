using _3DTools;
using CommunityToolkit.Mvvm.Input;
using LightsOutCube;
using LightsOutCube.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LightsOutCube
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        double CubeScale = 0.14;
        CubeViewModel ViewModel = new CubeViewModel();
        Dictionary<GeometryModel3D, int> cubes = new Dictionary<GeometryModel3D, int>();
        Dictionary<int, GeometryModel3D> cubesByIndex = new Dictionary<int, GeometryModel3D>();
        int[] model = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 39, 36, 33, 38, 35, 32, 37, 34, 31, 53, 52, 51, 56, 55, 54, 59, 58, 57, 23, 26, 29, 22, 25, 28, 21, 24, 27, 49, 46, 43, 48, 45, 42, 47, 44, 41, 11, 12, 13, 14, 15, 16, 17, 18, 19 };

        Trackball trackball;
        double OldX, OldY, OldZ;

        private Material _yellowMaterial;
        private Material _defaultMaterial;

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new CubeViewModel();
            DataContext = ViewModel;
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
                    cell.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName != nameof(cell.IsOn)) return;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            model3D.Material = cell.IsOn ? _yellowMaterial : _defaultMaterial;                        });
                    };
                }
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
            tg.Children.Add(TT);
            mvModel.Children.Add(extraCube);
            AddCube(model3d, Index);
        }

        private void AddCube(GeometryModel3D Model3D, int i)
        {
            int key = model[i];
            cubes.Add(Model3D, key);
            cubesByIndex.Add(key, Model3D);
        }

        void OnClick(Object sender, MouseButtonEventArgs args)
        {
            System.Windows.Point mouseposition = args.GetPosition(CubeViewport);
            Point3D testpoint3D = new Point3D(mouseposition.X, mouseposition.Y, 0);
            Vector3D testdirection = new Vector3D(mouseposition.X, mouseposition.Y, 10);
            PointHitTestParameters pointparams = new PointHitTestParameters(mouseposition);
            RayHitTestParameters rayparams = new RayHitTestParameters(testpoint3D, testdirection);

            VisualTreeHelper.HitTest(CubeViewport, null, HTResult, pointparams);

            if (ViewModel.Solved)
            {
                int sel = PuzzleCombo.SelectedIndex;
                if (PuzzleCombo.Items.Count != sel + 1)
                    sel++;
                PuzzleCombo.SelectedIndex = sel;
            }
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
                ViewModel.Toggle(iButton);
                ViewModel.SetCube();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling button {iButton}: {ex}");
            }

            return HitTestResultBehavior.Stop;
        }

        public void XValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            trackball.TrackX(XSlider.Value - OldX);
            OldX = XSlider.Value;
        }

        public void YValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            trackball.TrackY(YSlider.Value - OldY);
            OldY = YSlider.Value;
        }

        public void ZValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            trackball.TrackZ(ZSlider.Value - OldZ);
            OldZ = ZSlider.Value;
        }

    }

}
