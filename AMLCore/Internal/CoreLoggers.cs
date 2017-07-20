﻿using AMLCore.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Internal
{
    internal class CoreLoggers
    {
        public static readonly Logger Injection = new Logger("Core/Injection");
        public static readonly Logger Loader = new Logger("Core/Loader");
        public static readonly Logger Main = new Logger("Core/Main");
        public static readonly Logger Input = new Logger("Core/Input");
        public static readonly Logger Update = new Logger("Core/Update");
    }
}
