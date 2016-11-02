using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
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

        Logger logger;

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

            logger = new Logger();
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

            SendMD("$name=" + name + "<EOF>");
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
                    // Find metadata
                    if (message.StartsWith("$"))
                    {
                        message = message.Substring(0, 1).Remove(message.IndexOf("<EOF>"));
                        string[] substr = message.Split('=');
                        switch (substr[0])
                        {
                            case "name":
                                state.username = substr[1];
                                logger.WriteIP(state.username, ((IPEndPoint)state.workSocket.RemoteEndPoint).Address.ToString());
                                SendMsg(Format("[Server]", state.username + " has joined the chat!"));
                                break;
                            case "discon":
                                SendMsg(Format("[Server]", state.username + " has disconnected!"));
                                clientLock.WaitOne();
                                sCLients.Remove(state);
                                clientLock.ReleaseMutex();
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
            // Not all data received. Get more.
            handler.BeginReceive(state.buffer, 0, MAX_CHAR, 0,
                new AsyncCallback(ReadCallback), state);
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
                if (chat.Count > MAX_LINES) chat.RemoveAt(0);
                mutex.WaitOne();
                if (isTyping)
                {
                    line = Console.ReadLine();

                    // Commands
                    if (line.Length > 0 && line.ToCharArray()[0] == '/')
                    {
                        line = line.Remove(0, 1);
                        line.ToUpper();
                        if (line.ToUpper().Contains("EXIT"))
                        {
                            clientLock.WaitOne();
                            foreach (State s in sCLients)
                                Disconnect(s);
                            clientLock.ReleaseMutex();
                            break;
                        }
                        else if (line.ToUpper().Contains("CLEAR"))
                        {
                            chatLock.WaitOne();
                            chat.Clear();
                            chatLock.ReleaseMutex();
                        }
                        else if (line.ToUpper().Contains("DISCON"))
                        {
                            string[] arg = line.Split('=');
                            if (arg.Length > 1)
                            {
                                clientLock.WaitOne();
                                foreach (State s in sCLients)
                                {
                                    if (arg[1] == s.username) Disconnect(s);
                                }
                                clientLock.ReleaseMutex();
                            }
                        }
                    }
                    else
                    {
                        SendMsg(Format(name, line));
                        mutex.WaitOne();
                        isTyping = false;
                        mutex.ReleaseMutex();
                    }
                }
                mutex.ReleaseMutex();
                Draw();
                Thread.Sleep(500);
            }

            poll.Abort();

            while (true)
            {
                clientLock.WaitOne();
                if (sCLients.Count == 0) return; //Environment.Exit(0);
                clientLock.ReleaseMutex();
                Thread.Sleep(250);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void Draw()
        {
            Console.Clear();
            Console.WriteLine("Server IP: " + ipLocal.ToString());
            Console.WriteLine("Room Name: " + name);
            base.Draw();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        protected override void SendMsg(string message)
        {
            byte[] msg = Encoding.ASCII.GetBytes(message);
            message = message.Remove(message.IndexOf("<EOF>"));
            chatLock.WaitOne();
            chat.Add(message);
            chatLock.ReleaseMutex();

            clientLock.WaitOne();
            foreach (State s in sCLients)
            {
                // Begin sending the data to the remote device.
                s.workSocket.BeginSend(msg, 0, msg.Length, 0,
                    new AsyncCallback(SendCallback), s);
            }
            clientLock.ReleaseMutex();

            logger.WriteLine(message);
        }

        protected override void SendMD(string message)
        {
            byte[] msg = Encoding.ASCII.GetBytes(message);

            clientLock.WaitOne();
            foreach (State s in sCLients)
            {
                // Begin sending the metadata to the remote device.
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
                Socket handler = ((State)ar.AsyncState).workSocket;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void Disconnect(State user)
        {
            user.workSocket.Send(Encoding.ASCII.GetBytes("$discon<EOF>"));
        }
    }
}
