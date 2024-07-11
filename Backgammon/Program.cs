using Backgammon.GamePlay;
using Backgammon.Models;
using Backgammon.Models.NeuralNetwork;
using Backgammon.Training;
using Backgammon.Util;
using Backgammon.Util.AI;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Numerics;
using static Backgammon.Util.Constants;

/*
static void testEval(NeuralNetwork[] _neuralNetworks) {
    int[] sample = [0, -2, 2, 2, 2, 3, 3, 3, 0, 0, 0, 0, 0, 0, 0, -1, -2, -3, -1, -2, -2, 0, 0, 0, -2, 0, 0, 0];
    MinMaxUtility minMaxUtility = new MinMaxUtility();
    BackgammonBoard backgammonBoard = new BackgammonBoard();
    int currentPlayer = 1;
    backgammonBoard.Position = sample;
    backgammonBoard.CurrentPlayer = currentPlayer;
    int die1 = 2;
    int die2 = 4;
    backgammonBoard.Die1 = die1;
    backgammonBoard.Die2 = die2;
    Console.WriteLine("Pos:\n" + backgammonBoard);
    Console.WriteLine("Pos:\n" + string.Join(", ", backgammonBoard.Position));
    var (equals, scorev) = minMaxUtility.MinMax(backgammonBoard.Position, currentPlayer, die1, die2, 1, _neuralNetworks);
    Console.WriteLine("Scorev MinMax:\n" + string.Join(", ", scorev));
    var score1 = ScoreUtility.EvaluatePosition(backgammonBoard.Position, currentPlayer, _neuralNetworks, 0f, 1f, false,true);
    Console.WriteLine("ScoreV1 EvalPos: \n" + string.Join(", ", score1));
    var mirroredPos = BackgammonBoard.MirrorBoard(backgammonBoard.Position);
    var score2 = ScoreUtility.EvaluatePosition(mirroredPos, BackgammonGameHelper.Opponent(currentPlayer), _neuralNetworks, 0f, 1f, false,true);
    Console.WriteLine("ScoreV2 Eval Mirrored: \n" + string.Join(", ", score2));
    // If we mirror the board and swap player Evaluate Pos should return the same!
}*/

/*
static void testMinMax(NeuralNetwork[] nn)
{
    int[] sample = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, -2, 0, 13, -13];
    MinMaxUtility minMaxUtility = new MinMaxUtility();
    var ply = 1;
    var player = BackgammonBoard.Player2;
    var (eq, scoreVector) = minMaxUtility.EvaluatePositionAverage(sample, player, ply, nn);
    Console.WriteLine(eq + "bear0ff SCV: " + String.Join(",", scoreVector));
    Console.WriteLine("Leafs: " + minMaxUtility.LeafCounter);
}*/

/*
static void testBearoffDatabaseMinMax(NeuralNetwork[] nn)
{
    int[] sample = [0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, -1, 0, -2, 0, 12, -11];
    MinMaxUtility minMaxUtility = new MinMaxUtility();
    var ply = 1000;// to the leaves
    var player = BackgammonBoard.Player1;
    var (eq, scoreVector) = minMaxUtility.EvaluatePositionAverage(sample, player, ply, nn);
    Console.WriteLine(eq + "bear0ff" + String.Join(",", scoreVector));
    Console.WriteLine("Leafs: " + minMaxUtility.LeafCounter);
}*/

//Pos Evaluators where the nns are cloned
static Dictionary<PositionType, IBackgammonPositionEvaluator> clonePosEvaluators(Dictionary<PositionType, IBackgammonPositionEvaluator> source) {
    var positionNeuralEvalDictClone = new Dictionary<PositionType, IBackgammonPositionEvaluator>();

    foreach (KeyValuePair<PositionType, IBackgammonPositionEvaluator> kvp in source)
    {
        if (kvp.Value is NeuralNetworkPositionEvaluator)
        {
            NeuralNetwork nn = ((NeuralNetworkPositionEvaluator)kvp.Value).NeuralNetwork;
            var nnClone = nn.Clone();
            positionNeuralEvalDictClone[kvp.Key] = new NeuralNetworkPositionEvaluator(nnClone, kvp.Key);
        }
        else {
            positionNeuralEvalDictClone[kvp.Key] = kvp.Value;
        }
    }
    return positionNeuralEvalDictClone;
}

// Retrieve the environment variable
string dataPath = Environment.GetEnvironmentVariable("BG_DATA_PATH");

var modelsDir = Path.Combine(dataPath, "neuralnets");
Dictionary<string, float[]> bearOffDatabase = [];
var logDir = Path.Combine(dataPath, "logs");
//var names = NeuralNetworkManager.getModelNames();
var positionEvaluatorDict = NeuralNetworkManager.GetBackgammonPosEvaluators(modelsDir, logDir);


static string getBearOffFileName(string dir, int maxCheckers) {
    return Path.Combine(dir, "bearoff" + maxCheckers + ".json");
}

var jsonBearOffFilename = getBearOffFileName(modelsDir, BearOffUtility.MaxCheckers);
{
    try
    {
        string jsonBearOff = File.ReadAllText(jsonBearOffFilename);
        bearOffDatabase = JsonConvert.DeserializeObject<Dictionary<string, float[]>>(jsonBearOff);
    }
    catch (Exception e)
    {
        Console.WriteLine("Couldnt load bear off db");
    }
}

if (bearOffDatabase is null || bearOffDatabase.Count() == 0)
{
    Console.WriteLine("Creating bearoff db");
    var bearOffDictSmaller = new Dictionary<string, float[]>();
    var jsonBearOffFilenameSmaller = getBearOffFileName(modelsDir, BearOffUtility.MaxCheckers-1);

    //Read the JSON string from a file
    //var createDatabase = false;
    //for (int i = BeafOffUtility.MaxCheckers; i >= 1; i--)
    {
        try
        {
            string jsonBearOff = File.ReadAllText(jsonBearOffFilenameSmaller);
            bearOffDictSmaller = JsonConvert.DeserializeObject<Dictionary<string, float[]>>(jsonBearOff);
        }
        catch (Exception e)
        {
            Console.WriteLine("Couldnt load smaller bearoff db");
        }
    }

    bearOffDatabase = BearOffUtility.CreateBearOffDataBase(bearOffDictSmaller);
    string json = JsonConvert.SerializeObject(bearOffDatabase, Formatting.Indented);
    // Save the JSON string to a file
    File.WriteAllText(jsonBearOffFilename, json);
}

var bearOffEvaluator = new BearoffDatabaseEvaluator(bearOffDatabase);
positionEvaluatorDict.Add(PositionType.BearOffDatabase, bearOffEvaluator);

var gameLogs = Path.Combine(dataPath, "logs", "games.log");

var gameSimulator = new GameSimulator(positionEvaluatorDict, logDir);

var moneyGame = gameSimulator.PlayMoneygame();
gameSimulator.exportGameToFile(moneyGame);

/*var clonedEvaluators = clonePosEvaluators(positionEvaluatorDict);
foreach (var move in moneyGame.MoveData) {
    var position = move.BoardBefore;
    var positionType = BackgammonBoard.MapBoardToPositionType(position);
    var nn = ((NeuralNetworkPositionEvaluator)positionEvaluatorDict[positionType]).NeuralNetwork;
    var nnClone = ((NeuralNetworkPositionEvaluator)clonedEvaluators[positionType]).NeuralNetwork;
    Console.WriteLine("Compare after clone: " + positionType);
    nn.Compare(nnClone);
    var (inputData, _) = BoardToNeuralInputsEncoder.EncodeBoardToNeuralInputs(position, positionType, 0);    
    nn.FeedForward(inputData);
    nnClone.FeedForwardNNUE(inputData);
    Console.WriteLine("Compare after feedforward");
    nn.Compare(nnClone);
}
Environment.Exit(0);*/

var gameDataTrainLogs = Path.Combine(logDir, "trainData.log");
var gameDataTrainer = new GameDataTrainer(positionEvaluatorDict, gameDataTrainLogs);

var totalPlayingTime = 0L;
var totalTrainingTime = 0L;

var nrOfTrainingGames = 500;
var trainFrequency = 2;
var saveFrequency = 100;
var maxExtraGames = 300; // When extraTrainPositions exceeds this we dont add any more
var inspectLearningFrequency = 20;
var batchLearningRate = 0.02f;
int epochs = 10;
var trainingGamesData = new List<TrainingData>[nrOfTrainingGames];
List<(int[], int)> extraTrainPositions = [];
int saveCounter = 0;
int maxExtraSubPositions = 30; // How many extra max subpositions from one game (, can be recursive)
int maxExtraMoveCandidates = 3;// How many extra max subpositions from one position (, can be recursive)
int maxMovesToPlay = 60; // How many moves to play in a subposition
int subPositions = 0;
//modelContact.CheckActivationHistory(true);
var rand = new Random();
for (int i = 0; i < 90000; i++)
{
    Stopwatch stopwatchPlay = Stopwatch.StartNew();
    GameData? gameData = null;
    bool gameDataIsExtraGame = false;
    if (extraTrainPositions.Any())
    {
        var lastItem = extraTrainPositions[extraTrainPositions.Count - 1]; // Pick the last item
        extraTrainPositions.RemoveAt(extraTrainPositions.Count - 1); // Remove the last item
        Console.WriteLine("Simulate an extra game" + i);
        gameData = gameSimulator.SimulateSingleGame(lastItem.Item1, lastItem.Item2, maxMovesToPlay);
        gameDataIsExtraGame = true;
    }
    if (gameData is null || gameData.IsEmpty())
    {
        Console.WriteLine("Simualate a game"+i);
        gameData = gameSimulator.SimulateSingleGame();
        if (gameData.IsEmpty())
            Console.WriteLine("Empty GameData" + i);
        subPositions = 0;
    }
    stopwatchPlay.Stop();
    totalPlayingTime += stopwatchPlay.ElapsedMilliseconds;
    // Console.WriteLine($"GEN TrainData");
    // Seems we can use a higher learning rate for batchupdate
    //var singlePatternLearningRate = 0.02f;

    var trainData = gameDataTrainer.GameToTrainingDataMinMax(gameData, batchLearningRate);

    trainingGamesData[i % nrOfTrainingGames] = trainData;
    Stopwatch stopwatchTrain = Stopwatch.StartNew();
    if (i % trainFrequency == 0 && i>100)
    {
        // Flatten the list of lists into a single list 
        // Flatten the list of lists into a single list, excluding null entries
        var flatList = trainingGamesData
            .Where(list => list != null)  // Exclude null lists
            .SelectMany(list => list)     // Flatten the lists
            .ToList();
        Console.WriteLine("Train on patterns:" + flatList.Count);

        // Partition the flat list into two lists based on ModelIndex
        // var flatLists = new List<TrainingData>[2];
        
        Dictionary<PositionType, List<TrainingData>> trainingPatternsDict = [];
        // We should maybe update the this list each epoch so we get new target values
        foreach (var elem in flatList) {
            var positionType = BackgammonBoard.MapBoardToPositionType(elem.board);
            if (positionType != PositionType.BearOffDatabase) {
                if (!trainingPatternsDict.ContainsKey(positionType))
                { 
                    trainingPatternsDict.Add(positionType, new List<TrainingData>());
                }
                trainingPatternsDict[positionType].Add(elem);
            }
        }

        foreach (var positionType in trainingPatternsDict.Keys) {
            var trainingPatterns = trainingPatternsDict[positionType];
            var neuralNetwork = ((NeuralNetworkPositionEvaluator) positionEvaluatorDict[positionType]).NeuralNetwork;
            Console.WriteLine("Train" +  positionType + " patterns"+ trainingPatterns.Count + ": " + neuralNetwork.DetfaultfilePath);
            var sample = trainingPatterns[0];
            Console.WriteLine("sample" +  string.Join(", ", sample.board));
            Console.WriteLine("sample input" +  sample.InputData.Length);
            neuralNetwork.BatchUpdate(trainingPatterns, epochs);
        }

    }
    stopwatchTrain.Stop();
    totalTrainingTime += stopwatchTrain.ElapsedMilliseconds;
    if (i % inspectLearningFrequency == 0)
    {
        Console.WriteLine($"Game nr: {i} positions {gameData.MoveData.Count}");
        Console.WriteLine($"Playing time: {totalPlayingTime / 1000L} s");
        Console.WriteLine($"Training time: {totalTrainingTime / 1000L} s");
        foreach (var (key, value) in positionEvaluatorDict)
        {
            if (value is NeuralNetworkPositionEvaluator neuralNetworkEvaluator)
            {
                Console.WriteLine(key+ "NN" + neuralNetworkEvaluator.NeuralNetwork.Description);
                var (_ ,labels) = BoardToNeuralInputsEncoder.EncodeBoardToNeuralInputs(BackgammonPositions.BarpointHoldingGame, key);
                neuralNetworkEvaluator.NeuralNetwork.checkMaxFeatureRelevance();
                neuralNetworkEvaluator.NeuralNetwork.CheckForwardTime();
            }
        }
        /*models[0].CheckForwardTime();
        models[1].CheckForwardTime();
        //model.CheckNNUERatio();
        //model.CheckDeadInputs();
        //modelContact.CheckWeightsInc();
        //modelContact.CheckActivationHistory();
        
        var (_, labelsNoContact) = BoardToNeuralInputsEncoder.EncodeBoardToNeuralInputs(BackgammonPositions.BearOffGamesNoContact[0], modelIndex:0);

        models[0].checkMaxFeatureRelevance(labelsNoContact);
        Console.WriteLine();
        //models[1].checkFeatureRelevance(labelsContact);
        models[1].checkMaxFeatureRelevance(labelsContact);
       */
    }

    //Most extra games we add from the 'main' game
    var addExtraGameProbability = gameDataIsExtraGame ? 0.02 : 0.1;
    if (subPositions < maxExtraGames && rand.NextDouble()<addExtraGameProbability)
    {
        var generatedSubPositions = gameSimulator.generateExtraData(gameData, maxExtraSubPositions, maxExtraMoveCandidates);
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
        foreach (var (key, value) in positionEvaluatorDict)
        {
            if (value is NeuralNetworkPositionEvaluator neuralNetworkEvaluator)
            {
                neuralNetworkEvaluator.NeuralNetwork.Save();
                neuralNetworkEvaluator.NeuralNetwork.Save(saveCounter);
            }
        }

        moneyGame = gameSimulator.PlayMoneygame();
        gameSimulator.exportGameToFile(moneyGame);

        //Code for checking that a loaded network behave the same
        //modelContact.Save(modelFile);
        //noContactModel.Save(modelNoContFile);

        //modelContact.Save(modelsDir + $"\\{modelDescription1}CP{saveCounter}.json");
        //noContactModel.Save(modelsDir + $"\\{modelDescriptionNoContact}CP{saveCounter}.json");
        /* var model2 = NeuralNetwork.Load(modelFile);
        model.Compare(model2);
        model.CheckActivationHistory(true);
        Console.WriteLine("model2");
        foreach (var testData in trainingGamesData[0]) {
            var result1 = model.FeedForward(testData.InputData);
            var result2 = model2.FeedForward(testData.InputData);
            Console.WriteLine("Comparing input" + string.Join("," , testData.InputData));
            for (int resIndex = 0; resIndex < result1.Length; resIndex++) {
                Console.Write("["+result1[resIndex] + " , " +  result2[resIndex]+"]");
            }
            break;
        }
        model2.CheckActivationHistory(true);*/
        //Environment.Exit(0);
    }
}
