using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace RatBase
{
    public class RatClient : IDisposable 
    {
        public event DStateChanged StateChanged;
        public delegate void DStateChanged(RatClient  sender, bool connected, string reason);

        public event DPacketHeaderReceived PacketHeaderReceived;
        public delegate void DPacketHeaderReceived(RatClient sender, int value);

        public RatClient() : base() { }
        public RatClient (Socket old_socket) { socket = old_socket; }

        #region "Public methods" 
        public void Connect(string host, int port)
        {
            if (socket == null)
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.BeginConnect(host, port, new AsyncCallback(ConnectCallback), this);
        }
        public void Disconnect()
        {
            if (socket == null)
                return;
            socket.BeginDisconnect(true, new AsyncCallback(DisconnectCallback ), this);
        }
        public void Start()
        {
            if (socket == null)
                return;

            if (!socket.Connected)
                return;

            Stream = new NetworkStream(this.socket);
            Input = new BinaryReader(Stream, Encoding.UTF8 );
            Output = new BinaryWriter(Stream, Encoding.UTF8);
            read = new Thread(() => {
                do
                {
                    bool canread = this.socket.Poll(3000, SelectMode.SelectRead) && this.socket.Available > 0;
                    int packet_magic_header = 0;
                    //if(!canread )
                    //{
                    //    OnStateChanged(false, "Disconnected");
                    //    return;
                    //}
                    try
                    {
                        packet_magic_header  = Input .ReadByte();
                    } catch (IOException  e)
                    {
                        OnStateChanged(false, "Disconnected: "+ e.Message);
                        return;
                    }
                    if (packet_magic_header == 0)
                    {
                        OnStateChanged(false, "The connection was aborted (zerobyte/disconnect)");
                        return;
                    }
                    if (packet_magic_header == -1)
                    {
                        OnStateChanged(false, "The connection was terminated (end of stream)");
                        return;
                    }
                    OnPacketHeaderReceived(packet_magic_header);
                } while (true);
            });
            read.IsBackground = true;
            read.Start();
        }
        //public NetworkStream GetStream()
        //{
        //    return Stream;
        //}
        #endregion

        #region "Event handlers"
        protected void OnStateChanged(bool connected, string reason)
        {
            StateChanged?.Invoke(this, connected, reason);
        }
        protected void OnPacketHeaderReceived(int value )
        {
            PacketHeaderReceived?.Invoke(this, (byte)value);
        }
        #endregion

        #region "Callbacks"
        protected static void ConnectCallback(IAsyncResult ar)
        {
            RatClient context = (RatClient)ar.AsyncState;
            try
            {
                context.socket.EndConnect(ar);
                if (context.socket.Connected)
                {
                    context.OnStateChanged(true, "Successfully connected to server");
                }

            } catch (SocketException e)
            {
                context.OnStateChanged(false, "Failed to connect to server: " + e.Message );
            }
        }
        protected static void DisconnectCallback(IAsyncResult ar)
        {
            RatClient context = (RatClient)ar.AsyncState;
            try
            {
                context.socket.EndDisconnect(ar);
                
            } catch (SocketException e)
            {
                // not sure
            }
            context.OnStateChanged(false, "Successfully disconnected from server");
        }
        #endregion

        #region "Disposable Pattern"
        public void Dispose()
        {
            Input?.Dispose();
            Output?.Dispose();
            Stream?.Dispose();
            socket?.Dispose();
        }
        #endregion

        #region "Fields"
        public NetworkStream Stream;
        public BinaryWriter Output;
        public BinaryReader Input;
        private Socket socket;
        private Thread read;
        #endregion 

    }
}
