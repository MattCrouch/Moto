﻿using System;
using System.Collections.Generic;
using Microsoft.Kinect;
using System.Windows.Threading;

namespace Moto
{
    public partial class handMovements
    {

        //static Dictionary<string, Dictionary<string, double>> jointPosition = new Dictionary<string, Dictionary<string, double>>();
        public static Dictionary<int, Dictionary<JointType, SkeletonPoint>> jointPosition = new Dictionary<int, Dictionary<JointType, SkeletonPoint>>();

        //public static Dictionary<Joint, Dictionary<string, double>> difference = new Dictionary<Joint, Dictionary<string, double>>();
        public static Dictionary<int, Dictionary<JointType, difference3>> difference = new Dictionary<int, Dictionary<JointType, difference3>>();

        public enum UserDecisions
        {
            Triggered,
            NotTriggered,
        }

        public class GestureEventArgs : EventArgs
        {
            public UserDecisions Trigger { get; set; }
        }

        public static event EventHandler<GestureEventArgs> KinectGuideGesture;
        public static event EventHandler<GestureEventArgs> LeftGesture;
        public static event EventHandler<GestureEventArgs> RightGesture;

        public static bool KinectGuideGestureStatus;
        public static bool LeftGestureStatus;
        public static bool RightGestureStatus;

        public struct difference3
        {
            //Structure of the difference between two points
            //Used for tracking joint progression

            public double X { get; set; }

            public double Y { get; set; }

            public double Z { get; set; }
        }

        public static void trackJointProgression(Skeleton skeleton, Joint joint)
        {
            //Pick up the position of provided joint
            /*Dictionary<string, double> newPos = new Dictionary<string, double>();
            newPos.Add("X", joint.Position.X);
            newPos.Add("Y", joint.Position.Y);
            newPos.Add("Z", joint.Position.Z);*/

            if (!jointPosition.ContainsKey(skeleton.TrackingId)) {
                jointPosition.Add(skeleton.TrackingId, new Dictionary<JointType,SkeletonPoint>());
            }

            if (!jointPosition[skeleton.TrackingId].ContainsKey(joint.JointType))
            {
                //We don't have a record of this joint so far, so make one
                jointPosition[skeleton.TrackingId].Add(joint.JointType, new SkeletonPoint());

                if (!difference.ContainsKey(skeleton.TrackingId)) {
                    difference.Add(skeleton.TrackingId, new Dictionary<JointType, difference3>());
                }
                difference[skeleton.TrackingId].Add(joint.JointType, new difference3());

                //We can't do any more calculation. Stop now.
                return;
            }
            else
            {
                difference3 theDifference = new difference3();
                theDifference.X = joint.Position.X - jointPosition[skeleton.TrackingId][joint.JointType].X;
                theDifference.Y = joint.Position.Y - jointPosition[skeleton.TrackingId][joint.JointType].Y;
                theDifference.Z = joint.Position.Z - jointPosition[skeleton.TrackingId][joint.JointType].Z;

                difference[skeleton.TrackingId][joint.JointType] = theDifference;

                jointPosition[skeleton.TrackingId][joint.JointType] = joint.Position;
            }
        }

        private double speedOfJoint(Double newPos, Double oldPos)
        {
            return Math.Abs(newPos - oldPos);
        }

        public static void listenForGestures(Skeleton skeleton)
        {
            int angleDrift = 10;
            double anAngle;
            bool failed;

            //Console.WriteLine(getAngle(skeleton.Joints[JointType.ShoulderRight], skeleton.Joints[JointType.HandRight]));
            //isLimbStraight(skeleton.Joints[JointType.ShoulderRight], skeleton.Joints[JointType.ElbowRight], skeleton.Joints[JointType.HandRight], 5);

            if (LeftGesture != null)
            {
                failed = true;

                //Check if left hand stretched out
                anAngle = getAngle(skeleton.Joints[JointType.ShoulderLeft], skeleton.Joints[JointType.HandLeft]);

                if (Math.Abs(anAngle - 90) < angleDrift)
                {
                    if (isLimbStraight(skeleton.Joints[JointType.ShoulderLeft], skeleton.Joints[JointType.ElbowLeft], skeleton.Joints[JointType.HandLeft], angleDrift))
                    {
                        failed = false;
                        if (!LeftGestureStatus)
                        {
                            toggleGestureStatus(ref LeftGestureStatus, LeftGesture, true);
                        }
                    }
                }

                if (failed)
                {
                    toggleGestureStatus(ref LeftGestureStatus, LeftGesture, false);
                }
            }

            if(KinectGuideGesture != null)
            {

                failed = true;

                anAngle = getAngle(skeleton.Joints[JointType.ShoulderLeft], skeleton.Joints[JointType.HandLeft]);
                //Console.WriteLine(anAngle);
                //'Kinect Guide' gesture
                if ((Math.Abs(anAngle - 45) < angleDrift) && (skeleton.Joints[JointType.HandLeft].Position.Y < skeleton.Position.Y))
                {
                    if (isLimbStraight(skeleton.Joints[JointType.ShoulderLeft], skeleton.Joints[JointType.ElbowLeft], skeleton.Joints[JointType.HandLeft], angleDrift))
                    {
                        failed = false;
                        if (!KinectGuideGestureStatus)
                        {
                            toggleGestureStatus(ref KinectGuideGestureStatus, KinectGuideGesture, true);
                        }
                    }
                }

                if (failed)
                {
                    toggleGestureStatus(ref KinectGuideGestureStatus, KinectGuideGesture, false);
                }
            }

            failed = true;

            if (RightGesture != null)
            {
                //Check if right hand stretched out
                anAngle = getAngle(skeleton.Joints[JointType.ShoulderRight], skeleton.Joints[JointType.HandRight]);

                if (Math.Abs(anAngle - 90) < angleDrift)
                {
                    if (isLimbStraight(skeleton.Joints[JointType.ShoulderRight], skeleton.Joints[JointType.ElbowRight], skeleton.Joints[JointType.HandRight], angleDrift))
                    {
                        failed = false;
                        if (!RightGestureStatus)
                        {
                            toggleGestureStatus(ref RightGestureStatus, RightGesture, true);
                        }
                    }
                }

                if (failed)
                {
                    toggleGestureStatus(ref RightGestureStatus, RightGesture, false);
                }
            }
        }

        static void KinectGuideTimer_Tick(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public static double jointDistance(Joint joint1, Joint joint2, string axis)
        {
            switch (axis)
            {
                case "X":
                    return Math.Abs(joint1.Position.X - joint2.Position.X);
                case "Y":
                    return Math.Abs(joint1.Position.Y - joint2.Position.Y);
                case "Z":
                    return Math.Abs(joint1.Position.Z - joint2.Position.Z);
            }
            return 100;
        }

        public static bool jointsLevel(Joint joint1, Joint joint2, string axis, double drift)
        {
            //returns true if joints in supplied axis are level (+ or - the 'drift', or 'leeway')
            switch (axis)
            {
                case "X":
                    if (Math.Abs(joint1.Position.X - joint2.Position.X) <= drift)
                    {
                        return true;
                    }
                    break;
                case "Y":
                    if (Math.Abs(joint1.Position.Y - joint2.Position.Y) <= drift)
                    {
                        return true;
                    }
                    break;
                case "Z":
                    if (Math.Abs(joint1.Position.Z - joint2.Position.Z) <= drift)
                    {
                        return true;
                    }
                    break;
            }
            return false;
        }

        public static double getAngle(Joint joint1, Joint joint2)
        {
            //Returns an angle created by two points from the first point
            /*
             * NOTE: This is as the RGB camera sees - 2D
             * NOTE: Our baseline is the x-axis - two points are vertically level with an angle of 0,
             *       and horizontally level with an angle of 90
            */

            //Grab the side lengths
            double xLength = jointDistance(joint1, joint2, "X");
            double yLength = jointDistance(joint1, joint2, "Y");

            //Calculate theta
            double theta = xLength / yLength;

            //Convert result to degrees
            double result = Math.Atan(theta) * (180.0 / Math.PI);

            return result;
        }

        public static bool isLimbStraight(Joint start, Joint intermediary, Joint end, int drift = 0)
        {
            //Returns true when the three provided joints are in alignment
            /*
             * NOTE: This is as the RGB camera sees - 2D
             * NOTE: The 'intermediary' variable is either an elbow for the arm, or knee for leg
             */
            double angle1 = getAngle(start, intermediary);
            double angle2 = getAngle(start, end);

            if (Math.Abs(angle1 - angle2) <= drift)
            {
                return true;
            }

            return false;
        }

        public static void toggleGestureStatus(ref bool flag, EventHandler<GestureEventArgs> theEvent, bool on)
        {
            //Takes the boolean flag of the gesture status, flicks it to 'on' value and fires the appropriate event
            if (flag != on)
            {
                flag = on;

                if (flag)
                {
                    theEvent(null, new GestureEventArgs { Trigger = UserDecisions.Triggered });
                }
                else
                {
                    theEvent(null, new GestureEventArgs { Trigger = UserDecisions.NotTriggered });
                }
            }
        }
    }
}