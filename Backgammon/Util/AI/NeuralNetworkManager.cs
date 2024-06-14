using Backgammon.Models.NeuralNetwork.ActivationFunctions;
using Backgammon.Models.NeuralNetwork;
using Backgammon.Models;
using static Backgammon.Util.Constants;

namespace Backgammon.Util.AI
{
    public static class NeuralNetworkManager
    {
        private static readonly int[] NoContactPosition = BackgammonPositions.BearOffGamesNoContact[0];

        public static Dictionary<PositionType, IBackgammonPositionEvaluator> GetBackgammonPosEvaluators(String modelsDir, String logDir) {
            Dictionary<PositionType, IBackgammonPositionEvaluator> dict = [];
            NeuralNetwork noContact = NoContactNeuralNetwork(modelsDir,logDir);
            var posEvaluatorNoContact = new NeuralNetworkPositionEvaluator(noContact, PositionType.NoContact);
            dict.Add(PositionType.NoContact, posEvaluatorNoContact);
            
            var hiddens1 = 16;
            var hiddens2 = 16;
            var contactNN = ContactNeuralNetwork(modelsDir, logDir,  hiddens1, hiddens2, "ContactNN16");
            var posEvaluatorContact = new NeuralNetworkPositionEvaluator(contactNN, PositionType.Contact);
            dict.Add(PositionType.Contact, posEvaluatorContact);

            var backgame12 = ContactNeuralNetwork(modelsDir, logDir, hiddens1, hiddens2, "Backgame12");
            var posEvaluatorBackgame12 = new NeuralNetworkPositionEvaluator(backgame12, PositionType.Backgame12);
            dict.Add(PositionType.Backgame12, posEvaluatorBackgame12);

            var backgame13 = ContactNeuralNetwork(modelsDir, logDir, hiddens1, hiddens2, "Backgame13");
            var posEvaluatorBackgame13 = new NeuralNetworkPositionEvaluator(backgame13, PositionType.Backgame13);
            dict.Add(PositionType.Backgame13, posEvaluatorBackgame13);

            var backgame23 = ContactNeuralNetwork(modelsDir, logDir, hiddens1, hiddens2, "Backgame23");
            var posEvaluatorBackgame23 = new NeuralNetworkPositionEvaluator(backgame23, PositionType.Backgame13);
            dict.Add(PositionType.Backgame23, posEvaluatorBackgame23);

            var sixPrime = ContactNeuralNetwork(modelsDir, logDir, hiddens1, hiddens2, "SixPrime");
            var posEvaluatorSixPrime = new NeuralNetworkPositionEvaluator(sixPrime, PositionType.SixPrime);
            dict.Add(PositionType.SixPrime, posEvaluatorSixPrime);

            var fivePrime = ContactNeuralNetwork(modelsDir, logDir, hiddens1, hiddens2, "FivePrime");
            var posEvaluatorFivePrime = new NeuralNetworkPositionEvaluator(fivePrime, PositionType.FivePrime);
            dict.Add(PositionType.FivePrime, posEvaluatorFivePrime);

            setInputLabels(dict);
            return dict;
        }

        private static void AddNeuralNetworkToDict(Dictionary<PositionType, IBackgammonPositionEvaluator> dict, 
            string description, int hiddens1, int hiddens2, PositionType positionType, string modelsDir, string logDir) {
            var nn = ContactNeuralNetwork(modelsDir, logDir, hiddens1, hiddens2, description);
            var posEvaluatorContact = new NeuralNetworkPositionEvaluator(nn, positionType);
            dict.Add(positionType, posEvaluatorContact);
        }

        private static void setInputLabels(Dictionary<PositionType, IBackgammonPositionEvaluator> neuralNetworkDict) {
            foreach (var (positionType, nn) in neuralNetworkDict)
            {
                if (nn is NeuralNetworkPositionEvaluator neuralNetworkEvaluator)
                {
                    var (_, labels) = BoardToNeuralInputsEncoder.EncodeBoardToNeuralInputs(BackgammonPositions.BarpointHoldingGame, positionType);
                    neuralNetworkEvaluator.NeuralNetwork.SetInputLabels(labels);                   
                }
            }
        }

        private static NeuralNetwork ContactNeuralNetwork(string modelsDir, string logDir, int hiddens1, int hiddens2, string modelDescription)
        {
            var modelFile = Path.Combine(modelsDir, modelDescription + ".json");
            var modelIndexNoContact = BoardToNeuralInputsEncoder.MapBoardToModel(NoContactPosition);
            NeuralNetwork? modelContact = NeuralNetwork.Load(modelFile);
            if (modelContact is null)
            {
                var (encodedInputs, _) = BoardToNeuralInputsEncoder.EncodeContactGameToNeuralInputs(BackgammonPositions.TwoOneSlotOpening);
                Console.WriteLine("creating new Contact NN" + encodedInputs.Length);
                modelContact = GetLeakyNNUENeuralNetworkModel(encodedInputs.Length, 16, 16,logDir);
                // Handle the case where the network could not be loaded
                
            }
            modelContact.DetfaultfilePath = modelFile;
            return modelContact;
        }

        private static NeuralNetwork NoContactNeuralNetwork(string modelsDir, string logDir) {
            var modelDescription = "NoContactNN";
            var modelFile = Path.Combine(modelsDir, modelDescription + ".json");
            var modelIndexNoContact = BoardToNeuralInputsEncoder.MapBoardToModel(NoContactPosition);
            NeuralNetwork? modelNoContact = NeuralNetwork.Load(modelFile);
            if (modelNoContact is null)
            {
                var (encodedInputs, _) = BoardToNeuralInputsEncoder.EncodeBoardToNeuralInputs(NoContactPosition, modelIndexNoContact);
                Console.WriteLine("creating new nn no contact" + encodedInputs.Length);
                modelNoContact = GetNoContactNNUENeuralNetworkModel(encodedInputs.Length, logDir);
                
                // Handle the case where the network could not be loaded
            }
            modelNoContact.DetfaultfilePath = modelFile;
            return modelNoContact;
        }

        private static NeuralNetwork BackgameNeuralNetwork(string modelsDir, string modelDescription, string logDir)
        {
            var modelFile = Path.Combine(modelsDir, modelDescription + ".json");
            var modelIndexNoContact = BoardToNeuralInputsEncoder.MapBoardToModel(NoContactPosition);
            NeuralNetwork? modelNoContact = NeuralNetwork.Load(modelFile);
            if (modelNoContact is null)
            {
                var (encodedInputs, _) = BoardToNeuralInputsEncoder.EncodeBoardToNeuralInputs(NoContactPosition, modelIndexNoContact);
                Console.WriteLine("creating new nn" + modelDescription + encodedInputs.Length);
                modelNoContact = GetNoContactNNUENeuralNetworkModel(encodedInputs.Length, logDir);
                modelNoContact.DetfaultfilePath = modelFile;
                // Handle the case where the network could not be loaded
            }
            return modelNoContact;
        }


        public static NeuralNetwork[] getNeuralNetworks(String modelsDir, String logDir)
        {
                var models = new NeuralNetwork[2];
            var backgammonBoard = new BackgammonBoard();
            var startPos = backgammonBoard.Position;
            var modelIndexStartPos = BoardToNeuralInputsEncoder.MapBoardToModel(startPos);//->1

            var modelDescription = "ContactNN";
            var modelFile = Path.Combine(modelsDir, modelDescription + ".json");
            Console.WriteLine("models file" + modelFile);
            NeuralNetwork? modelContact = NeuralNetwork.Load(modelFile);
            if (modelContact is null)
            {
                var (encodedInputs, inputLabels) = BoardToNeuralInputsEncoder.EncodeBoardToNeuralInputs(startPos, modelIndexStartPos);
                Console.WriteLine("creating new Contact NN" + encodedInputs.Length);
                modelContact = GetLeakyNNUENeuralNetworkModel(encodedInputs.Length,64,32,logDir);
                // Handle the case where the network could not be loaded
                
                Console.WriteLine("models Contact default" + modelContact.DetfaultfilePath);
            }
            modelContact.DetfaultfilePath = modelFile;
            models[modelIndexStartPos] = modelContact;

            modelDescription = "NoContactNN";
            modelFile = Path.Combine(modelsDir, modelDescription + ".json");

            var modelIndexNoContact = BoardToNeuralInputsEncoder.MapBoardToModel(NoContactPosition);
            NeuralNetwork? modelNoContact = NeuralNetwork.Load(modelFile);
            if (modelNoContact is null)
            {
                var (encodedInputs, _) = BoardToNeuralInputsEncoder.EncodeBoardToNeuralInputs(NoContactPosition, modelIndexNoContact);
                Console.WriteLine("creating new nn no contact" + encodedInputs.Length);
                modelNoContact = GetNoContactNNUENeuralNetworkModel(encodedInputs.Length, logDir);
                
                // Handle the case where the network could not be loaded
            }
            modelNoContact.DetfaultfilePath = modelFile;
            models[modelIndexNoContact] = modelNoContact;
            return models;
        }

        static NeuralNetwork GetNoContactNNUENeuralNetworkModel(int inputSize, string loggerPath)
        {
            var description = "Leaky16,8,6Lin";
            // Define the Sequential model
            var model = new NeuralNetwork(loggerPath, description);
            model.Description = description;

            // Add layers
            model.AddFirstLayer(inputSize, 8, new LeakyReluActivationFunction());
            model.AddLayer(6, new LeakyReluActivationFunction());
            // model.AddLayer(8, new LeakyReluActivationFunction());
            // model.AddLayer(128, new LeakyReluActivationFunction());
            // model.AddLayer(32, new LeakyReluActivationFunction());
            model.AddLayer(6, new SigmoidActivationFunction()); // Assuming 6 outputs for the scores
            return model;
            // SmallModel LeakyRelu 10,8, Linear 6 lasso LR/10 seems robust for learning Bearoff no dead nodes.!
        }

        static NeuralNetwork GetLeakyNNUENeuralNetworkModel(int inputSize, int hiddens1, int hiddens2, string loggerPath)
        {
            var description = "Leaky16,16,6Lin";
            // Define the Sequential model
            var model = new NeuralNetwork(loggerPath, description);
            model.Description = description;

            // Add layers
            model.AddFirstLayer(inputSize, hiddens1, new LeakyReluActivationFunction());
            model.AddLayer(hiddens2, new LeakyReluActivationFunction());
            //model.AddLayer(8, new LeakyReluActivationFunction());
            //model.AddLayer(128, new LeakyReluActivationFunction());
            //model.AddLayer(8, new LeakyReluActivationFunction());
            model.AddLayer(6, new SigmoidActivationFunction()); // Assuming 6 outputs for the scores
            //model.AddLayer(6, new LeakyReluActivationFunction()); // Assuming 6 outputs for the scores
            return model;
            // SmallModel LeakyRelu 10,8, Linear 6 lasso LR/10 seems robust for learning Bearoff no dead nodes.!
        }

        /*
static NeuralNetwork GetLeakyNNUESigmoidNeuralNetworkModel(int inputSize)
{
    // Define the Sequential model
    var model = new NeuralNetwork();
    model.Description = "Leaky64Lin";
    // Add layers
    model.AddFirstLayerNNUE(inputSize, 64, new LeakyReluActivationFunction());
    model.AddLayer(32, new LeakyReluActivationFunction());
    model.AddLayer(16, new LeakyReluActivationFunction());
    model.AddLayer(6, new SigmoidActivationFunction()); // Assuming 6 outputs for the scores
    return model;
}

static NeuralNetwork GetReluNeuralNetworkModel(int inputSize)
{
    // Define the Sequential model
    var model = new NeuralNetwork();
    
    // Add layers
    model.AddFirstLayer(inputSize, 64, new ReluActivationFunction());
    model.AddLayer(32, new ReluActivationFunction());
    model.AddLayer(16, new ReluActivationFunction());
    model.AddLayer(6, new LinearActivationFunction()); // Assuming 6 outputs for the scores
    return model;
}

static NeuralNetwork GetSigmoidNeuralNetworkModel(int inputSize)
{
    // Define the Sequential model
    var model = new NeuralNetwork();
    // Add layers
    model.AddFirstLayer(inputSize, 64, new ReluActivationFunction());
    model.AddLayer(64, new ReluActivationFunction());
    model.AddLayer(32, new ReluActivationFunction());
    model.AddLayer(6, new SigmoidActivationFunction()); // Assuming 6 outputs for the scores
    return model;
}

static NeuralNetwork GetNeuralNetworkModel(int inputSize)
{
    // Define the Sequential model
    var model = new NeuralNetwork();
    // Add layers
    model.AddFirstLayer(inputSize, 64, new ReluActivationFunction());
    //model.AddLayer(64, new ReluActivationFunction());
    model.AddLayer(32, new ReluActivationFunction());
    model.AddLayer(6, new AdjustedSigmoidActivationFunction()); // Assuming 6 outputs for the scores
    return model;
}*/
        /*
static NeuralNetwork GetNNUESigNeuralNetworkModel(int inputSize)
{
    // Define the Sequential model  
    var model = new NeuralNetwork();
    // Add layers
    model.AddFirstLayerNNUE(inputSize, 128, new ReluActivationFunction());
    model.AddLayer(64, new ReluActivationFunction());
    //model.AddLayer(32, new ReluActivationFunction());
    model.AddLayer(6, new SigmoidActivationFunction()); // Assuming 6 outputs for the scores
    return model;
    

static NeuralNetwork GetNNUEReluNeuralNetworkModel(int inputSize)
{
    // Define the Sequential model
    var model = new NeuralNetwork();
    // Add layers
    model.AddFirstLayerNNUE(inputSize, 64, new ReluActivationFunction());
    model.AddLayer(32, new ReluActivationFunction());
    model.AddLayer(16, new ReluActivationFunction());
    model.AddLayer(6, new SigmoidActivationFunction()); // Assuming 6 outputs for the scores
    return model;
}*/

    }
}
