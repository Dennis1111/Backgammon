using NUnit.Framework.Legacy;

using Backgammon.Models.NeuralNetwork;
using Backgammon.Models.NeuralNetwork.ActivationFunctions;

namespace Backgammon.Tests
{
    [TestFixture]
    public class NeuralNetworkTests
    {       
        [Test]
        public void NeuralNetwork_Test_NNUE()
        {
            // Arrange
            var description = "Neural Network for Testing";
            var loggerPath = "will not be used";
            // Define the Sequential model
            var neuralNetwork = new NeuralNetwork(loggerPath, description);
            neuralNetwork.Description = description;
            var inputSize = 20;
            var hiddens1 = 15;
            var hiddens2 = 8;
            // Add layers
            neuralNetwork.AddFirstLayer(inputSize, hiddens1, new LeakyReluActivationFunction());
            neuralNetwork.AddLayer(hiddens2, new LeakyReluActivationFunction());
            neuralNetwork.AddLayer(6, new SigmoidActivationFunction());
            neuralNetwork.EnableNNUE(false);
            var nnueNetwork = neuralNetwork.Clone();
            nnueNetwork.EnableNNUE(true);
            var inputs = getRandomInputs(inputSize);
            ClassicAssert.IsFalse(neuralNetwork.IsEnabledForNNUE(), "pred1 pred2 should be same");
            ClassicAssert.IsTrue(nnueNetwork.IsEnabledForNNUE(), "pred1 pred2 should be same");
            // Act
            var pred1 = neuralNetwork.FeedForward(inputs);
            var pred2 = nnueNetwork.FeedForward(inputs);
            // The first pass will never use NNUE so we need one more with new inputs
            for (int i = 1; i < 10; i++)
            {
                inputs[i] -= 0.05f;
            }
            var pred3 = neuralNetwork.FeedForward(inputs);
            var pred4 = nnueNetwork.FeedForward(inputs);
            for (int i = 2; i < 11; i++)
            {
                inputs[i] += 0.03f;
            }
            var pred5 = neuralNetwork.FeedForward(inputs);
            var pred6 = nnueNetwork.FeedForward(inputs);

            float isSameTreshold= 0.00001f;
            // Assert
            ClassicAssert.IsTrue(isSame(pred1, pred2, isSameTreshold), "pred1 pred2 should be same");
            ClassicAssert.IsTrue(isSame(pred3, pred4, isSameTreshold), "pred3 pred4 should be same");
            ClassicAssert.IsTrue(isSame(pred5, pred6, isSameTreshold), "pred5 pred6 should be same");
            ClassicAssert.IsTrue(neuralNetwork.Compare(nnueNetwork), "Neural networks should be same.");
        }

        private float[] getFixedInputs(int inputSize)
        {
            var inputs = new float[inputSize];
            var rand = new Random();
            for (int i = 0; i < inputSize; i++)
            {
                inputs[i] = i/inputSize;
            }
            return inputs;
        }

        private float[] getRandomInputs(int inputSize)
        {
            var inputs = new float[inputSize];
            var rand = new Random();
            for (int i = 0; i < inputSize; i++) {
                inputs[i] = (float)rand.NextDouble();
            }
            return inputs;
        }

        private bool isSame(float[] vector1, float[] vector2, float treshold) {
            for (int i = 0; i < vector1.Length; i++)
            {
                if (Math.Abs(vector1[i] - vector2[i]) > treshold) { 
                    return false;
                }
            }
            return true;
        }
    }
}
