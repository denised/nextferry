using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;


namespace NextFerry
{
    public partial class RoutePage : PhoneApplicationPage
    {
        private Route routeWB = null;
        private Route routeEB = null;

        // Note that the data on this page is "dead".  If the schedule is updated while the 
        // user is on this page (or while the page is tombstoned), the page will not update.
        // However, the page is recomputed from scratch every time it is visited fresh from
        // mainpage.   I consider this a reasonable tradeoff.

        public RoutePage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            bool recovered = false;

            // if our state is already set, we don't need to do anything
            if (routeWB != null)
                return;

            // This is either a new page, or we're being restored from tombstoning.
            // If we tombstoned, the route should be in State
            // If this is a new page, the route name is in the URL.
            String routeName = null;
            if (State.ContainsKey("route"))
            {
                routeName = (string)State["route"];
                recoverLists();
                recovered = true;
            }
            else
            {
                NavigationContext.QueryString.TryGetValue("route", out routeName);
            }

            if (routeName == null)
            {
                Log.write("Can't recover RoutePage state!");
                throw new InvalidOperationException();
            }

            routeWB = Routes.getRoute(routeName, "wb");
            routeEB = routeWB.sibling();

            eastport.Text = routeWB.eastTerminal().name;
            westport.Text = routeWB.westTerminal().name;

            if (!recovered)
            {
                assignLists();
            }
        }


        #region content management
        private void assignLists()
        {
            wbwdam.Text = computeString(Departures.beforeNoon(routeWB.weekday.times));
            wbwdpm.Text = computeString(Departures.afterNoon(routeWB.weekday.times));
            wbweam.Text = computeString(Departures.beforeNoon(routeWB.weekend.times));
            wbwepm.Text = computeString(Departures.afterNoon(routeWB.weekend.times));

            ebwdam.Text = computeString(Departures.beforeNoon(routeEB.weekday.times));
            ebwdpm.Text = computeString(Departures.afterNoon(routeEB.weekday.times));
            ebweam.Text = computeString(Departures.beforeNoon(routeEB.weekend.times));
            ebwepm.Text = computeString(Departures.afterNoon(routeEB.weekend.times));
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            State["route"] = routeWB.name;
            State["wbwdam"] = wbwdam.Text;
            State["wbwdpm"] = wbwdpm.Text;
            State["wbweam"] = wbweam.Text;
            State["wbwepm"] = wbwepm.Text;

            State["ebwdam"] = ebwdam.Text;
            State["ebwdpm"] = ebwdpm.Text;
            State["ebweam"] = ebweam.Text;
            State["ebwepm"] = ebwepm.Text;
        }

        private void recoverLists()
        {
            wbwdam.Text = (string)State["wbwdam"];
            wbwdpm.Text = (string)State["wbwdpm"];
            wbweam.Text = (string)State["wbweam"];
            wbwepm.Text = (string)State["wbwepm"];

            ebwdam.Text = (string)State["ebwdam"];
            ebwdpm.Text = (string)State["ebwdpm"];
            ebweam.Text = (string)State["ebweam"];
            ebwepm.Text = (string)State["ebwepm"];
        }
        #endregion

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



        private void gotoSettings(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
        }
    }
}