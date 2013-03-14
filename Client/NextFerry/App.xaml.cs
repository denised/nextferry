using System;
using System.Threading;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Threading;
using System.ComponentModel;


namespace NextFerry
{
    public partial class App : Application
    {
        public PhoneApplicationFrame RootFrame { get; private set; }
        public string appVersion = "3.0";  // TODO: set via reflection?  (didn't work?)
        public MainPage theMainPage = null;

        public App()
        {
            UnhandledException += Application_UnhandledException;
            InitializeComponent();
            InitializePhoneApplication();
        }
     
        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            AppSettings.init();
            Log.write("Launching");
            
            upgrade();
            startBackground();
        }


        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            if (e.IsApplicationInstancePreserved)
            {
                Log.write("Activating, instance preserved");
            }
            else // start over again
            {
                AppSettings.init();
                Log.write("Activating, rehydrating, cache version " + AppSettings.cacheVersion );
            }
            startBackground();
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            LocationMonitor.stop();
            AppSettings.close();
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            LocationMonitor.stop();
            AppSettings.close();
        }

        #region error handlers

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            Log.write("Root navigation failed! " + e.Exception);
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            // This is a big hammer, but it seems better than trying to be more delicate:
            // *Any* error that happens because we try to communicate with a server is caught and "disappeared" here.
            // That is okay because the app is architected to not need server response.
            // NB: we have to do this because the service reference code can cause exceptions on 
            // other threads that we can't put a try/catch around.
            if (e.ExceptionObject is System.ServiceModel.CommunicationException)
            {
                Log.write("Server communication failure: " + e.ExceptionObject);
                e.Handled = true;
            }
            else if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        #endregion

        #region boiler plate application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion

        #region the real work

        private void upgrade()
        {
            // check to see if we need to updgrade any data
            string lastv = AppSettings.lastAppVersion;
            if (lastv != "0.0" && lastv != appVersion)
            {
                Log.write("Upgrading");
                if (lastv.StartsWith("1"))
                {
                    Log.write("...from V1");
                    // Clear the cache --- it will have bad route data.
                    ScheduleIO.deleteCache();
                }
                // For either V1 or V2, update display information
                AppSettings.RouteSetting.upgrade();
            }
            AppSettings.lastAppVersion = appVersion;
        }

        private void startBackground()
        {
            // Readcache is synchronous, and we want it to complete before beginning
            // the others.   The others are all asynchronous.
            Util.Asynch(() =>
            {
                // if we are resuming, instance intact, we should already have schedules
                if (!RouteManager.haveSchedules())
                {
                    ScheduleIO.readCache();
                    ServerIO.requestInitUpdate();
                    AlertManager.recoverCache();
                }
                LocationMonitor.start();
            });
        }

        #endregion
    }
}