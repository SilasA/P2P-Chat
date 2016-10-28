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
    // Crashes after host connection lost

    class Client : Chat
    {
        EndPoint epHost;
        IPAddress ipHost;
        Socket socket;

        string hostName;

        public ManualResetEvent connected = new ManualResetEvent(false);

        /// <summary>
        /// Prompts the user for the peer IP address.
        /// </summary>
        /// <returns></returns>
        private IPAddress GetHostIp()
        {
            Console.Write("Host IP Address: ");
            return IPAddress.Parse(Console.ReadLine());
        }

        /// <summary>
        /// Prompts the user for a username.
        /// </summary>
        /// <returns></returns>
        private string GetUserName()
        {
            Console.Write("Username: ");
            return Console.ReadLine();
        }

        /// <summary>
        /// 
        /// </summary>
        public Client() : base()
        {
            socket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
            socket.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                true);

            name = GetUserName();

            ipLocal = GetLocalIp();
            Console.WriteLine(ipLocal);
            epLocal = new IPEndPoint(ipLocal, port);
            socket.Bind(epLocal);

            ipHost = GetHostIp();
            epHost = new IPEndPoint(ipHost, port);
            Console.Clear();

            mutex = new Mutex();
            poll = new Thread(IsTypeMsg);

            // Connect to the remote endpoint.
            socket.BeginConnect(epHost,
                new AsyncCallback(ConnectCallback), socket);
            connected.WaitOne();
            socket.Send(Encoding.ASCII.GetBytes("$name=" + name));
        }

        public override void Run()
        {
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
                Receive();
                Draw();
                Thread.Sleep(500);
            }

            poll.Abort();
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            // Retrieve the socket from the state object.
            Socket client = (Socket)ar.AsyncState;

            // Complete the connection.
            client.EndConnect(ar);
            connected.Set();
        }

        private void Receive()
        {
            byte[] buffer = new byte[MAX_CHAR];
            socket.BeginReceive(buffer, 0, MAX_CHAR, 0,
                new AsyncCallback(ReceiveCallback), buffer);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the client socket 
            // from the asynchronous state object.
            byte[] msg = (byte[])ar.AsyncState;

            // Read data from the remote device.
            string message = Encoding.ASCII.GetString(msg);

            if (message.IndexOf("<EOF>") < -1)
            {
                // Get the rest of the data.
                socket.BeginReceive(msg, 0, MAX_CHAR, 0,
                    new AsyncCallback(ReceiveCallback), msg);
            }
            else
            {
                // All the data has arrived; put it in response.
                if (msg.Length > 0)
                {
                    message = message.Remove(message.IndexOf("<EOF>"));
                    // Find metadata
                    if (message.StartsWith("$"))
                    {
                        message = message.Remove(message.IndexOf('$'), 1);
                        string[] substr = message.Split('=');
                        switch (substr[0])
                        {
                            case "name":
                                hostName = substr[1];
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        chat.Add(message);
                    }
                }
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            // Retrieve the socket from the state object.
            Socket client = (Socket)ar.AsyncState;

            client.EndSend(ar);
        }

        protected override void Draw()
        {
            Console.Clear();
            Console.WriteLine("Room Name: " + hostName);
            base.Draw();
        }

        /// <summary>
        /// Sends a message to the host.
        /// </summary>
        /// <param name="message">Formatted string to send</param>
        protected override void SendMsg(string message)
        {
            // User has joined
            byte[] msg = new byte[MAX_CHAR];
            msg = Encoding.ASCII.GetBytes(message);
            // Begin sending the data to the remote device.
            socket.BeginSend(msg, 0, msg.Length, 0,
                new AsyncCallback(SendCallback), socket);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        protected override void SendMD(string message)
        {
            SendMsg(message);
        }
    }
}
