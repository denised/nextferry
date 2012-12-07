using System;
using System.Collections.Generic;
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
using WP7Contrib;

namespace NextFerry
{
    public partial class debug : PhoneApplicationPage
    {
        public debug()
        {
            InitializeComponent();

        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            WP7Contrib.Diagnostics.RuntimeDebug.ShowDebug();
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            WP7Contrib.Diagnostics.RuntimeDebug.HideDebug();
            base.OnNavigatingFrom(e);
        }
    }
}