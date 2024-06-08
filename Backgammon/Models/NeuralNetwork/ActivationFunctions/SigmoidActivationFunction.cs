using Newtonsoft.Json;

namespace Backgammon.Models.NeuralNetwork.ActivationFunctions
{
    public class SigmoidActivationFunction : IActivationFunction
    {
        public float Calculate(float input)
        {
            return 1f / (1f + (float)Math.Exp(-input));
        }

        public float CalculateDerivative(float input)
        {
            float output = Calculate(input);
            return output * (1f - output);
        }
    }

    public class AdjustedSigmoidActivationFunction : IActivationFunction
    {
        // Define the new bounds
        [JsonProperty]
        private const float a = -0.2f;
        [JsonProperty]
        private const float b = 1.2f;

        public float Calculate(float input)
        {
            // Standard sigmoid
            float sigmoid = 1f / (1f + (float)Math.Exp(-input));

            // Adjust the output range
            return a + (sigmoid * (b - a));
        }

        public float CalculateDerivative(float input)
        {
            // Calculate the derivative based on the adjusted sigmoid output
            float sigmoid = Calculate(input);
            // Adjust the derivative according to the transformation
            float derivative = (sigmoid - a) * (b - sigmoid) / (b - a);
            return derivative;
        }
    }
}
