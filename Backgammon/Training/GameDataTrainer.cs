using Backgammon.Models;
using Backgammon.Models.NeuralNetwork;
using Backgammon.Util;
using Backgammon.Util.AI;
using Backgammon.Utils;
using Serilog;
using static Backgammon.Util.Constants;

namespace Backgammon.Training
{
    public class GameDataTrainer
    {
        private readonly ILogger _trainLogger; // Custom logger for game simulation
        private readonly MinMaxUtility _minMaxUtility; // Custom logger for game simulation
        
        public GameDataTrainer(Dictionary<PositionType, IBackgammonPositionEvaluator> positionEvaluators, string logFilePath)
        {
            //_neuralNetworks = neuralNetworks;
            // Optional: Clear the log file at startup
            // ClearLogFile(logFilePath);
            // Configure Serilog
            _trainLogger = CreateLogger(logFilePath); // Initialize the custom logger
            _minMaxUtility = new MinMaxUtility(positionEvaluators);
            //_minMaxUtility.useClampValues(ClampMin, ClampMax);
            //_bearOffDatabase = bearOffDatabase;

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

        public void inspectTrainingData(TrainingData trainingData, int player)
        {
            BackgammonBoard backgammonBoard = new BackgammonBoard();
            backgammonBoard.Position = trainingData.board;
            Console.WriteLine($"Train board \n {backgammonBoard}");
            Console.WriteLine($"player \n {player}");
            Console.WriteLine($"Target: {string.Join(", ", trainingData.Target)} ");
            Console.WriteLine($"Inputs: {string.Join(", ", trainingData.InputData)} ");
        }

        public List<TrainingData> GameToTrainingDataMinMax(GameData gameData, float learningRate)
        {
            List<TrainingData> trainingDatas = [];
            var moveDataListReversed = gameData.MoveData.ToList().AsEnumerable().Reverse();
            var backgammonBoard = new BackgammonBoard();
            var initalLearningRate = learningRate;
            PositionType? previousPositionType = null;
            foreach (var elem in moveDataListReversed)
            {
                var trainingBoard = elem.BoardBefore;
                var playerAtTurn = elem.Player;
                //Just for debugging
                backgammonBoard.Position = trainingBoard;
                backgammonBoard.CurrentPlayer = playerAtTurn;

                if (BearOffUtility.IsBearOffPosition(trainingBoard))
                {
                    //We use a bearoff database for these positions so no training needed here
                    continue;
                }
                _trainLogger.Information("Board");
                _trainLogger.Information("\n" + backgammonBoard);

                var ply = 1;
                (float equity, float[] scoreVector) = _minMaxUtility.EvaluatePositionAverage(trainingBoard, playerAtTurn, ply);

                _trainLogger.Information(" ScoreVec MinMax" + string.Join(", ", scoreVector));
                //var modelIndex = BoardToNeuralInputsEncoder.MapBoardToModel(trainingBoard);
                var positionType = BackgammonBoard.MapBoardToPositionType(trainingBoard);
                
                var (inputs, _) = BoardToNeuralInputsEncoder.EncodeBoardToNeuralInputs(trainingBoard, positionType, playerAtTurn);

                if (MirrorBoardForPlayer2 && elem.Player == BackgammonBoard.Player2)
                {
                    var mirroredScore = ScoreUtility.MirrorScore(scoreVector);
                    // the inputs is already mirrored by EncodeBoardToNeuralInputs
                    var trainingData = new TrainingData(inputs, mirroredScore, learningRate, positionType)
                    {
                        board = trainingBoard
                    };
                    trainingDatas.Add(trainingData);
                    //inspectTrainingData(trainingData, playerAtTurn);
                }
                else
                {
                    var trainingData = new TrainingData(inputs, scoreVector, learningRate, positionType)
                    {
                        board = trainingBoard
                    };
                    trainingDatas.Add(trainingData);
                    //inspectTrainingData(trainingData, playerAtTurn);
                }
                if (previousPositionType.HasValue && previousPositionType != positionType)
                {
                    learningRate = initalLearningRate;
                }
                else
                {                    
                    learningRate *= 0.8f;
                }
                previousPositionType = positionType;
            }
            return trainingDatas;
        }

        /*
        public List<TrainingData> GameToTrainingData(GameData gameData, float learningRate)
        {
            var initialLearningRate = learningRate;
            List<TrainingData> trainingDatas = [];
            List<TrainingData> trainingDatasMirrored = [];
            var moveData = gameData.MoveData;
            var lastPosition = moveData.Last();
            if (lastPosition is null)
            {
                return trainingDatas;
            }
            var modelIndex = BoardToNeuralInputsEncoder.MapBoardToModel(lastPosition.BoardAfter);
            // Create tranining data from last position (BoardAfter in last movedata)
            var playerAtTurnBoardAfter = BackgammonGameHelper.Opponent(lastPosition.Player);
            //We get mirrored inputs for P2
            var (inputsLastPos, _) = BoardToNeuralInputsEncoder.EncodeBoardToNeuralInputs(lastPosition.BoardAfter, modelIndex, playerAtTurnBoardAfter);
            // To be used for temporal difference with previous positions

            var lastScoreVector = ScoreUtility.EvaluatePosition(lastPosition.BoardAfter, playerAtTurnBoardAfter, _positionEvaluators);
            //The scored is based on the actual provided board position though so we should mirror the score for player to match the train inputs

            //Test skipping adding final position to avoid training values near zero and one
            if (MirrorBoardForPlayer2 && playerAtTurnBoardAfter == BackgammonBoard.Player2)
            {
                var mirroredScore = ScoreUtility.MirrorScore(lastScoreVector);
                var trainingData = new TrainingData(inputsLastPos, mirroredScore, learningRate, modelIndex);
                trainingData.board = lastPosition.BoardAfter;
                //trainingDatas.Add(trainingData);
                //inspectTrainingData(trainingData);
            }
            else
            {
                var trainingData = new TrainingData(inputsLastPos, lastScoreVector, learningRate, modelIndex);
                trainingData.board = lastPosition.BoardAfter;
                //trainingDatas.Add(new TrainingData(inputsLastPos, lastScoreVector, learningRate, modelIndex));
                //inspectTrainingData(trainingData);
            }
            // Player was at turn before before BoardAfter so we want to add the mirrored lastposition with same player again.
            //trainingDatasMi   rrored.Add(MirroredTrainingData(lastPosition.BoardAfter, playerAtTurnBoardAfter, lastScoreVector, learningRate, modelIndex));
            //var lastTrainingData = new TrainingData(inputsLastPos, lastScoreVector, learningRate);

            var moveDataListReversed = gameData.MoveData.ToList().AsEnumerable().Reverse();
            int index = moveDataListReversed.Count();
            var backgammonBoard = new BackgammonBoard();
            // A high factor like 0.9 means that the current targetVector will keep 90% of it's value compared to the vector we combine it with
            // When combining two vectors with larger time distance we want decrease the effect of combining with previous vector
            // So lets say the final pos i time n then n-1 would use 0.9 n-2 0.8
            var decayRate = 0.02f;
            var combineVectorsFactor = 0.9f;
            var prevModelIndex = -1; //Use -1 as 'undefined' for first iteration
            var prevScoreVector = lastScoreVector;
            foreach (var elem in moveDataListReversed)
            {
                // We learn from the end and in early training some can van be many thousand of moves
                if (moveDataListReversed.Count() - index > 150) { break; }
                float[] scoreVector;
                bool final2PlyPosition = false;
                modelIndex = BoardToNeuralInputsEncoder.MapBoardToModel(elem.BoardBefore);

                if (moveDataListReversed.Count() - index < 2 && !BackgammonBoard.StillContact(elem.BoardBefore))
                {
                    final2PlyPosition = true;
                    backgammonBoard.Position = elem.BoardBefore;
                    backgammonBoard.CurrentPlayer = elem.Player;
                    _trainLogger.Information("Board");
                    _trainLogger.Information("\n" + backgammonBoard);

                    var ply = 2;
                    (float equity, scoreVector) = _minMaxUtility.EvaluatePositionAverage(elem.BoardBefore, elem.Player, ply);
                    // WE should consider 2 ply here as the truth and not combine with the pos n (if this is n-1) from he end
                    _trainLogger.Information(" ScoreVec MinMax" + string.Join(", ", scoreVector));
                    //_trainLogger.Information(" ScoreVecAdjusted" + string.Join(", ", scoreVector));
                    // Min max should now use clamp need to check
                    // ScoreVector = ScoreUtility.AdjustEstimatedScore(scoreVector, elem.BoardBefore, ClampMin, ClampMax);
                }
                else
                {
                    //temp testing using minmax ..
                    //(float equity, scoreVector) = _minMaxUtility.EvaluatePositionAverage(elem.BoardBefore, elem.Player, 1 , _neuralNetworks);
                    scoreVector = ScoreUtility.EvaluatePosition(elem.BoardBefore, elem.Player, _positionEvaluators);
                    _trainLogger.Information("Board to evaluate" + string.Join(", ", elem.BoardBefore));
                    _trainLogger.Information("ScoreVec Eval Pos" + string.Join(", ", scoreVector));
                }

                if (prevModelIndex >= 0 && prevModelIndex != modelIndex)
                {
                    // lets have a good estimation that we will use for the model
                    // (float equity, scoreVector) = _minMaxUtility.EvaluatePositionAverage(elem.BoardBefore, elem.Player, 2, _neuralNetworks);
                    _trainLogger.Information("Board before" + string.Join(", ", elem.BoardBefore));
                    _trainLogger.Information("Contact finished" + string.Join(", ", elem.BoardAfter));
                    lastScoreVector = prevScoreVector;
                    combineVectorsFactor = 0.9f;
                    learningRate = initialLearningRate;

                    _trainLogger.Warning(index + "UPDATING Last ScoreVec" + string.Join(", ", lastScoreVector));
                    // we are about to switch neural networks and what to use the last evaluation for combining score vectors
                }
                prevModelIndex = modelIndex;
                prevScoreVector = scoreVector;
                _trainLogger.Information("Board" + string.Join(", ", elem.BoardBefore));
                _trainLogger.Information(index + " ScoreVec" + string.Join(", ", scoreVector));
                // Console.WriteLine(index + " ScoreVec" + string.Join(", ", scoreVector));
                // Console.WriteLine(index + " LastScoreVec" + string.Join(", ", lastScoreVector));
                float[] scoreVectorCombined;
                if (final2PlyPosition)
                {
                    scoreVectorCombined = scoreVector;
                }
                else
                {
                    // For early training this param can be quite hight to faster propagate learning backwards
                    // but for final training should be much closer to 1
                    //var scoreVectorWeight = 0.8f;
                    scoreVectorCombined = CombineVectors(scoreVector, lastScoreVector, combineVectorsFactor);
                    combineVectorsFactor = DecayTowardsOneNextValue(combineVectorsFactor, decayRate);
                    _trainLogger.Information(" Combine Vec Factor" + combineVectorsFactor);
                }

                var (inputs, _) = BoardToNeuralInputsEncoder.EncodeBoardToNeuralInputs(elem.BoardBefore, modelIndex, elem.Player);

                //lastScoreVector = scoreVector;
                // Changed to this as I feel it can be a better parameter for next CombineVectors
                // I think this can speed up the learning..
                //lastScoreVector = scoreVectorCombined;
                if (Constants.MirrorBoardForPlayer2 && elem.Player == BackgammonBoard.Player2)
                {
                    var mirroredScore = ScoreUtility.MirrorScore(scoreVectorCombined);
                    // the inputs should be mirrored here..
                    var trainingData = new TrainingData(inputs, mirroredScore, learningRate, modelIndex)
                    {
                        board = elem.BoardBefore
                    };
                    trainingDatas.Add(trainingData);
                    //inspectTrainingData(trainingData);
                }
                else
                {
                    var trainingData = new TrainingData(inputs, scoreVectorCombined, learningRate, modelIndex)
                    {
                        board = elem.BoardBefore
                    };
                    trainingDatas.Add(trainingData);
                    //inspectTrainingData(trainingData);
                }

                //var mirroredTrainData = MirroredTrainingData(elem.BoardBefore, elem.Player, scoreVectorCombined, learningRate, modelIndex);
                //trainingDatasMirrored.Add(mirroredTrainData);
                _trainLogger.Information(index + " ScvCombined" + string.Join(", ", scoreVectorCombined));
                index--;
                learningRate *= 0.95f;//Focus on learning from the end of them game
                // To do when gammon or backgammon is set target to 0!
            }


            _trainLogger.Information("Postions added" + trainingDatas.Count);
            // var previousScoreVector = gameData()
            //trainingDatas.AddRange(trainingDatasMirrored);
            return trainingDatas;
        }*/

        // When using a mirrored board for learning the game (player at turn is not part of the input) then 
        // to generare mirrored trainingdata we need a mirrored inputdata and a mirrored score
        // Should only be use when wen dont acutallt mirror inputs and have player part of input
        private TrainingData MirroredTrainingData(int[] board, int player, float[] targetVector, float learningRate, PositionType positionType)
        {
            var opponent = BackgammonGameHelper.Opponent(player);
            // Calling encode with opponent will mirror the when constant MirrorBoardForPlayer2 = true and player =2
            var mirrorBoard = true;
            var (inputDataMirrored, _) = BoardToNeuralInputsEncoder.EncodeBoardToNeuralInputs(board, positionType, opponent, mirrorBoard);
            var mirrorScore = ScoreUtility.MirrorScore(targetVector);
            return new TrainingData(inputDataMirrored, mirrorScore, learningRate, positionType);
        }

        /*private TrainingData MirroredTrainingData(int[] board, int player, float[] targetVector, float learningRate, int modelIndex)
        {
            //The logic seems to hold both with and without using mirrored board always for player 2 Constants.MirrorBoardForPlayer2
            var mirrorBoard = BackgammonBoard.MirrorBoard(board);
            //if player is player == player1 Encode will mirror again, not a bug I think as that means 1 was mirrored once in the other data
            var opponent = BackgammonGameHelper.Opponent(player);
            var inputData = BoardToNeuralInputsEncoder.EncodeBoardToNeuralInputs(mirrorBoard, modelIndex, opponent);
            var mirrorScore = ScoreUtility.MirrorScore(targetVector);
            return new TrainingData(inputData, mirrorScore, learningRate, modelIndex);
        }*/

        // For simplicity we do a forward pass in Train before backprop
        // (it's needed to do a forward pass but it can be optimized from calling code since we have already have done a forward) 
        public void Train(float[] inputData, float[] target, float learningRate, int epochs, NeuralNetwork nn)
        {
            //Need to be updated for choosing models
            if (learningRate > 0.000002) //Bad temporar debug condition
            {
                _trainLogger.Information("InputData" + inputData.Length);
                _trainLogger.Information("Target" + target.Length);
                var roundedTargets = target.Select(t => Math.Round(t, 4).ToString());
                _trainLogger.Information("Target: " + string.Join(", ", roundedTargets));
                _trainLogger.Information("Start training learn rate=" + learningRate);
            }
            var previousMse = 0f;
            for (int i = 0; i < epochs; i++)
            {
                var predict = nn.FeedForward(inputData);
                var lasso = learningRate / 10f;
                var mse = 0f;//
                mse = nn.Backpropagate(predict, target, learningRate, lasso);
                if (learningRate > 0.0000008)
                {
                    var roundedPredict = predict.Select(t => Math.Round(t, 3).ToString());
                    _trainLogger.Information("Predict: " + string.Join(", ", roundedPredict));

                    /*foreach (var value in roundedPredict)
                    {
                        _trainLogger.Information($"{value} ");
                    }*/
                    _trainLogger.Information("MSE: " + Math.Round(mse, 6));
                    if (i > 0)
                    {
                        _trainLogger.Information("Improvement: " + (previousMse - mse));
                    }
                }
                previousMse = mse;
            }
        }

        private static float[] CombineVectors(float[] vector1, float[] vector2, float weightVector1)
        {
            if (vector1.Length != vector2.Length)
            {
                throw new ArgumentException("Vectors must be of the same length.");
            }

            float[] scoreVector = new float[vector1.Length];

            for (int i = 0; i < vector1.Length; i++)
            {
                scoreVector[i] = (vector1[i] * weightVector1) + (vector2[i] * (1 - weightVector1));
            }

            return scoreVector;
        }

        private static float DecayTowardsOneNextValue(float currentValue, float decayRate)
        {
            if (currentValue < 0 || currentValue > 1)
                throw new ArgumentException("Current value must be between 0 and 1.");
            if (decayRate < 0 || decayRate > 1)
                throw new ArgumentException("Decay rate must be between 0 and 1.");

            return currentValue + (1 - currentValue) * decayRate;
        }
    }
}
