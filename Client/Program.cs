using RatBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Client
{
    class Program
    {
        static RatClient client = new RatClient();
        static void Main(string[] args)
        {
            client.StateChanged += Client_StateChanged;
            client.PacketHeaderReceived += Client_PacketHeaderReceived;

            client.Connect("localhost", 8850);

            Console.In.Read();
        }

        private static  void SendFile(string filename )
        {
            byte[] buffer = new byte[8192];
            int r = 0;

            client.Output.Write((byte)0x48);
            client.Output.Write(filename);
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read , FileShare.Read))
            {
                client.Output.Write(fs.Length);
                while (( r= fs.Read(buffer, 0, buffer.Length )) > 0)
                {
                    client.Output.Write(buffer, 0, r);
                }
            }
        }
        private static void SendMessage(string message)
        {
            client.Output.Write((byte)0x58);
            client.Output.Write(message);
        }

        private static void Client_PacketHeaderReceived(RatClient  sender, int value)
        {
            Console.WriteLine("CLIENT: Received packet: " + value.ToString("x2"));
        }

        private static void Client_StateChanged(RatClient sender, bool connected, string reason)
        {
            Console.WriteLine("CLIENT: Connected: {0}, Reason: {1}", connected, reason);
            if(connected)
            {
                // initializes the client, must happen after a successful connect
                sender.Start();

                // send a couple of packets
                SendMessage("Hello there, I'm sending you a file now");
                SendFile("C:\\Windows\\system32\\calc.exe");
            }
        }
    }
}
