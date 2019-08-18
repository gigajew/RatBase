using RatBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Server
{
    class Program
    {
        static RatServer server = new RatServer();
        static List<RatClient> clients = new List<RatClient>();
        static void Main(string[] args)
        {
            server.ClientConnected += Server_ClientConnected;
            server.Listen(8850);

            Console.Read();
        }

        private static void Server_ClientConnected(RatServer sender, RatClient client)
        {
            Console.WriteLine("A new client has connected!");

            // subscribe to clients events
            client.PacketHeaderReceived += Client_PacketHeaderReceived;
            client.StateChanged += Client_StateChanged;

            // initialize client, must happen after events are subscribed to
            client.Start();

            // add client to list
            lock (clients )
            {
                clients.Add(client);
            }
        }

        private static void Client_StateChanged(RatClient sender, bool connected, string reason)
        {
            Console.WriteLine("Connected: {0}, Reason: {1}", connected, reason);

            // remove client from list
            if(!connected )
            {
                lock(clients )
                {
                    if(clients.Contains(sender))
                    {
                        clients.Remove(sender);
                    }
                }
            }
        }

        private static void Client_PacketHeaderReceived(RatClient sender, int value)
        {
            if( value == 0x02)
            {
                // read a packet
                int message_length = sender.Input.ReadInt32 ();
                byte[] message_raw = sender.Input.ReadBytes(message_length );

                Console.WriteLine("Client said: {0}", Encoding.UTF8.GetString(message_raw));
            } else if (value == 0x35)
            {
                int length = sender.Input.ReadInt32();
                using (FileStream stream = new FileStream("output.bin", FileMode.Create, FileAccess.ReadWrite))
                {
                    byte[] buffer = sender.Input.ReadBytes(length);
                    stream.Write(buffer, 0, buffer.Length);
                    buffer = null;
                }
                Console.WriteLine("Received a file of size: {0}mB", length / 1024/1024);
         
            }
        }
    }
}
