using Libs;
using Libs.Actions;
using Libs.GOAP;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private AddonThread addonThread;
        private PlayerDirection playerDirection;
        private Thread thread;
        private GoapAgent agent;
        private GoapAction currentAction;
        private HashSet<GoapAction> availableActions;

        public MainWindow()
        {
            InitializeComponent();

            var colorReader = new WowScreen();

            var config = new DataFrameConfiguration(colorReader);

            var frames = config.ConfigurationExists()
                ? config.LoadConfiguration()
                : config.CreateConfiguration(WowScreen.GetAddonBitmap());

            this.addonThread = new AddonThread(colorReader, frames);
            playerDirection = new PlayerDirection(this.addonThread.PlayerReader, WowProcess);
            this.thread = new Thread(addonThread.DoWork);



            //var text = File.ReadAllText(@"D:\GitHub\WowPixelBot\Path_20200210195132.json");
            //var text = File.ReadAllText(@"D:\GitHub\WowPixelBot\Path_20200215184939.json");
            //var text = File.ReadAllText(@"D:\GitHub\WowPixelBot\Path_20200217215324.json");
            var text = File.ReadAllText(@"D:\GitHub\WowPixelBot\WetlandsWhelps.json");
            

            var points = JsonConvert.DeserializeObject<List<WowPoint>>(text);//.Select(p => new WowPoint(p.X, p.Y)).ToList();
            var points2 = JsonConvert.DeserializeObject<List<WowPoint>>(text);//.Select(p => new WowPoint(p.X, p.Y)).ToList();
            points2.Reverse();
            points = points.Concat(points2).ToList();

            var followRouteAction = new FollowRouteAction(addonThread.PlayerReader, WowProcess, playerDirection, points);
            this.currentAction = followRouteAction;
            
            var killMobAction = new KillTargetAction(WowProcess, addonThread.PlayerReader);
            var pullTargetAction = new PullTargetAction(WowProcess, addonThread.PlayerReader);
            var approachTargetAction = new ApproachTargetAction(WowProcess, addonThread.PlayerReader);
            var lootAction = new LootAction(WowProcess, addonThread.PlayerReader);
            var healAction = new HealAction(WowProcess, addonThread.PlayerReader);

            this.availableActions = new HashSet<GoapAction>
            {
                followRouteAction,
                killMobAction,
                pullTargetAction,
                approachTargetAction,
                lootAction,
                healAction,
                new TargetDeadAction(WowProcess,addonThread.PlayerReader),
                new WalkToCorpseAction(addonThread.PlayerReader,WowProcess,playerDirection)
            };

            this.agent = new GoapAgent(this.addonThread.PlayerReader, this.availableActions);

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
            playerDirection.SetDirection(0.0);
        }

        private void East_Click(object sender, RoutedEventArgs e)
        {
            playerDirection.SetDirection(1.5707963268);
        }

        private void West_Click(object sender, RoutedEventArgs e)
        {
            playerDirection.SetDirection(4.7123889804);
        }

        private void South_Click(object sender, RoutedEventArgs e)
        {
            playerDirection.SetDirection(3.1415926536);
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

        private void SetStartDirection_Click(object sender, RoutedEventArgs e)
        {
            playerDirection.SetDirection(4.3507);
        }

        private void SetEndDirection_Click(object sender, RoutedEventArgs e)
        {
            playerDirection.SetDirection(5.3871);
        }

        private void StopRunPath_Click(object sender, RoutedEventArgs e)
        {
            stop = true;
        }


        private async void RunPath_Click(object sender, RoutedEventArgs e)
        {
            stop = false;

            while (!stop)
            {
                var newAction = this.agent.GetAction();
                
                if (newAction != null)
                {
                    if (newAction != this.currentAction)
                    {
                        this.currentAction.DoReset();
                        this.currentAction = newAction;
                        Debug.WriteLine($"New Plan= {newAction.GetType().Name}");
                    }

                    await newAction.PerformAction();
                }
                else
                {
                    Debug.WriteLine($"New Plan= NULL");
                    await (Task.Delay(1000));
                }
            }

        }

        private bool stop = false;



        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            var location = new WowPoint(addonThread.PlayerReader.XCoord, addonThread.PlayerReader.YCoord);
            if (locations.Count == 0 || DistanceTo(location, locations.Last()) > 6)
            {
                locations.Add(location);
                Debug.WriteLine($"Points: {locations.Count}");
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