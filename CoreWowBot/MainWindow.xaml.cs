using Libs;
using Libs.Actions;
using Libs.Cursor;
using Libs.GOAP;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;

namespace Powershell
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WowData addonThread;
        private PlayerDirection playerDirection;
        private Thread thread;
        //private GoapAgent agent;
        //private GoapAction currentAction;
        //private HashSet<GoapAction> availableActions;

        public MainWindow()
        {
            InitializeComponent();

            var colorReader = new WowScreen();

            var config = new DataFrameConfiguration(colorReader);

            var frames = config.ConfigurationExists()
                ? config.LoadConfiguration()
                : config.CreateConfiguration(WowScreen.GetAddonBitmap());

            this.addonThread = new WowData(colorReader, frames);
            playerDirection = new PlayerDirection(this.addonThread.PlayerReader, WowProcess);
            this.thread = new Thread(addonThread.DoWork);



            //var text = File.ReadAllText(@"D:\GitHub\WowPixelBot\Path_20200210195132.json");
            //var text = File.ReadAllText(@"D:\GitHub\WowPixelBot\Path_20200215184939.json");
            //var text = File.ReadAllText(@"D:\GitHub\WowPixelBot\Path_20200217215324.json");
            //var pathText = File.ReadAllText(@"D:\GitHub\WowPixelBot\ThousandNeedles.json");
            //var spiritText = File.ReadAllText(@"D:\GitHub\WowPixelBot\ThousandNeedlesSpiritHealer.json");

            //var pathText = File.ReadAllText(@"D:\GitHub\WowPixelBot\Arathi.json");
            //var spiritText = File.ReadAllText(@"D:\GitHub\WowPixelBot\Arathi_SpritHealer.json");


            //var pathPoints = JsonConvert.DeserializeObject<List<WowPoint>>(pathText);
            //var pathPointsReversed = JsonConvert.DeserializeObject<List<WowPoint>>(pathText);
            //pathPointsReversed.Reverse();
            //var pathThereAndBack = pathPoints.Concat(pathPointsReversed).ToList();

            //var spiritPoints = JsonConvert.DeserializeObject<List<WowPoint>>(spiritText);

            //var followRouteAction = new FollowRouteAction(addonThread.PlayerReader, WowProcess, playerDirection, pathThereAndBack);
            //this.currentAction = followRouteAction;
            
            //var killMobAction = new KillTargetAction(WowProcess, addonThread.PlayerReader);
            //var pullTargetAction = new PullTargetAction(WowProcess, addonThread.PlayerReader);
            //var approachTargetAction = new ApproachTargetAction(WowProcess, addonThread.PlayerReader);
            //var lootAction = new LootAction(WowProcess, addonThread.PlayerReader);
            //var healAction = new HealAction(WowProcess, addonThread.PlayerReader);

            //this.availableActions = new HashSet<GoapAction>
            //{
            //    followRouteAction,
            //    killMobAction,
            //    pullTargetAction,
            //    approachTargetAction,
            //    lootAction,
            //    healAction,
            //    new TargetDeadAction(WowProcess,addonThread.PlayerReader),
            //    new WalkToCorpseAction(addonThread.PlayerReader,WowProcess,playerDirection,spiritPoints,pathPoints)
            //};

            //this.agent = new GoapAgent(this.addonThread.PlayerReader, this.availableActions);

            thread.Start();
        }

        private void Dump(long v)
        {
            var s = Convert.ToString(v, 2);
            s = "00000000000000000000000000".Substring(0, 18 - s.Length) + s;
            System.Diagnostics.Debug.WriteLine(s);
        }

        private WowProcess? wowProcess;

        public WowProcess WowProcess
        {
            get
            {
                if (this.wowProcess == null)
                {
                    this.wowProcess = new WowProcess();
                }
                return this.wowProcess;
            }
        }

        public bool UpKeyIsDown = false;
        public bool DownKeyIsDown = false;

        private void Up_Click(object sender, RoutedEventArgs e)
        {
            UpKeyIsDown = !UpKeyIsDown;
            WowProcess.SetKeyState(ConsoleKey.UpArrow, UpKeyIsDown);
        }

        private void Left_Down(object sender, MouseButtonEventArgs e)
        {
            WowProcess.SetKeyState(ConsoleKey.LeftArrow, true);
        }

        private void Left_Up(object sender, MouseButtonEventArgs e)
        {
            WowProcess.SetKeyState(ConsoleKey.LeftArrow, false);
        }

        private void Right_Down(object sender, MouseButtonEventArgs e)
        {
            WowProcess.SetKeyState(ConsoleKey.RightArrow, true);
        }

        private void Right_Up(object sender, MouseButtonEventArgs e)
        {
            WowProcess.SetKeyState(ConsoleKey.RightArrow, false);
        }

        private void Down_Click(object sender, RoutedEventArgs e)
        {
            DownKeyIsDown = !DownKeyIsDown;
            WowProcess.SetKeyState(ConsoleKey.DownArrow, DownKeyIsDown);
        }

        private void North_Click(object sender, RoutedEventArgs e)
        {
            System.Threading.Thread.Sleep(2000);
            CursorClassifier.Classify(out CursorClassification cursor);
           // playerDirection.SetDirection(0.0);
        }

        private async void East_Click(object sender, RoutedEventArgs e)
        {
            await playerDirection.SetDirection(1.5707963268,new WowPoint(0,0),"");
        }

        private async void West_Click(object sender, RoutedEventArgs e)
        {
            await playerDirection.SetDirection(4.7123889804, new WowPoint(0, 0), "");
        }

        private async void South_Click(object sender, RoutedEventArgs e)
        {
            await playerDirection.SetDirection(3.1415926536, new WowPoint(0, 0), "");
        }

        private System.Timers.Timer? timer;
        private List<WowPoint> locations = new List<WowPoint>();

        private void Record_Click(object sender, RoutedEventArgs e)
        {
            if (timer != null)
            {
                timer.Enabled = false;
                timer.Dispose();
                timer = null;

                File.WriteAllText($"../../../../Path_{DateTime.Now.ToString("yyyyMMddHHmmss")}.json", JsonConvert.SerializeObject(locations));
            }
            else
            {
                timer = new System.Timers.Timer(100);
                timer.Elapsed += OnTimedEvent;
                timer.AutoReset = true;
                timer.Enabled = true;
                locations.Clear();
            }
        }

        private async void SetStartDirection_Click(object sender, RoutedEventArgs e)
        {
            await playerDirection.SetDirection(4.3507, new WowPoint(0, 0), "");
        }

        private async void SetEndDirection_Click(object sender, RoutedEventArgs e)
        {
            await playerDirection.SetDirection(5.3871, new WowPoint(0, 0), "");
        }

        private void OnMouseEvent(object sender, ElapsedEventArgs e)
        {
            var pos = GetCursorPosition();
            Debug.WriteLine($"{pos.X},{pos.Y}");
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator Point(POINT point)
            {
                return new Point(point.X, point.Y);
            }
        }

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);
        public static Point GetCursorPosition()
        {
            POINT lpPoint;
            GetCursorPos(out lpPoint);
            return lpPoint;
        }

        private void StopRunPath_Click(object sender, RoutedEventArgs e)
        {
            stop = true;
        }


        private async void RunPath_Click(object sender, RoutedEventArgs e)
        {
            if (!stop)
            {
                var bot = new Bot(this.addonThread);
                await bot.DoWork();

            }
            stop = false;
        }

        private bool stop = false;



        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            var location = new WowPoint(addonThread.PlayerReader.XCoord, addonThread.PlayerReader.YCoord);
            if (locations.Count == 0 || DistanceTo(location, locations.Last()) > 60 & location.X!=0)
            {
                locations.Add(location);
                Debug.WriteLine($"Points: {locations.Count}, {location.X},{location.Y}");
            }
        }

        private double DistanceTo(WowPoint l1, WowPoint l2)
        {
            var x = l1.X - l2.X;
            var y = l1.Y - l2.Y;
            x = x * 100;
            y = y * 100;
            var distance = Math.Sqrt((x * x) + (y * y));

            //Debug.WriteLine($"distance:{x} {y} {distance.ToString()}");
            return distance;
        }

        //private void SetDirection(double desiredDirection)
        //{
        //    var key = GetDirectionKeyToPress(desiredDirection);

        //    // Press Right
        //    WowProcess.SetKeyState(key, true);

        //    // Wait until we are going the right direction
        //    while (true)
        //    {
        //        var actualDirection = addonThread.PlayerReader.Direction;

        //        bool goingTheWrongWay = GetDirectionKeyToPress(desiredDirection) != key;
        //        bool closeEnoughToDesiredDirection = Math.Abs(actualDirection - desiredDirection) < 0.1;

        //        if (closeEnoughToDesiredDirection || goingTheWrongWay)
        //        {
        //            if (goingTheWrongWay && !closeEnoughToDesiredDirection) { Debug.WriteLine("GOING THE WRONG WAY!"); }

        //            WowProcess.SetKeyState(key, false);
        //            break;
        //        }
        //    }
        //}

        //private ConsoleKey GetDirectionKeyToPress(double desiredDirection)
        //{
        //    return (RADIAN - desiredDirection + addonThread.PlayerReader.Direction) % RADIAN < Math.PI ? ConsoleKey.RightArrow : ConsoleKey.LeftArrow;
        //}
    }
}