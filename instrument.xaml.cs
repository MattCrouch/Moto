using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Collections.Generic;
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

        playerFocus currentFocus;

        public enum instrumentList
        {
            None = 0,
            Drums,
            GuitarLeft,
            GuitarRight,
            WallOfSound,
        }

        enum playerFocus
        {
            None = 0,
            KinectGuide,
            Metronome,
            Picture
        }

        void processExistingSkeletons(Dictionary<int, MainWindow.Player> activeSkeletons)
        {
            foreach (var player in activeSkeletons)
            {
                switchInstrument(player.Value, instrumentList.Drums);
                Console.WriteLine(player.Value.skeleton.TrackingId);
            }
        }

        DispatcherTimer kinectGuideTimer;


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

        private bool beatSet = false;

        private DispatcherTimer beatSetTimeout;

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
                beatSetTimeout.Tick +=new EventHandler(beatSetTimeout_Tick);
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

        private void destroyInstrumentSelectionTimer()
        {
            if (instrumentSelectionTimer != null)
            {
                instrumentSelectionTimer.Stop();
                instrumentSelectionTimer.Tick -= instrumentSelectionTimer_Tick;
                instrumentSelectionTimer = null;
            }
        }

        void instrumentSelectionTimer_Tick(object sender, EventArgs e)
        {
            destroyInstrumentSelectionTimer();
            clearInstrumentRefs(MainWindow.activeSkeletons[MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton.TrackingId]);
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

            if (centreX > Canvas.GetLeft(bottom) && centreX < (Canvas.GetLeft(bottom) + bottom.ActualWidth)) {
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

        private void setupVoice()
        {
            MainWindow.mySpeechRecognizer.SaidSomething += this.RecognizerSaidSomething;
            MainWindow.mySpeechRecognizer.ListeningChanged += this.ListeningChanged;
        }

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

        void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                //COLOUR IMAGE CODE
                if (colorFrame == null)
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

                if (skeletonList.Count < MainWindow.activeSkeletons.Count) {
                    List<int> activeList = new List<int>(MainWindow.activeSkeletons.Keys);
                    //We've lost at least one skeleton
                    //find which one(s) it/they are
                    for (int i = 0; i < skeletonList.Count; i++)
                    {
                        if (activeList.Contains(skeletonList[i])) {
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

        private void manageInstrumentImage(MainWindow.Player player, instrumentList instrument)
        {
            //Remove the old image
            MainCanvas.Children.Remove(MainWindow.activeSkeletons[player.skeleton.TrackingId].instrumentImage);

            Image image = new Image();
            //image.Name = "image" + aSkeleton.TrackingId.ToString();

            switch (instrument)
            {
                case instrumentList.Drums:
                    image.Source = new BitmapImage(new Uri("images/drumplaceholder.png", UriKind.Relative));
                    image.Width = 100;
                    image.Height = 100;
                    break;
                case instrumentList.GuitarLeft:
                case instrumentList.GuitarRight:
                    image.Source = new BitmapImage(new Uri("images/guitarplaceholder.png", UriKind.Relative));
                    image.Width = 50;
                    image.Height = 50;
                    break;
            }
            
            MainCanvas.Children.Add(image);

            player.instrumentImage = image;
        }

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
                    startCaptureAnim();
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

        private void switchInstrument(MainWindow.Player player, instrumentList instrument)
        {
            //Hide all the instrument-specific overlays & set the new instrument

            manageInstrumentImage(MainWindow.activeSkeletons[player.skeleton.TrackingId], instrument);

            switch (instrument)
            {
                case instrumentList.Drums:
                    setupDrums(MainWindow.activeSkeletons[player.skeleton.TrackingId]);
                    break;
                case instrumentList.GuitarLeft:
                case instrumentList.GuitarRight:
                    setupGuitar(MainWindow.activeSkeletons[player.skeleton.TrackingId]);
                    break;
            }

            MainWindow.activeSkeletons[player.skeleton.TrackingId].instrument = instrument;

        }

        //Dictionary<Joint, Dictionary<string, double>> difference = new Dictionary<Joint, Dictionary<string, double>>();

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
                        checkDrumHit(player.skeleton, JointType.HandLeft);
                        checkDrumHit(player.skeleton, JointType.HandRight);
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

        private Storyboard flashStoryboard;
        private Rectangle cameraFlash;
        private DispatcherTimer pictureCountdown;

        void uploadPicture(string imageAddress)
        {
            System.Net.WebClient Client = new System.Net.WebClient();

            Client.Headers.Add("Content-Type", "binary/octet-stream");

            Uri uri = new Uri("http://mattcrouch.net/moto/uploadimage.php");

            Client.UploadFileAsync(uri, "POST", imageAddress);

            Client.UploadProgressChanged += new System.Net.UploadProgressChangedEventHandler(Client_UploadProgressChanged);
            Client.UploadFileCompleted += new System.Net.UploadFileCompletedEventHandler(Client_UploadFileCompleted);


            //string s = System.Text.Encoding.UTF8.GetString(result, 0, result.Length);
            //MessageBox.Show(s);
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
            //WHAT HAPPENS WHEN THE UPLOAD HAS FINISHED
            //System.Windows.Controls.Label label = new Label();

            //label.Content = "FINISHED UPLOADING!!!!";

            //MainCanvas.Children.Add(label);
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
        
        private void btnCaptureImage_Click(object sender, RoutedEventArgs e)
        {
            startCaptureAnim();
            //MainWindow.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution1280x960Fps12);
            //MainWindow.sensor.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(sensor_resetResolution);
        }

        private void startCaptureAnim()
        {
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
            pictureCountdown.Stop();
            pictureCountdown.Tick -= new EventHandler(pictureCountdown_Tick);
            uploadPicture(captureImage((BitmapSource)userImage.Source));
            imgGetReady.Visibility = Visibility.Hidden;
            imgCamera.Visibility = Visibility.Hidden;
        }

        /*void sensor_resetResolution(object sender, ColorImageFrameReadyEventArgs e)
        {
            MainWindow.sensor.ColorFrameReady -= new EventHandler<ColorImageFrameReadyEventArgs>(sensor_resetResolution);
            uploadPicture(captureImage(e.OpenColorImageFrame().ToBitmapSource()));
            MainWindow.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
        }*/

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

        private void btnBackFromDrums_Click(object sender, RoutedEventArgs e)
        {
            returnToStart();
        }

        private void returnToStart() {
            MainWindow.sensor.AllFramesReady -= new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);
            destroyVoice();
            metronome.destroyMetronome();
            this.NavigationService.GoBack();
        }

        public void showReadout(string text)
        {
            coordReadout.Content = text;
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
                    startCaptureAnim();
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
    }
}
