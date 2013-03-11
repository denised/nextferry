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
    /// Everything we know about a ferry route.
    /// Note that a single Route actually encompasses multiple schedules:
    ///    weekend / weekday
    ///    eastbound / westbound
    /// </summary>
    public class Route : INotifyPropertyChanged
    {
        #region change notified properties
        /// <summary>
        /// True if the user has selected to see this route on the display
        /// </summary>
        public bool display
        {
            get { return _display; }
            set { if (value != _display) { _display = value; OnChanged("display"); } }
        }
        private bool _display;

        /// <summary>
        /// Departures we should display on the main page, namely:
        /// departures happening today, after now, in the direction we care about.
        /// </summary>
        public IEnumerable<DepartureTime> futureDepartures
        {
            // futureDepartures gets change notified (and hence recomputed)
            // if the schedule is changed (in setTimeList)
            // or if the eb/wb changes (in appSettingsChanged)
            // Note that we do *not* track changes to "Now" --- in theory we could,
            // but the added complexity isn't worth it.

            get
            {
                return (AppSettings.displayWB ?
                    today.timesWest.afterNow() :
                    today.timesEast.afterNow());
            }
        }

        public string displayName { get { return (AppSettings.displayWB ? wbName : ebName); }}

        #endregion

        #region non-change notified properties

        public Schedule weekday { get; private set; }
        public Schedule weekend { get; private set; }
        public Schedule special { get; private set; }

        public int routeCode { get; private set; }  // route code.  bits, may be OR'd together
        public int eastCode { get; private set; }   // code for east-most terminal
        public int westCode { get; private set; }   // code for west-most terminal

        public string wbName { get; private set; }  // name of route when traveling west
        public string ebName { get; private set; }  // name of route when traveling east

        // convenient shortcuts
        public Schedule today { get { return (special == null ? (Schedule.useWeekendSchedule() ? weekend : weekday ) : special); }}
        public Terminal departureTerminal { get { return Terminal.lookup(AppSettings.displayWB ? eastCode : westCode); }}
        public Terminal destinationTerminal { get { return Terminal.lookup(AppSettings.displayWB ? westCode : eastCode); }}

        #endregion

        #region state management

        public Route(int code, int east, int west, string wName, string eName)
        {
            routeCode = code;
            eastCode = east;
            westCode = west;
            wbName = wName;
            ebName = eName;
            clearSchedules();
            AppSettings.PropertyChanged += appSettingsChanged;
        }

        /// <summary>
        /// Accept a new schedule.  This method can be called from other threads.
        /// </summary>
        public void setTimeList(bool isSpecial, bool isWeekend, bool isWest, int[] times)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (isSpecial)
                {
                    if (special == null)
                        special = new Schedule(false, true);
                    special.setTimeList(isWest, times);
                }
                else if (isWeekend)
                    weekend.setTimeList(isWest, times);
                else
                    weekday.setTimeList(isWest, times);

                OnChanged("futureDepartures");  // noisy: will signal unnecessarily, but not really a problem.
            });
        }


        /// <summary>
        /// Remove schedules.  Used to clean up/reload.  Must be called on UI thread.
        /// </summary>
        public void clearSchedules()
        {
            weekday = new Schedule(false, false);
            weekend = new Schedule(true, false);
            special = null;
        }

        /// <summary>
        /// Recompute the visible goodness of each departure time.
        /// </summary>
        public void updateGoodness()
        {
            int now = DepartureTime.Now;
            int buffer = AppSettings.bufferTime;
            int tt = -1; // signal value for "don't use"

            if (AppSettings.useLocation && departureTerminal.hasTT)
                tt = departureTerminal.tt;

            foreach (DepartureTime d in futureDepartures)
            {
                d.computeGood(now, tt, buffer);
            }
        }

        #endregion

        #region event management

        // events we send
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnChanged(string s)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(s));
            }
        }

        // events we listen to
        public void appSettingsChanged(Object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == AppSettings.KbufferTime ||
                e.PropertyName == AppSettings.KuseLocation)
            {
                updateGoodness();
            }
            else if (e.PropertyName == AppSettings.KdisplayWB)
            {
                OnChanged("futureDepartures");
                OnChanged("displayName");
                updateGoodness();
            }
        }

        #endregion
    }
}
