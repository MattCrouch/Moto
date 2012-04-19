using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Kinect;
using Coding4Fun.Kinect.Wpf;
using Moto.Speech;

namespace Moto
{
    /// <summary>
    /// Interaction logic for wallOfSound.xaml
    /// </summary>
    public partial class wallOfSound : Page
    {
        public wallOfSound()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Listening for when our frames are ready
            MainWindow.sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);


            //Listen for Kinect Guide gesture
            handMovements.KinectGuideGesture += new EventHandler<handMovements.GestureEventArgs>(handMovements_KinectGuideGesture);

            //Create dictionary definitions for all the Media Players available
            generateMediaPlayers();

            setupVoice();

            setupKinectGuide();

            userImage.Source = MainWindow.colorImageBitmap;
            userDepth.Source = MainWindow.depthImageBitmap;

            processExistingSkeletons(MainWindow.activeSkeletons);

            this.FocusVisualStyle = null;
            this.Focus();
        }

        //Image processing variables
        short[] DepthPixelData = new short[MainWindow.sensor.DepthStream.FramePixelDataLength];

        //Wall of Sound areas
        Dictionary<int, Dictionary<int, MainWindow.HitBox>> hitArea = new Dictionary<int, Dictionary<int, MainWindow.HitBox>>();
        Dictionary<int, Dictionary<JointType, Dictionary<int, bool>>> insideArea = new Dictionary<int, Dictionary<JointType, Dictionary<int, bool>>>();

        //Wall audio
        string[] wallAudio = new string[9];

        //Audio dictionarys
        Dictionary<int, MediaPlayer> mpDictionary = new Dictionary<int, MediaPlayer>();
        int mpCounter = 0;

        //Kinect Guide variables
        DispatcherTimer kinectGuideTimer;
        DispatcherTimer menuMovementTimer;
        handMovements.scrollDirection menuScrollDirection;
        menuOptions[] kinectGuideMenu = new menuOptions[Enum.GetValues(typeof(menuOptions)).Length];
        int menuPosition;

        enum menuOptions
        {
            //All menu items
            GoBack,
            CustomWall,
            RecordNewWall,
            Technologic,
            Drum,
        }

        //Player's current focus
        playerFocus currentFocus = playerFocus.None;

        enum playerFocus
        {
            None = 0,
            KinectGuide,
            Picture
        }

        //Housekeeping
        private void processExistingSkeletons(Dictionary<int, MainWindow.Player> activeSkeletons)
        {
            foreach (var player in activeSkeletons)
            {
                newPlayerWall(player.Value);
                Console.WriteLine(player.Value.skeleton.TrackingId);
            }
        }

        private void setupWall(MainWindow.Player player)
        {
            if (!hitArea.ContainsKey(player.skeleton.TrackingId))
            {
                //Blank dictionary of the drums of one person
                Dictionary<int, MainWindow.HitBox> blankDefinitions = new Dictionary<int, MainWindow.HitBox>();
                blankDefinitions.Add(0, new MainWindow.HitBox());
                blankDefinitions.Add(1, new MainWindow.HitBox());
                blankDefinitions.Add(2, new MainWindow.HitBox());
                blankDefinitions.Add(3, new MainWindow.HitBox());
                blankDefinitions.Add(4, new MainWindow.HitBox());
                blankDefinitions.Add(5, new MainWindow.HitBox());
                blankDefinitions.Add(6, new MainWindow.HitBox());
                blankDefinitions.Add(7, new MainWindow.HitBox());

                hitArea.Add(player.skeleton.TrackingId, blankDefinitions);

                switch (player.mode)
                {
                    case MainWindow.PlayerMode.Technologic:
                        technologicAudio();
                        break;
                    case MainWindow.PlayerMode.Drum:
                        drumsetAudio();
                        break;
                }
            }

            //Make sure the hands aren't in the drums areas in the first place
            insideArea.Add(player.skeleton.TrackingId, createPlayerDictionary());
        }

        private void setupVoice()
        {
            MainWindow.mySpeechRecognizer.SaidSomething += this.RecognizerSaidSomething;
            MainWindow.mySpeechRecognizer.ListeningChanged += this.ListeningChanged;
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

        private void technologicAudio()
        {
            wallAudio[0] = "audio/wall/technologic/buyit.wav";
            wallAudio[1] = "audio/wall/technologic/useit.wav";
            wallAudio[2] = "audio/wall/technologic/breakit.wav";
            wallAudio[3] = "audio/wall/technologic/fixit.wav";
            wallAudio[4] = "audio/wall/technologic/trashit.wav";
            wallAudio[5] = "audio/wall/technologic/changeit.wav";
            wallAudio[6] = "audio/wall/technologic/mail.wav";
            wallAudio[7] = "audio/wall/technologic/upgradeit.wav";
        }

        private void drumsetAudio()
        {
            wallAudio[0] = "audio/drums/drum0.wav";
            wallAudio[1] = "audio/drums/drum1.wav";
            wallAudio[2] = "audio/drums/drum2.wav";
        }

        internal void defineHitAreas(MainWindow.Player player)
        {
            if (player.skeleton != null)
            {
                double boxSize = 0.2; //Size of drum edges (in metres)

                //First box
                hitArea[player.skeleton.TrackingId][0].X1 = player.skeleton.Joints[JointType.HipCenter].Position.X - 0.4532758;
                hitArea[player.skeleton.TrackingId][0].X2 = hitArea[player.skeleton.TrackingId][0].X1 + boxSize;
                hitArea[player.skeleton.TrackingId][0].Y1 = player.skeleton.Joints[JointType.HipCenter].Position.Y + 0.36289116;
                hitArea[player.skeleton.TrackingId][0].Y2 = hitArea[player.skeleton.TrackingId][0].Y1 + boxSize;
                hitArea[player.skeleton.TrackingId][0].Z1 = player.skeleton.Joints[JointType.HipCenter].Position.Z - 0.5014166;
                hitArea[player.skeleton.TrackingId][0].Z2 = hitArea[player.skeleton.TrackingId][0].Z1 + boxSize;

                //Second box
                hitArea[player.skeleton.TrackingId][1].X1 = player.skeleton.Joints[JointType.HipCenter].Position.X - 0.2032758;
                hitArea[player.skeleton.TrackingId][1].X2 = hitArea[player.skeleton.TrackingId][1].X1 + boxSize;
                hitArea[player.skeleton.TrackingId][1].Y1 = player.skeleton.Joints[JointType.HipCenter].Position.Y + 0.36289116;
                hitArea[player.skeleton.TrackingId][1].Y2 = hitArea[player.skeleton.TrackingId][1].Y1 + boxSize;
                hitArea[player.skeleton.TrackingId][1].Z1 = player.skeleton.Joints[JointType.HipCenter].Position.Z - 0.5014166;
                hitArea[player.skeleton.TrackingId][1].Z2 = hitArea[player.skeleton.TrackingId][1].Z1 + boxSize;

                //Third box
                hitArea[player.skeleton.TrackingId][2].X1 = player.skeleton.Joints[JointType.HipCenter].Position.X - 0.0532758;
                hitArea[player.skeleton.TrackingId][2].X2 = hitArea[player.skeleton.TrackingId][2].X1 + boxSize;
                hitArea[player.skeleton.TrackingId][2].Y1 = player.skeleton.Joints[JointType.HipCenter].Position.Y + 0.36289116;
                hitArea[player.skeleton.TrackingId][2].Y2 = hitArea[player.skeleton.TrackingId][2].Y1 + boxSize;
                hitArea[player.skeleton.TrackingId][2].Z1 = player.skeleton.Joints[JointType.HipCenter].Position.Z - 0.4014166;
                hitArea[player.skeleton.TrackingId][2].Z2 = hitArea[player.skeleton.TrackingId][2].Z1 + boxSize;

                //Fourth box
                hitArea[player.skeleton.TrackingId][3].X1 = player.skeleton.Joints[JointType.HipCenter].Position.X + 0.3032758;
                hitArea[player.skeleton.TrackingId][3].X2 = hitArea[player.skeleton.TrackingId][3].X1 + boxSize;
                hitArea[player.skeleton.TrackingId][3].Y1 = player.skeleton.Joints[JointType.HipCenter].Position.Y + 0.36289116;
                hitArea[player.skeleton.TrackingId][3].Y2 = hitArea[player.skeleton.TrackingId][3].Y1 + boxSize;
                hitArea[player.skeleton.TrackingId][3].Z1 = player.skeleton.Joints[JointType.HipCenter].Position.Z - 0.5014166;
                hitArea[player.skeleton.TrackingId][3].Z2 = hitArea[player.skeleton.TrackingId][3].Z1 + boxSize;

                //Fifth box
                hitArea[player.skeleton.TrackingId][4].X1 = player.skeleton.Joints[JointType.HipCenter].Position.X - 0.4532758;
                hitArea[player.skeleton.TrackingId][4].X2 = hitArea[player.skeleton.TrackingId][4].X1 + boxSize;
                hitArea[player.skeleton.TrackingId][4].Y1 = player.skeleton.Joints[JointType.HipCenter].Position.Y + 0.11289116;
                hitArea[player.skeleton.TrackingId][4].Y2 = hitArea[player.skeleton.TrackingId][4].Y1 + boxSize;
                hitArea[player.skeleton.TrackingId][4].Z1 = player.skeleton.Joints[JointType.HipCenter].Position.Z - 0.5014166;
                hitArea[player.skeleton.TrackingId][4].Z2 = hitArea[player.skeleton.TrackingId][4].Z1 + boxSize;

                //Sixth box
                hitArea[player.skeleton.TrackingId][5].X1 = player.skeleton.Joints[JointType.HipCenter].Position.X - 0.2032758;
                hitArea[player.skeleton.TrackingId][5].X2 = hitArea[player.skeleton.TrackingId][5].X1 + boxSize;
                hitArea[player.skeleton.TrackingId][5].Y1 = player.skeleton.Joints[JointType.HipCenter].Position.Y + 0.11289116;
                hitArea[player.skeleton.TrackingId][5].Y2 = hitArea[player.skeleton.TrackingId][5].Y1 + boxSize;
                hitArea[player.skeleton.TrackingId][5].Z1 = player.skeleton.Joints[JointType.HipCenter].Position.Z - 0.5014166;
                hitArea[player.skeleton.TrackingId][5].Z2 = hitArea[player.skeleton.TrackingId][5].Z1 + boxSize;

                //Seventh box
                hitArea[player.skeleton.TrackingId][6].X1 = player.skeleton.Joints[JointType.HipCenter].Position.X - 0.0532758;
                hitArea[player.skeleton.TrackingId][6].X2 = hitArea[player.skeleton.TrackingId][6].X1 + boxSize;
                hitArea[player.skeleton.TrackingId][6].Y1 = player.skeleton.Joints[JointType.HipCenter].Position.Y + 0.11289116;
                hitArea[player.skeleton.TrackingId][6].Y2 = hitArea[player.skeleton.TrackingId][6].Y1 + boxSize;
                hitArea[player.skeleton.TrackingId][6].Z1 = player.skeleton.Joints[JointType.HipCenter].Position.Z - 0.5014166;
                hitArea[player.skeleton.TrackingId][6].Z2 = hitArea[player.skeleton.TrackingId][6].Z1 + boxSize;

                //Eighth box
                hitArea[player.skeleton.TrackingId][7].X1 = player.skeleton.Joints[JointType.HipCenter].Position.X + 0.3032758;
                hitArea[player.skeleton.TrackingId][7].X2 = hitArea[player.skeleton.TrackingId][7].X1 + boxSize;
                hitArea[player.skeleton.TrackingId][7].Y1 = player.skeleton.Joints[JointType.HipCenter].Position.Y + 0.11289116;
                hitArea[player.skeleton.TrackingId][7].Y2 = hitArea[player.skeleton.TrackingId][7].Y1 + boxSize;
                hitArea[player.skeleton.TrackingId][7].Z1 = player.skeleton.Joints[JointType.HipCenter].Position.Z - 0.5014166;
                hitArea[player.skeleton.TrackingId][7].Z2 = hitArea[player.skeleton.TrackingId][7].Z1 + boxSize;
            }
        }

        Dictionary<JointType, Dictionary<int, bool>> createPlayerDictionary()
        {
            Dictionary<JointType, Dictionary<int, bool>> dictionary = new Dictionary<JointType, Dictionary<int, bool>>();

            dictionary.Add(JointType.HandLeft, new Dictionary<int, bool>());
            dictionary.Add(JointType.HandRight, new Dictionary<int, bool>());

            dictionary[JointType.HandLeft].Add(0, false);
            dictionary[JointType.HandLeft].Add(1, false);

            dictionary[JointType.HandRight].Add(0, false);
            dictionary[JointType.HandRight].Add(1, false);

            return dictionary;
        }

        private void generateMediaPlayers()
        {
            mpDictionary.Add(0, new MediaPlayer());
            mpDictionary.Add(1, new MediaPlayer());
            mpDictionary.Add(2, new MediaPlayer());
            mpDictionary.Add(3, new MediaPlayer());
        }

        //Skeleton data processing (ran every frame)
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

            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                //DEPTH IMAGE CODE
                if (depthFrame == null)
                {
                    return;
                }

                depthFrame.CopyPixelDataTo(DepthPixelData);

                CreatePlayerDepthImage(depthFrame, DepthPixelData);
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
                                //What to do when a player gets added
                                newPlayerWall(MainWindow.activeSkeletons[aSkeleton.TrackingId]);
                            }
                        }

                        //The player we're referencing at this point in the loop
                        MainWindow.Player player = MainWindow.activeSkeletons[aSkeleton.TrackingId];

                        //Player-specific code
                        handMovements.trackJointProgression(player.skeleton, player.skeleton.Joints[JointType.HandLeft]);
                        handMovements.trackJointProgression(player.skeleton, player.skeleton.Joints[JointType.HandRight]);
                        wallUpdate(player);
                    }
                }

                if (MainWindow.activeSkeletons.Count > 0)
                {
                    int tempKey = MainWindow.primarySkeletonKey;
                    MainWindow.primarySkeletonKey = MainWindow.selectPrimarySkeleton(MainWindow.activeSkeletons);

                    alignPrimaryGlow(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey]);

                    handMovements.listenForGestures(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton);

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
                        removePlayerWall(MainWindow.activeSkeletons[activeList[i]]);
                        MainWindow.playerRemoved(activeList[i]);
                    }

                    activeList = null;
                }

                skeletonList = null;

                if (MainWindow.activeSkeletons.Count > 0)
                {
                    //Listen for gestures from the primary skeleton
                    //handMovements.listenForGestures(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton);
                }
            }
        }

        internal void checkBoxHit(Skeleton skeleton, JointType joint)
        {
            //checkDrumHit code
            //MessageBox.Show(Convert.ToString(hitAreaStart[0][1]));
            if (skeleton != null)
            {

                double posX = skeleton.Joints[joint].Position.X;
                double posY = skeleton.Joints[joint].Position.Y;
                double posZ = skeleton.Joints[joint].Position.Z;

                for (int i = 0; i <= hitArea[skeleton.TrackingId].Count-1; i++)
                {
                    if (hitArea[skeleton.TrackingId][i].X1 < posX && hitArea[skeleton.TrackingId][i].X2 > posX && hitArea[skeleton.TrackingId][i].Y1 < posY && hitArea[skeleton.TrackingId][i].Y2 > posY && hitArea[skeleton.TrackingId][i].Z1 < posZ && hitArea[skeleton.TrackingId][i].Z2 > posZ)
                    {
                        if (!insideArea[skeleton.TrackingId][joint][i])
                        {
                            playWallSound(i, skeleton, joint);
                            Console.WriteLine("HIT! " + i);
                            insideArea[skeleton.TrackingId][joint][i] = true;
                        }
                    }
                    else
                    {
                        insideArea[skeleton.TrackingId][joint][i] = false;
                    }
                }
            }
        }

        private void wallUpdate(MainWindow.Player player)
        {
            //What we need to do every skeleton frame with respect to this player's Wall
            defineHitAreas(player);

            checkBoxHit(player.skeleton, JointType.HandLeft);
            checkBoxHit(player.skeleton, JointType.HandRight);

            setWallPosition(player);
        }

        private void setWallPosition(MainWindow.Player player)
        {
            FrameworkElement image = player.instrumentImage;

            ColorImagePoint point = MainWindow.sensor.MapSkeletonPointToColor(player.skeleton.Joints[JointType.Spine].Position, ColorImageFormat.RgbResolution640x480Fps30);

            //Grab the image reference and move it to the correct place
            Canvas.SetLeft(image, point.X - (image.Width / 2));
            Canvas.SetTop(image, point.Y - (image.Height / 2));
        }

        private void playWallSound(int i, Skeleton skeleton, JointType joint)
        {
            if (handMovements.difference != null)
            {
                if (handMovements.difference[skeleton.TrackingId][joint].Z < -0.01)
                {
                    if (wallAudio[i] != null)
                    {
                        mpDictionary[(mpCounter % 4)].Open(new Uri(wallAudio[i], UriKind.Relative));
                        mpDictionary[(mpCounter % 4)].Play();

                        mpCounter++;
                    }
                }
            }
        }

        private void newPlayerWall(MainWindow.Player player)
        {
            //Set up a new player with their Wall
            player.instrument = instrument.instrumentList.WallOfSound;
            player.mode = MainWindow.PlayerMode.Drum;
            
            Image image = new Image();
            image.Source = new BitmapImage(new Uri("images/wall-sample.png", UriKind.Relative));
            image.Height = 250;
            image.Width = 250;

            MainCanvas.Children.Add(image);

            player.instrumentImage = image;

            setupWall(player);
        }

        private void removePlayerWall(MainWindow.Player player)
        {
            //Clean up player wall data
            MainCanvas.Children.Remove(player.instrumentImage);
        }

        //Voice navigation

        private void RecognizerSaidSomething(object sender, SpeechRecognizer.SaidSomethingEventArgs e)
        {
            switch (e.Verb)
            {
                case SpeechRecognizer.Verbs.Capture:
                    //Take a picture
                    takeAPicture();
                    break;
                case SpeechRecognizer.Verbs.ReturnToStart:
                    //Back to StartScreen
                    returnToStart();
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

        //Kinect Guide code
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

            MainWindow.animateSlide(testCanvas, false, false, -150, 0.5);
            MainWindow.animateSlide(imgMenuSelected, false, false, -150, 0.5);
            
            testCanvas.Visibility = System.Windows.Visibility.Visible;
            imgMenuSelected.Visibility = System.Windows.Visibility.Visible;

            menuPosition = 0;

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
            if (handMovements.isLimbStraight(player.Joints[JointType.ShoulderLeft], player.Joints[JointType.ElbowLeft], player.Joints[JointType.HandLeft], 10))
            {
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
        }

        private void kinectGuideManipulation(MainWindow.Player player)
        {
            double angleValue = handMovements.getAngle(player.skeleton.Joints[JointType.ShoulderLeft], player.skeleton.Joints[JointType.HandLeft]);

            handMovements.scrollDirection oldDirection = menuScrollDirection;

            menuScrollDirection = handMovements.sliderMenuValue(player, angleValue);

            if (oldDirection != menuScrollDirection)
            {
                //Console.WriteLine("CHANGE IN DIRECTION: " + menuScrollDirection);
                adjustMenuSpeed(menuScrollDirection);

                if((oldDirection == handMovements.scrollDirection.None && menuScrollDirection == handMovements.scrollDirection.SmallUp) || (oldDirection == handMovements.scrollDirection.SmallUp && menuScrollDirection == handMovements.scrollDirection.LargeUp) || (oldDirection == handMovements.scrollDirection.None && menuScrollDirection == handMovements.scrollDirection.SmallDown) || (oldDirection == handMovements.scrollDirection.SmallDown && menuScrollDirection == handMovements.scrollDirection.LargeDown)) {
                    //If we're increasing in any direction, tick when the speed changes
                    menuTick();
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

            MainWindow.animateSlide(testCanvas, true, false, -150, 0.5);
            MainWindow.animateSlide(imgMenuSelected, true, false, -150, 0.5);
            //MainWindow.animateSlide(imgKinectGuideDimmer, true, false, -150, 0.5);

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

            switch (kinectGuideMenu[menuPosition])
            {
                case menuOptions.Technologic:
                    technologicAudio();
                    break;
                case menuOptions.Drum:
                    drumsetAudio();
                    break;
            }
        }

        #region Image Capture
        private Storyboard flashStoryboard;
        private Rectangle cameraFlash;
        private DispatcherTimer pictureCountdown;
        private DispatcherTimer imgProcessDelay;
        private TextBlock uploadFeedback;
        private Image cameraUpload;

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
            Storyboard sb2 = this.FindResource("photoPrep") as Storyboard;
            sb2.AutoReverse = true;
            sb2.Begin(this, true);
            sb2.Seek(this, new TimeSpan(0, 0, 0), TimeSeekOrigin.Duration);

            Storyboard sb3 = this.FindResource("photoLoading") as Storyboard;
            sb2.Stop();

            currentFocus = playerFocus.Picture;
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

        private void toggleRGB(ColorImageFormat format, int delay = 3000)
        {
            if (MainWindow.sensor.ColorStream.Format != format)
            {
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
        #endregion

        private void returnToStart()
        {
            MainWindow.sensor.AllFramesReady -= new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);

            MainWindow.mySpeechRecognizer.SaidSomething -= this.RecognizerSaidSomething;
            MainWindow.mySpeechRecognizer.ListeningChanged -= this.ListeningChanged;

            this.NavigationService.GoBack();
        }

        //Development code
        private void CreatePlayerDepthImage(DepthImageFrame depthFrame, short[] pixelData)
        {
            int playerIndex;
            int depthBytePerPixel = 4;
            byte[] enhPixelData = new byte[depthFrame.Width * depthFrame.Height * depthBytePerPixel];


            for (int i = 0, j = 0; i < pixelData.Length; i++, j += depthBytePerPixel)
            {
                playerIndex = pixelData[i] & DepthImageFrame.PlayerIndexBitmask;

                if (playerIndex == 0)
                {
                    enhPixelData[j] = 0xFF;
                    enhPixelData[j + 1] = 0xFF;
                    enhPixelData[j + 2] = 0xFF;
                    enhPixelData[j + 3] = 0x00;
                }
                else
                {
                    enhPixelData[j] = 0x00;
                    enhPixelData[j + 1] = 0x00;
                    enhPixelData[j + 2] = 0x00;
                    enhPixelData[j + 3] = 0xFF;
                }
            }


            MainWindow.depthImageBitmap.WritePixels(MainWindow.depthImageBitmapRect, enhPixelData, MainWindow.depthImageStride, 0);
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
                case System.Windows.Input.Key.C:
                    //Take a picture
                    takeAPicture();
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

        private void animateMenu(bool up = true, int count = 1)
        {
           Canvas.SetTop(testCanvas, 60 * menuPosition);
            
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
            testCanvas.RenderTransform = tt;

            CircleEase ease = new CircleEase();
            ease.EasingMode = EasingMode.EaseOut;
            animation.EasingFunction = ease;

            tt.BeginAnimation(TranslateTransform.YProperty, animation);
            Console.WriteLine(menuPosition);
        }
    }
}
