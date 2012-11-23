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

        private Schedule _weekday = new Schedule();
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


        private Schedule _weekend = new Schedule();
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
            foreach (DepartureTime d in departuresToday)
            {
                d.goodness = TravelTime.goodness(d, Terminal.lookup(sourceCode));
            }
        }

        public void appSettingsChanged(Object sender, PropertyChangedEventArgs e)
        {
            if (String.Equals(e.PropertyName, AppSettings.KterminalTravelTime))
                stateRefresh();
        }

        #endregion

        #region utilities

        /// <summary>
        /// Remove schedules.  Used to clean up/reload.  Must be called on UI thread.
        /// </summary>
        public void clearSchedules()
        {
            weekday = new Schedule();
            weekend = new Schedule();
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
        public bool isWeekend { get; set; }
        public List<DepartureTime> times { get; set; }
        public DateTime expiration { get; set; }

        private static DateTime longAgo = new DateTime(1900, 1, 1);
        public Schedule()
        {
            times = new List<DepartureTime>();
            expiration = longAgo;
        }
    }


    /// <summary>
    /// Represents the end-point of a ferry route.   
    /// </summary>
    public class Terminal
    {
        public int code { get; private set; }   // codes as used in the WSDOT's web services.
        public string name { get; private set; }
        public string loc { get; private set; } // in string lat,long form

        private Terminal(int c, string n, string l)
        {
            code = c;
            name = n;
            loc = l;
        }

        public static List<Terminal> AllTerminals = new List<Terminal>
        {
            new Terminal(1, "Anacortes", "48.502220, -122.679455"),
            new Terminal(3, "Bainbridge", "47.623046, -122.511377"),
            new Terminal(4, "Bremerton", "47.564990, -122.627012"),
            new Terminal(5, "Clinton", "47.974785, -122.352139"),
            new Terminal(8, "Edmonds", "47.811240, -122.382631"),
            new Terminal(9, "Fauntleroy", "47.523115, -122.392952"),
            new Terminal(10, "Friday Harbor", "48.535010, -123.014645"),
            new Terminal(11, "Keystone", "48.160592, -122.674305"),
            new Terminal(12, "Kingston", "47.796943, -122.496785"),
            new Terminal(13, "Lopez Island", "48.570447, -122.883646"),
            new Terminal(14, "Mukilteo", "47.947758, -122.304138"),
            new Terminal(15, "Orcas Island", "48.597971, -122.943985"),
            new Terminal(16, "Point Defiance", "47.305414, -122.514123"),
            new Terminal(17, "Port Townsend", "48.112648, -122.760715"),
            new Terminal( 7, "Seattle", "47.601767, -122.336089"),
            new Terminal(18, "Shaw Island", "48.583991, -122.929351"),
            new Terminal(20, "Southworth", "47.512130, -122.500970"),
            new Terminal(21, "Tahlequah", "47.333023, -122.506999"),
            new Terminal(22, "Vashon Island", "47.508616, -122.464127")
        };


        public static Terminal lookup(int code)
        {
            foreach (Terminal t in AllTerminals)
            {
                if (t.code == code)
                    return t;
            }
            return null;
        }

        public static Terminal lookup(string name)
        {
            foreach (Terminal t in AllTerminals)
            {
                if (t.name == name)
                    return t;
            }
            return null;
        }
    }
}
