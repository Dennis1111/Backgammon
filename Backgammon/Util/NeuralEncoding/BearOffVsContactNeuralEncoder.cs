using static Backgammon.Util.NeuralEncoding.BoardToNeuralInputsEncoder;
using static Backgammon.Models.BackgammonBoard;
using Backgammon.Models;

namespace Backgammon.Util.NeuralEncoding
{
    internal class BearOffVsContactNeuralEncoder
    {
        // Here we assume that the position has Player1 on turn and is also the player bearing off
        // if player 2 is bearing off the position has to be mirrored first before calling this function)
        public static (float[] neuralInputs, string[] labels) EncodeBearOffVs1PointToNeuralInputs(int[] position)
        {
            // Get base features from EncodeContactGameToNeuralInputs
            var (baseNeuralInputs, baseLabels) = EncodeContactGameToNeuralInputs(position);

            // Add BearOffVs1Point specific features
            var (bearOffFeatures, bearOffLabels) = EncodeBearOffVs1PointNeuralInputs(position);

            // Combine base and specific features
            var combinedFeatures = new List<float>(baseNeuralInputs);
            var combinedLabels = new List<string>(baseLabels);

            combinedFeatures.AddRange(bearOffFeatures);
            combinedLabels.AddRange(bearOffLabels);

            return (combinedFeatures.ToArray(), combinedLabels.ToArray());
        }

        private static (float[], string[]) EncodeBearOffVs1PointNeuralInputs(int[] position)
        {
            // Implement the BearOffVs1Point specific features here
            // This can include checking how many checkers are on the opponent's 1-point,
            // the player's bear-off potential, etc.

            var encodedData = new List<(float[], string[])>
            {
                EncodeBearOffCheckersAtTheBackPlayerP1(position),
                EncodeBearOffGapsPlayerP1(position),
            };
            var combinedFeatures = new List<float>();
            var combinedLabels = new List<string>();

            foreach (var (features, labels) in encodedData)
            {
                combinedFeatures.AddRange(features);
                combinedLabels.AddRange(labels);
            }
            return (combinedFeatures.ToArray(), combinedLabels.ToArray());
        }

        // Here we assume that the position has Player1 on turn and is also the player bearing off
        // if player 2 is bearing off the position has to be mirrored first before calling this function)
        public static (float[] neuralInputs, string[] labels) EncodeBearOffVs1PointDefenceToNeuralInputs(int[] position)
        {
            // Get base features from EncodeContactGameToNeuralInputs
            var (baseNeuralInputs, baseLabels) = EncodeContactGameToNeuralInputs(position);

            // Add BearOffVs1Point specific features
            var (bearOffFeatures, bearOffLabels) = EncodeBearOffVs1PointDefenceNeuralInputs(position);

            // Combine base and specific features
            var combinedFeatures = new List<float>(baseNeuralInputs);
            var combinedLabels = new List<string>(baseLabels);

            combinedFeatures.AddRange(bearOffFeatures);
            combinedLabels.AddRange(bearOffLabels);

            return (combinedFeatures.ToArray(), combinedLabels.ToArray());
        }

        private static (float[], string[]) EncodeBearOffVs1PointDefenceNeuralInputs(int[] position)
        {
            // Implement the BearOffVs1Point specific features here
            // This can include checking how many checkers are on the opponent's 1-point,
            // the player's bear-off potential, etc.

            var encodedData = new List<(float[], string[])>
            {
                EncodeBearOffCheckersAtTheBackPlayerP2(position),
                EncodeBearOffGapsPlayerP2(position),
            };
            var combinedFeatures = new List<float>();
            var combinedLabels = new List<string>();

            foreach (var (features, labels) in encodedData)
            {
                combinedFeatures.AddRange(features);
                combinedLabels.AddRange(labels);
            }
            return (combinedFeatures.ToArray(), combinedLabels.ToArray());
        }

        // Only makes sense to have these inputs for the Player bearing off checkers, makes it a bit complex as we also mirror boards
        // So then the question is should one have different neural networks depending which side is bearing off,
        // It seems to be the way to go !
        internal static (float[], string[]) EncodeBearOffCheckersAtTheBackPlayerP1(int[] position)
        {
            (int lastP1, int secLastP1) = CountCheckersAtTheBack(position, Player1);
            var inputLastEvenP1 = lastP1 % 2 == 0 ? inputMin : inputMax;
            var inputSecLastEvenP1 = secLastP1 % 2 == 0 ? inputMin : inputMax;
            var sumIsEvenP1 = (secLastP1 + lastP1) % 2 == 0 ? inputMin : inputMax;
            float[] inputs = { inputLastEvenP1, inputSecLastEvenP1, sumIsEvenP1 };
            
            string[] labels = { "EvenLastP1", "EvenSecLastP1", "EvenSumP1" };
            return (inputs, labels);
        }

        // Only makes sense to have these inputs for the Player bearing off checkers, makes it a bit complex as we also mirror boards
        internal static (float[], string[]) EncodeBearOffCheckersAtTheBackPlayerP2(int[] position)
        {
            (int lastP2, int secLastP2) = CountCheckersAtTheBack(position, Player2);
            var inputLastEvenP2 = lastP2 % 2 == 0 ? inputMin : inputMax;
            var inputSecLastEvenP2 = secLastP2 % 2 == 0 ? inputMin : inputMax;
            var sumIsEvenP2 = (secLastP2 + lastP2) % 2 == 0 ? inputMin : inputMax;
            float[] inputs = { inputLastEvenP2, inputSecLastEvenP2, sumIsEvenP2 };
            string[] labels = { "EvenLastP2", "EvenSecLastP2", "EvenSumP2" };
            
            return (inputs, labels);
        }

        // Having gaps increase future shots a lot
        public static (float[], string[]) EncodeBearOffGapsPlayerP1(int[] position)
        {
            var (lastCheckersP1, lastCheckersP2) = LastCheckers(position);
            int gapsP1 = BearOffMaxGap(position, Player1, lastCheckersP1, lastCheckersP2);
            var maxGap = 5;
            float[] neuralInputs = new float[maxGap];
            for (int i = 0; i < Math.Min(maxGap, gapsP1); i++)
            {
                neuralInputs[i] = inputMax;
            }
            string[] labels = new string[maxGap];
            for (int i = 0; i < maxGap; i++)
            {
                labels[i] = $"MaxGapP1{i + 1}";
            }

            return (neuralInputs, labels);
        }

        // Having gaps increase future shots a lot
        public static (float[], string[]) EncodeBearOffGapsPlayerP2(int[] position)
        {
            var (lastCheckersP1, lastCheckersP2) = LastCheckers(position);
            int gapsP2 = BearOffMaxGap(position, Player2, lastCheckersP1, lastCheckersP2);
            var maxGap = 5;
            float[] neuralInputs = new float[maxGap];
            for (int i = 0; i < Math.Min(maxGap, gapsP2); i++)
            {
                neuralInputs[i] = inputMax;
            }
            string[] labels = new string[maxGap];
            for (int i = 0; i < maxGap; i++)
            {
                labels[i] = $"MaxGapP2{i + 1}";
            }            

            return (neuralInputs, labels);
        }
    }
}
