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
        Dictionary<int, MainWindow.HitBox> triangleArea = new Dictionary<int, MainWindow.HitBox>();
        Dictionary<int, bool> insideTriangleArea = new Dictionary<int, bool>();

        internal void setupTriangle(MainWindow.Player player)
        {
            //Setting up guitar variables/runtimes
            if (!triangleArea.ContainsKey(player.skeleton.TrackingId))
            {
                triangleArea.Add(player.skeleton.TrackingId, new MainWindow.HitBox());
            }

            if (!insideTriangleArea.ContainsKey(player.skeleton.TrackingId))
            {
                insideTriangleArea.Add(player.skeleton.TrackingId, false);
            }
        }

        internal void defineTriangleArea(MainWindow.Player player)
        {
            double triangleSize = 0.2; //Size of triangle edges (in metres)

            triangleArea[player.skeleton.TrackingId].X1 = player.skeleton.Joints[JointType.HandLeft].Position.X;
            triangleArea[player.skeleton.TrackingId].X2 = player.skeleton.Joints[JointType.HandLeft].Position.X + triangleSize;
            triangleArea[player.skeleton.TrackingId].Y1 = player.skeleton.Joints[JointType.HandLeft].Position.Y - triangleSize;
            triangleArea[player.skeleton.TrackingId].Y2 = player.skeleton.Joints[JointType.HandLeft].Position.Y;
            triangleArea[player.skeleton.TrackingId].Z1 = player.skeleton.Joints[JointType.HandLeft].Position.Z - (triangleSize * 2);
            triangleArea[player.skeleton.TrackingId].Z2 = player.skeleton.Joints[JointType.HandLeft].Position.Z + (triangleSize * 2);

            SetTrianglePosition(player);
        }

        private void SetTrianglePosition(MainWindow.Player player)
        {
            FrameworkElement image = player.instrumentImage;

            image.Width = scaledWidth(player.skeleton.Joints[JointType.HandLeft].Position, player.instrument);

            ColorImagePoint point = MainWindow.sensor.MapSkeletonPointToColor(player.skeleton.Joints[JointType.HandLeft].Position, ColorImageFormat.RgbResolution640x480Fps30);

            
            //Grab the image reference and move it to the correct place
            Canvas.SetLeft(image, point.X - (image.Width / 2));
            Canvas.SetTop(image, point.Y);

            if (currentFocus == playerFocus.None || currentFocus == playerFocus.Picture || currentFocus == playerFocus.Metronome)
            {
                if (player.skeleton.Joints[JointType.HandLeft].TrackingState == JointTrackingState.Inferred && (Math.Abs(player.skeleton.Joints[JointType.HandLeft].Position.X) - Math.Abs(player.skeleton.Joints[JointType.HipCenter].Position.X) < 0.30 && player.skeleton.Joints[JointType.HandLeft].Position.Z > player.skeleton.Joints[JointType.HipCenter].Position.Z))
                {
                    if (image.Visibility != System.Windows.Visibility.Hidden)
                    {
                        image.Visibility = System.Windows.Visibility.Hidden;
                    }
                }
                else
                {
                    if (image.Visibility != System.Windows.Visibility.Visible)
                    {
                        image.Visibility = System.Windows.Visibility.Visible;
                    }
                }
            }
        }

        public void checkTriangle(MainWindow.Player player, JointType joint)
        {
            if (player != null && player.skeleton.Joints[joint].TrackingState == JointTrackingState.Tracked)
            {
                //Did the player just ding the triangle?
                double posX = player.skeleton.Joints[joint].Position.X;
                double posY = player.skeleton.Joints[joint].Position.Y;
                double posZ = player.skeleton.Joints[joint].Position.Z;

                if (triangleArea[player.skeleton.TrackingId].X1 < posX && triangleArea[player.skeleton.TrackingId].X2 > posX && triangleArea[player.skeleton.TrackingId].Y1 < posY && triangleArea[player.skeleton.TrackingId].Y2 > posY && triangleArea[player.skeleton.TrackingId].Z1 < posZ && triangleArea[player.skeleton.TrackingId].Z2 > posZ)
                {
                    if (!insideTriangleArea[player.skeleton.TrackingId])
                    {
                        hitTriangle();
                        insideTriangleArea[player.skeleton.TrackingId] = true;
                    }
                }
                else
                {
                    insideTriangleArea[player.skeleton.TrackingId] = false;
                }
            }
        }

        void hitTriangle()
        {
            if (mpDictionary[(mpCounter % mpDictionary.Count)] == null)
            {
                mpDictionary[(mpCounter % mpDictionary.Count)] = new MediaPlayer();
            }

            mpDictionary[(mpCounter % mpDictionary.Count)].Open(new Uri("audio/triangle/triangle.wav", UriKind.Relative));

            mpDictionary[(mpCounter % mpDictionary.Count)].Play();

            mpDictionary[(mpCounter % mpDictionary.Count)].MediaEnded += new EventHandler(instruments_MediaEnded);

            mpCounter++;
        }
    }

}