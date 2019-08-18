using RatBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

                // send a packet
                byte packet_header = 0x02;
                byte[] payload = Encoding.UTF8.GetBytes("Hello there, server");
                client.Output.Write(packet_header);
                client.Output.Write(payload.Length);
                client.Output.Write(payload);

                packet_header = 0x35;
                int payload_size = 3145728;
                client.Output.Write(packet_header);
                client.Output.Write(payload_size);
                byte[] file = new byte[payload_size];
                new Random().NextBytes(file);
                client.Output.Write(file);
            }
        }
    }
}
