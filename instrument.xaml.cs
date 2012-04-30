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

            handMovements.KinectGuideGesture += new EventHandler<handMovements.GestureEventArgs>(handMovements_KinectGuideGesture);

            setupVoice();

            setupKinectGuide();

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

        DispatcherTimer menuMovementTimer;
        handMovements.scrollDirection menuScrollDirection;
        menuOptions[] kinectGuideMenu = new menuOptions[Enum.GetValues(typeof(menuOptions)).Length];
        int menuPosition;

        enum menuOptions
        {
            //All menu items
            Cancel,
            GoBack,
            TakeAPicture,
            Metronome,
            Guitar,
            LeftyGuitar,
            Drum,
        }

        //Metronome variables
        private bool beatSet = false;

        private DispatcherTimer beatSetTimeout;

        //Speech variables
        SpeechRecognizer.SaidSomethingEventArgs voiceConfirmEvent;
        bool showingConfirmDialog = false;
        DispatcherTimer voiceConfirmTime;

        Dictionary<SpeechRecognizer.Verbs, BitmapImage> voiceVisuals = new Dictionary<SpeechRecognizer.Verbs, BitmapImage>();
        Image confirmationVisual = new Image();

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

            setupVoiceVisuals();
        }

        private void setupVoiceVisuals()
        {
            voiceVisuals.Add(SpeechRecognizer.Verbs.DrumsSwitch, new BitmapImage(new Uri("/Moto;component/images/voice/switchtodrums.png", UriKind.Relative)));
            voiceVisuals.Add(SpeechRecognizer.Verbs.GuitarSwitch, new BitmapImage(new Uri("/Moto;component/images/voice/switchtoguitar.png", UriKind.Relative)));
            voiceVisuals.Add(SpeechRecognizer.Verbs.StartMetronome, new BitmapImage(new Uri("/Moto;component/images/voice/metronome.png", UriKind.Relative)));
            voiceVisuals.Add(SpeechRecognizer.Verbs.StopMetronome, new BitmapImage(new Uri("/Moto;component/images/voice/stopmetronome.png", UriKind.Relative)));
            voiceVisuals.Add(SpeechRecognizer.Verbs.BackToInstruments, new BitmapImage(new Uri("/Moto;component/images/voice/backtoinstruments.png", UriKind.Relative)));
            voiceVisuals.Add(SpeechRecognizer.Verbs.Capture, new BitmapImage(new Uri("/Moto;component/images/voice/takeapicture.png", UriKind.Relative)));
            voiceVisuals.Add(SpeechRecognizer.Verbs.ReturnToStart, new BitmapImage(new Uri("/Moto;component/images/voice/backtostart.png", UriKind.Relative)));
            voiceVisuals.Add(SpeechRecognizer.Verbs.Close, new BitmapImage(new Uri("/Moto;component/images/voice/close.png", UriKind.Relative)));
        }

        private void setupKinectGuide()
        {
            //Loop through the 'menuOptions' enum and assign an array position for each

            int i = 0;
            foreach (menuOptions option in Enum.GetValues(typeof(menuOptions)))
            {
                kinectGuideMenu[i] = option;
                i++;
            }
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

                    if (currentFocus == playerFocus.KinectGuide)
                    {
                        kinectGuideManipulation(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey]);
                    }

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

                if (MainWindow.activeSkeletons.ContainsKey(MainWindow.primarySkeletonKey))
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

        #region Voice Navigation
        //Voice navigation
        private void RecognizerSaidSomething(object sender, SpeechRecognizer.SaidSomethingEventArgs e)
        {
            //What to do when we're pretty certain the player said something we know

            if (e.Verb == SpeechRecognizer.Verbs.SpeechStart || e.Verb == SpeechRecognizer.Verbs.SpeechStop || e.Verb == SpeechRecognizer.Verbs.None)
            {
                return;
            }

            if (!showingConfirmDialog)
            {
                //Confirm the action
                showConfirmation(e);
            }
            else
            {
                //We're already at that stage, shall we do it yes or no?
                if (e.Verb == SpeechRecognizer.Verbs.True)
                {
                    actOnVoiceDecision(true);
                }
                else if (e.Verb == SpeechRecognizer.Verbs.False)
                {
                    actOnVoiceDecision(false);
                }
            }
        }

        private void showConfirmation(SpeechRecognizer.SaidSomethingEventArgs e)
        {
            voiceConfirmEvent = e;

            switch (voiceConfirmEvent.Verb)
            {
                case SpeechRecognizer.Verbs.DrumsSwitch:
                case SpeechRecognizer.Verbs.GuitarSwitch:
                case SpeechRecognizer.Verbs.StartMetronome:
                case SpeechRecognizer.Verbs.StopMetronome:
                case SpeechRecognizer.Verbs.BackToInstruments:
                case SpeechRecognizer.Verbs.Capture:
                case SpeechRecognizer.Verbs.ReturnToStart:
                case SpeechRecognizer.Verbs.Close:
                    voicePromptVisual(true);

                    voiceConfirmTime = new DispatcherTimer();
                    voiceConfirmTime.Interval = TimeSpan.FromMilliseconds(5000);
                    voiceConfirmTime.Tick += new EventHandler(voiceConfirmTime_Tick);
                    voiceConfirmTime.Start();
                    break;
            }
        }

        void voiceConfirmTime_Tick(object sender, EventArgs e)
        {
            //What happens when the confirm box has been there a while
            actOnVoiceDecision(true);
        }
        
        private void actOnVoiceDecision(bool trigger)
        {
            removeConfirmationTime();
            voicePromptVisual(false);
            if (trigger)
            {
                voiceGoDoThis(voiceConfirmEvent);
            }
        }

        private void removeConfirmationTime()
        {
            if (voiceConfirmTime != null)
            {
                voiceConfirmTime.Stop();
                voiceConfirmTime.Tick -= new EventHandler(voiceConfirmTime_Tick);
                voiceConfirmTime = null;
            }
        }

        private void voicePromptVisual(bool showing)
        {
            if (showing)
            {
                showingConfirmDialog = true;
                confirmationVisual = new Image();
                MainCanvas.Children.Add(confirmationVisual);
                MainWindow.animateSlide(confirmationVisual);
                confirmationVisual.Source = voiceVisuals[voiceConfirmEvent.Verb];
                confirmationVisual.Height = 50;
                Canvas.SetTop(confirmationVisual, (MainCanvas.ActualHeight - confirmationVisual.Height - 15));
                Canvas.SetLeft(confirmationVisual, 75);
            }
            else
            {
                showingConfirmDialog = false;
                MainWindow.animateSlide(confirmationVisual, true);
            }
        }

        void voiceGoDoThis(SpeechRecognizer.SaidSomethingEventArgs voiceCommand)
        {
            switch (voiceCommand.Verb)
            {
                case SpeechRecognizer.Verbs.DrumsSwitch:
                    switchInstrument(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey], instrumentList.Drums);
                    break;
                case SpeechRecognizer.Verbs.GuitarSwitch:
                    switchInstrument(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey], instrumentList.GuitarRight);
                    break;
                case SpeechRecognizer.Verbs.StartMetronome:
                    currentFocus = playerFocus.Metronome;
                    metronome.destroyMetronome();
                    MainWindow.sensor.AllFramesReady -= listenForMetronome;
                    metronome.setupMetronome();
                    MainWindow.sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(listenForMetronome);
                    break;
                case SpeechRecognizer.Verbs.StopMetronome:
                    metronome.destroyMetronome();
                    MainWindow.sensor.AllFramesReady -= listenForMetronome;
                    break;
                case SpeechRecognizer.Verbs.BackToInstruments:
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
        #endregion

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

            if (currentFocus == playerFocus.KinectGuide)
            {
                MainWindow.hidePlayerOverlays();
            }
        }

        private void switchInstrument(MainWindow.Player player, instrumentList instrument)
        {
            //Hide all the instrument-specific overlays & set the new instrument
            if (MainWindow.activeSkeletons.ContainsKey(player.skeleton.TrackingId))
            {
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
        }

        //MediaPlayer Dictionary
        int mpCounter = 0;

        Dictionary<int, MediaPlayer> mpDictionary = new Dictionary<int, MediaPlayer>();

        private void generateMediaPlayers()
        {
            mpDictionary.Add(0, null);
            mpDictionary.Add(1, null);
            mpDictionary.Add(2, null);
            mpDictionary.Add(3, null);
            mpDictionary.Add(4, null);
            mpDictionary.Add(5, null);
            mpDictionary.Add(6, null);
            mpDictionary.Add(7, null);
        }

        //Metronome code
        private void listenForMetronome(object sender, EventArgs e)
        {
            if (MainWindow.activeSkeletons.ContainsKey(MainWindow.primarySkeletonKey))
            {
                if (MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton != null)
                {
                    if (Math.Abs(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton.Joints[JointType.HandLeft].Position.X - MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton.Joints[JointType.HandRight].Position.X) < 0.1 && !beatSet)
                    {
                        Console.WriteLine("#############Set the beat##########\n\n");

                        beatSet = true;
                        metronome.metronomeBeat();

                        resetBeatSetTimeout(metronome.theMetronome.Interval);
                    }
                    else if (Math.Abs(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton.Joints[JointType.HandLeft].Position.X - MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton.Joints[JointType.HandRight].Position.X) > 0.25)
                    {
                        beatSet = false;
                    }
                }
                else
                {
                    MainWindow.sensor.AllFramesReady -= listenForMetronome;
                }
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
            currentFocus = playerFocus.None;
            MainWindow.sensor.AllFramesReady -= listenForMetronome;
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

            Canvas.SetZIndex(imgCamera, 1);
            Canvas.SetZIndex(imgGetReady, 1);

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

        void handMovements_KinectGuideGesture(object sender, handMovements.GestureEventArgs e)
        {
            Storyboard sb = this.FindResource("kinectGuideStart") as Storyboard;
            Console.WriteLine("Kinect Guide: " + e.Trigger);
            switch (e.Trigger)
            {
                case handMovements.UserDecisions.Triggered:
                    if (currentFocus != playerFocus.KinectGuide)
                    {
                        sb.Begin();
                        kinectGuideTimer = new DispatcherTimer();
                        kinectGuideTimer.Interval = TimeSpan.FromSeconds(3);
                        kinectGuideTimer.Tick += new EventHandler(kinectGuideTimer_Tick);
                        kinectGuideTimer.Start();
                    }
                    break;
                case handMovements.UserDecisions.NotTriggered:
                    sb.Stop();
                    if (kinectGuideTimer != null)
                    {
                        kinectGuideTimer.Stop();
                        kinectGuideTimer.Tick -= kinectGuideTimer_Tick;
                        kinectGuideTimer = null;
                    }
                    break;
            }
        }

        void kinectGuideTimer_Tick(object sender, EventArgs e)
        {
            currentFocus = playerFocus.KinectGuide;

            if (kinectGuideTimer != null)
            {
                kinectGuideTimer.Stop();
                kinectGuideTimer.Tick -= kinectGuideTimer_Tick;
                kinectGuideTimer = null;
            }

            MainWindow.animateSlide(kinectGuideCanvas, false, false, -150, 0.5);

            kinectGuideCanvas.Visibility = System.Windows.Visibility.Visible;
            imgDimmer.Visibility = System.Windows.Visibility.Visible;

            MainWindow.animateFade(imgDimmer, 0, 0.5, 0.5);

            MainWindow.hidePlayerOverlays();

            menuPosition = 0;
            Canvas.SetTop(kinectGuideCanvas, 0);

            //Listen for swipe gesture
            handMovements.LeftSwipeRight += new EventHandler<handMovements.GestureEventArgs>(handMovements_LeftSwipeRight);

            menuMovementTimer = new DispatcherTimer();
            menuMovementTimer.Interval = TimeSpan.FromMilliseconds(500);
            menuMovementTimer.Tick += new EventHandler(menuMovementTimer_Tick);
            menuMovementTimer.Start();
        }

        void menuMovementTimer_Tick(object sender, EventArgs e)
        {
            menuTick();

            Console.WriteLine(kinectGuideMenu[menuPosition]);
        }

        private void menuTick()
        {
            Skeleton player = MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton;

            if (menuScrollDirection == handMovements.scrollDirection.SmallDown || menuScrollDirection == handMovements.scrollDirection.LargeDown)
            {
                if (menuPosition > 0)
                {
                    animateMenu(false);
                }
            }
            else if (menuScrollDirection == handMovements.scrollDirection.SmallUp || menuScrollDirection == handMovements.scrollDirection.LargeUp)
            {
                if (menuPosition < kinectGuideMenu.Length - 1)
                {
                    animateMenu(true);
                }
            }
        }

        private void kinectGuideManipulation(MainWindow.Player player)
        {
            if (handMovements.leftSwipeRightIn == null)
            {
                //Manipulate the guide if we're not currently swiping to select
                SkeletonPoint bodyMidpoint = handMovements.getMidpoint(player.skeleton.Joints[JointType.HipCenter], player.skeleton.Joints[JointType.ShoulderCenter]);

                double angleValue = handMovements.getAngle(bodyMidpoint, player.skeleton.Joints[JointType.HandLeft].Position);

                handMovements.scrollDirection oldDirection = menuScrollDirection;

                menuScrollDirection = handMovements.sliderMenuValue(player, angleValue);

                if (oldDirection != menuScrollDirection)
                {
                    //Console.WriteLine("CHANGE IN DIRECTION: " + menuScrollDirection);
                    adjustMenuSpeed(menuScrollDirection);

                    if ((oldDirection == handMovements.scrollDirection.None && menuScrollDirection == handMovements.scrollDirection.SmallUp) || (oldDirection == handMovements.scrollDirection.SmallUp && menuScrollDirection == handMovements.scrollDirection.LargeUp) || (oldDirection == handMovements.scrollDirection.None && menuScrollDirection == handMovements.scrollDirection.SmallDown) || (oldDirection == handMovements.scrollDirection.SmallDown && menuScrollDirection == handMovements.scrollDirection.LargeDown))
                    {
                        //If we're increasing in any direction, tick when the speed changes
                        menuTick();
                    }
                }
            }
        }

        private void adjustMenuSpeed(handMovements.scrollDirection scrollDirection)
        {
            if (scrollDirection == handMovements.scrollDirection.SmallUp || scrollDirection == handMovements.scrollDirection.SmallDown)
            {
                menuMovementTimer.Interval = TimeSpan.FromMilliseconds(1000);
                menuMovementTimer.Start();
            }
            else if (scrollDirection == handMovements.scrollDirection.LargeUp || scrollDirection == handMovements.scrollDirection.LargeDown)
            {
                menuMovementTimer.Interval = TimeSpan.FromMilliseconds(250);
                menuMovementTimer.Start();
            }
            else
            {
                menuMovementTimer.Stop();
            }
        }

        void handMovements_LeftSwipeRight(object sender, handMovements.GestureEventArgs e)
        {
            Console.WriteLine("Left swipe right");

            exitKinectGuide();

            switch (kinectGuideMenu[menuPosition])
            {
                case menuOptions.GoBack:
                    returnToStart();
                    break;
                case menuOptions.TakeAPicture:
                    takeAPicture();
                    break;
                case menuOptions.Metronome:
                    currentFocus = playerFocus.Metronome;
                    metronome.destroyMetronome();
                    MainWindow.sensor.AllFramesReady -= listenForMetronome;
                    metronome.setupMetronome();
                    MainWindow.sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(listenForMetronome);
                    break;
                case menuOptions.Guitar:
                    switchInstrument(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey], instrumentList.GuitarRight);
                    break;
                case menuOptions.LeftyGuitar:
                    switchInstrument(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey], instrumentList.GuitarLeft);
                    break;
                case menuOptions.Drum:
                    switchInstrument(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey], instrumentList.Drums);
                    break;
            }
        }

        private void exitKinectGuide()
        {
            Canvas.SetTop(kinectGuideCanvas, 60 * menuPosition);

            MainWindow.animateSlide(kinectGuideCanvas, true, false, -150, 0.5);
            MainWindow.animateFade(imgDimmer, 0.5, 0, 0.5);

            MainWindow.showPlayerOverlays();

            currentFocus = playerFocus.None;

            //Stop listening and reset the flag for next time
            handMovements.LeftSwipeRight -= handMovements_LeftSwipeRight;
            handMovements.LeftSwipeRightStatus = false;

            //Remove menu nav tick
            if (menuMovementTimer != null)
            {
                menuMovementTimer.Stop();
                menuMovementTimer.Tick -= menuMovementTimer_Tick;
                menuMovementTimer = null;
            }
        }

        private void animateMenu(bool up = true, int count = 1)
        {

            Canvas.SetTop(kinectGuideCanvas, 60 * menuPosition);
            //Canvas.SetTop(rectangle1, 60 * -menuPosition);

            DoubleAnimation animation = new DoubleAnimation();

            animation.Duration = TimeSpan.FromMilliseconds(200);
            animation.From = 0;

            if (up)
            {
                menuPosition++;
                //Selection going up, move menu down
                animation.By = animation.From + (60 * count);
            }
            else
            {
                menuPosition--;
                //Selection going down, move menu up
                animation.By = animation.From + (-60 * count);
            }

            TranslateTransform tt = new TranslateTransform();
            kinectGuideCanvas.RenderTransform = tt;

            CircleEase ease = new CircleEase();
            ease.EasingMode = EasingMode.EaseOut;
            animation.EasingFunction = ease;

            tt.BeginAnimation(TranslateTransform.YProperty, animation);
            Console.WriteLine(menuPosition);
        }


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
                MainWindow.hidePlayerOverlays();

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

            MainWindow.showPlayerOverlays();

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
