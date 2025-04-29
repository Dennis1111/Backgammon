namespace Backgammon.WebApp.Dtos
{
    public class GameStateDto
    {
        public int[] Board { get; set; } = Array.Empty<int>();
        public int Player { get; set; }
        public int Die1 { get; set; }
        public int Die2 { get; set; }
    }
}
