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
        static Chat chat;

        static void Main(string[] args)
        {
            if (Console.ReadKey().Key == ConsoleKey.H)
                args = new string[1] { "-h" };
            if (args.Length > 0)
            {
                if (args[0] == "-h")
                    chat = new Host();
                else if (args[0] == "-c")
                    chat = new Client();
                else return;
            }
            else
                chat = new Client();
            chat.Run();
        }
    }
}