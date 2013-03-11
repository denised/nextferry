using System;
using System.Collections.Generic;
using Microsoft.Phone.Controls;


namespace NextFerry
{
    public partial class RoutePage : PhoneApplicationPage
    {
        private Route r = null;

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
            if (r != null)
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

            r = RouteManager.lookup(routeName);
            eastport.Text = Terminal.lookup(r.eastCode).name;
            westport.Text = Terminal.lookup(r.westCode).name;

            if (!recovered)
            {
                assignLists();
            }
        }


        #region content management
        private void assignLists()
        {
            wbwdam.Text = computeString(r.weekday.timesWest.beforeNoon());
            wbwdpm.Text = computeString(r.weekday.timesWest.afterNoon());
            wbweam.Text = computeString(r.weekend.timesWest.beforeNoon());
            wbwepm.Text = computeString(r.weekend.timesWest.afterNoon());

            ebwdam.Text = computeString(r.weekday.timesEast.beforeNoon());
            ebwdpm.Text = computeString(r.weekday.timesEast.afterNoon());
            ebweam.Text = computeString(r.weekend.timesEast.beforeNoon());
            ebwepm.Text = computeString(r.weekend.timesEast.afterNoon());
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            State["route"] = r.wbName;
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