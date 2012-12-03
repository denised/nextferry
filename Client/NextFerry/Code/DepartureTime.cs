using System;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;


namespace NextFerry
{
    /// <summary>
    /// Convert departure times into useful things.   
    /// Note that a time, once established, never changes.
    /// What does change is how "good" a time is, and this depends on the time, the route, and the current time.
    /// (And possibly the current location, etc. as well.)
    /// So that is the only property we do change notification on.
    /// </summary>
    public class DepartureTime : INotifyPropertyChanged
    {
        #region properties

        /// <summary>
        /// The time of departure, in minutes past midnight.
        /// </summary>
        public int value { get; private set; }

        /// <summary>
        /// The current time, in minutes past midnight, corrected for "WSDOT time" (see comments under Departures).
        /// </summary>
        public static int Now 
        { 
            get 
            { 
                int x = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
                if (x < Departures.MorningCutoff) // see comments at Departures for why we do this.
                    x += 24 * 60;
                return x;
            } 
        }

        #region goodness
        public enum GoodnessValue { TooLate, Risky, Good, Unknown, Indifferent };
        public const GoodnessValue TooLate = GoodnessValue.TooLate;
        public const GoodnessValue Risky = GoodnessValue.Risky;
        public const GoodnessValue Good = GoodnessValue.Good;
        public const GoodnessValue Indifferent = GoodnessValue.Indifferent; // unreachable, or too far in the future to care
        public const GoodnessValue Unknown = GoodnessValue.Unknown;

        private GoodnessValue _goodness = 0;
        public GoodnessValue goodness
        {
            get { return _goodness; }
            set
            {
                if (value != _goodness)
                {
                    _goodness = value;
                    OnChanged("goodness");
                    OnChanged("useStyle");
                }
            }
        }
        #endregion


        #region string conversion
        private string _display12 = null;
        public string display12
        {
            get
            {
                if (_display12 == null)
                {
                    int hours = value / 60;
                    int minutes = value % 60;
                    // convert hour to 12 hour format, and correct for "over 24" times
                    if (hours > 24)
                        hours -= 24;
                    if (hours > 12)
                        hours -= 12;
                    if (hours == 0) hours = 12;
                    _display12 = String.Format("{0}:{1,2:00}", hours, minutes);
                }
                return _display12;
            }
        }

        private string _display24 = null;
        public string display24
        {
            get
            {
                if (_display24 == null)
                {
                    int hours = value / 60;
                    int minutes = value % 60;
                    if (hours > 24)
                        hours -= 24;
                    _display24 = String.Format("{0,2:00}:{1,2:00}", hours, minutes);
                }
                return _display24;
            }
        }
        #endregion
        #endregion

        #region infrastructure

        public DepartureTime(int v)
        {
            value = v;
            goodness = Unknown;
        }

        public override string ToString()
        {
            return (AppSettings.display12hr ? display12 : display24);
        }


        public event PropertyChangedEventHandler PropertyChanged;
        public void OnChanged(string s)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(s));
            }
        }
        #endregion

        #region style handling
        // this should just be a dictionary, and we should have a better way of initializing it.
        public static Style tooLateStyle;
        public static Style riskyStyle;
        public static Style goodStyle;
        public static Style defaultStyle;

        public Style useStyle
        {
            get
            {
                switch (goodness)
                {
                    case TooLate: return tooLateStyle;
                    case Risky: return riskyStyle;
                    case Good: return goodStyle;
                    default: return defaultStyle;
                }
            }
        }
        #endregion

        #region goodness computation
        /// How and when goodness is computed, and what happens in various cases:
        /// 
        /// Rule 1: "tt + wait" is travel time plus whatever buffer time the user has requested.
        /// Rule 2: There are five possible states:
        ///         Too late:  Now + 0.95(tt + wait) is after  departure time
        ///            (Translation: even if you drive too fast, you will be too late.)
        ///         Risky:  not too late   AND   Now + (tt + wait)  is after  (departure time - 5min)
        ///             (Translation: reasonable travel and wait estimates don't get you there with at least 5 min to spare)
        ///         Good:  neither of the above  AND this is within a couple of hours from now
        ///         Indifferent: none of the above  **OR** terminal is out of range.
        ///         Unknown: we cannot get server data, so we don't have up-to-date info on travel time or range.
        ///
        ///              (NB: Currently, there's no effective difference between Indifferent and Unknown, but we maintain 
        ///               them because they are semantically distinct.)
        /// 
        /// We ask the server for updated tt information periodically (every 5 minutes).
        /// We do not update the goodness value inbetween server responses (that is, no extrapolation), except as follows:
        /// 
        /// Rule 3: If the tt data is more than 5 minutes old, we issue a warning that goodness data may be out of date.
        /// Rule 4: If the tt data is more than 10 minutes old, all goodness values are converted to Unknown.
        ///
        /// Since we get the server data of travel times for all terminals (and hence all departure times) at once,
        /// Rules 3 and 4 are implemented globally (TBD: where?)   Rules 1 and 2 are computed here.


        /// <summary>
        /// Compute the goodness of this departure time.
        /// </summary>
        /// <param name="now">Current time.</param>
        /// <param name="tt">Travel time to terminal, or -1 for unknown.</param>
        /// <param name="buffer">How much ahead of time to arrive.</param>
        public void computeGood(int now, int tt, int buffer)
        {
            if (tt == -1)
                goodness = Unknown;
            else if (now + 0.95 * (tt + buffer) > this.value)
                goodness = TooLate;
            else if (now + tt + buffer + 5 > this.value)
                goodness = Risky;
            else if (now + tt + buffer + 120 > this.value)
                goodness = Good;
            else
                goodness = Indifferent;
        }

        #endregion
    }


    /// <summary>
    /// Management of sequences of departure times.
    /// </summary>
    public static class Departures
    {
        // There's this weirdness with WSDOT numbers that they put the just-after-midnight times at the 
        // end of the list.   But that is actually really useful, so we maintain the behavior.
        // Which means our "AM" and "PM" are useful but not entirely accurate.
        // This has a useful side effect that elsewhere in the code we can add times (like t + 120 to add two hours)
        // and not have to check whether the time passed midnight --- "it just works"
        // (but I'm sure it will be the source of some hideous bug someday...)

        private const int Noon = 12 * 60;
        internal const int MorningCutoff = 150;  // 2:30 am: the real "break" between days

        // NB:  Before is '<'
        //      After is '>='

        public static IEnumerable<DepartureTime> before(IEnumerable<DepartureTime> list, int target)
        {
            if (target < MorningCutoff)
                target += (24 * 60);

            foreach (DepartureTime t in list)
            {
                if (t.value < target)
                    yield return t;
                else
                    break;
            }
        }

        public static IEnumerable<DepartureTime> after(IEnumerable<DepartureTime> list, int target)
        {
            if (target < MorningCutoff)
                target += (24 * 60);

            foreach (DepartureTime t in list)
            {
                if (t.value < target)
                    continue;
                else
                    yield return t;
            }
        }

        /// <summary>
        /// Return the set of times starting at the beginning and proceeding up to no more than interval.
        /// </summary>
        public static IEnumerable<DepartureTime> between(IEnumerable<DepartureTime> list, int start, int end)
        {
            return before(after(list, start), end);
        }

        public static IEnumerable<DepartureTime> beforeNoon(IEnumerable<DepartureTime> list)
        {
            return before(list, Noon);
        }

        public static IEnumerable<DepartureTime> afterNoon(IEnumerable<DepartureTime> list)
        {
            return after(list, Noon);
        }
    }
}