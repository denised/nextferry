using System;
using System.ComponentModel;
using System.Reflection;

namespace NextFerry
{
    public static class Util
    {
        public static void Asynch( Action a )
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (o, e) => { a(); };
            bw.RunWorkerAsync();
        }
    }
}