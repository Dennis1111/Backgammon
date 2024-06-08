using Newtonsoft.Json;

namespace Backgammon.Models.NeuralNetwork.ActivationFunctions
{
    public class ReluActivationFunction : IActivationFunction
    {
        public float Calculate(float input) => Math.Max(0, input);
        public float CalculateDerivative(float input) => input > 0 ? 1f : 0f;
    }

    public class LeakyReluActivationFunction : IActivationFunction
    {
        [JsonProperty]
        private readonly float _alpha;

        public LeakyReluActivationFunction(float alpha = 0.01f)
        {
            _alpha = alpha;
        }

        public float Calculate(float input) => input > 0 ? input : _alpha * input;

        public float CalculateDerivative(float input) => input > 0 ? 1f : _alpha;
    }
}