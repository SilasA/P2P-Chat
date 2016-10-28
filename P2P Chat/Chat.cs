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
    public class Chat
    {
        // CONST
        public const int port = 666;
        public const int MAX_CHAR = 300;

        protected List<string> chat;
        protected EndPoint epLocal;

        protected string name;

        protected IPAddress ipLocal;

        protected bool isTyping = false;
        protected bool showIPs = false;

        protected Mutex mutex;
        protected Thread poll;

        /// <summary>
        /// Thread to monitor if the user is typing.
        /// </summary>
        protected void IsTypeMsg()
        {
            while (true)
            {
                Console.ReadKey();
                mutex.WaitOne();
                isTyping = true;
                mutex.ReleaseMutex();
                Thread.Sleep(200);
            }
        }

        /// <summary>
        /// Finds the local IP address.
        /// </summary>
        /// <returns></returns>
        protected IPAddress GetLocalIp()
        {
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    if (ip.ToString().StartsWith("10")) return ip;
            }
            return new IPAddress(new byte[4]);
        }

        /// <summary>
        /// 
        /// </summary>
        public Chat()
        {
            chat = new List<string>();
            poll = new Thread(IsTypeMsg);
            mutex = new Mutex();
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Run()
        {
            
        }

        protected virtual void Draw()
        {
            Console.WriteLine(new string('=', 30));
            foreach (string s in chat)
                Console.WriteLine(s);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        protected virtual void SendMsg(string message)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        protected virtual void SendMD(string message)
        {

        }


        /// <summary>
        /// Formats a message to be displayed/sent.
        /// </summary>
        /// <param name="user">User that created the message</param>
        /// <param name="message">Message content</param>
        public static string Format(string user, string message)
        {
            return user + ": " + message + "<EOF>";
        }
    }
}
