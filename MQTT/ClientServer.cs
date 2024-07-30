using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Log;

namespace MQTT
{
    public class Broker
    {
        private Socket _socket;
        private bool _running;
        public bool IsRunning { get { return _running; } }

        private Dictionary<string, Socket> _ConnectedClients;

        private Dictionary<string, List<string>> Topics;

        private readonly object _lock = new object();

        private readonly string LogPath = @"C:\Logs";
        private readonly string LogFilename = "Broker.log";
        private Logger Logger;

        public Broker(IPAddress ipAddress, int Port)
        {
            this._socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this._socket.Bind(new IPEndPoint(ipAddress, Port));
            this._ConnectedClients = new Dictionary<string, Socket>();
            this.Logger = new Logger(this.LogPath, this.LogFilename, ConsoleOutput: true);
        }

        public async Task Start()
        {
            this.Logger.CreateLogEntry("Starting Broker",Logger.LogLevel.Debug,true);
            if (_running)
            {
                this.Logger.CreateLogEntry("Broker already running", Logger.LogLevel.Error, true);
                throw new InvalidOperationException("Broker already running");
            }

            this.Logger.CreateLogEntry("Setting the running Status of the Broker", Logger.LogLevel.Debug, true, true);
            this._running = true;
            
            try
            {
                this.Logger.CreateLogEntry("Start Listening", Logger.LogLevel.Info, true);
                this._socket.Listen(10);

                this.Logger.CreateLogEntry("Broker started", Logger.LogLevel.Info, true);
                while (this._running)
                {
                    Socket client = await this._socket.AcceptAsync();
                    Task.Run(() =>{ HandleClient(client); });
                }
            }
            catch (ObjectDisposedException odex) when (!this._running)
            {
                this.Logger.CreateLogEntry(odex.Message + " [Start()]", Logger.LogLevel.Error, true);
            }
            catch (Exception ex)
            {
                this.Logger.CreateLogEntry(ex.Message + " [Start()]", Logger.LogLevel.Error, true);
            }
            finally
            {
                this.Logger.CreateLogEntry("Stopping the Broker", Logger.LogLevel.Info, true);
                this.Stop();
            }
        }

        private async Task HandleClient(Socket ClientSocket)
        {
            this.Logger.CreateLogEntry("Start Handeling a Connection", Logger.LogLevel.Info, true);
            // indicates if the Client properly Disconnected
            bool ClientDisconnected = false;
            try
            {
                // Contains the ClientIdentifier of this Connection
                string ClientIdentifier = "";
                // Buffer for incoming Frames
                byte[] FrameBuffer = new byte[1024];
                // Buffer for outgoing Frames
                byte[] respons;
                // Number of bytes read
                int BytesRead;

                while(this._running == true && ClientSocket.Connected == true)
                {
                    BytesRead = ClientSocket.Receive(FrameBuffer, SocketFlags.None);

                    if (BytesRead == 0)
                    {
                        this.Logger.CreateLogEntry("0 bytes read from the connection", Logger.LogLevel.Warn, true);
                        
                        if(ClientIdentifier != "")
                        {
                            this.RemoveClient(ClientIdentifier);
                        }
                    }
                    this.Logger.CreateLogEntry("A Message was recieved", Logger.LogLevel.Info, true);

                    // Temporary Console Output of the recieved Frame
                    Console.WriteLine("Received data:");
                    for (int i = 0; i < BytesRead; i++)
                    {
                        Console.Write($"{FrameBuffer[i]:X2} ");
                    }
                    Console.WriteLine("");

                    // Resolving the Frame and outputting its type
                    var RecievedFrame = FrameResolver.Resolve(FrameBuffer);
                    this.Logger.CreateLogEntry($"Messagetype: [{RecievedFrame.Type}]", Logger.LogLevel.Info, true);

                    switch (RecievedFrame.Type)
                    {
                        case Frametype.CONN:
                            this.Logger.CreateLogEntry($"Starting Process for CONNECTION-Packets", Logger.LogLevel.Debug, true);
                            CONNACK ResponseFrame = null;
                            var ResponseFrameFactory = new CONNACKFactory();
                            ClientIdentifier = ((CONN)RecievedFrame).ClientIdentifier;

                            //if (this.AddClient(((CONN)RecievedFrame).ClientIdentifier, ClientSocket) == false)
                            //{
                            //    // Client was not added to the Dictionary because there was already a Client with the same ID
                            //    // 1. Disconnect Client
                            //    Socket ConnectedClient = this.GetByClientIdentifier(ClientIdentifier);

                            //    if(ConnectedClient != null)
                            //    {
                            //        // Check if the Client is connected
                            //        if (ConnectedClient.Connected)
                            //        {
                            //            // Diconnect 
                            //            ConnectedClient.Shutdown(SocketShutdown.Both);
                            //            ConnectedClient.Close();
                            //            }
                            //        // Remove from Dictionary
                            //        this.RemoveClient(ClientIdentifier);
                            //    }

                            //    // 2. Add new Client
                            //    try
                            //    {
                            //        this.AddClient(((CONN)RecievedFrame).ClientIdentifier, ClientSocket);
                            //    }
                            //    catch (Exception ex)
                            //    {
                            //        Console.WriteLine(ex.Message);
                            //    }
                            //}
                            Socket ConnectedClient = this.GetByClientIdentifier(ClientIdentifier);

                            if (ConnectedClient != null)
                            {
                                // ID already connected
                                if (ConnectedClient.Connected)
                                {
                                    this.Logger.CreateLogEntry("a Client with the same ID was found", Logger.LogLevel.Warn, true);
                                    this.Logger.CreateLogEntry("shutting down the Connection", Logger.LogLevel.Info, true);
                                    this.RemoveClient(ClientIdentifier);
                                }
                                
                                ResponseFrame = (CONNACK)ResponseFrameFactory.CreateFrameByReturnCode(ConnectReturnCode.Accepted, false, this.Logger);
                            }
                            else
                            {
                                // everything is fine
                                ResponseFrame = (CONNACK)ResponseFrameFactory.CreateFrameByReturnCode(ConnectReturnCode.Accepted, false, this.Logger);
                            }
                            
                            if(ResponseFrame != null)
                            {
                                respons = ResponseFrame.GetBytes();
                                foreach (byte b in respons)
                                {
                                    Console.Write($"{b:X2} ");
                                }
                                Console.WriteLine();

                                this.Logger.CreateLogEntry("Sending Responseframe to Client", Logger.LogLevel.Info, true);
                                ClientSocket.Send(respons, SocketFlags.None);
                            }

                            this.AddClient(ClientIdentifier, ClientSocket);
                            
                            this.Logger.CreateLogEntry($"Client: {ClientIdentifier} sucessfully connected", Logger.LogLevel.Info, true);
                            this.Logger.CreateLogEntry($"Finished Process for CONNECTION-Packets", Logger.LogLevel.Debug, true);
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
                            this.Logger.CreateLogEntry($"Starting Process for DISCONNECTION-Packets", Logger.LogLevel.Debug, true);
                            if (ClientSocket.Connected)
                            {
                                this.RemoveClient(ClientIdentifier);
                                ClientDisconnected = true;
                            }
                            this.Logger.CreateLogEntry($"Client: {ClientIdentifier} sucessfully disconnected", Logger.LogLevel.Info, true);
                            ClientIdentifier = "";
                            this.Logger.CreateLogEntry($"Finished Process for DISCONNECTION-Packets", Logger.LogLevel.Debug, true);
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.CreateLogEntry(ex.Message + " [HandleClient()]", Logger.LogLevel.Error, true);
            }
            finally
            {
                if(ClientDisconnected == false)
                {
                    this.Logger.CreateLogEntry("Shutting down the Connection [HandleClient()]", Logger.LogLevel.Info, true);
                    ClientSocket.Shutdown(SocketShutdown.Both);
                    ClientSocket.Close();
                }
            }
        }

        public bool AddClient(string ClientIdentifier, Socket socket)
        {
            lock (this._lock)
            {
                if (this._ConnectedClients.ContainsKey(ClientIdentifier))
                {
                    this.Logger.CreateLogEntry("Client was not added to the _ConnectedClients Dict", Logger.LogLevel.Warn, true);
                    return false;
                }
                else
                {
                    this._ConnectedClients.Add(ClientIdentifier, socket);
                    this.Logger.CreateLogEntry("Client was added to the _ConnectedClients Dict", Logger.LogLevel.Info, true);
                    return true;
                }
            }
        }

        public void RemoveClient(string ClientIdentifier)
        {
            lock (this._lock)
            {
                if (this._ConnectedClients.ContainsKey(ClientIdentifier))
                {
                    this.Logger.CreateLogEntry("Shutting down the Connection [RemoveClient()]", Logger.LogLevel.Info, true);
                    this._ConnectedClients[ClientIdentifier].Shutdown(SocketShutdown.Both);
                    this._ConnectedClients[ClientIdentifier].Close();
                    this._ConnectedClients.Remove(ClientIdentifier);
                }
            }
        }

        public Socket GetByClientIdentifier(string ClientIdentifier)
        {
            lock (this._lock)
            {
                Socket socket;
                return this._ConnectedClients.TryGetValue(ClientIdentifier, out socket) ? socket : null;
            }
        }
        public void Stop()
        {
            this.Logger.CreateLogEntry($"Starting Process for Stopping the Broker", Logger.LogLevel.Debug, true);
            lock (this._lock)
            {
                if (!this._running)
                    return;

                this.Logger.CreateLogEntry($"Stopping Broker", Logger.LogLevel.Info, true);
                this._running = false;

                try
                {
                    if(this._socket.Connected == true)
                    {
                        this._socket.Shutdown(SocketShutdown.Both);
                    }
                    this.Logger.CreateLogEntry($"Broker Stopped", Logger.LogLevel.Info, true);
                }
                catch (SocketException ex)
                {
                    this.Logger.CreateLogEntry($"SocketException: [{ex.Message}]", Logger.LogLevel.Error, true);
                }
                finally
                {
                    this.Logger.CreateLogEntry($"Shutting Down Brokersocket", Logger.LogLevel.Info, true);
                    this._socket.Close();
                }
            }
            this.Logger.CreateLogEntry($"Finished Process for Stopping the Broker", Logger.LogLevel.Debug, true);
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
