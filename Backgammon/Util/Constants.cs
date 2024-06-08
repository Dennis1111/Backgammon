using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backgammon.Util
{
    public static class Constants
    {
        public const bool MirrorBoardForPlayer2 = true;

        public enum PositionType
        {
            NoContact,
            Contact,
            Backgame12,
            Backgame13,
            Backgame23,
            OtherBackgame
            // Add more models as needed
        }
    }
}
