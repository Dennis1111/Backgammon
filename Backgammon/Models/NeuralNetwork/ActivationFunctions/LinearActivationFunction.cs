using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backgammon.Models.NeuralNetwork.ActivationFunctions
{
    public class LinearActivationFunction : IActivationFunction
    {
        public float Calculate(float input) => input;

        public float CalculateDerivative(float input) => 1f;
    }
}
