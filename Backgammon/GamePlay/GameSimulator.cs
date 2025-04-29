using Backgammon.Models;
using Backgammon.Training;
using Backgammon.Util;
using Backgammon.Util.AI;
using Backgammon.Utils;
using Serilog;
using System.Diagnostics;
using static Backgammon.Util.Constants;
using static Backgammon.Models.BackgammonBoard;
using Backgammon.Util.NeuralEncoding;

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

        /// <summary>
        /// Simulates backgammon games and provides functionality for training neural networks.
        /// </summary>
        public GameSimulator()
        {
            string dataPath = Environment.GetEnvironmentVariable("BG_DATA_PATH");
            // Set up directories
            var modelsDir = Path.Combine(dataPath, "neuralnets");
            _logdir = Path.Combine(dataPath, "logs");

            // Load position evaluators
            _positionEvaluators = NeuralNetworkManager.GetBackgammonPosEvaluators(modelsDir, _logdir);

            // Load or create the bear-off database
            var bearOffDatabase = BearOffDatabaseUtil.LoadOrCreateBearOffDatabase(modelsDir);
            var bearOffEvaluator = new BearoffDatabaseEvaluator(bearOffDatabase);
            _positionEvaluators.Add(PositionType.BearOffDatabase, bearOffEvaluator);

            // Initialize other components
            _gameLogger = CreateLogger(Path.Combine(_logdir, "games.log"));
            _moneyGameLogger = CreateLogger(Path.Combine(_logdir, "moneygames.log"));
            _extraGamesLogger = CreateLogger(Path.Combine(_logdir, "ExtraGames.log"));
            _board = new BackgammonBoard();
            _minMaxUtility = new MinMaxUtility(_positionEvaluators);
        }

        private ILogger CreateLogger(string logFilePath)
        {
            return new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        /// <summary>
        /// Simulates a specified number of backgammon games.
        /// </summary>
        /// <param name="numberOfGames">The number of games to simulate.</param>
        /// <returns>A list of <see cref="GameData"/> objects representing the simulated games.</returns>
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

        /// <summary>
        /// Exports a game to a file in a readable format that can also be read by other backgammon software.
        /// </summary>
        /// <param name="gameData">The game data to export.</param>
        public void exportGameToFile(GameData gameData)
        {              
            // Initialize game notations
            List<string> gameNotations = new List<string>
            {
                "1 Point Match",
                " Game 1",
                "Player1 : 0                            Player2 : 0"
            };

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
                Console.WriteLine(i + " : " + move.Move.MovesAsStandardNotation());
                if (move.Player == Player2)
                    rowNotation = rowNotation.PadRight(39);
                rowNotation += move.Move.MovesAsStandardNotation();

                if (move.Player == Player2 || i == gameData.MoveData.Count - 1)
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
                    var score = Math.Abs(Score(winningPosition, 1));
                    rowNotation = rowCount < 10 ? $"  {rowCount})" : $" {rowCount})";
                    Console.WriteLine(rowNotation + ": score" + score);

                    if (move.Player == Player2)
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
                foreach (var row in gameNotations)
                {
                    writer.WriteLine(row);
                }
            }
        }

        /// <summary>
        /// Plays a money game with optional starting position and player.
        /// When you play for money you also consider winning and loosing gammons and backgammons when choosing the best move 
        /// (which affects the equity of a position). 
        /// here we have not introduced the cube and jacoby rule yet though.
        /// </summary>
        /// <param name="startingPosition">The starting position of the game (optional).</param>
        /// <param name="player">The starting player (optional).</param>
        /// <returns>A <see cref="GameData"/> object representing the money game.</returns>
        public GameData PlayMoneyGame(int[]? startingPosition = null, int? player = null)
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

            var currentPlayer = player ?? (random.Next(2) == 0 ? Player1 : Player2);

            //bool showTheGame = true;
            bool isStartingPosition = true;

            while (!GameEndedStatic(currentPosition))
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
                Console.WriteLine(backgammonBoard);
                Console.WriteLine("dies: " + die1 + " , " + die2 + " moveCount: " + gameData.MoveData.Count);
                Console.WriteLine("positionType: " + MapBoardToPositionType(currentPosition, currentPlayer));
                var legalMoves = GenerateLegalMovesStatic(currentPosition, die1, die2, currentPlayer);
                var opponent = BackgammonGameHelper.Opponent(currentPlayer);
                if (legalMoves.Count > 0)
                {
                    Stopwatch stopwatchMinMax = Stopwatch.StartNew();
                    var minMaxPly = 2;
                    var evaluationsMinMax = _minMaxUtility.EvaluateMoveCandidates(currentPosition, currentPlayer, die1, die2, minMaxPly);
                    var ply1Millis = stopwatchMinMax.ElapsedMilliseconds;
                    //var leafsPly1 = _minMaxUtility.LeafCounter;
                    Console.WriteLine($"leafs at ply ({minMaxPly}): " + _minMaxUtility.LeafCounter);
                    //skip ply 3 for fast game
                    //var firstNElements = evaluationsMinMax.Take(3).ToList();
                    //evaluationsMinMax = _minMaxUtility.EvaluateMoveCandidates(currentPosition, currentPlayer, die1, die2, minMaxPly + 1, firstNElements);
                    Console.WriteLine($"leafs at ply ({minMaxPly + 1}): " + _minMaxUtility.LeafCounter);
                    Console.WriteLine($"Best Move " + evaluationsMinMax.First().Move.MovesAsStandardNotation());
                    //var prevLeafCount = 0;

                    var ply2Millis = stopwatchMinMax.ElapsedMilliseconds - ply1Millis;
                    stopwatchMinMax.Stop();

                    if (logTheGame)
                    {
                        _gameLogger.Information("Board");
                        _gameLogger.Information("\n" + backgammonBoard);
                        _gameLogger.Information("Pos:\n" + string.Join(", ", backgammonBoard.Position));                   
                        //double seconds = (double)stopwatchMinMax.ElapsedTicks / Stopwatch.Frequency;
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
        
        public MoveData? GenerateComputerMove(int[] currentPosition, int die1, int die2, int playerToMove)
        {
            const int minMaxPly = 2;
            var legalMoves = GenerateLegalMovesStatic(currentPosition, die1, die2, playerToMove);
            if (legalMoves.Count > 0)
            {
                var evaluationsMinMax = _minMaxUtility.EvaluateMoveCandidates(currentPosition, playerToMove, die1, die2, minMaxPly);
                return evaluationsMinMax[0];
            }
            return null;
        }

        /// <summary>
        /// Trains the neural networks by playing and learning from games.
        /// </summary>
        /// <param name="games">The number of games to play and train on.</param>
        /// <param name="trainFrequency">The frequency of training during the games.</param>
        /// <param name="epochs">The number of epochs for training.</param>
        /// <param name="saveFrequency">The frequency of saving the neural networks.</param>
        public void playAndTrain(int games, int trainFrequency, int epochs, int saveFrequency)
        {
            var gameDataTrainLogs = Path.Combine(_logdir, "trainData.log");
            var gameDataTrainer = new GameDataTrainer(_positionEvaluators, gameDataTrainLogs);

            var totalPlayingTime = 0L;
            var totalTrainingTime = 0L;

            var nrOfTrainingGames = 3000;

            var maxExtraGames = 1000; // When extraTrainPositions exceeds this we dont add any more
            var inspectLearningFrequency = 10;
            var batchLearningRate = 0.01f;
            var minGamesBeforeBatchLearning = 800;
            var trainingGamesData = new List<TrainingData>[nrOfTrainingGames];
            List<(int[], int)> extraTrainPositions = [];
            int saveCounter = 0;
            int maxExtraSubPositions = 300; // How many extra max sub positions can be generated from one game (, can be recursive)
            int maxExtraMoveCandidates = 3; // How many extra max sub positions can be generated from one position (, can be recursive)
            int maxMovesToPlay = 80;        // How many moves to play in a subposition
            int subPositions = 0;
            // Sometimes we play a moneygame and save it for evaluation in XG
            int moneyGameFrequency = 200;
            var rand = new Random();
            for (int i = 0; i < games; i++)
            {
                Stopwatch stopwatchPlay = Stopwatch.StartNew();
                GameData? gameData = null;
                bool gameDataIsExtraGame = false;
                if (extraTrainPositions.Any())
                {
                    var lastItem = extraTrainPositions[extraTrainPositions.Count - 1]; // Pick the last item    
                    extraTrainPositions.RemoveAt(extraTrainPositions.Count - 1); // Remove the last item
                    Console.WriteLine("Simulate an extra game" + i);
                    gameData = SimulateSingleGame(lastItem.Item1, lastItem.Item2, maxMovesToPlay);
                    gameDataIsExtraGame = true;
                }
                if (gameData is null || gameData.IsEmpty())
                {
                    Console.WriteLine("Simulate a game" + i);
                    gameData = SimulateSingleGame();
                    if (gameData.IsEmpty())
                        Console.WriteLine("Empty GameData" + i);
                    subPositions = 0;
                }
                stopwatchPlay.Stop();
                totalPlayingTime += stopwatchPlay.ElapsedMilliseconds;

                Stopwatch stopwatchTrain = Stopwatch.StartNew();
                //For batchlearning we want to create a dataset before starting training
                if (i % trainFrequency == 0 && i > minGamesBeforeBatchLearning)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        var trainData = gameDataTrainer.GameToTrainingDataMinMax(gameData, batchLearningRate);

                        trainingGamesData[i % nrOfTrainingGames] = trainData;
                        var flatList = trainingGamesData
                            .Where(list => list != null)  // Exclude null lists
                            .SelectMany(list => list)     // Flatten the lists
                            .ToList();
                        Console.WriteLine("Train on patterns:" + flatList.Count);

                        Dictionary<PositionType, List<TrainingData>> trainingPatternsDict = [];
                        // We should maybe update the this list each epoch so we get new target values
                        foreach (var elem in flatList)
                        {
                            //var positionType = MapBoardToPositionType(elem.board);
                            var positionType = elem.PositionType;
                            if (positionType != PositionType.BearOffDatabase)
                            {
                                if (!trainingPatternsDict.ContainsKey(positionType))
                                {
                                    trainingPatternsDict.Add(positionType, new List<TrainingData>());
                                }
                                trainingPatternsDict[positionType].Add(elem);
                            }
                        }

                        foreach (var positionType in trainingPatternsDict.Keys)
                        {
                            var trainingPatterns = trainingPatternsDict[positionType];
                            var neuralNetwork = ((NeuralNetworkPositionEvaluator)_positionEvaluators[positionType]).NeuralNetwork;
                            Console.WriteLine("Train" + positionType + " patterns" + trainingPatterns.Count + ": " + neuralNetwork.DetfaultfilePath);
                            var sample = trainingPatterns[0];
                            Console.WriteLine("sample" + string.Join(", ", sample.board));
                            Console.WriteLine("sample input" + sample.InputData.Length);
                            neuralNetwork.BatchUpdate(trainingPatterns, epochs);
                        }
                    }
                }
                stopwatchTrain.Stop();
                totalTrainingTime += stopwatchTrain.ElapsedMilliseconds;
                if (i % inspectLearningFrequency == 0)
                {
                    Console.WriteLine($"Game nr: {i} positions {gameData.MoveData.Count}");
                    Console.WriteLine($"Playing time: {totalPlayingTime / 1000L} s");
                    Console.WriteLine($"Training time: {totalTrainingTime / 1000L} s");
                    foreach (var (key, value) in _positionEvaluators)
                    {
                        if (value is NeuralNetworkPositionEvaluator neuralNetworkEvaluator)
                        {
                            try
                            {
                                Console.WriteLine(key + "NN" + neuralNetworkEvaluator.NeuralNetwork.Description);
                                var (_, labels) = BoardToNeuralInputsEncoder.EncodeBoardToNeuralInputs(BackgammonPositions.BarPointMutualHoldingGame, key);
                                neuralNetworkEvaluator.NeuralNetwork.SetInputLabels(labels);
                                neuralNetworkEvaluator.NeuralNetwork.checkMaxFeatureRelevance();
                            }
                            catch (Exception ex) { Console.WriteLine(ex.Message); }
                        }
                    }
                }

                // Most extra games we add from the 'main' game rather an extra game from anothe extra game
                var addExtraGameProbability = gameDataIsExtraGame ? 0.01 : 0.2;
                if (subPositions < maxExtraGames && rand.NextDouble() < addExtraGameProbability)
                {
                    var generatedSubPositions = generateExtraData(gameData, maxExtraSubPositions, maxExtraMoveCandidates);
                    if (generatedSubPositions.Count > 0)
                    {
                        subPositions += generatedSubPositions.Count();
                        extraTrainPositions.AddRange(generatedSubPositions);
                        Console.WriteLine("\nCreated extra train positions" + extraTrainPositions.Count);
                    }
                }

                if ((i + 1) % saveFrequency == 0)
                {
                    saveCounter++;
                    foreach (var (key, value) in _positionEvaluators)
                    {
                        if (value is NeuralNetworkPositionEvaluator neuralNetworkEvaluator)
                        {
                            neuralNetworkEvaluator.NeuralNetwork.Save();
                            neuralNetworkEvaluator.NeuralNetwork.Save(saveCounter);
                        }
                    }
                }

                if ((i + 1) % moneyGameFrequency == 0)
                {
                    var moneyGame = PlayMoneyGame();
                    exportGameToFile(moneyGame);
                }
            }
        }

        /// <summary>
        /// Simulates a single backgammon game.
        /// For training purpose it can be useful to start from different positions , 
        /// that's why it's possible to play from one of the predefined positions picked ny random
        /// </summary>
        /// <param name="startingPosition">The starting position of the game (optional).</param>
        /// <param name="player">The starting player (optional).</param>
        /// <returns>A <see cref="GameData"/> object representing the simulated game.</returns>
        public GameData SimulateSingleGame(int[]? startingPosition = null, int? player = null, int maxMoves = 200)
        {
            var startingPositions = BackgammonPositions.BearOffGames;
            startingPositions = BackgammonPositions.MergeArrays(startingPositions, BackgammonPositions.RollThePrime);
            startingPositions = BackgammonPositions.MergeArrays(startingPositions, BackgammonPositions.OpeningGames);
            startingPositions = BackgammonPositions.MergeArrays(startingPositions, BackgammonPositions.HoldingGames);
            startingPositions = BackgammonPositions.MergeArrays(startingPositions, BackgammonPositions.BackGames);
            startingPositions = BackgammonPositions.MergeArrays(startingPositions, BackgammonPositions.FishingGames);
            startingPositions = BackgammonPositions.MergeArrays(startingPositions, BackgammonPositions.BearOffGamesNoContact);
            startingPositions = BackgammonPositions.MergeArrays(startingPositions, BackgammonPositions.PrimeVsPrimeGames);
            startingPositions = BackgammonPositions.MergeArrays(startingPositions, BackgammonPositions.SplitGames);
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
                currentPosition = MirrorBoard(currentPosition);
            }
            var backgammonBoard = new BackgammonBoard();
            backgammonBoard.Position = currentPosition;

            // Deciding the current player. If 'player' is null, choose randomly.
            int currentPlayer = player ?? (random.NextDouble() > 0.5 ? Player1 : Player2);
            Console.WriteLine("Staring Position:\n" + backgammonBoard + "bear off" + BearOffUtility.IsBearOffPosition(currentPosition));
            bool showTheGame = true;
            while (!GameEndedStatic(currentPosition) && !BearOffUtility.IsBearOffPosition(currentPosition) && gameData.MoveData.Count < maxMoves)
            {
                var die1 = random.Next(6) + 1;
                var die2 = random.Next(6) + 1;
                backgammonBoard.Position = currentPosition;
                backgammonBoard.Die1 = die1;
                backgammonBoard.Die2 = die2;
                backgammonBoard.CurrentPlayer = currentPlayer;
                if (gameData.MoveData.Count > 3)
                    showTheGame = false;

                var positionType = MapBoardToPositionType(backgammonBoard.Position, currentPlayer);
                if (showTheGame)
                {
                    Console.WriteLine("Pos:\n" + backgammonBoard);
                    Console.WriteLine("PositionType" + positionType);
                    Console.WriteLine("Pos:\n" + string.Join(", ", backgammonBoard.Position));
                    // var (quity, scorev) = _minMaxUtility.MinMax(backgammonBoard.Position, currentPlayer, die1, die2, 1);
                    // Console.WriteLine("Scorev MinMax:\n" + string.Join(", ", scorev));
                    var score1 = ScoreUtility.EvaluatePosition(backgammonBoard.Position, currentPlayer, _positionEvaluators);
                    var scoresRounded = score1.Select(t => Math.Round(t, 4).ToString());
                    Console.WriteLine("ScoreV1 EvalPos: \n" + string.Join(", ", scoresRounded));
                    var mirroredPos = MirrorBoard(backgammonBoard.Position);
                    // var score2 = ScoreUtility.EvaluatePosition(mirroredPos, BackgammonGameHelper.Opponent(currentPlayer), _positionEvaluators);
                    // Console.WriteLine("ScoreV2 Eval Mirrored: \n" + string.Join(", ", score2));
                }

                var legalMoves = GenerateLegalMovesStatic(currentPosition, die1, die2, currentPlayer);
                //List<MoveData> evaluatedMoves = [];
                var opponent = BackgammonGameHelper.Opponent(currentPlayer);
                if (legalMoves.Count > 0)
                {
                    Stopwatch stopwatchMinMax = Stopwatch.StartNew();
                    var minMaxPly = 1;
                    var evaluationsMinMax = _minMaxUtility.EvaluateMoveCandidates(currentPosition, currentPlayer, die1, die2, minMaxPly);
                    var ply1Millis = stopwatchMinMax.ElapsedMilliseconds;
                    var leafsPly1 = _minMaxUtility.LeafCounter;
                    var firstNElements = evaluationsMinMax.Take(3).ToList();

                    if (_minMaxUtility.LeafCounter < 2000)//
                    {
                        evaluationsMinMax = _minMaxUtility.EvaluateMoveCandidates(currentPosition, currentPlayer, die1, die2, minMaxPly + 1, firstNElements);
                    }

                    var ply2Millis = stopwatchMinMax.ElapsedMilliseconds - ply1Millis;
                    _gameLogger.Information("ply1 milliseconds:" + ply1Millis + ", LEAFS:" + leafsPly1);
                    _gameLogger.Information("ply2 milliseconds:" + ply2Millis + ", LEAFS:" + _minMaxUtility.LeafCounter);
                    stopwatchMinMax.Stop();

                    if (logTheGame)
                    {
                        _gameLogger.Information("Board: " + positionType);
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
        /// Generate extra data from a game. This is used to generate extra positions from a game that we can use for training
        /// instead of always starting from the first position.
        /// </summary>
        /// <param name="gameData"></param>
        /// <param name="maxExtraPos"></param>     How many maximum candidate positions from this game that we will play as a new game
        /// <param name="maxMovesToPlay"></param>  How many maximum candidate positions that we will generate from one position then game 
        /// (taken from the legal candidate moves) 
        /// <returns></returns>
        private List<(int[], int)> generateExtraData(GameData gameData, int maxExtraPos, int maxMovesToPlay)
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
                var (primeLengthForPlayedMove, _) = CountPrimes(boardAfter, player);
                var nrOfSafePoints = CountSafePoints(boardAfter, player);
                var rand = new Random();
                var remainingPositions = nrOfPositions - moveCount;
                var backgammonBoard = new BackgammonBoard();
                var (stillContact, _) = StillContact(boardAfter);
                var hitOpponent = moveData.Move.IsOpponentHit();
                var directHits = BoardToNeuralInputsEncoder.CountDirectHits(boardAfter, player);

                if (moveData.MoveCandidates is not null && stillContact)
                {
                    // With some probability we add move from 30 best candidates
                    var nBest = 60;
                    if (rand.NextDouble() > 0.7f)
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
                        break;// Only add 'random candidates' for this moveData
                    }
                    // When not adding positions randomly try to add 'interesting' candidates                    
                    var extraPositionsForMoveAdded = 0;
                    for (int candCount = 1; candCount < moveData.MoveCandidates.Count; candCount++)
                    {
                        var moveCand = moveData.MoveCandidates[candCount];
                        if (candCount > 0 && !GameEndedStatic(moveCand.BoardAfter))
                        {
                            var (primeLengthCand, _) = CountPrimes(moveCand.BoardAfter, player);
                            var directHitsCand = BoardToNeuralInputsEncoder.CountDirectHits(moveCand.BoardAfter, player);
                            var nrOfSafePointsCand = CountSafePoints(moveCand.BoardAfter, player);
                            _extraGamesLogger.Information(nrOfSafePoints + "Safe points comp:" + nrOfSafePointsCand);
                            _extraGamesLogger.Information("scorev" + string.Join(", ", moveCand.ScoreVector));

                            if (primeLengthCand >= 3 && primeLengthCand > primeLengthForPlayedMove)
                            {
                                extraData.Add((moveCand.BoardAfter, opponent));
                                LogBoardPosition(boardBefore, "Move" + moveData.Move.MovesAsStandardNotation());
                                LogBoardPosition(boardAfter, "Primepos Length Played:" + primeLengthForPlayedMove);
                                backgammonBoard.Position = moveData.BoardAfter;
                                LogBoardPosition(moveCand.BoardAfter, "EXTRA POS Primeposition Cand Length:" + primeLengthCand);
                                extraPositionsForMoveAdded++;
                            }
                            else if (IsSplittingOrGoingForBetterAnchor(moveCand.BoardBefore, moveCand.Move))
                            {
                                extraData.Add((moveCand.BoardAfter, opponent));
                                LogBoardPosition(boardBefore, "Move" + moveData.Move.MovesAsStandardNotation());
                                LogBoardPosition(boardAfter, "Split Cand added:");
                                extraPositionsForMoveAdded++;
                            }
                            else if (nrOfSafePointsCand > nrOfSafePoints) //This can be good sometimes but not when rolling the prime
                            {
                                //Any move that 
                                extraData.Add((moveCand.BoardAfter, opponent));
                                LogBoardPosition(boardBefore, "Move" + moveData.Move.MovesAsStandardNotation());
                                LogBoardPosition(boardAfter, $"Safe points played {nrOfSafePoints}\n ");
                                backgammonBoard.Position = moveData.BoardBefore;
                                LogBoardPosition(moveCand.BoardAfter, $"EXTRA POS Safe points Cand {nrOfSafePoints}\n ");
                                extraPositionsForMoveAdded++;
                                //break;
                            }
                            else if (moveCand.Move.IsDoubleTiger())
                            {
                                LogBoardPosition(boardBefore, "Move" + moveData.Move.MovesAsStandardNotation());
                                LogBoardPosition(boardAfter, $"EXTRA POS Double tiger!\n ");
                                extraData.Add((moveCand.BoardAfter, opponent));
                                extraPositionsForMoveAdded++;
                                break;
                            }
                            else if (directHits > directHitsCand)
                            {
                                extraData.Add((moveCand.BoardAfter, opponent));
                                LogBoardPosition(boardBefore, "Move" + moveData.Move.MovesAsStandardNotation());
                                LogBoardPosition(boardAfter, $"Playd move {nrOfSafePoints}\n ");
                                backgammonBoard.Position = moveData.BoardBefore;
                                LogBoardPosition(moveCand.BoardAfter, $"EXTRA POS Fewer direct hits {nrOfSafePoints}\n ");
                                extraPositionsForMoveAdded++;
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
                moveCount++;
            }
            return extraData;
        }

        //It's not good to be stuck on the deuce or the one point so moves that fights for a better anchor is valuable
        private bool IsSplittingOrGoingForBetterAnchor(int[] position, Move move)
        {
            if (move.Player == Player1)
            {
                foreach (var checkerMove in move.CheckerMoves)
                {
                    if (checkerMove.From == AcePointP2 && position[AcePointP2] >= 2)
                    {
                        return true;
                    }
                    if (checkerMove.From == DeucePointP2 && position[DeucePointP2] >= 2)
                    {
                        return true;
                    }
                }
                return false;
            }

            foreach (var checkerMove in move.CheckerMoves)
            {
                if (checkerMove.From == AcePointP1 && position[AcePointP1] <= -2)
                {
                    return true;
                }
                if (checkerMove.From == DeucePointP1 && position[DeucePointP1] <= -2)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
