namespace Backgammon.WebApp.Dtos
{
    public class MoveDto
    {
        public string MoveAnnotation { get; set; } = string.Empty;
        public int Die1 { get; set; }
        public int Die2 { get; set; }
        public int Player { get; set; }
        public List<double>? ScoreEstimation { get; set; }
        public int[] Board { get; set; } = Array.Empty<int>();
        public string? FinalScore { get; set; }  // Add this for final score if applicable
    }
}
