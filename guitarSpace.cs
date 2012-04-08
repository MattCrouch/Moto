using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;
using Microsoft.Kinect;


namespace Moto
{
    public partial class instrument
    {
        Dictionary<int, MainWindow.HitBox> strumArea = new Dictionary<int, MainWindow.HitBox>();
        Dictionary<int, bool> insideStrumArea = new Dictionary<int, bool>();

        internal void setupGuitar(MainWindow.Player player)
        {
            //Setting up guitar variables/runtimes
            if (!strumArea.ContainsKey(player.skeleton.TrackingId))
            {
                strumArea.Add(player.skeleton.TrackingId, new MainWindow.HitBox());
            }

            if (!insideStrumArea.ContainsKey(player.skeleton.TrackingId)) {
                insideStrumArea.Add(player.skeleton.TrackingId, false);
            }
            
        }

        private double doPythag(double a, double b)
        {
            double c = Math.Sqrt(Math.Pow(a, 2) + Math.Pow(b, 2));
            return c;
        }

        internal double checkNeckDist(MainWindow.Player player, Joint hand)
        {
            Joint hip = player.skeleton.Joints[JointType.HipCenter];

            double xLength = doPythag((hip.Position.Z - hand.Position.Z), (hip.Position.X - hand.Position.X));

            double neckDist = doPythag(xLength, (hip.Position.Y - hand.Position.Y));

            return neckDist;
        }
        
        internal void defineStrumArea(MainWindow.Player player) {
             double strumSize = 0.2; //Size of strum area edges (in metres)

            strumArea[player.skeleton.TrackingId].X1 = player.skeleton.Joints[JointType.HipCenter].Position.X - (strumSize / 2);
            strumArea[player.skeleton.TrackingId].X2 = player.skeleton.Joints[JointType.HipCenter].Position.X + (strumSize / 2);
            strumArea[player.skeleton.TrackingId].Y1 = player.skeleton.Joints[JointType.HipCenter].Position.Y - (strumSize / 2);
            strumArea[player.skeleton.TrackingId].Y2 = player.skeleton.Joints[JointType.HipCenter].Position.Y + (strumSize / 2);
            strumArea[player.skeleton.TrackingId].Z1 = player.skeleton.Joints[JointType.HipCenter].Position.Z - strumSize;
            strumArea[player.skeleton.TrackingId].Z2 = player.skeleton.Joints[JointType.HipCenter].Position.Z;

            /*//Smaller values go here
             strumAreaStart = new double[] { Convert.ToDouble(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton.Joints[JointType.HipCenter].Position.X) - (strumSize / 2), Convert.ToDouble(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton.Joints[JointType.HipCenter].Position.Y) - (strumSize / 2), Convert.ToDouble(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton.Joints[JointType.HipCenter].Position.Z) - strumSize };
            
            //Bigger values go here
            strumAreaEnd = new double[] { strumAreaStart[0] + (strumSize), strumAreaStart[1] + (strumSize), strumAreaStart[2] + strumSize };

            coordReadout.Content = "Z Start: " + strumAreaStart[2] + " Z End: " + strumAreaEnd[2];
             */

            SetStrumPosition(player);
        }

        private void SetStrumPosition(MainWindow.Player player)
        {
            FrameworkElement image = player.instrumentImage;

            ColorImagePoint point = MainWindow.sensor.MapSkeletonPointToColor(player.skeleton.Position, ColorImageFormat.RgbResolution640x480Fps30);

            //Grab the image reference and move it to the correct place
            Canvas.SetLeft(image, point.X - (image.Width / 2));
            Canvas.SetTop(image, point.Y - (image.Height / 2));
            /*(FrameworkElement square)
             * double posStartX = (strumAreaStart[0] * 320) + 320;
            double posStartY = 480 - ((strumAreaStart[1] * 240) + 240);

            double posEndX = (strumAreaEnd[0] * 320) + 320;
            double posEndY = 480 - ((strumAreaEnd[1] * 240) + 240);

            square.Height = Math.Abs(posEndY - posStartY);
            square.Width = Math.Abs(posEndX - posStartX);

            Canvas.SetLeft(square, posStartX);
            Canvas.SetTop(square, posEndY);*/
        }

        public void checkStrum(MainWindow.Player player, JointType joint)
        {
            if (player != null && player.skeleton.Joints[joint].TrackingState == JointTrackingState.Tracked)
            {
                //Did the player strum just then?
                double posX = player.skeleton.Joints[joint].Position.X;
                double posY = player.skeleton.Joints[joint].Position.Y;
                double posZ = player.skeleton.Joints[joint].Position.Z;

                if (strumArea[player.skeleton.TrackingId].X1 < posX && strumArea[player.skeleton.TrackingId].X2 > posX && strumArea[player.skeleton.TrackingId].Y1 < posY && strumArea[player.skeleton.TrackingId].Y2 > posY && strumArea[player.skeleton.TrackingId].Z1 < posZ && strumArea[player.skeleton.TrackingId].Z2 > posZ)
                {
                    if (!insideStrumArea[player.skeleton.TrackingId])
                    {
                        if (player.instrument == instrumentList.GuitarRight)
                        {
                            //Normal guitar stance
                            strumGuitar(checkNeckDist(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey], MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton.Joints[JointType.HandLeft]));
                        }
                        else if (player.instrument == instrumentList.GuitarLeft)
                        {
                            //Lefty stance
                            strumGuitar(checkNeckDist(MainWindow.activeSkeletons[MainWindow.primarySkeletonKey], MainWindow.activeSkeletons[MainWindow.primarySkeletonKey].skeleton.Joints[JointType.HandRight]));
                        }
                        insideStrumArea[player.skeleton.TrackingId] = true;
                    }
                }
                else
                {
                    insideStrumArea[player.skeleton.TrackingId] = false;
                }
            }
        }

        MediaPlayer mp = new MediaPlayer();

        void strumGuitar(double neckDist)
        {
            if (neckDist > 0.7)
            {
                mp.Open(new Uri("audio/guitar/guitar1.wav", UriKind.Relative));
            }
            else if (neckDist > 0.55)
            {
                mp.Open(new Uri("audio/guitar/guitar2.wav", UriKind.Relative));
            }
            else if (neckDist > 0.4)
            {
                mp.Open(new Uri("audio/guitar/guitar3.wav", UriKind.Relative));
            }
            else
            {
                mp.Open(new Uri("audio/guitar/guitar4.wav", UriKind.Relative));
            }

            mp.Play();
        }
        }

    }