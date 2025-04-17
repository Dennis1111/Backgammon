using Backgammon.Models;
using Backgammon.Utils;
using static Backgammon.Util.Constants;
using static Backgammon.Models.BackgammonBoard;
namespace Backgammon.Util.AI
{
    internal class MinMaxUtility
    {
        private int _leafCounter = 0;
        private int _maxLeaves = 1000;
        private float _clampMin = 0f;
        private float _clampMax = 1f;
        private readonly Dictionary<string, float[]> _bearOffDatabase;
        private bool _isCreatingBearOffDatabase=false;
        private Dictionary<PositionType, IBackgammonPositionEvaluator> _positionEvaluators;
        
        public MinMaxUtility(Dictionary<string, float[]> bearOffDatabase)
        {
            _bearOffDatabase = bearOffDatabase;
            _positionEvaluators = [];
            _isCreatingBearOffDatabase = true;
        }
        public MinMaxUtility(Dictionary<PositionType, IBackgammonPositionEvaluator> positionEvaluators)
        {
            _positionEvaluators = positionEvaluators;
            _bearOffDatabase = ((BearoffDatabaseEvaluator)positionEvaluators[PositionType.BearOffDatabase]).BearOffDatabase;           
        }

        public void useClampValues(float min, float max) {
            _clampMin = min;
            _clampMax = max;
        }

        public void createBearOffDatabase() { 
            _isCreatingBearOffDatabase = true;
        }

        public int LeafCounter => _leafCounter;

        public void ResetLeafCounter()
        {
            _leafCounter = 0;
        }

        /// <summary>
        /// Given a Backgammon position and player and rolled we can evaluate each moveCandidate , larger ply gives more accurate eval
        /// It's assumed this function is only called when there are legal moves 
        /// (since we have already called BackgammonBoard.GenerateLegalMovesStatic(board, die1, die2, currentPlayer);)
        /// it should maybe be an argument instead
        /// </summary>
        /// <param name="board"></param>
        /// <param name="currentPlayer"></param>
        /// <param name="die1"></param>
        /// <param name="die2"></param>
        /// <param name="plyDepth"></param>
        /// <param name="neuralNetwork"></param>
        /// <returns>An ordered list of moves with best move first based on the moves equity</returns>
        internal List<MoveData> EvaluateMoveCandidates(int[] board, int currentPlayer, int die1, int die2, int plyDepth, List<MoveData> moveCandidates=null)
        {
            ResetLeafCounter();
            List<MoveData> moveDatas = [];
            if (moveCandidates is null)
            {

                var movesAndBoards = GenerateLegalMovesStatic(board, die1, die2, currentPlayer);
                foreach (var moveAndBoard in movesAndBoards)
                {
                    var newBoard = moveAndBoard.board;
                    var move = moveAndBoard.move;
                    // shall we decrease ply here ?
                    var (equity, scoreVector) = EvaluatePositionAverage(newBoard, BackgammonGameHelper.Opponent(currentPlayer), plyDepth - 1);
                    var moveData = new MoveData(currentPlayer, board, newBoard, move, equity, scoreVector);
                    moveDatas.Add(moveData);
                }
            }
            else {
                foreach (var moveCandidate in moveCandidates) {
                    var newBoard = moveCandidate.BoardAfter;
                    var (equity, scoreVector) = EvaluatePositionAverage(newBoard, BackgammonGameHelper.Opponent(currentPlayer), plyDepth - 1);
                    var moveData = new MoveData(currentPlayer, board, newBoard, moveCandidate.Move, equity, scoreVector);
                    moveDatas.Add(moveData);
                }
            }
            if (currentPlayer == Player1)
            {
                // Sort descending for PLAYER_1 to maximize equity
                moveDatas.Sort((x, y) => y.Equity.CompareTo(x.Equity));
            }
            else
            {
                // Sort ascending for PLAYER_2 to minimize equity
                moveDatas.Sort((x, y) => x.Equity.CompareTo(y.Equity));
            }
            //Console.WriteLine("Leaf"+  LeafCounter);
            return moveDatas;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="board"></param>
        /// <param name="currentPlayer"></param>
        /// <param name="die1"></param>
        /// <param name="die2"></param>
        /// <param name="plyDepth"></param>
        /// <param name="neuralNetwork"></param>
        /// <returns>The minmax equity and scoreVector for this position</returns>
        internal (float, float[]) MinMax(int[] board, int currentPlayer, int die1, int die2, int plyDepth)
        {
            //Console.WriteLine("Minmax board" + String.Join(",", board));
            var gameEnded = GameEndedStatic(board);
            var isBearOffLeave = BearOffUtility.IsBearOffPosition(board);
            if (isBearOffLeave && _isCreatingBearOffDatabase) {
                if (!BearOffUtility.PosExistsInDatabase(board, currentPlayer, _bearOffDatabase)) {
                    isBearOffLeave = false;
                }
            }

            if (plyDepth <= 0 || gameEnded || isBearOffLeave)
            {
                _leafCounter++;
                var scoreVector = ScoreUtility.EvaluatePosition(board, currentPlayer, _positionEvaluators, gameEnded);
                var equity = ScoreUtility.CalculateEquity(scoreVector);
                return (equity, scoreVector);
            }
            var bestEquity = currentPlayer == Player1 ? float.NegativeInfinity : float.PositiveInfinity;
            var bestScoreVector = new float[6];

            List<(Move move , int[] board)> movesAndBoards = GenerateLegalMovesStatic(board, die1, die2, currentPlayer);
            if (movesAndBoards.Count == 0)
            {
                // Since the player can't move the evaluation of this position is the average for this position with the opponent as currentPlayer
                return EvaluatePositionAverage(board, BackgammonGameHelper.Opponent(currentPlayer), plyDepth - 1);
            }
            else
            {
                Dictionary<int[], float> heuristicEvaluations = [];
                // Create a list of tuples to store move, board, equity, and win percentages
                List<(Move move, int[] board, float equity, float[] winPercentages)> evaluatedMoves = new List<(Move, int[], float, float[])>();

                // Perform a 0-ply heuristic search first
                foreach (var moveAndBoard in movesAndBoards)
                {
                    var heuristicPosition = moveAndBoard.board;
                    var heuristicEvaluation = EvaluatePositionAverage(heuristicPosition, BackgammonGameHelper.Opponent(currentPlayer), 0);
                    evaluatedMoves.Add((moveAndBoard.move, heuristicPosition, heuristicEvaluation.averageEquity, heuristicEvaluation.averageScores));
                }

                // Sort the list by equity
                if (currentPlayer == Player1)
                {
                    evaluatedMoves.Sort((a, b) => b.equity.CompareTo(a.equity)); // Maximize for Player1
                }
                else
                {
                    evaluatedMoves.Sort((a, b) => a.equity.CompareTo(b.equity)); // Minimize for Player2
                }
                var bestHeuristicMove = evaluatedMoves.First();
                if (plyDepth == 1) {
                    return (bestHeuristicMove.equity, bestHeuristicMove.winPercentages);
                }
                float bestHeuristicValue = bestHeuristicMove.equity;
                                
                // Set an absolute threshold to prune suboptimal moves
                float thresholdMargin = 0.2f;
                // Set a threshold to prune suboptimal moves (for example, 20% worse)
                float threshold = currentPlayer == Player1 ? bestHeuristicValue - thresholdMargin: bestHeuristicValue + thresholdMargin;
                
                /*var backgammonBoard = new BackgammonBoard();
                backgammonBoard.Position = board;
                backgammonBoard.Die1 = die1;
                backgammonBoard.Die2 = die2;
                backgammonBoard.CurrentPlayer = currentPlayer;
                Console.WriteLine("\n\nStarting pos\n" + backgammonBoard);
                Console.WriteLine("\nNumber of candidates" + movesAndBoards.Count);*/
                int skippedPositions = 0;
                int positionCount = 0;
                int maxPositions = 2;
                foreach (var evaluatedMove in evaluatedMoves.Take(maxPositions))
                {
                    positionCount++;
                    //Console.WriteLine("position" + positionCount);
                    var newBoard = evaluatedMove.board;
                    // This part should never be true and probably be removed in my new version
                    if ((currentPlayer == Player1 && evaluatedMove.equity < threshold) ||
                        (currentPlayer == Player2 && evaluatedMove.equity > threshold))
                    {
                        //backgammonBoard.Position = newBoard;
                        //Console.WriteLine("Bad Move\n" + evaluatedMove.move.MovesAsStandardNotation());
                        //Console.WriteLine("skipping bad position\n" + backgammonBoard);
                        //Skip this move as it's much worse than the best found heuristic value
                        skippedPositions++;
                        continue;
                    }

                    var move = evaluatedMove.move;
                    var (equity, scoreVector) = EvaluatePositionAverage(newBoard, BackgammonGameHelper.Opponent(currentPlayer), plyDepth -1);
                    if ((currentPlayer == Player1 && equity > bestEquity) ||
                        (currentPlayer == Player2 && equity < bestEquity))
                    {
                        bestEquity = equity;
                        bestScoreVector = scoreVector;
                        //backgammonBoard.Position = evaluatedMove.board;
                        //Console.WriteLine("New Best Move\n" + evaluatedMove.move.MovesAsStandardNotation());
                        //Console.WriteLine("New Best position\n" + backgammonBoard);
                    }/*
                    else {
                        Console.WriteLine("Close but not new best Move\n" + evaluatedMove.move.MovesAsStandardNotation());
                        Console.WriteLine("New Best position\n" + backgammonBoard);
                    }*/
                }
                //Console.WriteLine("Skipped positions" + skippedPositions+ "posCount" + positionCount);
            }
            return (bestEquity, bestScoreVector);
        }

        /// <summary>
        /// For a given position and player on turn calculate the average equity based on a minmax search
        /// If ply = 1 it will call MinMax with ply = 0 and the it will just evaluate the same (leaf) board pos 21 times and average it so I need to change this
        /// perhaps the leaves eval should be placed only here instead
        /// </summary>
        /// <param name="board"></param>
        /// <param name="currentPlayer"></param>
        /// <param name="plyDepth"></param>
        /// <param name="neuralNetwork"></param>
        /// <returns>The equity and scoreVector for this position</returns>
        internal (float averageEquity, float[] averageScores) EvaluatePositionAverage(int[] board, int currentPlayer, int plyDepth)
        {
            //Console.WriteLine("Minmax av board" + String.Join(",", board));
            var gameEnded = GameEndedStatic(board);
            var isBearOffLeave = BearOffUtility.IsBearOffPosition(board);
            if (isBearOffLeave && _isCreatingBearOffDatabase)
            {
                if (!BearOffUtility.PosExistsInDatabase(board, currentPlayer, _bearOffDatabase))
                {
                    isBearOffLeave = false;
                }
            }

            if (plyDepth <= 0 || gameEnded || isBearOffLeave)
            {
                _leafCounter++;
                var scoreVector = ScoreUtility.EvaluatePosition(board, currentPlayer, _positionEvaluators, gameEnded);
                var equity = ScoreUtility.CalculateEquity(scoreVector);
                //Console.WriteLine("Minmax av leafboard" + String.Join(",", board));
                //Console.WriteLine("Minmax av score" + String.Join(",", scoreVector));
                return (equity, scoreVector);
            }
            var scores = new float[6];
            var totalWeight = 0.0f;
            //Console.WriteLine("Board AV:"+ string.Join(", ", board));
            for (int die1 = 1; die1 <= 6; die1++)
            {
                for (int die2 = die1; die2 <= 6; die2++)
                {
                    var weight = die1 == die2 ? 1 : 2;
                    var (_, bestScoreVector) = MinMax(board, currentPlayer, die1, die2, plyDepth);
                    //Console.WriteLine("die1: " + die1 + "die2: " + die2 + "ply" + plyDepth);
                    for (int i = 0; i < scores.Length; i++)
                    {
                        scores[i] += bestScoreVector[i] * weight;
                    }
                    totalWeight += weight;
                }
            }

            var averageScores = scores.Select(s => s / totalWeight).ToArray();
            var averageEquity = ScoreUtility.CalculateEquity(averageScores);
            //Console.WriteLine("averageEQ" + averageEquity + ": " + string.Join("," , averageScores));
            return (averageEquity, averageScores);
        }
    }
}
