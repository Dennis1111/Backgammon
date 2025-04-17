using Backgammon.Models.NeuralNetwork;
using Backgammon.Models;
using static Backgammon.Util.Constants;
using Backgammon.Utils;
using Backgammon.Util.NeuralEncoding;

public class NeuralNetworkPositionEvaluator : IBackgammonPositionEvaluator
{
    private NeuralNetwork _neuralNetwork;
    private PositionType _positionType;
    public NeuralNetwork NeuralNetwork => _neuralNetwork;

    public NeuralNetworkPositionEvaluator(NeuralNetwork neuralNetwork, PositionType positionType)
    {
        _neuralNetwork = neuralNetwork;
        _positionType = positionType;
    }

    public float[] Evaluate(int[] position, int player)
    {
        // Assuming NeuralNetwork has a method EvaluatePosition
        var (inputData, labels) = BoardToNeuralInputsEncoder.EncodeBoardToNeuralInputs(position, _positionType, player);
        _neuralNetwork.SetInputLabels(labels);// A bit ugly temporar solution
        var predict = _neuralNetwork.FeedForward(inputData);

        if (MirrorBoardForPlayer2 && player == BackgammonBoard.Player2)
        {
            predict = ScoreUtility.MirrorScore(predict);
            /*if (debug)
            {
                Console.WriteLine($"\n Mirrored + {string.Join(", ", predict)}");
            }*/
        }
        
        predict = ScoreUtility.AdjustEstimatedScore(predict, position);
        return predict;
    }
}