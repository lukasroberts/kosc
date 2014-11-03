using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KOSC
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Kinect;

    public enum SkeletonChooserMode
    {
        /// <summary>
        /// Use system default tracking
        /// </summary>
        DefaultSystemTracking,

        /// <summary>
        /// Track the player nearest to the sensor
        /// </summary>
        Closest1Player,

        /// <summary>
        /// Track the two players nearest to the sensor
        /// </summary>
        Closest2Player,

        /// <summary>
        /// Track one player based on id
        /// </summary>
        Sticky1Player,

        /// <summary>
        /// Track two players based on id
        /// </summary>
        Sticky2Player,

        /// <summary>
        /// Track one player based on most activity
        /// </summary>
        MostActive1Player,

        /// <summary>
        /// Track two players based on most activity
        /// </summary>
        MostActive2Player
    }

    /// <summary>
    /// KinectSkeletonChooser class is a lookless control that will select skeletons based on a specified heuristic.
    /// It contains logic to track players over multiple frames and use this data to select based on distance, activity level or other methods.
    /// It is intended that if you use this control, no other code will manage the skeleton tracking state on the Kinect Sensor,
    /// as they will collide in unpredictable ways.
    /// </summary>
    public class KinectSkeletonChooser
    {


        private readonly List<ActivityWatcher> recentActivity = new List<ActivityWatcher>();
        private readonly List<int> activeList = new List<int>();
        private SkeletonChooserMode skelEnum = new SkeletonChooserMode();
        private SkeletonStreamRenderer skelRenderer;

        public SkeletonChooserMode SkeletonChooserMode
        {
            get { return skelEnum; }
            set { skelEnum = value; }
        }

        public KinectSkeletonChooser(SkeletonStreamRenderer sr)
        {
            this.skelRenderer = sr;
            this.EnsureSkeletonStreamState();
        }

        private void EnsureSkeletonStreamState()
        {
            //ensure that our application is allowed to change the skeleton selection methods
            //by activating these two options on the skeletal stream
            if ((null != this.skelRenderer.Chooser) && (null != this.skelRenderer.Chooser.Sensor))
            {
                this.skelRenderer.Chooser.Sensor.SkeletonStream.AppChoosesSkeletons = true;
                this.skelRenderer.Chooser.Sensor.SkeletonStream.EnableTrackingInNearRange = true;
            }
        }

        public void ChooseTrackedSkeletons(IEnumerable<Skeleton> skeletonDataValue)
        {
            switch (skelEnum)
            {
                case SkeletonChooserMode.Closest1Player:
                    this.ChooseClosestSkeletons(skeletonDataValue, 1);
                    break;
                case SkeletonChooserMode.Closest2Player:
                    this.ChooseClosestSkeletons(skeletonDataValue, 2);
                    break;
                case SkeletonChooserMode.Sticky1Player:
                    this.ChooseOldestSkeletons(skeletonDataValue, 1);
                    break;
                case SkeletonChooserMode.Sticky2Player:
                    this.ChooseOldestSkeletons(skeletonDataValue, 2);
                    break;
                case SkeletonChooserMode.MostActive1Player:
                    this.ChooseMostActiveSkeletons(skeletonDataValue, 1);
                    break;
                case SkeletonChooserMode.MostActive2Player:
                    this.ChooseMostActiveSkeletons(skeletonDataValue, 2);
                    break;
            }
        }

        private void ChooseClosestSkeletons(IEnumerable<Skeleton> skeletonDataValue, int count)
        {
            SortedList<float, int> depthSorted = new SortedList<float, int>();

            foreach (Skeleton s in skeletonDataValue)
            {
                if (s.TrackingState != SkeletonTrackingState.NotTracked)
                {
                    float valueZ = s.Position.Z;
                    while (depthSorted.ContainsKey(valueZ))
                    {
                        valueZ += 0.0001f;
                    }

                    depthSorted.Add(valueZ, s.TrackingId);
                }
            }

            this.ChooseSkeletonsFromList(depthSorted.Values, count);
        }

        private void ChooseOldestSkeletons(IEnumerable<Skeleton> skeletonDataValue, int count)
        {
            List<int> newList = (from s in skeletonDataValue where s.TrackingState != SkeletonTrackingState.NotTracked select s.TrackingId).ToList();

            // Remove all elements from the active list that are not currently present
            this.activeList.RemoveAll(k => !newList.Contains(k));

            // Add all elements that aren't already in the activeList
            this.activeList.AddRange(newList.FindAll(k => !this.activeList.Contains(k)));

            this.ChooseSkeletonsFromList(this.activeList, count);
        }

        private void ChooseMostActiveSkeletons(IEnumerable<Skeleton> skeletonDataValue, int count)
        {
            foreach (ActivityWatcher watcher in this.recentActivity)
            {
                watcher.NewPass();
            }

            foreach (Skeleton s in skeletonDataValue)
            {
                if (s.TrackingState != SkeletonTrackingState.NotTracked)
                {
                    ActivityWatcher watcher = this.recentActivity.Find(w => w.TrackingId == s.TrackingId);
                    if (watcher != null)
                    {
                        watcher.Update(s);
                    }
                    else
                    {
                        this.recentActivity.Add(new ActivityWatcher(s));
                    }
                }
            }

            // Remove any skeletons that are gone
            this.recentActivity.RemoveAll(aw => !aw.Updated);

            this.recentActivity.Sort();
            this.ChooseSkeletonsFromList(this.recentActivity.ConvertAll(f => f.TrackingId), count);
        }

        private void ChooseSkeletonsFromList(IList<int> list, int max)
        {
            if (this.skelRenderer.Chooser.Sensor.SkeletonStream.IsEnabled)
            {
                int argCount = Math.Min(list.Count, max);

                if (argCount == 0)
                {
                    this.skelRenderer.Chooser.Sensor.SkeletonStream.ChooseSkeletons();
                }

                if (argCount == 1)
                {
                    this.skelRenderer.Chooser.Sensor.SkeletonStream.ChooseSkeletons(list[0]);
                }

                if (argCount >= 2)
                {
                    this.skelRenderer.Chooser.Sensor.SkeletonStream.ChooseSkeletons(list[0], list[1]);
                }
            }
        }

        /// <summary>
        /// Private class used to track the activity of a given player over time, which can be used to assist the KinectSkeletonChooser 
        /// when determing which player to track.
        /// </summary>
        private class ActivityWatcher : IComparable<ActivityWatcher>
        {
            private const float ActivityFalloff = 0.98f;
            private float activityLevel;
            private SkeletonPoint previousPosition;
            private SkeletonPoint previousDelta;

            internal ActivityWatcher(Skeleton s)
            {
                this.activityLevel = 0.0f;
                this.TrackingId = s.TrackingId;
                this.Updated = true;
                this.previousPosition = s.Position;
                this.previousDelta = new SkeletonPoint();
            }

            internal int TrackingId { get; private set; }

            internal bool Updated { get; private set; }

            public int CompareTo(ActivityWatcher other)
            {
                if (null == other)
                {
                    return -1;
                }

                // Use the existing CompareTo on float, but reverse the arguments,
                // since we wish to have larger activityLevels sort ahead of smaller values.
                return other.activityLevel.CompareTo(this.activityLevel);
            }

            internal void NewPass()
            {
                this.Updated = false;
            }

            internal void Update(Skeleton s)
            {
                SkeletonPoint newPosition = s.Position;
                SkeletonPoint newDelta = new SkeletonPoint
                {
                    X = newPosition.X - this.previousPosition.X,
                    Y = newPosition.Y - this.previousPosition.Y,
                    Z = newPosition.Z - this.previousPosition.Z
                };

                SkeletonPoint deltaV = new SkeletonPoint
                {
                    X = newDelta.X - this.previousDelta.X,
                    Y = newDelta.Y - this.previousDelta.Y,
                    Z = newDelta.Z - this.previousDelta.Z
                };

                this.previousPosition = newPosition;
                this.previousDelta = newDelta;

                float deltaVLengthSquared = (deltaV.X * deltaV.X) + (deltaV.Y * deltaV.Y) + (deltaV.Z * deltaV.Z);
                float deltaVLength = (float)Math.Sqrt(deltaVLengthSquared);

                this.activityLevel = this.activityLevel * ActivityFalloff;
                this.activityLevel += deltaVLength;

                this.Updated = true;
            }
        }
    }
}
