namespace WP7Contrib.Diagnostics
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using System.Collections.ObjectModel;

    /// <summary>
    /// visual control which shows a real time ui of debug statements
    /// </summary>
    sealed internal class RunTimeDebugDisplay:Grid
    {
            

        #region properties

        #region ItemSource (DependencyProperty)

        /// <summary>
        /// Debug Lines
        /// </summary>
        public ObservableCollection<string> ItemSource
        {
            get { return (ObservableCollection<string>)GetValue(ItemSourceProperty); }
            set { SetValue(ItemSourceProperty, value); }
        }
        public static readonly DependencyProperty ItemSourceProperty =
            DependencyProperty.Register("ItemSource", typeof(ObservableCollection<string>), typeof(RunTimeDebugDisplay),
              new PropertyMetadata(null));

        #endregion

        #endregion

        #region inits

        public RunTimeDebugDisplay()
        {
            this.Loaded += new RoutedEventHandler(RunTimeDebugDisplay_Loaded);
            this.Unloaded += new RoutedEventHandler(RunTimeDebugDisplay_Unloaded);
            this.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(20, GridUnitType.Auto) });
            this.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(800, GridUnitType.Star) });
            this.SetValue(Grid.RowSpanProperty, 1000);
            this.SetValue(Grid.ColumnSpanProperty, 1000);
        }

        /// <summary>
        /// on unload clear the contents
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void RunTimeDebugDisplay_Unloaded(object sender, RoutedEventArgs e)
        {
            this.Children.Clear();
        }

        /// <summary>
        /// on loaded construct the controls, constructing inline as there is by design
        /// no templating or instantiation outside of the diagnostics project.  The visual is by design for 
        /// dev only, and really shouldnt be shown to users
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void RunTimeDebugDisplay_Loaded(object sender, RoutedEventArgs e)
        {
            StackPanel spRoot = new StackPanel();
            spRoot.Orientation = Orientation.Vertical;
            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;

            Button clearButton = new Button();
            Button emailButton = new Button();
            //Border memoryborder = new Border();
            //memoryborder.Opacity = 0.5;
            //memoryborder.Background = new SolidColorBrush(Colors.White);

            //TextBlock memoryoutput = new TextBlock();
            //memoryoutput.Foreground = new SolidColorBrush(Colors.Red);
            //memoryoutput.FontWeight = FontWeights.ExtraBold;
            //memoryborder.Child = memoryoutput;
            //memoryborder.Visibility = Visibility.Collapsed;

            Rectangle line = new Rectangle() { Height = 2, Fill = new SolidColorBrush(Colors.Red) };
            line.Visibility = System.Windows.Visibility.Collapsed;

            //memoryoutput.Text = MemoryStats.DebugString; 
            
            ItemsControl ic = new ItemsControl();
            ic.IsHitTestVisible = false;
            ic.Visibility = Visibility.Collapsed;
            ic.ItemsSource = ItemSource;
            ic.Foreground = new SolidColorBrush(Colors.Red);
            ic.FontWeight = FontWeights.Bold;
            //CheckBox cb = new CheckBox();
            //cb.Checked += delegate(object sender2, RoutedEventArgs e2)
            //{
            //    ic.Visibility = Visibility.Visible;
            //    clearButton.Visibility = Visibility.Visible;
            //    emailButton.Visibility = Visibility.Visible;
            //    memoryborder.Visibility = Visibility.Visible;
            //    line.Visibility = System.Windows.Visibility.Visible;
            //};
            //cb.Unchecked += delegate(object sender3, RoutedEventArgs e3)
            //{
            //    ic.Visibility = Visibility.Collapsed;
            //    clearButton.Visibility = Visibility.Collapsed;
            //    emailButton.Visibility = Visibility.Collapsed;
            //    memoryborder.Visibility = Visibility.Collapsed;
            //    line.Visibility = System.Windows.Visibility.Collapsed;

            //};

            //cb.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            //cb.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            
            //cb.Content = "diag.";
            ic.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            ic.Background = new SolidColorBrush(Colors.White);
            ic.Opacity = 0.7;
            ic.Height = 800;
            ic.SetValue(Grid.RowProperty, 1);
            //cb.SetValue(Grid.RowProperty, 0);

            clearButton.Content = "clear";
            clearButton.Visibility = System.Windows.Visibility.Collapsed;
            clearButton.Click += delegate(object sender4, RoutedEventArgs e4)
            {
                RuntimeDebug.Clear();
            };

            emailButton.Content = "email";
            emailButton.Visibility = System.Windows.Visibility.Collapsed;
            emailButton.Click += delegate(object sender5, RoutedEventArgs e5)
            {
                RuntimeDebug.SendEmail();

            };
            sp.Children.Add(clearButton);
            sp.Children.Add(emailButton);
            //sp.Children.Add(cb);
            spRoot.Children.Add(sp);
            //spRoot.Children.Add(memoryborder);
            spRoot.Children.Add(line);

            spRoot.SetValue(Grid.RowProperty, 0);
            sp.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;            
            this.Children.Add(ic);
            this.Children.Add(spRoot);

            ic.Visibility = Visibility.Visible;
            clearButton.Visibility = Visibility.Visible;
            emailButton.Visibility = Visibility.Visible;
            line.Visibility = Visibility.Visible;
        }


        #endregion

    }
}
