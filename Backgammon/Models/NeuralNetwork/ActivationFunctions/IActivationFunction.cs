namespace Backgammon.Models.NeuralNetwork.ActivationFunctions
{
    public interface IActivationFunction
    {
        float Calculate(float input);
        float CalculateDerivative(float input);
    }
}