using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace NextFerry
{
    /// <summary>
    /// The collection of all routes.
    /// </summary>
    public static class RouteManager
    {
        /// <summary>
        /// The primary application state: a list of all the ferry routes and their schedules.
        /// </summary>
        public static ObservableCollection<Route> AllRoutes = new ObservableCollection<Route>()
        {
            // Some day we may want to receive these from the server, rather than hard code them.
            // Today is not that day.
            new Route(1,     7,  3, "bainbridge","bainbridge"),
            new Route(1<<2,  8, 12, "edmonds","edmonds"),
            new Route(1<<3, 14,  5, "mukilteo","mukilteo"),
            new Route(1<<4, 11, 17, "pt townsend","pt townsend"),
            new Route(1<<5,  9, 20, "fauntleroy-southworth","southworth-fauntleroy"),
            new Route(1<<6,  9, 22, "fauntleroy-vashon","vashon-fauntleroy"),
            new Route(1<<7, 22, 20, "vashon-southworth","southworth-vashon"),
            new Route(1<<8,  7,  4, "bremerton","bremerton"),
            new Route(1<<9, 21, 16, "vashon-pt defiance","pt defiance-vashon"),
            new Route(1<<10, 1, 10, "friday harbor","friday harbor"),
            new Route(1<<11, 1, 15, "orcas","orcas")
        };


        /// <summary>
        /// Return the route that corresponds to this name (which may be either a wb or eb name)
        /// </summary>
        public static Route lookup(string name)
        {
            return AllRoutes.First(r => r.ebName == name || r.wbName == name);
        }

        /// <summary>
        /// Return the route that corresponds to this route code.
        /// </summary>
        public static Route lookup(int code)
        {
            return AllRoutes.First(r => r.routeCode == code);
        }

        /// <summary>
        /// Return all routes represented by bitcode
        /// </summary>
        public static IEnumerable<Route> bitRoutes(int bitcode)
        {
            foreach (Route r in AllRoutes)
            {
                if ( (r.routeCode & bitcode)  != 0)
                    yield return r;
            }
        }

        /// <summary>
        /// Remove all the current schedules from the routes.   Used to completely refresh schedule state.
        /// </summary>
        public static void clearSchedules()
        {
            foreach (Route r in AllRoutes)
                r.clearSchedules();
        }

        /// <summary>
        /// Return true if we have at least some schedules.
        /// </summary>
        public static bool haveSchedules()
        {
            return AllRoutes.Any(r => !r.weekday.isEmpty());
        }

        /// <summary>
        /// Update visual appearance of visable routes.
        /// </summary>
        public static void updateDisplay()
        {
            foreach (Route r in AllRoutes)
            {
                if (r.display)
                {
                    r.updateGoodness();
                }
            }
        }
    }
}
