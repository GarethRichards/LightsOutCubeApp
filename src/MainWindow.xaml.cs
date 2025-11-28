using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Windows.Input;
using System.Xml;
using _3DTools;

namespace LightsOutCube
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        

        double CubeScale=0.14;
        LightsOutCubeModel LCM=new LightsOutCubeModel(); 
        long State;
        Dictionary<GeometryModel3D, string> cubes = new Dictionary< GeometryModel3D, String>();
        Dictionary<int, GeometryModel3D> cubesByIndex = new Dictionary<int, GeometryModel3D>();
        int[] model = {1, 2, 3, 4, 5, 6, 7, 8, 9, 39, 36, 33, 38, 35, 32, 37, 34, 31, 53, 52, 51, 56, 55, 54, 59, 58, 57, 23, 26, 29, 22, 25, 28, 21, 24, 27, 49, 46, 43, 48, 45, 42, 47, 44, 41, 11, 12, 13, 14, 15, 16, 17, 18, 19};
        Trackball trackball;
        System.Xml.XmlDocument Puzzles = new XmlDocument();
        double OldX, OldY, OldZ;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void SetPuzzle(int iPuzzle)
        {
            long NewState = 0;
            System.Xml.XmlNode n = Puzzles.FirstChild;
            System.Xml.XmlNode p = n.ChildNodes[iPuzzle - 1];
            for (int i = 0; i < p.ChildNodes.Count; i++)
            {
                int but = Convert.ToInt32(p.ChildNodes[i].InnerText);
                LCM.Tog1(but, ref NewState);
            }
            SetCube(NewState);
        }

        public void OnLoaded(Object sender, System.Windows.RoutedEventArgs args)
        {

            Puzzles.LoadXml(LightsOutCube.Properties.Resources.Puzzle);
            System.Xml.XmlNode n = Puzzles.FirstChild;
            for (int i = 0; i < n.ChildNodes.Count; i++)
            {
                PuzzleCombo.Items.Add(i + 1);
            }

            trackball = new Trackball(myTransformGroup);
            trackball.EventSource = myElement;

            cubes.Add(myCube, "Cube");
            // create a couple extra cubes
            double ti, tj;
            double cs= 0.47;

            for(int i = 0; i<3; i++){
                for (int j = 0; j<3; j++){
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
            PuzzleCombo.SelectedIndex=0;
        }

        private void CreateCube(int Index, TranslateTransform3D TT  )
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
            cubes.Add(Model3D, key.ToString());
            cubesByIndex.Add(key, Model3D);
        }

        void OnClick(Object sender, MouseButtonEventArgs args)
        {
            System.Windows.Point mouseposition = args.GetPosition(myViewport);
            Point3D testpoint3D = new Point3D(mouseposition.X, mouseposition.Y, 0);
            Vector3D testdirection = new Vector3D(mouseposition.X, mouseposition.Y, 10);
            PointHitTestParameters pointparams = new PointHitTestParameters(mouseposition);
            RayHitTestParameters rayparams = new RayHitTestParameters(testpoint3D, testdirection);

            VisualTreeHelper.HitTest(myViewport, null, HTResult, pointparams);

            if (State == 0)
            {
                int sel=PuzzleCombo.SelectedIndex;
                if (PuzzleCombo.Items.Count != sel + 1)
                    sel++;
                PuzzleCombo.SelectedIndex = sel;
            }
        }

        public HitTestResultBehavior HTResult(System.Windows.Media.HitTestResult rawresult)
        {
            RayHitTestResult rayResult = (RayHitTestResult)rawresult;


            if ( rayResult != null) {
                RayMeshGeometry3DHitTestResult rayMeshResult = (RayMeshGeometry3DHitTestResult)rayResult;
                if (rayMeshResult != null) {
                    GeometryModel3D hitgeo = (GeometryModel3D)rayMeshResult.ModelHit;
                    if (cubes.ContainsKey(hitgeo)) {
                        string Key = cubes[hitgeo];
                        try {
                            int iBut = Convert.ToInt32(Key);
                            long NewState;
                            if (iBut > 0) {
                                NewState = State;
                                LCM.Tog5(iBut,ref NewState);
                                SetCube(NewState);
                            }
                        }
                        catch (Exception ex) {

                        }
                    }
                }
            }

            return HitTestResultBehavior.Stop;
        }

        public void StopAnimation(Object sender  , RoutedEventArgs e ){
            myCube.Material = null;
        }

        
        public void  XValueChanged(object sender , RoutedPropertyChangedEventArgs< double> e){
            trackball.TrackX(XSlider.Value-OldX);
            OldX = XSlider.Value;
        }

        public void  YValueChanged(object sender , RoutedPropertyChangedEventArgs< double> e){
            trackball.TrackY(YSlider.Value - OldY);
            OldY = YSlider.Value;
        }

        public void ZValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            trackball.TrackZ(ZSlider.Value - OldZ);
            OldZ = ZSlider.Value;
        }

        public void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetPuzzle(Convert.ToInt32(PuzzleCombo.SelectedItem));
        }

        void SetCube(long NewState)
        {
            long m;
            Boolean SetInCurrent;
            Boolean SetInNew;
            m = 2;
            for (int i = 1; i < 60; i++)
            {
                if (i % 10 != 0)
                {
                    SetInCurrent = (m & State) != 0;
                    SetInNew = (m & NewState) != 0;
                    if (SetInCurrent != SetInNew)
                    {
                        if (SetInNew)
                        {
                            cubesByIndex[i].Material = new DiffuseMaterial(new SolidColorBrush(Colors.Yellow));
                        }
                        else
                        {
                            cubesByIndex[i].Material = (Material)System.Windows.Application.Current.Resources["myFunkyMaterial"];
                        }
                    }
                }
                m = m * 2;
            }
            State = NewState;
        }
    }
}