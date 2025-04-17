using static Backgammon.Util.NeuralEncoding.BoardToNeuralInputsEncoder;

namespace Backgammon.Util.NeuralEncoding
{


    internal class HoldingGameNeuralEncoder
    {


        // Assumes player One has a last anchor, doesn't have to be true unless mutual holding game
        public static (float[] neuralInputs, string[] labels) EncodeAnchorContactPlayerOne(int[] position, int anchorPoint, string anchorDescription)
        {
            float[] contactInputs = new float[7];
            string[] contactLabels = new string[7];
            int blockedPoints = 0;
            for (int i = 0; i < 6; i++)
            {
                if (position[anchorPoint + 1 + i] <= -2)
                {
                    blockedPoints++;
                    contactInputs[i] = inputMax;
                }
                else
                {
                    contactInputs[i] = inputMin;
                }
                contactLabels[i] = $"{anchorDescription}{i + 1}P1";
            }
            contactInputs[6] = blockedPoints / 6;
            contactLabels[6] = anchorDescription + "P1TotalContact";
            return (contactInputs, contactLabels);
        }

        public static (float[] neuralInputs, string[] labels) EncodeAnchorContactPlayerTwo(int[] position, int anchorPoint, string anchorDescription)
        {
            float[] contactInputs = new float[7];
            string[] contactLabels = new string[7];
            int blockedPoints = 0;
            for (int i = 0; i < 6; i++)
            {
                if (position[anchorPoint - 1 - i] <= -2)
                {
                    blockedPoints++;
                    contactInputs[i] = inputMax;
                }
                else
                {
                    contactInputs[i] = inputMin;
                }
                contactLabels[i] = $"{anchorDescription}{i + 1}P2";
            }
            contactInputs[6] = blockedPoints / 6;
            contactLabels[6] = anchorDescription + "P2TotalContact";
            return (contactInputs, contactLabels);
        }
    }
}
