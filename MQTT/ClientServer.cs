using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MQTT
{
    public class Broker
    {
        private Socket _socket;
        private bool _running;
        public bool IsRunning { get { return _running; } }

        private Dictionary<string, Client> _ConnectedClients;

        public Broker(IPAddress ipAddress, int Port)
        {
            this._socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this._socket.Bind(new IPEndPoint(ipAddress, Port));
            this._ConnectedClients = new Dictionary<string, Client>();
        }

        public async Task Start()
        {
            try
            {
                this._socket.Listen(10);

                while (true)
                {
                    Socket client = await this._socket.AcceptAsync();
                    _ = HandleClient(client);
                    this._running = true;
                }
            }
            catch (Exception)
            {
                this._running = false;
            }
        }

        private async Task HandleClient(Socket client)
        {
            byte[] buffer = new byte[1024];

            int bytesRead = await client.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
            if (bytesRead > 0) 
            {
                System.Diagnostics.Debug.WriteLine(FrameResolver.Resolve(buffer).Type.ToString());
            }
        }
    }

    class Client
    {
        public string ID { get; private set; }
        public Socket Socket { get; private set; }

        public Client(string ID, Socket socket) 
        {
            this.ID = ID;
            this.Socket = socket;
        }
    }
}
