using Backgammon.Models;
using Backgammon.Util;
using Backgammon.Util.AI;
using Backgammon.Utils;
using Serilog;
using System.Diagnostics;
using static Backgammon.Util.Constants;

namespace Backgammon.GamePlay
{
    public class GameSimulator
    {
        private readonly ILogger _gameLogger; // Custom logger for game simulation
        private readonly ILogger _moneyGameLogger; // Custom logger for game simulation
        private readonly ILogger _extraGamesLogger; // Custom logger for extra game positions
        private readonly string _logdir;
        private readonly BackgammonBoard _board;
        private readonly MinMaxUtility _minMaxUtility;
        private Dictionary<PositionType, IBackgammonPositionEvaluator> _positionEvaluators;

        public GameSimulator(Dictionary<PositionType, IBackgammonPositionEvaluator> positionEvaluators, string logFileDirectory)
        {
            _positionEvaluators = positionEvaluators;
            _gameLogger = CreateLogger(logFileDirectory + "//games.log"); // Initialize the custom logger
            _moneyGameLogger = CreateLogger(logFileDirectory + "//games.log"); // Initialize the custom logger
            _extraGamesLogger = CreateLogger(logFileDirectory + "//ExtraGames.log"); // Initialize the custom logger
            _board = new BackgammonBoard();
            _minMaxUtility = new MinMaxUtility(positionEvaluators);
            _logdir = logFileDirectory;
            // Make sure to flush and close the logger
            // Log.CloseAndFlush();
        }

        private ILogger CreateLogger(string logFilePath)
        {
            return new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        static void ClearOldLogFiles(string logDirectory, string logFilePrefix, int retentionDays)
        {
            var now = DateTime.UtcNow;
            //foreach (var filePath in Directory.GetFiles(logDirectory, $"{logFilePrefix}-*.txt"))
            foreach (var filePath in Directory.GetFiles(logDirectory, $"*.log"))
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                File.Delete(filePath);
                /*var datePart = fileName.Substring(logFilePrefix.Length + 1); // Adjust based on your actual prefix
                if (DateTime.TryParseExact(datePart, "yyyyMMdd", null, DateTimeStyles.None, out var fileDate))
                {
                    if ((now - fileDate).TotalDays > retentionDays)
                    {
                        File.Delete(filePath);
                    }
                }*/
            }
        }

        static void ClearLogFile(string filePath)
        {
            // Clear the file if it exists
            if (File.Exists(filePath))
            {
                File.WriteAllText(filePath, string.Empty);
            }
        }


        public List<GameData> SimulateGames(int numberOfGames)
        {
            List<GameData> playedGames = new List<GameData>();
            // Additional data structures as needed...

            for (int i = 0; i < numberOfGames; i++)
            {
                var gameData = SimulateSingleGame();
                playedGames.Add(gameData);
                // Handle extra positions...
            }

            return playedGames;
        }

        public void exportGameToFile(GameData gameData)
        {
            List<String> gameNotations = [];
            gameNotations.Add("1 Point Match");
            gameNotations.Add(" Game 1");
            gameNotations.Add("Player1 : 0                            Player2 : 0");

            int rowCount = 1;
            bool newRow = true;
            string rowNotation = "";

            for (int i = 0; i < gameData.MoveData.Count; i++)
            {                
                if (newRow)
                {
                    rowNotation = rowCount < 10 ? $"  {rowCount}) " : $" {rowCount}) ";
                }
                var move = gameData.MoveData[i];
                Console.WriteLine(i + " : "+ move.Move.MovesAsStandardNotation());
                if (move.Player == BackgammonBoard.Player2)
                    rowNotation = rowNotation.PadRight(39);
                rowNotation += move.Move.MovesAsStandardNotation();

                if (move.Player == BackgammonBoard.Player2 || i == gameData.MoveData.Count - 1)
                {
                    gameNotations.Add(rowNotation);
                    rowCount++;
                    newRow = true;
                }
                else
                {
                    
                    newRow = false;
                }


                if (i == gameData.MoveData.Count - 1)
                {
                    var winningPosition = move.BoardAfter;
                    var score = Math.Abs(BackgammonBoard.Score(winningPosition, 1));
                    rowNotation = rowCount < 10 ? $"  {rowCount})" : $" {rowCount})";
                    Console.WriteLine(rowNotation+ ": score"+ score);

                    if (move.Player == BackgammonBoard.Player2)
                    {
                        rowNotation = rowNotation.PadRight(39);
                    }
                    
                    Console.WriteLine(rowNotation + ": score" + score);
                    rowNotation += $"  Wins {score} point";
                    gameNotations.Add(rowNotation);
                }
            }
            
            using (StreamWriter writer = new StreamWriter(_logdir + "/moneygame.txt"))
            {
                foreach (var row in gameNotations) { 
                    writer.WriteLine(row);
                }
            }
        }

        public GameData PlayMoneygame(int[]? startingPosition = null, int? player = null)
        {
            var random = new Random();

            GameData gameData = new GameData();
            var logTheGame = random.Next(10) < 5;

            var backgammonBoard = new BackgammonBoard();
            var currentPosition = startingPosition ?? backgammonBoard.Position;

            var die1 = random.Next(6) + 1;
            var die2 = random.Next(6) + 1;

            while (die1 == die2)
            {
                die1 = random.Next(6) + 1;
                die2 = random.Next(6) + 1;
            }

            var currentPlayer = player ?? (random.Next(2) == 0 ? BackgammonBoard.Player1 : BackgammonBoard.Player2);

            //bool showTheGame = true;
            bool isStartingPosition = true;

            while (!BackgammonBoard.GameEndedStatic(currentPosition))
            {
                backgammonBoard.Position = currentPosition;
                if (!isStartingPosition)
                {
                    die1 = random.Next(6) + 1;
                    die2 = random.Next(6) + 1;
                }
                backgammonBoard.Die1 = die1;
                backgammonBoard.Die2 = die2;
                backgammonBoard.CurrentPlayer = currentPlayer;
                Console.WriteLine("die1:" + die1 + " die2:" + die2+" moves"+gameData.MoveData.Count);
                
                var legalMoves = BackgammonBoard.GenerateLegalMovesStatic(currentPosition, die1, die2, currentPlayer);
                var opponent = BackgammonGameHelper.Opponent(currentPlayer);
                if (legalMoves.Count > 0)
                {
                    Stopwatch stopwatchMinMax = Stopwatch.StartNew();
                    var minMaxPly = 2;
                    var evaluationsMinMax = _minMaxUtility.EvaluateMoveCandidates(currentPosition, currentPlayer, die1, die2, minMaxPly);
                    var ply1Millis = stopwatchMinMax.ElapsedMilliseconds;
                    var leafsPly1 = _minMaxUtility.LeafCounter;
                    var prevLeafCount = 0;
                    Console.WriteLine($"leafs at ply ({minMaxPly}): " + _minMaxUtility.LeafCounter);
                    var maxLeaves = 200;
                    while (_minMaxUtility.LeafCounter < maxLeaves && _minMaxUtility.LeafCounter > 1 && _minMaxUtility.LeafCounter > prevLeafCount)
                    {
                        prevLeafCount = _minMaxUtility.LeafCounter;
                        minMaxPly++;
                        evaluationsMinMax = _minMaxUtility.EvaluateMoveCandidates(currentPosition, currentPlayer, die1, die2, minMaxPly);
                        Console.WriteLine($"leafs at ply ({minMaxPly}): " + _minMaxUtility.LeafCounter);           
                    }

                    var ply2Millis = stopwatchMinMax.ElapsedMilliseconds - ply1Millis;
                    stopwatchMinMax.Stop();

                    if (logTheGame)
                    {
                        _gameLogger.Information("Board");
                        _gameLogger.Information("\n" + backgammonBoard);
                        _gameLogger.Information("Pos:\n" + string.Join(", ", backgammonBoard.Position));

                        // var stopWatch = new Stopwatch();
                        // stopWatch.Start();
                        // var (quity, scorev) = _minMaxUtility.MinMax(backgammonBoard.Points, currentPlayer, die1, die2, 1, _neuralNetworks);

                        //stopWatch.Stop();
                        double seconds = (double)stopwatchMinMax.ElapsedTicks / Stopwatch.Frequency;
                        //_gameLogger.Information("min max eval time: " + seconds);
                        //_gameLogger.Information("Leaves:" + _minMaxUtility.LeafCounter);
                        //_gameLogger.Information("average leaf time ms : " + (1000 * seconds / _minMaxUtility.LeafCounter));

                    }

                    var bestMove = evaluationsMinMax[0];
                    bestMove.MoveCandidates = evaluationsMinMax;
                    foreach (var evaluation in evaluationsMinMax)
                    {
                        var scoreVector = evaluation.ScoreVector;
                        var eq = evaluation.Equity;
                    }

                    currentPosition = bestMove.BoardAfter;
                    gameData.AddMoveData(bestMove);
                }
                else
                {
                    if (logTheGame)
                    {
                        _gameLogger.Information("Cant Move");
                    }
                    float[] scoreVector = ScoreUtility.EvaluatePosition(currentPosition, opponent, _positionEvaluators); // Evaluate the board resulting from the move
                    float equity = ScoreUtility.CalculateEquity(scoreVector);
                    var moveData = new MoveData(currentPlayer, currentPosition, currentPosition, new Move(die1, die2, currentPlayer), equity, scoreVector);
                    gameData.AddMoveData(moveData);
                }
                currentPlayer = opponent;
                isStartingPosition = false;
            }
            return gameData;
        }

        public GameData SimulateSingleGame(int[]? startingPosition = null, int? player = null, int maxMoves = 200)
        {
            var startingPositions = BackgammonPositions.BearOffGames;
            startingPositions = BackgammonPositions.MergeArrays(startingPositions, BackgammonPositions.RollThePrime);
            startingPositions = BackgammonPositions.MergeArrays(startingPositions, BackgammonPositions.OpeningGames);
            startingPositions = BackgammonPositions.MergeArrays(startingPositions, BackgammonPositions.HoldingGames);
            startingPositions = BackgammonPositions.MergeArrays(startingPositions, BackgammonPositions.BackGames);
            startingPositions = BackgammonPositions.MergeArrays(startingPositions, BackgammonPositions.FishingGames);
            startingPositions = BackgammonPositions.MergeArrays(startingPositions, BackgammonPositions.BearOffGamesNoContact);
            var random = new Random();
            var startingPositionIndex = random.Next(startingPositions.Length);
            //int[] currentPosition = BackgammonPositions.BearoffVs1Point; //startingPosition ?? startingPositions[random.Next(startingPositions.Length)];
            var defaultStartingPosition = startingPositions[startingPositionIndex];
            //var defaultStartingPosition = BackgammonPositions.BearoffVs13Off2OnTheBar; //startingPositions[random.Next(startingPositions.Length)];
            int[] currentPosition = startingPosition ?? defaultStartingPosition;

            GameData gameData = new GameData();
            var logTheGame = random.Next(10) < 5;

            if (random.Next(2) > 0)
            {
                currentPosition = BackgammonBoard.MirrorBoard(currentPosition);
            }
            var backgammonBoard = new BackgammonBoard();
            backgammonBoard.Position = currentPosition;

            // Deciding the current player. If 'player' is null, choose randomly.
            int currentPlayer = player ?? (random.NextDouble() > 0.8 ? BackgammonBoard.Player1 : BackgammonBoard.Player2);
            Console.WriteLine("Staring Position:\n" + backgammonBoard + "bearoff" + BearOffUtility.IsBearOffPosition(currentPosition));
            bool showTheGame = true;
            while (!BackgammonBoard.GameEndedStatic(currentPosition) && !BearOffUtility.IsBearOffPosition(currentPosition) && gameData.MoveData.Count < maxMoves)
            {
                var die1 = random.Next(6) + 1;
                var die2 = random.Next(6) + 1;
                backgammonBoard.Position = currentPosition;
                backgammonBoard.Die1 = die1;
                backgammonBoard.Die2 = die2;
                backgammonBoard.CurrentPlayer = currentPlayer;
                if (gameData.MoveData.Count > 50)
                    showTheGame = false;

                if (showTheGame)
                {
                    var positionType = BackgammonBoard.MapBoardToPositionType(backgammonBoard.Position);

                    Console.WriteLine("Pos:\n" + backgammonBoard);
                    Console.WriteLine("PositionType" + positionType);
                    Console.WriteLine("Pos:\n" + string.Join(", ", backgammonBoard.Position));
                    //var (quity, scorev) = _minMaxUtility.MinMax(backgammonBoard.Position, currentPlayer, die1, die2, 1);
                    //Console.WriteLine("Scorev MinMax:\n" + string.Join(", ", scorev));
                    var score1 = ScoreUtility.EvaluatePosition(backgammonBoard.Position, currentPlayer, _positionEvaluators);
                    var scoresRounded = score1.Select(t => Math.Round(t, 4).ToString());
                    Console.WriteLine("ScoreV1 EvalPos: \n" + string.Join(", ", scoresRounded));
                    var mirroredPos = BackgammonBoard.MirrorBoard(backgammonBoard.Position);
                    // var score2 = ScoreUtility.EvaluatePosition(mirroredPos, BackgammonGameHelper.Opponent(currentPlayer), _positionEvaluators);
                    // Console.WriteLine("ScoreV2 Eval Mirrored: \n" + string.Join(", ", score2));
                }

                var legalMoves = BackgammonBoard.GenerateLegalMovesStatic(currentPosition, die1, die2, currentPlayer);
                //List<MoveData> evaluatedMoves = [];
                var opponent = BackgammonGameHelper.Opponent(currentPlayer);
                if (legalMoves.Count > 0)
                {
                    Stopwatch stopwatchMinMax = Stopwatch.StartNew();
                    var minMaxPly = 1;
                    var evaluationsMinMax = _minMaxUtility.EvaluateMoveCandidates(currentPosition, currentPlayer, die1, die2, minMaxPly);
                    var ply1Millis = stopwatchMinMax.ElapsedMilliseconds;
                    var leafsPly1 = _minMaxUtility.LeafCounter;


                    if (_minMaxUtility.LeafCounter < 20000)
                    {
                        evaluationsMinMax = _minMaxUtility.EvaluateMoveCandidates(currentPosition, currentPlayer, die1, die2, minMaxPly + 1);
                    }

                    var ply2Millis = stopwatchMinMax.ElapsedMilliseconds - ply1Millis;
                    _gameLogger.Information("ply1 milliseconds:" + ply1Millis + ", LEAFS:" + leafsPly1);
                    _gameLogger.Information("ply2 milliseconds:" + ply2Millis + ", LEAFS:" + _minMaxUtility.LeafCounter);
                    stopwatchMinMax.Stop();

                    if (logTheGame)
                    {
                        _gameLogger.Information("Board");
                        _gameLogger.Information("\n" + backgammonBoard);
                        _gameLogger.Information("Pos:\n" + string.Join(", ", backgammonBoard.Position));

                        // var stopWatch = new Stopwatch();
                        // stopWatch.Start();
                        // var (quity, scorev) = _minMaxUtility.MinMax(backgammonBoard.Points, currentPlayer, die1, die2, 1, _neuralNetworks);

                        //stopWatch.Stop();
                        double seconds = (double)stopwatchMinMax.ElapsedTicks / Stopwatch.Frequency;
                        _gameLogger.Information("min max eval time: " + seconds);
                        _gameLogger.Information("Leaves:" + _minMaxUtility.LeafCounter);
                        _gameLogger.Information("average leaf time ms : " + (1000 * seconds / _minMaxUtility.LeafCounter));
                        //_gameLogger.Information("Scorev MinMax:\n" + string.Join(", ", scorev));
                        //var score1 = ScoreUtility.EvaluatePosition(backgammonBoard.Points, currentPlayer, _neuralNetworks, _bearOffDatabase, 0f, 1f);
                        //_gameLogger.Information("ScoreV1 EvalPos: \n" + string.Join(", ", score1));
                        //var mirroredPos = BackgammonBoard.MirrorBoard(backgammonBoard.Points);
                        //var score2 = ScoreUtility.EvaluatePosition(mirroredPos, BackgammonGameHelper.Opponent(currentPlayer), _neuralNetworks, _bearOffDatabase, 0f, 1f);
                        //_gameLogger.Information("ScoreV2 Eval Mirrored: \n" + string.Join(", ", score2));

                        //Console.WriteLine("Board");
                        //Console.WriteLine(backgammonBoard);
                    }


                    /*if (stopwatchMinMax.ElapsedMilliseconds > 0)
                    {
                        _gameLogger.Information($"MinMax time: {stopwatchMinMax.ElapsedMilliseconds}");
                        _gameLogger.Information($"Leaf Nodes: {minMaxUtil.LeafCounter} s");
                        _gameLogger.Information($"Av Node time: {minMaxUtil.LeafCounter / stopwatchMinMax.ElapsedMilliseconds} millis");                        
                    }*/
                    var bestMove = evaluationsMinMax[0];
                    bestMove.MoveCandidates = evaluationsMinMax;
                    foreach (var evaluation in evaluationsMinMax)
                    {
                        var scoreVector = evaluation.ScoreVector;
                        var eq = evaluation.Equity;
                    }

                    if (logTheGame || showTheGame)
                    {
                        int i = 0;
                        foreach (var evMove in evaluationsMinMax)
                        {
                            if (i == 4)
                                break;
                            //Console.WriteLine("old format" + evMove.Move);
                            //Console.WriteLine("new format" +  evMove.Move.MovesAsStandardNotation());
                            _gameLogger.Information("Move Cand: " + evMove.Move.MovesAsStandardNotation() + " Player" + evMove.Player);
                            var roundedScores = evMove.ScoreVector.Select(t => Math.Round(t, 4).ToString());
                            _gameLogger.Information("Equity: " + Math.Round(evMove.Equity, 3) + " : " + string.Join(", ", roundedScores));

                            //_gameLogger.Information("Vect Cand: " + string.Join(", ", evMove.ScoreVector));
                            /*if (die1 == die2) {
                                Console.WriteLine($"Cand old format: {evMove.Move}");
                                Console.WriteLine($"Cand move : {evMove.Move.MovesAsStandardNotation()}");
                            }*/
                            i++;
                        }
                        //Console.WriteLine("BestMove: " + bestMove.Move);
                        _gameLogger.Information("BestMove: " + bestMove.Move.MovesAsStandardNotation());
                        var roundedSV = bestMove.ScoreVector.Select(t => Math.Round(t, 4).ToString());
                        //Console.WriteLine("eval: " + string.Join(", ", roundedSV));
                        _gameLogger.Information("eval: " + string.Join(", ", roundedSV));
                    }
                    currentPosition = bestMove.BoardAfter;
                    gameData.AddMoveData(bestMove);
                }
                else
                {
                    if (logTheGame)
                    {
                        //Console.WriteLine(backgammonBoard);
                        //Console.WriteLine("Cant Move");
                        _gameLogger.Information("Cant Move");
                    }
                    //var boardEncoded = BoardToNeuralInputsEncoder.EncodeBoardToNeuralInputs(currentPosition, opponent);
                    //Should make clampmin max global vars
                    float[] scoreVector = ScoreUtility.EvaluatePosition(currentPosition, opponent, _positionEvaluators); // Evaluate the board resulting from the move
                    //Maybe need to check this
                    //float[] scoreVector = _neuralNetwork.FeedForward(boardEncoded); // Evaluate the board resulting from the move
                    float equity = ScoreUtility.CalculateEquity(scoreVector);
                    //We can use currentPosition as both boardBefore and boardAfter since the position hasnt changed
                    var moveData = new MoveData(currentPlayer, currentPosition, currentPosition, new Move(die1, die2, currentPlayer), equity, scoreVector);
                    gameData.AddMoveData(moveData);
                }
                if (showTheGame)
                {
                    Console.WriteLine("Move: " + gameData.MoveData.Last().Move.MovesAsStandardNotation());
                }
                currentPlayer = opponent;
            }
            return gameData;
        }

        private void LogBoardPosition(int[] board, string description)
        {
            _board.Position = board;
            _extraGamesLogger.Information($"{description} : \n {_board}");
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameData"></param>
        /// <param name="maxExtraPos"></param>     How many maximum candidate positions from this game that we will play as a new game
        /// <param name="maxMovesToPlay"></param>  How many maximum candidate positions that we will generate from one position then game 
        /// (taken from the legal candidate moves) 
        /// <returns></returns>
        public List<(int[], int)> generateExtraData(GameData gameData, int maxExtraPos, int maxMovesToPlay)
        {
            List<(int[], int)> extraData = [];
            int moveCount = 0;
            int nrOfPositions = gameData.MoveData.Count;
            //Each MoveData contains a boardBefore the player at turn and a move and the boardAfter the move
            //BoardData.MoveCandidates is a list of all possibles the player had

            // Loop from the end of the game backwards, as we learn from the end first it's more valuable to add games from the the end
            // Can also as the computer start playing endless games and we mignht need to explore more of my starting positions
            foreach (var moveData in gameData.MoveData)
            {
                if (extraData.Count >= maxExtraPos)
                    return extraData;
                var boardBefore = moveData.BoardBefore;
                var boardAfter = moveData.BoardAfter;
                var player = moveData.Player;
                var opponent = BackgammonGameHelper.Opponent(player);
                var primeLengthForPlayedMove = BackgammonBoard.CountPrimes(boardAfter, player);
                var nrOfSafePoints = BackgammonBoard.CountSafePoints(boardAfter, player);
                //var primeLengthP1 = BoardToNeuralInputsEncoder.CountPrimes(board, BackgammonBoard.Player1);
                //var primeLengthP2 = BoardToNeuralInputsEncoder.CountPrimes(board, BackgammonBoard.Player2);
                var rand = new Random();
                var remainingPositions = nrOfPositions - moveCount;
                var backgammonBoard = new BackgammonBoard();
                var stillContact = BackgammonBoard.StillContact(boardAfter);
                var hitOpponent = moveData.Move.IsOpponentHit();
                var directHits = BoardToNeuralInputsEncoder.CountDirectHits(boardAfter, player);
                // Early training add more no contact pos
                /*if (!stillContact && rand.Next(10) == 0)
                {
                    LogBoardPosition(boardBefore, "endgame pos:");

                    extraData.Add((boardBefore, player));
                }*/

                //LogBoardPosition(boardAfter, "played move");
                if (moveData.MoveCandidates is not null && stillContact)
                {
                    // With some probability we add move from 30 best candidates
                    var nBest = 60;
                    if (rand.NextDouble() > 0.5f)
                    {
                        var addCandidateProb = 0.8;
                        _extraGamesLogger.Information("Adding from 30 best candidates :");
                        for (int candCount = 1; candCount <= Math.Min(moveData.MoveCandidates.Count - 1, nBest); candCount++)
                        {
                            var cand = moveData.MoveCandidates[candCount];
                            if (rand.NextDouble() > addCandidateProb)//Add most candidates
                            {
                                extraData.Add((cand.BoardAfter, opponent));
                                addCandidateProb *= 0.8;
                            }
                        }
                        break;
                    }
                    //When not adding positions randomly try to add 'interesting' candidates                    
                    var extraPositionsForMoveAdded = 0;
                    for (int candCount = 1; candCount < moveData.MoveCandidates.Count; candCount++)
                    {
                        var moveCand = moveData.MoveCandidates[candCount];
                        if (candCount > 0 && !BackgammonBoard.GameEndedStatic(moveCand.BoardAfter))
                        {
                            //LogBoardPosition(moveCand.BoardAfter, "Cand move");
                            var primeLengthCand = BackgammonBoard.CountPrimes(moveCand.BoardAfter, player);
                            var directHitsCand = BoardToNeuralInputsEncoder.CountDirectHits(moveCand.BoardAfter, player);
                            var nrOfSafePointsCand = BackgammonBoard.CountSafePoints(moveCand.BoardAfter, player);
                            _extraGamesLogger.Information(nrOfSafePoints + "Safe points comp:" + nrOfSafePointsCand);
                            _extraGamesLogger.Information("scorev" + string.Join(", ", moveCand.ScoreVector));
                            if (directHits > directHitsCand)
                            {
                                extraData.Add((moveCand.BoardAfter, opponent));
                                LogBoardPosition(boardBefore, "Move" + moveData.Move);
                                LogBoardPosition(boardAfter, $"Playd move {nrOfSafePoints}\n ");
                                backgammonBoard.Position = moveData.BoardBefore;
                                LogBoardPosition(moveCand.BoardAfter, $"EXTRA POS Fewer direct hits {nrOfSafePoints}\n ");
                                extraPositionsForMoveAdded++;
                            }
                            else if (primeLengthCand >= 3 && primeLengthCand > primeLengthForPlayedMove)
                            {
                                extraData.Add((moveCand.BoardAfter, opponent));
                                LogBoardPosition(boardBefore, "Move" + moveData.Move);
                                LogBoardPosition(boardAfter, "Primepos Length Played:" + primeLengthForPlayedMove);
                                backgammonBoard.Position = moveData.BoardAfter;
                                LogBoardPosition(moveCand.BoardAfter, "EXTRA POS Primeposition Cand Length:" + primeLengthCand);
                                extraPositionsForMoveAdded++;
                            }
                            /*else if (nrOfSafePointsCand > nrOfSafePoints) //This can be good sometimes but not when rolling the prime
                            {
                                extraData.Add((moveCand.BoardAfter, opponent));
                                LogBoardPosition(boardBefore, "Move" + moveData.Move);
                                LogBoardPosition(boardAfter, $"Safe points played {nrOfSafePoints}\n ");
                                backgammonBoard.Points = moveData.BoardBefore;
                                LogBoardPosition(moveCand.BoardAfter, $"EXTRA POS Safe points Cand {nrOfSafePoints}\n ");
                                extraPositionsForMoveAdded++;
                                //break;
                            }*/
                            else if (moveCand.Move.IsDoubleTiger())
                            {
                                LogBoardPosition(boardBefore, "Move" + moveData.Move);
                                LogBoardPosition(boardAfter, $"EXTRA POS Double tiger!\n ");
                                extraData.Add((moveCand.BoardAfter, opponent));
                                extraPositionsForMoveAdded++;
                                break;
                            }
                            else if (!hitOpponent && moveCand.Move.IsOpponentHit())
                            {
                                LogBoardPosition(boardAfter, $"EXTRA POS Lets hit cand!\n ");
                                extraData.Add((moveCand.BoardAfter, opponent));
                                extraPositionsForMoveAdded++;
                                break;
                            }
                            else if (hitOpponent && !moveCand.Move.IsOpponentHit())
                            {
                                LogBoardPosition(boardAfter, $"EXTRA POS don't hit cand!\n ");
                                extraData.Add((moveCand.BoardAfter, opponent));
                                extraPositionsForMoveAdded++;
                                break;
                            }
                        }
                        if (extraPositionsForMoveAdded >= maxMovesToPlay)
                        {
                            break;
                        }
                    }
                }
                /*if (Math.Max(primeLengthP1, primeLengthP2) > 4)
                {
                    var die1 = rand.Next(6) + 1;
                    var die2 = rand.Next(6) + 1;
                    var legalMoves = BackgammonBoard.GenerateLegalMovesStatic(board, die1, die2, player);
                    foreach (var legalMove in legalMoves)
                    {
                        var primeLengthMoveCand = BoardToNeuralInputsEncoder.CountPrimes(board, BackgammonBoard.Player2);
                        if (rand.Next(6) == 0 && !BackgammonBoard.GameEndedStatic(legalMove.board)) //some arbitrarily percentage..
                        {
                            Console.WriteLine("Add prime board " + string.Join(", ", legalMove.board));
                            extraData.Add((legalMove.board, BackgammonGameHelper.Opponent(player)));
                            //break;
                        }
                    }
                }*/
                // Sometimes a game 1000 of moves with extreme superbackgames, doesnt make sense to train much there
                // Could make sense to train more on the first moves though because its probably bad choices that lead to long games
                //if ((remainingPositions < 5 || moveCount<10) && rand.Next(10) > 0)
                /*  else //if (moveCount < 4 && rand.Next(10) > 1)
                  {
                      // Add the three best moves
                      int candCount = 0;
                      if (moveData.MoveCandidates != null)
                      {
                          bool addNBestMoves = rand.Next(10) == 0;
                          int nBestMoves = 3;
                          foreach (var moveCandidate in moveData.MoveCandidates)
                          {
                              if (BackgammonBoard.GameEndedStatic(moveCandidate.BoardAfter))
                              {
                                  break;
                              }
                              backgammonBoard.Points = moveData.BoardAfter;//For printing only
                              // With some probability play out three first positions (perhaps should skip bearoff)
                              if (addNBestMoves)
                              {
                                  if (candCount < nBestMoves)
                                  {
                                      extraData.Add((moveData.BoardAfter, BackgammonGameHelper.Opponent(moveData.Player)));

                                      Console.WriteLine($"Add bestMoves cand \n{backgammonBoard}");
                                  }
                              }

                              // TO hit or not to hit is very complex so evaluate the opposite
                              else if (moveData.Move.IsOpponentHit())
                              {
                                  if (!moveCandidate.Move.IsOpponentHit())
                                  {
                                      extraData.Add((moveData.BoardAfter, BackgammonGameHelper.Opponent(moveData.Player)));
                                      Console.WriteLine($"Add Hit cand \n{backgammonBoard}");
                                      break;
                                  }
                              }
                              else
                              {
                                  if (moveCandidate.Move.IsOpponentHit())
                                  {
                                      extraData.Add((moveData.BoardAfter, BackgammonGameHelper.Opponent(moveData.Player)));
                                      Console.WriteLine($"Add not Hit cand \n{backgammonBoard}");
                                      break;
                                  }
                              }
                          }
                      }
                  }*/
                moveCount++;
            }
            return extraData;
        }
    }
}
