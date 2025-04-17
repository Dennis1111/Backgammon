namespace Backgammon.Models
{
    public interface IBackgammonPositionEvaluator
    {
        float[] Evaluate(int[] position, int player);
    }
}
