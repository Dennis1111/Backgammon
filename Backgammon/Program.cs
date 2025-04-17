using Backgammon.GamePlay;
using Backgammon.Models;
using Backgammon.Util.AI;
using Newtonsoft.Json;
using static Backgammon.Util.Constants;

// Retrieve the environment variable
string dataPath = Environment.GetEnvironmentVariable("BG_DATA_PATH");

var modelsDir = Path.Combine(dataPath, "neuralnets");
Dictionary<string, float[]> bearOffDatabase = [];
var logDir = Path.Combine(dataPath, "logs");
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

//var gameLogs = Path.Combine(dataPath, "logs", "games.log");

var gameSimulator = new GameSimulator(positionEvaluatorDict, logDir);

var moneyGame = gameSimulator.PlayMoneygame();
gameSimulator.exportGameToFile(moneyGame);

var nrOfGames = 100000;
var trainFrequency = 3;
var epochs = 2;
var saveFrequency = 100;

gameSimulator.playAndTrain(nrOfGames, trainFrequency, epochs, saveFrequency);

// Testing code for NNUE
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
//Environment.Exit(0);
foreach (var positionEvaluator in positionEvaluatorDict) {
    var value = positionEvaluator.Value;
    if (value is NeuralNetworkPositionEvaluator) {
        ((NeuralNetworkPositionEvaluator)value).NeuralNetwork.EnableNNUE(true);
    }
}*/