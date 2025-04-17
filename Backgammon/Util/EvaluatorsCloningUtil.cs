using Backgammon.Models.NeuralNetwork;
using Backgammon.Models;
using static Backgammon.Util.Constants;

namespace Backgammon.Util
{
    internal class EvaluatorsCloningUtil
    {
        //Pos Evaluators where the nns are cloned
        static Dictionary<PositionType, IBackgammonPositionEvaluator> ClonePosEvaluators(Dictionary<PositionType, IBackgammonPositionEvaluator> source)
        {
            var positionNeuralEvalDictClone = new Dictionary<PositionType, IBackgammonPositionEvaluator>();

            foreach (KeyValuePair<PositionType, IBackgammonPositionEvaluator> kvp in source)
            {
                if (kvp.Value is NeuralNetworkPositionEvaluator)
                {
                    NeuralNetwork nn = ((NeuralNetworkPositionEvaluator)kvp.Value).NeuralNetwork;
                    var nnClone = nn.Clone();
                    positionNeuralEvalDictClone[kvp.Key] = new NeuralNetworkPositionEvaluator(nnClone, kvp.Key);
                }
                else
                {
                    positionNeuralEvalDictClone[kvp.Key] = kvp.Value;
                }
            }
            return positionNeuralEvalDictClone;
        }

    }
}
