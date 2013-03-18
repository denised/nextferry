using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Phone.Controls;
using System.Windows.Navigation;
using System.ComponentModel;
using System.Windows;


namespace NextFerry
{
    public partial class RoutePage : PhoneApplicationPage
    {
        private Route r { get; set; }

        // RoutePage is the landing page for a pseudo-pivot composed of multiple pages.
        // In addition to holding the main pivot (Schedules), we also set up the other page(s)
        // (Currently just alerts; in the future maybe the terminal cams will get a page too.)

        public RoutePage()
        {
            InitializeComponent();
        }

        #region setup
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            r = findRoute(this,r);
            r.initDisplayStrings();
            DataContext = r;
            manageBackPointer(this);
            pageTitle.Text = r.eastTerminal.name + " / " + r.westTerminal.name;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            State["route"] = r.wbName;
        }

        public static Route findRoute(PhoneApplicationPage me, Route r)
        {
            if (r != null)  // we already have it.
                return r;
            
            // This is either a new page, or we're being restored from tombstoning.
            // If we tombstoned, the route should be in State
            // If this is a new page, the route name is in the URL.
            String routeName = null;
            if (me.State.ContainsKey("route"))
            {
                routeName = (string)me.State["route"];
            }
            else
            {
                me.NavigationContext.QueryString.TryGetValue("route", out routeName);
            }
            return RouteManager.lookup(routeName);
        }
        #endregion

        #region pseudo-pivot
        /// <summary>
        /// Enable the user to see the "back button" go back to wherever previously
        /// navigated from (not one of the pivots).
        /// </summary>
        public static void manageBackPointer(PhoneApplicationPage me)
        {
            if (me.NavigationService.CanGoBack)
            {
                // if we got here from one of our peers, pop that off the back stack.
                JournalEntry j = me.NavigationService.BackStack.First();
                if (j.Source.OriginalString.Contains("RoutePage") ||
                    j.Source.OriginalString.Contains("RouteAlerts"))
                    me.NavigationService.RemoveBackEntry();
            }
        }

        private void gotoAlerts(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (r.hasAlerts)
            {
                string urlWithData = string.Format("/RouteAlerts.xaml?route={0}", r.wbName);
                NavigationService.Navigate(new Uri(urlWithData, UriKind.Relative));
            }
        }
        #endregion
    }

    #region schedule content management

    public partial class Route : INotifyPropertyChanged
    {
        // Cached string versions of the departuretimes.
        public string ds_wbwdam { get; private set; }
        public string ds_wbwdpm { get; private set; }
        public string ds_wbweam { get; private set; }
        public string ds_wbwepm { get; private set; }
        public string ds_ebwdam { get; private set; }
        public string ds_ebwdpm { get; private set; }
        public string ds_ebweam { get; private set; }
        public string ds_ebwepm { get; private set; }
        private int ds_initState = 0;
            // 0: no initialization
            // 1: event added
            // 2: strings correct

        internal void initDisplayStrings()
        {
            if (ds_initState == 0)
                AppSettings.PropertyChanged += watchTimeFormat;

            if (ds_initState != 2 )
            {
                ds_wbwdam = computeString(weekday.timesWest.beforeNoon());
                ds_wbwdpm = computeString(weekday.timesWest.afterNoon());
                ds_wbweam = computeString(weekend.timesWest.beforeNoon());
                ds_wbwepm = computeString(weekend.timesWest.afterNoon());

                ds_ebwdam = computeString(weekday.timesEast.beforeNoon());
                ds_ebwdpm = computeString(weekday.timesEast.afterNoon());
                ds_ebweam = computeString(weekend.timesEast.beforeNoon());
                ds_ebwepm = computeString(weekend.timesEast.afterNoon());
                ds_initState = 2;
            }
        }

        internal void watchTimeFormat(Object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == AppSettings.Kdisplay12hr)
            {
                ds_initState = 1;
            }
        }

        private static System.Text.StringBuilder sb = new System.Text.StringBuilder();

        private string computeString(IEnumerable<DepartureTime> timelist)
        {
            sb.Clear();
            foreach (DepartureTime t in timelist)
            {
                sb.Append(t.ToString());
                sb.Append("\n");
            }
            if (sb.Length > 0)
                sb.Remove(sb.Length - 1, 1); // remove the last newline.

            return sb.ToString();
        }
    }
    #endregion
}