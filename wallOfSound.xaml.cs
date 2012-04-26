using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.IO;
using System.Text;
using System.Threading;
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
        Dictionary<int, string[]> wallAudio = new Dictionary<int, string[]>();

        //Audio dictionarys
        Dictionary<int, playerSound> mpDictionary = new Dictionary<int, playerSound>();
        int mpCounter = 0;

        class playerSound
        {
            public int skeleton;
            public int box;
            public MediaPlayer mediaPlayer;

            public playerSound()
            {
                mediaPlayer = new MediaPlayer();
            }
        }

        //Kinect Guide variables
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
            CustomWall,
            RecordNewWall,
            Technologic,
            Drum,
            EightBit,
        }

        //Player's current focus
        playerFocus currentFocus = playerFocus.None;

        enum playerFocus
        {
            None = 0,
            KinectGuide,
            Picture
        }

        //Wall record variables
        string boxToRecord;
        bool boxRecording = false;
        DispatcherTimer recordingTimer;


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
                blankDefinitions.Add(8, new MainWindow.HitBox());
                blankDefinitions.Add(9, new MainWindow.HitBox());
                blankDefinitions.Add(10, new MainWindow.HitBox());

                hitArea.Add(player.skeleton.TrackingId, blankDefinitions);

                switch (player.mode)
                {
                    case MainWindow.PlayerMode.Technologic:
                        technologicAudio(player);
                        break;
                    case MainWindow.PlayerMode.Drum:
                        drumsetAudio(player);
                        break;
                    case MainWindow.PlayerMode.EightBit:
                        drumsetAudio(player);
                        break;
                }
            }

            //Make sure the hands aren't in the drums areas in the first place
            insideArea.Add(player.skeleton.TrackingId, createPlayerDictionary());
        }

        private void setupPlayerAudio(MainWindow.Player player)
        {
            wallAudio.Add(player.skeleton.TrackingId, new string[11]);
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

        private void customAudio(MainWindow.Player player)
        {
            wallAudio[player.skeleton.TrackingId][0] = "audio/wall/create/0.wav";
            wallAudio[player.skeleton.TrackingId][1] = "audio/wall/create/1.wav";
            wallAudio[player.skeleton.TrackingId][2] = "audio/wall/create/2.wav";
            wallAudio[player.skeleton.TrackingId][3] = "audio/wall/create/3.wav";
            wallAudio[player.skeleton.TrackingId][4] = "audio/wall/create/4.wav";
            wallAudio[player.skeleton.TrackingId][5] = "audio/wall/create/5.wav";
            wallAudio[player.skeleton.TrackingId][6] = "audio/wall/create/6.wav";
            wallAudio[player.skeleton.TrackingId][7] = "audio/wall/create/7.wav";
            wallAudio[player.skeleton.TrackingId][8] = "audio/wall/create/8.wav";
            wallAudio[player.skeleton.TrackingId][9] = "audio/wall/create/9.wav";
            wallAudio[player.skeleton.TrackingId][10] = "audio/wall/create/10.wav";
        }

        private void technologicAudio(MainWindow.Player player)
        {
            wallAudio[player.skeleton.TrackingId][0] = "audio/wall/technologic/buyit.wav";
            wallAudio[player.skeleton.TrackingId][1] = "audio/wall/technologic/useit.wav";
            wallAudio[player.skeleton.TrackingId][2] = "audio/wall/technologic/breakit.wav";
            wallAudio[player.skeleton.TrackingId][3] = "audio/wall/technologic/fixit.wav";
            wallAudio[player.skeleton.TrackingId][4] = "audio/wall/technologic/trashit.wav";
            wallAudio[player.skeleton.TrackingId][5] = "audio/wall/technologic/changeit.wav";
            wallAudio[player.skeleton.TrackingId][6] = "audio/wall/technologic/mail.wav";
            wallAudio[player.skeleton.TrackingId][7] = "audio/wall/technologic/upgradeit.wav";
        }

        private void drumsetAudio(MainWindow.Player player)
        {
            wallAudio[player.skeleton.TrackingId][0] = "audio/drums/drum0.wav";
            wallAudio[player.skeleton.TrackingId][1] = "audio/drums/drum1.wav";
            wallAudio[player.skeleton.TrackingId][2] = "audio/drums/drum2.wav";
        }

        private void eightBitAudio(MainWindow.Player player)
        {
            wallAudio[player.skeleton.TrackingId][0] = "audio/wall/8bit/0.wav";
            wallAudio[player.skeleton.TrackingId][1] = "audio/wall/8bit/1.wav";
            wallAudio[player.skeleton.TrackingId][2] = "audio/wall/8bit/2.wav";
            wallAudio[player.skeleton.TrackingId][3] = "audio/wall/8bit/3.wav";
            wallAudio[player.skeleton.TrackingId][4] = "audio/wall/8bit/4.wav";
            wallAudio[player.skeleton.TrackingId][5] = "audio/wall/8bit/5.wav";
            wallAudio[player.skeleton.TrackingId][6] = "audio/wall/8bit/6.wav";
            wallAudio[player.skeleton.TrackingId][7] = "audio/wall/8bit/7.wav";
            wallAudio[player.skeleton.TrackingId][8] = "audio/wall/8bit/8.wav";
            wallAudio[player.skeleton.TrackingId][9] = "audio/wall/8bit/9.wav";
            wallAudio[player.skeleton.TrackingId][10] = "audio/wall/8bit/10.wav";
        }

        internal void defineHitAreas(MainWindow.Player player)
        {
            if (player.skeleton != null)
            {
                //Front panels
                double panelHeight = 0.2;
                double panelWidth = panelHeight;
                double panelDepth = 0.1;

                definePanel(player, 0, -0.3516321, 0.1761248, -0.4665765, panelHeight, panelWidth, panelDepth);
                definePanel(player, 1, -0.1016321, 0.1761248, -0.4665765, panelHeight, panelWidth, panelDepth);
                definePanel(player, 2, 0.1516321, 0.1761248, -0.4665765, panelHeight, panelWidth, panelDepth);
                definePanel(player, 3, -0.3516321, -0.0861248, -0.4665765, panelHeight, panelWidth, panelDepth);
                definePanel(player, 4, -0.1016321, -0.0861248, -0.4665765, panelHeight, panelWidth, panelDepth);
                definePanel(player, 5, 0.1516321, -0.0861248, -0.4665765, panelHeight, panelWidth, panelDepth);
                definePanel(player, 6, -0.3516321, -0.3361248, -0.4665765, panelHeight, panelWidth, panelDepth);
                definePanel(player, 7, -0.1016321, -0.3361248, -0.4665765, panelHeight, panelWidth, panelDepth);
                definePanel(player, 8, 0.1516321, -0.3361248, -0.4665765, panelHeight, panelWidth, panelDepth);

                //Side panels
                panelHeight = 0.5;
                panelDepth = 1;

                definePanel(player, 9, -0.5973037, -0.0743588, -0.5065765, panelHeight, panelWidth, panelDepth);
                definePanel(player, 10, 0.3473037, -0.0743588, -0.5065765, panelHeight, panelWidth, panelDepth);
            }
        }

        private void definePanel(MainWindow.Player player, int panel, double X, double Y, double Z, double panelHeight = 0.2, double panelWidth = 0.2, double panelDepth = 0.2)
        {
            hitArea[player.skeleton.TrackingId][panel].X1 = player.skeleton.Joints[JointType.HipCenter].Position.X + X;
            hitArea[player.skeleton.TrackingId][panel].X2 = hitArea[player.skeleton.TrackingId][panel].X1 + panelWidth;
            hitArea[player.skeleton.TrackingId][panel].Y1 = player.skeleton.Joints[JointType.HipCenter].Position.Y + Y;
            hitArea[player.skeleton.TrackingId][panel].Y2 = hitArea[player.skeleton.TrackingId][panel].Y1 + panelHeight;
            hitArea[player.skeleton.TrackingId][panel].Z1 = player.skeleton.Joints[JointType.HipCenter].Position.Z + Z;
            hitArea[player.skeleton.TrackingId][panel].Z2 = hitArea[player.skeleton.TrackingId][panel].Z1 + panelDepth;
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
            mpDictionary.Add(0, null);
            mpDictionary.Add(1, null);
            mpDictionary.Add(2, null);
            mpDictionary.Add(3, null);
            mpDictionary.Add(4, null);
            mpDictionary.Add(5, null);
            mpDictionary.Add(6, null);
            mpDictionary.Add(7, null);
        }

        //Skeleton data processing (ran every frame)
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

                handMovements.currentTimestamp = skeletonFrame.Timestamp;

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

                        if (player.mode != MainWindow.PlayerMode.Create)
                        {
                            wallUpdate(player);
                        }
                        else if (player.mode == MainWindow.PlayerMode.Create)
                        {
                            //Creation mode code
                            wallCreateUpdate(player);
                        }
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

        internal void checkBoxHit(MainWindow.Player player, JointType joint)
        {
            //checkDrumHit code
            //MessageBox.Show(Convert.ToString(hitAreaStart[0][1]));
            if (player.skeleton != null)
            {

                double posX = player.skeleton.Joints[joint].Position.X;
                double posY = player.skeleton.Joints[joint].Position.Y;
                double posZ = player.skeleton.Joints[joint].Position.Z;

                for (int i = 0; i <= hitArea[player.skeleton.TrackingId].Count-1; i++)
                {
                    if (hitArea[player.skeleton.TrackingId][i].X1 < posX && hitArea[player.skeleton.TrackingId][i].X2 > posX && hitArea[player.skeleton.TrackingId][i].Y1 < posY && hitArea[player.skeleton.TrackingId][i].Y2 > posY && hitArea[player.skeleton.TrackingId][i].Z1 < posZ && hitArea[player.skeleton.TrackingId][i].Z2 > posZ)
                    {
                        if (!insideArea[player.skeleton.TrackingId][joint][i])
                        {
                            if (handMovements.difference != null)
                            {
                                if (i <= 8 && handMovements.difference[player.skeleton.TrackingId][joint].Z < -0.01 || (i == 9 && handMovements.difference[player.skeleton.TrackingId][joint].X < -0.01) || (i == 10 && handMovements.difference[player.skeleton.TrackingId][joint].X > 0.01))
                                {
                                    playWallSound(i, player, joint);
                                    Console.WriteLine("HIT! " + i);
                                    insideArea[player.skeleton.TrackingId][joint][i] = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        insideArea[player.skeleton.TrackingId][joint][i] = false;
                    }
                }
            }
        }

        internal void checkBoxRecordHit(MainWindow.Player player, JointType joint)
        {
            //checkDrumHit code
            //MessageBox.Show(Convert.ToString(hitAreaStart[0][1]));
            if (player.skeleton != null)
            {

                double posX = player.skeleton.Joints[joint].Position.X;
                double posY = player.skeleton.Joints[joint].Position.Y;
                double posZ = player.skeleton.Joints[joint].Position.Z;

                for (int i = 0; i <= hitArea[player.skeleton.TrackingId].Count - 1; i++)
                {
                    if (hitArea[player.skeleton.TrackingId][i].X1 < posX && hitArea[player.skeleton.TrackingId][i].X2 > posX && hitArea[player.skeleton.TrackingId][i].Y1 < posY && hitArea[player.skeleton.TrackingId][i].Y2 > posY && hitArea[player.skeleton.TrackingId][i].Z1 < posZ && hitArea[player.skeleton.TrackingId][i].Z2 > posZ)
                    {
                        if (!insideArea[player.skeleton.TrackingId][joint][i])
                        {
                            if (handMovements.difference != null)
                            {
                                if (i <= 8 && handMovements.difference[player.skeleton.TrackingId][joint].Z < -0.01 || (i == 9 && handMovements.difference[player.skeleton.TrackingId][joint].X < -0.01) || (i == 10 && handMovements.difference[player.skeleton.TrackingId][joint].X > 0.01))
                                {
                                    //Hit - Selected, start recording, finish recording
                                    if (boxToRecord == null && !boxRecording)
                                    {
                                        //Select a box to record into
                                        boxRecordSelection(player, i);
                                    }
                                    else if (boxToRecord != null && !boxRecording)
                                    {
                                        //Record into this box
                                        if (boxToRecord == i.ToString())
                                        {
                                            if (!boxRecording)
                                            {
                                                boxRecordSelection(player, i, true);
                                                recordingTimer = new DispatcherTimer();
                                                recordingTimer.Interval = TimeSpan.FromSeconds(2);
                                                recordingTimer.Tick += new EventHandler(recordingTimer_Tick);
                                                recordingTimer.Start();

                                                var t = new Thread(new ParameterizedThreadStart((boxRecordStart)));
                                                t.Start(MainWindow.sensor);
                                            }
                                        }
                                        else
                                        {
                                            boxRecordSelection(player, i);
                                        }
                                        
                                    }
                                    /*else if (boxRecording)
                                    {
                                        //Stop recording
                                        boxRecordStop(player);
                                    }*/
                                    insideArea[player.skeleton.TrackingId][joint][i] = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        insideArea[player.skeleton.TrackingId][joint][i] = false;
                    }
                }
            }
        }

        void recordingTimer_Tick(object sender, EventArgs e)
        {
            if (recordingTimer != null)
            {
                recordingTimer.Stop();
                recordingTimer.Tick -= recordingTimer_Tick;
                recordingTimer = null;

                removeWallInteractionVisual(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey]);
            }
        }

        private void boxRecordStart(object sensor)
        {
            KinectSensor aSensor = (KinectSensor)sensor;
            boxRecordStart();
        }

        private void boxRecordSelection(MainWindow.Player player, int box, bool recording = false)
        {
            Console.WriteLine("Selected box: " + box);
            boxToRecord = box.ToString();
            removeWallInteractionVisual(player);
            wallInteractionVisual(player, box, recording);
        }

        private void boxSelection(MainWindow.Player player, int box, bool recording = false)
        {
            
            wallInteractionVisual(player, box);
        }

        private event RoutedEventHandler FinishedRecording;

        private void boxRecordStart()
        {
            FinishedRecording += new RoutedEventHandler(wallOfSound_FinishedRecording);
            Console.WriteLine("Start recording: " + boxToRecord);
            boxRecording = true;

            byte[] buffer = new byte[1024];

            if (!Directory.Exists("audio/wall/create"))
            {
                Directory.CreateDirectory("audio/wall/create");
            }


            using (FileStream _fileStream = new FileStream("audio/wall/create/" + boxToRecord + ".wav", FileMode.Create))
            {
                WriteWavHeader(_fileStream, 2 * 2 * 16000);

                //Start capturing audio                               
                using (Stream audioStream = MainWindow.sensor.AudioSource.Start())
                {
                    //Simply copy the data from the stream down to the file
                    int count, totalCount = 0;
                    while ((count = audioStream.Read(buffer, 0, buffer.Length)) > 0 && totalCount < (2 * 2 * 16000))
                    {
                        _fileStream.Write(buffer, 0, count);
                        totalCount += count;
                    }
                }
            }

            if (FinishedRecording != null)
            {
                FinishedRecording(null, null);
            }

        }

        void wallOfSound_FinishedRecording(object sender, RoutedEventArgs e)
        {
            //Finished recording
            FinishedRecording -= wallOfSound_FinishedRecording;
            boxRecording = false;
            boxToRecord = null;

            //TODO: Surely there's a better way than reinitialising the speech recogniser?
            MainWindow.mySpeechRecognizer.Start(MainWindow.sensor.AudioSource);
        }
        #region .wav creation code
        static void WriteWavHeader(Stream stream, int dataLength)
        {
            //We need to use a memory stream because the BinaryWriter will close the underlying stream when it is closed
            using (var memStream = new MemoryStream(64))
            {
                int cbFormat = 18; //sizeof(WAVEFORMATEX)
                WAVEFORMATEX format = new WAVEFORMATEX()
                {
                    wFormatTag = 1,
                    nChannels = 1,
                    nSamplesPerSec = 16000,
                    nAvgBytesPerSec = 32000,
                    nBlockAlign = 2,
                    wBitsPerSample = 16,
                    cbSize = 0
                };

                using (var bw = new BinaryWriter(memStream))
                {
                    //RIFF header
                    WriteString(memStream, "RIFF");
                    bw.Write(dataLength + cbFormat + 4); //File size - 8
                    WriteString(memStream, "WAVE");
                    WriteString(memStream, "fmt ");
                    bw.Write(cbFormat);

                    //WAVEFORMATEX
                    bw.Write(format.wFormatTag);
                    bw.Write(format.nChannels);
                    bw.Write(format.nSamplesPerSec);
                    bw.Write(format.nAvgBytesPerSec);
                    bw.Write(format.nBlockAlign);
                    bw.Write(format.wBitsPerSample);
                    bw.Write(format.cbSize);

                    //data header
                    WriteString(memStream, "data");
                    bw.Write(dataLength);
                    memStream.WriteTo(stream);
                }
            }
        }

        static void WriteString(Stream stream, string s)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(s);
            stream.Write(bytes, 0, bytes.Length);
        }

        struct WAVEFORMATEX
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }
        #endregion

        private void boxRecordStop(MainWindow.Player player)
        {
            Console.WriteLine("Stop recording");
            boxRecording = false;
            boxToRecord = null;
            removeWallInteractionVisual(player);
        }

        private void wallUpdate(MainWindow.Player player)
        {
            //What we need to do every skeleton frame with respect to this player's Wall
            defineHitAreas(player);

            if (currentFocus == playerFocus.None)
            {
                checkBoxHit(player, JointType.HandLeft);
                checkBoxHit(player, JointType.HandRight);
            }

            setWallPosition(player);
        }

        private void wallCreateUpdate(MainWindow.Player player)
        {
            //What we need to do every skeleton frame with respect to this player's Wall
            defineHitAreas(player);

            if (currentFocus == playerFocus.None)
            {
                checkBoxRecordHit(player, JointType.HandLeft);
                checkBoxRecordHit(player, JointType.HandRight);
            }

            setWallPosition(player);
        }

        private double scaledWidth(MainWindow.Player player)
        {
            //y = 696.24e-0.007x

            //Player distance (Converted to centimetres)
            double distance = player.skeleton.Position.Z * 100;

            double width = 1112.5 * Math.Pow(Math.E, -0.006 * distance);

            return width;
        }

        private void setWallPosition(MainWindow.Player player)
        {
            FrameworkElement image = player.instrumentImage;

            image.Width = scaledWidth(player);

            ColorImagePoint point = MainWindow.sensor.MapSkeletonPointToColor(player.skeleton.Joints[JointType.Spine].Position, ColorImageFormat.RgbResolution640x480Fps30);

            //Grab the image reference and move it to the correct place
            Canvas.SetLeft(image, point.X - (image.ActualWidth / 2));
            Canvas.SetTop(image, point.Y - (image.ActualHeight / 2));

            if (player.instrumentOverlay != null)
            {
                //If the player currently has an overlay to display, align that too
                foreach (var overlay in player.instrumentOverlay)
                {
                    image = overlay.Value;

                    image.Width = scaledWidth(player);

                    Canvas.SetLeft(image, point.X - (image.ActualWidth / 2));
                    Canvas.SetTop(image, point.Y - (image.ActualHeight / 2));
                }
            }
        }

        private void playWallSound(int i, MainWindow.Player player, JointType joint)
        {
            if (wallAudio[player.skeleton.TrackingId][i] != null)
            {
                if (mpDictionary[(mpCounter % mpDictionary.Count)] == null)
                {
                    mpDictionary[(mpCounter % mpDictionary.Count)] = new playerSound();
                }
                else
                {
                    mpDictionary[(mpCounter % mpDictionary.Count)].mediaPlayer.MediaEnded -= wallOfSound_MediaEnded;
                    if (mpDictionary[(mpCounter % mpDictionary.Count)].box != i)
                    {
                        removeWallInteractionVisual(player, mpDictionary[(mpCounter % mpDictionary.Count)].box);
                    }
                }

                mpDictionary[(mpCounter % mpDictionary.Count)].skeleton = player.skeleton.TrackingId;
                mpDictionary[(mpCounter % mpDictionary.Count)].box = i;

                mpDictionary[(mpCounter % mpDictionary.Count)].mediaPlayer.Open(new Uri(wallAudio[player.skeleton.TrackingId][i], UriKind.Relative));
                mpDictionary[(mpCounter % mpDictionary.Count)].mediaPlayer.Play();

                mpDictionary[(mpCounter % mpDictionary.Count)].mediaPlayer.MediaEnded += new EventHandler(wallOfSound_MediaEnded);

                mpCounter++;

                boxSelection(player, i);
            }
        }

        void wallOfSound_MediaEnded(object sender, EventArgs e)
        {
            MediaPlayer player = (MediaPlayer)sender;

            foreach (var entry in mpDictionary)
            {
                if (entry.Value != null && entry.Value.mediaPlayer == player)
                {
                    if (MainWindow.activeSkeletons.ContainsKey(mpDictionary[entry.Key].skeleton))
                    {
                        removeWallInteractionVisual(MainWindow.activeSkeletons[mpDictionary[entry.Key].skeleton],entry.Value.box);
                    }

                    mpDictionary[entry.Key] = null;
                    return;
                }
            }
            
            player.Close();
            
            player.MediaEnded -= wallOfSound_MediaEnded;
        }

        private void newPlayerWall(MainWindow.Player player)
        {
            //Set up a new player with their Wall
            player.instrument = instrument.instrumentList.WallOfSound;
            player.mode = MainWindow.PlayerMode.Drum;
            
            Image image = new Image();
            image.Source = new BitmapImage(new Uri("images/wall-sample.png", UriKind.Relative));

            MainCanvas.Children.Add(image);

            player.instrumentImage = image;

            setupPlayerAudio(player);
            setupWall(player);
        }

        private void wallInteractionVisual(MainWindow.Player player, int box, bool recording = false)
        {
            if (!player.instrumentOverlay.ContainsKey(box))
            {
                Image image = new Image();

                string url;

                if (recording)
                {
                    url = "images/wall-selection/wall" + box + "-rec.png";
                }
                else
                {
                    url = "images/wall-selection/wall" + box + ".png";
                }
                image.Source = new BitmapImage(new Uri(url, UriKind.Relative));

                player.instrumentOverlay.Add(box, image);
                MainCanvas.Children.Add(image);
            }
        }

        private void removePlayerWall(MainWindow.Player player)
        {
            //Clean up player wall data
            
            //Remove all audio references
            if (wallAudio.ContainsKey(player.skeleton.TrackingId))
            {
                wallAudio.Remove(player.skeleton.TrackingId);
            }

            //Remove their wall graphic
            MainCanvas.Children.Remove(player.instrumentImage);
            removeWallInteractionVisual(player);
        }

        private void removeWallInteractionVisual(MainWindow.Player player, int box = 99)
        {
            if (player.instrumentOverlay.Count > 0)
            {
                if (box == 99)
                {
                    //Remove all
                    Dictionary<int, Image> overlays = new Dictionary<int,Image>(player.instrumentOverlay);

                    foreach (var image in overlays)
                    {
                        MainCanvas.Children.Remove(image.Value);
                        player.instrumentOverlay.Remove(image.Key);
                    }
                }
                else
                {
                    //Remove specified
                    int typeCount = 0;

                    foreach (var mp in mpDictionary)
                    {
                        if (mp.Value != null && mp.Value.box == box)
                        {
                            typeCount++;
                        }
                    }

                    if (typeCount < 2)
                    {
                        MainCanvas.Children.Remove(player.instrumentOverlay[box]);
                        player.instrumentOverlay.Remove(box);
                    }
                }
            }
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

            MainWindow.animateSlide(kinectGuideCanvas, false, false, -150, 0.5);
            MainWindow.animateSlide(imgMenuSelected, false, false, -150, 0.5);
            
            kinectGuideCanvas.Visibility = System.Windows.Visibility.Visible;
            imgMenuSelected.Visibility = System.Windows.Visibility.Visible;

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
            if (handMovements.leftSwipeRightIn == null)
            {
                //Manipulate the guide if we're not currently swiping to select
                double angleValue = handMovements.getAngle(player.skeleton.Joints[JointType.ShoulderLeft], player.skeleton.Joints[JointType.HandLeft]);

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

            Canvas.SetTop(kinectGuideCanvas, 60 * menuPosition);

            MainWindow.animateSlide(kinectGuideCanvas, true, false, -150, 0.5);
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
                case menuOptions.GoBack:
                    returnToStart();
                    break;
                case menuOptions.TakeAPicture:
                    takeAPicture();
                    break;
                case menuOptions.RecordNewWall:
                    MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].mode = MainWindow.PlayerMode.Create;
                    break;
                case menuOptions.CustomWall:
                    MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].mode = MainWindow.PlayerMode.Custom;
                    customAudio(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey]);
                    break;
                case menuOptions.Technologic:
                    MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].mode = MainWindow.PlayerMode.Technologic;
                    technologicAudio(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey]);
                    break;
                case menuOptions.Drum:
                    MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].mode = MainWindow.PlayerMode.Drum;
                    drumsetAudio(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey]);
                    break;
                case menuOptions.EightBit:
                    MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].mode = MainWindow.PlayerMode.EightBit;
                    eightBitAudio(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey]);
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

        private void toggleRGB(ColorImageFormat format, int delay = 3000)
        {
            if (MainWindow.sensor.ColorStream.Format != format)
            {
                foreach (var player in MainWindow.activeSkeletons)
                {
                    player.Value.instrumentImage.Visibility = System.Windows.Visibility.Hidden;
                    if (player.Value.instrumentOverlay != null)
                    {
                        foreach (var image in player.Value.instrumentOverlay)
                        {
                            image.Value.Visibility = System.Windows.Visibility.Hidden;
                        }
                        
                    }
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
                if (player.Value.instrumentOverlay != null)
                {
                    foreach(var image in player.Value.instrumentOverlay) {
                        image.Value.Visibility = System.Windows.Visibility.Hidden;
                    }
                }
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

            destroyVoice();

            this.NavigationService.GoBack();
        }

        private void destroyVoice()
        {
            MainWindow.mySpeechRecognizer.toggleListening(false);
            MainWindow.mySpeechRecognizer.SaidSomething -= this.RecognizerSaidSomething;
            MainWindow.mySpeechRecognizer.ListeningChanged -= this.ListeningChanged;
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
           Canvas.SetTop(kinectGuideCanvas, 60 * menuPosition);
            
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
    }
}
