using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;

namespace _CLDC_Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Form1 Myform1 = new Form1();
        }
    }

    public partial class Form1 : Form
    {
        #region VARIABLES
        private string UserName = System.Environment.UserName;//get the username, or the person currently logged on
        private string Computer = System.Environment.MachineName;//get the name of the computer
        IPHostEntry Host = Dns.GetHostEntry(Dns.GetHostName());
        private string Ipadd
        {
            get
            {
                foreach (IPAddress Add in Host.AddressList)
                {
                    if (Add.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return Add.ToString();
                    }
                }
                return "xxxx";
            }
        }

        private StreamWriter swSender;
        private StreamReader srReceiver;
        private TcpClient tcpServer;
        string Server_Ip = "5.203.193.115";//Ip address the server.

        private delegate void UpdateLogCallback(string strMessage);
        private delegate void CloseConnectionCallback(string strReason);
        private Thread thrMessaging, scan_thread;
        private IPAddress ipAddr;
        private bool Connected;
        Transfer.RecieveFile MyFile;
        #endregion

        #region CONSTRUCTOR
        public Form1()
        {
            Application.ApplicationExit += new EventHandler(OnApplicationExit);
            Thread TransferThread = new Thread(new ThreadStart(start_transfer));
            scan_thread = new Thread(new ThreadStart(scan_for_server));
            TransferThread.Start();
            scan_thread.Start();
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Initialize connection
        /// </summary>
        private void InitializeConnection()
        {
            if (Connected)
            { return; }//Save guard to prevent double execution of the initialize method

            try//Added this: In case the user inputs the Ip Address Wrongly
            { ipAddr = IPAddress.Parse(Server_Ip); }
            catch (Exception)
            { return; }

            //Start The new TCP connections to the chat server
            tcpServer = new TcpClient();

            //1989 is the port value, It doesnt matter as long as it is open
            //so I am using 20500, if it sticks, change it to something else xD
            try
            { tcpServer.Connect(ipAddr, 20500); }//
            catch (Exception)
            {/*Do nothing*/}

            //Establish that we have connected to the Chat Server
            Connected = true;

            // Send the desired username to the server
            swSender = new StreamWriter(tcpServer.GetStream());
            string full_name = UserName + "[" + Ipadd + "]"; //this way, we can know their computer name, as well as prevent multiple logins
            swSender.WriteLine(full_name);
            swSender.Flush();

            // Start the thread for receiving messages and further communication
            // Starting a new thread allows you to Run processes[Send/Receive] simulataeneously
            thrMessaging = new Thread(new ThreadStart(ReceiveMessages));
            thrMessaging.Start();
        }

        /// <summary>
        /// //method to folow the thrmesaging thread
        /// </summary>
        private void ReceiveMessages()
        {
            //Recieve the Response from the Server
            srReceiver = new StreamReader(tcpServer.GetStream());

            //if the first character of the response is 1, connection was successfull
            //tweak this to account for the possibility the user might be busy/hidden/Or Whatever

            string ConResponse = srReceiver.ReadLine();

            if (ConResponse != null && ConResponse[0] == '1')//Added stuff
            {
                //Invoke(new UpdateLogCallback(UpdateLog), new object[] { "Connected Successfully!" });
                //do somthing with the initial connection
            }
            else//The connection was probably Unsuccessful
            {
                if (ConResponse == null)//Added this
                { ConResponse = "00NULL Exception Error!"; }//Added 00 since the first 2 characters are deleted

                string Reason = "Not Connected: ";
                // Extract the reason out of the response message. The reason starts at the 3rd character
                Reason += ConResponse.Substring(2, ConResponse.Length - 2);
                // Update the form with the reason why we couldn't connect
                try
                {
                    Invoke(new CloseConnectionCallback(CloseConnection), new object[] { Reason });
                }
                catch (Exception)
                { Connected = false; }
                // Exit the method
                return;
            }

            //While We are connected to the server Getting Incoming Messages
            while (Connected)
            {
                try//Added this
                { Invoke(new UpdateLogCallback(UpdateLog), new object[] { srReceiver.ReadLine() }); }// Show the messages in the log TextBox
                catch
                { Connected = false; }
            }

        }

        /// <summary>
        /// This method is called from a different thread in order to update the log TextBox
        /// This method basically servers as the laison btw 2 threads
        /// </summary>
        /// <param name="strMessage"></param>
        private void UpdateLog(string strMessage)
        {
            int count = 0;
            string new_string = string.Empty;//initialize new_string with null o_0
            string command, parameter;
            int i = 1;

            //Handle null pointer exceptions
            if (strMessage == string.Empty || !strMessage.StartsWith("9"))
            { return; }

            strMessage = strMessage.Remove(0, 1);//delete the 9 attached to the string
            string[] Fullmessage = strMessage.Split();//split the string into an array of strings by their white spaces

            //Delete the sender from the string an attach and trim any leaading whitespaces
            strMessage = strMessage.Remove(0, Fullmessage[0].Length + 1);
            strMessage = strMessage.TrimStart();


            if (strMessage.StartsWith("["))//Security measure to ensure above process workd properly
            {
                try//Filter off any lucky passes
                {
                    //peel off the length of the command form the message
                    while (i != strMessage.IndexOf(']'))
                    {
                        new_string = new_string + strMessage[i];
                        i++;
                    }
                    count = Convert.ToInt32(new_string);//cast the parsed length to an integer
                    new_string = strMessage.Remove(0, new_string.Length + 2);//peel off the "[command length]" from the message

                    //Conditional crap
                    //the computer name is included in the returnd meesage so the user can knoew where it originated from
                    if (new_string.Trim() == "Shut down")
                    {
                        Shutdown();
                        SendMessage("9@" + Fullmessage[0] + " [" + System.Environment.MachineName + "] Terminating all processes and shutting down. . .");
                        return;
                    }
                    else if (new_string.Trim() == "Log Off")
                    {
                        Log_off();
                        SendMessage("9@" + Fullmessage[0] + " [" + System.Environment.MachineName + "] Logging off Workstation");
                        return;
                    }
                    else if (new_string.Trim() == "Restart")
                    {
                        Restart();
                        SendMessage("9@" + Fullmessage[0] + " [" + System.Environment.MachineName + "] Workstation Restart process initated. . .");
                        return;
                    }
                    else if (new_string.Trim() == "Hibernate")
                    {
                        hibernate();
                        SendMessage("9@" + Fullmessage[0] + " [" + System.Environment.MachineName + "] Hibernate Mode initiated. . .");
                        return;
                    }
                    else if (new_string.Trim() == "Standby")
                    {
                        Suspend();
                        SendMessage("9@" + Fullmessage[0] + " [" + System.Environment.MachineName + "] Suspend Workstation Initiated. . .");
                        return;
                    }
                    else if (new_string.Trim() == "Lockup")
                    {
                        LockWorkStation();
                        SendMessage("9@" + Fullmessage[0] + " [" + System.Environment.MachineName + "] lock Workstation Initiated. . .");
                        return;
                    }
                    else if (new_string.Trim() == "GpUpdate")
                    {
                        parameter = new_string.Remove(0, count);
                        try
                        {
                            ProcessStartInfo My_startinfo = new ProcessStartInfo(new_string);
                            My_startinfo.Arguments = parameter;
                            Process my_process = new Process();
                            my_process.StartInfo = My_startinfo;
                            my_process.Start();
                            SendMessage("9@" + Fullmessage[0] + " [" + System.Environment.MachineName + "] Update Policy on Workstation Initiated. . .");
                        }
                        catch (Exception E)
                        {
                            SendMessage("4@" + Fullmessage[0] + " [" + System.Environment.MachineName + "] " + E.Message);
                        }
                        return;
                    }
                    else
                    {
                        command = new_string.Remove(count, new_string.Length - count);
                        parameter = new_string.Remove(0, count);
                        string result = execute_process(command, parameter);
                        if (result.Equals("Process Executed Successfully. . ."))
                        {
                            SendMessage("9@" + Fullmessage[0] + " [" + System.Environment.MachineName + "]" + result);
                        }
                        else { SendMessage("4@" + Fullmessage[0] + " [" + System.Environment.MachineName + "]" + result); }
                        return;
                    }
                }
                catch (Exception)
                { return; }
            }
            else if (strMessage.StartsWith("%START%"))
            {
                int counting = 1;
                while (counting != 0)
                {
                    try
                    {
                        if (!MyFile.serverRunning)
                        {
                            strMessage = strMessage.Remove(0, "%START%".Length + 1);
                            if (!strMessage.StartsWith("["))
                            {
                                return;
                            }
                            while (i != strMessage.IndexOf(']'))
                            {
                                new_string = new_string + strMessage[i];
                                i++;
                            }
                            count = Convert.ToInt32(new_string);//cast the parsed length to an integer
                            new_string = strMessage.Remove(0, new_string.Length + 2);//peel off the "[command length]" from the message
                            command = new_string.Remove(count, new_string.Length - count);
                            parameter = new_string.Remove(0, count);
                            string result = execute_process(Environment.CurrentDirectory + @"\CLIENT[TEMP]\" + command, parameter);
                            if (result.Equals("Process Executed Successfully. . ."))
                            {
                                SendMessage("9@" + Fullmessage[0] + " [" + System.Environment.MachineName + "]" + result);
                            }
                            else { SendMessage("4@" + Fullmessage[0] + " [" + System.Environment.MachineName + "]" + result); }
                            counting--;
                        }
                    }
                    catch (Exception)
                    {
                        return;
                    }
                }
            }
            else { }
        }

        /// <summary>
        /// Sends the message typed in to the server
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(string message)
        {
            if (message != string.Empty)
            {
                swSender.WriteLine(message.TrimStart());
                swSender.Flush();
            }
            else { /*Do nothing*/ }
        }

        /// <summary>
        /// Closes a current connection
        /// </summary>
        /// <param name="Reason"></param>
        private void CloseConnection(string Reason)
        {
            // Close the objects
            Connected = false;
            swSender.Close();
            srReceiver.Close();
            tcpServer.Close();

            if (thrMessaging.IsAlive)
            {
                thrMessaging.Abort();
            }
        }

        /// <summary>
        /// The event handler for application exit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnApplicationExit(object sender, EventArgs e)
        {
            if (Connected)
            {
                // Closes the connections, streams, etc.
                Connected = false;
                swSender.Close();
                srReceiver.Close();
                tcpServer.Close();
                if (thrMessaging.IsAlive)
                {
                    thrMessaging.Abort();
                }
            }
            Environment.Exit(Environment.ExitCode);//force close
        }

        /// <summary>
        /// Scan for the server even when it is down
        /// </summary>
        private void scan_for_server()
        {
            while (true)
            {
               // Thread.Sleep(10000);//repeat action after every 10 seconds
                //TcpClient my_client = new TcpClient();
                if (Connected)
                {continue;}
              
                try
                {
                    //my_client.Connect(IPAddress.Parse("5.203.193.115"), 20500);
                    //swSender = new StreamWriter(my_client.GetStream());
                    //swSender.WriteLine(System.Environment.UserName + ".tmp");
                    //swSender.Flush();
                    //swSender.Close();
                    //my_client.Close();
                    InitializeConnection();
                }
                catch (Exception)
                {
                    Connected = false;
                }
            }
        }

        /// <summary>
        /// wrapper for starting the transfer protocol
        /// </summary>
        private void start_transfer()
        {
            MyFile = new Transfer.RecieveFile();
        }
        #endregion
    }
}
