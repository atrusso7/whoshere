using System;
using System.Diagnostics;
using System.Threading;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using Figgle;

namespace whoshere
{
    class Program
    {
        static CountdownEvent countdown;
        static int upCount = 0;
        static object lockObj = new object();
        const bool resolveNames = true;
        public static IEnumerable<string> GetAddresses()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return (from ip in host.AddressList where ip.AddressFamily == AddressFamily.InterNetwork select ip.ToString()).ToList();
        }
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(FiggleFonts.SubZero.Render("who here"));
            Console.ResetColor();
            int c = 1;
            Console.WriteLine("Select IP to scan from: ");
            foreach (string ip in GetAddresses())
            { 
                Console.WriteLine(c + ". " + ip);
                c++;
            }
            int a = Convert.ToInt32(Console.ReadLine()) - 1;
            var iplist = GetAddresses(); 
            var selectedIP = iplist.ElementAt(a);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[Scanning neighbors of " + selectedIP + "]");
            Console.ResetColor();
            string ipBase = selectedIP.Substring(0, selectedIP.LastIndexOf(".") + 1);
            countdown = new CountdownEvent(1);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            for (int i = 1; i < 255; i++)
            {
                string ip = ipBase + i.ToString();

                Ping p = new Ping();
                p.PingCompleted += new PingCompletedEventHandler(p_PingCompleted);
                countdown.AddCount();
                p.SendAsync(ip, 100, ip);
            }
            countdown.Signal();
            countdown.Wait();
            sw.Stop();
            TimeSpan span = new TimeSpan(sw.ElapsedTicks);
            Console.WriteLine("Took {0} milliseconds. {1} hosts active.", sw.ElapsedMilliseconds, upCount);
            Console.ReadLine();
        }

        static void p_PingCompleted(object sender, PingCompletedEventArgs e)
        {
            string ip = (string)e.UserState;
            if (e.Reply != null && e.Reply.Status == IPStatus.Success)
            {
                Console.WriteLine("{0} is up: ({1} ms)", ip, e.Reply.RoundtripTime);
                lock (lockObj)
                {
                    upCount++;
                }
            }
            else if (e.Reply == null)
            {
                Console.WriteLine("Pinging {0} failed. (Null Reply object?)", ip);
            }
            countdown.Signal();
        }
    }
}
