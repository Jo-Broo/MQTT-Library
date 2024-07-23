using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTT
{
    public static class FrameResolver
    {
        public static Frame Resolve(byte[] data)
        {
            Frametype frametype = GetFrametype(data);
            Factory factory = GetFactory(frametype);
            Frame frame = factory.CreateFrame();
            frame.Parse(data);
            return frame;
        }

        private static Frametype GetFrametype(byte[] data) 
        {
            return Frametype.UNKNOWN;
        }

        private static Factory GetFactory(Frametype type)
        {
            switch (type)
            {
                case Frametype.CONN:
                    return new CONNFactory();
                case Frametype.CONNACK:
                    return new CONNACKFactory();
                case Frametype.PUB:
                    return new PUBFactory();
                case Frametype.PUBACK:
                    return new PUBACKFactory();
                case Frametype.PUBREC:
                    return new PUBRECFactory();
                case Frametype.PUBREL:
                    return new PUBRELFactory(); 
                case Frametype.PUBCOMP:
                    return new PUBCOMPFactory();
                case Frametype.SUB:
                    return new SUBFactory();
                case Frametype.SUBACK:
                    return new SUBACKFactory();
                case Frametype.UNSUB:
                    return new UNSUBFactory();
                case Frametype.UNSUBACK:
                    return new UNSUBACKFactory();
                case Frametype.PINGREQ:
                    return new PINGREQFactory();
                case Frametype.PINGRESP:
                    return new PINGRESFactory();
                case Frametype.DISCONN:
                    return new DISCONNFactory();
                default:
                    throw new InvalidOperationException("Unknown Frametype");
            }
        }
    }
}
