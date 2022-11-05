using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.GSO
{
    internal static class InternalMessageId
    {
        public const int Debug0 = 0x60;
        public const int Debug1 = 0x61;
        public const int RequestModString = 0x70;
        public const int ReplyModString = 0x71;
        public const int CustomMessageClient = 0x72; //From client to server (server will broadcast)
        public const int CustomMessageServer = 0x73; //From server to all clients
        public const int UserDefined = 0xE0; //Not handled by AML
    }
}
