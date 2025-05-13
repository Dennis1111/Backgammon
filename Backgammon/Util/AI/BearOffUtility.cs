using Backgammon.Models;
using Backgammon.Models.NeuralNetwork;
using Backgammon.Utils;
using System.Text;
using static Backgammon.Util.Constants;

namespace Backgammon.Util.AI
{
    internal class BearOffUtility
    {
        // The maximum number of checkers that can be on the board for a bear-off position
        public static int MaxCheckers = 6;

        public static string ConvertBearOffBoardToString(int[] boardPosition)
        {
            StringBuilder sb = new StringBuilder(12);
            for (int i = 1; i <= 6; i++) // Looping over player 1 home board
            {
                var digit = (char)('0' + boardPosition[i]); // Ensures that the number is converted to the corresponding character
                sb.Append(digit);  // Append character representation of the integer
            }

            for (int i = 19; i <= 24; i++) // Looping over player 2 home board
            {
                var digit = (char)('0' - boardPosition[i]); // For negative numbers, convert to positive and add to '0'
                sb.Append(digit);
            }

            return sb.ToString();  // Convert StringBuilder to string
        }

        /*//
        public static string ConvertBearOffBoardToString(int[] boardPosition)
        {
            StringBuilder sb = new(12);
            for (int i = 1; i <= 6; i++) // Player 1's positions
            {
                char value = (char)(boardPosition[i]);
                sb.Append(value);  // Directly append as character
            }

            for (int i = 19; i <= 24; i++) // Player 2's positions
            {
                char value = (char)(-boardPosition[i]); // Negate to make positive
                sb.Append(value);  // Directly append as character
            }

            return sb.ToString();  // Convert StringBuilder to string
        }*/

        //Condition for when we will use the bear off Database
        public static bool IsBearOffPosition(int[] boardPos) {
            for (int i = 0; i <= 18;i++) {//
                if (boardPos[i] < 0) {
                    return false;
                }
                if (boardPos[i+7] > 0)
                {
                    return false;
                }
            }

            /*for (int i = BackgammonBoard.OnTheBarP1; i <= 6; i++)
            {// Scan the homeboard for opp checkers
                if (boardPos[i] < 0)
                {
                    return false;
                }
            }

            for (int i = BackgammonBoard.OnTheBarP2; i <= BackgammonBoard.OnTheBarP2; i++)
            {// Scan the homeboard for opp checkers
                if (boardPos[i] < 0)
                {
                    return false;
                }
            }*/

            /*if (boardPos[BackgammonBoard.OnTheBarP1] != 0 || boardPos[BackgammonBoard.OnTheBarP2] != 0) {
                return false;
            }*/

            if (boardPos[BackgammonBoard.BearOffP1] < (15 - MaxCheckers))
                return false;
            if (-boardPos[BackgammonBoard.BearOffP2] < (15 - MaxCheckers))
                return false;
            return true;
        }

        public static bool PosExistsInDatabase(int[] board, int player, Dictionary<string, float[]> bearOffDataBase) {
            if (player == BackgammonBoard.Player2) { 
                board= BackgammonBoard.MirrorBoard(board);
            }
            var key = ConvertBearOffBoardToString(board);
            return bearOffDataBase.ContainsKey(key);
        }

        public static List<(int, int)> GenerateIJPairs(int maxCheckers)
        {
            var pairs = new List<(int, int)>();

            if (maxCheckers <= 0) return pairs; // Ensure maxCheckers is valid

            for (int i = 1; i <= maxCheckers; i++)
            {
                for (int j = 1; j <= i; j++)
                {
                    pairs.Add((i, j));
                    if (i != j) // Add (j, i) only if i != j to avoid duplicate pairs
                    {
                        pairs.Add((j, i));
                    }
                }
            }

            return pairs;
        }

        // For all bearoff positions we assume player one 1 is one move so when evaluating a pos with player 2 first mirror the board
        public static Dictionary<string, float[]> CreateBearOffDataBase(Dictionary<string, float[]> bearOffDict)
        {
            var backgammonBoard = new BackgammonBoard();
            var rand = new Random();
            //var bearOffDict = new Dictionary<string, float[]>();
            NeuralNetwork[] neuralNetworks = []; // Min Max requires these but will not be used when creating bearoff
            var minMaxUtility = new MinMaxUtility(bearOffDict);
            minMaxUtility.createBearOffDatabase();
            var ijPairs = GenerateIJPairs(MaxCheckers);

            foreach (var pair in ijPairs)
            {
                int i = pair.Item1;
                int j = pair.Item2;

                var bearOffPositionsCompact = CreateBearOffPositions(i, j);
                foreach (var bearOffPosCompact in bearOffPositionsCompact)
                {
                    var bearOffPosition = generateBoardPosfromHomeBoards(bearOffPosCompact);
                    var key = ConvertBearOffBoardToString(bearOffPosition);
                    if (bearOffDict.ContainsKey(key)) {
                        continue;
                    }
                    backgammonBoard.Position = bearOffPosition;
                        
                    var ply = 10000;
                    var (evaluation, scoreVector) = minMaxUtility.EvaluatePositionAverage(bearOffPosition, BackgammonBoard.Player1, ply);
                    
                    /*if (i != j) {
                        var mirroredBearOffPosition = BackgammonBoard.MirrorBoard(bearOffPosition);
                        var mirroredScoreVector = ScoreUtility.MirrorScore(scoreVector);
                        var mirroredKey = ConvertBearOffBoardToString(mirroredBearOffPosition);
                        bearOffDict.Add(mirroredKey, mirroredScoreVector);
                    }*/

                    if (rand.NextDouble() > 0.99)
                    {
                        Console.WriteLine("\n" + backgammonBoard);
                        Console.WriteLine("scv" + string.Join(", ", scoreVector));
                    }

                    bearOffDict.Add(key, scoreVector);
                }
            }
            return bearOffDict;
        }

        // Convert compact bearoff position representation to an actual board , compact version has positive values 
        // but on a board we use negaive for player 2
        private static int[] generateBoardPosfromHomeBoards(int[] bearOffPointsCompactRepr)
        {
            int[] bearOffPosition = new int[28];
            var checkersOffP1 = 15;
            var checkersOffP2 = 15;
            for (int i = 0; i < 6; i++)
            {
                var checkers = bearOffPointsCompactRepr[i];
                if (checkers == 0)
                {
                    continue;
                }
                checkersOffP1 -= checkers;
                bearOffPosition[BackgammonBoard.AcePointP1 + i] = checkers;
            }

            for (int i = 0; i < 6; i++)
            {
                var checkers = bearOffPointsCompactRepr[i+6];
                if (checkers == 0)
                {
                    continue;
                }
                checkersOffP2 -= checkers;
                bearOffPosition[BackgammonBoard.AcePointP2 - i] = -checkers;
            }

            bearOffPosition[BackgammonBoard.BearOffP1] = checkersOffP1;
            bearOffPosition[BackgammonBoard.BearOffP2] = -checkersOffP2;
            return bearOffPosition;
        }

        /// <summary>
        /// Create a list of int arrays of size 12 representing the home board points for both players
        /// index 0..5 homeboard p1 and 6..11 homeboard p2
        /// </summary>
        /// <param name="remainingCheckersP1"></param>
        /// <param name="remainingCheckersP2"></param>
        /// <returns></returns>
        public static List<int[]> CreateBearOffPositions(int remainingCheckersP1, int remainingCheckersP2)
        {
            var positionsP1 = CreateBearOffPositionsForOneSide(remainingCheckersP1); // Returns an int[6] for the checkers in the home board
            var positionsP2 = CreateBearOffPositionsForOneSide(remainingCheckersP2);
            List<int[]> combinedPositions = new List<int[]>();

            foreach (var posP1 in positionsP1.AsEnumerable().Reverse())
            {
                foreach (var posP2 in positionsP2.AsEnumerable().Reverse())
                {
                    int[] combined = new int[12]; // Compact version for just the home board positions
                    Array.Copy(posP1, 0, combined, 0, 6); // Copy player 1's positions into positions 0 to 5 of the combined array
                    Array.Copy(posP2, 0, combined, 6, 6); // Copy player 2's positions into positions 6 to 11 of the combined array
                    combinedPositions.Add(combined);
                }
            }
            return combinedPositions;
        }
        /*private static void GeneratePositions(int[] current, int index, int remainingCheckers, List<int[]> positions)
        {
            if (index < 0)
            {
                positions.Add((int[])current.Clone()); // Add the configuration to the list
                return;
            }

            for (int i = 0; i <= remainingCheckers; i++)
            {
                current[index] = i; // Place i checkers in the current position
                GeneratePositions(current, index - 1, remainingCheckers - i, positions);
            }
        }

        private static List<int[]> CreateBearOffPositionsForOneSide(int remainingCheckers)
        {
            List<int[]> positions = new List<int[]>();
            GeneratePositions(new int[6], 5, remainingCheckers, positions); // Start from the last index (5) of a 6-length array
            return positions;
        }*/

        
        private static List<int[]> CreateBearOffPositionsForOneSide(int remainingCheckers)
        {
            List<int[]> positions = [];
            GeneratePositions(new int[6], 0, remainingCheckers, positions);
            return positions;
        }

        private static void GeneratePositions(int[] current, int index, int remainingCheckers, List<int[]> positions)
        {
            if (index == current.Length - 1)
            {
                current[index] = remainingCheckers; // Put all remaining checkers in the last slot
                positions.Add((int[])current.Clone()); // Add the configuration to the list
                return;
            }

            for (int i = 0; i <= remainingCheckers; i++)
            {
                current[index] = i; // Place i checkers in the current position
                GeneratePositions(current, index + 1, remainingCheckers - i, positions);
            }
        }
    }
}
