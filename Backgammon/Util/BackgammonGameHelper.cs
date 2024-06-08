using Backgammon.Models;

namespace Backgammon.Util
{
    public static class BackgammonGameHelper
    {
        public static int Opponent(int player)
        {
            return (player == BackgammonBoard.Player1) ? BackgammonBoard.Player2 : BackgammonBoard.Player1;
        }
    }
}
