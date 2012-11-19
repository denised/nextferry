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
            if ( AppSettings.display12hr )
            {
                time12.Style = (Style)this.Resources["toggleSelected"];
                time24.Style = (Style)this.Resources["toggleUnselected"];
            }
            else
            {
                time12.Style = (Style)this.Resources["toggleUnselected"];
                time24.Style = (Style)this.Resources["toggleSelected"];
            }

            waitslider.Value = AppSettings.terminalTravelTime;
            debugScreen.Text = RouteIO.cacheStatus();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            // We wait to set this until leaving so as not to randomize other things.
            // (We could do the same with other properties?  There are no perf issues now.)
            // Note: we read out of the textblock since we've already converted to nice ints there.
            int newval = Int16.Parse(slideValue.Text);
            if (newval != AppSettings.terminalTravelTime)
                AppSettings.terminalTravelTime = newval;
        }


        private void deleteCache(object sender, EventArgs e)
        {
            RouteIO.deleteCache();
            Routes.clearSchedules();
            debugScreen.Text = RouteIO.cacheStatus();
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Terminal.testTerms();
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