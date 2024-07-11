namespace Backgammon.Models.NeuralNetwork
{
    internal interface ILayer
    {
        float[] Feedforward(float[] inputs);

        float[] Backpropagate(float[] outputErrors, float learningRate, float[] previousLayerActivations);
    }
}
