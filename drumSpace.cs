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

                hitArea.Add(player.skeleton.TrackingId, blankDefinitions);
            }

            //Make sure the hands aren't in the drums areas in the first place
            insideArea.Add(player.skeleton.TrackingId, createPlayerDictionary());
        }

        Dictionary<JointType,Dictionary<int,bool>> createPlayerDictionary()
        {
            Dictionary<JointType, Dictionary<int, bool>> dictionary = new Dictionary<JointType, Dictionary<int, bool>>();

            dictionary.Add(JointType.HandLeft, new Dictionary<int, bool>());
            dictionary.Add(JointType.HandRight, new Dictionary<int, bool>());

            dictionary[JointType.HandLeft].Add(0, false);
            dictionary[JointType.HandLeft].Add(1, false);
            dictionary[JointType.HandLeft].Add(2, false);
            dictionary[JointType.HandRight].Add(0, false);
            dictionary[JointType.HandRight].Add(1, false);
            dictionary[JointType.HandRight].Add(2, false);

            return dictionary;
        }

        internal void defineHitAreas(MainWindow.Player player)
        {
            if (player.skeleton != null)
            {
                double distQuotient = instrument.distQuotient(1, 3, player.skeleton.Position.Z, -0.05, 0.05);
                double drumSize = 0.3 - distQuotient; //Size of drum edges (in metres)

                //First drum
                hitArea[player.skeleton.TrackingId][0].X1 = player.skeleton.Joints[JointType.HipCenter].Position.X - 0.2228546;
                hitArea[player.skeleton.TrackingId][0].X2 = hitArea[player.skeleton.TrackingId][0].X1 + drumSize;
                hitArea[player.skeleton.TrackingId][0].Y1 = player.skeleton.Joints[JointType.HipCenter].Position.Y - 0.36289116;
                hitArea[player.skeleton.TrackingId][0].Y2 = hitArea[player.skeleton.TrackingId][0].Y1 + drumSize;
                hitArea[player.skeleton.TrackingId][0].Z1 = player.skeleton.Joints[JointType.HipCenter].Position.Z - 0.1224589;
                hitArea[player.skeleton.TrackingId][0].Z2 = hitArea[player.skeleton.TrackingId][0].Z1 + drumSize;

                //Second drum
                hitArea[player.skeleton.TrackingId][1].X1 = player.skeleton.Joints[JointType.HipCenter].Position.X + 0.2029613;
                hitArea[player.skeleton.TrackingId][1].X2 = hitArea[player.skeleton.TrackingId][1].X1 + drumSize;
                hitArea[player.skeleton.TrackingId][1].Y1 = player.skeleton.Joints[JointType.HipCenter].Position.Y - 0.36289116;
                hitArea[player.skeleton.TrackingId][1].Y2 = hitArea[player.skeleton.TrackingId][1].Y1 + drumSize;
                hitArea[player.skeleton.TrackingId][1].Z1 = player.skeleton.Joints[JointType.HipCenter].Position.Z - 0.1224589;
                hitArea[player.skeleton.TrackingId][1].Z2 = hitArea[player.skeleton.TrackingId][1].Z1 + drumSize;

                //Third drum
                hitArea[player.skeleton.TrackingId][2].X1 = player.skeleton.Joints[JointType.HipCenter].Position.X + 0.3183096;
                hitArea[player.skeleton.TrackingId][2].X2 = hitArea[player.skeleton.TrackingId][2].X1 + drumSize;
                hitArea[player.skeleton.TrackingId][2].Y1 = player.skeleton.Joints[JointType.HipCenter].Position.Y + 0.22338450;
                hitArea[player.skeleton.TrackingId][2].Y2 = hitArea[player.skeleton.TrackingId][2].Y1 + drumSize;
                hitArea[player.skeleton.TrackingId][2].Z1 = player.skeleton.Joints[JointType.HipCenter].Position.Z - 0.1224589;
                hitArea[player.skeleton.TrackingId][2].Z2 = hitArea[player.skeleton.TrackingId][2].Z1 + drumSize;

                SetDrumPosition(player);
            }
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

                for (int i = 0; i <= 2; i++)
                {
                    if (hitArea[player.skeleton.TrackingId][i].X1 < posX && hitArea[player.skeleton.TrackingId][i].X2 > posX && hitArea[player.skeleton.TrackingId][i].Y1 < posY && hitArea[player.skeleton.TrackingId][i].Y2 > posY && hitArea[player.skeleton.TrackingId][i].Z1 < posZ && hitArea[player.skeleton.TrackingId][i].Z2 > posZ)
                    {
                        if (!insideArea[player.skeleton.TrackingId][joint][i])
                        {
                            hitDrum("drum" + i, player.skeleton, joint);
                            Debug.Print("HIT! " + i);
                            insideArea[player.skeleton.TrackingId][joint][i] = true;
                        }
                    }
                    else
                    {
                        insideArea[player.skeleton.TrackingId][joint][i] = false;
                    }
                }
            }
        }

        int mpCounter = 0;

        //MediaPlayer[] mpArray;
        Dictionary<int, MediaPlayer> mpDictionary = new Dictionary<int, MediaPlayer>();

        private void generateMediaPlayers()
        {
            mpDictionary.Add(0, new MediaPlayer());
            mpDictionary.Add(1, new MediaPlayer());
            mpDictionary.Add(2, new MediaPlayer());
            mpDictionary.Add(3, new MediaPlayer());
        }

        private void hitDrum(string drumName, Skeleton skeleton, JointType joint)
        {
            //MessageBox.Show("HIT DRUM!");
            if (handMovements.difference != null)
            {
                //MessageBox.Show(Convert.ToString(difference["X"]));
                if (handMovements.difference[skeleton.TrackingId][joint].Y < -0.01)
                {
                    mpDictionary[(mpCounter % 4)].Open(new Uri("audio/drums/" + drumName + ".wav", UriKind.Relative));
                    mpDictionary[(mpCounter % 4)].Play();

                    mpCounter++;
                                        
                    //SoundPlayer drumSound = new SoundPlayer(drumName + ".wav");
                    //drumSound.Play();
                }
            }
        }

        private void SetDrumPosition(MainWindow.Player player)
        {
            FrameworkElement image = player.instrumentImage;

            ColorImagePoint point = MainWindow.sensor.MapSkeletonPointToColor(player.skeleton.Position, ColorImageFormat.RgbResolution640x480Fps30);

            //Grab the image reference and move it to the correct place
            Canvas.SetLeft(image, point.X - (image.Width / 2));
            Canvas.SetTop(image, point.Y - (image.Height / 2));
        }
    }
}
