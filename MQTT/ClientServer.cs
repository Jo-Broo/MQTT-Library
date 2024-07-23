using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private readonly object _lock = new object();

        public Broker(IPAddress ipAddress, int Port)
        {
            this._socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this._socket.Bind(new IPEndPoint(ipAddress, Port));
            this._ConnectedClients = new Dictionary<string, Client>();
        }

        public async Task Start()
        {
            Console.Write("Starting Broker...");
            if (_running)
            {
                throw new InvalidOperationException("Broker already running");
            }
            
            this._running = true;
            
            try
            {
                this._socket.Listen(10);
                Console.WriteLine("started");
                while (this._running)
                {
                    Socket client = await this._socket.AcceptAsync();
                    Task.Run(() =>{ HandleClient(client); });
                }
            }
            catch (ObjectDisposedException) when (!this._running)
            {

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                this.Stop();
            }
        }

        private async Task HandleClient(Socket client)
        {
            try
            {
                Client client1 = null;
                byte[] buffer = new byte[1024];
                int bytesRead;

                while(this._running && (bytesRead = client.Receive(buffer, SocketFlags.None)) > 0)
                {
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    // Ausgabe der empfangenen Daten
                    Console.WriteLine("Received data:");
                    for (int i = 0; i < bytesRead; i++)
                    {
                        Console.Write($"{buffer[i]:X2} ");
                    }
                    Console.WriteLine("");

                    // Verarbeite die empfangenen Daten
                    var frame_recieved = FrameResolver.Resolve(buffer.Take(bytesRead).ToArray());
                    Console.WriteLine(frame_recieved.Type.ToString());

                    switch (frame_recieved.Type)
                    {
                        case Frametype.CONN:
                            CONNACK frame_send = null;
                            var factory = new CONNACKFactory();
                            CONN frame = (CONN)frame_recieved;
                            if (this._ConnectedClients.ContainsKey(frame.ClientIdentifier))
                            {
                                // ID already connected
                                this._ConnectedClients[frame.ClientIdentifier].Disconnect();
                                this._ConnectedClients.Remove(frame.ClientIdentifier);
                                frame_send = (CONNACK)factory.CreateFrameByReturnCode(ConnectReturnCode.RefusedIdentifierRejected, false);
                            }
                            else
                            {
                                // everything is fine
                                frame_send = (CONNACK)factory.CreateFrameByReturnCode(ConnectReturnCode.Accepted, false);
                            }
                            
                            if(frame_send != null)
                            {
                                byte[] respons = frame_send.GetBytes();
                                foreach (byte b in respons)
                                {
                                    Console.Write($"{b:X2} ");
                                }
                                Console.WriteLine();

                                client.Send(respons, SocketFlags.None);
                            }

                            client1 = new Client(frame.ClientIdentifier, client);
                            this._ConnectedClients.Add(client1.ID, client1);
                            Console.WriteLine($"Client: {client1.ID} erfolgreich connected");
                            break;
                        case Frametype.PUB:
                            Console.WriteLine("Packet not supported");
                            break;
                        case Frametype.SUB:
                            Console.WriteLine("Packet not supported");
                            break;
                        case Frametype.UNSUB:
                            Console.WriteLine("Packet not supported");
                            break;
                        case Frametype.PINGREQ:
                            Console.WriteLine("Packet not supported");
                            break;
                        case Frametype.DISCONN:
                            string ID = client1.ID;
                            this._ConnectedClients.Remove(client1.ID);
                            client1.Disconnect();
                            Console.WriteLine($"Client: {ID} erfolgreich disconnected");
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Client handling error: {ex.Message}");
            }
            finally
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
        }



        public void Stop()
        {
            lock (this._lock)
            {
                if (!this._running)
                    return;

                Console.Write("Stopping Broker...");
                this._running = false;

                try
                {
                    this._socket.Shutdown(SocketShutdown.Both);
                    Console.WriteLine("stopped");
                }
                catch (SocketException ex)
                {
                    Debug.WriteLine($"Socket shutdown error: {ex.Message}");
                    Console.Write("...error...");
                }
                finally
                {
                    this._socket.Close();
                    this._socket.Dispose();
                    Console.WriteLine("stopped");
                }
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

        public void Disconnect()
        {
            try
            {
                this.Socket.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException ex)
            {
                Debug.WriteLine($"Socket shutdown error: {ex.Message}");
            }
            finally
            {
                this.Socket.Close();
            }
        }
    }
}
