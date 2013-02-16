using System;
using System.Net;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Threading;

namespace NextFerry
{
    /// <summary>
    /// Everything we know about ferry route.  This includes both the "static" part (identification)
    /// and the "dynamic" part (current schedule)
    /// </summary>
    public class Route : INotifyPropertyChanged
    {
        #region state
        // change notified properties
        private bool _display;
        public bool display
        {
            get { return _display; }
            set { if (value != _display) { _display = value; OnChanged("display"); } }
        }

        private Schedule _weekday = new Schedule(false);
        public Schedule weekday
        {
            get { return _weekday; }
            set {
                _weekday = value;
                OnChanged("weekday");
                if (!useWeekendScheduleToday)
                {
                    stateRefresh();
                    OnChanged("departuresToday");
                    OnChanged("recentPastDepartures");
                    OnChanged("futureDepartures");
                }
            }
        }


        private Schedule _weekend = new Schedule(true);
        public Schedule weekend
        {
            get { return _weekend; }
            set {
                _weekend = value;
                OnChanged("weekend");
                if (useWeekendScheduleToday)
                {
                    stateRefresh();
                    OnChanged("departuresToday");
                    OnChanged("recentPastDepartures");
                    OnChanged("futureDepartures");
                }
            }
        }

        /// <summary>
        /// A setter for use from other threads.  Synchronous if the wait
        /// parameter is true.   (Note that synchronous means waiting for
        /// all the changed events to be processed too, not just the actual assignment.
        /// Not ideal, but it will work.)
        /// </summary>
        public void setScheduleMT(Schedule news, bool wait)
        {
            ManualResetEvent wh = null;
            if (wait)
                wh = new ManualResetEvent(false);

            Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        if (news.isWeekend)
                            this.weekend = news;
                        else
                            this.weekday = news;
                    }
                    finally
                    {
                        if (wait)
                            wh.Set();
                    }
                });

            if (wait)
                wh.WaitOne();
        }


        // these are immutable, so we don't actually do notification
        public string direction { get; private set; }
        public string name { get; private set; }
        public int sourceCode { get; private set; }
        public int destCode { get; private set; }
        public bool useWeekendScheduleToday { get; private set; }
        // note we assume that the app does not remain open for hours on end, hence isWeekend does not change.

        public Route(string dir, string name, int src, int dest)
        {
            direction = dir;
            this.name = name;
            sourceCode = src;
            destCode = dest;
            useWeekendScheduleToday = initWeekendHoliday();
            AppSettings.PropertyChanged += appSettingsChanged;
        }

        #endregion

        #region magic properties
        // These properties are computed on demand.

        public IEnumerable<DepartureTime> departuresToday
        {
            get { return useWeekendScheduleToday ? weekend.times : weekday.times ; }
        }

        /// <summary>
        /// Return a set of the most recent departures.
        /// </summary>
        public IEnumerable<DepartureTime> recentPastDepartures
        {
            get { return Departures.between(departuresToday, DepartureTime.Now - 50, DepartureTime.Now); }
        }

        public IEnumerable<DepartureTime> futureDepartures
        {
            get { return Departures.after(departuresToday, DepartureTime.Now); }
        }

        #endregion

        #region change notification event

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnChanged(string s)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(s));
            }
        }

        #endregion

        #region state refresh
        /// <summary>
        /// Update state changes that occur due to the passage of time.   Basically means computing the "goodness"
        /// of departure times.
        /// </summary>
        public void stateRefresh()
        {
            int now = DepartureTime.Now;
            int buffer = AppSettings.bufferTime;
            int tt = (AppSettings.useLocation ? Terminal.gettt(sourceCode) : -1);

            foreach (DepartureTime d in departuresToday)
            {
                d.computeGood(now, tt, buffer);
            }
        }

        public void appSettingsChanged(Object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == AppSettings.KbufferTime ||
                e.PropertyName == AppSettings.KuseLocation )
                stateRefresh();
        }

        #endregion

        #region utilities

        /// <summary>
        /// Remove schedules.  Used to clean up/reload.  Must be called on UI thread.
        /// </summary>
        public void clearSchedules()
        {
            weekday = new Schedule(false);
            weekend = new Schedule(true);
        }
        
        /// <summary>
        /// Return the terminal on the eastern end of the route.
        /// </summary>
        public Terminal eastTerminal()
        {
            return Terminal.lookup(direction[0] == 'w' ? sourceCode : destCode);
        }


        /// <summary>
        /// Return the terminal on the eastern end of the route.
        /// </summary>
        public Terminal westTerminal()
        {
            return Terminal.lookup(direction[0] == 'w' ? destCode : sourceCode);
        }

        public Route sibling()
        {
            return RouteManager.getSibling(this);
        }

        /// <summary>
        /// Return true if the weekend schedule should be used today, i.e. today is a weekend or holiday.
        /// </summary>
        /// <returns></returns>
        private bool initWeekendHoliday()
        {
            DateTime today = DateTime.Today;
            return today.DayOfWeek == DayOfWeek.Saturday
                   || today.DayOfWeek == DayOfWeek.Sunday
                   || (Holiday.isHoliday(today) && (String.Equals(this.name, "bainbridge") ||
                                                    String.Equals(this.name, "pt defiance") ||
                                                    String.Equals(this.name, "mukilteo")));
            // yup, only some of the routes have holiday schedules.
            // I didn't know that either until I was trying to code this up...
        }
        #endregion
    }

    /// <summary>
    /// A target schedule for a specific route and weekday or weekend, valid through the expiration date.
    /// Schedules are immutable once fully initialized, so notifications happen a level up.
    /// </summary>
    public class Schedule
    {
        public bool isWeekend { get; private set; }
        public List<DepartureTime> times { get; private set; }

        public Schedule(bool isw)
        {
            isWeekend = isw;
            times = new List<DepartureTime>();
        }
    }
}
