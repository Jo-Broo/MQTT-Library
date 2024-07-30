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

        private Dictionary<string, Socket> _ConnectedClients;

        private readonly object _lock = new object();

        public Broker(IPAddress ipAddress, int Port)
        {
            this._socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this._socket.Bind(new IPEndPoint(ipAddress, Port));
            this._ConnectedClients = new Dictionary<string, Socket>();
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

        private async Task HandleClient(Socket ClientSocket)
        {
            try
            {
                string ClientIdentifier = "";
                byte[] FrameBuffer = new byte[1024];
                byte[] respons;
                int BytesRead;

                while(this._running && (BytesRead = ClientSocket.Receive(FrameBuffer, SocketFlags.None)) > 0)
                {
                    if (BytesRead == 0)
                    {
                        throw new Exception("The Client is Disconnected.");
                    }

                    // Ausgabe der empfangenen Daten
                    Console.WriteLine("Received data:");
                    for (int i = 0; i < BytesRead; i++)
                    {
                        Console.Write($"{FrameBuffer[i]:X2} ");
                    }
                    Console.WriteLine("");

                    // Verarbeite die empfangenen Daten
                    var RecievedFrame = FrameResolver.Resolve(FrameBuffer);
                    Console.WriteLine(RecievedFrame.Type.ToString());

                    switch (RecievedFrame.Type)
                    {
                        case Frametype.CONN:
                            CONNACK ResponseFrame = null;
                            var ResponseFrameFactory = new CONNACKFactory();
                            
                            if (this._ConnectedClients.ContainsKey(((CONN)RecievedFrame).ClientIdentifier))
                            {
                                // ID already connected
                                this._ConnectedClients[((CONN)RecievedFrame).ClientIdentifier].Shutdown(SocketShutdown.Both);
                                this._ConnectedClients[((CONN)RecievedFrame).ClientIdentifier].Close();
                                this._ConnectedClients.Remove(((CONN)RecievedFrame).ClientIdentifier);
                                ResponseFrame = (CONNACK)ResponseFrameFactory.CreateFrameByReturnCode(ConnectReturnCode.RefusedIdentifierRejected, false);
                            }
                            else
                            {
                                // everything is fine
                                ResponseFrame = (CONNACK)ResponseFrameFactory.CreateFrameByReturnCode(ConnectReturnCode.Accepted, false);
                            }
                            
                            if(ResponseFrame != null)
                            {
                                respons = ResponseFrame.GetBytes();
                                foreach (byte b in respons)
                                {
                                    Console.Write($"{b:X2} ");
                                }
                                Console.WriteLine();

                                ClientSocket.Send(respons, SocketFlags.None);
                            }

                            ClientIdentifier = ((CONN)RecievedFrame).ClientIdentifier;
                            this._ConnectedClients.Add(ClientIdentifier, ClientSocket);
                            Console.WriteLine($"Client: {ClientIdentifier} erfolgreich connected");
                            break;
                        case Frametype.PUB:
                            Console.WriteLine($"{((PUB)RecievedFrame).TopicName}: {((PUB)RecievedFrame).TopicMessage}");
                            break;
                        case Frametype.SUB:
                            Console.WriteLine("Packet not supported");
                            break;
                        case Frametype.UNSUB:
                            Console.WriteLine("Packet not supported");
                            break;
                        case Frametype.PINGREQ:
                            PINGRES PingRespond = null;
                            PINGRESFactory PingRespondFactory = new PINGRESFactory();
                            PingRespond = (PINGRES)PingRespondFactory.CreateFrame();
                            respons = PingRespond.GetBytes();
                            foreach (byte b in respons)
                            {
                                Console.Write($"{b:X2} ");
                            }
                            Console.WriteLine();
                            ClientSocket.Send(respons, SocketFlags.None);
                            break;
                        case Frametype.DISCONN:
                            this._ConnectedClients.Remove(ClientIdentifier);
                            if (ClientSocket.Connected)
                            {
                                ClientSocket.Shutdown(SocketShutdown.Both);
                                ClientSocket.Close();
                            }
                            Console.WriteLine($"Client: {ClientIdentifier} erfolgreich disconnected");
                            ClientIdentifier = "";
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
                ClientSocket.Shutdown(SocketShutdown.Both);
                ClientSocket.Close();
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
