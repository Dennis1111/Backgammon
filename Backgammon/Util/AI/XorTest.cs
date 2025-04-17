using Backgammon.Models.NeuralNetwork.ActivationFunctions;
using Backgammon.Models.NeuralNetwork;

namespace Backgammon.Util.AI
{
    internal class XorTest
    {

        public static List<(float[], float[])> getXorDataSet()
        {
            List<(float[], float[])> XoR = [];
            var input = new float[] { 0f, 0f };
            var target = new float[] { 0f };
            XoR.Add((input, target));
            input = [1f, 1f];
            target = [0f];
            XoR.Add((input, target));
            input = [0f, 1f];
            target = [1f];
            XoR.Add((input, target));
            input = [1f, 0f];
            target = [1f];
            XoR.Add((input, target));

            return XoR;
        }

        static NeuralNetwork GetXORNetworkModel(int inputSize, string loggerPath)
        {
            // Define the Sequential model
            var model = new NeuralNetwork(loggerPath, "XOR");
            // Add layers
            model.AddFirstLayer(inputSize, 4, new LeakyReluActivationFunction());
            model.AddLayer(16, new LeakyReluActivationFunction());
            model.AddLayer(1, new SigmoidActivationFunction()); // Assuming 6 outputs for the scores
            return model;
        }

        //var xOrModel = GetXORNetworkModel(2);
        //var gameDataXORTrainer = new GameDataTrainer(xOrModel, projPath + "\\data\\logs\\train.log");
        /*for (int i = 0; i < 50000; i++) {
            foreach (var sample in XoR) {
                input = sample.Item1;
                target = sample.Item2;
                var pred = xOrModel.FeedForward(input);
                var mse = xOrModel.Backpropagate(pred, target, 0.01f, 0.001f);

                Console.WriteLine($"Prediction {input[0]} {input[1]} -> {pred[0]} mse {mse}");
            }    
        }*/
    }
}
