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
    /*
*    _____ ______   _____ _______  __          ______  _____  _  __ _____    _____   ____  _   _ _ _______   _______ ____  _    _  _____ _    _   _____ _______ 
*    |_   _|  ____| |_   _|__   __| \ \        / / __ \|  __ \| |/ // ____|  |  __ \ / __ \| \ | ( )__   __| |__   __/ __ \| |  | |/ ____| |  | | |_   _|__   __|
*      | | | |__      | |    | |     \ \  /\  / / |  | | |__) | ' /| (___    | |  | | |  | |  \| |/   | |       | | | |  | | |  | | |    | |__| |   | |    | |   
*      | | |  __|     | |    | |      \ \/  \/ /| |  | |  _  /|  <  \___ \   | |  | | |  | | . ` |    | |       | | | |  | | |  | | |    |  __  |   | |    | |   
*     _| |_| |       _| |_   | |       \  /\  / | |__| | | \ \| . \ ____) |  | |__| | |__| | |\  |    | |       | | | |__| | |__| | |____| |  | |  _| |_   | |   
*    |_____|_|      |_____|  |_|        \/  \/   \____/|_|  \_\_|\_\_____( ) |_____/ \____/|_| \_|    |_|       |_|  \____/ \____/ \_____|_|  |_| |_____|  |_|   
*                                                                        |/                                                                                     
*/
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
            this.model.GetWorkPlaneHandler().SetCurrentTransformationPlane(new TransformationPlane());
        }
        
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TransformationPlane currentPlane = model.GetWorkPlaneHandler().GetCurrentTransformationPlane();

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
            model.GetWorkPlaneHandler().SetCurrentTransformationPlane(currentPlane);
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
            TransformationPlane currentPlane = model.GetWorkPlaneHandler().GetCurrentTransformationPlane();
            TSG.CoordinateSystem parCoordinate = pickedObject.GetCoordinateSystem();
            TransformationPlane partPlane = new TransformationPlane(parCoordinate);
            model.GetWorkPlaneHandler().SetCurrentTransformationPlane(partPlane);
            double zMin = double.MaxValue;
            bool ret = true;
            foreach (var list in faces)
            {
                foreach (var point in list)
                {
                    if (Math.Round((point).Y) < Math.Round(zMin))
                    {
                        zMin = Math.Round((point).Y);
                    }
                }
            }
            foreach(var point in face)
            {
                if( Math.Round(partPlane.TransformationMatrixToLocal.Transform((point)).Y) != Math.Round(zMin))
                {
                    ret = false;
                }
            }
            model.GetWorkPlaneHandler().SetCurrentTransformationPlane(currentPlane);
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
            TransformationPlane currentPlane = model.GetWorkPlaneHandler().GetCurrentTransformationPlane();
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
                        model.GetWorkPlaneHandler().SetCurrentTransformationPlane(currentPlane);
                        return true;
                    }
                }
                counter = 0;
            }
            model.GetWorkPlaneHandler().SetCurrentTransformationPlane(currentPlane);
            return false;
        }
        private bool isVertical(List<TSG.Point> side)
        {
            TransformationPlane currentPlane = model.GetWorkPlaneHandler().GetCurrentTransformationPlane();
            TSG.CoordinateSystem parCoordinate = pickedObject.GetCoordinateSystem();
            TransformationPlane partPlane = new TransformationPlane(parCoordinate);
            model.GetWorkPlaneHandler().SetCurrentTransformationPlane(partPlane);
            double rf = Math.Round(partPlane.TransformationMatrixToLocal.Transform(side[0]).Z);
            foreach(var item in side)
            {
                if(Math.Round(partPlane.TransformationMatrixToLocal.Transform((TSG.Point)item).Z) != Math.Round(rf))
                { model.GetWorkPlaneHandler().SetCurrentTransformationPlane(currentPlane); return true; }
            }
            model.GetWorkPlaneHandler().SetCurrentTransformationPlane(currentPlane);
            return false;
        }
        private const double plateLength = 85;
        private const double plateWidth = 50;
        private TSG.Point calculateMiddle(TSG.Point point1, TSG.Point point2)
        {
            return new TSG.Point((point1.X + point2.X) / 2, (point1.Y + point2.Y) / 2, (point1.Z + point2.Z) / 2);
        }
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            TransformationPlane currentPlane = model.GetWorkPlaneHandler().GetCurrentTransformationPlane();
            TSG.CoordinateSystem parCoordinate = pickedObject.GetCoordinateSystem();
            TransformationPlane partPlane = new TransformationPlane(parCoordinate);
            model.GetWorkPlaneHandler().SetCurrentTransformationPlane(partPlane);

            foreach (var point in pickedFacePoints)
            {
                File.AppendAllText("horizontal.txt", (partPlane.TransformationMatrixToLocal.Transform((TSG.Point)point)).ToString() + "\n");
            }
            File.AppendAllText("horizontal.txt", "\n");

            if (model.GetConnectionStatus())
            {
                if(!listContainsList(filteredFaces,AsEnumerable(pickedFacePoints)))
                {
                    MessageBox.Show("Wrong face picked");
                    return;
                }
                //ContourPoint cp1 = new ContourPoint(partPlane.TransformationMatrixToLocal.Transform(new TSG.Point((TSG.Point)pickedFacePoints[0])), null);
                //ContourPoint cp2 = new ContourPoint(partPlane.TransformationMatrixToLocal.Transform(new TSG.Point((TSG.Point)pickedFacePoints[1])), null);
                //ContourPoint cp3 = new ContourPoint(partPlane.TransformationMatrixToLocal.Transform(new TSG.Point((TSG.Point)pickedFacePoints[2])), null);
                //ContourPoint cp4 = new ContourPoint(partPlane.TransformationMatrixToLocal.Transform(new TSG.Point((TSG.Point)pickedFacePoints[3])), null);

                TSG.Point tp1 = partPlane.TransformationMatrixToLocal.Transform((TSG.Point)pickedFacePoints[0]);
                TSG.Point tp2 = partPlane.TransformationMatrixToLocal.Transform((TSG.Point)pickedFacePoints[1]);
                TSG.Point tp3 = partPlane.TransformationMatrixToLocal.Transform((TSG.Point)pickedFacePoints[2]);
                TSG.Point tp4 = partPlane.TransformationMatrixToLocal.Transform((TSG.Point)pickedFacePoints[3]);

                double legth = Math.Round(partPlane.TransformationMatrixToLocal.Transform((pickedObject as Beam).EndPoint).X);
                legth -= plateLength;
                legth /= 2;
                bool flag = false;
                string profile = (pickedObject as Beam).Profile.ProfileString;
                profile = profile.Remove(0, 3);
                var paramArr = profile.Split('*');
                double width = Convert.ToDouble(paramArr[0]);
                width -= plateWidth;
                width /= 2;
                ContourPoint cp1 = null;
                ContourPoint cp2 = null;
                ContourPoint cp3 = null;
                ContourPoint cp4 = null;
                bool side = false;
                if (!isVertical(AsEnumerable(pickedFacePoints)))
                {
                    side = true;
                    MessageBox.Show("Side");
                    if (tp1.Z > 0)
                    {
                        cp1 = new ContourPoint(new TSG.Point(tp1.X + legth, tp1.Y - width, tp1.Z), null);
                        cp2 = new ContourPoint(new TSG.Point(tp2.X + legth, tp2.Y + width, tp2.Z), null);
                        cp3 = new ContourPoint(new TSG.Point(tp3.X - legth, tp3.Y + width, tp3.Z), null);
                        cp4 = new ContourPoint(new TSG.Point(tp4.X - legth, tp4.Y - width, tp4.Z), null);
                    }
                    else
                    {
                        flag = true;
                        cp1 = new ContourPoint(new TSG.Point(tp1.X + legth, tp1.Y + width, tp1.Z), null);
                        cp2 = new ContourPoint(new TSG.Point(tp2.X + legth, tp2.Y - width, tp2.Z), null);
                        cp3 = new ContourPoint(new TSG.Point(tp3.X - legth, tp3.Y - width, tp3.Z), null);
                        cp4 = new ContourPoint(new TSG.Point(tp4.X - legth, tp4.Y + width, tp4.Z), null);
                    }
                }
                else
                {
                    if (isBackSide(AsEnumerable(pickedFacePoints), pickedModelPoints))
                    {
                        MessageBox.Show("Back");
                        cp1 = new ContourPoint(new TSG.Point(tp1.X + legth, tp1.Y , tp1.Z - width), null);
                        cp2 = new ContourPoint(new TSG.Point(tp2.X + legth, tp2.Y , tp2.Z+ width), null);
                        cp3 = new ContourPoint(new TSG.Point(tp3.X - legth, tp3.Y , tp3.Z+ width), null);
                        cp4 = new ContourPoint(new TSG.Point(tp4.X - legth, tp4.Y , tp4.Z- width), null);
                    }
                    else
                    {
                        cp1 = new ContourPoint(new TSG.Point(tp1.X + legth, tp1.Y , tp1.Z + width), null);
                        cp2 = new ContourPoint(new TSG.Point(tp2.X + legth, tp2.Y , tp2.Z - width), null);
                        cp3 = new ContourPoint(new TSG.Point(tp3.X - legth, tp3.Y , tp3.Z - width), null);
                        cp4 = new ContourPoint(new TSG.Point(tp4.X - legth, tp4.Y , tp4.Z + width), null);
                    }
                }
                Contour contour = new Contour();
                contour.AddContourPoint(cp1);
                contour.AddContourPoint(cp2);
                contour.AddContourPoint(cp3);
                contour.AddContourPoint(cp4);
                ContourPlate contourPlate = new ContourPlate();
                contourPlate.Contour = contour;
                if (flag)
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

                BoltXYList bolt = new BoltXYList();
                var start = calculateMiddle((TSG.Point)cp1, (TSG.Point)cp2);
                var end = calculateMiddle((TSG.Point)cp3, (TSG.Point)cp4);
                bolt.FirstPosition = start;
                bolt.SecondPosition = end;
                bolt.PartToBoltTo = Plate;
                bolt.PartToBeBolted = (pickedObject as Beam);

                bolt.AddBoltDistX(20);
                bolt.AddBoltDistY(-10);

                bolt.AddBoltDistX(20);
                bolt.AddBoltDistY(10);

                

                bolt.BoltSize = 4.0;
                bolt.Tolerance = 1;
                bolt.BoltStandard = "NELSON";
                bolt.BoltType = BoltGroup.BoltTypeEnum.BOLT_TYPE_SITE;
                bolt.Length = 200;
                bolt.ThreadInMaterial = BoltGroup.BoltThreadInMaterialEnum.THREAD_IN_MATERIAL_YES;
                bolt.Position.Depth = Position.DepthEnum.MIDDLE;
                bolt.Position.Plane = Position.PlaneEnum.MIDDLE;
                if (!side)
                {
                    bolt.Position.Rotation = Position.RotationEnum.BELOW;

                    bolt.AddBoltDistX(64.5);
                    bolt.AddBoltDistY(-10);

                    bolt.AddBoltDistX(60);
                    bolt.AddBoltDistY(10);
                }
                else
                {
                    bolt.Position.Rotation = Position.RotationEnum.BACK;
                    bolt.AddBoltDistX(64.5);
                    bolt.AddBoltDistY(10);

                    bolt.AddBoltDistX(60);
                    bolt.AddBoltDistY(-10);
                }
                bolt.Bolt = false;
                bolt.Nut1 = false;
                bolt.Nut2 = false;
                bolt.Washer1 = false;
                bolt.Washer2 = false;
                bolt.Washer3 = false;
                if(!bolt.Insert())MessageBox.Show("Bolt failed");
                model.GetWorkPlaneHandler().SetCurrentTransformationPlane(currentPlane);
                model.CommitChanges();
            }
        }
    }
    
}
