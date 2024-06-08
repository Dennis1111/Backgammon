using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backgammon.Models
{
    public class TrainingData(float[] inputData, float[] target, float learningRate, int modelIndex)
    {
        public float[] InputData { get; set; } = inputData;
        public float[] Target { get; set; } = target;
        public float LearningRate { get; set; } = learningRate;
        public int Epochs { get; set; }
        //We want to use different models for training if there is contact or not
        public int ModelIndex { get; set; } = modelIndex;

        public int[]? board { get; set; }


    }
}
