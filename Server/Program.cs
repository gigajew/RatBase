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
            lock (clients)
            {
                clients.Add(client);
            }
        }

        private static void Client_StateChanged(RatClient sender, bool connected, string reason)
        {
            Console.WriteLine("Connected: {0}, Reason: {1}", connected, reason);

            // remove client from list
            if (!connected)
            {
                lock (clients)
                {
                    if (clients.Contains(sender))
                    {
                        clients.Remove(sender);
                    }
                }
            }
        }

        private static void ReadFile(RatClient sender)
        {
            byte[] buffer = new byte[8192];
            int r = 0;
            int tot = 0;

            string full_filename = sender.Input.ReadString();
            string filename = Path.GetFileName(full_filename);
            long file_size = sender.Input.ReadInt64();

            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.Read ))
            {
                while ((r = sender.Input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fs.Write(buffer, 0, r);
                    tot += r;

                    if (tot == file_size)
                        break;
                }
            }

            Console.WriteLine("Successfully received a file: {0}, Size: {1}kB", filename, (decimal)file_size / 1024m);

        }

        private static void ReadMessage(RatClient sender)
        {
           string msg=  sender.Input.ReadString();
            Console.WriteLine(msg);
        }

        private static void Client_PacketHeaderReceived(RatClient sender, int value)
        {
            switch (value)
            {
                case 0x58:
                    ReadMessage(sender);
                    break;
                case 0x48:
                    ReadFile(sender);
                    break;
            }
        }
    }
}
