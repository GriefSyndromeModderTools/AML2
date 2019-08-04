using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Scene.Caocao
{
    internal class CaocaoPlayerLocation
    {
        public static int Name;
        public static int X, Y;

        public static void ResetLocation()
        {
            X = Y = 0;
        }
    }
}
