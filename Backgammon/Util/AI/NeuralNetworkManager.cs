using Backgammon.Models.NeuralNetwork.ActivationFunctions;
using Backgammon.Models.NeuralNetwork;
using Backgammon.Models;
using static Backgammon.Util.Constants;
using Backgammon.Util.NeuralEncoding;

namespace Backgammon.Util.AI
{
    public static class NeuralNetworkManager
    {
        private static readonly int[] NoContactPosition = BackgammonPositions.BearOffGamesNoContact[0];

        private static void AddContactTypeEvaluator(PositionType positionType, int hiddens1, int hiddens2, String modelsDir, String logDir, String Description, Dictionary<PositionType, IBackgammonPositionEvaluator> dict) {
            NeuralNetwork nn = ContactNeuralNetwork(modelsDir, logDir, hiddens1, hiddens2, Description);
            var posEvaluator = new NeuralNetworkPositionEvaluator(nn, positionType);
            dict.Add(positionType, posEvaluator);
        }

        public static Dictionary<PositionType, IBackgammonPositionEvaluator> GetBackgammonPosEvaluators(String modelsDir, String logDir) {
            Dictionary<PositionType, IBackgammonPositionEvaluator> dict = [];

            var positionType = PositionType.NoContact;
            NeuralNetwork noContact = NoContactNeuralNetwork(modelsDir,logDir, "NoContactNN");
            var posEvaluatorNoContact = new NeuralNetworkPositionEvaluator(noContact, positionType);
            dict.Add(positionType, posEvaluatorNoContact);

            positionType = PositionType.BearOff;
            NeuralNetwork bearOff = NoContactNeuralNetwork(modelsDir, logDir, "BearOff");
            var posEvaluatorBearoff = new NeuralNetworkPositionEvaluator(bearOff, positionType);
            dict.Add(positionType, posEvaluatorBearoff);

            var hiddens1 = 32;
            var hiddens2 = 16;
            // I Should clean this up with some method
            AddContactTypeEvaluator(PositionType.EarlyGame, hiddens1, hiddens2, modelsDir, logDir, "EarlyGame", dict);
            AddContactTypeEvaluator(PositionType.Contact, hiddens1, hiddens2, modelsDir, logDir, "Contact", dict);
          
            AddContactTypeEvaluator(PositionType.HoldingGame, hiddens1, hiddens2, modelsDir, logDir, "HoldingGame", dict);
            
            AddContactTypeEvaluator(PositionType.MutualHoldingGame, hiddens1, hiddens2, modelsDir, logDir, "MutualHoldingGame", dict);
            AddContactTypeEvaluator(PositionType.ButterFlyAnchor, hiddens1, hiddens2, modelsDir, logDir, "ButterFlyAnchor", dict);
            AddContactTypeEvaluator(PositionType.DeucePointAnchor, hiddens1, hiddens2, modelsDir, logDir, "DeucePointAnchor", dict);
            AddContactTypeEvaluator(PositionType.WeakContact, hiddens1, hiddens2, modelsDir, logDir, "WeakContact", dict);
            AddContactTypeEvaluator(PositionType.Backgame12, hiddens1, hiddens2, modelsDir, logDir, "Backgame12", dict);

            AddContactTypeEvaluator(PositionType.Backgame13, hiddens1, hiddens2, modelsDir, logDir, "Backgame13", dict);
            
            AddContactTypeEvaluator(PositionType.Backgame23, hiddens1, hiddens2, modelsDir, logDir, "Backgame23", dict);
            AddContactTypeEvaluator(PositionType.OtherBackgame, hiddens1, hiddens2, modelsDir, logDir, "OtherBackgame", dict);
            AddContactTypeEvaluator(PositionType.PrimeVsPrime, hiddens1, hiddens2, modelsDir, logDir, "PrimeVsPrime", dict);
            AddContactTypeEvaluator(PositionType.SixPrime, hiddens1, hiddens2, modelsDir, logDir, "SixPrime", dict);

            AddContactTypeEvaluator(PositionType.FivePrime, hiddens1, hiddens2, modelsDir, logDir, "FivePrime", dict);

            AddContactTypeEvaluator(PositionType.FourPrime, hiddens1, hiddens2, modelsDir, logDir, "FourPrime", dict);

            AddContactTypeEvaluator(PositionType.CompletedStage, hiddens1, hiddens2, modelsDir, logDir, "CompletedStage", dict);
            AddContactTypeEvaluator(PositionType.BigRaceLead, hiddens1, hiddens2, modelsDir, logDir, "BigRaceLead", dict);

            AddContactTypeEvaluator(PositionType.Crunched, hiddens1, hiddens2, modelsDir, logDir, "CrunchedStage", dict);

            AddContactTypeEvaluator(PositionType.BigCrunch, hiddens1, hiddens2, modelsDir, logDir, "BigCrunch", dict);

            AddContactTypeEvaluator(PositionType.BearOffContact, hiddens1, hiddens2, modelsDir, logDir, "BearOffContact", dict);
            AddContactTypeEvaluator(PositionType.BearOffContactDefence, hiddens1, hiddens2, modelsDir, logDir, "BearOffContactDef", dict);
            AddContactTypeEvaluator(PositionType.BearOffVsBackgame, hiddens1, hiddens2, modelsDir, logDir, "BearOffVsBackgame", dict);
            AddContactTypeEvaluator(PositionType.BearOffVs1Point, hiddens1, hiddens2, modelsDir, logDir, "BearOffVs1Point", dict);
            AddContactTypeEvaluator(PositionType.BearOffVs1PointDefence, hiddens1, hiddens2, modelsDir, logDir, "BearOffVs1PointDef", dict);
            setInputLabels(dict);
            return dict;
        }

        // This code is ugly (always using BarPointMutualHoldingGame but any valid pos works) , ok for now 
        private static void setInputLabels(Dictionary<PositionType, IBackgammonPositionEvaluator> neuralNetworkDict) {
            foreach (var (positionType, nn) in neuralNetworkDict)
            {
                if (nn is NeuralNetworkPositionEvaluator neuralNetworkEvaluator)
                {
                    var (_, labels) = BoardToNeuralInputsEncoder.EncodeBoardToNeuralInputs(BackgammonPositions.BarPointMutualHoldingGame, positionType);
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
                modelContact = GetLeakyNNUENeuralNetworkModel(encodedInputs.Length, 32, 16,logDir);
                // Handle the case where the network could not be loaded
                
            }
            modelContact.DetfaultfilePath = modelFile;
            return modelContact;
        }

        private static NeuralNetwork NoContactNeuralNetwork(string modelsDir, string logDir, string modelDescription) {
            //var modelDescription = "NoContactNN";
            var modelFile = Path.Combine(modelsDir, modelDescription + ".json");
            var modelIndexNoContact = BoardToNeuralInputsEncoder.MapBoardToModel(NoContactPosition);
            NeuralNetwork? modelNoContact = NeuralNetwork.Load(modelFile);
            if (modelNoContact is null)
            {
                var (encodedInputs, _) = BoardToNeuralInputsEncoder.EncodeBoardToNeuralInputs(NoContactPosition, modelIndexNoContact);
                Console.WriteLine("creating new nn no contact" + encodedInputs.Length);
                modelNoContact = GetNoContactNNUENeuralNetworkModel(encodedInputs.Length, logDir);
            }
            modelNoContact.DetfaultfilePath = modelFile;
            return modelNoContact;
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
            model.AddLayer(6, new SigmoidActivationFunction()); // Assuming 6 outputs for the scores
            return model;
        }
    }
}
