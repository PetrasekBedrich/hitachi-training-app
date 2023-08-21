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
using System.Windows.Navigation;
using System.Windows.Shapes;
using TSG = Tekla.Structures.Geometry3d;
using TS = Tekla.Structures;
using Tekla.Structures.Model;
using Tekla.Structures.Model.UI;
using System.Runtime.CompilerServices;
using Tekla.Structures.Solid;
using System.Collections;
using Tekla.Structures.Datatype;
using System.Net;
using TSM = Tekla.Structures.Model;
using System.IO;
using System.Data;
using Tekla.Structures.Geometry3d;

namespace ukol1
{
    /// <summary>
    /// Interakční logika pro MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Model model;
        private ModelObject pickedObject;
        private PickInput face;
        private ArrayList pickedFacePoints;
        private List<List<TSG.Point>> pickedModelPoints;
        private ContourPlate Plate;
        private List<List<TSG.Point>> filteredFaces;
        public MainWindow()
        {
            InitializeComponent();
            this.model = new Model();
            this.Title += " Tekla connected: " + this.model.GetConnectionStatus();
        }
        
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Picker picker = new Picker();
            pickedObject = picker.PickObject(Picker.PickObjectEnum.PICK_ONE_OBJECT);
            Beam pickedBeam = pickedObject as Beam;
            pickedLabel.Content += "Object picked\nType: " + pickedObject.GetType().ToString() + " \nProfile: " + pickedBeam.Profile.ProfileString;
            TSG.CoordinateSystem parCoordinate = pickedObject.GetCoordinateSystem();
            TransformationPlane partPlane = new TransformationPlane(parCoordinate);
            model.GetWorkPlaneHandler().SetCurrentTransformationPlane(partPlane);
            model.CommitChanges();
            PickFaceButton.IsEnabled = true;
            pickedModelPoints = GetFaces(pickedBeam);
        }
        public static TSM.Beam createBeam(TSG.Point startPoint, TSG.Point endPoint)
        {
            TS.Model.Model model = new TS.Model.Model();
            if (model.GetConnectionStatus())
            {
                TSM.Beam beam = new TSM.Beam();
                beam.StartPoint = startPoint;
                beam.EndPoint = endPoint;


                beam.Material.MaterialString = "S235JR";
                beam.Profile.ProfileString = "IPE300";

                beam.Position.Plane = TSM.Position.PlaneEnum.MIDDLE;

                beam.Insert();

                if (model.CommitChanges()) return beam;
            }
            return null;
        }
        public List<List<TSG.Point>> filterFacesCube(List<List<TSG.Point>> faces)
        {
            List<List<TSG.Point>> ret = new List<List<TSG.Point>>();
            double zMin = double.MaxValue;
            double zMax = double.MinValue;
            foreach(var list in  faces)
            {
                foreach(var point in list)
                {
                    if(point.Z < zMin)
                    {
                        zMin = point.Z;
                    }
                    if(point.Z > zMax)
                    {
                        zMax = point.Z;
                    }

                }
            }
            zMin = Math.Round(zMin);
            zMax = Math.Round(zMax);
            bool valid = true;
            foreach(var list in faces)
            {
                if (Math.Round(list[0].X) == Math.Round(list[1].X) && Math.Round(list[2].X) == Math.Round(list[3].X) && Math.Round(list[1].X) == Math.Round(list[2].X))
                {
                    continue;
                }
                foreach (var point in list)
                {
                    if(Math.Round(point.Z) != zMin && Math.Round(point.Z) != zMax)
                    {
                        valid = false;
                    }
                }
                if(valid)ret.Add(list);
                valid = true;
            }
            return ret;
        }
        public static TSG.Point AveragePoint(List<TSG.Point> pts)
        {
            var sum = new TSG.Point();
            var count = 0;
            foreach (var pt in pts)
            {
                count++;
                sum += pt;
            }

            return new TSG.Point(sum.X / count, sum.Y / count, sum.Z / count);
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Picker picker = new Picker();
            face = picker.PickFace();
            List<TSG.Point> facePickedPoints = new List<TSG.Point>();
            var enumer = face.GetEnumerator();
            while (enumer.MoveNext())
            {
                InputItem Item = enumer.Current as InputItem;
                if (Item.GetInputType() == InputItem.InputTypeEnum.INPUT_POLYGON)
                {
                    pickedFacePoints = Item.GetData() as ArrayList;
                }
            }

            filteredFaces = filterFacesCube(pickedModelPoints);
        }
        public static List<List<TSG.Point>> GetFaces(Beam pt, TransformationPlane tp = null)
        {
            var model = new Model();
            var tmpTp = model.GetWorkPlaneHandler().GetCurrentTransformationPlane();
            if (tp != null)
                model.GetWorkPlaneHandler().SetCurrentTransformationPlane(tp);
            var solid = pt.GetSolid();
            var faceEnum = solid.GetFaceEnumerator();
            var faceList = new List<List<TSG.Point>>();
            while (faceEnum.MoveNext())
            {
                var loopEnum = faceEnum.Current.GetLoopEnumerator();
                while (loopEnum.MoveNext())
                {
                    var facePoints = new List<TSG.Point>();
                    var vertexEnum = loopEnum.Current.GetVertexEnumerator();
                    while (vertexEnum.MoveNext())
                        facePoints.Add(vertexEnum.Current);
                    faceList.Add(facePoints);
                }
            }

            if (tp != null)
                model.GetWorkPlaneHandler().SetCurrentTransformationPlane(tmpTp);
            return faceList;
        }
        private bool isBackSide(List<TSG.Point> face, List<List<TSG.Point>> faces)
        {
            TSG.CoordinateSystem parCoordinate = pickedObject.GetCoordinateSystem();
            TransformationPlane partPlane = new TransformationPlane(parCoordinate);
            model.GetWorkPlaneHandler().SetCurrentTransformationPlane(partPlane);
            double zMin = double.MaxValue;
            bool ret = true;
            foreach (var list in faces)
            {
                foreach (var point in list)
                {
                    if ((point).Z < zMin)
                    {
                        zMin = (point).Z;
                    }
                }
            }
            foreach(var point in face)
            {
                if( Math.Round(partPlane.TransformationMatrixToLocal.Transform((point)).Z) != Math.Round(zMin))
                {
                    ret = false;
                }
            }
            return ret;
        }
        public static List<TSG.Point> AsEnumerable(ArrayList a)
        {
            var l = new List<TSG.Point>();
            foreach (var item in a)
                l.Add(item as TSG.Point);
            return l;
        }
        public bool listContainsList(List<List<TSG.Point>> list,List<TSG.Point> find)
        {
            TSG.CoordinateSystem parCoordinate = pickedObject.GetCoordinateSystem();
            TransformationPlane partPlane = new TransformationPlane(parCoordinate);
            model.GetWorkPlaneHandler().SetCurrentTransformationPlane(partPlane);
            List<TSG.Point> transformedFind = new List<TSG.Point>();
            foreach(var item in find)
            {
                transformedFind.Add(partPlane.TransformationMatrixToLocal.Transform(item));
                File.AppendAllText("transformed.txt", partPlane.TransformationMatrixToLocal.Transform(item).ToString() + "\n");
            }
            int counter = 0;
            foreach(var item in list)
            {
                foreach(var pt in transformedFind)
                {
                    if(item.Contains(pt))
                    {
                        counter++;
                    }
                    if(counter == 4)
                    {
                        return true;
                    }
                }
                counter = 0;
            }
            return false;
        }
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            TSG.CoordinateSystem parCoordinate = pickedObject.GetCoordinateSystem();
            TransformationPlane partPlane = new TransformationPlane(parCoordinate);
            model.GetWorkPlaneHandler().SetCurrentTransformationPlane(partPlane);
            
            if (model.GetConnectionStatus())
            {
                if(!listContainsList(filteredFaces,AsEnumerable(pickedFacePoints)))
                {
                    MessageBox.Show("Wrong face picked");
                    return;
                }
                ContourPoint cp1 = new ContourPoint(partPlane.TransformationMatrixToLocal.Transform(new TSG.Point((TSG.Point)pickedFacePoints[0])), null);
                ContourPoint cp2 = new ContourPoint(partPlane.TransformationMatrixToLocal.Transform(new TSG.Point((TSG.Point)pickedFacePoints[1])), null);
                ContourPoint cp3 = new ContourPoint(partPlane.TransformationMatrixToLocal.Transform(new TSG.Point((TSG.Point)pickedFacePoints[2])), null);
                ContourPoint cp4 = new ContourPoint(partPlane.TransformationMatrixToLocal.Transform(new TSG.Point((TSG.Point)pickedFacePoints[3])), null);
                Contour contour = new Contour();
                contour.AddContourPoint(cp1);
                contour.AddContourPoint(cp2);
                contour.AddContourPoint(cp3);
                contour.AddContourPoint(cp4);
                ContourPlate contourPlate = new ContourPlate();
                contourPlate.Contour = contour;
                if (isBackSide(AsEnumerable(pickedFacePoints), pickedModelPoints))
                {
                    contourPlate.Position.Depth = Position.DepthEnum.BEHIND;
                }
                else
                {
                    contourPlate.Position.Depth = Position.DepthEnum.FRONT;
                }
                contourPlate.Position.Plane = (pickedObject as Beam).Position.Plane;
                contourPlate.Position.Rotation = (pickedObject as Beam).Position.Rotation;
                contourPlate.Class = "11";
                contourPlate.Name = "Test plate";
                contourPlate.Material.MaterialString = "S235JR";
                contourPlate.Profile.ProfileString = "PL20";

                contourPlate.AssemblyNumber.Prefix = "RR";
                contourPlate.AssemblyNumber.StartNumber = 1;

                contourPlate.PartNumber.Prefix = "FF";
                contourPlate.PartNumber.StartNumber = 11;
                
                contourPlate.Insert();
                Plate = contourPlate;
                model.CommitChanges();
            }
        }
    }
    
}
