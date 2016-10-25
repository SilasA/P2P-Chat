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
    class Client : Chat
    {
        EndPoint epHost;
        IPAddress ipHost;
        Socket socket;

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
        /// Sends a message to the host.
        /// </summary>
        /// <param name="message">Formatted string to send</param>
        protected override void SendMsg(string message)
        {
            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] msg = new byte[MAX_CHAR];
            msg = ascii.GetBytes(message);
            socket.Send(msg);
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
            socket.Connect(epHost);

            Console.Clear();

            mutex = new Mutex();
            poll = new Thread(IsTypeMsg);

            byte[] buffer = new byte[1500];
            socket.BeginReceiveFrom(buffer, 0, buffer.Length,
                SocketFlags.None, ref epHost,
                new AsyncCallback(MessageCallback), buffer);
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
                        /*else if (line.ToUpper() == "CON")
                            showIPs = !showIPs;*/
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
        private void MessageCallback(IAsyncResult ar)
        {
            
        }

        protected override void Draw()
        {
            Console.Clear();
            base.Draw();
        }
    }
}
