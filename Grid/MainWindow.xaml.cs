using Libs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
using static Libs.Actions.WalkToCorpseAction;

namespace Grid
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class GridPoint
        { 
            public GridPoint(WowPoint wowPoint, double min, int margin, double pointToGrid)
            {
                this.WowPoint = wowPoint;
                this.CanvasX = margin + ((wowPoint.X- min) * pointToGrid);
                this.CanvasY = margin + ((wowPoint.Y- min) * pointToGrid);
            }

            public double CanvasX { get; set; }
            public double CanvasY { get; set; }
            public WowPoint WowPoint { get; set; }

          
        }

  
        public List<WowPoint> pathPoints;
        public List<WowPoint> spiritPoints;
        public double max;
        public double min;
        public int margin = 20;
        public double pointToGrid;

        public MainWindow()
        {
            InitializeComponent();

            ObservableCollection<string> list = new ObservableCollection<string>();
            var dirInfo = new DirectoryInfo(@"D:\GitHub\WowPixelBot");

            FileInfo[] info = dirInfo.GetFiles("*.json");
            foreach (FileInfo f in info)
            {
                list.Add(f.Name);
            }

            

            this.Files.ItemsSource = list;


            canvas.SizeChanged += Canvas_SizeChanged;

            var pathText = File.ReadAllText(@"D:\GitHub\WowPixelBot\ThousandNeedles.json");
            var spiritText = File.ReadAllText(@"D:\GitHub\WowPixelBot\ThousandNeedlesSpiritHealer.json");

            pathPoints = JsonConvert.DeserializeObject<List<WowPoint>>(pathText);
            spiritPoints = JsonConvert.DeserializeObject<List<WowPoint>>(spiritText);

            var allPoints = pathPoints.Concat(spiritPoints);
            var allPoint = allPoints.Select(p => p.X).Concat(allPoints.Select(p => p.Y));

            max = allPoint.Max(p => p);
            min = allPoint.Min(p => p);
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            pointToGrid = ((double)canvas.ActualHeight - (margin * 2)) / (max - min);
            canvas.Children.Clear();
            Draw();
        }

        public void Draw()
        {   
            DrawPoints(pathPoints, Brushes.LightSteelBlue);
            DrawPoints(spiritPoints, Brushes.Red);

            if (this.Files.SelectedItem != null)
            {

                var pathText = File.ReadAllText(@"D:\GitHub\WowPixelBot\" + this.Files.SelectedItem);

                var path = JsonConvert.DeserializeObject<CorpsePath>(pathText);

                var corpseLocation = new GridPoint(path.CorpseLocation, min, margin, pointToGrid);
                DrawPoint(corpseLocation, Brushes.Purple,4);

                var myLocation = new GridPoint(path.MyLocation, min, margin, pointToGrid);
                DrawPoint(myLocation, Brushes.Black, 4);
            }
        }

        private void DrawPoints(List<WowPoint> pathPoints, SolidColorBrush color)
        {
            var gridPoints = pathPoints.Select(p => new GridPoint(p, min, margin, pointToGrid)).ToList();

            for (int i = 0; i < gridPoints.Count - 1; i++)
            {
                var line = new Line();
                line.Stroke = color;

                line.X1 = gridPoints[i].CanvasX;
                line.Y1 = gridPoints[i].CanvasY;
                line.X2 = gridPoints[i + 1].CanvasX;
                line.Y2 = gridPoints[i + 1].CanvasY;

                line.StrokeThickness = 2;
                canvas.Children.Add(line);

                DrawPoint(gridPoints[i], Brushes.White,2);

            }
        }

        private void DrawPoint(GridPoint point, SolidColorBrush color, int size)
        {
            var line = new Line();
            line.Stroke = color;
            line.X1 = point.CanvasX - size;
            line.X2 = point.CanvasX + size;
            line.Y1 = point.CanvasY - size;
            line.Y2 = point.CanvasY+ size;
            line.StrokeThickness = 2;
            canvas.Children.Add(line);

            var line2 = new Line();
            line2.Stroke = color;
            line2.X1 = point.CanvasX + size;
            line2.X2 = point.CanvasX - size;
            line2.Y1 = point.CanvasY - size;
            line2.Y2 = point.CanvasY + size;
            line2.StrokeThickness = 2;
            canvas.Children.Add(line2);
        }

        private void Files_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Canvas_SizeChanged(null, null);
        }
    }
}
