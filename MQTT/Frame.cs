using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTT
{
    public abstract class Frame
    {
        #region Fixed Header
        internal byte[] fixedHeader_raw;
        public Frametype Frametype { get; internal set; }
        #endregion

        #region Variable Header
        internal byte[] variableHeader_raw;
        #endregion

        #region Payload
        internal byte[] payload_raw;
        #endregion

        #region Funktionen
        public abstract void Parse(byte[] data);
        #endregion
    }

    public class CONN : Frame
    {

    }

    public class CONNACK : Frame
    {

    }

    public class PUB : Frame
    {

    }

    public class PUBACK : Frame
    {

    }

    public class PUBREC : Frame
    {

    }

    public class PUBREL : Frame
    {

    }

    public class PUBCOMP : Frame
    {

    }

    public class SUB : Frame
    {

    }

    public class SUBACK : Frame
    {

    }

    public class UNSUB : Frame
    {

    }

    public class UNSUBACK : Frame
    {

    }

    public class PINGREQ : Frame
    {

    }

    public class PINGRES : Frame
    {

    }

    public class DISCONN : Frame
    {

    }

    public enum Frametype
    {
        UNKNOWN = -1,
        CONN = 1,
        CONNACK = 2,
        PUB = 3,
        PUBACK = 4,
        PUBREC = 5,
        PUBREL = 6,
        PUBCOMP = 7,
        SUB = 8,
        SUBACK = 9,
        UNSUB = 10,
        UNSUBACK = 11,
        PINGREQ = 12,
        PINGRESP = 13,
        DISCONN = 14,
    }

    public enum QualityOfService
    {
        AtMostOnce = 0,
        AtLeastOnce = 1,
        ExactlyOnce = 2
    }
}
