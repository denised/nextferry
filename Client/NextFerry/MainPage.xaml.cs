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

namespace NextFerry
{

    public partial class MainPage : PhoneApplicationPage
    {
        private ObservableCollection<Route> displayRoutes = new ObservableCollection<Route>();

        public MainPage()
        {
            InitializeComponent();
 
            DepartureTime.tooLateStyle = (Style)this.Resources["tooLateStyle"];
            DepartureTime.tooSoonStyle = (Style)this.Resources["tooSoonStyle"];
            DepartureTime.goodStyle = (Style)this.Resources["goodStyle"];
            DepartureTime.tooFarStyle = (Style)this.Resources["tooFarStyle"];

            list1.ItemsSource = displayRoutes;
            list3.ItemsSource = displayRoutes;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            SystemTray.BackgroundColor = (Color)this.Resources["PugetGreyColor"];
            SystemTray.ForegroundColor = (Color)this.Resources["MilkySkyColor"];

            if (AppSettings.displayWB)
                switchToWB(this, null);
            else
                switchToEB(this, null);
        }

        #region events / actions
        private void switchToWB(object sender, RoutedEventArgs e)
        {
            AppSettings.displayWB = true;
            displayRoutes.Clear();
            foreach (Route r in Routes.AllRoutes)
            {
                if (r.display && String.Equals(r.direction, "wb"))
                    displayRoutes.Add(r);
            }

            buttonWB.Style = (Style)this.Resources["toggleSelected"];
            buttonEB.Style = (Style)this.Resources["toggleUnselected"];
            westsign.Visibility = Visibility.Visible;
            eastsign.Visibility = Visibility.Collapsed;
        }

        private void switchToEB(object sender, RoutedEventArgs e)
        {
            AppSettings.displayWB = false;
            displayRoutes.Clear();
            foreach (Route r in Routes.AllRoutes)
            {
                if (r.display && String.Equals(r.direction, "eb"))
                    displayRoutes.Add(r);
            }

            buttonWB.Style = (Style)this.Resources["toggleUnselected"];
            buttonEB.Style = (Style)this.Resources["toggleSelected"];
            westsign.Visibility = Visibility.Collapsed;
            eastsign.Visibility = Visibility.Visible;
        }


        private void gotoRoutePage(object sender, GestureEventArgs e)
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
        #endregion
    }
}