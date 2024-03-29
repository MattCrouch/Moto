﻿using System;
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
        Dictionary<int, Dictionary<int,MainWindow.HitBox>> hitArea = new Dictionary<int,Dictionary<int, MainWindow.HitBox>>();

        Dictionary<int, Dictionary<JointType, Dictionary<int, bool>>> insideArea = new Dictionary<int, Dictionary<JointType, Dictionary<int, bool>>>();

        internal void setupDrums(MainWindow.Player player)
        {
            if (!hitArea.ContainsKey(player.skeleton.TrackingId))
            {
                //Blank dictionary of the drums of one person
                Dictionary<int,MainWindow.HitBox> blankDefinitions = new Dictionary<int,MainWindow.HitBox>();
                blankDefinitions.Add(0,new MainWindow.HitBox());
                blankDefinitions.Add(1,new MainWindow.HitBox());
                blankDefinitions.Add(2, new MainWindow.HitBox());
                blankDefinitions.Add(3, new MainWindow.HitBox());
                blankDefinitions.Add(4, new MainWindow.HitBox());
                blankDefinitions.Add(5, new MainWindow.HitBox());
                blankDefinitions.Add(6, new MainWindow.HitBox());
                blankDefinitions.Add(7, new MainWindow.HitBox());

                hitArea.Add(player.skeleton.TrackingId, blankDefinitions);
            }

            //Make sure the hands aren't in the drums areas in the first place
            insideArea.Add(player.skeleton.TrackingId, createPlayerDictionary(8));
        }

        Dictionary<JointType,Dictionary<int,bool>> createPlayerDictionary(int dictCount)
        {
            Dictionary<JointType, Dictionary<int, bool>> dictionary = new Dictionary<JointType, Dictionary<int, bool>>();

            dictionary.Add(JointType.HandLeft, defaultInsideAreaVals(dictCount));
            dictionary.Add(JointType.HandRight, defaultInsideAreaVals(dictCount));
            dictionary.Add(JointType.FootLeft, defaultInsideAreaVals(dictCount));
            dictionary.Add(JointType.FootRight, defaultInsideAreaVals(dictCount));

            return dictionary;
        }

        internal Dictionary<int,bool> defaultInsideAreaVals(int dictCount)
        {
            Dictionary<int, bool> dictionary = new Dictionary<int, bool>();

            for (int i = 0; i < dictCount; i++)
            {
                dictionary.Add(i, false);
            }

            return dictionary;
        }

        internal void defineHitAreas(MainWindow.Player player)
        {
            if (player.skeleton != null)
            {
                //Drums
                defineDrum(player, 0, -0.3035938, -0.0150525, -0.3892172);
                defineDrum(player, 1, 0.1035938, -0.0150525, -0.3892172);
                defineDrum(player, 2, -0.4431849, -0.15812907, -0.2434439);
                defineDrum(player, 3, 0.2913417, -0.15812907, -0.2434439);

                //Cymbal
                defineDrum(player, 4, -0.5374842, 0.2215563, -0.6076295, 0.3, 0.1, 0.5);
                defineDrum(player, 5, 0.30892, 0.2215563, -0.6076295, 0.3, 0.1, 0.5);

                //Kick drum
                defineDrum(player, 6, -0.3076267, -0.7402052, -0.8686381,0.6, 0.5, 0.6);

                SetDrumPosition(player);
            }
        }

        private void defineDrum(MainWindow.Player player, int drum, double X, double Y, double Z, double drumWidth = 0.2, double drumHeight = 0.2, double drumDepth = 0.2)
        {
            hitArea[player.skeleton.TrackingId][drum].X1 = player.skeleton.Joints[JointType.HipCenter].Position.X + X;
            hitArea[player.skeleton.TrackingId][drum].X2 = hitArea[player.skeleton.TrackingId][drum].X1 + drumWidth;
            hitArea[player.skeleton.TrackingId][drum].Y1 = player.skeleton.Joints[JointType.HipCenter].Position.Y + Y;
            hitArea[player.skeleton.TrackingId][drum].Y2 = hitArea[player.skeleton.TrackingId][drum].Y1 + drumHeight;
            hitArea[player.skeleton.TrackingId][drum].Z1 = player.skeleton.Joints[JointType.HipCenter].Position.Z + Z;
            hitArea[player.skeleton.TrackingId][drum].Z2 = hitArea[player.skeleton.TrackingId][drum].Z1 + drumDepth;
        }

        internal void checkDrumHit(MainWindow.Player player, JointType joint)
        {
            //checkDrumHit code
            //MessageBox.Show(Convert.ToString(hitAreaStart[0][1]));
            if (player.skeleton != null && player.skeleton.Joints[joint].TrackingState == JointTrackingState.Tracked)
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
                                if (handMovements.difference[player.skeleton.TrackingId][joint].Y < -0.01 || ((joint == JointType.FootLeft || joint == JointType.FootRight) && handMovements.difference[player.skeleton.TrackingId][joint].Z < -0.02))
                                {
                                    hitDrum("drum" + i);
                                    Debug.Print("HIT! " + i);
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

        private void hitDrum(string drumName)
        {
            if (mpDictionary[(mpCounter % mpDictionary.Count)] == null)
            {
                mpDictionary[(mpCounter % mpDictionary.Count)] = new MediaPlayer();
            }

            mpDictionary[(mpCounter % mpDictionary.Count)].Open(new Uri("audio/drums/hard/" + drumName + ".wav", UriKind.Relative));
            
            mpDictionary[(mpCounter % mpDictionary.Count)].Play();

            mpDictionary[(mpCounter % mpDictionary.Count)].MediaEnded += new EventHandler(instruments_MediaEnded);

            mpCounter++;
        }

        void instruments_MediaEnded(object sender, EventArgs e)
        {
            MediaPlayer player = (MediaPlayer)sender;
            foreach (var entry in mpDictionary)
            {
                if (entry.Value == player)
                {
                    mpDictionary[entry.Key] = null;
                    return;
                }
            }

            player.Close();

            player.MediaEnded -= instruments_MediaEnded;
        }

        private void SetDrumPosition(MainWindow.Player player)
        {
            FrameworkElement image = player.instrumentImage;

            image.Width = scaledWidth(player.skeleton.Position, player.instrument);

            ColorImagePoint point = MainWindow.sensor.MapSkeletonPointToColor(player.skeleton.Position, ColorImageFormat.RgbResolution640x480Fps30);

            //Grab the image reference and move it to the correct place
            Canvas.SetLeft(image, point.X - (image.ActualWidth / 2));
            Canvas.SetTop(image, point.Y - (image.ActualHeight / 2));
        }
    }
}
