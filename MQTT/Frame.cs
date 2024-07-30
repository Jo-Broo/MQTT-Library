using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MQTT
{
    public abstract class Frame
    {
        /// <summary>
        /// 
        /// </summary>
        internal int pointer;

        #region Fixed Header
        /// <summary>
        /// Rohdaten des Fixed Headers
        /// </summary>
        internal byte[] fixedHeader_raw;
        /// <summary>
        /// ruft den Packettyp ab
        /// </summary>
        public Frametype Type { get; internal set; }
        /// <summary>
        /// enthält die verbleibende länge des Frames
        /// </summary>
        internal byte flags;
        internal int remainingLength;
        #endregion

        #region Variable Header
        /// <summary>
        /// Rohdaten des Variable Headers
        /// </summary>
        internal byte[] variableHeader_raw;
        #endregion

        #region Payload
        /// <summary>
        /// Rohdaten des Payloads
        /// </summary>
        internal byte[] payload_raw;
        #endregion

        #region Funktionen
        /// <summary>
        /// Analysiert die Daten und trägt sie in die entsprechenden Felder ein
        /// </summary>
        /// <param name="data"></param>
        public abstract void Parse(byte[] data);

        /// <summary>
        /// Analysiert den Fixed Header
        /// </summary>
        /// <param name="data">Gesamten gelesenen Daten</param>
        internal void ParseFixedHeader(byte[] data)
        {
            // We start at Byte 0
            this.pointer = 0;
            // The first half of Byte 0 contains the Packettype
            this.Type = (Frametype)(data[this.pointer] >> 4);
            // The second half contains various Flagbits that we just store for later use
            this.flags = (byte)(data[this.pointer++] & 0x0F);

            // The Length of the Frame is encoded in the Bytes 2..
            // We start with a multiplier of 1 
            int multiplier = 1;
            byte encodedByte;
            do
            {
                encodedByte = data[this.pointer++];
                // The actual Length is stored in the first 7 Bits of the Byte, the 8th Bit is used as a continuation Bit
                this.remainingLength += (encodedByte & 127) * multiplier;
                multiplier *= 128;
            } while ((encodedByte & 128) != 0);

            // We store the length of the Header as the Position of the Pointer -1
            int headerLength = this.pointer - 1; 
            // and save the Part of the Frame 
            this.fixedHeader_raw = new byte[headerLength];
            Array.Copy(data, 0, this.fixedHeader_raw, 0, headerLength);
        }

        /// <summary>
        /// Analysiert den Variable Header
        /// </summary>
        /// <param name="data">Gesamten gelesenen Daten</param>
        internal abstract void ParseVariableHeader(byte[] data);
        // this is specific for every other Frame

        /// <summary>
        /// Analysiert den Payload
        /// </summary>
        /// <param name="data">Gesamten gelesenen Daten</param>
        internal abstract void ParsePayload(byte[] data);
        // this is specific for every other Frame

        /// <summary>
        /// Gibt den Frame als Byte[] zurück
        /// </summary>
        /// <returns></returns>
        internal virtual byte[] GetBytes()
        {
            List<byte> Frame = new List<byte>();
            Frame.AddRange(this.fixedHeader_raw);
            Frame.AddRange(this.variableHeader_raw);
            Frame.AddRange(this.payload_raw);

            return Frame.ToArray();
        }

        /// <summary>
        /// Überprüft, ob das angegebene Bit in einem Byte gesetzt ist.
        /// </summary>
        /// <param name="data">Das Byte, in dem überprüft werden soll.</param>
        /// <param name="bit">Die Position des zu überprüfenden Bits (0 bis 7).</param>
        /// <returns>True, wenn das Bit gesetzt ist, sonst False.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Wird ausgelöst, wenn die Bit-Position außerhalb des Bereichs 0-7 liegt.</exception>
        internal bool IsBitset(byte data, int bit)
        {
            if (bit < 0 || bit > 7)
            {
                throw new ArgumentOutOfRangeException(nameof(bit), "Bit-Position muss im Bereich von 0 bis 7 liegen.");
            }
            return (data & (1 << bit)) == 1;
        }


        /// <summary>
        /// Berechnet die Feldlänge aus zwei Bytes, wobei das erste Byte das Most Significant Byte (MSB) und das zweite Byte das Least Significant Byte (LSB) ist.
        /// </summary>
        /// <param name="mostSignificantByte">Das Most Significant Byte (MSB) der Länge.</param>
        /// <param name="leastSignificantByte">Das Least Significant Byte (LSB) der Länge.</param>
        /// <returns>Die berechnete Länge als Integer.</returns>
        internal int GetFieldValue(byte mostSignificantByte, byte leastSignificantByte)
        {
            return (mostSignificantByte << 8) | leastSignificantByte;
        }

        #endregion
    }

    public class CONN : Frame
    {
        public string ProtocolName { get; private set; }
        public string ClientIdentifier { get; private set; }
        public bool UsernameFlag { get; private set; }
        public string Username { get; private set; }
        public bool PasswordFlag { get; private set; }
        public string Password { get; private set; }
        public bool RetainFlag { get; private set; }
        public QualityOfService QoSLevel { get; private set; }
        public bool WillFlag { get; private set; }
        public string WillTopic { get; private set; }
        public string WillMessage { get; private set; }
        public bool CleanSessionFlag { get; private set; }
        public int KeepAlive { get; private set; }

        public override void Parse(byte[] data)
        {
            this.ParseFixedHeader(data);
            this.ParseVariableHeader(data);
            this.ParsePayload(data);
        }

        internal override void ParseVariableHeader(byte[] data)
        {
            // We save the current Pointer Position to later calculate the length of the Variable Header
            int startindex = this.pointer;
            // Protocol Name
            byte[] protocolNameLengthBytes = new byte[]
            {
                data[this.pointer++],                
                data[this.pointer++]                
            };
            int protocolNameLength = (protocolNameLengthBytes[0] << 8) | protocolNameLengthBytes[1];
            byte[] protocolNameBytes = new byte[protocolNameLength];
            Array.Copy(data,this.pointer,protocolNameBytes,0,protocolNameLength);
            this.ProtocolName = UTF8Encoding.UTF8.GetString(protocolNameBytes);
            // We update the Pointer based on the length of the Protocol Name
            this.pointer += protocolNameLength;

            // Retrieving various Flags
            byte protocolLevel = data[this.pointer++];
            byte connectFlag = data[this.pointer++];
            this.UsernameFlag = this.IsBitset(connectFlag, 7);
            this.PasswordFlag = this.IsBitset(connectFlag, 6);
            this.RetainFlag = this.IsBitset(connectFlag, 5);
            this.QoSLevel = (QualityOfService)((connectFlag & 0xC) >> 2);
            this.WillFlag = this.IsBitset(connectFlag, 2);
            this.CleanSessionFlag = this.IsBitset(connectFlag, 1);
            
            // Keep Alive Time of the Connection
            byte[] KeepAliveBytes = new byte[]
            {
            data[this.pointer++],
            data[this.pointer++]
            };
            this.KeepAlive = this.GetFieldValue(KeepAliveBytes[0], KeepAliveBytes[1]);

            // Calculating the Length
            int VariableHeaderLength = this.pointer - startindex;
            this.remainingLength -= VariableHeaderLength;

            // Saving
            this.variableHeader_raw = new byte[VariableHeaderLength];
            Array.Copy(data, startindex, this.variableHeader_raw, 0, VariableHeaderLength);
        }

        internal override void ParsePayload(byte[] data)
        {
            int startindex = this.pointer;
            this.payload_raw = new byte[this.remainingLength];
            Array.Copy(data, this.pointer, this.payload_raw, 0, remainingLength);

            // The Client Identifier MUST be present and MUST be the first field 
            byte[] ClientIdentifierLengthBytes = new byte[]
            {
                data[this.pointer++],
                data[this.pointer++]
            };
            int ClientIdentifierLength = this.GetFieldValue(ClientIdentifierLengthBytes[0], ClientIdentifierLengthBytes[1]);
            byte[] ClientIdentifierBytes = new byte[ClientIdentifierLength];
            Array.Copy(data, this.pointer, ClientIdentifierBytes, 0, ClientIdentifierLength);
            this.ClientIdentifier = UTF8Encoding.UTF8.GetString(ClientIdentifierBytes);
            this.pointer += ClientIdentifierLength;

            // Contains the Will Topic and the Will Message
            if (this.WillFlag == true)
            {
                Debug.WriteLine("= Extracting Will Topic =");
                byte[] WillTopicLengthBytes = new byte[]
                {
                    data[this.pointer++],
                    data[this.pointer++]
                };
                int WillTopicLength = this.GetFieldValue(WillTopicLengthBytes[0], WillTopicLengthBytes[1]);
                byte[] WillTopicBytes = new byte[WillTopicLength];
                Array.Copy(data, this.pointer, WillTopicBytes, 0, WillTopicLength);
                this.WillTopic = UTF8Encoding.UTF8.GetString(WillTopicBytes);
                this.pointer += WillTopicLength;

                Debug.WriteLine("= Extracting Will Message =");
                byte[] WillMessageLengthBytes = new byte[]
                {
                    data[this.pointer++],
                    data[this.pointer++]
                };
                int WillMessageLength = this.GetFieldValue(WillMessageLengthBytes[0], WillMessageLengthBytes[1]);
                byte[] WillMessageBytes = new byte[WillMessageLength];
                Array.Copy(data, this.pointer, WillMessageBytes, 0, WillMessageLength);
                this.WillMessage = UTF8Encoding.UTF8.GetString(WillMessageBytes);
                this.pointer += WillMessageLength;
            }

            if (this.UsernameFlag == true)
            {
                byte[] UsernameLengthBytes = new byte[]
                {
                    data[this.pointer++],
                    data[this.pointer++]
                };
                int UsernameLength = this.GetFieldValue(UsernameLengthBytes[0], UsernameLengthBytes[1]);
                byte[] UsernameBytes = new byte[UsernameLength];
                Array.Copy(data, this.pointer, UsernameBytes, 0, UsernameLength);
                this.Username = UTF8Encoding.UTF8.GetString(UsernameBytes);
                this.pointer += UsernameLength;
            }

            if (this.PasswordFlag == true)
            {
                byte[] PasswordLengthBytes = new byte[]
                {
                    data[this.pointer++],
                    data[this.pointer++]
                };
                int PasswordLength = this.GetFieldValue(PasswordLengthBytes[0], PasswordLengthBytes[1]);
                byte[] PasswordBytes = new byte[PasswordLength];
                Array.Copy(data, this.pointer, PasswordBytes, 0, PasswordLength);
                this.Password = UTF8Encoding.UTF8.GetString(PasswordBytes);
                this.pointer += PasswordLength;
            }

            int PayloadLength = this.pointer - startindex;
            this.remainingLength -= PayloadLength;

            if(this.remainingLength != 0)
            {
                throw new InvalidOperationException("The Package could not be correctly resolved");
            }
        }
    }

    public class CONNACK : Frame
    {
        public bool SessionPresentFlag { get; set; }
        public int ConnectReturnCode { get; set; }
        
        public override void Parse(byte[] data)
        {
            // Not needed because the Server doesnt need to analyze anything
            // Of course the Client has to do it but i am focusing on getting the Server Setup and then the client
            return;
        }

        internal override void ParseVariableHeader(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override void ParsePayload(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override byte[] GetBytes()
        {
            List<byte> Frame = new List<byte>
            {
                (byte)((byte)this.Type << 4),
                (byte)this.remainingLength,
                (byte)((this.SessionPresentFlag)?1:0),
                (byte)this.ConnectReturnCode
            };

            return Frame.ToArray();
        }
    }

    public class PUB : Frame
    {
        public bool DUPFlag { get; internal set; }
        public QualityOfService QoSLevel { get; internal set; }
        public bool RetainFlag { get; internal set; }
        public string TopicName { get; internal set; }
        public int PacketIdentifier { get; internal set; }

        public string TopicMessage { get; internal set; }

        public override void Parse(byte[] data)
        {
            this.ParseFixedHeader(data);
            this.DUPFlag = this.IsBitset(this.flags, 3);
            this.QoSLevel = (QualityOfService)((this.flags & 6) >> 1);
            this.DUPFlag = this.IsBitset(this.flags, 0);
            this.ParseVariableHeader(data);
            this.ParsePayload(data);
        }

        internal override void ParseVariableHeader(byte[] data)
        {
            int startindex = this.pointer;

            // Topic Name
            byte[] TopicNameLengthBytes = new byte[]
            {
                data[this.pointer++],
                data[this.pointer++]
            };
            int TopicNameLength = this.GetFieldValue(TopicNameLengthBytes[0], TopicNameLengthBytes[1]);
            byte[] TopicNameBytes = new byte[TopicNameLength];
            Array.Copy(data,this.pointer,TopicNameBytes,0,TopicNameLength);
            this.TopicName = UTF8Encoding.UTF8.GetString(TopicNameBytes);
            this.pointer += TopicNameLength;

            if(this.QoSLevel > 0)
            {
                // Packet Identifier
                byte[] PacketIdentifierBytes = new byte[]
                {
                data[this.pointer++],
                data[this.pointer++]
                };
                this.PacketIdentifier = this.GetFieldValue(PacketIdentifierBytes[0], PacketIdentifierBytes[1]);
                this.pointer += PacketIdentifierBytes.Length;
            }

            int VariableHeaderLength = this.pointer - startindex;
            this.remainingLength -= VariableHeaderLength;
        }

        internal override void ParsePayload(byte[] data)
        {
            if(this.remainingLength < 0) { throw new Exception("Remaining Length was less than 0"); }
            if(remainingLength == 0) { this.TopicMessage = ""; }
            byte[] MessageBytes = new byte[this.remainingLength];
            Array.Copy(data, this.pointer, MessageBytes, 0, this.remainingLength);
            this.TopicMessage = UTF8Encoding.UTF8.GetString(MessageBytes);
            this.pointer += MessageBytes.Length;
        }
    }

    public class PUBACK : Frame
    {
        public override void Parse(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override void ParseVariableHeader(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override void ParsePayload(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override byte[] GetBytes()
        {
            throw new NotImplementedException();
        }
    }

    public class PUBREC : Frame
    {
        public override void Parse(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override void ParseVariableHeader(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override void ParsePayload(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override byte[] GetBytes()
        {
            throw new NotImplementedException();
        }
    }

    public class PUBREL : Frame
    {
        public override void Parse(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override void ParseVariableHeader(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override void ParsePayload(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override byte[] GetBytes()
        {
            throw new NotImplementedException();
        }
    }

    public class PUBCOMP : Frame
    {
        public override void Parse(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override void ParseVariableHeader(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override void ParsePayload(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override byte[] GetBytes()
        {
            throw new NotImplementedException();
        }
    }

    public class SUB : Frame
    {
        public override void Parse(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override void ParseVariableHeader(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override void ParsePayload(byte[] data)
        {
            throw new NotImplementedException();
        }
    }

    public class SUBACK : Frame
    {
        public override void Parse(byte[] data)
        {
            return;
        }

        internal override void ParseVariableHeader(byte[] data)
        {
            return;
        }

        internal override void ParsePayload(byte[] data)
        {
            return;
        }

        internal override byte[] GetBytes()
        {
            throw new NotImplementedException();
        }
    }

    public class UNSUB : Frame
    {
        public override void Parse(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override void ParseVariableHeader(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override void ParsePayload(byte[] data)
        {
            throw new NotImplementedException();
        }
    }

    public class UNSUBACK : Frame
    {
        public override void Parse(byte[] data)
        {
            return;
        }

        internal override void ParseVariableHeader(byte[] data)
        {
            return;
        }

        internal override void ParsePayload(byte[] data)
        {
            return;
        }

        internal override byte[] GetBytes()
        {
            throw new NotImplementedException();
        }
    }

    public class PINGREQ : Frame
    {
        public override void Parse(byte[] data)
        {
            this.ParseFixedHeader(data);
        }

        internal override void ParseVariableHeader(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override void ParsePayload(byte[] data)
        {
            throw new NotImplementedException();
        }
    }

    public class PINGRES : Frame
    {
        public override void Parse(byte[] data)
        {
            this.ParseFixedHeader(data);
        }

        internal override void ParseVariableHeader(byte[] data)
        {
            return;
        }

        internal override void ParsePayload(byte[] data)
        {
            return;
        }

        internal override byte[] GetBytes()
        {
            List<byte> Frame = new List<byte>
            {
                (byte)((byte)this.Type << 4),
                (byte)this.remainingLength
            };

            return Frame.ToArray();
        }
    }

    public class DISCONN : Frame
    {
        public override void Parse(byte[] data)
        {
            this.ParseFixedHeader(data);
        }

        internal override void ParseVariableHeader(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override void ParsePayload(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override byte[] GetBytes()
        {
            throw new NotImplementedException();
        }
    }
}
