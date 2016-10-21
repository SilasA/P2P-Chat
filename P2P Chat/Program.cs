using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


namespace P2P_Chat
{
    class Program
    {
        static List<string> chat;

        static Socket socket;
        static EndPoint epLocal, epPeer;
        static IPAddress ipLocal, ipPeer;
        static int port = 666;
        static string username;

        static bool isTyping = false;

        static Mutex mutex;
        static Thread poll;

        /// <summary>
        /// Thread to monitor if the user is typing.
        /// </summary>
        private static void IsTypeMsg()
        {
            while (true)
            {
                Console.ReadKey();
                mutex.WaitOne();
                isTyping = true;
                mutex.ReleaseMutex();
                Thread.Sleep(500);
            }
        } 

        static void Main(string[] args)
        {
            chat = new List<string>();
            socket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Dgram,
                ProtocolType.Udp);
            socket.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                true);

            username = GetUserName();

            ipLocal = GetLocalIp();
            Console.WriteLine(ipLocal);
            epLocal = new IPEndPoint(ipLocal, port);
            socket.Bind(epLocal);

            ipPeer = GetPeerIp();
            epPeer = new IPEndPoint(ipPeer, port);
            socket.Connect(epPeer);

            Console.Clear();

            mutex = new Mutex();
            poll = new Thread(IsTypeMsg);

            byte[] buffer = new byte[1500];
            socket.BeginReceiveFrom(buffer, 0, buffer.Length,
                SocketFlags.None, ref epPeer,
                new AsyncCallback(MessageCallBack), buffer);

            poll.Start();

            while (true)
            {
                string line;
                if (chat.Count > 15) chat.RemoveAt(0);
                if (isTyping)
                {
                    line = Console.ReadLine();
                    SendMsg(Format(username, line));
                    mutex.WaitOne();
                    isTyping = false;
                    mutex.ReleaseMutex();

                    // Commands
                    if (line.Length > 0 && line.ToCharArray()[0] == '/')
                    {
                        line = line.Remove(0, 1);
                        if (line.ToUpper() == "EXIT")
                            break;
                        else if (line.ToUpper() == "CLEAR")
                            chat.Clear();
                    }
                }
                Draw();
                Thread.Sleep(500);
            }

            poll.Abort();
        }

        /// <summary>
        /// Formats a message to be displayed/sent.
        /// </summary>
        /// <param name="user">User that created the message</param>
        /// <param name="message">Message content</param>
        private static string Format(string user, string message)
        {
            return user + ": " + message;
        }

        /// <summary>
        /// Adds an already formatted string to the chat.
        /// </summary>
        /// <param name="formattedMsg"></param>
        private static void Write(string formattedMsg)
        {
            chat.Add(formattedMsg);
        }

        /// <summary>
        /// Clears and re-draws connected users and chat.
        /// </summary>
        private static void Draw()
        {
            Console.Clear();
            Console.WriteLine("Connected:\n" + ipLocal + "\n" + ipPeer);
            Console.WriteLine(new string('=', 30));
            foreach (string s in chat)
                Console.WriteLine(s);
        }

        /// <summary>
        /// Finds thelocal IP address.
        /// </summary>
        /// <returns></returns>
        private static IPAddress GetLocalIp()
        {
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    if (ip.ToString().StartsWith("10")) return ip;   
                }
            }
            return new IPAddress(new byte[4]);
        }

        /// <summary>
        /// Prompts the user for the peer IP address.
        /// </summary>
        /// <returns></returns>
        private static IPAddress GetPeerIp()
        {
            Console.Write("Peer IP Address: ");
            return IPAddress.Parse(Console.ReadLine());
        }

        /// <summary>
        /// Prompts the user for a username.
        /// </summary>
        /// <returns></returns>
        private static string GetUserName()
        {
            Console.Write("Username: ");
            return Console.ReadLine();
        }

        /// <summary>
        /// Sends a message to peer client and adds message to chat.
        /// </summary>
        /// <param name="message">Formatted string to send</param>
        private static void SendMsg(string message)
        {
            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] msg = new byte[200];
            msg = ascii.GetBytes(message);

            socket.Send(msg);
            Write(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aResult"></param>
        private static void MessageCallBack(IAsyncResult aResult)
        {
            try
            {
                int size = socket.EndReceiveFrom(aResult, ref epPeer);
                if (size > 0)
                {
                    byte[] receivedData;
                    receivedData = (byte[])aResult.AsyncState;
                    ASCIIEncoding eEncoding = new ASCIIEncoding();
                    string receivedMsg = eEncoding.GetString(receivedData);
                    for (int i = 0; i < receivedData.Length; i++)
                    {
                        if (receivedData[i] == '\0')
                        {
                            receivedMsg = receivedMsg.Remove(i);
                            break;
                        }
                    }
                    Write(receivedMsg);
                }

                byte[] buffer = new byte[200];
                socket.BeginReceiveFrom(buffer, 0, buffer.Length,
                    SocketFlags.None, ref epPeer,
                    new AsyncCallback(MessageCallBack), buffer);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}