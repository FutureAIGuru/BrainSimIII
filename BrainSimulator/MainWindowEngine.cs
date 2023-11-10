//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace BrainSimulator
{
    public partial class MainWindow : Window
    {
        public static bool engineIsPaused = false;
        static long engineElapsed = 0;
        static bool updateDisplay = false;

        static List<int> engineTimerMovingAverage;
        static bool engineIsCancelled = false;
        private void EngineLoop()
        {
            while (!engineIsCancelled)
            {
                if (IsArrayEmpty())
                {
                    Thread.Sleep(100);
                }
                else if (IsEngineSuspended())
                {
                    if (updateDisplay)
                    {
                        updateDisplay = false;
                        displayUpdateTimer.Start();
                    }
                    Thread.Sleep(100); //check the engineDelay every 100 ms.
                    engineIsPaused = true;
                }
                else
                {
                    engineIsPaused = false;
                    if (theNeuronArray != null)
                    {
                        long start = Utils.GetPreciseTime();
                        theNeuronArray.Fire();
                        long end = Utils.GetPreciseTime();
                        engineElapsed = end - start;

                        if (updateDisplay)
                        {
                            Application.Current.Dispatcher.Invoke((Action)delegate
                            {
                                long dStart = Utils.GetPreciseTime();
                                fullUpdateNeeded = false;
                                long dEnd = Utils.GetPreciseTime();
                            });
                            updateDisplay = false;
                            displayUpdateTimer.Start();
                        }
                    }
                    Thread.Sleep(Math.Abs(engineDelay));
                }
            }
        }

        // stack to make sure we supend and resume the engine properly
        static Stack<int> engineSpeedStack = new Stack<int>();

        public bool IsEngineSuspended()
        {
            return engineSpeedStack.Count > 0;
        }

        public static void SuspendEngine()
        {
            // just pushing an int here, we won't restore it later
            engineSpeedStack.Push(engineDelay);
            if (theNeuronArray == null)
                return;

            string currentThreadName = Thread.CurrentThread.Name;
            // wait for the engine to actually stop before returning
            while (theNeuronArray != null && !engineIsPaused && currentThreadName != "EngineThread")
            {
                Thread.Sleep(100);
                System.Windows.Forms.Application.DoEvents();
            }
        }

        public static void ResumeEngine()
        {
            // first pop the top to make sure we balance the suspends and resumes
            if (engineSpeedStack.Count > 0)
                engineDelay = engineSpeedStack.Pop();
            if (theNeuronArray == null)
                return;

            // resume the engine
            // on shutdown, the current application may be gone when this is requested
            if (theNeuronArray != null && Application.Current != null)
            {
            }
        }
    }
}
