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
        private List<string> redrawEvents = null;

        public RoutePage()
        {
            InitializeComponent();

            // This setting exists for the lifetime of the page.
            // Since we reuse this page for all routes, this is okay --- the page will only be
            // destroyed on app exist or tombstoning --- and in those cases AppSettings is destroyed too.
            AppSettings.PropertyChanged += maybeRedraw;
            redrawEvents = new List<string>();
            redrawEvents.Add("display12hr"); // from AppSettings
            redrawEvents.Add("weekday"); // from Route
            redrawEvents.Add("weekend"); // from Route
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            // if our state is already set, we don't need to do anything
            if (routeWB != null)
                return;

            // Otherwise, this is either a new page, or we're being restored from tombstoning.
            // If we tombstoned, the route should be in State
            // If this is a new page, the route name is in the URL.
            String routeName = null;
            if (State.ContainsKey("route"))
                routeName = (string)State["route"];
            else
                NavigationContext.QueryString.TryGetValue("route", out routeName);

            if (routeName == null)
            {
                Log.write("Can't recover RoutePage state!");
                throw new InvalidOperationException();
            }

            routeWB = Routes.getRoute(routeName, "wb");
            routeEB = Routes.getRoute(routeName, "eb");

            // some names are different in each direction,
            // in which case we need to recover the missing one from its sibling.
            if (routeWB == null) routeWB = routeEB.sibling();
            if (routeEB == null) routeEB = routeWB.sibling();

            eastport.Text = routeWB.eastTerminal().name;
            westport.Text = routeWB.westTerminal().name;

            assignLists();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            // We just store the name of the route and regenerate everything else (it's cheap enough)
            State["route"] = routeWB.name;
        }

        private void gotoSettings(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
        }

       
        private void maybeRedraw(Object sender, PropertyChangedEventArgs args)
        {
            if (redrawEvents != null && redrawEvents.Contains(args.PropertyName))
            {
                assignLists();
            }
        }

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
}