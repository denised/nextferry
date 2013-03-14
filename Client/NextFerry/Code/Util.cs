using System;
using System.ComponentModel;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;

namespace NextFerry
{
    public static class Util
    {
        /// <summary>
        /// Make any simple action asynchronous.
        /// </summary>
        /// <param name="a"></param>
        public static void Asynch( Action a )
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (o, e) => { a(); };
            bw.RunWorkerAsync();
        }

        private static IsolatedStorageFile iStore = IsolatedStorageFile.GetUserStoreForApplication();

        /// <summary>
        /// Encapsulate reading a file from IsolatedStorage as a chunnk of text.
        /// </summary>
        /// <returns>The contents as a string, or null if no file or an error occurs.</returns>
        public static string readText(string filename)
        {
            if (iStore.FileExists(filename))
            {
                FileStream fs = null;
                StreamReader s = null;
                try
                {
                    fs = new IsolatedStorageFileStream(filename, FileMode.Open, iStore);
                    s = new StreamReader(fs);
                    return s.ToString();
                }
                catch (Exception e)
                {
                    Log.write("Read of " + filename + " failed: " + e);
                }
                finally
                {
                    if (s != null)
                        s.Close();
                    else if (fs != null)
                        fs.Close();
                }
            }
            return null;
        }

        /// <summary>
        /// Encapsulate over-writing a file as a single block of text.
        /// </summary>
        public static void writeText(string filename, string text)
        {
            IsolatedStorageFileStream stream = null;
            StreamWriter writer = null;
            try 
            {
                stream = new IsolatedStorageFileStream(filename, FileMode.Create, iStore);
                writer = new StreamWriter(stream);
                writer.Write(text);
            }
            catch(Exception e)
            {
                Log.write("Write of " + filename + " failed: " + e);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
                else if (stream != null)
                    stream.Close();
            }
        }
    }
}