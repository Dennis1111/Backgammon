using Backgammon.GamePlay;

var gameSimulator = new GameSimulator();

var moneyGame = gameSimulator.PlayMoneyGame();
gameSimulator.exportGameToFile(moneyGame);

var nrOfGames = 100000;
var trainFrequency = 3;
var epochs = 2;
var saveFrequency = 100;

gameSimulator.playAndTrain(nrOfGames, trainFrequency, epochs, saveFrequency);