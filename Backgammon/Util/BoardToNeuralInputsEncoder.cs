using Backgammon.Models;
using static Backgammon.Util.Constants;

namespace Backgammon.Util
{
    public static class BoardToNeuralInputsEncoder
    {
        private static readonly float inputMin = -1;
        private static readonly float inputMax = 1;

        public static int MapBoardToModel(int[] board)
        {
            return BackgammonBoard.StillContact(board) ? 1 : 0;
        }

        public static (float[] neuralInputs, string[] labels) EncodeBoardToNeuralInputs(int[] position, PositionType positionType, int player = BackgammonBoard.Player1, bool alwaysMirror = false)
        {
            if (alwaysMirror || (MirrorBoardForPlayer2 && player == BackgammonBoard.Player2))
            {
                position = BackgammonBoard.MirrorBoard(position);
            }
            switch (positionType)
            {
                case PositionType.NoContact:
                    // Code to handle Value1
                    return EncodeNoContactBoardToNeuralInputs(position);
                case PositionType.Backgame12://For now we only have two different input encoders
                    //return EncodeContactGameToNeuralInputs(position);
                case PositionType.Backgame13:
                    //return EncodeContactGameToNeuralInputs(position);
                case PositionType.Backgame23:
                    //return EncodeContactGameToNeuralInputs(position);
                case PositionType.SixPrime:
                    //return EncodeContactGameToNeuralInputs(position);
                case PositionType.FivePrime:
                    //return EncodeContactGameToNeuralInputs(position);
                default:
                    return EncodeContactGameToNeuralInputs(position);
            }
        }
    

        public static (float[] neuralInputs, string[] labels) EncodeBoardToNeuralInputs(int[] points, int modelIndex, int player = BackgammonBoard.Player1, bool alwaysMirror = false)
        {
            if (alwaysMirror || (MirrorBoardForPlayer2 && player == BackgammonBoard.Player2))
            {
                points = BackgammonBoard.MirrorBoard(points);
            }

            return modelIndex == 0
                ? EncodeNoContactBoardToNeuralInputs(points)
                : EncodeContactGameToNeuralInputs(points);
        }

        public static (float[] neuralInputs, string[] labels) EncodeContactGameToNeuralInputs(int[] points)
        {
            (int p1PipCount, int p2PipCount) = BackgammonBoard.PipCountStatic(points);
            (int p1PipCountBackGame, int p2PipCountBackGame) = BackgammonBoard.PipCountBackgameTiming(points);
            var encodedData = new List<(float[], string[])>
            {
                EncodeBackgameTiming(p1PipCountBackGame, p2PipCountBackGame),
                EncodeBorneOffDifferenceToNeuralInputs(points),
                EncodeBoard1To24SemiSparseToNeuralInputs(points),
                EncodeBarCheckersToNeuralInputs(points),
                EncodeBorneOffDifferenceToNeuralInputsSparse(points),
                EncodeBorneOffNeuralInputsSparse(points),
                EncodePipCountPercentageNeuralInputs(p1PipCount, p2PipCount),
                EncodePipCountDifferenceToNeuralInputsSparse(p1PipCount - p2PipCount, 15),
                EncodePipCountDifferenceToNeuralInputsSparse(p1PipCount - p2PipCount, 50),
                EncodePipCountDifferenceToNeuralInputsSparse(p1PipCount - p2PipCount, 150),
                EncodeSafePointsToNeuralInputSparse(points),//2 Inputs
                EncodePrimesToNeuralInput(points),// 6 Inputs (4,5,6 Prime)
                EncodeBlotsToNeuralInputSparse(points),// 2 inputs
                EncodeGammonSavePipCount(points),
                EncodeBackGammonSavePipCount(points),
                EncodeGammonSaveCrossoverCountSparse(points),
                EncodeInnerBoardStrength(points),
                EncodeDirectHits(points),
                EncodeGammonSavedNeuralInputs(points)
            };

            var combinedFeatures = new List<float>();
            var combinedLabels = new List<string>();

            foreach (var (features, labels) in encodedData)
            {
                combinedFeatures.AddRange(features);
                /*if (features.Length != labels.Length) {
                    Console.WriteLine("first label" + labels[0] + "features" + features.Length + ":" +labels.Length);
                    //Console.WriteLine("combined length" + combinedFeatures.Count);
                }*/

                combinedLabels.AddRange(labels);
            }

            return (combinedFeatures.ToArray(), combinedLabels.ToArray());
        }

        private static (float[] neuralInputs, string[] labels) EncodeNoContactBoardToNeuralInputs(int[] points)
        {
            (int p1PipCount, int p2PipCount) = BackgammonBoard.PipCountStatic(points);

            var encodedData = new List<(float[], string[])>
            {
                EncodePipCountPercentageNeuralInputs(p1PipCount, p2PipCount),
                EncodeBorneOffDifferenceToNeuralInputsSparse(points),
                EncodeBorneOffNeuralInputsSparse(points),
                EncodePipCountDifferenceToNeuralInputsSparse(p1PipCount - p2PipCount, 15),
                EncodePipCountDifferenceToNeuralInputsSparse(p1PipCount - p2PipCount, 50),
                EncodePipCountDifferenceToNeuralInputsSparse(p1PipCount - p2PipCount, 150),
                EncodeGammonSavePipCount(points),
                EncodeBackGammonSavePipCount(points),
                EncodeGammonSaveCrossoverCountSparse(points),
                EncodeBoard1To24SparseToNeuralInputs(points, maxCheckersConsidered:5)
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
            return targetMinVal + (proportion * (targetMaxVal - targetMinVal));
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

        private static (float[] neuralInputs, string[] labels) EncodeBoard1To24SemiSparseToNeuralInputs(int[] points)
        {
            var (encodedBlots, blotLabels) = EncodeBlotsToNeuralInputs(points);
            var (encodedSafePoints, safePointLabels) = EncodeSafePoints1to24ToNeuralInputs(points);
            var encodedSpares = EncodeSparesToNeuralInputs(points);

            var combinedFeatures = new List<float>();
            var combinedLabels = new List<string>();

            combinedFeatures.AddRange(encodedBlots);
            combinedFeatures.AddRange(encodedSafePoints);
            combinedFeatures.AddRange(encodedSpares);

            combinedLabels.AddRange(blotLabels);
            combinedLabels.AddRange(safePointLabels);
            for (int i = 0; i < encodedSpares.Length; i++)
            {
                combinedLabels.Add($"Spares{i + 1}");
            }

            return (combinedFeatures.ToArray(), combinedLabels.ToArray());
        }

        private static (float[] neuralInputs, string[] labels) EncodeBlotsToNeuralInputs(int[] points)
        {
            const int NumPoints = 24;
            const int InputsPerPoint = 2;
            float[] neuralInputs = new float[NumPoints * InputsPerPoint];
            string[] labels = new string[NumPoints * InputsPerPoint];
            for (int i = 0; i < NumPoints; i++)
            {
                neuralInputs[i] = points[BackgammonBoard.AcePointP1 + i] == 1 ? inputMin : inputMax;
                labels[i] = $"Blots{i + 1}P1";
                neuralInputs[i + NumPoints] = points[BackgammonBoard.AcePointP1 + i] == -1 ? inputMin : inputMax;
                labels[i + NumPoints] = $"Blots{i + 1}P2";
            }
            return (neuralInputs, labels);
        }

        private static (float[] neuralInputs, string[] labels) EncodeSafePoints1to24ToNeuralInputs(int[] points)
        {
            const int NumPoints = 24;
            const int InputsPerPoint = 2;
            float[] neuralInputs = new float[NumPoints * InputsPerPoint];
            string[] labels = new string[NumPoints * InputsPerPoint];
            for (int i = 0; i < NumPoints; i++)
            {
                neuralInputs[i] = points[BackgammonBoard.AcePointP1 + i] > 1 ? inputMin : inputMax;
                labels[i] = $"Safe{i + 1}P1";
                neuralInputs[i + NumPoints] = points[BackgammonBoard.AcePointP1 + i] < -1 ? inputMin : inputMax;
                labels[i + NumPoints] = $"Safe{i + 1}P2";
            }
            return (neuralInputs, labels);
        }

        private static float[] EncodeSparesToNeuralInputs(int[] points)
        {
            const int NumPoints = 24;
            const int InputsPerPoint = 2;
            const int MaxSpares = 4;
            float[] neuralInputs = new float[NumPoints * InputsPerPoint];
            for (int i = 0; i < NumPoints; i++)
            {
                int sparesP1 = Math.Min(Math.Max(points[BackgammonBoard.AcePointP1 + i] - 2, 0), MaxSpares);
                int sparesP2 = Math.Min(Math.Max(-points[BackgammonBoard.AcePointP1 + i] - 2, 0), MaxSpares);
                neuralInputs[i] = ScaleToRangeMinus1Plus1(sparesP1, 0, MaxSpares);
                neuralInputs[i + NumPoints] = ScaleToRangeMinus1Plus1(sparesP2, 0, MaxSpares);
            }
            return neuralInputs;
        }

        private static (float[] neuralInputs, string[] labels) EncodeBarCheckersToNeuralInputs(int[] board)
        {
            const int MaxCheckers = 6;
            float[] neuralInputs = new float[MaxCheckers * 2];
            for (int i = 0; i < neuralInputs.Length; i++)
            {
                neuralInputs[i] = inputMin;
            }

            int p1BarCheckers = Math.Max(board[BackgammonBoard.OnTheBarP1], 0);
            for (int i = 0; i < Math.Min(p1BarCheckers, MaxCheckers); i++)
            {
                neuralInputs[i] = inputMax;
            }

            int p2BarCheckers = Math.Max(-board[BackgammonBoard.OnTheBarP2], 0);
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

        private static (float[] neuralInputs, string[] labels) EncodePipCountDifferenceToNeuralInputsSparse(int pipCountDifference, int maxDifference = 40)
        {
            float[] neuralInputs =
            {
                ScaleDifferenceToInput(pipCountDifference, maxDifference),
            };
            string[] labels = { "PipCountDifference" };
            return (neuralInputs, labels);
        }

        public static (float[] neuralInputs, string[] labels) EncodeBorneOffDifferenceToNeuralInputsSparse(int[] board)
        {
            const int MaxDifference = 15;
            float[] neuralInputs = new float[1];
            string[] labels = { "BorneOffDifference" };

            int p1BorneOff = Math.Max(board[BackgammonBoard.CheckersOffP1], 0);
            int p2BorneOff = Math.Max(-board[BackgammonBoard.CheckersOffP2], 0);
            int difference = p1BorneOff - p2BorneOff;

            neuralInputs[0] = ScaleToRangeMinus1Plus1(difference, -MaxDifference, MaxDifference);

            return (neuralInputs, labels);
        }

        public static (float[] neuralInputs, string[] labels) EncodeBorneOffNeuralInputsSparse(int[] board)
        {
            int p1BorneOff = Math.Max(board[BackgammonBoard.CheckersOffP1], 0);
            int p2BorneOff = Math.Max(-board[BackgammonBoard.CheckersOffP2], 0);
            var p1BorneOffScaled = ScaleToRangeMinus1Plus1(p1BorneOff, 0f, 15f);
            var p2BorneOffScaled = ScaleToRangeMinus1Plus1(p2BorneOff, 0f, 15f);
            string[] labels = { "BorneOffP1", "BorneOffP2" };
            return (new[] { p1BorneOffScaled, p2BorneOffScaled }, labels);
        }

        public static (float[] neuralInputs, string[] labels) EncodePipCountPercentageNeuralInputs(int pipCountP1, int pipCountP2)
        {
            var maxPercentageCut = 0.3f;
            float percentageP1 = pipCountP1 == 0 ? inputMax : CutValue((pipCountP2 - pipCountP1) / (float)pipCountP1, -maxPercentageCut, maxPercentageCut);
            percentageP1 = ScaleToRange(percentageP1, -maxPercentageCut, maxPercentageCut, -1f, 1f);
            string[] labels = { "PipCountPercentage" };
            return (new[] { percentageP1 }, labels);
        }

        private static int CountInnerBoardPoints(int[] board, int player)
        {
            int safeCount = 0;
            for (int point = 1; point <= 6; point++)
            {
                if ((player == BackgammonBoard.Player1 && board[point] >= 2) || (player == BackgammonBoard.Player2 && board[point + 18] <= -2))
                {
                    safeCount++;
                }
            }
            return safeCount;
        }

        private static (float[] neuralInputs, string[] labels) EncodeInnerBoardStrength(int[] board)
        {
            var MaxInnerBoardStrength = 6;
            float[] neuralInputs = new float[MaxInnerBoardStrength * 2];
            for (int i = 0; i < neuralInputs.Length; i++)
            {
                neuralInputs[i] = inputMin;
            }

            var innerBoardStrengthP1 = CountInnerBoardPoints(board, BackgammonBoard.Player1);
            for (int i = 0; i < Math.Min(innerBoardStrengthP1, MaxInnerBoardStrength); i++)
            {
                neuralInputs[i] = inputMax;
            }

            var innerBoardStrengthP2 = CountInnerBoardPoints(board, BackgammonBoard.Player2);
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

        public static (float[] neuralInputs, string[] labels) EncodeSafePointsToNeuralInputSparse(int[] board)
        {
            const int MaxSafePoints = 7;
            int safePointsP1 = BackgammonBoard.CountSafePoints(board, BackgammonBoard.Player1);
            int safePointsP2 = BackgammonBoard.CountSafePoints(board, BackgammonBoard.Player2);
            float[] neuralInputs =
            {
                safePointsP1 / (float)MaxSafePoints,
                safePointsP2 / (float)MaxSafePoints,
            };
            string[] labels = { "SafePointsP1", "SafePointsP2" };
            return (neuralInputs, labels);
        }        

        public static (float[] neuralInputs, string[] labels) EncodePrimesToNeuralInput(int[] board)
        {
            int longestPrimeP1 = BackgammonBoard.CountPrimes(board, BackgammonBoard.Player1);
            int longestPrimeP2 = BackgammonBoard.CountPrimes(board, BackgammonBoard.Player2);

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

        public static (float[] neuralInputs, string[] labels) EncodeBlotsToNeuralInputSparse(int[] board)
        {
            int maxCount = 10;
            int blotsP1 = Math.Min(BackgammonBoard.CountBlots(board, BackgammonBoard.Player1), maxCount);
            int blotsP2 = Math.Min(BackgammonBoard.CountBlots(board, BackgammonBoard.Player2), maxCount);

            float[] neuralInputs =
            {
                blotsP1 / (float)maxCount,
                blotsP2 / (float)maxCount,
            };
            string[] labels = { "BlotsP1", "BlotsP2" };
            return (neuralInputs, labels);
        }

        public static (float[] neuralInputs, string[] labels) EncodeBlotsToNeuralInput(int[] board)
        {
            int blotsP1 = BackgammonBoard.CountBlots(board, BackgammonBoard.Player1);
            int blotsP2 = BackgammonBoard.CountBlots(board, BackgammonBoard.Player2);

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

        public static (float[] neuralInputs, string[] labels) EncodeGammonSavePipCount(int[] board)
        {
            int gammonPipsPlayer1 = 0;
            int gammonPipsPlayer2 = 0;
            bool gammonSavedPlayer1, gammonSavedPlayer2;

            (gammonSavedPlayer1, gammonSavedPlayer2) = BackgammonBoard.SavedGammonForBoth(board);

            if (!gammonSavedPlayer1)
            {
                for (int i = 25; i >= 7; i--)
                {
                    if (board[i] > 0)
                    {
                        int distanceToHome = i - 6;
                        gammonPipsPlayer1 += distanceToHome * board[i];
                    }
                }
            }

            if (!gammonSavedPlayer2)
            {
                for (int i = 0; i <= 18; i++)
                {
                    if (board[i] < 0)
                    {
                        int distanceToHome = 19 - i;
                        gammonPipsPlayer2 += distanceToHome * Math.Abs(board[i]);
                    }
                }
            }

            int maxPipCount = 30;
            float input1 = NormalizePipCount(gammonPipsPlayer1, maxPipCount);
            float input2 = NormalizePipCount(gammonPipsPlayer2, maxPipCount);
            string[] labels = { "GammonSavePipCountP1", "GammonSavePipCountP2" };
            return (new[] { input1, input2 }, labels);
        }

        public static (float[] neuralInputs, string[] labels) EncodeBackGammonSavePipCount(int[] board)
        {
            bool gammonSavedPlayer1, gammonSavedPlayer2;
            (gammonSavedPlayer1, gammonSavedPlayer2) = BackgammonBoard.SavedGammonForBoth(board);

            int backGammonPipsPlayer1 = 0;
            int backGammonPipsPlayer2 = 0;

            if (!gammonSavedPlayer1)
            {
                for (int i = 25; i >= 19; i--)
                {
                    if (board[i] > 0)
                    {
                        int distance = i - 18;
                        backGammonPipsPlayer1 += distance * board[i];
                    }
                }
            }

            if (!gammonSavedPlayer2)
            {
                for (int i = 0; i <= 6; i++)
                {
                    if (board[i] < 0)
                    {
                        int distance = 7 - i;
                        backGammonPipsPlayer2 += distance * Math.Abs(board[i]);
                    }
                }
            }

            int maxPipCount = 30;
            float input1 = NormalizePipCount(backGammonPipsPlayer1, maxPipCount);
            float input2 = NormalizePipCount(backGammonPipsPlayer2, maxPipCount);
            string[] labels = { "BackGammonSavePipCountP1", "BackGammonSavePipCountP2" };
            return (new[] { input1, input2 }, labels);
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

        public static (float[] neuralInputs, string[] labels) EncodeGammonSaveCrossoverCountSparse(int[] board)
        {
            int gammonCrossoversPlayer1 = 0;
            int gammonCrossoversPlayer2 = 0;

            for (int i = 0; i < 19; i++)
            {
                int checkersAtI = Math.Abs(board[i]);
                if (board[i] < 0)
                {
                    gammonCrossoversPlayer2 += i == 0 ? 4 * checkersAtI : i <= 7 ? 3 * checkersAtI : i <= 13 ? 2 * checkersAtI : checkersAtI;
                }
            }

            for (int i = 0; i < 19; i++)
            {
                int index = BackgammonBoard.OnTheBarP1 - i;
                if (board[index] > 0)
                {
                    int checkersAtI = board[index];
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
            var timingP1 = Math.Max((pipTimingP1 - crunchTreshold), 0);
            var timingP2 = Math.Max((pipTimingP2 - crunchTreshold), 0);
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

        public static (float[] neuralInputs, string[] labels) EncodeGammonSaveCrossoverCount(int[] board)
        {
            int gammonCrossoversPlayer1 = 0;
            int gammonCrossoversPlayer2 = 0;
            bool gammonSavedPlayer1, gammonSavedPlayer2;

            (gammonSavedPlayer1, gammonSavedPlayer2) = BackgammonBoard.SavedGammonForBoth(board);

            if (!gammonSavedPlayer2)
            {
                for (int i = 0; i < 19; i++)
                {
                    int checkersAtI = Math.Abs(board[i]);
                    if (board[i] < 0)
                    {
                        gammonCrossoversPlayer2 += i <= 7 ? 3 * checkersAtI : i <= 13 ? 2 * checkersAtI : checkersAtI;
                    }
                }
            }

            if (!gammonSavedPlayer1)
            {
                for (int i = 0; i < 19; i++)
                {
                    int index = BackgammonBoard.OnTheBarP1 - i;
                    if (board[index] > 0)
                    {
                        int checkersAtI = board[index];
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

        public static (float[] neuralInputs, string[] labels) EncodeStillContact(int[] board)
        {
            bool contact = BackgammonBoard.StillContact(board);
            float[] neuralInputs = { contact ? 1 : 0 };
            string[] labels = { "StillContact" };
            return (neuralInputs, labels);
        }

        public static (float[] neuralInputs, string[] labels) EncodeDirectHits(int[] board)
        {
            float[] neuralInputs = new float[12];
            for (int i = 0; i < neuralInputs.Length; i++)
            {
                neuralInputs[i] = inputMin;
            }

            for (int i = 1; i <= 24; i++)
            {
                if (board[i] == -1)
                {
                    for (int die = 1; die <= 6; die++)
                    {
                        int hitFromPoint = i + die;
                        if (hitFromPoint > BackgammonBoard.OnTheBarP1)
                            break;
                        if (board[hitFromPoint] > 0)
                            neuralInputs[die - 1] = inputMax;
                    }
                }

                if (board[i] == 1)
                {
                    for (int die = 1; die <= 6; die++)
                    {
                        int hitFromPoint = i - die;
                        if (hitFromPoint < BackgammonBoard.OnTheBarP2)
                            break;
                        if (board[hitFromPoint] < 0)
                            neuralInputs[die + 5] = inputMax;
                    }
                }
            }

            string[] labels = { "DirectHitsP1_1", "DirectHitsP1_2", "DirectHitsP1_3", "DirectHitsP1_4", "DirectHitsP1_5", "DirectHitsP1_6", "DirectHitsP2_1", "DirectHitsP2_2", "DirectHitsP2_3", "DirectHitsP2_4", "DirectHitsP2_5", "DirectHitsP2_6" };
            return (neuralInputs, labels);
        }

        public static (float[] neuralInputs, string[] labels) EncodeGammonSavedNeuralInputs(int[] board)
        {
            var (gammonSavedPlayer1, gammonSavedPlayer2) = BackgammonBoard.SavedGammonForBoth(board);
            float[] neuralInputs =
            {
                gammonSavedPlayer1 ? inputMin : inputMax,
                gammonSavedPlayer2 ? inputMin : inputMax,
            };
            string[] labels = { "GammonSavedP1", "GammonSavedP2" };
            return (neuralInputs, labels);
        }

        public static (float[] neuralInputs, string[] labels) EncodeBorneOffDifferenceToNeuralInputs(int[] board)
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
            int p1BorneOff = Math.Max(board[BackgammonBoard.CheckersOffP1], 0);
            int p2BorneOff = Math.Max(-board[BackgammonBoard.CheckersOffP2], 0); // Correction for negative representation

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

        private static (float[], string[]) EncodeBoard1To24SparseToNeuralInputs(int[] points, int maxCheckersConsidered)
        {
            //const int MaxCheckersConsidered = 5;

            const int NumPoints = 24; // 24 Boardpoints to scan
            float[] neuralInputs = new float[NumPoints*2]; // 24 inputs representing the board state
            //string[] labels = new string[NumPoints];
            for (int i = 0; i < NumPoints; i++)
            {
                var checkersP1 = Math.Min(maxCheckersConsidered, Math.Max(points[i + 1], 0));
                var checkersP2 = Math.Min(maxCheckersConsidered, Math.Max(-points[i + 1], 0));
                //if (points[i + 1] > 0)
                //{   // Shuld maybe replace with new scaling function
                    // Player 1's checkers
                    
                neuralInputs[i] = ScaleToRangeMinus1Plus1(checkersP1, 0, maxCheckersConsidered);
                // Player 2's checkers, invert the sign of points since they are negative
                neuralInputs[i+NumPoints] = ScaleToRangeMinus1Plus1(checkersP2, 0, maxCheckersConsidered);
                
            }

            string[] labels = new string[NumPoints * 2];
            for (int i = 0; i < NumPoints; i++)
            {
                labels[i] = $"CheckersP1_{i + 1}";
                labels[NumPoints+i] = $"CheckersP2_{i + 1}";
            }

            return (neuralInputs, labels);
        }

        // Check possible direct hits can be  0..6 (6 if all die are a direct hit) 
        public static int CountDirectHits(int[] board, int player)
        {
            var (encodedDirectHits,_ )= EncodeDirectHits(board);
            var hits = 0;
            for (int die = 0; die < 6; die++)
            {
                var encodedIndex = (player == BackgammonBoard.Player1) ? die + 6 : die;
                if (encodedDirectHits[encodedIndex] > 0)
                {
                    hits++;
                }
            }
            return hits;
        }
    }
}