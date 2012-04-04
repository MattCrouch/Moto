using System;
using System.Collections.Generic;
using System.Linq;
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

            //Create dictionary definitions for all the Media Players available
            generateMediaPlayers();

            setupVoice();

            userImage.Source = MainWindow.colorImageBitmap;
            userDepth.Source = MainWindow.depthImageBitmap;

            processExistingSkeletons(MainWindow.activeSkeletons);

            this.FocusVisualStyle = null;
            this.Focus();
        }

        private void processExistingSkeletons(Dictionary<int, MainWindow.Player> activeSkeletons)
        {
            foreach (var player in activeSkeletons)
            {
                newPlayerWall(player.Value);
                Console.WriteLine(player.Value.skeleton.TrackingId);
            }
        }

        //Wall of Sound areas
        Dictionary<int, Dictionary<int, MainWindow.HitBox>> hitArea = new Dictionary<int, Dictionary<int, MainWindow.HitBox>>();
        Dictionary<int, Dictionary<JointType, Dictionary<int, bool>>> insideArea = new Dictionary<int, Dictionary<JointType, Dictionary<int, bool>>>();

        //Wall audio
        string[] wallAudio = new string[9];

        private void setupWall(MainWindow.Player player)
        {
            if (!hitArea.ContainsKey(player.skeleton.TrackingId))
            {
                //Blank dictionary of the drums of one person
                Dictionary<int,MainWindow.HitBox> blankDefinitions = new Dictionary<int,MainWindow.HitBox>();
                blankDefinitions.Add(0,new MainWindow.HitBox());
                blankDefinitions.Add(1,new MainWindow.HitBox());
                blankDefinitions.Add(2, new MainWindow.HitBox());

                hitArea.Add(player.skeleton.TrackingId, blankDefinitions);

                defaultWallAudio();
            }

            //Make sure the hands aren't in the drums areas in the first place
            insideArea.Add(player.skeleton.TrackingId, createPlayerDictionary());
        }

        private void defaultWallAudio()
        {
            wallAudio[0] = "audio/buyit.wav";
            wallAudio[1] = "audio/useit.wav";
        }

        Dictionary<JointType, Dictionary<int, bool>> createPlayerDictionary()
        {
            Console.WriteLine("Player dictionary created");
            Dictionary<JointType, Dictionary<int, bool>> dictionary = new Dictionary<JointType, Dictionary<int, bool>>();

            dictionary.Add(JointType.HandLeft, new Dictionary<int, bool>());
            dictionary.Add(JointType.HandRight, new Dictionary<int, bool>());

            dictionary[JointType.HandLeft].Add(0, false);
            dictionary[JointType.HandLeft].Add(1, false);

            dictionary[JointType.HandRight].Add(0, false);
            dictionary[JointType.HandRight].Add(1, false);

            return dictionary;
        }

        internal void defineHitAreas(MainWindow.Player player)
        {
            if (player.skeleton != null)
            {
                double boxSize = 0.25; //Size of drum edges (in metres)

                //First drum
                hitArea[player.skeleton.TrackingId][0].X1 = player.skeleton.Joints[JointType.HipCenter].Position.X - 0.2228546;
                hitArea[player.skeleton.TrackingId][0].X2 = hitArea[player.skeleton.TrackingId][0].X1 + boxSize;
                hitArea[player.skeleton.TrackingId][0].Y1 = player.skeleton.Joints[JointType.HipCenter].Position.Y - 0.36289116;
                hitArea[player.skeleton.TrackingId][0].Y2 = hitArea[player.skeleton.TrackingId][0].Y1 + boxSize;
                hitArea[player.skeleton.TrackingId][0].Z1 = player.skeleton.Joints[JointType.HipCenter].Position.Z - 0.1224589;
                hitArea[player.skeleton.TrackingId][0].Z2 = hitArea[player.skeleton.TrackingId][0].Z1 + boxSize;

                //Second drum
                hitArea[player.skeleton.TrackingId][1].X1 = player.skeleton.Joints[JointType.HipCenter].Position.X + 0.2029613;
                hitArea[player.skeleton.TrackingId][1].X2 = hitArea[player.skeleton.TrackingId][1].X1 + boxSize;
                hitArea[player.skeleton.TrackingId][1].Y1 = player.skeleton.Joints[JointType.HipCenter].Position.Y - 0.36289116;
                hitArea[player.skeleton.TrackingId][1].Y2 = hitArea[player.skeleton.TrackingId][1].Y1 + boxSize;
                hitArea[player.skeleton.TrackingId][1].Z1 = player.skeleton.Joints[JointType.HipCenter].Position.Z - 0.1224589;
                hitArea[player.skeleton.TrackingId][1].Z2 = hitArea[player.skeleton.TrackingId][1].Z1 + boxSize;
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

                for (int i = 0; i <= 2; i++)
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

        private void playWallSound(int i, Skeleton skeleton, JointType joint)
        {
            if (handMovements.difference != null)
            {
                if (handMovements.difference[skeleton.TrackingId][joint].Y < -0.01)
                {
                    mpDictionary[(mpCounter % 4)].Open(new Uri(wallAudio[i], UriKind.Relative));
                    mpDictionary[(mpCounter % 4)].Play();

                    mpCounter++;
                }
            }
        }

        //Audio dictionarys
        Dictionary<int, MediaPlayer> mpDictionary = new Dictionary<int, MediaPlayer>();
        int mpCounter = 0;

        private void generateMediaPlayers()
        {
            mpDictionary.Add(0, new MediaPlayer());
            mpDictionary.Add(1, new MediaPlayer());
            mpDictionary.Add(2, new MediaPlayer());
            mpDictionary.Add(3, new MediaPlayer());
        }

        private void setupVoice()
        {
            MainWindow.mySpeechRecognizer.SaidSomething += this.RecognizerSaidSomething;
            MainWindow.mySpeechRecognizer.ListeningChanged += this.ListeningChanged;
        }

        short[] DepthPixelData = new short[MainWindow.sensor.DepthStream.FramePixelDataLength];

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
                    MainWindow.primarySkeletonKey = MainWindow.selectPrimarySkeleton(MainWindow.activeSkeletons);
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

        private void newPlayerWall(MainWindow.Player player)
        {
            //Set up a new player with their Wall
            player.instrument = instrument.instrumentList.WallOfSound;
            
            Image image = new Image();
            image.Source = new BitmapImage(new Uri("images/wosplaceholder.png", UriKind.Relative));
            image.Height = 100;
            image.Width = 100;

            MainCanvas.Children.Add(image);

            player.instrumentImage = image;

            setupWall(player);
        }

        private void wallUpdate(MainWindow.Player player)
        {
            //What we need to do every skeleton frame with respect to this player's Wall
            defineHitAreas(player);

            checkBoxHit(player.skeleton, JointType.HandLeft);
            checkBoxHit(player.skeleton, JointType.HandRight);

            setWallPosition(player);
        }

        private void setWallPosition(MainWindow.Player player) {
            FrameworkElement image = player.instrumentImage;

            ColorImagePoint point = MainWindow.sensor.MapSkeletonPointToColor(player.skeleton.Position, ColorImageFormat.RgbResolution640x480Fps30);

            //Grab the image reference and move it to the correct place
            Canvas.SetLeft(image, point.X - (image.Width / 2));
            Canvas.SetTop(image, point.Y - (image.Height / 2));
        }

        private void removePlayerWall(MainWindow.Player player)
        {
            //Clean up player wall data
            MainCanvas.Children.Remove(player.instrumentImage);
        }

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

        private void RecognizerSaidSomething(object sender, SpeechRecognizer.SaidSomethingEventArgs e)
        {
            switch (e.Verb)
            {
                case SpeechRecognizer.Verbs.DrumsSwitch:
                    //Do something
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

        #region Image Capture
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
        #endregion

        private void btnBackFromDrums_Click(object sender, RoutedEventArgs e)
        {
            returnToStart();
        }

        private void returnToStart()
        {
            MainWindow.sensor.AllFramesReady -= new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);

            this.NavigationService.GoBack();
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
