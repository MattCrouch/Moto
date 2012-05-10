using System;
using System.Windows;
using System.Windows.Media.Imaging;
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

            if (!player.instrumentOverlay.ContainsKey(0))
            {
                player.instrumentOverlay.Add(0, new Image());
            }
            else
            {
                player.instrumentOverlay[0] = new Image();
            }

            MainCanvas.Children.Add(player.instrumentOverlay[0]);
        }

        private double doPythag(double a, double b)
        {
            double c = Math.Sqrt(Math.Pow(a, 2) + Math.Pow(b, 2));
            return c;
        }

        internal double checkNeckDist(MainWindow.Player player, JointType fretHand)
        {
            Joint hip = player.skeleton.Joints[JointType.Spine];
            Joint hand = player.skeleton.Joints[fretHand];

            double xLength = doPythag((hip.Position.Z - hand.Position.Z), (hip.Position.X - hand.Position.X));

            double neckDist = doPythag(xLength, (hip.Position.Y - hand.Position.Y));

            return neckDist;
        }
        
        internal void defineStrumArea(MainWindow.Player player) {
             double strumSize = 0.15; //Size of strum area edges (in metres)

            strumArea[player.skeleton.TrackingId].X1 = player.skeleton.Joints[JointType.Spine].Position.X - (strumSize / 2);
            strumArea[player.skeleton.TrackingId].X2 = player.skeleton.Joints[JointType.Spine].Position.X + (strumSize / 2);
            strumArea[player.skeleton.TrackingId].Y1 = player.skeleton.Joints[JointType.Spine].Position.Y - (strumSize / 2);
            strumArea[player.skeleton.TrackingId].Y2 = player.skeleton.Joints[JointType.Spine].Position.Y + (strumSize / 2);
            strumArea[player.skeleton.TrackingId].Z1 = player.skeleton.Joints[JointType.Spine].Position.Z - (strumSize * 2);
            strumArea[player.skeleton.TrackingId].Z2 = player.skeleton.Joints[JointType.Spine].Position.Z + (strumSize * 2);

            SetStrumPosition(player);
        }

        private void SetStrumPosition(MainWindow.Player player)
        {
            JointType fretHand = JointType.HandLeft;

            if (player.instrument == instrumentList.GuitarLeft)
            {
                fretHand = JointType.HandRight;
            }

            if (player.instrumentOverlay.ContainsKey(0))
            {
                player.instrumentOverlay[0].Source = guitarImage(player);
            }

            FrameworkElement image = player.instrumentImage;

            image.Height = scaledWidth(player.skeleton.Position, player.instrument);
            player.instrumentOverlay[0].Height = image.Height;

            ColorImagePoint point = MainWindow.sensor.MapSkeletonPointToColor(player.skeleton.Joints[JointType.Spine].Position, ColorImageFormat.RgbResolution640x480Fps30);

            double angle = handMovements.getAngle(player.skeleton.Joints[JointType.Spine].Position, player.skeleton.Joints[fretHand].Position);

            if (player.skeleton.Joints[fretHand].Position.Y > player.skeleton.Joints[JointType.Spine].Position.Y)
            {
                //Upper quadrant
                if (player.skeleton.Joints[fretHand].Position.X > player.skeleton.Joints[JointType.Spine].Position.X)
                {
                    
                }
                else
                {
                    angle = -angle;
                }
            }
            else
            {
                //Lower quadrant
                if (player.skeleton.Joints[fretHand].Position.X > player.skeleton.Joints[JointType.Spine].Position.X)
                {
                    angle = 180 - angle;
                }
                else
                {
                    angle += 180;
                }
            }

            //Define center point to pivot around
            double centerX = image.ActualWidth * 0.5;
            double centerY = image.ActualHeight * 0.75;

            //Grab the image reference and move it to the correct place
            Canvas.SetLeft(image, point.X - centerX);
            Canvas.SetTop(image, point.Y - centerY);

            image.RenderTransform = new RotateTransform(angle, centerX, centerY);

            //Grab the image reference and move it to the correct place
            if (player.instrumentOverlay.ContainsKey(0))
            {
                Canvas.SetLeft(player.instrumentOverlay[0], point.X - centerX);
                Canvas.SetTop(player.instrumentOverlay[0], point.Y - centerY);

                player.instrumentOverlay[0].RenderTransform = new RotateTransform(angle, centerX, centerY);
            }
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
                        if (handMovements.difference != null)
                        {
                            if (player.instrument == instrumentList.GuitarRight)
                            {
                                //Normal guitar stance
                                double strength;
                                if (handMovements.difference[player.skeleton.TrackingId][JointType.HandRight].Y > handMovements.difference[player.skeleton.TrackingId][JointType.HandRight].X)
                                {
                                    strength = Math.Abs(handMovements.difference[player.skeleton.TrackingId][JointType.HandRight].Y);
                                }
                                else
                                {
                                    strength = Math.Abs(handMovements.difference[player.skeleton.TrackingId][JointType.HandRight].X);
                                }

                                strumGuitar(checkNeckDist(player, JointType.HandLeft), player.mode);
                            }
                            else if (player.instrument == instrumentList.GuitarLeft)
                            {
                                //Lefty stance
                                double strength;
                                if (handMovements.difference[player.skeleton.TrackingId][JointType.HandLeft].Y > handMovements.difference[player.skeleton.TrackingId][JointType.HandLeft].X)
                                {
                                    strength = Math.Abs(handMovements.difference[player.skeleton.TrackingId][JointType.HandLeft].Y);
                                }
                                else
                                {
                                    strength = Math.Abs(handMovements.difference[player.skeleton.TrackingId][JointType.HandLeft].X);
                                }

                                strumGuitar(checkNeckDist(player, JointType.HandRight), player.mode);
                            }
                            insideStrumArea[player.skeleton.TrackingId] = true;
                        }
                    }
                }
                else
                {
                    insideStrumArea[player.skeleton.TrackingId] = false;
                }
            }
        }

        void strumGuitar(double neckDist, MainWindow.PlayerMode mode)
        {
            if (mpDictionary[(mpCounter % mpDictionary.Count)] == null)
            {
                mpDictionary[(mpCounter % mpDictionary.Count)] = new MediaPlayer();
            }

            if (neckDist > 0.7)
            {
                if (mode == MainWindow.PlayerMode.Acoustic)
                {
                    mpDictionary[(mpCounter % mpDictionary.Count)].Open(new Uri("audio/guitar/acoustic/hard/guitar1.wav", UriKind.Relative));
                }
                else
                {
                    mpDictionary[(mpCounter % mpDictionary.Count)].Open(new Uri("audio/guitar/electric/hard/guitar1.wav", UriKind.Relative));
                }
            }
            else if (neckDist > 0.55)
            {
                if (mode == MainWindow.PlayerMode.Acoustic)
                {
                    mpDictionary[(mpCounter % mpDictionary.Count)].Open(new Uri("audio/guitar/acoustic/hard/guitar2.wav", UriKind.Relative));
                }
                else
                {
                    mpDictionary[(mpCounter % mpDictionary.Count)].Open(new Uri("audio/guitar/electric/hard/guitar2.wav", UriKind.Relative));
                }
            }
            else if (neckDist > 0.4)
            {
                if (mode == MainWindow.PlayerMode.Acoustic)
                {
                    mpDictionary[(mpCounter % mpDictionary.Count)].Open(new Uri("audio/guitar/acoustic/hard/guitar3.wav", UriKind.Relative));
                }
                else
                {
                    mpDictionary[(mpCounter % mpDictionary.Count)].Open(new Uri("audio/guitar/electric/hard/guitar3.wav", UriKind.Relative));
                }
            }
            else
            {
                if (mode == MainWindow.PlayerMode.Acoustic)
                {
                    mpDictionary[(mpCounter % mpDictionary.Count)].Open(new Uri("audio/guitar/acoustic/hard/guitar4.wav", UriKind.Relative));
                }
                else
                {
                    mpDictionary[(mpCounter % mpDictionary.Count)].Open(new Uri("audio/guitar/electric/hard/guitar4.wav", UriKind.Relative));
                }
            }

            mpDictionary[(mpCounter % mpDictionary.Count)].Play();

            mpDictionary[(mpCounter % mpDictionary.Count)].MediaEnded += new EventHandler(instruments_MediaEnded);

            mpCounter++;
        }

        private BitmapImage guitarImage(MainWindow.Player player)
        {
            JointType fretHand = JointType.HandLeft;

            if (player.instrument == instrumentList.GuitarLeft)
            {
                fretHand = JointType.HandRight;
            }

            double neckDist = checkNeckDist(player, fretHand);

            if (neckDist > 0.7)
            {
                return new BitmapImage(new Uri("images/guitar-overlays/green.png", UriKind.Relative));
            }
            else if (neckDist > 0.55)
            {
                return new BitmapImage(new Uri("images/guitar-overlays/red.png", UriKind.Relative));
            }
            else if (neckDist > 0.4)
            {
                return new BitmapImage(new Uri("images/guitar-overlays/yellow.png", UriKind.Relative));
            }
            else
            {
                return new BitmapImage(new Uri("images/guitar-overlays/blue.png", UriKind.Relative));
            }
        }
    }

}