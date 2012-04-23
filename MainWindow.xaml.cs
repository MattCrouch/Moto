using System;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Microsoft.Kinect;
using Moto.Speech;

namespace Moto
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : NavigationWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            setupKinect();
            setupVoice();
            KinectSensor.KinectSensors.StatusChanged += new EventHandler<StatusChangedEventArgs>(KinectSensors_StatusChanged);
            this.NavigationService.Navigate(new StartScreen());
        }

        public static KinectSensor sensor;
        public static SpeechRecognizer mySpeechRecognizer;

        //Color image vars
        public static WriteableBitmap colorImageBitmap;
        public static Int32Rect colorImageBitmapRect;
        public static int colorImageStride;

        //Depth+Player image vars
        public static WriteableBitmap depthImageBitmap;
        public static Int32Rect depthImageBitmapRect;
        public static int depthImageStride;

        //Player structure
        public class Player
        {
            public Player()
            {
                skeleton = null;
                instrument = Moto.instrument.instrumentList.None;
                mode = PlayerMode.None;
                instrumentImage = null;
            }

            public Skeleton skeleton { get; set; }
            public instrument.instrumentList instrument { get; set; }
            public PlayerMode mode { get; set; }
            public Image instrumentImage {get; set; }
        }

        public enum PlayerMode
        {
            None = 0,
            //Guitar
            Acoustic,
            Electric,
            //Wall of Sound
            Technologic,
            Drum,
            EightBit,
            Custom,
            Create,
        }

        //Hit area structure
        public class HitBox
        {
            public HitBox()
            {
                X1 = 0;
                X2 = 0;
                Y1 = 0;
                Y2 = 0;
                Z1 = 0;
                Z2 = 0;
            }

            public double X1 { get; set; }
            public double Y1 { get; set; }
            public double Z1 { get; set; }
            public double X2 { get; set; }
            public double Y2 { get; set; }
            public double Z2 { get; set; }
        }

        //Player data holds
        public static Dictionary<int, Player> activeSkeletons = new Dictionary<int, Player>();
        public static int primarySkeletonKey;

        //Skeleton variables
        public static Skeleton[] allSkeletons = new Skeleton[6]; //Holds all skeleton data (always returns six skeletons regardless)
        
        public void setupKinect()
        {
            if (KinectSensor.KinectSensors.Count <= 0)
            {
                MessageBox.Show("No Kinect connected :(");
            }
            else
            {
                //use first Kinect
                sensor = KinectSensor.KinectSensors[0];

                switch (sensor.Status)
                {
                    case KinectStatus.Connected:
                        sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                        //sensor.ColorStream.Enable(ColorImageFormat.RgbResolution1280x960Fps12);
                        sensor.DepthStream.Enable();
                        sensor.SkeletonStream.Enable();

                        colorImageBitmap = new WriteableBitmap(sensor.ColorStream.FrameWidth, sensor.ColorStream.FrameHeight, 96, 96, PixelFormats.Bgr32, null);
                        colorImageBitmapRect = new Int32Rect(0, 0, sensor.ColorStream.FrameWidth, sensor.ColorStream.FrameHeight);
                        colorImageStride = sensor.ColorStream.FrameWidth * sensor.ColorStream.FrameBytesPerPixel;

                        depthImageBitmap = new WriteableBitmap(sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight, 96, 96, PixelFormats.Bgra32, null);
                        depthImageBitmapRect = new Int32Rect(0, 0, sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight);
                        depthImageStride = sensor.DepthStream.FrameWidth * 4;

                        try
                        {
                            sensor.Start();
                        }
                        catch (System.IO.IOException)
                        {
                            MessageBox.Show("One program at a time, please");
                            throw;
                        }
                        break;
                    case KinectStatus.Disconnected:
                        MessageBox.Show("No Kinect connected :(");
                        break;
                }
            }
        }

        void setupVoice()
        {
            mySpeechRecognizer = SpeechRecognizer.Create();
            mySpeechRecognizer.Start(sensor.AudioSource);
        }

        void destroyVoice()
        {
            mySpeechRecognizer.Dispose();
            mySpeechRecognizer = null;
        }

        void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Connected:
                    MessageBox.Show("Connected");
                    setupKinect();
                    break;
                case KinectStatus.DeviceNotGenuine:
                    MessageBox.Show("Device not genuine");
                    break;
                case KinectStatus.DeviceNotSupported:
                    MessageBox.Show("Device not supported");
                    break;
                case KinectStatus.Disconnected:
                    MessageBox.Show("Disconnected");
                    stopKinect(sensor);
                    break;
                case KinectStatus.Error:
                    MessageBox.Show("Error");
                    break;
                case KinectStatus.Initializing:
                    MessageBox.Show("Initialising");
                    break;
                case KinectStatus.InsufficientBandwidth:
                    MessageBox.Show("Insufficient bandwidth");
                    break;
                case KinectStatus.NotPowered:
                    MessageBox.Show("No power");
                    break;
                case KinectStatus.NotReady:
                    MessageBox.Show("Not ready");
                    break;
                case KinectStatus.Undefined:
                    MessageBox.Show("Undefined error");
                    break;
                default:
                    MessageBox.Show("Default");
                    break;
            }
        }

        public void stopKinect(KinectSensor theSensor)
        {
            if (theSensor != null)
            {
                theSensor.Stop();
                theSensor.AudioSource.Stop();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //MessageBox.Show("DOES NOT FAIL!");
            stopKinect(sensor);
            destroyVoice();
            //StartScreen.speechRecognizer.Stop();
        }

        public static void adjustKinectAngle(int angleDiff)
        {
            //Adjust Kinect angle by amount supplied
            //Negative values move downwards, positive move upwards
            //If beyond limits, it will go as far as possible
            int angle = sensor.ElevationAngle + angleDiff;

            if (angle > sensor.MaxElevationAngle)
            {
                angle = sensor.MaxElevationAngle;
            }
            else if (angle < sensor.MinElevationAngle)
            {
                angle = sensor.MinElevationAngle;
            }

            sensor.ElevationAngle = angle;
        }

        public static int selectPrimarySkeleton(Dictionary<int,Player> skeletons)
        {
            int skeletonId = 0;

            if (skeletons.Count > 0)
            {
                foreach (var player in skeletons)
                {
                    if (skeletonId == 0)
                    {
                        skeletonId = player.Key;
                    }
                    else
                    {
                        if (player.Value.skeleton.Position.Z < skeletons[skeletonId].skeleton.Position.Z)
                        {
                            skeletonId = player.Key;
                        }
                    }
                }

                return skeletonId;
            }
            else
            {

                return 0;
            }
        }

        public static bool playerAdded(Skeleton skeleton)
        {
            Console.WriteLine("Player added");

            Player newPlayer = new Player();
            newPlayer.skeleton = skeleton;

            activeSkeletons.Add(skeleton.TrackingId, newPlayer);

            return true;
        }

        public static bool playerRemoved(int skeletonId)
        {
            //Clear out all references to the skeleton
            Console.WriteLine("Player Removed");

            if (activeSkeletons.Remove(skeletonId))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void animateSlide(FrameworkElement item, bool reverse = false, bool vertical = true, double movement = 10, double duration = 1)
        {
            DoubleAnimation daOpacity = new DoubleAnimation();
            DoubleAnimation daMovement = new DoubleAnimation();

            if (!reverse)
            {
                daOpacity.From = 0;
                daOpacity.To = 1;
                daMovement.From = movement;
                daMovement.To = 0;
            }
            else
            {
                daOpacity.From = 1;
                daOpacity.To = 0;
                daMovement.From = 0;
                daMovement.To = movement;
            }

            daOpacity.Duration = TimeSpan.FromSeconds(duration);

            daMovement.Duration = TimeSpan.FromSeconds(duration);

            TranslateTransform tt = new TranslateTransform();
            item.RenderTransform = tt;

            CircleEase ease = new CircleEase();
            ease.EasingMode = EasingMode.EaseOut;
            daOpacity.EasingFunction = ease;
            daMovement.EasingFunction = ease;
            
            item.BeginAnimation(FrameworkElement.OpacityProperty, daOpacity);
            if (vertical)
            {
                tt.BeginAnimation(TranslateTransform.YProperty, daMovement);
            }
            else
            {
                tt.BeginAnimation(TranslateTransform.XProperty, daMovement);
            }
        }
    }
}

