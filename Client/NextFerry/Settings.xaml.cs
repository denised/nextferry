using System;
using System.Windows;
using System.Windows.Data;
using Microsoft.Phone.Controls;


namespace NextFerry
{
    public partial class Settings : PhoneApplicationPage
    {

        public Settings()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            time12.Style = (Style)this.Resources[ AppSettings.display12hr ? "toggleSelected" : "toggleUnselected" ];
            time24.Style = (Style)this.Resources[ AppSettings.display12hr ? "toggleUnselected" : "toggleSelected" ];

            uselocYes.Style = (Style)this.Resources[AppSettings.useLocation ? "toggleSelected" : "toggleUnselected"];
            uselocNo.Style = (Style)this.Resources[AppSettings.useLocation ? "toggleUnselected" : "toggleSelected"];

            waitslider.Value = AppSettings.bufferTime;
            cacheStatus.Text = RouteIO.cacheStatus();
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
            RouteIO.deleteCache();
            Routes.clearSchedules();
            cacheStatus.Text = RouteIO.cacheStatus();
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
            AppSettings.useLocation = true;
            uselocYes.Style = (Style)this.Resources["toggleSelected"];
            uselocNo.Style = (Style)this.Resources["toggleUnselected"];
            explanation.Text = "Wait time.";
            int val = Int16.Parse(slideValue.Text);
            if (val > 10)
            {
                waitslider.Value = Math.Max(5,val-30);
            }
            LocationMonitor.checkNow(null, null);
        }

        private void uselocOff(object sender, RoutedEventArgs e)
        {
            AppSettings.useLocation = false;
            uselocYes.Style = (Style)this.Resources["toggleUnselected"];
            uselocNo.Style = (Style)this.Resources["toggleSelected"];
            explanation.Text = "Travel + wait time.";
            int newval = Int16.Parse(slideValue.Text) + 30;
            waitslider.Value = (newval > waitslider.Maximum ? waitslider.Maximum : newval);
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).usingNetwork = !((App)Application.Current).usingNetwork;
            Routes.clearSchedules();
            ((App)Application.Current).verifySchedule();
            //((App)Application.Current).theMainPage.addWarning("hi there!");
        }
    }


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