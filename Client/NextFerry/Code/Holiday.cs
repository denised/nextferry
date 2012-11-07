using System;

namespace NextFerry
{
    public static class Holiday
    {
        // By the time this no longer holds, we'll have a version that updates itself automatically...
        public static bool isHoliday(DateTime day)
        {
            switch (day.Year)
            {
                case 2012:
                    switch (day.Month)
                    {
                        case 1:
                            return (day.Day == 1 || day.Day == 16);
                        case 2:
                            return (day.Day == 20);
                        case 5:
                            return (day.Day == 28);
                        case 7:
                            return (day.Day == 4);
                        case 9:
                            return (day.Day == 3);
                        case 11:
                            return (day.Day == 12 || day.Day == 22);
                        case 12:
                            return (day.Day == 25);
                        default: return false;
                    }
                case 2013:
                    switch (day.Month)
                    {
                        case 1:
                            return (day.Day == 1 || day.Day == 21);
                        case 2:
                            return (day.Day == 18);
                        case 5:
                            return (day.Day == 27);
                        case 7:
                            return (day.Day == 4);
                        case 9:
                            return (day.Day == 2);
                        case 11:
                            return (day.Day == 11 || day.Day == 28);
                        case 12:
                            return (day.Day == 25);
                        default: return false;
                    }
                case 2014:
                    switch (day.Month)
                    {
                        case 1:
                            return (day.Day == 1 || day.Day == 20);
                        case 2:
                            return (day.Day == 17);
                        case 5:
                            return (day.Day == 26);
                        case 7:
                            return (day.Day == 4);
                        case 9:
                            return (day.Day == 1);
                        case 11:
                            return (day.Day == 11 || day.Day == 27);
                        case 12:
                            return (day.Day == 25);
                        default: return false;
                    }
                default:
                    switch (day.Month)
                    {
                        case 1:
                            return (day.Day == 1);
                        case 7:
                            return (day.Day == 4);
                        case 12:
                            return (day.Day == 25);
                        default:
                            return false;
                    }
            }
        }
    }
}
