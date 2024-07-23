using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTT
{
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

    public enum ConnectReturnCode 
    {
        Accepted = 0,
        RefusedUnacceptableProtocol = 1,
        RefusedIdentifierRejected = 2,
        RefusedServerUnavailable = 3,
        RefusedBadUsernameOrPassword = 4,
        RefusedNotAuthorized = 5,
    }
}
