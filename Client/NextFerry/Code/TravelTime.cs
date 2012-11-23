using System;
using System.Net;
using System.Windows;
using System.Device.Location;

namespace NextFerry
{
    /// <summary>
    /// Encapsulates the logic for deciding the travel time to different ferry terminals.
    /// Options include a fixed value, a linear distance metric, and an actual traffic estimation,
    /// depending on the options the user has selected and whether we can get the data or not.
    /// </summary>
    public static class TravelTime
    {
        public static DepartureTime.GoodnessValue goodness(DepartureTime d, Terminal t)
        {
            int now = DepartureTime.Now;
            int tooSoon = now + AppSettings.terminalTravelTime;
            int dontcare = tooSoon + 120;


            if (d.value < now)
                return DepartureTime.TooLate;
            else if (d.value < tooSoon)
                return DepartureTime.TooSoon;
            else if (d.value < dontcare)
                return DepartureTime.Good;
            else
                return DepartureTime.TooFar;
        }


        public static GeoCoordinateWatcher locationAccess = null; // only initialize after we've determined we will use it.
    }
}
