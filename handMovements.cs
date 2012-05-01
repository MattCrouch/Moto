using System;
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
        public static event EventHandler<GestureEventArgs> LeftSwipeRight;

        public static bool KinectGuideGestureStatus;
        public static bool LeftGestureStatus;
        public static bool RightGestureStatus;
        public static bool LeftSwipeRightStatus;

        public static bool leftSwipeRightStarted;
        public static gesturePoint leftSwipeRightIn;
        public static gesturePoint leftSwipeRightOut;

        public static long currentTimestamp;

        /// <summary>
        /// Used with the difference engines.
        /// Provides a template to record the points.
        /// </summary>
        public struct difference3
        {
            public double X { get; set; }

            public double Y { get; set; }

            public double Z { get; set; }
        }

        /// <summary>
        /// Records a point for a point within a gesture
        /// </summary>
        public class gesturePoint
        {
            public difference3 Position { get; set; }
            public long Timestamp { get; set; }
        }

        public enum scrollDirection
        {
            None = 0,
            SmallUp,
            LargeUp,
            SmallDown,
            LargeDown
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
            int angleDrift = 15;
            double anAngle;
            bool failed;

            //Console.WriteLine(getAngle(skeleton.Joints[JointType.ShoulderRight], skeleton.Joints[JointType.HandRight]));
            //isLimbStraight(skeleton.Joints[JointType.ShoulderRight], skeleton.Joints[JointType.ElbowRight], skeleton.Joints[JointType.HandRight], 5);

            if (LeftGesture != null)
            {
                failed = true;

                //Check if left hand stretched out
                anAngle = getAngle(skeleton.Joints[JointType.ShoulderLeft].Position, skeleton.Joints[JointType.HandLeft].Position);

                if (Math.Abs(anAngle - 90) < angleDrift)
                {
                    if (isLimbStraight(skeleton.Joints[JointType.ShoulderLeft], skeleton.Joints[JointType.ElbowLeft], skeleton.Joints[JointType.HandLeft], 30))
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

                anAngle = getAngle(skeleton.Joints[JointType.ShoulderLeft].Position, skeleton.Joints[JointType.WristLeft].Position);
                //Console.WriteLine(Math.Abs(anAngle - 45));
                //'Kinect Guide' gesture
                if ((Math.Abs(anAngle - 45) < angleDrift) && (skeleton.Joints[JointType.HandLeft].Position.Y < getMidpoint(skeleton.Joints[JointType.Spine],skeleton.Joints[JointType.HipCenter]).Y) && (skeleton.Joints[JointType.HandLeft].Position.X < skeleton.Joints[JointType.Spine].Position.X))
                {
                    if (isLimbStraight(skeleton.Joints[JointType.ShoulderLeft], skeleton.Joints[JointType.ElbowLeft], skeleton.Joints[JointType.HandLeft], 30))
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
                anAngle = getAngle(skeleton.Joints[JointType.ShoulderRight].Position, skeleton.Joints[JointType.HandRight].Position);

                if (Math.Abs(anAngle - 90) < angleDrift)
                {
                    if (isLimbStraight(skeleton.Joints[JointType.ShoulderRight], skeleton.Joints[JointType.ElbowRight], skeleton.Joints[JointType.HandRight], 30))
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

            if (LeftSwipeRight != null)
            {
                if (difference[skeleton.TrackingId][JointType.HandLeft].X > 0.05)
                {
                    if (leftSwipeRightIn == null)
                    {
                        leftSwipeRightIn = new gesturePoint();

                        //Create a new start point
                        difference3 newPosition = new difference3();
                        newPosition.X = skeleton.Joints[JointType.HandLeft].Position.X;
                        newPosition.Y = skeleton.Joints[JointType.HandLeft].Position.Y;
                        newPosition.Z = skeleton.Joints[JointType.HandLeft].Position.Z;

                        leftSwipeRightIn.Position = newPosition;
                        leftSwipeRightIn.Timestamp = currentTimestamp;
                    }
                }
                else
                {
                    if (leftSwipeRightIn != null)
                    {
                        //Create a new end point, then compare
                        //Creating the end point
                        leftSwipeRightOut = new gesturePoint();

                        difference3 newPosition = new difference3();
                        newPosition.X = skeleton.Joints[JointType.HandLeft].Position.X;
                        newPosition.Y = skeleton.Joints[JointType.HandLeft].Position.Y;
                        newPosition.Z = skeleton.Joints[JointType.HandLeft].Position.Z;

                        leftSwipeRightOut.Position = newPosition;
                        leftSwipeRightOut.Timestamp = currentTimestamp;

                        //Comparison
                        if ((leftSwipeRightOut.Position.X - leftSwipeRightIn.Position.X) >= 0.2 && (leftSwipeRightOut.Timestamp - leftSwipeRightIn.Timestamp) < 4000)
                        {
                            //Fire the event
                            toggleGestureStatus(ref LeftSwipeRightStatus, LeftSwipeRight, true);
                        }

                        //Reset both values
                        leftSwipeRightIn = null;
                        leftSwipeRightOut = null;
                    }
                }



                /*if (skeleton.Joints[JointType.HandLeft].Position.X > skeleton.Joints[JointType.ElbowLeft].Position.X)
                {
                    //The hand has moved from left to right, was it fast enough for a swipe?
                    Console.WriteLine(difference[skeleton.TrackingId][JointType.HandLeft].X);
                    if (difference[skeleton.TrackingId][JointType.HandLeft].X > 0.2 && skeleton.Joints[JointType.HandLeft].TrackingState == JointTrackingState.Tracked)
                    {
                        toggleGestureStatus(ref LeftSwipeRightStatus, LeftSwipeRight, true);
                    }
                }*/
            }
        }

        /// <summary>
        /// Measures distance between two joints in a given axis
        /// </summary>
        /// <param name="joint1">A joint to measure from</param>
        /// <param name="joint2">A joint to measure to</param>
        /// <param name="axis">"X", "Y" or "Z"</param>
        /// <returns>distance in metres between joints</returns>
        public static double jointDistance(SkeletonPoint joint1, SkeletonPoint joint2, string axis)
        {
            switch (axis)
            {
                case "X":
                    return Math.Abs(joint1.X - joint2.X);
                case "Y":
                    return Math.Abs(joint1.Y - joint2.Y);
                case "Z":
                    return Math.Abs(joint1.Z - joint2.Z);
            }
            return 100;
        }

        /// <summary>
        /// Calculates whether the supplied joints are level in a given axis
        /// </summary>
        /// <param name="joint1">A point of which to measure from</param>
        /// <param name="joint2">A point of which to measure from</param>
        /// <param name="axis">"X", "Y" or "Z"</param>
        /// <param name="drift">Difference in metres between two points that could be considered 'level'</param>
        /// <returns>true if the supplied joints are level, else false</returns>
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

        /// <summary>
        /// <para>This is as the RGB camera sees - 2D</para>
        /// <para>The baseline is the y-axis - two points are vertically level with an angle of 0 and horizontally level with an angle of 90</para>
        /// </summary>
        /// <param name="joint1">A joint from which to get the angle</param>
        /// <param name="joint2">A joint to measure an angle with</param>
        /// <returns>double - size of the angle in degrees</returns>
        public static double getAngle(SkeletonPoint joint1, SkeletonPoint joint2)
        {
            
            //Returns an angle created by two points from the first point
            /*
             * NOTE: This is as the RGB camera sees - 2D
             * NOTE: Our baseline is the y-axis - two points are vertically level with an angle of 0,
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
            double angle1 = getAngle(start.Position, intermediary.Position);
            double angle2 = getAngle(start.Position, end.Position);

            if (Math.Abs(angle1 - angle2) <= drift)
            {
                return true;
            }

            return false;
        }

        public static scrollDirection sliderMenuValue(MainWindow.Player player, double angleValue)
        {
            bool upwards = false;
            if (player.skeleton.Joints[JointType.ShoulderLeft].Position.Y < player.skeleton.Joints[JointType.HandLeft].Position.Y)
            {
                upwards = true;
            }

            if (angleValue > 75 || (player.skeleton.Joints[JointType.HandLeft].Position.X > player.skeleton.Joints[JointType.HipLeft].Position.X - 0.15))
            {
                //No movement
                return scrollDirection.None;
            }
            else if (angleValue > 50)
            {
                //Small increment
                if (upwards)
                {
                    return scrollDirection.SmallUp;
                }
                else
                {
                    return scrollDirection.SmallDown;
                }
            }
            else
            {
                //Large increment
                if (upwards)
                {
                    return scrollDirection.LargeUp;
                }
                else
                {
                    return scrollDirection.LargeDown;
                }
            }
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

        /// <summary>
        /// Gives a point that lies exactly half way between the two supplied Joints
        /// </summary>
        /// <param name="joint1">A joint to measure from</param>
        /// <param name="joint2">A joint to measure to</param>
        /// <returns>A SkeletonPoint in between these joints</returns>
        public static SkeletonPoint getMidpoint(Joint joint1, Joint joint2)
        {
            SkeletonPoint newPoint = new SkeletonPoint();

            //Point in X
            if (joint1.Position.X < joint2.Position.X)
            {
                newPoint.X = joint1.Position.X + ((joint2.Position.X - joint1.Position.X) / 2);
            }
            else
            {
                newPoint.X = joint2.Position.X + ((joint1.Position.X - joint2.Position.X) / 2);
            }

            //Point in Y
            if (joint1.Position.Y < joint2.Position.Y)
            {
                newPoint.Y = joint1.Position.Y + ((joint2.Position.Y - joint1.Position.Y) / 2);
            }
            else
            {
                newPoint.Y = joint2.Position.Y + ((joint1.Position.Y - joint2.Position.Y) / 2);
            }

            //Point in Z
            if (joint1.Position.Z < joint2.Position.Z)
            {
                newPoint.Z = joint1.Position.Z + ((joint2.Position.Z - joint1.Position.Z) / 2);
            }
            else
            {
                newPoint.Z = joint2.Position.Z + ((joint1.Position.Z - joint2.Position.Z) / 2);
            }

            return newPoint;
        }
    }
}