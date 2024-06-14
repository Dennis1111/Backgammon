using static Backgammon.Util.Constants;

namespace Backgammon.Models
{
    public class TrainingData(float[] inputData, float[] target, float learningRate, PositionType positionType)
    {
        public float[] InputData { get; set; } = inputData;
        public float[] Target { get; set; } = target;
        public float LearningRate { get; set; } = learningRate;
        public int Epochs { get; set; }
        //We want to use different models for training if there is contact or not
        //public int ModelIndex { get; set; } = modelIndex;
        public PositionType PositionType { get; set; } = positionType;
        public int[] board { get; set; }
    }
}
