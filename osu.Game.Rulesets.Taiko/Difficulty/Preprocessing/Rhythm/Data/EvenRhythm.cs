using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Difficulty.Utils;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm.Data
{
    /// <summary>
    /// A base class for grouping <see cref="IHasInterval"/>s by their interval. In edges where an interval change
    /// occurs, the <see cref="IHasInterval"/> is added to the group with the smaller interval.
    /// </summary>
    public abstract class EvenRhythm<ChildType>
        where ChildType : IHasInterval
    {
        public IReadOnlyList<ChildType> Children { get; private set; }

        private bool isFlat(ChildType current, ChildType previous, double marginOfError)
        {
            return Math.Abs(current.Interval - previous.Interval) <= marginOfError;
        }

        /// <summary>
        /// Create a new <see cref="EvenRhythm{ChildType}"/> from a list of <see cref="IHasInterval"/>s, and add 
        /// them to the <see cref="Children"/> list until the end of the group.
        /// </summary>
        ///
        /// <param name="data">The list of <see cref="IHasInterval"/>s.</param>
        ///
        /// <param name="i">
        /// Index in <paramref name="data"/> to start adding children. This will be modified and should be passed into
        /// the next <see cref="EvenRhythm{ChildType}"/>'s constructor.
        /// </param>
        ///
        /// <param name="marginOfError">
        /// The margin of error for the interval, within of which no interval change is considered to have occured.
        /// </param>
        public EvenRhythm(List<ChildType> data, ref int i, double marginOfError)
        {
            List<ChildType> children = new List<ChildType>();
            Children = children;
            children.Add(data[i]);
            i++;

            for (; i < data.Count - 1; i++)
            {
                if (!isFlat(data[i], data[i + 1], marginOfError))
                {
                    if (data[i + 1].Interval > data[i].Interval + marginOfError)
                    {
                        children.Add(data[i]);
                        i++;
                    }

                    return;
                }

                children.Add(data[i]);
            }

            // Handle final data
            if (data.Count > 2 && isFlat(data[^1], data[^2], marginOfError))
            {
                children.Add(data[i]);
                i++;
            }
        }
    }
}