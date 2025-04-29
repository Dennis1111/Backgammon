using Backgammon.Models;

namespace Backgammon.WebApp.Dtos
{
    public class LegalMovesResponseDto
    {
        public List<MoveResult> LegalMoves { get; set; } = new();
    }

    public class MoveResult
    {
        public Move Move { get; set; }
        public int[] Board { get; set; }
    }
}
