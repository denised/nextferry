using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Threading;

namespace NextFerry
{

    public partial class MainPage : PhoneApplicationPage
    {
        private ObservableCollection<Route> displayRoutes = new ObservableCollection<Route>();
        private DispatcherTimer travelTimeWatcher = new DispatcherTimer();

        public MainPage()
        {
            InitializeComponent();

            ((App)Application.Current).theMainPage = this;
 
            DepartureTime.tooLateStyle = (Style)this.Resources["tooLateStyle"];
            DepartureTime.riskyStyle = (Style)this.Resources["riskyStyle"];
            DepartureTime.goodStyle = (Style)this.Resources["goodStyle"];
            DepartureTime.defaultStyle = (Style)this.Resources["defaultStyle"];

            list1.ItemsSource = displayRoutes;
            list3.ItemsSource = displayRoutes;

            WP7Contrib.Diagnostics.RuntimeDebug.Initialize(true, false, "denisesandbox@mailup.net", "");

            initWatcher();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            SystemTray.BackgroundColor = (Color)this.Resources["PugetGreyColor"];
            SystemTray.ForegroundColor = (Color)this.Resources["MilkySkyColor"];

            if (AppSettings.displayWB)
                switchToWB(this, null);
            else
                switchToEB(this, null);

            if (AppSettings.useLocation)
                travelTimeWatcher.Start();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            travelTimeWatcher.Stop();
        }

        #region user actions
        private void switchToWB(object sender, RoutedEventArgs e)
        {
            AppSettings.displayWB = true;
            displayRoutes.Clear();
            foreach (Route r in RouteManager.AllRoutes)
            {
                if (r.display && String.Equals(r.direction, "wb"))
                    displayRoutes.Add(r);
            }

            buttonWB.Style = (Style)this.Resources["toggleSelected"];
            buttonEB.Style = (Style)this.Resources["toggleUnselected"];
            ewsign.Opacity = 0;
            ewsign.Text = "west";
            fadeIn.Begin();
        }

        private void switchToEB(object sender, RoutedEventArgs e)
        {
            AppSettings.displayWB = false;
            displayRoutes.Clear();
            foreach (Route r in RouteManager.AllRoutes)
            {
                if (r.display && String.Equals(r.direction, "eb"))
                    displayRoutes.Add(r);
            }

            buttonWB.Style = (Style)this.Resources["toggleUnselected"];
            buttonEB.Style = (Style)this.Resources["toggleSelected"];
            ewsign.Opacity = 0;
            ewsign.Text = "east";
            fadeIn.Begin();
        }


        private void gotoRoutePage(object sender, System.Windows.Input.GestureEventArgs e)
        {          
            // Figure out which item we were on (thank you msdn code samples!)
            FrameworkElement item = (FrameworkElement)e.OriginalSource;
            Route r = (Route)item.DataContext;
            if (r == null) return;

            string urlWithData = string.Format("/RoutePage.xaml?route={0}", r.name);
            NavigationService.Navigate(new Uri(urlWithData, UriKind.Relative));
        }

        private void gotoSettings(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
        }

        private void gotoChoose(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Choose.xaml", UriKind.Relative));
        }

        private void gotoHelp(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/help.xaml", UriKind.Relative));
        }
        #endregion

        #region travel time management

        private TimeSpan shortInterval = new TimeSpan(0, 0, 10);
        private TimeSpan longInterval = new TimeSpan(0, 3, 0);

        private void initWatcher()
        {
            travelTimeWatcher.Interval = shortInterval;
            travelTimeWatcher.Tick += (o, a) =>
                {
                    if (AppSettings.useLocation)
                    {
                        addWarning("Waiting for travel times");
                    }
                    // The timer will be turned back on when travel times arrive.
                    travelTimeWatcher.Stop();
                };

            LocationMonitor.NewTravelTimes += timesReceived;
        }

        /// <summary>
        /// What to do when some new travel times arrive.
        /// </summary>
        private void timesReceived(TravelTimeEventArgs args)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (args.traveltimes.Count > 0)
                    removeWarning();
                else
                    addWarning("Travel times not available");

                foreach (Terminal t in Terminal.AllTerminals)
                {
                    if (args.traveltimes.ContainsKey(t.code))
                    {
                        t.setTT(args.traveltimes[t.code]);
                    }
                    else
                    {
                        t.clearTT();
                    }
                }
                RouteManager.updateDisplay();


                // After the first times are received, set the watch interval to longer
                travelTimeWatcher.Interval = longInterval;
                travelTimeWatcher.Start();
            });
        }


        private void addWarning(string contents)
        {
            warningText.Text = contents;
            warning.Visibility = System.Windows.Visibility.Visible;
        }

        private void removeWarning()
        {
            warning.Visibility = System.Windows.Visibility.Collapsed;
        }


        #endregion
        
        #region notifications

        private void notifications_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {

        }

        #endregion
    }
}