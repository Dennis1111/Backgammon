using Backgammon.Models;

namespace Backgammon.WebApp.Dtos
{
    public class ComputerMoveResponseDto
    {
        public Move? Move { get; set; }
        public int[]? BoardAfterMove { get; set; }
    }
}
