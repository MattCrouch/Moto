using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Media;
using Microsoft.Kinect;
using Coding4Fun.Kinect.Wpf;


namespace Moto
{
    public partial class instrument
    {
        //Set up hit areas - TrackingId, XYZ of skeletonDrum
        Dictionary<int, Dictionary<int, MainWindow.HitBox>> keyArea = new Dictionary<int, Dictionary<int, MainWindow.HitBox>>();

        Dictionary<int, Dictionary<JointType, Dictionary<int, bool>>> insideKey = new Dictionary<int, Dictionary<JointType, Dictionary<int, bool>>>();

        internal void setupKeyboard(MainWindow.Player player)
        {
            if (!keyArea.ContainsKey(player.skeleton.TrackingId))
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

                keyArea.Add(player.skeleton.TrackingId, blankDefinitions);
            }

            //Make sure the hands aren't in the areas in the first place
            insideKey.Add(player.skeleton.TrackingId, createPlayerDictionary(keyArea[player.skeleton.TrackingId].Count));
        }

        internal void defineKeyAreas(MainWindow.Player player)
        {
            if (player.skeleton != null)
            {
                //Drums
                defineKey(player, 0, -0.4592496, -0.14543834, -0.3577266);
                defineKey(player, 1, -0.3342496, -0.14543834, -0.3577266);
                defineKey(player, 2, -0.2092496, -0.14543834, -0.3577266);
                defineKey(player, 3, -0.0842496, -0.14543834, -0.3577266);
                defineKey(player, 4, 0.0407504, -0.14543834, -0.3577266);
                defineKey(player, 5, 0.1657504, -0.14543834, -0.3577266);
                defineKey(player, 6, 0.2907504, -0.14543834, -0.3577266);
                defineKey(player, 7, 0.4107504, -0.14543834, -0.3577266);

                SetKeyboardPosition(player);
            }
        }

        private void defineKey(MainWindow.Player player, int key, double X, double Y, double Z, double keyWidth = 0.12, double keyHeight = 0.2, double keyDepth = 0.2)
        {
            keyArea[player.skeleton.TrackingId][key].X1 = player.skeleton.Joints[JointType.HipCenter].Position.X + X;
            keyArea[player.skeleton.TrackingId][key].X2 = keyArea[player.skeleton.TrackingId][key].X1 + keyWidth;
            keyArea[player.skeleton.TrackingId][key].Y1 = player.skeleton.Joints[JointType.HipCenter].Position.Y + Y;
            keyArea[player.skeleton.TrackingId][key].Y2 = keyArea[player.skeleton.TrackingId][key].Y1 + keyHeight;
            keyArea[player.skeleton.TrackingId][key].Z1 = player.skeleton.Joints[JointType.HipCenter].Position.Z + Z;
            keyArea[player.skeleton.TrackingId][key].Z2 = keyArea[player.skeleton.TrackingId][key].Z1 + keyDepth;
        }

        private void SetKeyboardPosition(MainWindow.Player player)
        {
            FrameworkElement image = player.instrumentImage;

            image.Width = scaledWidth(player.skeleton.Joints[JointType.Spine].Position, player.instrument);

            if (MainWindow.sensor.IsRunning)
            {
                ColorImagePoint point = MainWindow.sensor.MapSkeletonPointToColor(player.skeleton.Joints[JointType.Spine].Position, ColorImageFormat.RgbResolution640x480Fps30);

                //Grab the image reference and move it to the correct place
                Canvas.SetLeft(image, point.X - (image.ActualWidth / 2));
                Canvas.SetTop(image, point.Y);

                foreach (var overlay in player.instrumentOverlay)
                {
                    overlay.Value.Width = image.Width;
                    Canvas.SetLeft(overlay.Value, point.X - (image.ActualWidth / 2));
                    Canvas.SetTop(overlay.Value, point.Y);
                }
            }
        }

        internal void checkKeyHit(MainWindow.Player player, JointType joint)
        {
            //checkDrumHit code
            //MessageBox.Show(Convert.ToString(hitAreaStart[0][1]));
            if (player.skeleton != null && player.skeleton.Joints[joint].TrackingState == JointTrackingState.Tracked)
            {

                double posX = player.skeleton.Joints[joint].Position.X;
                double posY = player.skeleton.Joints[joint].Position.Y;
                double posZ = player.skeleton.Joints[joint].Position.Z;

                for (int i = 0; i <= keyArea[player.skeleton.TrackingId].Count - 1; i++)
                {
                    if (keyArea[player.skeleton.TrackingId][i].X1 < posX && keyArea[player.skeleton.TrackingId][i].X2 > posX && keyArea[player.skeleton.TrackingId][i].Y1 < posY && keyArea[player.skeleton.TrackingId][i].Y2 > posY && keyArea[player.skeleton.TrackingId][i].Z1 < posZ && keyArea[player.skeleton.TrackingId][i].Z2 > posZ)
                    {
                        if (!insideKey[player.skeleton.TrackingId][joint][i])
                        {
                            if (handMovements.difference != null)
                            {
                                //MessageBox.Show(Convert.ToString(difference["X"]));
                                if (handMovements.difference[player.skeleton.TrackingId][joint].Y < 0 && (joint != JointType.FootLeft || joint != JointType.FootRight))
                                {
                                    hitKey("keyboard" + i);
                                    Debug.Print("HIT! " + i);
                                    insideKey[player.skeleton.TrackingId][joint][i] = true;
                                    showKeyboardOverlay(player, i);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (insideKey[player.skeleton.TrackingId][joint][i])
                        {
                            hideKeyboardOverlay(player, i);
                            insideKey[player.skeleton.TrackingId][joint][i] = false;
                        }
                    }
                }
            }
        }

        private void showKeyboardOverlay(MainWindow.Player player, int i)
        {
            Image overlay = new Image();
            overlay.Source = new BitmapImage(new Uri("images/keyboard-overlays/" + i + ".png", UriKind.Relative));

            if (MainWindow.activeSkeletons[player.skeleton.TrackingId].instrumentOverlay.ContainsKey(i))
            {
                hideKeyboardOverlay(player, i);
            }
            MainWindow.activeSkeletons[player.skeleton.TrackingId].instrumentOverlay.Add(i, overlay);

            MainCanvas.Children.Add(overlay);
            Canvas.SetTop(overlay, -200);
        }

        private void hideKeyboardOverlay(MainWindow.Player player, int i)
        {
            if (player.instrumentOverlay.ContainsKey(i))
            {
                MainCanvas.Children.Remove(player.instrumentOverlay[i]);
                player.instrumentOverlay.Remove(i);
            }
        }

        private void hitKey(string keyName)
        {
            if (mpDictionary[(mpCounter % mpDictionary.Count)] == null)
            {
                mpDictionary[(mpCounter % mpDictionary.Count)] = new MediaPlayer();
            }

            mpDictionary[(mpCounter % mpDictionary.Count)].Open(new Uri("audio/keyboard/hard/" + keyName + ".wav", UriKind.Relative));
            mpDictionary[(mpCounter % mpDictionary.Count)].Play();

            mpDictionary[(mpCounter % mpDictionary.Count)].MediaEnded += new EventHandler(instruments_MediaEnded);

            mpCounter++;
        }
    }
}
