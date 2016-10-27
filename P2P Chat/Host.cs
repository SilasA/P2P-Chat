using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace P2P_Chat
{
    /// <summary>
    /// 
    /// </summary>
    class State
    {
        // Client  socket.
        public Socket workSocket = null;
        // Receive buffer.
        public byte[] buffer = new byte[Chat.MAX_CHAR];

        public string username = "";

        public bool Usable => username.Length > 0;

        public State(Socket workSocket)
        {
            this.workSocket = workSocket;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    class Host : Chat
    {
        List<State> sCLients;
        //List<IPAddress> ipCLients;

        private int Top => sCLients.Count - 1;

        private Thread listenThread;
        private Mutex clientLock;

        public ManualResetEvent allDone = new ManualResetEvent(false);

        /// <summary>
        /// 
        /// </summary>
        public Host() : base()
        {
            name = GetRoomName();

            ipLocal = GetLocalIp();
            epLocal = new IPEndPoint(ipLocal, port);
            sCLients = new List<State>();
            clientLock = new Mutex();
        }
        ~Host()
        {
            listenThread.Abort();
        }

        /// <summary>
        /// Asynchronously listens for clients to connect. 
        /// </summary>
        private void Listen()
        {
            Socket listener = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            try
            {
                listener.Bind(epLocal);
                listener.Listen(100);

                while (true)
                {
                    allDone.Reset();

                    State state = new State(listener);

                    // Start an asynchronous socket to listen for connections.
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        state);

                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ar"></param>
        private void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            allDone.Set();

            // Get the socket that handles the client request.
            Socket listener = ((State)ar.AsyncState).workSocket;
            Socket handler = listener.EndAccept(ar);

            clientLock.WaitOne();
            // Create the state object.
            sCLients.Add(new State(handler));

            handler.BeginReceive(sCLients[Top].buffer, 0, MAX_CHAR, 0,
                new AsyncCallback(ReadCallback), sCLients[Top]);
            clientLock.ReleaseMutex();

            SendMsg("$name=" + name + "<EOF>", false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ar"></param>
        private void ReadCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            State state = (State)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket. 
            int bytesRead = handler.EndReceive(ar);
            string message = "";

            if (bytesRead > 0)
            {
                // There might be more data, so store the data received so far.
                message += Encoding.ASCII.GetString(state.buffer, 0, bytesRead);

                // Check for end-of-file tag. If it is not there, read 
                // more data.
                if (message.IndexOf("<EOF>") > -1)
                {
                    message = message.Remove(message.IndexOf("<EOF>"));
                    // Find metadata
                    if (message.StartsWith("$"))
                    {
                        message = message.Substring(0, 1);
                        string[] substr = message.Split('=');
                        switch (substr[0])
                        {
                            case "name":
                                state.username = substr[1];
                                break;
                            default:
                                break;
                        }
                    }
                    else 
                        SendMsg(message);
                }
                else
                {
                    // Not all data received. Get more.
                    handler.BeginReceive(state.buffer, 0, MAX_CHAR, 0,
                        new AsyncCallback(ReadCallback), state);
                }
            }
        }

        /// <summary>
        /// Prompts the host for a chat room name.
        /// </summary>
        /// <returns></returns>
        private string GetRoomName()
        {
            Console.Write("Chat Room Name: ");
            return Console.ReadLine();
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Run()
        {
            listenThread = new Thread(Listen);
            listenThread.Start();

            poll.Start();

            while (true)
            {
                string line;
                if (chat.Count > 15) chat.RemoveAt(0);
                if (isTyping)
                {
                    line = Console.ReadLine();

                    // Commands
                    if (line.Length > 0 && line.ToCharArray()[0] == '/')
                    {
                        line = line.Remove(0, 1);
                        if (line.ToUpper() == "EXIT")
                            break;
                        else if (line.ToUpper() == "CLEAR")
                            chat.Clear();
                    }

                    SendMsg(Format(name, line));
                    mutex.WaitOne();
                    isTyping = false;
                    mutex.ReleaseMutex();
                }
                Draw();
                Thread.Sleep(500);
            }

            poll.Abort();
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void Draw()
        {
            Console.Clear();
            Console.WriteLine("Server IP: " + ipLocal.ToString());
            base.Draw();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        protected override void SendMsg(string message, bool inChat = true)
        {
            byte[] msg = Encoding.ASCII.GetBytes(message);
            if (!inChat) chat.Add(message);

            clientLock.WaitOne();
            foreach (State s in sCLients)
            {
                // Begin sending the data to the remote device.
                s.workSocket.BeginSend(msg, 0, msg.Length, 0,
                    new AsyncCallback(SendCallback), s);
            }
            clientLock.ReleaseMutex();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ar"></param>
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
