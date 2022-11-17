using System;
using System.Linq;
using System.Collections.Generic;
using osu.Game.Rulesets.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm.Data
{
    /// <summary>
    /// A flat pattern is defined as a sequence of hit objects that has effectively no variation in rhythm, i.e. all
    /// hit object within will have rhythm ratios of almost 1, with the exception of the first two hit objects.
    /// </summary>
    public class EvenHitObjects : EvenRhythm<TaikoDifficultyHitObject>, IHasInterval
    {
        public TaikoDifficultyHitObject FirstHitObject => Children.First();

        /// <summary>
        /// Start time of the first hit object.
        /// </summary>
        public double StartTime => Children.First().StartTime;

        /// <summary>
        /// The interval between the first and last hit object
        /// </summary>
        public double Duration => Children.Last().StartTime - Children.First().StartTime;

        public EvenHitObjects? Previous;

        /// <summary>
        /// The <see cref="TaikoDifficultyHitObjectRhythm.Ratio"/> of the first hit object in this pattern.
        /// </summary>
        public double Ratio => Children[0].Rhythm.Ratio;

        /// <summary>
        /// The interval in ms of each hit object in this <see cref="EvenHitObjects"/>. This is only defined if there is
        /// more than two hit objects in this <see cref="EvenHitObjects"/>.
        /// </summary>
        public double? HitObjectInterval = null;

        /// <summary>
        /// The ratio of <see cref="HitObjectInterval"/> between this and the previous <see cref="EvenHitObjects"/>. In the
        /// case where one or both of the <see cref="HitObjectInterval"/> is undefined, this will have a value of 1.
        /// </summary>
        public double HitObjectIntervalRatio = 1;

        /// <summary>
        /// The interval between the <see cref="StartTime"/> of this and the previous <see cref="EvenHitObjects"/>.
        /// </summary>
        public double StartTimeInterval = double.PositiveInfinity;

        public double Interval => StartTimeInterval;

        /// <summary>
        /// The index of this <see cref="EvenHitObjects"/> in a list of evenly spaced <see cref="EvenHitObjects"/>s, defined 
        /// as having even <see cref="StartTimeInterval"/>s.
        /// </summary>
        /// TODO: Need to be named better
        public int EvenStartTimeIndex = 0;

        public EvenHitObjects(EvenHitObjects? previous, List<TaikoDifficultyHitObject> data, ref int i)
            : base(data, ref i, 3)
        {
            Previous = previous;

            foreach (var hitObject in base.Children)
            {
                hitObject.Rhythm.EvenHitObjects = this;
            }

            calculateIntervals();
        }

        public static List<EvenHitObjects> GroupHitObjects(List<TaikoDifficultyHitObject> data)
        {
            List<EvenHitObjects> flatPatterns = new List<EvenHitObjects>();

            // Index does not need to be incremented, as it is handled within EvenRhythm's constructor.
            EvenHitObjects? current = null;
            for (int i = 0; i < data.Count;)
            {
                current = new EvenHitObjects(current, data, ref i);
                flatPatterns.Add(current);
            }

            return flatPatterns;
        }

        private void calculateIntervals()
        {
            HitObjectInterval = Children.Count < 2 ? null : Children[1].StartTime - Children[0].StartTime;

            if (Previous?.HitObjectInterval != null && HitObjectInterval != null)
            {
                HitObjectIntervalRatio = HitObjectInterval.Value / Previous.HitObjectInterval.Value;
            }

            if (Previous == null)
            {
                return;
            }

            StartTimeInterval = StartTime - Previous.StartTime;
        }

        /// <summary>
        /// Two <see cref="EvenHitObjects"/>s are considered repetitions if they have the same amount of hit objects and
        /// have the same interval between the first two hit objects. Only the first two hit objects are taken into
        /// account due to <see cref="EvenHitObjects"/>s are defined as having no variation in rhythm.
        ///
        /// If there is only one hit object in the <see cref="EvenHitObjects"/>s, they are considered repetitions if their
        /// first (and only) hit objects have the same interval.
        /// </summary>
        public bool IsRepetitionOf(EvenHitObjects? other)
        {
            if (other == null || Children.Count != other.Children.Count)
                return false;

            if (Children.Count <= 1)
                return Math.Abs(Children[0].DeltaTime - other.Children[0].DeltaTime) < 3;

            return Math.Abs(Children[1].DeltaTime - other.Children[1].DeltaTime) < 3;
        }
    }
}