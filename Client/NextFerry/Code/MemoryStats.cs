namespace WP7Contrib.Diagnostics
{
    using Microsoft.Phone.Info;

    /// <summary>
    /// phone memory helper
    /// </summary>
    public static class MemoryStats
    {
       private static string TOTALMEMORY = "DeviceTotalMemory";
       private static string CURRENTMEMORY = "ApplicationCurrentMemoryUsage";
       private static string PEAKMEMORY = "ApplicationPeakMemoryUsage";

       /// <summary>
       /// Total device memory
       /// </summary>
       public static long TotalMemory 
       {
           get
           {
               return    (long)DeviceExtendedProperties.GetValue(TOTALMEMORY);            
           }
       }

       /// <summary>
       /// Current used memory
       /// </summary>
       public static long CurrentMemory
       {
           get
           {
               return (long)DeviceExtendedProperties.GetValue(CURRENTMEMORY);
           }
       }

       /// <summary>
       /// peak used memory
       /// </summary>
       public static long PeakMemory
       {
           get
           {
               return (long)DeviceExtendedProperties.GetValue(PEAKMEMORY);
           }
       }

       public static string DebugString
       {
           get
           {
               return string.Format("total: {0}|current: {1}|peak: {2}", TotalMemory, CurrentMemory, PeakMemory);
           }
       }

    }
}
