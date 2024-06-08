namespace Backgammon.Models
{
    public class MoveData(int player, int[] boardBefore, int[] boardAfter, Move move, float equity, float[] scoreVector)
    {
        public int Player { get; set; } = player;
        public int[] BoardBefore { get; set; } = boardBefore;
        public int[] BoardAfter { get; set; } = boardAfter;
        public Move Move { get; set; } = move;
        public float Equity { get; set; } = equity;
        public float[] ScoreVector { get; set; } = scoreVector;
        public List<MoveData>? MoveCandidates { get; set; }
    }
}