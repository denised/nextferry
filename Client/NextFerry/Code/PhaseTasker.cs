using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;


namespace NextFerry
{
    /// <summary>
    /// In spite of the name, this is not a general-purpose piece of code.
    /// It just isolates the logic for the specific scneario we have in this app.
    /// <para>
    /// To wit:
    /// Tasks are executed with "barriers" between the phases --- that is, all the 
    /// tasks from the first phase are completed before tasks in the 2nd phase start.
    /// Tasks are only created from one thread (the UI thread), and task creation never runs in parallel
    /// to task execution, so we don't worry about multi-threaded access to the queues.
    /// Once a group is set up, it isn't added to, so we aren't worrying about starvation, etc.
    /// Tasks running in parallel do not interact, so we are not worrying aboud deadlock.
    /// All tasks are assumed to be expendable, so we just trap errors and move on.
    /// No reporting back to the parent is done (each task uses Dispatcher.BeginInvoke as needed).
    /// 
    /// Note this would be an obvious place to use WaitHandle.WaitAll, but the phone doesn't have it.
    /// And I like the elegance of this solution anyway :-)
    /// </summary>
    public class PhaseTasker
    {
        private BackgroundWorker mythread;
        private List<Action> list1;
        private List<Action> list2;
        private List<Action> list3;
        private int waitCount;

        public PhaseTasker()
        {
            list1 = new List<Action>();
            list2 = new List<Action>();
            list3 = new List<Action>();
        }

        public void addAction(int phase, Action action)
        {
            switch (phase)
            {
                case 1: list1.Add(action); break;
                case 2: list2.Add(action); break;
                case 3: list3.Add(action); break;
                default: throw new InvalidOperationException("phase must be 1, 2, or 3");
            }
        }


        public void go()
        {
            if (mythread == null)
            {
                mythread = new BackgroundWorker();
                mythread.DoWork += phase1;
                mythread.RunWorkerAsync();
            }
            else
                throw new InvalidOperationException("can't run more than once");
        }


        void phase1(object sender, DoWorkEventArgs e)
        {
            if (list1.Count > 0)
            {
                waitCount = list1.Count;
                foreach (Action a in list1)
                {
                    Action mine = a;
                    ThreadPool.QueueUserWorkItem((o) =>
                    {
                        try
                        {
                            mine();
                        }
                        catch (Exception err)
                        {
                            Log.write("task error " + err.ToString());
                        }
                        finally
                        {
                            bool Im_it = false;
                            lock (mythread)
                            {
                                waitCount--;
                                if (waitCount == 0) Im_it = true;
                            }

                            if (Im_it) // I was the last thread, become the master of the next phase
                                phase2();
                        }
                    });
                }
                // Background worker exits when all tasks have been queued.
            }
            else
            {
                phase2();
            }
        }

        private void phase2()
        {
            // looks just like phase1...
            if (list2.Count > 0)
            {
                waitCount = list2.Count;
                foreach (Action a in list2)
                {
                    Action mine = a;
                    ThreadPool.QueueUserWorkItem((o) =>
                    {
                        try
                        {
                            mine();
                        }
                        catch (Exception err)
                        {
                            Log.write("task error " + err.ToString());
                        }
                        finally
                        {
                            bool Im_it = false;
                            lock (mythread)
                            {
                                waitCount--;
                                if (waitCount == 0) Im_it = true;
                            }

                            if (Im_it) // I was the last thread, become the master of the next phase
                                phase3();
                        }
                    });
                }
                // phase 2 thread exits.
            }
            else
            {
                phase3();
            }
        }

        private void phase3()
        {
            // simpler: just fire and forget
            foreach (Action a in list3)
            {
                Action mine = a;
                ThreadPool.QueueUserWorkItem((o) =>
                {
                    try
                    {
                        mine();
                    }
                    catch (Exception err)
                    {
                        Log.write("task error " + err.ToString());
                    }
                });
            }
            // phase 3 thread exits.
        }
    }
}
