using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTT
{
    public abstract class Factory
    {
        public abstract Frame ResolveFrame();
    }
}
