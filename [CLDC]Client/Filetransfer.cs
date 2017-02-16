using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.IO.Compression;

namespace Transfer
{
    public class RecieveFile : Form
    {
        private delegate void restart();

        #region VARIABLES
        private Thread thread1;
        private int flag = 0;
        private string receivedPath = null;
        public bool serverRunning;
        #endregion

        #region CONSTRUCTOR
        /// <summary>
        /// This is the constructor for the receive file class
        /// the Target parameter holds the location of the recieved file,
        /// and the Execute parameter, tells the program whether or not to execute the 
        /// the file after copying it, the sender parameter allows for return of messages to the 
        /// sender of the file
        /// </summary>
        /// <param name="Target"></param>
        /// <param name="Execute"></param>
        public RecieveFile()
        {
            Directory.CreateDirectory(Environment.CurrentDirectory + @"\CLIENT[TEMP]").Attributes = FileAttributes.Hidden;//This would make the folder hidden
            thread1 = new Thread(new ThreadStart(StartListening_));
            thread1.Start();
        }

        #endregion

        #region METHODS
        /// <summary>
        /// This method is called by the initial thread to start 
        /// listening for incoming packets
        /// </summary>
        public void StartListening_()
        {
            byte[] bytes = new Byte[20480];
            IPEndPoint ipEnd = new IPEndPoint(IPAddress.Any, 20501);
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                listener.Bind(ipEnd);
                listener.Listen(100);
                while (true)
                {
                    allDone.Reset();
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                    allDone.WaitOne();
                }
            }
            catch (Exception E)
            {
                //TODO send message the sender saying file transfer failed
                MessageBox.Show(E.Message);
            }
        }

        public class StateObject
        {
            // Client socket.
            public Socket workSocket = null;
            public const int BufferSize = 20480;
            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];
        }

        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public void AcceptCallback(IAsyncResult ar)
        {
            allDone.Set();
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            new AsyncCallback(ReadCallback), state);
            flag = 0;
        }

        public void ReadCallback(IAsyncResult ar)
        {
            int fileNameLen = 1;
            String content = String.Empty;
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            int bytesRead = handler.EndReceive(ar);
            if (bytesRead > 0)
            {
                serverRunning = true;
                if (flag == 0)
                {
                    fileNameLen = BitConverter.ToInt32(state.buffer, 0);
                    string fileName = Encoding.UTF8.GetString(state.buffer, 4, fileNameLen);
                    receivedPath = Environment.CurrentDirectory + @"\CLIENT[TEMP]\" + fileName;
                    flag++;
                }
                if (flag >= 1)
                {
                    BinaryWriter writer = new BinaryWriter(File.Open(receivedPath, FileMode.Append));
                    if (flag == 1)
                    {
                        writer.Write(state.buffer, 4 + fileNameLen, bytesRead - (4 + fileNameLen));
                        flag++;
                    }
                    else
                        writer.Write(state.buffer, 0, bytesRead);
                    writer.Close();
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
                }
            }
            else
            {
                try
                {
                    FileInfo Fileinfo_ = new FileInfo(receivedPath);
                    Decompress(Fileinfo_);
                    File.Delete(Fileinfo_.FullName);
                }catch(Exception)
                {}
                serverRunning = false;
            }
        }

        private void Restart()
        {
            thread1.Abort();
            thread1.Start();
        }
        /// <summary>
        /// Algorithm to decpompress file types
        /// </summary>
        /// <param name="fi"></param>
        public static void Decompress(FileInfo fi)
        {
            // Get the stream of the source file.
            using (FileStream inFile = fi.OpenRead())
            {
                // Get original file extension, for example
                // "doc" from report.doc.gz.
                string curFile = fi.FullName;
                string origName = curFile.Remove(curFile.Length -
                        fi.Extension.Length);

                //Create the decompressed file.
                using (FileStream outFile = File.Create(origName))
                {
                    using (GZipStream Decompress = new GZipStream(inFile,
                            CompressionMode.Decompress))
                    {
                        byte[] Buffer = new byte[inFile.Length];
                        int counter;
                        while ((counter = Decompress.Read(Buffer, 0, Buffer.Length)) != 0)
                        {
                            outFile.Write(Buffer, 0, counter);
                        }
                    }
                }
            }
        }
        #endregion
    }
}
