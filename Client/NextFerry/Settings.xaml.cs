using System;
using System.Windows;
using System.Windows.Data;
using Microsoft.Phone.Controls;


namespace NextFerry
{
    public partial class Settings : PhoneApplicationPage
    {
        private const string explainWT = "Wait time.";
        private const string explainWTT = "Travel + wait time.";

        public Settings()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            time12.Style = (Style)this.Resources[ AppSettings.display12hr ? "toggleSelected" : "toggleUnselected" ];
            time24.Style = (Style)this.Resources[ AppSettings.display12hr ? "toggleUnselected" : "toggleSelected" ];

            waitslider.Value = AppSettings.bufferTime;
            useLoc.IsChecked = AppSettings.useLocation;
            explanation.Text = (AppSettings.useLocation ? explainWT : explainWTT);

            debug.IsChecked = AppSettings.debug;
            setDebugAppearance(AppSettings.debug);


            cacheStatus.Text = ScheduleIO.cacheStatus();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            // We wait to set this until leaving so as not to randomize other things.
            // (We could do the same with other properties?  There are no perf issues now.)
            // Note: we read out of the textblock since we've already converted to nice ints there.
            int newval = Int16.Parse(slideValue.Text);
            if (newval != AppSettings.bufferTime)
                AppSettings.bufferTime = newval;
        }


        private void deleteCache(object sender, EventArgs e)
        {
            ScheduleIO.deleteCache();
            RouteManager.clearSchedules();
            cacheStatus.Text = ScheduleIO.cacheStatus();
        }

        private void switchTo12hr(object sender, RoutedEventArgs e)
        {
            AppSettings.display12hr = true;
            time12.Style = (Style)this.Resources["toggleSelected"];
            time24.Style = (Style)this.Resources["toggleUnselected"];
        }

        private void switchTo24hr(object sender, RoutedEventArgs e)
        {
            AppSettings.display12hr = false;
            time12.Style = (Style)this.Resources["toggleUnselected"];
            time24.Style = (Style)this.Resources["toggleSelected"];
        }

        private void uselocOn(object sender, RoutedEventArgs e)
        {
            if (!AppSettings.useLocation)   // the check is because the toggle switch will trigger an event
            {                               // even when being initialized, which we don't want to respond to.
                AppSettings.useLocation = true;
                explanation.Text = explainWT;
                int val = Int16.Parse(slideValue.Text);
                if (val > 10)
                {
                    waitslider.Value = Math.Max(5, val - 30);
                }
                Util.Asynch(() => { LocationMonitor.checkTravelTimes(); });
            }
        }

        private void uselocOff(object sender, RoutedEventArgs e)
        {
            if (AppSettings.useLocation)
            {
                AppSettings.useLocation = false;
                explanation.Text = explainWTT;
                int newval = Int16.Parse(slideValue.Text) + 30;
                waitslider.Value = (newval > waitslider.Maximum ? waitslider.Maximum : newval);
            }
        }



        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //((App)Application.Current).verifySchedule();
            //((App)Application.Current).theMainPage.addWarning("hi there!");
        }


        // We do a little sleight of hand with some controls that appear and disappear,
        // depending on state.
        private void setDebugAppearance(bool appear)
        {
            // The blind blocks the view of the rest of the controls, so we want the blind to be
            // *visible* when the controls are *invisible* and vice versa.
            blind.Visibility = (appear ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible);
        }

        private void gotoLog(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/debug.xaml", UriKind.Relative));
        }

        private void magic_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            setDebugAppearance(true);
        }

        private void debug_Checked(object sender, RoutedEventArgs e)
        {
            AppSettings.debug = true;
        }

        private void debug_Unchecked(object sender, RoutedEventArgs e)
        {
            AppSettings.debug = false;
            setDebugAppearance(false);
        }
    }


    /// <summary>
    /// Convert the slider value (a double) into an integer divisible by five.
    /// </summary>
    public class wtConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double val = (double)value;  
            return (((int) val + 2) / 5 ) * 5;  // round to five minute increments
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}