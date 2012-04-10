using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using Coding4Fun.Kinect.Wpf;
using Moto.Speech;
using System.Windows.Media.Animation;
using System.Collections.Generic;

namespace Moto
{
    /// <summary>
    /// Interaction logic for StartScreen.xaml
    /// </summary>
    public partial class StartScreen : Page
    {
        public StartScreen()
        {
            InitializeComponent();
             
            //Show the 'loading mic' animation
            //(It's here so it only runs the once)
            Storyboard sb = this.FindResource("loadingMic") as Storyboard;
            sb.Begin();

            MainWindow.animateSlide(imgStepInToPlay);
            MainWindow.animateSlide(imgMotoLogo);

            setupVoiceVisuals();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            setupStartScreen();
            setupVoice();
            voicePromptVisual(false);
            startScreenUserImage.Source = MainWindow.colorImageBitmap;

            this.FocusVisualStyle = null;
            this.Focus();
        }

        //Mode selection variables
        DispatcherTimer modeDecision;
        modeSelected selectedMode;

        enum modeSelected
        {
            None = 0,
            Instrument,
            WallOfSound,
        }

        //Speech variables
        SpeechRecognizer.SaidSomethingEventArgs voiceConfirmEvent;
        bool showingConfirmDialog = false;
        DispatcherTimer voiceConfirmTime;

        Dictionary<SpeechRecognizer.Verbs, BitmapImage> voiceVisuals = new Dictionary<SpeechRecognizer.Verbs, BitmapImage>();
        Image confirmationVisual = new Image();

        private void setupStartScreen()
        {
            MainWindow.sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);
            handMovements.KinectGuideGesture += new EventHandler<handMovements.GestureEventArgs>(handMovements_KinectGuideGesture);
            handMovements.LeftGesture += new EventHandler<handMovements.GestureEventArgs>(handMovements_LeftGesture);
            handMovements.RightGesture += new EventHandler<handMovements.GestureEventArgs>(handMovements_RightGesture);
        }

        private void setupVoice()
        {
            MainWindow.mySpeechRecognizer.SaidSomething += this.RecognizerSaidSomething;
            MainWindow.mySpeechRecognizer.ListeningChanged += this.ListeningChanged;
        }

        private void setupVoiceVisuals()
        {
            voiceVisuals.Add(SpeechRecognizer.Verbs.Close, new BitmapImage(new Uri("/Moto;component/images/voice/close.png", UriKind.Relative)));
            voiceVisuals.Add(SpeechRecognizer.Verbs.Instrument, new BitmapImage(new Uri("/Moto;component/images/voice/instrument.png", UriKind.Relative)));
            voiceVisuals.Add(SpeechRecognizer.Verbs.WallOfSound, new BitmapImage(new Uri("/Moto;component/images/voice/wallofsound.png", UriKind.Relative)));
            voiceVisuals.Add(SpeechRecognizer.Verbs.KinectUp, new BitmapImage(new Uri("/Moto;component/images/voice/angleup.png", UriKind.Relative)));
            voiceVisuals.Add(SpeechRecognizer.Verbs.KinectUpSmall, new BitmapImage(new Uri("/Moto;component/images/voice/angleslightlyup.png", UriKind.Relative)));
            voiceVisuals.Add(SpeechRecognizer.Verbs.KinectDown, new BitmapImage(new Uri("/Moto;component/images/voice/angledown.png", UriKind.Relative)));
            voiceVisuals.Add(SpeechRecognizer.Verbs.KinectDownSmall, new BitmapImage(new Uri("/Moto;component/images/voice/angleslightlydown.png", UriKind.Relative)));

        }

        //Skeleton frame code (run on every frame)
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
                            MainWindow.playerAdded(aSkeleton);
                            if (MainWindow.activeSkeletons.Count == 1)
                            {
                                playerVisibleChange(true);
                            }
                        }
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
                        MainWindow.playerRemoved(activeList[i]);
                    }

                    if (MainWindow.activeSkeletons.Count <= 0)
                    {
                        playerVisibleChange(false);
                    }

                    activeList = null;
                }

                skeletonList = null;

                if (MainWindow.activeSkeletons.Count > 0)
                {
                    handMovements.listenForGestures(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton);
                }

                //screenVisibility();
            }
        }

        //Gesture events
        void handMovements_LeftGesture(object sender, handMovements.GestureEventArgs e)
        {
            Storyboard sb = this.FindResource("selectInstrument") as Storyboard;
            switch (e.Trigger)
            {
                case handMovements.UserDecisions.Triggered:
                    selectedMode = modeSelected.Instrument;
                    modeDecision = new DispatcherTimer();
                    modeDecision.Interval = TimeSpan.FromSeconds(3);
                    modeDecision.Start();
                    modeDecision.Tick += new EventHandler(modeDecisionI_Tick);
                    sb.Begin();
                    break;
                case handMovements.UserDecisions.NotTriggered:
                    if (selectedMode == modeSelected.Instrument)
                    {
                        selectedMode = modeSelected.None;
                    }
                    sb.Stop();
                    if (modeDecision != null)
                    {
                        modeDecision.Stop();
                        modeDecision.Tick -= new EventHandler(modeDecisionI_Tick);
                        modeDecision = null;
                    }
                    break;
            }
        }

        void handMovements_RightGesture(object sender, handMovements.GestureEventArgs e)
        {
            Storyboard sb = this.FindResource("selectWallOfSound") as Storyboard;
            switch (e.Trigger)
            {
                case handMovements.UserDecisions.Triggered:
                    selectedMode = modeSelected.WallOfSound;
                    modeDecision = new DispatcherTimer();
                    modeDecision.Interval = TimeSpan.FromSeconds(3);
                    modeDecision.Start();
                    modeDecision.Tick += new EventHandler(modeDecisionWOS_Tick);
                    sb.Begin();
                    break;
                case handMovements.UserDecisions.NotTriggered:
                    if (selectedMode == modeSelected.WallOfSound)
                    {
                        selectedMode = modeSelected.None;
                    }
                    sb.Stop();
                    if (modeDecision != null)
                    {
                        modeDecision.Stop();
                        modeDecision.Tick -= new EventHandler(modeDecisionWOS_Tick);
                        modeDecision = null;
                    }
                    break;
            }
        }

        void handMovements_KinectGuideGesture(object sender, handMovements.GestureEventArgs e)
        {
            Console.WriteLine("Kinect guide: " + e.Trigger);
        }

        void modeDecisionI_Tick(object sender, EventArgs e)
        {
            if (selectedMode == modeSelected.Instrument)
            {
                if (modeDecision != null)
                {
                    modeDecision.Stop();
                    modeDecision.Tick -= new EventHandler(modeDecisionI_Tick);
                    modeDecision = null;

                    loadInstrument();
                }
            }
        }

        void modeDecisionWOS_Tick(object sender, EventArgs e)
        {
            if (selectedMode == modeSelected.WallOfSound)
            {
                if (modeDecision != null)
                {
                    modeDecision.Stop();
                    modeDecision.Tick -= new EventHandler(modeDecisionWOS_Tick);
                    modeDecision = null;

                    loadWallOfSound();
                }
            }
        }

        //Voice navigation code
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
                case SpeechRecognizer.Verbs.Close:
                case SpeechRecognizer.Verbs.Instrument:
                case SpeechRecognizer.Verbs.WallOfSound:
                case SpeechRecognizer.Verbs.KinectUp:
                case SpeechRecognizer.Verbs.KinectUpSmall:
                case SpeechRecognizer.Verbs.KinectDown:
                case SpeechRecognizer.Verbs.KinectDownSmall:
                    voicePromptVisual(true);
                    break;
            }

            voiceConfirmTime = new DispatcherTimer();
            voiceConfirmTime.Interval = TimeSpan.FromMilliseconds(5000);
            voiceConfirmTime.Tick += new EventHandler(voiceConfirmTime_Tick);
            voiceConfirmTime.Start();
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

        private void ListeningChanged(object sender, SpeechRecognizer.ListeningChangedEventArgs e)
        {
            if (e.Paused)
            {
                MainWindow.mySpeechRecognizer.stopListening(MainCanvas);
                MainWindow.animateSlide(imgMotoLogo, false);
            }
            else
            {
                MainWindow.mySpeechRecognizer.startListening(MainCanvas);
                MainWindow.animateSlide(imgMotoLogo, true);
            }
        }

        void voiceGoDoThis(SpeechRecognizer.SaidSomethingEventArgs voiceCommand)
        {
            switch (voiceCommand.Verb)
            {
                case SpeechRecognizer.Verbs.Close:
                    Application.Current.Shutdown();
                    break;
                case SpeechRecognizer.Verbs.Instrument:
                    loadInstrument();
                    break;
                case SpeechRecognizer.Verbs.WallOfSound:
                    loadWallOfSound();
                    break;
                case SpeechRecognizer.Verbs.KinectUp:
                    MainWindow.adjustKinectAngle(8);
                    break;
                case SpeechRecognizer.Verbs.KinectUpSmall:
                    MainWindow.adjustKinectAngle(4);
                    break;
                case SpeechRecognizer.Verbs.KinectDown:
                    MainWindow.adjustKinectAngle(-8);
                    break;
                case SpeechRecognizer.Verbs.KinectDownSmall:
                    MainWindow.adjustKinectAngle(-4);
                    break;
            }
        }

        private void unlistenVoice()
        {
            MainWindow.mySpeechRecognizer.toggleListening(false);
            MainWindow.mySpeechRecognizer.SaidSomething -= this.RecognizerSaidSomething;
            MainWindow.mySpeechRecognizer.ListeningChanged -= this.ListeningChanged;
        }

        //Any visible player code
        private void playerVisibleChange(bool visible)
        {
            if (visible)
            {
                //There wasn't someone visible, now there is
                Storyboard sb = this.FindResource("stepUpToPlay") as Storyboard;
                sb.AutoReverse = true;
                sb.Begin();
                sb.Seek(new TimeSpan(0, 0, 0), TimeSeekOrigin.Duration);
            } else {
                //There is now nobody visible
                Storyboard sb = this.FindResource("stepUpToPlay") as Storyboard;
                sb.AutoReverse = false;
                sb.Begin();
            }
        }

        private void screenVisibility()
        {
            if (MainWindow.activeSkeletons.Count > 0)
            {
                imgStepInToPlay.Visibility = Visibility.Hidden;

                //drumSlider.Visibility = Visibility.Visible;
                lblHoldOutLeftHand.Visibility = Visibility.Visible;
                lblPlayInstrument.Visibility = Visibility.Visible;

                lblHoldOutRightHand.Visibility = Visibility.Visible;
                lblPlayWallOfSound.Visibility = Visibility.Visible;
            }
            else
            {
                imgStepInToPlay.Visibility = Visibility.Visible;

                //imgInstrumentLoader.Visibility = Visibility.Hidden;
                lblHoldOutLeftHand.Visibility = Visibility.Hidden;
                lblPlayInstrument.Visibility = Visibility.Hidden;

                //imgWallOfSoundLoader.Visibility = Visibility.Hidden;
                lblHoldOutRightHand.Visibility = Visibility.Hidden;
                lblPlayWallOfSound.Visibility = Visibility.Hidden;
            }
        }

        //Primary player identification
        private void alignPrimaryGlow(MainWindow.Player player)
        {
            if (MainWindow.sensor.IsRunning)
            {
                ColorImagePoint leftPoint = MainWindow.sensor.MapSkeletonPointToColor(player.skeleton.Joints[JointType.HandLeft].Position, ColorImageFormat.RgbResolution640x480Fps30);
                ColorImagePoint rightPoint = MainWindow.sensor.MapSkeletonPointToColor(player.skeleton.Joints[JointType.HandRight].Position, ColorImageFormat.RgbResolution640x480Fps30);

                Canvas.SetLeft(imgPrimaryGlowLeft, leftPoint.X - (imgPrimaryGlowLeft.Width / 2));
                Canvas.SetTop(imgPrimaryGlowLeft, leftPoint.Y - (imgPrimaryGlowLeft.Height / 2));

                Canvas.SetLeft(imgPrimaryGlowRight, rightPoint.X - (imgPrimaryGlowRight.Width / 2));
                Canvas.SetTop(imgPrimaryGlowRight, rightPoint.Y - (imgPrimaryGlowRight.Height / 2));
            }
        }

        private void highlightPrimarySkeleton(MainWindow.Player player)
        {
            Storyboard sb = this.FindResource("primaryGlow") as Storyboard;
            sb.Begin();
        }

        //Load appropriate pages
        void loadInstrument()
        {
            if (voiceConfirmTime != null)
            {
                removeConfirmationTime();
            }

            removeListeners();

            this.NavigationService.Navigate(new instrument());
        }

        void loadWallOfSound()
        {
            if (voiceConfirmTime != null)
            {
                removeConfirmationTime();
            }

            removeListeners();

            this.NavigationService.Navigate(new wallOfSound());
        }

        private void removeListeners()
        {
            unlistenVoice();

            MainWindow.sensor.AllFramesReady -= new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);
            handMovements.KinectGuideGesture -= new EventHandler<handMovements.GestureEventArgs>(handMovements_KinectGuideGesture);
            handMovements.LeftGesture -= new EventHandler<handMovements.GestureEventArgs>(handMovements_LeftGesture);
            handMovements.RightGesture -= new EventHandler<handMovements.GestureEventArgs>(handMovements_RightGesture);
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
                case System.Windows.Input.Key.I:
                    //Load Instruments
                    loadInstrument();
                    break;
                case System.Windows.Input.Key.W:
                    //Load the Wall of Sound
                    loadWallOfSound();
                    break;
                case System.Windows.Input.Key.Escape:
                    //Close the application
                    Application.Current.Shutdown();
                    break;
            }
        }
    }
}
