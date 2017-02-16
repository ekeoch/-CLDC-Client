using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Org.Mentalis.Utilities;
using System.Threading;

namespace _CLDC_Client
{
    public partial class Form1
    {      
        /// <summary>
        /// //Log system off
        /// </summary>
        public static void Log_off()
        {
            WindowsController.ExitWindows(RestartOptions.LogOff, true);
        }
             
        /// <summary>
        /// //Restart System
        /// </summary>
        public static void Restart()
        {
            WindowsController.ExitWindows(RestartOptions.Reboot, true);
        }
        
        /// <summary>
        /// //Shut System down
        /// </summary>
        public static void Shutdown()
        {
            WindowsController.ExitWindows(RestartOptions.ShutDown, true);
        }

        /// <summary>
        /// //Suspend System
        /// </summary>
        public static void Suspend()
        {
            WindowsController.ExitWindows(RestartOptions.Suspend, true);
        }

        /// <summary>
        /// //Hibernate system
        /// </summary>
        public static void hibernate()
        {
            WindowsController.ExitWindows(RestartOptions.Hibernate, true);
        }

        /// <summary>
        /// //Lock Workstation
        /// </summary>
        [DllImport("user32.dll")]
        public static extern void LockWorkStation();

        /// <summary>
        ///  Execute a process at a given location
        ///  For update implement executable feed back
        ///  <remarks>
        ///  The problem with feed back, is because running 
        ///  programs that hang could crash the client end leading to even more 
        ///  problems.
        ///  </remarks>
        /// </summary>
        /// <param name="path"></param>
        /// <param name="parametr"></param>
        /// <returns></returns>
        public static string execute_process(string path, string parametr)
        {
            try
            {
                Thread.Sleep(1000);
                ProcessStartInfo My_startinfo = new ProcessStartInfo(path);
                My_startinfo.Arguments = parametr;

                Process my_process = new Process();
                my_process.StartInfo = My_startinfo;
                my_process.Start();
                return "Process Executed Successfully. . .";
            }
            catch (Exception E)
            {
                //Return the corresponding error to the user
                return "" + E.Message;
            }
        }
    }
}