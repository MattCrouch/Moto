using System;
using System.Collections.Generic;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
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
            if (KinectSensor.KinectSensors.Count > 0 && KinectSensor.KinectSensors[0].Status == KinectStatus.Connected)
            {
                setupKinect();
                setupVoice();
                checkForCommands();
                setupSFX();
                this.NavigationService.Navigate(new StartScreen());
            }
            else
            {
                KinectStatus status = new KinectStatus();

                if (KinectSensor.KinectSensors.Count == 0)
                {
                    status = KinectStatus.Disconnected;
                }
                else
                {
                    status = KinectSensor.KinectSensors[0].Status;
                }

                this.NavigationService.Navigate(new KinectError(status));
            }
        }

        private void setupSFX()
        {
            SFXStartup.LoadAsync();
            SFXSuccess.LoadAsync();
            SFXDismiss.LoadAsync();
            SFXMenu.LoadAsync();
            SFXListening.LoadAsync();
            SFXNotListening.LoadAsync();
            SFXCamera.LoadAsync();
            SFXUpTick.LoadAsync();
            SFXDownTick.LoadAsync();
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
                instrumentOverlay = new Dictionary<int,Image>();
            }

            public Skeleton skeleton { get; set; }
            public instrument.instrumentList instrument { get; set; }
            public PlayerMode mode { get; set; }
            public Image instrumentImage {get; set; }
            public Dictionary<int, Image> instrumentOverlay { get; set; }
        }

        public enum PlayerMode
        {
            None = 0,
            //Guitar
            Acoustic,
            Electric,
            //Wall of Sound
            Animal,
            Beatbox,
            Sax,
            Trance,
            Metal,
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

        public static int gestureSkeletonKey;
        public static handMovements.ActiveGesture activeGesture;

        //Skeleton variables
        public static Skeleton[] allSkeletons = new Skeleton[6]; //Holds all skeleton data (always returns six skeletons regardless)
        
        //Sound effects
        public static SoundPlayer SFXStartup = new SoundPlayer("audio/ui/startup.wav");
        public static SoundPlayer SFXSuccess = new SoundPlayer("audio/ui/confirm.wav");
        public static SoundPlayer SFXDismiss = new SoundPlayer();
        public static SoundPlayer SFXMenu = new SoundPlayer("audio/ui/mode-selection.wav");
        public static SoundPlayer SFXListening = new SoundPlayer("audio/ui/start-listening.wav");
        public static SoundPlayer SFXNotListening = new SoundPlayer("audio/ui/stop-listening.wav");
        public static SoundPlayer SFXCamera = new SoundPlayer("audio/ui/camera-shutter.wav");
        public static SoundPlayer SFXUpTick = new SoundPlayer("audio/ui/menu-up.wav");
        public static SoundPlayer SFXDownTick = new SoundPlayer("audio/ui/menu-down.wav");
        
        //Tutorials
        public static Dictionary<Tutorials, tutorialVisuals> availableTutorials = new Dictionary<Tutorials, tutorialVisuals>();

        public static Tutorials activeTutorial = Tutorials.None;

        public enum Tutorials
        {
            None,
            BandMode,
            WallOfSound,
            KinectGuide,
            Metronome,
            RecordNewWall,
            VoiceRecognition,
        }

        public class tutorialVisuals
        {
            public tutorialVisuals(string url) {
                tutImage = new Image();
                tutImage.Source = new BitmapImage(new Uri("/Moto;component/images/tutorials/" + url, UriKind.Relative));
                seen = false;
            }
            
            public Image tutImage { get; set; }
            public bool seen { get; set; }
        }

        public static void setupKinect()
        {
                //use first Kinect
                sensor = KinectSensor.KinectSensors[0];
                sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                sensor.SkeletonStream.Enable();

                colorImageBitmap = new WriteableBitmap(sensor.ColorStream.FrameWidth, sensor.ColorStream.FrameHeight, 96, 96, PixelFormats.Bgr32, null);
                colorImageBitmapRect = new Int32Rect(0, 0, sensor.ColorStream.FrameWidth, sensor.ColorStream.FrameHeight);
                colorImageStride = sensor.ColorStream.FrameWidth * sensor.ColorStream.FrameBytesPerPixel;

                try
                {
                    sensor.Start();
                }
                catch (System.IO.IOException)
                {
                    //Another program is already using the Kinect
                    MessageBox.Show("It looks like another program is already using your Kinect. Close it before trying to use Moto. Sorry about that.");
                    throw;
                }
        }

        public static void setupVoice()
        {
            mySpeechRecognizer = SpeechRecognizer.Create();
            mySpeechRecognizer.Start(sensor.AudioSource);
        }

        public static void destroyVoice()
        {
            mySpeechRecognizer.Dispose();
            mySpeechRecognizer = null;
        }

        public static void stopKinect(KinectSensor theSensor)
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


        private void checkForCommands()
        {
            String[] arguments = Environment.GetCommandLineArgs();
            foreach (var argument in arguments)
            {
                if (argument == "tutorials")
                {
                    //Enable the tutorials
                    setupTutorials();
                }
            }
        }

        public static void setupTutorials()
        {
            if (availableTutorials.Count <= 0)
            {
                availableTutorials.Add(Tutorials.BandMode, new tutorialVisuals("band-mode.png"));
                availableTutorials.Add(Tutorials.KinectGuide, new tutorialVisuals("kinect-guide.png"));
                availableTutorials.Add(Tutorials.Metronome, new tutorialVisuals("metronome.png"));
                availableTutorials.Add(Tutorials.RecordNewWall, new tutorialVisuals("record-new-wall.png"));
                availableTutorials.Add(Tutorials.VoiceRecognition, new tutorialVisuals("voice-recognition.png"));
                availableTutorials.Add(Tutorials.WallOfSound, new tutorialVisuals("wall-of-sound.png"));
            }
            else
            {
                availableTutorials[Tutorials.BandMode] = new tutorialVisuals("band-mode.png");
                availableTutorials[Tutorials.KinectGuide] = new tutorialVisuals("kinect-guide.png");
                availableTutorials[Tutorials.Metronome] = new tutorialVisuals("metronome.png");
                availableTutorials[Tutorials.RecordNewWall] = new tutorialVisuals("record-new-wall.png");
                availableTutorials[Tutorials.VoiceRecognition] = new tutorialVisuals("voice-recognition.png");
                availableTutorials[Tutorials.WallOfSound] = new tutorialVisuals("wall-of-sound.png");
            }
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
            Console.WriteLine("Player Removed" + skeletonId);

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

        public static void animateFade(FrameworkElement item, double from = 0, double to = 1, double duration = 1)
        {
            DoubleAnimation fader = new DoubleAnimation();
            
            fader.From = from;
            fader.To = to;
            fader.Duration = TimeSpan.FromSeconds(duration);

            item.BeginAnimation(FrameworkElement.OpacityProperty, fader);
        }

        public static void animateSpin(FrameworkElement item)
        {
            RotateTransform rotation = new RotateTransform(0, item.Width/2, item.Height/2);
            item.RenderTransform = rotation;

            DoubleAnimation spinner = new DoubleAnimation();
            spinner.From = 0;
            spinner.To = 360;
            spinner.Duration = TimeSpan.FromSeconds(0.75);
            spinner.RepeatBehavior = RepeatBehavior.Forever;

            rotation.BeginAnimation(RotateTransform.AngleProperty, spinner);
        }

        public static void hidePlayerOverlays()
        {
            foreach (var player in MainWindow.activeSkeletons)
            {
                player.Value.instrumentImage.Visibility = System.Windows.Visibility.Hidden;

                foreach (var overlay in player.Value.instrumentOverlay.Values)
                {
                    overlay.Visibility = System.Windows.Visibility.Hidden;
                }
            }
        }

        public static void showPlayerOverlays()
        {
            foreach (var player in MainWindow.activeSkeletons)
            {
                player.Value.instrumentImage.Visibility = System.Windows.Visibility.Visible;

                foreach (var overlay in player.Value.instrumentOverlay.Values)
                {
                    overlay.Visibility = System.Windows.Visibility.Visible;
                }
            }
        }

        public static void restartMoto()
        {
            stopKinect(sensor);
            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        public static Image generateError(KinectStatus kinectStatus)
        {
            Image kinectError = new Image();
            string errorUri;

            switch (kinectStatus)
            {
                case KinectStatus.DeviceNotGenuine:
                    errorUri = "devicenotgenuine.png";
                    break;
                case KinectStatus.DeviceNotSupported:
                    errorUri = "devicenotsupported.png";
                    break;
                case KinectStatus.Disconnected:
                    errorUri = "disconnected.png";
                    break;
                case KinectStatus.Error:
                    errorUri = "error.png";
                    break;
                case KinectStatus.Initializing:
                    errorUri = "initialising.png";
                    break;
                case KinectStatus.InsufficientBandwidth:
                    errorUri = "insufficientbandwidth.png";
                    break;
                case KinectStatus.NotPowered:
                    errorUri = "notpowered.png";
                    break;
                case KinectStatus.NotReady:
                    errorUri = "notready.png";
                    break;
                case KinectStatus.Undefined:
                default:
                    errorUri = "undefined.png";
                    break;
            }

            kinectError.Source = new BitmapImage(new Uri(
        "/Moto;component/images/kinect-fault/" + errorUri, UriKind.Relative));
            kinectError.Width = 640;
            kinectError.Height = 480;
            return kinectError;
        }


        public static int findVoiceCommandPlayer(double angle)
        {
            int skeletonId = 0;

            if (angle >= 0)
            {
                //Player is on the left of the Kinect (Right of the screen)
                double xPos = -5;
                foreach (var player in MainWindow.activeSkeletons)
                {
                    if (player.Value == null || player.Value.skeleton.Position.X > xPos)
                    {
                        xPos = player.Value.skeleton.Position.X;
                        skeletonId = player.Value.skeleton.TrackingId;
                    }
                }
            }
            else
            {
                //Player is on the right of the Kinect (Left of the screen)
                double xPos = 5;
                foreach (var player in MainWindow.activeSkeletons)
                {
                    if (player.Value == null || player.Value.skeleton.Position.X < xPos)
                    {
                        xPos = player.Value.skeleton.Position.X;
                        skeletonId = player.Value.skeleton.TrackingId;
                    }
                }
            }
            return skeletonId;
        }
    }
}

