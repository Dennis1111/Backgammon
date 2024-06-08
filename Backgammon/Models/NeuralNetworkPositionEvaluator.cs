using Backgammon.Models.NeuralNetwork;
using Backgammon.Models;

public class NeuralNetworkPositionEvaluator : IBackgammonPositionEvaluator
{
    private NeuralNetwork _neuralNetwork;

    public NeuralNetworkPositionEvaluator(NeuralNetwork neuralNetwork)
    {
        _neuralNetwork = neuralNetwork;
    }

    public float[] Evaluate(int[] position)
    {
        // Assuming NeuralNetwork has a method EvaluatePosition
        return null;// _neuralNetwork.FeedForward(position);
    }
}
