using static Backgammon.Util.Constants;
using static Backgammon.Models.BackgammonBoard;
using static Backgammon.Util.NeuralEncoding.BearOffVsContactNeuralEncoder;
namespace Backgammon.Util.NeuralEncoding
{
    public static class BoardToNeuralInputsEncoder
    {
        internal static readonly float inputMin = -1;
        internal static readonly float inputMax = 1;
        // Combinations to enter with one checker from the bar with N open points
        // 0 -> 0%,  1 -> 11 (30.6%), 2 -> 20 (55.6%), 3 -> 27 (75%), 4-> 32 (88.9%), 5 -> 35 (97.2%), 6 -> 36 (100%) 
        // Combinations to enter with two checkers from the bar with N open points
        // 0 -> 0%,  1 -> 1 (2.7%), 2 -> 4 (11.1%), 3 -> 9 (25%), 4-> 16 (44.4%), 5 -> 25 (69.4%), 6 -> 36 (100%) 


        // How likely are we to not enter with any checkers given number of innerboard points taken 0..6
        private static readonly float[] danceProbabilities = { 0f, 1f / 36, 1f / 9, 0.25f, 16f / 36, 25f / 36, 1f };

        // How likely is that we can run with N different dice combinations that are not blocked (The same probabilities as enter with one checker from the bar)
        private static readonly float[] runningProbabilities = { 0f, 11f / 36, 20f / 36, 0.75f, 32f / 36, 35f / 36, 1f };

        public static int MapBoardToModel(int[] board)
        {
            var (stillContact, _) = StillContact(board);
            return stillContact ? 1 : 0;
        }

        public static (float[] neuralInputs, string[] labels) EncodeBoardToNeuralInputs(int[] positionToEncode, PositionType positionType, int player = Player1, bool alwaysMirror = false)
        {
            var position = positionToEncode;
            if (alwaysMirror || MirrorBoardForPlayer2 && player == Player2)
            {
                position = MirrorBoard(position);
            }
            // For now we only have two different input encoders but plan to optimize that in the future
            switch (positionType)
            {
                case PositionType.BearOffVs1Point:
                    return EncodeBearOffVs1PointToNeuralInputs(position);
                case PositionType.BearOffVs1PointDefence:
                    return EncodeBearOffVs1PointDefenceToNeuralInputs(position);
                case PositionType.BearOffContact:
                    return EncodeBearOffVs1PointToNeuralInputs(position);
                case PositionType.BearOffContactDefence:
                    return EncodeBearOffVs1PointDefenceToNeuralInputs(position);
                
                case PositionType.BearOff:
                    return EncodeNoContactBoardToNeuralInputs(position);
                case PositionType.NoContact:
                    return EncodeNoContactBoardToNeuralInputs(position);
                case PositionType.Backgame12://For now we only have two different input encoders
                                             //return EncodeContactGameToNeuralInputs(position);
                case PositionType.Backgame13:
                //return EncodeContactGameToNeuralInputs(position);
                case PositionType.Backgame23:
                //return EncodeContactGameToNeuralInputs(position);
                case PositionType.OtherBackgame:
                //return EncodeContactGameToNeuralInputs(position);
                case PositionType.SixPrime:
                //return EncodeContactGameToNeuralInputs(position);
                case PositionType.FivePrime:
                //return EncodeContactGameToNeuralInputs(position);
                default:
                    return EncodeContactGameToNeuralInputs(position);
            }
        }

        public static (float[] neuralInputs, string[] labels) EncodeBoardToNeuralInputs(int[] position, int modelIndex, int player = Player1, bool alwaysMirror = false)
        {
            if (alwaysMirror || MirrorBoardForPlayer2 && player == Player2)
            {
                position = MirrorBoard(position);
            }

            return modelIndex == 0
                ? EncodeNoContactBoardToNeuralInputs(position)
                : EncodeContactGameToNeuralInputs(position);
        }

        

        public static (float[] neuralInputs, string[] labels) EncodeContactGameToNeuralInputs(int[] position)
        {
            (int p1PipCount, int p2PipCount) = PipCountStatic(position);
            (int p1PipCountBackGame, int p2PipCountBackGame) = PipCountBackgameTiming(position);
            var innerBoardStrengthP1 = CountInnerBoardPoints(position, Player1);
            var innerBoardStrengthP2 = CountInnerBoardPoints(position, Player2);

            // The prime related inputs should be excluded from some contact networks
            var (primeP1, primeStartsAtP1) = CountPrimes(position, Player1);
            var (primeP2, primeStartsAtP2) = CountPrimes(position, Player2);

            var maxPercentageCut = 0.3f;
            var encodedData = new List<(float[], string[])>
            {
                //Put the non binary inputs first ordered by how often they change
                EncodePipCountPercentageNeuralInputs(p1PipCount, p2PipCount, maxPercentageCut),
                EncodePipCountDifferenceToNeuralInputsSparse(p1PipCount - p2PipCount, 15),
                EncodePipCountDifferenceToNeuralInputsSparse(p1PipCount - p2PipCount, 50),
                EncodePipCountDifferenceToNeuralInputsSparse(p1PipCount - p2PipCount, 150),
                EncodeBackgameTiming(p1PipCountBackGame, p2PipCountBackGame),
                EncodeSafePointsToNeuralInputSparse(position),//2 Inputs
                EncodeBorneOffDifferenceToNeuralInputsSparse(position),
                EncodeBorneOffNeuralInputsSparse(position),
                EncodeBorneOffDifferenceToNeuralInputs(position),
                EncodeBarCheckersToNeuralInputs(position),
                EncodePrimesToNeuralInput(position),// 6 Inputs (4,5,6 Prime)
                EncodeBlotsToNeuralInputSparse(position),// 2 inputs
                EncodeGammonSavePipCount(position),
                EncodeBackGammonSavePipCount(position),
                EncodeGammonSaveCrossoverCountSparse(position),
                EncodeInnerBoardStrength(innerBoardStrengthP1, innerBoardStrengthP2),
                EncodeDirectHits(position),
                EncodeGammonSavedNeuralInputs(position),
                EncodeRackStrengthInputs(position),
                EncodeDeadCheckers(position),
                EncodeBoard1To24ToNeuralInputs(position),
                EncodeKeithCountPercentageNeuralInputs(position, p1PipCount, p2PipCount, 0.3f),//Should shange to start at 0.12f
                EncodeCheckersITZ(position),
                EncodeDancingProbability(position,innerBoardStrengthP1,innerBoardStrengthP2),
                EncodePrimeFuelCount(position,primeStartsAtP1,primeStartsAtP2),
                EncodePrimeFlexibility(position,primeStartsAtP1,primeStartsAtP2),
                EncodePrimeIsSlotted(position,primeStartsAtP1,primeStartsAtP2),
                EncodePrimeDeepCheckers(position,primeStartsAtP1,primeStartsAtP2),
                EncodeCheckersInOpponentsHome(position),
                EncodeConnectivity(position),
                EncodeLastAnchorEscapingProbability(position),
                EncodeAnchorBetweenDeepPoint(position),
                EncodeCrunchTiming(position, p1PipCount, p2PipCount),
                EncodeTotalEscapingProbability(position),
                EncodeAnchorFrontOfPrime(position,primeStartsAtP1,primeStartsAtP2),
                EncodeKeithCountPercentageNeuralInputs(position, p1PipCount, p2PipCount, 0.12f),
                EncodeBrokenPrimesToNeuralInput(position)
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

        internal static (float[] neuralInputs, string[] labels) EncodeNoContactBoardToNeuralInputs(int[] position)
        {
            (int p1PipCount, int p2PipCount) = PipCountStatic(position);
            var maxPercentageCut = 0.3f;
            var encodedData = new List<(float[], string[])>
            {
                EncodePipCountPercentageNeuralInputs(p1PipCount, p2PipCount, maxPercentageCut),
                EncodeBorneOffDifferenceToNeuralInputsSparse(position),
                EncodeBorneOffNeuralInputsSparse(position),
                EncodePipCountDifferenceToNeuralInputsSparse(p1PipCount - p2PipCount, 15),
                EncodePipCountDifferenceToNeuralInputsSparse(p1PipCount - p2PipCount, 50),
                EncodePipCountDifferenceToNeuralInputsSparse(p1PipCount - p2PipCount, 150),
                EncodeGammonSavePipCount(position),
                EncodeBackGammonSavePipCount(position),
                EncodeGammonSaveCrossoverCountSparse(position),
                EncodeBoard1To24SparseToNeuralInputs(position, maxCheckersConsidered:5),
                EncodeKeithCountPercentageNeuralInputs(position, p1PipCount, p2PipCount, 0.3f),
                EncodeKeithCountPercentageNeuralInputs(position, p1PipCount, p2PipCount, 0.12f),
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

        private static float CutValue(float val, float min, float max)
        {
            return Math.Clamp(val, min, max);
        }

        private static float ScaleToRange(float inputVal, float inputMinVal, float inputMaxVal, float targetMinVal, float targetMaxVal)
        {
            inputVal = Math.Max(inputMinVal, Math.Min(inputVal, inputMaxVal));
            float proportion = (inputVal - inputMinVal) / (inputMaxVal - inputMinVal);
            return targetMinVal + proportion * (targetMaxVal - targetMinVal);
        }

        private static float ScaleToRangeMinus1Plus1(float inputVal, float inputMinVal, float inputMaxVal)
        {
            inputVal = Math.Max(inputMinVal, Math.Min(inputVal, inputMaxVal));
            float proportion = (inputVal - inputMinVal) / (inputMaxVal - inputMinVal);
            return 2 * proportion - 1;
        }

        private static float ScaleCheckersToInput(int checkers, int maxCheckersConsidered, bool isPlayerOne)
        {
            float normalizedCheckers = Math.Min(checkers, maxCheckersConsidered) / (float)maxCheckersConsidered;
            float scaledCheckers = normalizedCheckers * 2 - 1;
            return isPlayerOne ? scaledCheckers : -scaledCheckers;
        }

        internal static (float[] neuralInputs, string[] labels) EncodeBoard1To24ToNeuralInputs(int[] position)
        {
            var (encodedBlots, blotLabels) = EncodeBlotsToNeuralInputs(position);
            var (encodedSafePoints, safePointLabels) = EncodeSafePoints1to24ToNeuralInputs(position);
            var (encodedSpares, spareLabels) = EncodeSparesToNeuralInputs(position);
            var (encodedSpares4and5, spares4and5Labels) = Encode4and5SparesInputs(position);
            var combinedFeatures = new List<float>();
            var combinedLabels = new List<string>();

            combinedFeatures.AddRange(encodedBlots);
            combinedFeatures.AddRange(encodedSafePoints);
            combinedFeatures.AddRange(encodedSpares);
            combinedFeatures.AddRange(encodedSpares4and5);

            combinedLabels.AddRange(blotLabels);
            combinedLabels.AddRange(safePointLabels);
            combinedLabels.AddRange(spareLabels);
            combinedLabels.AddRange(spares4and5Labels);
            return (combinedFeatures.ToArray(), combinedLabels.ToArray());
        }

        private static (float[] neuralInputs, string[] labels) EncodeBlotsToNeuralInputs(int[] position)
        {
            const int NumPoints = 24;
            const int InputsPerPoint = 2;
            float[] neuralInputs = new float[NumPoints * InputsPerPoint];
            string[] labels = new string[NumPoints * InputsPerPoint];
            for (int i = 0; i < NumPoints; i++)
            {
                neuralInputs[i] = position[AcePointP1 + i] == 1 ? inputMin : inputMax;
                labels[i] = $"Blots{i + 1}P1";
                neuralInputs[i + NumPoints] = position[AcePointP1 + i] == -1 ? inputMin : inputMax;
                labels[i + NumPoints] = $"Blots{i + 1}P2";
            }
            return (neuralInputs, labels);
        }

        private static (float[] neuralInputs, string[] labels) EncodeSafePoints1to24ToNeuralInputs(int[] position)
        {
            const int NumPoints = 24;
            const int InputsPerPoint = 2;
            float[] neuralInputs = new float[NumPoints * InputsPerPoint];
            string[] labels = new string[NumPoints * InputsPerPoint];
            for (int i = 0; i < NumPoints; i++)
            {
                neuralInputs[i] = position[AcePointP1 + i] > 1 ? inputMin : inputMax;
                labels[i] = $"Safe{i + 1}P1";
                neuralInputs[i + NumPoints] = position[AcePointP1 + i] < -1 ? inputMin : inputMax;
                labels[i + NumPoints] = $"Safe{i + 1}P2";
            }
            return (neuralInputs, labels);
        }

        // The rack is true when 6, 5, 4 Point is taken but I want different strenghts for 6, 5..
        internal static (float[] neuralInputs, string[] labels) EncodeRackStrengthInputs(int[] position)
        {
            const int NumPoints = 12;
            float[] neuralInputs = new float[NumPoints];
            string[] labels = new string[NumPoints];
            for (int i = 0; i < 6; i++)
            {
                if (position[SixPointP1 - i] < 2)
                    break;
                neuralInputs[i] = inputMax;
            }
            for (int i = 0; i < 6; i++)
            {
                if (position[SixPointP2 + i] > -2)
                    break;
                neuralInputs[i + 6] = inputMax;
            }

            for (int i = 0; i < 6; i++)
            {
                labels[i] = $"P1Rack{i + 1}";
                labels[i + 6] = $"P2Rack{i + 1}";
            }
            /*if (neuralInputs[3] == inputMax || neuralInputs[9] == inputMax) {
                Console.WriteLine("RACK" + string.Join(",", points));
                Console.WriteLine("inputs" + string.Join(",", neuralInputs));
            }*/

            return (neuralInputs, labels);
        }

        /*private static float[] EncodeSparesSparseToNeuralInputs(int[] position)
        {
            const int NumPoints = 24;
            const int InputsPerPoint = 2;
            const int MaxSpares = 4;
            float[] neuralInputs = new float[NumPoints * InputsPerPoint];
            for (int i = 0; i < NumPoints; i++)
            {
                int sparesP1 = Math.Min(Math.Max(position[AcePointP1 + i] - 2, 0), MaxSpares);
                int sparesP2 = Math.Min(Math.Max(-position[AcePointP1 + i] - 2, 0), MaxSpares);
                neuralInputs[i] = ScaleToRangeMinus1Plus1(sparesP1, 0, MaxSpares);
                neuralInputs[i + NumPoints] = ScaleToRangeMinus1Plus1(sparesP2, 0, MaxSpares);
            }
            return neuralInputs;
        }*/

        // Most of the the time there will be only 3 spares on any point so lets have some special inputs for 6 and 7 spares
        internal static (float[], string[]) EncodeSparesToNeuralInputs(int[] position)
        {
            const int NumPoints = 24;
            const int MaxSpares = 3;
            const int InputsPerPoint = MaxSpares * 2;

            float[] neuralInputs = new float[NumPoints * InputsPerPoint];
            string[] labels = new string[NumPoints * InputsPerPoint];

            for (int i = 0; i < NumPoints; i++)
            {
                int sparesP1 = Math.Min(Math.Max(position[AcePointP1 + i] - 2, 0), MaxSpares);
                for (int j = 0; j < sparesP1; j++)
                {
                    neuralInputs[i * InputsPerPoint + j] = 1;
                }

                int sparesP2 = Math.Min(Math.Max(-position[AcePointP1 + i] - 2, 0), MaxSpares);
                for (int j = 0; j < sparesP2; j++)
                {
                    neuralInputs[i * InputsPerPoint + j + MaxSpares] = 1;
                }

                for (int j = 0; j < MaxSpares; j++)
                {
                    labels[i * InputsPerPoint + j] = $"P1Spares{i + 1},{j + 1}";
                    labels[i * InputsPerPoint + j + MaxSpares] = $"P2Spares{24 - i},{j + 1}";
                }
            }
            return (neuralInputs, labels);
        }

        internal static (float[], string[]) Encode4and5SparesInputs(int[] position)
        {
            float[] neuralInputs = new float[4];
            string[] labels = { "P1Spares4", "P1Spares5", "P2Spares4", "P2Spares5" };

            for (int i = 0; i < 24; i++)
            {
                int sparesP1 = position[AcePointP1 + i] - 2;
                if (sparesP1 >= 5)
                {
                    neuralInputs[0] = 1;
                    neuralInputs[1] = 1;
                }
                else if (sparesP1 == 4)
                {
                    neuralInputs[0] = 1;
                }

                int sparesP2 = -position[AcePointP1 + i] - 2;
                if (sparesP2 >= 5)
                {
                    neuralInputs[0] = 1;
                    neuralInputs[1] = 1;
                }
                else if (sparesP1 == 4)
                {
                    neuralInputs[0] = 1;
                }
            }
            return (neuralInputs, labels);
        }

        internal static (float[] neuralInputs, string[] labels) EncodeBarCheckersToNeuralInputs(int[] board)
        {
            const int MaxCheckers = 6;
            float[] neuralInputs = new float[MaxCheckers * 2];
            for (int i = 0; i < neuralInputs.Length; i++)
            {
                neuralInputs[i] = inputMin;
            }

            int p1BarCheckers = Math.Max(board[OnTheBarP1], 0);
            for (int i = 0; i < Math.Min(p1BarCheckers, MaxCheckers); i++)
            {
                neuralInputs[i] = inputMax;
            }

            int p2BarCheckers = Math.Max(-board[OnTheBarP2], 0);
            for (int i = 0; i < Math.Min(p2BarCheckers, MaxCheckers); i++)
            {
                neuralInputs[MaxCheckers + i] = inputMax;
            }

            string[] labels = new string[MaxCheckers * 2];
            for (int i = 0; i < MaxCheckers; i++)
            {
                labels[i] = $"BarCheckersP1_{i + 1}";
                labels[MaxCheckers + i] = $"BarCheckersP2_{i + 1}";
            }

            return (neuralInputs, labels);
        }

        private static float ScaleDifferenceToInput(int difference, int maxAbsoluteDifference)
        {
            // Ensure the difference does not exceed the maximum absolute difference
            difference = Math.Clamp(difference, -maxAbsoluteDifference, maxAbsoluteDifference);

            // Normalize the difference to be between -1 and 1
            float normalizedDifference = difference / (float)maxAbsoluteDifference;

            return normalizedDifference;
        }

        internal static (float[] neuralInputs, string[] labels) EncodePipCountDifferenceToNeuralInputsSparse(int pipCountDifference, int maxDifference = 40)
        {
            float[] neuralInputs =
            {
                ScaleDifferenceToInput(pipCountDifference, maxDifference),
            };
            string[] labels = { "PipCountDifference" + maxDifference };
            return (neuralInputs, labels);
        }

        public static (float[] neuralInputs, string[] labels) EncodeBorneOffDifferenceToNeuralInputsSparse(int[] board)
        {
            const int MaxDifference = 15;
            float[] neuralInputs = new float[1];
            string[] labels = { "BorneOffDifference" };

            int p1BorneOff = Math.Max(board[CheckersOffP1], 0);
            int p2BorneOff = Math.Max(-board[CheckersOffP2], 0);
            int difference = p1BorneOff - p2BorneOff;

            neuralInputs[0] = ScaleToRangeMinus1Plus1(difference, -MaxDifference, MaxDifference);

            return (neuralInputs, labels);
        }

        public static (float[] neuralInputs, string[] labels) EncodeBorneOffNeuralInputsSparse(int[] board)
        {
            int p1BorneOff = Math.Max(board[CheckersOffP1], 0);
            int p2BorneOff = Math.Max(-board[CheckersOffP2], 0);
            var p1BorneOffScaled = ScaleToRangeMinus1Plus1(p1BorneOff, 0f, 15f);
            var p2BorneOffScaled = ScaleToRangeMinus1Plus1(p2BorneOff, 0f, 15f);
            string[] labels = { "BorneOffP1", "BorneOffP2" };
            return (new[] { p1BorneOffScaled, p2BorneOffScaled }, labels);
        }

        public static (float[] neuralInputs, string[] labels) EncodePipCountPercentageNeuralInputs(int pipCountP1, int pipCountP2, float maxPercentageCut)
        {
            float percentageP1 = pipCountP1 == 0 ? inputMax : CutValue((pipCountP2 - pipCountP1) / (float)pipCountP1, -maxPercentageCut, maxPercentageCut);
            percentageP1 = ScaleToRange(percentageP1, -maxPercentageCut, maxPercentageCut, -1f, 1f);
            string[] labels = { "PipCountPercentage" };
            return (new[] { percentageP1 }, labels);
        }

        public static (float[] neuralInputs, string[] labels) EncodeKeithCountPercentageNeuralInputs(int[] position, int pipCountP1, int pipCountP2, float maxPercentageCut)
        {
            var (penaltyP1, penaltyP2) = KeithPenalty(position);
            pipCountP1 -= penaltyP1;
            pipCountP2 -= penaltyP2;
            float percentageP1 = pipCountP1 == 0 ? inputMax : CutValue((pipCountP2 - pipCountP1) / (float)pipCountP1, -maxPercentageCut, maxPercentageCut);
            percentageP1 = ScaleToRange(percentageP1, -maxPercentageCut, maxPercentageCut, -1f, 1f);
            string[] labels = { "KeithCountPercentage" };
            return (new[] { percentageP1 }, labels);
        }

        internal static (float[] neuralInputs, string[] labels) EncodeInnerBoardStrength(int innerBoardStrengthP1, int innerBoardStrengthP2)
        {
            var MaxInnerBoardStrength = 6;
            float[] neuralInputs = new float[MaxInnerBoardStrength * 2];
            for (int i = 0; i < neuralInputs.Length; i++)
            {
                neuralInputs[i] = inputMin;
            }

            for (int i = 0; i < Math.Min(innerBoardStrengthP1, MaxInnerBoardStrength); i++)
            {
                neuralInputs[i] = inputMax;
            }

            for (int i = 0; i < Math.Min(innerBoardStrengthP2, MaxInnerBoardStrength); i++)
            {
                neuralInputs[MaxInnerBoardStrength + i] = inputMax;
            }
            string[] labels = new string[MaxInnerBoardStrength * 2];
            for (int i = 0; i < MaxInnerBoardStrength; i++)
            {
                labels[i] = $"InnerBoardStrengthP1_{i + 1}";
                labels[MaxInnerBoardStrength + i] = $"InnerBoardStrengthP2_{i + 1}";
            }
            return (neuralInputs, labels);
        }

        public static (float[] neuralInputs, string[] labels) EncodeSafePointsToNeuralInputSparse(int[] position)
        {
            const int MaxSafePoints = 7;
            int safePointsP1 = CountSafePoints(position, Player1);
            int safePointsP2 = CountSafePoints(position, Player2);
            float[] neuralInputs =
            {
                safePointsP1 / (float)MaxSafePoints,
                safePointsP2 / (float)MaxSafePoints,
            };
            string[] labels = { "SafePointsP1", "SafePointsP2" };
            return (neuralInputs, labels);
        }

        public static (float[] neuralInputs, string[] labels) EncodePrimesToNeuralInput(int[] position)
        {
            var (longestPrimeP1, _) = CountPrimes(position, Player1);
            var (longestPrimeP2, _) = CountPrimes(position, Player2);

            float[] neuralInputs = new float[6];
            for (int i = 4; i <= Math.Min(longestPrimeP1, 6); i++)
            {
                neuralInputs[i - 4] = inputMax;
            }

            for (int i = 4; i <= Math.Min(longestPrimeP2, 6); i++)
            {
                neuralInputs[3 + i - 4] = inputMax;
            }

            string[] labels = { "PrimesP1_4", "PrimesP1_5", "PrimesP1_6", "PrimesP2_4", "PrimesP2_5", "PrimesP2_6" };
            return (neuralInputs, labels);
        }

        public static (float[] neuralInputs, string[] labels) EncodeBrokenPrimesToNeuralInput(int[] position)
        {
            var (longestBrokenPrimeP1, _) = CountBrokenPrimes(position, Player1);
            var (longestBrokenPrimeP2, _) = CountBrokenPrimes(position, Player2);

            float[] neuralInputs = new float[6];
            for (int i = 3; i <= Math.Min(longestBrokenPrimeP1, 5); i++)
            {
                neuralInputs[i - 3] = inputMax;
            }

            for (int i = 3; i <= Math.Min(longestBrokenPrimeP2, 5); i++)
            {
                neuralInputs[i] = inputMax;
            }

            string[] labels = { "BrokenPrimesP1_3", "BrokenPrimesP1_4", "BrokenPrimesP1_5", "BrokenPrimesP2_3", "BrokenPrimesP2_4", "BrokenPrimesP2_5" };
            return (neuralInputs, labels);
        }

        public static (float[] neuralInputs, string[] labels) EncodeBlotsToNeuralInputSparse(int[] position)
        {
            int maxCount = 10;
            int blotsP1 = Math.Min(CountBlots(position, Player1), maxCount);
            int blotsP2 = Math.Min(CountBlots(position, Player2), maxCount);

            float[] neuralInputs =
            {
                blotsP1 / (float)maxCount,
                blotsP2 / (float)maxCount,
            };
            string[] labels = { "BlotsP1", "BlotsP2" };
            return (neuralInputs, labels);
        }

        public static (float[] neuralInputs, string[] labels) EncodeBlotsToNeuralInput(int[] position)
        {
            int blotsP1 = CountBlots(position, Player1);
            int blotsP2 = CountBlots(position, Player2);

            float[] neuralInputs = new float[10];
            for (int i = 1; i <= Math.Min(blotsP1, 4); i++)
            {
                neuralInputs[i - 1] = inputMax;
            }

            for (int i = 1; i <= Math.Min(blotsP2, 4); i++)
            {
                neuralInputs[5 + i - 1] = inputMax;
            }

            neuralInputs[0] = blotsP1 == 0 ? inputMin : inputMax;
            neuralInputs[5] = blotsP2 == 0 ? inputMin : inputMax;

            string[] labels = { "BlotsP1_0", "BlotsP1_1", "BlotsP1_2", "BlotsP1_3", "BlotsP1_4", "BlotsP2_0", "BlotsP2_1", "BlotsP2_2", "BlotsP2_3", "BlotsP2_4" };
            return (neuralInputs, labels);
        }

        public static float NormalizePipCount(int pipCount, int maxPipCount = 50)
        {
            int clippedPipCount = Math.Min(pipCount, maxPipCount);
            return clippedPipCount / (float)maxPipCount;
        }

        public static (float[] neuralInputs, string[] labels) EncodeGammonSavePipCount(int[] position)
        {
            int gammonPipsPlayer1 = 0;
            int gammonPipsPlayer2 = 0;
            bool gammonSavedPlayer1, gammonSavedPlayer2;

            (gammonSavedPlayer1, gammonSavedPlayer2) = SavedGammonForBoth(position);

            if (!gammonSavedPlayer1)
            {
                for (int i = 25; i >= 7; i--)
                {
                    if (position[i] > 0)
                    {
                        int distanceToHome = i - 6;
                        gammonPipsPlayer1 += distanceToHome * position[i];
                    }
                }
            }

            if (!gammonSavedPlayer2)
            {
                for (int i = 0; i <= 18; i++)
                {
                    if (position[i] < 0)
                    {
                        int distanceToHome = 19 - i;
                        gammonPipsPlayer2 += distanceToHome * Math.Abs(position[i]);
                    }
                }
            }

            int maxPipCount = 30;
            float input1 = NormalizePipCount(gammonPipsPlayer1, maxPipCount);
            float input2 = NormalizePipCount(gammonPipsPlayer2, maxPipCount);
            string[] labels = { "GammonSavePipCountP1", "GammonSavePipCountP2" };
            return (new[] { input1, input2 }, labels);
        }

        public static (float[] neuralInputs, string[] labels) EncodeBackGammonSavePipCount(int[] position)
        {
            bool gammonSavedPlayer1, gammonSavedPlayer2;
            (gammonSavedPlayer1, gammonSavedPlayer2) = SavedGammonForBoth(position);

            int backGammonPipsPlayer1 = 0;
            int backGammonPipsPlayer2 = 0;

            if (!gammonSavedPlayer1)
            {
                for (int i = 25; i >= 19; i--)
                {
                    if (position[i] > 0)
                    {
                        int distance = i - 18;
                        backGammonPipsPlayer1 += distance * position[i];
                    }
                }
            }

            if (!gammonSavedPlayer2)
            {
                for (int i = 0; i <= 6; i++)
                {
                    if (position[i] < 0)
                    {
                        int distance = 7 - i;
                        backGammonPipsPlayer2 += distance * Math.Abs(position[i]);
                    }
                }
            }

            int maxPipCount = 30;
            float input1 = NormalizePipCount(backGammonPipsPlayer1, maxPipCount);
            float input2 = NormalizePipCount(backGammonPipsPlayer2, maxPipCount);
            string[] labels = { "BackGammonSavePipCountP1", "BackGammonSavePipCountP2" };
            return (new[] { input1, input2 }, labels);
        }

        // From the the total pipcount subtract the pipcount of the potential primed checkers in opp homeboard and on the bar
        // That should give an good estimate of how much timing we have

        public static (float[] neuralInputs, string[] labels) EncodeCrunchTiming(int[] position, int pipCountP1, int pipCountP2)
        {
            int timingP1 = pipCountP1;
            for (int i = OnTheBarP1; i >= SixPointP2; i--)
            {
                if (position[i] > 0)
                {
                    // i is the pips at point i for P1
                    timingP1 -= position[i] * i;
                }
            }

            int timingP2 = pipCountP2;
            for (int i = OnTheBarP2; i <= SixPointP1; i++)
            {
                if (position[i] < 0)
                {
                    var pips = 25 - i;
                    timingP2 -= -position[i] * pips;
                }
            }
            float inputTimingP1 = NormalizePipCount(timingP1, 60);
            float inputTimingP2 = NormalizePipCount(timingP2, 60);
            if (inputTimingP1 > 60 || inputTimingP2 > 60)
            {
                Console.WriteLine("Timing problem" + string.Join(",", position));
            }

            float maxPercentageCut = 0.5f;
            (float[] timingDiffAsPercentage, _) = EncodePipCountPercentageNeuralInputs(timingP1, timingP2, maxPercentageCut);
            float[] neuralInputs = { inputTimingP1, inputTimingP2, timingDiffAsPercentage[0] };
            string[] labels = { "CrunchTimingP1", "CrunchTimingP2", "CrunchTimingPerc" };
            return (neuralInputs, labels);
        }

        private static float[] EncodeCountAdditive(int count, int maxCount)
        {
            float[] encoded = new float[maxCount];
            for (int i = 0; i < maxCount; i++)
            {
                encoded[i] = i < count ? inputMax : inputMin;
            }
            return encoded;
        }

        public static (float[] neuralInputs, string[] labels) EncodeGammonSaveCrossoverCountSparse(int[] position)
        {
            int gammonCrossoversPlayer1 = 0;
            int gammonCrossoversPlayer2 = 0;

            for (int i = 0; i < 19; i++)
            {
                int checkersAtI = Math.Abs(position[i]);
                if (position[i] < 0)
                {
                    gammonCrossoversPlayer2 += i == 0 ? 4 * checkersAtI : i <= 7 ? 3 * checkersAtI : i <= 13 ? 2 * checkersAtI : checkersAtI;
                }
            }

            for (int i = 0; i < 19; i++)
            {
                int index = OnTheBarP1 - i;
                if (position[index] > 0)
                {
                    int checkersAtI = position[index];
                    gammonCrossoversPlayer1 += i == 0 ? 4 * checkersAtI : i <= 7 ? 3 * checkersAtI : i <= 13 ? 2 * checkersAtI : checkersAtI;
                }
            }

            int maxCount = 10;
            gammonCrossoversPlayer1 = Math.Min(gammonCrossoversPlayer1, maxCount);
            gammonCrossoversPlayer2 = Math.Min(gammonCrossoversPlayer2, maxCount);

            float[] neuralInputs =
            {
                gammonCrossoversPlayer1 / (float)maxCount,
                gammonCrossoversPlayer2 / (float)maxCount
            };
            string[] labels = { "GammonSaveCrossoversP1", "GammonSaveCrossoversP2" };
            return (neuralInputs, labels);
        }

        // The pipcount for the checkers outside the opponents home board should be quite valuable to determine how likely crunch will happen
        // Normally you say that the pipcount difference should be in a range like for 12backgame min 100 but then that depends also on how many checkers
        // have 'freedom' for instance with with 5 checkers in an 12 backgame we need more than 100 diff
        public static (float[] neuralInputs, string[] labels) EncodeBackgameTiming(int pipTimingP1, int pipTimingP2)
        {
            int crunchTreshold = 40;

            // If the timing is very small (lets say > 40) the crunch has already started so I think we can treat 0-40 as quite the same
            var timingP1 = Math.Max(pipTimingP1 - crunchTreshold, 0);
            var timingP2 = Math.Max(pipTimingP2 - crunchTreshold, 0);
            // Lets set a maxTiming for better normalization

            int maxTiming = 80;
            timingP1 = Math.Min(timingP1, maxTiming);
            timingP2 = Math.Min(timingP2, maxTiming);
            float[] neuralInputs =
            {
                timingP1 / (float)maxTiming,
                timingP2 / (float)maxTiming
            };// Should maybe scale to -1,1
            string[] labels = { "BackgameTimingP1", "BackgameTimingP2" };
            return (neuralInputs, labels);
        }

        public static (float[] neuralInputs, string[] labels) EncodeGammonSaveCrossoverCount(int[] position)
        {
            int gammonCrossoversPlayer1 = 0;
            int gammonCrossoversPlayer2 = 0;
            bool gammonSavedPlayer1, gammonSavedPlayer2;

            (gammonSavedPlayer1, gammonSavedPlayer2) = SavedGammonForBoth(position);

            if (!gammonSavedPlayer2)
            {
                for (int i = 0; i < 19; i++)
                {
                    int checkersAtI = Math.Abs(position[i]);
                    if (position[i] < 0)
                    {
                        gammonCrossoversPlayer2 += i <= 7 ? 3 * checkersAtI : i <= 13 ? 2 * checkersAtI : checkersAtI;
                    }
                }
            }

            if (!gammonSavedPlayer1)
            {
                for (int i = 0; i < 19; i++)
                {
                    int index = OnTheBarP1 - i;
                    if (position[index] > 0)
                    {
                        int checkersAtI = position[index];
                        gammonCrossoversPlayer1 += i <= 7 ? 3 * checkersAtI : i <= 13 ? 2 * checkersAtI : checkersAtI;
                    }
                }
            }

            int maxCount = 5;
            float[] gammonCrossoversP1Encoded = EncodeCountAdditive(gammonCrossoversPlayer1, maxCount);
            float[] gammonCrossoversP2Encoded = EncodeCountAdditive(gammonCrossoversPlayer2, maxCount);

            float[] combinedEncodedCrossovers = new float[maxCount * 2];
            Array.Copy(gammonCrossoversP1Encoded, 0, combinedEncodedCrossovers, 0, maxCount);
            Array.Copy(gammonCrossoversP2Encoded, 0, combinedEncodedCrossovers, maxCount, maxCount);

            string[] labels = { "GammonSaveCrossoversP1_0", "GammonSaveCrossoversP1_1", "GammonSaveCrossoversP1_2", "GammonSaveCrossoversP1_3", "GammonSaveCrossoversP1_4", "GammonSaveCrossoversP2_0", "GammonSaveCrossoversP2_1", "GammonSaveCrossoversP2_2", "GammonSaveCrossoversP2_3", "GammonSaveCrossoversP2_4" };
            return (combinedEncodedCrossovers, labels);
        }

        public static (float[] neuralInputs, string[] labels) EncodeStillContact(int[] position)
        {
            var (contact, _) = StillContact(position);
            float[] neuralInputs = { contact ? 1 : 0 };
            string[] labels = { "StillContact" };
            return (neuralInputs, labels);
        }

        public static (float[] neuralInputs, string[] labels) EncodeDirectHits(int[] position)
        {
            float[] neuralInputs = new float[12];
            for (int i = 0; i < neuralInputs.Length; i++)
            {
                neuralInputs[i] = inputMin;
            }

            for (int i = 1; i <= 24; i++)
            {
                if (position[i] == -1)
                {
                    for (int die = 1; die <= 6; die++)
                    {
                        int hitFromPoint = i + die;
                        if (hitFromPoint > OnTheBarP1)
                            break;
                        if (position[hitFromPoint] > 0)
                            neuralInputs[die - 1] = inputMax;
                    }
                }

                if (position[i] == 1)
                {
                    for (int die = 1; die <= 6; die++)
                    {
                        int hitFromPoint = i - die;
                        if (hitFromPoint < OnTheBarP2)
                            break;
                        if (position[hitFromPoint] < 0)
                            neuralInputs[die + 5] = inputMax;
                    }
                }
            }

            string[] labels = { "DirectHitsP1_1", "DirectHitsP1_2", "DirectHitsP1_3", "DirectHitsP1_4", "DirectHitsP1_5", "DirectHitsP1_6", "DirectHitsP2_1", "DirectHitsP2_2", "DirectHitsP2_3", "DirectHitsP2_4", "DirectHitsP2_5", "DirectHitsP2_6" };
            return (neuralInputs, labels);
        }

        public static (float[] neuralInputs, string[] labels) EncodeGammonSavedNeuralInputs(int[] position)
        {
            var (gammonSavedPlayer1, gammonSavedPlayer2) = SavedGammonForBoth(position);
            float[] neuralInputs =
            {
                gammonSavedPlayer1 ? inputMin : inputMax,
                gammonSavedPlayer2 ? inputMin : inputMax,
            };
            string[] labels = { "GammonSavedP1", "GammonSavedP2" };
            return (neuralInputs, labels);
        }

        public static (float[] neuralInputs, string[] labels) EncodeBorneOffDifferenceToNeuralInputs(int[] position)
        {
            const int MaxDifference = 14; // Maximum difference to represent
            float[] neuralInputs = new float[MaxDifference * 2]; // x2 for two players
            string[] labels = new string[MaxDifference * 2];

            // Initialize all inputs to inputMin and labels appropriately
            for (int i = 0; i < neuralInputs.Length; i++)
            {
                neuralInputs[i] = inputMin;
                labels[i] = i < MaxDifference ? $"BorneOffDifferenceP1_{i + 1}" : $"BorneOffDifferenceP2_{i - MaxDifference + 1}";
            }

            // Retrieve the number of borne-off checkers for each player
            int p1BorneOff = Math.Max(position[CheckersOffP1], 0);
            int p2BorneOff = Math.Max(-position[CheckersOffP2], 0); // Correction for negative representation

            int difference = p1BorneOff - p2BorneOff;

            // Cap the difference at +/- MaxDifference
            int cappedDifference = Math.Max(Math.Min(difference, MaxDifference), -MaxDifference);

            // Encode the difference
            if (cappedDifference > 0)
            {
                for (int i = 0; i < cappedDifference; i++)
                {
                    neuralInputs[i] = inputMax; // Positive difference for Player 1
                }
            }
            else if (cappedDifference < 0)
            {
                for (int i = 0; i < -cappedDifference; i++)
                {
                    neuralInputs[MaxDifference + i] = inputMax; // Negative difference for Player 2
                }
            }

            return (neuralInputs, labels);
        }

        internal static (float[], string[]) EncodeBoard1To24SparseToNeuralInputs(int[] position, int maxCheckersConsidered)
        {
            //const int MaxCheckersConsidered = 5;

            const int NumPoints = 24; // 24 Boardpoints to scan
            float[] neuralInputs = new float[NumPoints * 2]; // 24 inputs representing the board state
            //string[] labels = new string[NumPoints];
            for (int i = 0; i < NumPoints; i++)
            {
                var checkersP1 = Math.Min(maxCheckersConsidered, Math.Max(position[i + 1], 0));
                var checkersP2 = Math.Min(maxCheckersConsidered, Math.Max(-position[i + 1], 0));
                //if (points[i + 1] > 0)
                //{   // Shuld maybe replace with new scaling function
                // Player 1's checkers

                neuralInputs[i] = ScaleToRangeMinus1Plus1(checkersP1, 0, maxCheckersConsidered);
                // Player 2's checkers, invert the sign of points since they are negative
                neuralInputs[i + NumPoints] = ScaleToRangeMinus1Plus1(checkersP2, 0, maxCheckersConsidered);
            }

            string[] labels = new string[NumPoints * 2];
            for (int i = 0; i < NumPoints; i++)
            {
                labels[i] = $"CheckersP1_{i + 1}";
                labels[NumPoints + i] = $"CheckersP2_{i + 1}";
            }

            return (neuralInputs, labels);
        }

        // Check possible direct hits can be  0..6 (6 if all die are a direct hit) 
        public static int CountDirectHits(int[] position, int player)
        {
            var (encodedDirectHits, _) = EncodeDirectHits(position);
            var hits = 0;
            for (int die = 0; die < 6; die++)
            {
                var encodedIndex = player == Player1 ? die + 6 : die;
                if (encodedDirectHits[encodedIndex] > 0)
                {
                    hits++;
                }
            }
            return hits;
        }

        // Count dead checkers if there are more than 2 checkers on 1 point we have dead checkers.
        // if 1 point is taken and 3 there are more checkers than 2 on the 2 point we we have more dead checkers..
        // Lets have 6 * 2 inputs counting max 6 dead checkers
        public static (float[], string[]) EncodeDeadCheckers(int[] position)
        {
            var maxDeadCheckers = 6;
            float[] neuralInputs = new float[maxDeadCheckers * 2]; // 24 inputs representing the board state

            (var deadCheckersP1, var deadCheckersP2) = DeadCheckers(position);
            for (int i = 0; i < Math.Min(deadCheckersP1, maxDeadCheckers); i++)
            {
                neuralInputs[i] = inputMax;
            }

            for (int i = 0; i < Math.Min(deadCheckersP2, maxDeadCheckers); i++)
            {
                neuralInputs[i + maxDeadCheckers] = inputMax;
            }

            string[] labels = new string[maxDeadCheckers * 2];
            for (int i = 0; i < maxDeadCheckers; i++)
            {
                labels[i] = $"DeadCheckersP1_{i + 1}";
                labels[maxDeadCheckers + i] = $"DeadCheckersP2_{i + 1}";
            }
            return (neuralInputs, labels);
        }

        // When opponent have 8 checkers In The Zone its small danger to split 
        // 9 is medium danger 10 is quite big.
        // Even though 8,9,10 is most valuable I think 6 to 11 is valuable info
        // With 9,10 almost never split into blitz pos
        public static (float[], string[]) EncodeCheckersITZ(int[] position)
        {
            var inTheZoneRange = 6; // 6,7,8,9,10,11
            float[] neuralInputs = new float[inTheZoneRange * 2]; // 12 inputs representing the board state
            (var itzP1, var itzP2) = CheckersInTheZone(position);
            itzP1 = Math.Min(itzP1, 11);
            for (int i = 0; i < itzP1 - 5; i++)
            {
                neuralInputs[i] = 1;
            }
            itzP2 = Math.Min(itzP2, 11);
            for (int i = 0; i < itzP2 - 5; i++)
            {
                neuralInputs[i + inTheZoneRange] = 1;
            }
            string[] labels = new string[inTheZoneRange * 2];
            for (int i = 0; i < inTheZoneRange; i++)
            {
                labels[i] = $"ITZP1_{i + 1}";
                labels[inTheZoneRange + i] = $"ITZP2_{i + 1}";
            }
            return (neuralInputs, labels);
        }

        // Dancing probabilities
        public static (float[], string[]) EncodeDancingProbability(int[] position, int innerBoardStrengthP1, int innerBoardStrengthP2)
        {
            //The first two values can be valuable for estimating the future even if not on the bar
            var danceProbP1 = danceProbabilities[innerBoardStrengthP2];
            var danceProbP2 = danceProbabilities[innerBoardStrengthP1];
            bool p1OnTheBar = position[OnTheBarP1] > 0;
            bool p2OnTheBar = -position[OnTheBarP2] > 0;
            float currentDanceProbP1 = p1OnTheBar ? danceProbP1 : 0f;
            float currentDanceProbP2 = p2OnTheBar ? danceProbP2 : 0f;
            float[] neuralInputs = { danceProbP1, danceProbP2, currentDanceProbP1, currentDanceProbP2 };
            string[] labels = { "danceProbP1", "danceProbP2", "CurrentDanceProbP1", "CurrentDanceProbP2" };
            /*if (p2OnTheBar)
            { 
                Console.WriteLine(neuralInputs[2] + ";" + neuralInputs[3] );
            }*/
            return (neuralInputs, labels);
        }

        // How likely is that we can't run when blocked by the opponent (should maybe be just one input)
        public static (float[], string[]) EncodeLastAnchorEscapingProbability(int[] position)
        {
            int blockingPointsForP1 = LastAnchorOrCheckerIsBlocked(position, Player1);
            int blockingPointsForP2 = LastAnchorOrCheckerIsBlocked(position, Player2);
            //Not being able to start running vs N blocking point is as likely as dancing on a N point board
            float escapingProbP1 = danceProbabilities[blockingPointsForP1];
            float escapingProbP2 = danceProbabilities[blockingPointsForP2];
            float[] neuralInputs = { escapingProbP1, escapingProbP2 };
            string[] labels = { "escapeProbP1", "escapeProbP2" };

            return (neuralInputs, labels);
        }

        private static void AddRunningNumbers(int runningFromPoint, int[] position, bool[] runningNumbers, int player)
        {
            if (player == Player1)
            {
                for (int i = 1; i <= 6; i++)
                {
                    if (position[runningFromPoint - i] >= -1)// Player 1 run towards low numbers
                    {
                        runningNumbers[i - 1] = true;
                    }
                }
            }
            else
            {
                for (int i = 1; i <= 6; i++)
                {
                    if (position[runningFromPoint + i] <= 1)
                    {
                        runningNumbers[i - 1] = true;
                    }
                }
            }
        }

        private static float RunningNumbersAsProbability(bool[] runningNumbers)
        {
            int totalRunningNumbers = 0;
            foreach (var runningNumber in runningNumbers)
            {
                if (runningNumber)
                {
                    totalRunningNumbers++;
                }
            }
            return runningProbabilities[totalRunningNumbers];
        }

        // Splitting the back checkers often help escaping probabilities so lets find all checkers in opp home and see what numbers can be used to run
        // So returning a high probability indicates that here is no running problem, but I think if there is no checker to escape we should also treat as no running problem
        public static (float[], string[]) EncodeTotalEscapingProbability(int[] position)
        {
            bool[] runningNumbersForP1 = new bool[6];
            bool checkerFoundP1 = false;
            for (int runFromCandidate = AcePointP2; runFromCandidate >= GoldenPointP2; runFromCandidate--)
            {
                if (position[runFromCandidate] >= 1)
                {
                    checkerFoundP1 = true;
                    AddRunningNumbers(runFromCandidate, position, runningNumbersForP1, Player1);
                }
            }

            bool[] runningNumbersForP2 = new bool[6];
            bool checkerFoundP2 = false;
            for (int runFromCandidate = AcePointP1; runFromCandidate <= GoldenPointP1; runFromCandidate++)
            {
                if (position[runFromCandidate] <= -1)
                {
                    checkerFoundP2 = true;
                    AddRunningNumbers(runFromCandidate, position, runningNumbersForP2, Player2);
                }
            }
            //If no checker to escape found we treat is as we have max freedom
            var runProbabilityP1 = checkerFoundP1 ? RunningNumbersAsProbability(runningNumbersForP1) : 1.0f;
            var runProbabilityP2 = checkerFoundP2 ? RunningNumbersAsProbability(runningNumbersForP2) : 1.0f;
            float[] neuralInputs = { runProbabilityP1, runProbabilityP2 };
            string[] labels = { "runProbP1", "runProbP2" };
            return (neuralInputs, labels);
        }

        // For evaluating the primes chances to roll forward and become longer its important to know
        // Amount of checkers from start of prime and 6 points backward (including spares and blots behind the prime)
        // Lets treat all checkers beyond 6 as fuel for rolling the prime

        public static (float[], string[]) EncodePrimeFuelCount(int[] position, int p1PrimeStartsAt, int p2PrimeStartsAt)
        {
            var dontIncludeFirstCheckers = 6;
            //var maxCount = 15;
            var primeFuelCountP1 = -dontIncludeFirstCheckers;
            for (int i = p1PrimeStartsAt; i < p1PrimeStartsAt + 6; i++)
            {
                if (position[i] > 0) // For the prime points this will always be true but opp might a blot behind the prime
                    primeFuelCountP1 += position[i];
            }
            var primeFuelCountP2 = -dontIncludeFirstCheckers;
            for (int i = p2PrimeStartsAt; i > p2PrimeStartsAt - 6; i--)
            {
                if (position[i] < 0)
                    primeFuelCountP2 -= position[i];
            }
            var maxFuelCheckers = 9;
            float[] neuralInputs = new float[maxFuelCheckers * 2]; // 12 inputs representing the board state
            for (int i = 0; i < primeFuelCountP1; i++)
            {
                neuralInputs[i] = inputMax;
            }
            for (int i = 0; i < primeFuelCountP2; i++)
            {
                neuralInputs[i + maxFuelCheckers] = inputMax;
            }
            string[] labels = new string[maxFuelCheckers * 2];
            for (int i = 0; i < maxFuelCheckers; i++)
            {
                labels[i] = $"PrimeFuel{i}P1";
                labels[i + maxFuelCheckers] = $"PrimeFuel{i}P2";
            }

            //Console.WriteLine("PrimeFuelCountP1(>6):" + primeFuelCountP1 + " : " + primeFuelCountP2);
            //Console.WriteLine("inputs" + string.Join(",", neuralInputs));
            return (neuralInputs, labels);
        }

        // 2. Spare Points + blots, While 1 gives the amount of checkers its a big difference on how they are distributed (preferrably not stacked)
        // So I think the number of points with one or more spares + blots behind the prime can be valuable as well
        // It might be a bit strange that if we if we from startpos play 13/10 we now have a 1prime with flexibility 3 but it can be valuable
        // for the early game also to evaluate how likely we can take the golden

        public static (float[], string[]) EncodePrimeFlexibility(int[] position, int p1PrimeStartsAt, int p2PrimeStartsAt)
        {
            var maxFlexibility = 6;
            var primeFlexibilityP1 = 0;
            for (int i = p1PrimeStartsAt; i < p1PrimeStartsAt + 6; i++)
            {
                if (position[i] == 1 || position[i] > 2) // For the prime points this will always be true but opp might a blot behind the prime
                    primeFlexibilityP1++;
            }
            var primeFlexibilityP2 = 0;
            for (int i = p2PrimeStartsAt; i > p2PrimeStartsAt - 6; i--)
            {
                if (-position[i] == 1 || -position[i] > 2) // For the prime points this will always be true but opp might a blot behind the prime
                    primeFlexibilityP2++;
            }

            var neuralInputs = new float[maxFlexibility * 2];
            for (int i = 0; i < primeFlexibilityP1; i++)
            {
                neuralInputs[i] = inputMax;
            }
            for (int i = 0; i < primeFlexibilityP2; i++)
            {
                neuralInputs[i + maxFlexibility] = inputMax;
            }

            var labels = new string[maxFlexibility * 2];
            for (int i = 0; i < maxFlexibility; i++)
            {
                labels[i] = $"PrimeFlex{i}P1";
                labels[i + maxFlexibility] = $"PrimeFlex{i}P2";
            }

            //Console.WriteLine("PrimeFlexP1: " + primeFlexibilityP1 + " : " + primeFlexibilityP2);
            //Console.WriteLine("inputs" + string.Join(",", neuralInputs));
            return (neuralInputs, labels);
        }

        // 3. If Front of the prime is slotted could also be valuable info        
        public static (float[], string[]) EncodePrimeIsSlotted(int[] position, int p1PrimeStartsAt, int p2PrimeStartsAt)
        {
            // Player 1
            bool slottedPrimeP1 = p1PrimeStartsAt != AcePointP1 && position[p1PrimeStartsAt - 1] == 1;
            bool slottedPrimeP2 = p2PrimeStartsAt != AcePointP2 && position[p2PrimeStartsAt + 1] == -1;
            float[] neuralInputs = {
                slottedPrimeP1 ? inputMax : inputMin,
                slottedPrimeP2 ? inputMax : inputMin
            };
            /*if (slottedPrimeP1 || slottedPrimeP2) {
                Console.WriteLine("slottedPrime" + slottedPrimeP1 + ", " + slottedPrimeP2+ string.Join(",", position));
            }*/
            string[] labels = { "slottedPrimeP1", "slottedPrimeP2" };
            return (neuralInputs, labels);
        }


        // 4. Deep checkers makes it harder to roll the prime as they will probably not ever become prime fuel
        // (unless recirculated or until the prime reachers those checkers).
        // When opponent needs to escape with one checker these deepcheckers also means we have fewer oufield guards
        // they hurt as double in the containment game
        public static (float[], string[]) EncodePrimeDeepCheckers(int[] position, int p1PrimeStartsAt, int p2PrimeStartsAt)
        {
            var deepCheckersP1 = 0;
            for (int i = p1PrimeStartsAt - 2; i >= AcePointP1; i--)
            {
                if (position[i] > 0)
                {
                    deepCheckersP1 += position[i];
                }
            }
            var maxDeepCheckers = 4;
            deepCheckersP1 = Math.Min(deepCheckersP1, maxDeepCheckers);

            var deepCheckersP2 = 0;
            for (int i = p2PrimeStartsAt + 2; i <= AcePointP2; i++)
            {
                if (position[i] < 0)
                {
                    deepCheckersP2 -= position[i];
                }
            }

            deepCheckersP2 = Math.Min(deepCheckersP2, maxDeepCheckers);

            var neuralInputs = new float[maxDeepCheckers * 2];
            for (int i = 0; i < deepCheckersP1; i++)
            {
                neuralInputs[i] = inputMax;
            }
            for (int i = 0; i < deepCheckersP2; i++)
            {
                neuralInputs[i + maxDeepCheckers] = inputMax;
            }
            var labels = new string[maxDeepCheckers * 2];
            for (int i = 0; i < maxDeepCheckers; i++)
            {
                labels[i] = $"PrimeDeepChecke{i}P1";
                labels[i + maxDeepCheckers] = $"PrimeDeepChecker{i}P2";
            }

            //Console.WriteLine("PrimeDeepCheckersP1:" + deepCheckersP1 + ":" + deepCheckersP2);
            //Console.WriteLine("inputs"+ string.Join(",", neuralInputs));
            return (neuralInputs, labels);
        }

        // 3. If Front of the prime is slotted could also be valuable info        
        public static (float[], string[]) EncodeAnchorFrontOfPrime(int[] position, int p1PrimeStartsAt, int p2PrimeStartsAt)
        {
            // Player 1
            bool P1AnchorNearP2Prime = p2PrimeStartsAt != AcePointP2 && position[p2PrimeStartsAt + 1] >= 2;
            bool P2AnchorNearP1Prime = p1PrimeStartsAt != AcePointP1 && position[p1PrimeStartsAt - 1] <= -2;

            float[] neuralInputs = {
                P1AnchorNearP2Prime ? inputMax : inputMin,
                P2AnchorNearP1Prime ? inputMax : inputMin,
            };
            /*if (P2AnchorNearP1Prime) {
                Console.WriteLine("AnchorNearPrime" + P1AnchorNearP2Prime + ", "+ P2AnchorNearP1Prime 
                    + string.Join(",", position));
            }*/

            string[] labels = { "P1anchorFrontOfP2Prime", "P2AnchorFrontOfP1Prime" };
            return (neuralInputs, labels);
        }

        public static (float[], string[]) EncodeCheckersInOpponentsHome(int[] position)
        {
            var maxCount = 6;
            var neuralInputs = new float[maxCount * 2];
            var (checkersP1, checkersP2) = CheckersInOppHomeBoard(position);
            checkersP1 = Math.Min(checkersP1, maxCount);
            checkersP2 = Math.Min(checkersP2, maxCount);
            for (int i = 0; i < checkersP1; i++)
            {
                neuralInputs[i] = inputMax;
            }
            for (int i = 0; i < checkersP2; i++)
            {
                neuralInputs[i + maxCount] = inputMax;
            }
            var labels = new string[maxCount * 2];
            for (int i = 0; i < maxCount; i++)
            {
                labels[i] = $"CheckerInOppHomeBoard{i}P1";
                labels[i + maxCount] = $"CheckerInOppHomeBoard{i}P2";
            }

            //Console.WriteLine("checkerOppHomeP1:" + checkersP1 + " : " + checkersP2);
            //Console.WriteLine("inputs"+ string.Join(",", neuralInputs));
            return (neuralInputs, labels);
        }

        public static (float[], string[]) EncodeConnectivity(int[] position)
        {
            var longestDistanceCount = 20;// If 20 is a problem for he position it probably is makes very little diff if its longer 
            var minDistanceCount = 8;
            var connectivityP1 = minDistanceCount;
            var connectivityP2 = minDistanceCount;
            var prevPoint = -1;//Use -1 to indicate we have not found a checker yet
            for (int i = OnTheBarP1; i >= AcePointP1; i--)
            {
                if (prevPoint == -1)
                {
                    if (position[i] > 0)
                    {
                        prevPoint = i;
                    }
                }
                else
                {
                    if (position[i] > 0)
                    {
                        var distance = prevPoint - i;
                        if (distance > connectivityP1)
                        {
                            connectivityP1 = distance;
                        }
                        prevPoint = i;
                    }
                }
            }
            connectivityP1 = Math.Min(connectivityP1, longestDistanceCount);

            prevPoint = -1;
            for (int i = OnTheBarP2; i <= AcePointP2; i++)
            {
                if (prevPoint == -1)
                {
                    if (position[i] < 0)
                    {
                        prevPoint = i;
                    }
                }
                else
                {
                    if (position[i] < 0)
                    {
                        var distance = i - prevPoint;
                        if (distance > connectivityP2)
                        {
                            connectivityP2 = distance;
                        }
                        prevPoint = i;
                    }
                }
            }

            connectivityP2 = Math.Min(connectivityP2, longestDistanceCount);
            var inputP1 = ScaleToRangeMinus1Plus1(connectivityP1, minDistanceCount, longestDistanceCount);
            var inputP2 = ScaleToRangeMinus1Plus1(connectivityP2, minDistanceCount, longestDistanceCount);
            /*if (connectivityP1 > 8 || connectivityP2 > 8) {
                BackgammonBoard board = new BackgammonBoard();
                board.Position = points;
                Console.WriteLine(board);
                Console.WriteLine("ConnectP1: " + connectivityP1 + " ,connp2: " + connectivityP2);
            }*/

            float[] inputs = { inputP1, inputP2 };
            string[] labels = { "ConnectivityP1", "ConnectivityP2" };
            return (inputs, labels);
        }

        // Its Valuable to get an 'advanced ' anchor between deep points  
        internal static (float[], string[]) EncodeAnchorBetweenDeepPoint(int[] position)
        {
            int deepPoint = 1000;//Just some big number
            bool player2Anchor = false;
            for (int i = AcePointP1; i <= GoldenPointP1; i++)
            {
                if (position[i] >= 2 && position[i] < deepPoint)
                {
                    deepPoint = i;
                }
                else if (position[i] <= -2 && i > deepPoint)
                {
                    player2Anchor = true;
                    break;
                }
            }

            deepPoint = -1000;//Just some big number
            bool player1Anchor = false;
            for (int i = AcePointP2; i >= GoldenPointP2; i--)
            {
                if (position[i] <= -2 && position[i] > deepPoint)
                {
                    deepPoint = i;
                }
                else if (position[i] >= 2 && i < deepPoint)
                {
                    player1Anchor = true;
                    break;
                }
            }
            var player1AnchorInput = player1Anchor ? inputMax : inputMin;
            var player2AnchorInput = player2Anchor ? inputMax : inputMin;
            float[] neuralInputs = { player1AnchorInput, player2AnchorInput };
            string[] labels = { "Player1AnchorBeetween", "Player2AnchorBeetween" };

            return (neuralInputs, labels);
        }

        

  
    }
}