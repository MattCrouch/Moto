﻿using System;
using System.Threading;
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
using Microsoft.Speech.Recognition;

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

            MainWindow.SFXStartup.Play();

            MainWindow.animateSlide(imgMotoLogo);

            setupVoiceVisuals();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            setupStartScreen();
            setupVoice();
            voicePromptVisual(false);
            startScreenUserImage.Source = MainWindow.colorImageBitmap;

            KinectSensor.KinectSensors.StatusChanged += new EventHandler<StatusChangedEventArgs>(KinectSensors_StatusChanged);

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
        Image helpVisual;

        //Kinect error imagery
        Image kinectError;
        Image initialisingSpinner;

        private void setupStartScreen()
        {
            MainWindow.sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);
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

                //Listen for gestures for everyone in the scene
                foreach (var player in MainWindow.activeSkeletons)
                {
                    handMovements.listenForGestures(player.Value.skeleton);
                }
            }
        }

        //Gesture events
        void handMovements_LeftGesture(object sender, handMovements.GestureEventArgs e)
        {
            Storyboard sb = this.FindResource("selectInstrument") as Storyboard;
            switch (e.Trigger)
            {
                case handMovements.UserDecisions.Triggered:
                    if (modeDecision == null)
                    {
                        selectedMode = modeSelected.Instrument;
                        modeDecision = new DispatcherTimer();
                        modeDecision.Interval = TimeSpan.FromSeconds(3);
                        modeDecision.Start();
                        modeDecision.Tick += new EventHandler(modeDecisionI_Tick);
                        sb.Begin();
                        MainWindow.SFXMenu.Play();
                    }

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
                    imgBandMode.Visibility = Visibility.Visible;
                    imgLeftHand.Visibility = Visibility.Visible;
                    imgWallOfSound.Visibility = Visibility.Visible;
                    imgRightHand.Visibility = Visibility.Visible;
                    break;
            }
        }

        void handMovements_RightGesture(object sender, handMovements.GestureEventArgs e)
        {
            Storyboard sb = this.FindResource("selectWallOfSound") as Storyboard;
            switch (e.Trigger)
            {
                case handMovements.UserDecisions.Triggered:
                    if (modeDecision == null)
                    {
                        selectedMode = modeSelected.WallOfSound;
                        modeDecision = new DispatcherTimer();
                        modeDecision.Interval = TimeSpan.FromSeconds(3);
                        modeDecision.Start();
                        modeDecision.Tick += new EventHandler(modeDecisionWOS_Tick);
                        sb.Begin();
                        MainWindow.SFXMenu.Play();
                    }
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
                    imgBandMode.Visibility = Visibility.Visible;
                    imgLeftHand.Visibility = Visibility.Visible;
                    imgWallOfSound.Visibility = Visibility.Visible;
                    imgRightHand.Visibility = Visibility.Visible;
                    break;
            }
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

                    selectedMode = modeSelected.None;

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

                    selectedMode = modeSelected.None;

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
                    MainWindow.mySpeechRecognizer.switchGrammar(new Choices[] { MainWindow.mySpeechRecognizer.booleanChoices }, false, false);

                    voicePromptVisual(true);

                    voiceConfirmTime = new DispatcherTimer();
                    voiceConfirmTime.Interval = TimeSpan.FromMilliseconds(5000);
                    voiceConfirmTime.Tick += new EventHandler(voiceConfirmTime_Tick);
                    voiceConfirmTime.Start();
                    break;
                case SpeechRecognizer.Verbs.VoiceHelp:
                    if (MainWindow.mySpeechRecognizer.paused == true)
                    {
                        MainWindow.mySpeechRecognizer.toggleListening(true);
                    }
                    MainWindow.mySpeechRecognizer.resetSpeechTimeout(10);
                    showHelpVisual();
                    break;
            }
        }

        private void showHelpVisual()
        {
            if (helpVisual == null)
            {
                helpVisual = new Image();
                helpVisual.Source = new BitmapImage(new Uri("/Moto;component/images/tutorials/voice-help-start.png", UriKind.Relative));
                if (!MainCanvas.Children.Contains(helpVisual))
                {
                    MainCanvas.Children.Add(helpVisual);
                }
                helpVisual.Width = MainCanvas.ActualWidth;
                imgDimmer.Visibility = System.Windows.Visibility.Visible;
                MainWindow.animateFade(imgDimmer, 0, 0.75, 0.5);
                MainWindow.animateSlide(helpVisual);
            }
        }

        private void hideHelpVisual()
        {
            if (helpVisual != null)
            {
                MainWindow.animateFade(imgDimmer, 0.75, 0, 0.5);
                MainWindow.animateSlide(helpVisual, true);
                helpVisual = null;
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
            else
            {
                if (MainWindow.mySpeechRecognizer.paused)
                {
                    MainWindow.mySpeechRecognizer.switchGrammar(new Choices[] { MainWindow.mySpeechRecognizer.startScreenChoices, MainWindow.mySpeechRecognizer.kinectMotorChoices }, true, true);
                }
                else
                {
                    MainWindow.mySpeechRecognizer.switchGrammar(new Choices[] { MainWindow.mySpeechRecognizer.startScreenChoices, MainWindow.mySpeechRecognizer.kinectMotorChoices }, false, false);
                }
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
                MainWindow.animateSlide(imgMotoLogo, true);
            }
            else
            {
                showingConfirmDialog = false;
                MainWindow.animateSlide(confirmationVisual, true);
                MainWindow.animateSlide(imgMotoLogo);
            }
        }

        private void ListeningChanged(object sender, SpeechRecognizer.ListeningChangedEventArgs e)
        {
            if (e.Paused)
            {
                MainWindow.mySpeechRecognizer.stopListening(MainCanvas);
                MainWindow.mySpeechRecognizer.switchGrammar(new Choices[] { MainWindow.mySpeechRecognizer.startScreenChoices, MainWindow.mySpeechRecognizer.kinectMotorChoices }, true, true);
                MainWindow.SFXNotListening.Play();
                hideHelpVisual();
            }
            else
            {
                MainWindow.mySpeechRecognizer.startListening(MainCanvas);
                MainWindow.mySpeechRecognizer.switchGrammar(new Choices[] { MainWindow.mySpeechRecognizer.startScreenChoices, MainWindow.mySpeechRecognizer.kinectMotorChoices, MainWindow.mySpeechRecognizer.stopListeningChoices }, false, false);
                MainWindow.SFXListening.Play();
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
                    MainWindow.mySpeechRecognizer.switchGrammar(new Choices[] { MainWindow.mySpeechRecognizer.startScreenChoices, MainWindow.mySpeechRecognizer.kinectMotorChoices }, true, true);
                    break;
                case SpeechRecognizer.Verbs.KinectUpSmall:
                    MainWindow.adjustKinectAngle(4);
                    MainWindow.mySpeechRecognizer.switchGrammar(new Choices[] { MainWindow.mySpeechRecognizer.startScreenChoices, MainWindow.mySpeechRecognizer.kinectMotorChoices }, true, true);
                    break;
                case SpeechRecognizer.Verbs.KinectDown:
                    MainWindow.adjustKinectAngle(-8);
                    MainWindow.mySpeechRecognizer.switchGrammar(new Choices[] { MainWindow.mySpeechRecognizer.startScreenChoices, MainWindow.mySpeechRecognizer.kinectMotorChoices }, true, true);
                    break;
                case SpeechRecognizer.Verbs.KinectDownSmall:
                    MainWindow.adjustKinectAngle(-4);
                    MainWindow.mySpeechRecognizer.switchGrammar(new Choices[] { MainWindow.mySpeechRecognizer.startScreenChoices, MainWindow.mySpeechRecognizer.kinectMotorChoices }, true, true);
                    break;
            }

            MainWindow.mySpeechRecognizer.toggleListening(false);
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
                MainWindow.animateSlide(imgStepInToPlay, true, true, 10, 0.5);

                imgBandMode.Visibility = System.Windows.Visibility.Visible;
                imgLeftHand.Visibility = System.Windows.Visibility.Visible;
                imgWallOfSound.Visibility = System.Windows.Visibility.Visible;
                imgRightHand.Visibility = System.Windows.Visibility.Visible;

                MainWindow.animateSlide(imgBandMode, false, true, 10, 0.5);
                MainWindow.animateSlide(imgLeftHand, false, true, 10, 0.5);
                MainWindow.animateSlide(imgWallOfSound, false, true, 10, 0.5);
                MainWindow.animateSlide(imgRightHand, false, true, 10, 0.5);
            }
            else
            {
                //There is now nobody visible
                MainWindow.animateSlide(imgStepInToPlay, false, true, 10, 0.5);
                MainWindow.animateSlide(imgBandMode, true, true, 10, 0.5);
                MainWindow.animateSlide(imgLeftHand, true, true, 10, 0.5);
                MainWindow.animateSlide(imgWallOfSound, true, true, 10, 0.5);
                MainWindow.animateSlide(imgRightHand, true, true, 10, 0.5);
            }
        }

        private void screenVisibility()
        {
            if (MainWindow.activeSkeletons.Count > 0)
            {
                imgStepInToPlay.Visibility = Visibility.Hidden;

                lblHoldOutLeftHand.Visibility = Visibility.Visible;
                lblPlayInstrument.Visibility = Visibility.Visible;

                lblHoldOutRightHand.Visibility = Visibility.Visible;
                lblPlayWallOfSound.Visibility = Visibility.Visible;
            }
            else
            {
                imgStepInToPlay.Visibility = Visibility.Visible;

                lblHoldOutLeftHand.Visibility = Visibility.Hidden;
                lblPlayInstrument.Visibility = Visibility.Hidden;

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
            handMovements.LeftGesture -= new EventHandler<handMovements.GestureEventArgs>(handMovements_LeftGesture);
            handMovements.RightGesture -= new EventHandler<handMovements.GestureEventArgs>(handMovements_RightGesture);

            KinectSensor.KinectSensors.StatusChanged -= new EventHandler<StatusChangedEventArgs>(KinectSensors_StatusChanged);
        }

        void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (kinectError != null)
            {
                MainWindow.animateSlide(kinectError, true, true);
            }

            if (initialisingSpinner != null)
            {
                MainWindow.animateSlide(initialisingSpinner, true);
                initialisingSpinner = null;
            }

            if (e.Status == KinectStatus.Connected)
            {
                MainWindow.animateFade(imgDimmer, 0.5, 0);
                MainWindow.animateSlide(kinectError, true);
                kinectError = null;

                MainWindow.setupKinect();
                MainWindow.setupVoice();
                setupVoice();
                startScreenUserImage.Source = MainWindow.colorImageBitmap;
            }
            else
            {
                MainWindow.animateFade(imgDimmer, 0, 0.5);

                kinectError = MainWindow.generateError(e.Status);
                MainCanvas.Children.Add(kinectError);
                MainWindow.animateSlide(kinectError);

                if (e.Status == KinectStatus.Initializing)
                {
                    //Spinny, turny progress animation
                    initialisingSpinner = new Image();
                    initialisingSpinner.Source = new BitmapImage(new Uri(
        "/Moto;component/images/loading.png", UriKind.Relative));
                    MainCanvas.Children.Add(initialisingSpinner);
                    initialisingSpinner.Width = 150;
                    initialisingSpinner.Height = 150;
                    MainWindow.animateSlide(initialisingSpinner);
                    MainWindow.animateSpin(initialisingSpinner);
                    Canvas.SetTop(initialisingSpinner, 230);
                    Canvas.SetLeft(initialisingSpinner, (MainCanvas.ActualWidth / 2) - (initialisingSpinner.Width / 2));
                }
                else if (e.Status == KinectStatus.Disconnected)
                {
                    MainWindow.stopKinect(MainWindow.sensor);
                }
            }
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
                case System.Windows.Input.Key.R:
                    //Restart the Application
                    MainWindow.restartMoto();
                    break;
                case System.Windows.Input.Key.T:
                    //Enable tutorials
                    MainWindow.setupTutorials();
                    break;
                case System.Windows.Input.Key.H:
                    //Toggle voice commands
                    if (helpVisual == null)
                    {
                        showHelpVisual();
                    }
                    else
                    {
                        hideHelpVisual();
                    }
                    break;
            }
        }
    }
}
