using Backgammon.Models;
using Backgammon.Models.NeuralNetwork;
using Backgammon.Util.AI;
using Backgammon.Util;
using System.CodeDom;

namespace Backgammon.Utils
{
    public static class ScoreUtility
    {
        public static float[] EvaluatePosition(int[] board, int player, NeuralNetwork[] neuralNetworks, float clampMin, float clampMax, bool gameEnded = false, bool debug = false) {
            return EvaluatePosition(board, player, neuralNetworks, [], clampMin, clampMax,gameEnded, debug);
        }

        // Consider moving to score utility
        // The equity , scorevector should not be mirrored ie if player1 is better the equity should be > 0 regardless player to move
        public static float[] EvaluatePosition(int[] board, int player, NeuralNetwork[] neuralNetworks, Dictionary<string, float[]> bearOffDatabase, float clampMin, float clampMax, bool gameEnded = false, bool debug=false)
        {
            // if gameEnded is not provided as arg we need to check it
            if (gameEnded || BackgammonBoard.GameEndedStatic(board))
            {
                var gameEndedEval = BackgammonBoard.ScoreAsVector(board);
                //gameEndedEval = gameEndedEval.Select(score => Math.Clamp(score, clampMin, clampMax)).ToArray();
                //_trainLogger.Information("Game Ended score" + string.Join(", ", gameEndedEval));
                return gameEndedEval;
            }

            if (BeafOffUtility.IsBearOffPosition(board)) {
                if (player == BackgammonBoard.Player2) { 
                    board = BackgammonBoard.MirrorBoard(board);
                }

                var key = BeafOffUtility.ConvertBearOffBoardToString(board);
                if (bearOffDatabase.ContainsKey(key))
                {
                    return player == BackgammonBoard.Player2 ? MirrorScore(bearOffDatabase[key]) : bearOffDatabase[key];
                }
            }

            float[] inputData;
            var modelIndex = BoardToNeuralInputsEncoder.MapBoardToModel(board);
            
            (inputData, _) = BoardToNeuralInputsEncoder.EncodeBoardToNeuralInputs(board, modelIndex, player);
            if (debug)
            {
                Console.WriteLine("Pl=: " + player+ " Board" + string.Join(", ", board) );
                Console.WriteLine("Pl=: " + player + " Inputdata" + string.Join(", ", inputData));
            }
            var predict = neuralNetworks[modelIndex].FeedForward(inputData);

            if (debug)
            {
                Console.WriteLine($"\n Predicted + {string.Join(", ", predict)}");
            }
            
            if (Constants.MirrorBoardForPlayer2 && player == BackgammonBoard.Player2)
            {
                predict = MirrorScore(predict);
                if (debug)
                {
                    Console.WriteLine($"\n Mirrored + {string.Join(", ", predict)}");
                }
                
            }
            
            predict = AdjustEstimatedScore(predict, board, clampMin, clampMax);
            if (debug) { 
                Console.WriteLine("Ev Adjusted" + string.Join(", ", predict));
            }
            return predict;
        }

        public static float[] AdjustProbabilities(float[] scoreVector)
        {
            // Adjust Player 1's probabilities
            float exclusiveBackgammonP1 = scoreVector[2]; // Probability of backgammon (exclusive)
            float exclusiveGammonP1 = scoreVector[1] - exclusiveBackgammonP1; // Subtract backgammon probability
            float exclusiveWinP1 = scoreVector[0] - scoreVector[1]; // Subtract gammon (inclusive of backgammon) probability

            // Adjust Player 2's probabilities
            float exclusiveBackgammonP2 = scoreVector[5]; // Probability of backgammon (exclusive)
            float exclusiveGammonP2 = scoreVector[4] - exclusiveBackgammonP2; // Subtract backgammon probability
            float exclusiveWinP2 = scoreVector[3] - scoreVector[4]; // Subtract gammon (inclusive of backgammon) probability

            return
            [
                exclusiveWinP1, exclusiveGammonP1, exclusiveBackgammonP1,
                exclusiveWinP2, exclusiveGammonP2, exclusiveBackgammonP2
            ];
        }

        public static float CalculateEquity(float[] scoreVector)
        {
            var adjustedProbs = AdjustProbabilities(scoreVector);

            // Calculate the equity for Player 1 and Player 2
            float equityP1 = adjustedProbs[0] + 2 * adjustedProbs[1] + 3 * adjustedProbs[2];
            float equityP2 = adjustedProbs[3] + 2 * adjustedProbs[4] + 3 * adjustedProbs[5];

            // Net equity might be the difference or another function of the two equities
            float netEquity = equityP1 - equityP2;

            return netEquity;
        }

        public static float[] AdjustEstimatedScore(float[] estimatedScore, int[] board, float clampMin = 0, float clampMax = 1)
        {
            var p1SavedGammon = BackgammonBoard.SavedGammon(board, BackgammonBoard.Player1);
            if (p1SavedGammon)
            {
                //P2 can't win gammon or Backgammon
                estimatedScore[4] = 0f;
                estimatedScore[5] = 0f;
            }
            else if (BackgammonBoard.SavedBackgammon(board, BackgammonBoard.Player1))
            {
                //P2 can't win Backgammon
                estimatedScore[5] = 0f;
            }
            var p2SavedGammon = BackgammonBoard.SavedGammon(board, BackgammonBoard.Player2);
            if (p2SavedGammon)
            {
                //P1 can't win gammon or Backgammon
                estimatedScore[1] = 0f;
                estimatedScore[2] = 0f;
            }
            else if (BackgammonBoard.SavedBackgammon(board, BackgammonBoard.Player2))
            {
                //P1 can't win Backgammon
                estimatedScore[2] = 0f;
            }

            estimatedScore = estimatedScore.Select(score => Math.Clamp(score, clampMin, clampMax)).ToArray();
            // Player 2 win estimate should be 1 - Player win estimate
            // It needs some more thaught though since we mirror boards..
            estimatedScore[3] = 1f - estimatedScore[0]; // This is unclear when modify the range to not be 0-1

            return estimatedScore;
        }

        public static float[] MirrorScore(float[] score)
        {
            // Ensure the score array has exactly 6 elements
            if (score.Length == 6)
            {
                float[] mirroredScore =
                [
                    // Rearrange the elements to mirror the score
                    score[3],
                    score[4],
                    score[5],
                    score[0],
                    score[1],
                    score[2],
                ];
                return mirroredScore;
            }
            else
            {
                throw new ArgumentException("Score array must have 6 elements for mirroring");
            }
        }
    }
}
