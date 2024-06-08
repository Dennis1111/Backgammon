using Backgammon.Models.NeuralNetwork;
using Backgammon.Models;
using Backgammon.Utils;

namespace Backgammon.Util.AI
{
    internal class MinMaxUtility
    {
        private int _leafCounter = 0;
        private int _maxLeaves = 1000;
        private float _clampMin = 0f;
        private float _clampMax = 1f;
        private readonly Dictionary<string, float[]> _bearOffDatabase;
        private bool _isCreatingBearoffDatabase=false;
        
        // private bool _useClampTargets=true;

        public MinMaxUtility()
        {
            _bearOffDatabase = [];
        }

        public MinMaxUtility(Dictionary<string, float[]> bearOffDatabase)
        {
            _bearOffDatabase = bearOffDatabase;
        }

        public void useClampValues(float min, float max) {
            _clampMin = min;
            _clampMax = max;
            //_useClampTargets = true;
        }

        public void createBearOffDatabase() { 
            _isCreatingBearoffDatabase = true;
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
        internal List<MoveData> EvaluateMoveCandidates(int[] board, int currentPlayer, int die1, int die2, int plyDepth, NeuralNetwork[] neuralNetworks)
        {
            ResetLeafCounter();
            List<MoveData> moveDatas = [];
            var movesAndBoards = BackgammonBoard.GenerateLegalMovesStatic(board, die1, die2, currentPlayer);
            foreach (var moveAndBoard in movesAndBoards)
            {
                var newBoard = moveAndBoard.board;
                var move = moveAndBoard.move;
                // shall we decrease ply here ?
                var (equity, scoreVector) = EvaluatePositionAverage(newBoard, BackgammonGameHelper.Opponent(currentPlayer), plyDepth, neuralNetworks);
                var moveData = new MoveData(currentPlayer, board, newBoard, move, equity, scoreVector);
                moveDatas.Add(moveData);
            }
            if (currentPlayer == BackgammonBoard.Player1)
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
        internal (float, float[]) MinMax(int[] board, int currentPlayer, int die1, int die2, int plyDepth, NeuralNetwork[] neuralNetworks)
        {
            //Console.WriteLine("Minmax board" + String.Join(",", board));
            var gameEnded = BackgammonBoard.GameEndedStatic(board);
            var isBearOffLeave = BeafOffUtility.IsBearOffPosition(board);
            if (isBearOffLeave && _isCreatingBearoffDatabase) {
                if (!BeafOffUtility.PosExistsInDatabase(board, currentPlayer, _bearOffDatabase)) {
                    isBearOffLeave = false;
                }
            }

            if (plyDepth <= 0 || gameEnded || isBearOffLeave)
            {
                _leafCounter++;
                var scoreVector = ScoreUtility.EvaluatePosition(board, currentPlayer, neuralNetworks, _bearOffDatabase, _clampMin, _clampMax, gameEnded);
                var equity = ScoreUtility.CalculateEquity(scoreVector);
                //Console.WriteLine("Minmax leafboard" + String.Join(",", board));
                //Console.WriteLine("Minmax score" + String.Join(",", scoreVector));
                return (equity, scoreVector);
            }
            var bestEquity = currentPlayer == BackgammonBoard.Player1 ? float.NegativeInfinity : float.PositiveInfinity;
            var bestScoreVector = new float[6];

            var movesAndBoards = BackgammonBoard.GenerateLegalMovesStatic(board, die1, die2, currentPlayer);
            if (movesAndBoards.Count == 0)
            {
                // Since the player can't move the evaluation of this position is the average for this position with the opponent as currentPlayer
                return EvaluatePositionAverage(board, BackgammonGameHelper.Opponent(currentPlayer), plyDepth -1, neuralNetworks);
            }
            else
            {
                foreach (var moveAndBoard in movesAndBoards)
                {
                    var newBoard = moveAndBoard.board;
                    var move = moveAndBoard.move;
                    // shall we decrease ply here ?
                    var (equity, scoreVector) = EvaluatePositionAverage(newBoard, BackgammonGameHelper.Opponent(currentPlayer), plyDepth -1 , neuralNetworks);
                    if ((currentPlayer == BackgammonBoard.Player1 && equity > bestEquity) ||
                        (currentPlayer == BackgammonBoard.Player2 && equity < bestEquity))
                    {
                        bestEquity = equity;
                        bestScoreVector = scoreVector;
                        //bestMoveAndBoard = moveAndBoard;
                    }
                }
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
        internal (float averageEquity, float[] averageScores) EvaluatePositionAverage(int[] board, int currentPlayer, int plyDepth, NeuralNetwork[] neuralNetworks)
        {
            //Console.WriteLine("Minmax av board" + String.Join(",", board));
            var gameEnded = BackgammonBoard.GameEndedStatic(board);
            var isBearOffLeave = BeafOffUtility.IsBearOffPosition(board);
            if (isBearOffLeave && _isCreatingBearoffDatabase)
            {
                if (!BeafOffUtility.PosExistsInDatabase(board, currentPlayer, _bearOffDatabase))
                {
                    isBearOffLeave = false;
                }
            }

            if (plyDepth <= 0 || gameEnded || isBearOffLeave)
            {
                _leafCounter++;
                var scoreVector = ScoreUtility.EvaluatePosition(board, currentPlayer, neuralNetworks, _bearOffDatabase, _clampMin, _clampMax, gameEnded);
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
                    var (_, bestScoreVector) = MinMax(board, currentPlayer, die1, die2, plyDepth, neuralNetworks);
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
