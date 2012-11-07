using System;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using Microsoft.Phone.Controls;


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

        public int value { get; private set; }

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
        public enum GoodnessValue { TooLate, TooSoon, Good, TooFar };
        public const GoodnessValue TooLate = GoodnessValue.TooLate;
        public const GoodnessValue TooSoon = GoodnessValue.TooSoon;
        public const GoodnessValue Good = GoodnessValue.Good;
        public const GoodnessValue TooFar = GoodnessValue.TooFar;

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
            goodness = ( v < Now ? TooLate : Good );
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
        public static Style tooSoonStyle;
        public static Style goodStyle;
        public static Style tooFarStyle;

        public Style useStyle
        {
            get
            {
                switch (goodness)
                {
                    case TooLate: return tooLateStyle;
                    case TooSoon: return tooSoonStyle;
                    case TooFar: return tooFarStyle;
                    default: return goodStyle;
                }
            }
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