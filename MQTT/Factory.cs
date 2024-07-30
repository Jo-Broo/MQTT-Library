using Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTT
{
    public abstract class Factory
    {
        public abstract Frame CreateFrame();
    }

    public class CONNFactory : Factory 
    {
        public override Frame CreateFrame()
        {
            return new CONN();
        }
    }

    public class CONNACKFactory : Factory
    {
        public override Frame CreateFrame()
        {
            return new CONNACK();
        }

        public Frame CreateFrameByReturnCode(ConnectReturnCode returnCode, bool SessionPresent)
        {
            CONNACK Frame = new CONNACK();
            Frame.Type = Frametype.CONNACK;
            Frame.remainingLength = 2;
            Frame.SessionPresentFlag = SessionPresent;
            Frame.ConnectReturnCode = (int)returnCode;

            return Frame;
        }

        public Frame CreateFrameByReturnCode(ConnectReturnCode returnCode, bool SessionPresent, Logger logger)
        {
            logger.CreateLogEntry($"Creating CONNACK with Returncode: [{returnCode}]", Logger.LogLevel.Info, true);
            return this.CreateFrameByReturnCode(returnCode, SessionPresent);
        }
    }

    public class PUBFactory : Factory
    {
        public override Frame CreateFrame()
        {
            return new PUB();
        }
    }

    public class PUBACKFactory : Factory
    {
        public override Frame CreateFrame()
        {
            return new PUBACK();
        }
    }

    public class PUBRECFactory : Factory
    {
        public override Frame CreateFrame()
        {
            return new PUBREC();
        }
    }

    public class PUBRELFactory : Factory
    {
        public override Frame CreateFrame()
        {
            return new PUBREL();
        }
    }

    public class PUBCOMPFactory : Factory
    {
        public override Frame CreateFrame()
        {
            return new PUBCOMP();
        }
    }

    public class SUBFactory : Factory
    {
        public override Frame CreateFrame()
        {
            return new SUB();
        }
    }

    public class SUBACKFactory : Factory
    {
        public override Frame CreateFrame()
        {
            return new SUBACK();
        }
    }

    public class UNSUBFactory : Factory
    {
        public override Frame CreateFrame()
        {
            return new UNSUB();
        }
    }

    public class UNSUBACKFactory : Factory
    {
        public override Frame CreateFrame()
        {
            return new UNSUBACK();
        }
    }

    public class PINGREQFactory : Factory
    {
        public override Frame CreateFrame()
        {
            return new PINGREQ();
        }
    }

    public class PINGRESFactory : Factory
    {
        public override Frame CreateFrame()
        {
            PINGRES Frame = new PINGRES();
            Frame.Type = Frametype.PINGRESP;
            Frame.remainingLength = 0;

            return Frame;   
        }
    }

    public class DISCONNFactory : Factory
    {
        public override Frame CreateFrame()
        {
            return new DISCONN();
        }
    }
}
