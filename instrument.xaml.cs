using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Coding4Fun.Kinect.Wpf;
using Microsoft.Kinect;
using Moto.Speech;

namespace Moto
{
    /// <summary>
    /// Interaction logic for instrument.xaml
    /// </summary>
    public partial class instrument : Page
    {
        public instrument()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            userImage.Source = MainWindow.colorImageBitmap;

            //Listening for when our frames are ready
            MainWindow.sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);

            //Create dictionary definitions for all the Media Players available
            generateMediaPlayers();

            handMovements.KinectGuideGesture += new EventHandler<handMovements.GestureEventArgs>(handMovements_KinectGuideInstrument);

            setupVoice();
            currentFocus = playerFocus.None;
            processExistingSkeletons(MainWindow.activeSkeletons);

            this.FocusVisualStyle = null;
            this.Focus();
        }

        //Player's current focus
        playerFocus currentFocus;

        enum playerFocus
        {
            None = 0,
            KinectGuide,
            Metronome,
            Picture
        }

        //What instruments are available (USED IN WALL OF SOUND TOO)
        public enum instrumentList
        {
            None = 0,
            Drums,
            GuitarLeft,
            GuitarRight,
            WallOfSound,
        }

        //Kinect guide variables
        DispatcherTimer kinectGuideTimer;

        bool handUp = false;

        DispatcherTimer instrumentSelectionTimer;
        instrumentSelectionOptions currentInstrumentSelection = instrumentSelectionOptions.None;

        enum instrumentSelectionOptions
        {
            None = 0,
            Guitar,
            GuitarLeft,
            Drums,
            Metronome,
        }

        //Metronome variables
        private bool beatSet = false;

        private DispatcherTimer beatSetTimeout;

        //Image capture variables
        private Storyboard flashStoryboard;
        private Rectangle cameraFlash;
        private DispatcherTimer pictureCountdown;
        private DispatcherTimer imgProcessDelay;
        private TextBlock uploadFeedback;
        private Image cameraUpload;

        //Housekeeping
        void processExistingSkeletons(Dictionary<int, MainWindow.Player> activeSkeletons)
        {
            foreach (var player in activeSkeletons)
            {
                switchInstrument(player.Value, instrumentList.Drums);
                Console.WriteLine(player.Value.skeleton.TrackingId);
            }
        }

        private void setupVoice()
        {
            MainWindow.mySpeechRecognizer.SaidSomething += this.RecognizerSaidSomething;
            MainWindow.mySpeechRecognizer.ListeningChanged += this.ListeningChanged;
        }

        //Skeleton processing code (ran every frame)
        void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                //COLOUR IMAGE CODE
                if (colorFrame == null || colorFrame.Format != MainWindow.sensor.ColorStream.Format)
                {
                    return;
                }
                
                byte[] pixelData = new byte[colorFrame.PixelDataLength];
                colorFrame.CopyPixelDataTo(pixelData);

                MainWindow.colorImageBitmap.WritePixels(MainWindow.colorImageBitmapRect, pixelData, MainWindow.colorImageStride, 0);

                //userImage.Source = colorFrame.ToBitmapSource();
            }

            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                //DEPTH IMAGE CODE
                if (depthFrame == null)
                {
                    return;
                }

                //userDepth.Source = depthFrame.ToBitmapSource();
            }

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                //SKELETON CODE
                if (skeletonFrame == null)
                {
                    return;
                }

                skeletonFrame.CopySkeletonDataTo(MainWindow.allSkeletons);

                Skeleton aSkeleton;
                List<int> skeletonList = new List<int>();

                for (int i = 0; i < MainWindow.allSkeletons.Length; i++)
                {
                    aSkeleton = MainWindow.allSkeletons[i];

                    if (aSkeleton.TrackingState == SkeletonTrackingState.Tracked)
                    {

                        skeletonList.Add(aSkeleton.TrackingId);

                        //A new skeleton?
                        if (!MainWindow.activeSkeletons.ContainsKey(aSkeleton.TrackingId))
                        {
                            if (MainWindow.playerAdded(aSkeleton))
                            {
                                switchInstrument(MainWindow.activeSkeletons[aSkeleton.TrackingId], instrumentList.GuitarLeft);
                            }
                        }

                        //The player we're referencing at this point in the loop
                        MainWindow.Player player = MainWindow.activeSkeletons[aSkeleton.TrackingId];

                        handMovements.trackJointProgression(player.skeleton, player.skeleton.Joints[JointType.HandLeft]);
                        handMovements.trackJointProgression(player.skeleton, player.skeleton.Joints[JointType.HandRight]);
                        handMovements.trackJointProgression(player.skeleton, player.skeleton.Joints[JointType.FootLeft]);
                        handMovements.trackJointProgression(player.skeleton, player.skeleton.Joints[JointType.FootRight]);
                        instrumentUpdate(player);
                    }
                }

                if (MainWindow.activeSkeletons.Count > 0)
                {
                    int tempKey = MainWindow.primarySkeletonKey;
                    MainWindow.primarySkeletonKey = MainWindow.selectPrimarySkeleton(MainWindow.activeSkeletons);

                    alignPrimaryGlow(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey]);

                    if (tempKey != MainWindow.primarySkeletonKey)
                    {
                        //Primary Skeleton changed
                        highlightPrimarySkeleton(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey]);
                    }
                }

                if (skeletonList.Count < MainWindow.activeSkeletons.Count)
                {
                    List<int> activeList = new List<int>(MainWindow.activeSkeletons.Keys);
                    //We've lost at least one skeleton
                    //find which one(s) it/they are
                    for (int i = 0; i < skeletonList.Count; i++)
                    {
                        if (activeList.Contains(skeletonList[i]))
                        {
                            activeList.Remove(skeletonList[i]);
                        }
                    }

                    //Remove them
                    for (int i = 0; i < activeList.Count; i++)
                    {
                        MainCanvas.Children.Remove(MainWindow.activeSkeletons[activeList[i]].instrumentImage);
                        clearInstrumentRefs(MainWindow.activeSkeletons[activeList[i]]);
                        MainWindow.playerRemoved(activeList[i]);
                        hitArea.Remove(activeList[i]);
                        insideArea.Remove(activeList[i]);

                    }

                    activeList = null;
                }

                skeletonList = null;

                if (MainWindow.activeSkeletons.Count > 0)
                {
                    handMovements.listenForGestures(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton);
                }
            }
        }

        private void instrumentUpdate(MainWindow.Player player)
        {
            switch (player.instrument)
            {
                case instrumentList.Drums:
                    //DRUMS
                    defineHitAreas(player);
                    //showReadout((skeleton.Joints[JointType.HipLeft].Position.X - skeleton.Joints[JointType.HipRight].Position.X).ToString());
                    if (currentFocus == playerFocus.None)
                    {
                        checkDrumHit(player, JointType.HandLeft);
                        checkDrumHit(player, JointType.HandRight);
                        checkDrumHit(player, JointType.FootLeft);
                        checkDrumHit(player, JointType.FootRight);
                    }
                    break;
                case instrumentList.GuitarRight:
                    //GUITAR
                    defineStrumArea(player);
                    if (currentFocus == playerFocus.None)
                    {
                        checkStrum(player, JointType.HandRight);
                    }
                    break;
                case instrumentList.GuitarLeft:
                    //GUITAR LEFTY
                    defineStrumArea(player);
                    if (currentFocus == playerFocus.None)
                    {
                        checkStrum(player, JointType.HandLeft);
                    }
                    break;
            }
        }

        private double scaledWidth(MainWindow.Player player, instrumentList instrument)
        {
            //Player distance (Converted to centimetres)
            double distance = player.skeleton.Position.Z * 100;
            double width = 0;

            switch (instrument)
            {
                case instrumentList.Drums:
                    width = 1112.5 * Math.Pow(Math.E, -0.006 * distance);
                    break;
                case instrumentList.GuitarRight:
                case instrumentList.GuitarLeft:
                    width = 680.35 * Math.Pow(Math.E, -0.004 * distance);
                    break;
            }

            return width;
        }

        //Voice navigation
        private void RecognizerSaidSomething(object sender, SpeechRecognizer.SaidSomethingEventArgs e)
        {
            switch (e.Verb)
            {
                case SpeechRecognizer.Verbs.DrumsSwitch:
                    switchInstrument(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey], instrumentList.Drums);
                    break;
                case SpeechRecognizer.Verbs.GuitarSwitch:
                    switchInstrument(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey], instrumentList.GuitarRight);
                    break;
                case SpeechRecognizer.Verbs.StartMetronome:
                    metronome.setupMetronome();
                    currentInstrumentSelection = instrumentSelectionOptions.Metronome;
                    MainWindow.sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(listenForMetronome);
                    break;
                case SpeechRecognizer.Verbs.StopMetronome:
                    metronome.destroyMetronome();
                    currentInstrumentSelection = instrumentSelectionOptions.None;
                    MainWindow.sensor.AllFramesReady -= listenForMetronome;
                    break;
                case SpeechRecognizer.Verbs.BackToInstruments:
                    currentInstrumentSelection = instrumentSelectionOptions.None;
                    break;
                case SpeechRecognizer.Verbs.Capture:
                    takeAPicture();
                    break;
                case SpeechRecognizer.Verbs.ReturnToStart:
                    returnToStart();
                    break;
                case SpeechRecognizer.Verbs.Close:
                    Application.Current.Shutdown();
                    break;
            }
        }

        private void ListeningChanged(object sender, SpeechRecognizer.ListeningChangedEventArgs e)
        {
            if (e.Paused)
            {
                MainWindow.mySpeechRecognizer.stopListening(MainCanvas);
            }
            else
            {
                MainWindow.mySpeechRecognizer.startListening(MainCanvas);
            }
        }

        //Instrument management code
        private void manageInstrumentImage(MainWindow.Player player, instrumentList instrument)
        {
            //Remove the old image
            MainCanvas.Children.Remove(MainWindow.activeSkeletons[player.skeleton.TrackingId].instrumentImage);

            Image image = new Image();
            //image.Name = "image" + aSkeleton.TrackingId.ToString();

            switch (instrument)
            {
                case instrumentList.Drums:
                    image.Source = new BitmapImage(new Uri("images/drums.png", UriKind.Relative));
                    //image.Width = 360;
                    //image.Height = image.Width * 0.80;
                    break;
                case instrumentList.GuitarLeft:
                case instrumentList.GuitarRight:
                    image.Source = new BitmapImage(new Uri("images/guitar.png", UriKind.Relative));
                    //image.Height = 225;
                    //image.Width = image.Height * 0.35;
                    break;
            }

            MainCanvas.Children.Add(image);

            player.instrumentImage = image;
        }

        private void switchInstrument(MainWindow.Player player, instrumentList instrument)
        {
            //Hide all the instrument-specific overlays & set the new instrument

            manageInstrumentImage(MainWindow.activeSkeletons[player.skeleton.TrackingId], instrument);

            switch (instrument)
            {
                case instrumentList.Drums:
                    setupDrums(MainWindow.activeSkeletons[player.skeleton.TrackingId]);
                    MainWindow.activeSkeletons[player.skeleton.TrackingId].mode = MainWindow.PlayerMode.None;
                    break;
                case instrumentList.GuitarLeft:
                case instrumentList.GuitarRight:
                    setupGuitar(MainWindow.activeSkeletons[player.skeleton.TrackingId]);
                    MainWindow.activeSkeletons[player.skeleton.TrackingId].mode = MainWindow.PlayerMode.Acoustic;
                    break;
            }

            MainWindow.activeSkeletons[player.skeleton.TrackingId].instrument = instrument;

        }

        //Kinect guide code
        void handMovements_KinectGuideInstrument(object sender, handMovements.GestureEventArgs e)
        {
            if (currentFocus != playerFocus.KinectGuide || currentFocus != playerFocus.None)
            {
                Storyboard sb = this.FindResource("kinectGuideStart") as Storyboard;
                switch (e.Trigger)
                {
                    case handMovements.UserDecisions.Triggered:
                        if (currentFocus != playerFocus.KinectGuide)
                        {
                            //imgKinectGuide.Visibility = Visibility.Visible;
                            kinectGuideTimer = new DispatcherTimer();
                            kinectGuideTimer.Interval = TimeSpan.FromSeconds(3);
                            kinectGuideTimer.Start();
                            kinectGuideTimer.Tick += new EventHandler(kinectGuideTimer_Tick);
                            sb.Begin();
                        }
                        break;
                    case handMovements.UserDecisions.NotTriggered:
                        sb.Stop();
                        if (kinectGuideTimer != null)
                        {
                            kinectGuideTimer.Stop();
                            kinectGuideTimer.Tick -= new EventHandler(kinectGuideTimer_Tick);
                            kinectGuideTimer = null;
                        }
                        imgKinectGuide.Visibility = Visibility.Hidden;
                        break;
                }
            }
        }

        void kinectGuideTimer_Tick(object sender, EventArgs e)
        {
            showKinectGuide();

            //MainWindow.sensor.AllFramesReady -= selectAnInstrument;
        }

        private void showKinectGuide()
        {
            //Show the instruments, and make sure nothing else can be triggered at this time
            currentFocus = playerFocus.KinectGuide;

            metronome.destroyMetronome();

            MainWindow.sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(selectAnInstrument);

            Storyboard sb = this.FindResource("instrumentSelectionIn") as Storyboard;
            sb.AutoReverse = false;
            sb.Begin();
        }

        private void setupInstrumentSelectionTimer()
        {
            if (instrumentSelectionTimer != null)
            {
                destroyInstrumentSelectionTimer();
            }
            instrumentSelectionTimer = new DispatcherTimer();
            instrumentSelectionTimer.Interval = TimeSpan.FromSeconds(3);
            instrumentSelectionTimer.Tick += new EventHandler(instrumentSelectionTimer_Tick);
            instrumentSelectionTimer.Start();
        }

        private void selectAnInstrument(object sender, EventArgs e)
        {
            if (MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton != null)
            {
                Joint leftHand = MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton.Joints[JointType.HandLeft];
                double relXPos = MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton.Position.X - MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton.Joints[JointType.HandLeft].Position.X;

                if (handUp)
                {
                    Canvas.SetLeft(imgSelRef, 640 - distQuotient(0, 0.5, relXPos, 0, userImage.Width));
                    if (checkOverlay(imgSelRef, imgSelGuitar))
                    {
                        currentInstrumentSelection = instrumentSelectionOptions.Guitar;
                        if (instrumentSelectionTimer == null)
                        {
                            Console.WriteLine("Setting up a timer for Guitar");
                            setupInstrumentSelectionTimer();
                        }
                    }
                    else if (checkOverlay(imgSelRef, imgSelGuitarLeft))
                    {
                        currentInstrumentSelection = instrumentSelectionOptions.GuitarLeft;
                        if (instrumentSelectionTimer == null)
                        {
                            Console.WriteLine("Setting up a timer for Guitar Left");
                            setupInstrumentSelectionTimer();
                        }
                    }
                    else if (checkOverlay(imgSelRef, imgSelDrums))
                    {
                        currentInstrumentSelection = instrumentSelectionOptions.Drums;
                        if (instrumentSelectionTimer == null)
                        {
                            Console.WriteLine("Setting up a timer for Drums");
                            setupInstrumentSelectionTimer();
                        }
                    }
                    else if (checkOverlay(imgSelRef, imgSelMetronome))
                    {
                        currentInstrumentSelection = instrumentSelectionOptions.Metronome;
                        if (instrumentSelectionTimer == null)
                        {
                            Console.WriteLine("Setting up a timer for Metronome");
                            setupInstrumentSelectionTimer();
                        }
                    }
                    else
                    {
                        currentInstrumentSelection = instrumentSelectionOptions.None;
                    }



                    if (instrumentSelectionTimer != null && currentInstrumentSelection == instrumentSelectionOptions.None)
                    {
                        destroyInstrumentSelectionTimer();
                    }
                }

                if (handUp && leftHand.Position.Y < MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton.Position.Y)
                {
                    handUp = false;
                    Console.WriteLine(handUp);
                    exitKinectGuide();
                    return;
                }

                if (leftHand.Position.Y > MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton.Position.Y)
                {
                    handUp = true;
                }
            }
        }

        void instrumentSelectionTimer_Tick(object sender, EventArgs e)
        {
            destroyInstrumentSelectionTimer();
            if (currentInstrumentSelection != instrumentSelectionOptions.Metronome)
            {
                clearInstrumentRefs(MainWindow.activeSkeletons[MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton.TrackingId]);
            }

            switch (currentInstrumentSelection)
            {
                case instrumentSelectionOptions.Guitar:
                    switchInstrument(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey], instrumentList.GuitarRight);
                    currentInstrumentSelection = instrumentSelectionOptions.None;
                    break;
                case instrumentSelectionOptions.GuitarLeft:
                    switchInstrument(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey], instrumentList.GuitarLeft);
                    currentInstrumentSelection = instrumentSelectionOptions.None;
                    break;
                case instrumentSelectionOptions.Drums:
                    switchInstrument(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey], instrumentList.Drums);
                    currentInstrumentSelection = instrumentSelectionOptions.None;
                    break;
                case instrumentSelectionOptions.Metronome:
                    currentInstrumentSelection = instrumentSelectionOptions.Metronome;
                    //switchInstrument(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey], instrumentList.None);
                    metronome.setupMetronome();
                    MainWindow.sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(listenForMetronome);
                    break;
            }

            handUp = false;
            exitKinectGuide();
        }

        private bool checkOverlay(FrameworkElement top, FrameworkElement bottom, bool listenX = true, bool listenY = true)
        {
            //Checks if the centre of the top FE is anywhere within bottom FE
            double centreX = Canvas.GetLeft(top) + (top.ActualWidth / 2);
            double centreY = Canvas.GetTop(top) + (top.ActualHeight / 2);

            if (centreX > Canvas.GetLeft(bottom) && centreX < (Canvas.GetLeft(bottom) + bottom.ActualWidth))
            {
                if (centreY > Canvas.GetTop(bottom) && centreY < (Canvas.GetTop(bottom) + bottom.ActualHeight))
                {
                    return true;
                }
            }

            return false;
        }

        private void exitKinectGuide()
        {
            currentFocus = playerFocus.None;

            destroyInstrumentSelectionTimer();

            MainWindow.sensor.AllFramesReady -= selectAnInstrument;

            Storyboard sb = this.FindResource("instrumentSelectionIn") as Storyboard;
            sb.AutoReverse = true;
            sb.Begin(this, true);
            sb.Seek(this, new TimeSpan(0, 0, 0), TimeSeekOrigin.Duration);
        }

        private void destroyInstrumentSelectionTimer()
        {
            if (instrumentSelectionTimer != null)
            {
                instrumentSelectionTimer.Stop();
                instrumentSelectionTimer.Tick -= instrumentSelectionTimer_Tick;
                instrumentSelectionTimer = null;
            }
        }

        //MediaPlayer Dictionary
        int mpCounter = 0;

        Dictionary<int, MediaPlayer> mpDictionary = new Dictionary<int, MediaPlayer>();

        private void generateMediaPlayers()
        {
            mpDictionary.Add(0, new MediaPlayer());
            mpDictionary.Add(1, new MediaPlayer());
            mpDictionary.Add(2, new MediaPlayer());
            mpDictionary.Add(3, new MediaPlayer());
            mpDictionary.Add(4, new MediaPlayer());
            mpDictionary.Add(5, new MediaPlayer());
            mpDictionary.Add(6, new MediaPlayer());
            mpDictionary.Add(7, new MediaPlayer());
        }

        //Metronome code
        private void listenForMetronome(object sender, EventArgs e)
        {
            if (currentInstrumentSelection == instrumentSelectionOptions.Metronome && MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton != null)
            {
                if (Math.Abs(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton.Joints[JointType.HandLeft].Position.X - MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton.Joints[JointType.HandRight].Position.X) < 0.1 && !beatSet)
                {
                    Console.WriteLine("#############Set the beat##########\n\n");

                    beatSet = true;
                    metronome.metronomeBeat();

                    resetBeatSetTimeout(metronome.theMetronome.Interval);
                }
                else if (Math.Abs(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton.Joints[JointType.HandLeft].Position.X - MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton.Joints[JointType.HandRight].Position.X) > 0.4)
                {
                    beatSet = false;
                }
            }
            else
            {
                MainWindow.sensor.AllFramesReady -= listenForMetronome;
            }
        }

        private void resetBeatSetTimeout(TimeSpan interval, bool restart = true)
        {
            if (beatSetTimeout == null)
            {
                beatSetTimeout = new DispatcherTimer();
            }

            beatSetTimeout.Interval = TimeSpan.FromTicks(interval.Ticks * 3);

            if (!beatSetTimeout.IsEnabled)
            {
                beatSetTimeout.Tick += new EventHandler(beatSetTimeout_Tick);
            }

            if (restart)
            {
                beatSetTimeout.Start();
            }
        }

        void beatSetTimeout_Tick(object sender, EventArgs e)
        {
            //Stop listening for the metronome
            currentInstrumentSelection = instrumentSelectionOptions.None;
            beatSetTimeout.Tick -= beatSetTimeout_Tick;
            beatSetTimeout = null;
        }

        //Primary player identification
        private void alignPrimaryGlow(MainWindow.Player player)
        {
            ColorImagePoint leftPoint = MainWindow.sensor.MapSkeletonPointToColor(player.skeleton.Joints[JointType.HandLeft].Position, ColorImageFormat.RgbResolution640x480Fps30);
            ColorImagePoint rightPoint = MainWindow.sensor.MapSkeletonPointToColor(player.skeleton.Joints[JointType.HandRight].Position, ColorImageFormat.RgbResolution640x480Fps30);

            Canvas.SetLeft(imgPrimaryGlowLeft, leftPoint.X - (imgPrimaryGlowLeft.Width / 2));
            Canvas.SetTop(imgPrimaryGlowLeft, leftPoint.Y - (imgPrimaryGlowLeft.Height / 2));

            Canvas.SetLeft(imgPrimaryGlowRight, rightPoint.X - (imgPrimaryGlowRight.Width / 2));
            Canvas.SetTop(imgPrimaryGlowRight, rightPoint.Y - (imgPrimaryGlowRight.Height / 2));
        }

        private void highlightPrimarySkeleton(MainWindow.Player player)
        {
            Storyboard sb = this.FindResource("primaryGlow") as Storyboard;
            sb.Begin();
        }

        #region Image Capture
        
        void takeAPicture()
        {
            currentFocus = playerFocus.Picture;
            toggleRGB(ColorImageFormat.RgbResolution1280x960Fps12);

            Storyboard sb = this.FindResource("photoPrep") as Storyboard;
            sb.AutoReverse = false;
            sb.Begin();

            Storyboard sb2 = this.FindResource("photoLoading") as Storyboard;
            sb2.Begin();
        }

        void uploadPicture(string imageAddress)
        {
            System.Net.WebClient Client = new System.Net.WebClient();

            Client.Headers.Add("Content-Type", "binary/octet-stream");

            Uri uri = new Uri("http://mattcrouch.net/moto/uploadimage.php");

            Client.UploadFileAsync(uri, "POST", imageAddress);

            Client.UploadProgressChanged += new System.Net.UploadProgressChangedEventHandler(Client_UploadProgressChanged);
            Client.UploadFileCompleted += new System.Net.UploadFileCompletedEventHandler(Client_UploadFileCompleted);
        }

        void cameraFlash_Loaded(object sender, RoutedEventArgs e)
        {
            flashStoryboard.Begin(this);
        }

        void flashStoryboard_Completed(object sender, EventArgs e)
        {
            MainCanvas.Children.Remove(cameraFlash);
            this.UnregisterName("cameraFlash");
        }

        void Client_UploadFileCompleted(object sender, System.Net.UploadFileCompletedEventArgs e)
        {
            string uploadString;

            if (e.Error != null)
            {
                uploadString = "cannot upload to moto";
            }
            else
            {
                uploadString = System.Text.Encoding.UTF8.GetString(e.Result, 0, e.Result.Length);
            }

            //WHAT HAPPENS WHEN THE UPLOAD HAS FINISHED
            uploadFeedback = new TextBlock();
            uploadFeedback.FontFamily = new FontFamily(new Uri("pack://application:,,,/Fonts/La-chata-normal.ttf"), "La Chata");
            uploadFeedback.FontSize = 40;
            uploadFeedback.Foreground = new SolidColorBrush(Color.FromRgb(230, 229, 255));
            uploadFeedback.Text = uploadString;
      
            cameraUpload = new Image();
            cameraUpload.Source = new BitmapImage(new Uri("/Moto;component/images/camera-game.png", UriKind.Relative));
            cameraUpload.Width = 70;

            MainCanvas.Children.Add(uploadFeedback);
            MainCanvas.Children.Add(cameraUpload);

            Canvas.SetLeft(uploadFeedback, 70);

            MainWindow.animateSlide(uploadFeedback);
            MainWindow.animateSlide(cameraUpload);
        }

        void Client_UploadProgressChanged(object sender, System.Net.UploadProgressChangedEventArgs e)
        {
            Console.WriteLine("Download {0}% complete. ", e.ProgressPercentage);
        }

        private string captureImage(BitmapSource image)
        {
            cameraFlash = new Rectangle();
            cameraFlash.Height = 800;
            cameraFlash.Width = 800;
            cameraFlash.Fill = new SolidColorBrush(Colors.White);
            cameraFlash.Name = "cameraFlash";
            this.RegisterName(cameraFlash.Name, cameraFlash);

            DoubleAnimation da = new DoubleAnimation();
            da.From = 1.0;
            da.To = 0.0;
            da.Duration = new Duration(TimeSpan.FromSeconds(1));

            flashStoryboard = new Storyboard();
            flashStoryboard.Children.Add(da);
            Storyboard.SetTargetName(da, cameraFlash.Name);
            Storyboard.SetTargetProperty(da, new PropertyPath(Rectangle.OpacityProperty));

            cameraFlash.Loaded += new RoutedEventHandler(cameraFlash_Loaded);
            MainCanvas.Children.Add(cameraFlash);

            flashStoryboard.Completed += new EventHandler(flashStoryboard_Completed);
            
            string imageAddress = "moto-" + DateTime.Now.ToString("ddMMyyyy-HHmmss") + ".jpg";

            image.Save(imageAddress, ImageFormat.Jpeg);

            return imageAddress;

        }

        private void startCaptureAnim()
        {
            //MainWindow.animateSlide(preparingImg, true, true, 5, 0.5);

            Storyboard sb2 = this.FindResource("photoPrep") as Storyboard;
            sb2.AutoReverse = true;
            sb2.Begin(this, true);
            sb2.Seek(this, new TimeSpan(0, 0, 0), TimeSeekOrigin.Duration);

            Storyboard sb3 = this.FindResource("photoLoading") as Storyboard;
            sb2.Stop();

            imgGetReady.Visibility = Visibility.Visible;
            imgCamera.Visibility = Visibility.Visible;
            pictureCountdown = new DispatcherTimer();
            pictureCountdown.Interval = TimeSpan.FromSeconds(3);
            pictureCountdown.Tick += new EventHandler(pictureCountdown_Tick);
            pictureCountdown.Start();

            Storyboard sb = this.FindResource("cameraCountdown") as Storyboard;
            sb.Begin();
        }

        void pictureCountdown_Tick(object sender, EventArgs e)
        {
            currentFocus = playerFocus.None;
            pictureCountdown.Stop();
            pictureCountdown.Tick -= new EventHandler(pictureCountdown_Tick);
            uploadPicture(captureImage((BitmapSource)userImage.Source));
            imgGetReady.Visibility = Visibility.Hidden;
            imgCamera.Visibility = Visibility.Hidden;
            toggleRGB(ColorImageFormat.RgbResolution640x480Fps30, 5000);
        }

        #endregion

        //Tidy up
        private void destroyVoice()
        {
            MainWindow.mySpeechRecognizer.toggleListening(false);
            MainWindow.mySpeechRecognizer.SaidSomething -= this.RecognizerSaidSomething;
            MainWindow.mySpeechRecognizer.ListeningChanged -= this.ListeningChanged;
        }

        private void clearInstrumentRefs(MainWindow.Player player)
        {
            switch (player.instrument)
            {
                case instrumentList.Drums:
                    hitArea.Remove(player.skeleton.TrackingId);
                    insideArea.Remove(player.skeleton.TrackingId);
                    break;
                case instrumentList.GuitarLeft:
                case instrumentList.GuitarRight:
                    strumArea.Remove(player.skeleton.TrackingId);
                    insideStrumArea.Remove(player.skeleton.TrackingId);
                    break;
            }
        }

        private void returnToStart()
        {
            MainWindow.sensor.AllFramesReady -= new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);
            destroyVoice();
            metronome.destroyMetronome();
            this.NavigationService.GoBack();
        }

        //Reused methods
        private double getCoords(string axis, Joint joint)
        {
            double position = 0;
            switch (axis)
            {
                case "X":
                    position = joint.Position.X;
                    break;
                case "Y":
                    position = joint.Position.Y;
                    break;
                case "Z":
                    position = joint.Position.Z;
                    break;
            }


            return position;
        }

        public static double distQuotient(double unitMin, double unitMax, double playerDist, double scaleMin, double scaleMax)
        {
            double quotient;

            double no1 = unitMax - unitMin;
            double no1Pos = playerDist - unitMin;

            double percent = (no1Pos / no1) * 100;

            double pcDiff = (scaleMax - scaleMin) / 100;

            quotient = scaleMin + (pcDiff * percent);

            if (quotient > scaleMax)
            {
                quotient = scaleMax;
            }
            else if (quotient < scaleMin)
            {
                quotient = scaleMin;
            }

            return quotient;
        }

        //Development code
        private void Page_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //Development shortcuts
            switch (e.Key)
            {
                case System.Windows.Input.Key.S:
                    //Toggle speech recognition
                    MainWindow.mySpeechRecognizer.toggleSpeech();
                    break;
                case System.Windows.Input.Key.K:
                    //Invoke the Kinect Guide
                    if (currentFocus != playerFocus.KinectGuide)
                    {
                        showKinectGuide();
                    }
                    else
                    {
                        exitKinectGuide();
                    }
                    break;
                case System.Windows.Input.Key.C:
                    //Take a picture
                    takeAPicture();
                    break;
                case System.Windows.Input.Key.F:
                    //Toggle RGB input style
                    if (MainWindow.sensor.ColorStream.Format == ColorImageFormat.RgbResolution640x480Fps30)
                    {
                        toggleRGB(ColorImageFormat.RgbResolution1280x960Fps12);
                    }
                    else
                    {
                        toggleRGB(ColorImageFormat.RgbResolution640x480Fps30);
                    }
                    break;
                case System.Windows.Input.Key.B:
                    //Back to the start screen
                    returnToStart();
                    break;
                case System.Windows.Input.Key.Escape:
                    //Close the application
                    Application.Current.Shutdown();
                    break;
            }
        }

        private void toggleRGB(ColorImageFormat format, int delay = 3000)
        {
            if (MainWindow.sensor.ColorStream.Format != format)
            {
                foreach (var player in MainWindow.activeSkeletons)
                {
                    player.Value.instrumentImage.Visibility = System.Windows.Visibility.Hidden;
                }

                MainWindow.sensor.ColorStream.Enable(format);

                MainWindow.colorImageBitmap = new WriteableBitmap(MainWindow.sensor.ColorStream.FrameWidth, MainWindow.sensor.ColorStream.FrameHeight, 96, 96, PixelFormats.Bgr32, null);
                MainWindow.colorImageBitmapRect = new Int32Rect(0, 0, MainWindow.sensor.ColorStream.FrameWidth, MainWindow.sensor.ColorStream.FrameHeight);
                MainWindow.colorImageStride = MainWindow.sensor.ColorStream.FrameWidth * MainWindow.sensor.ColorStream.FrameBytesPerPixel;

                imgProcessDelay = new DispatcherTimer();
                imgProcessDelay.Interval = TimeSpan.FromMilliseconds(delay);
                imgProcessDelay.Tick += new EventHandler(imgProcessDelay_Tick);
                imgProcessDelay.Start();
            }
        }

        void imgProcessDelay_Tick(object sender, EventArgs e)
        {
            if (imgProcessDelay != null)
            {
                imgProcessDelay.Tick -= imgProcessDelay_Tick;
                imgProcessDelay.Stop();
                imgProcessDelay = null;
            }

            foreach (var player in MainWindow.activeSkeletons)
            {
                player.Value.instrumentImage.Visibility = System.Windows.Visibility.Visible;
            }

            userImage.Source = MainWindow.colorImageBitmap;

            if (currentFocus == playerFocus.Picture)
            {
                startCaptureAnim();
            }

            if (uploadFeedback != null)
            {
                MainCanvas.Children.Remove(uploadFeedback);
                uploadFeedback = null;
            }

            if (cameraUpload != null)
            {
                MainCanvas.Children.Remove(cameraUpload);
                cameraUpload = null;
            }
        }
    }
}
