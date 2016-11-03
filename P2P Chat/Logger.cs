using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace P2P_Chat
{
    /// <summary>
    /// 
    /// </summary>
    public class Logger
    {
        StreamWriter log;
        StreamWriter ipLog;

        /// <summary>
        /// 
        /// </summary>
        public Logger()
        {
            Directory.CreateDirectory("./Logs/");
            Directory.CreateDirectory("./IP-Logs/");

            ipLog = new StreamWriter("./IP-Logs/IP-History.txt", true);

            log = new StreamWriter("./Logs/" + DateTime.Now.ToString("HH-mm-ss") + ".txt");
            log.WriteLine("===========Chat Room Started===========");
        }

        ~Logger()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="line"></param>
        public void WriteLine(string line)
        {
            log.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss.tt") + "]: " + line);
            log.Flush();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="ip"></param>
        public void WriteIP(string user, string ip)
        {
            ipLog.WriteLine(
                "[" + DateTime.Now.ToString("MM-dd-yyyy") + "] " +
                user + ": " + ip);
            ipLog.Flush();
        }
    }
}
