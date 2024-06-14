using Backgammon.Models.NeuralNetwork.ActivationFunctions;
using Backgammon.Util;
using Newtonsoft.Json;
using Serilog;
using System.Diagnostics;

namespace Backgammon.Models.NeuralNetwork
{
    public class NeuralNetwork
    {
        [JsonProperty]
        private readonly List<Layer> Layers = [];
        private float[]? InitialInputs { get; set; }
        private string[]? _inputLabels;
        public string Description { get; set; } = "NeuralNetwork";
        public string LogfilePath { get; set; }
        public string DetfaultfilePath { get; set; } = "";
        public string LogFile => LogfilePath + "\\" + Description + ".log";
        private ILogger _Logger;

        public NeuralNetwork(string logfilePath, string description)
        {
            Layers = [];
            LogfilePath = logfilePath;
            Description = description;
            _Logger = LogManager.CreateLogger(LogFile);
        }

        public void Save() {
            Save(DetfaultfilePath);
        }

        public void SetInputLabels(string[] inputLabels)
        {
            _inputLabels = inputLabels;
        }

        //A little uggly but for now we just want some history
        public void Save(int index)
        {
            Save(DetfaultfilePath+index);
        }

        public void Save(string filePath)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto, // Handle type names for polymorphic object graph
                Formatting = Formatting.Indented         // Make the output JSON easier to read
            };
            string json = JsonConvert.SerializeObject(this, settings);
            File.WriteAllText(filePath, json);
        }

        public static NeuralNetwork? Load(string filePath)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto // Handle type names for polymorphic object graph
            };
            try
            {
                string json = File.ReadAllText(filePath);
                var nn = JsonConvert.DeserializeObject<NeuralNetwork>(json, settings);                
                nn._Logger = LogManager.CreateLogger(nn.LogFile);
                foreach (var layer in nn.Layers)
                {
                    if (layer is NNUELayer)
                    {
                        ((NNUELayer)layer).ResetFirstPass();
                    }
                    layer._Logger = nn._Logger;
                }
                return nn;
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"Error: The file '{filePath}' was not found.{ex.Message}");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error: There was an issue deserializing the file '{filePath}'. {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: An unexpected error occurred while loading the neural network from '{filePath}'. {ex.Message}");
            }

            return null; // Return null if an error occurred
        }

        public void AddFirstLayer(int inputNodes, int outputNodes, IActivationFunction activationFunction)
        {
            var layer = new Layer(inputNodes, outputNodes, activationFunction, _Logger);
            Layers.Add(layer);
        }

        public void AddFirstLayerNNUE(int inputNodes, int outputNodes, IActivationFunction activationFunction)
        {
            var layer = new NNUELayer(inputNodes, outputNodes, activationFunction, _Logger);
            Layers.Add(layer);
        }

        public void AddLayer(int outputNodes, IActivationFunction activationFunction)
        {
            var inputNodes = Layers.Last().NumberOfOutputs;
            var layer = new Layer(inputNodes, outputNodes, activationFunction,_Logger);
            Layers.Add(layer);
        }

        // Activation function application adjusted for clarity
        public float[] FeedForward(float[] inputs)
        { 
            //Console.WriteLine("FF" + Description);
            InitialInputs = inputs; // Store the inputs for later use in backpropagation

            // Directly initialize 'activations' with the input to emphasize 
            // that these are the activations for the first actual layer processing.
            float[] layerActivations = inputs;
            // Debugging code
            var firstLayer = Layers[0];
            if (inputs.Length != firstLayer.Weights.GetLength(0)) {
                throw new InvalidOperationException("inputs doesnt match"+ Description + "in"+ inputs.Length + "W:" +firstLayer.Weights.GetLength(0));
            }

            foreach (var layer in Layers)
            {
                layerActivations = layer.FeedForward(layerActivations);
            }

            float[] output = new float[layerActivations.Length];
            Array.Copy(layerActivations, output, layerActivations.Length);

            return output;
        }

        public float Backpropagate(float[] prediction, float[] targetValues, float learningRate, float lassoLambda)
        {
            // Calculate output errors...
            float[] outputErrors = CalculateOutputErrors(prediction, targetValues);
            for (int i = 0; i < outputErrors.Length; i++) {
                if (float.IsNaN(outputErrors[i]) || float.IsInfinity(outputErrors[i]))
                {
                    _Logger.Information($"BACK output errors {outputErrors[i]}");
                    _Logger.Information($" i = {i} pred i {prediction[i]} , target i {targetValues[i]} ");
                    throw new InvalidOperationException("NaN or Infinity detected in deltas during backpropagation");
                }
            }
            // Calculate MSE for monitoring
            float mse = CalculateMSE(outputErrors);
            if (mse > 1f)
            {
                _Logger.Information("MSE" + mse);
                _Logger.Information("Prediction" + string.Join(", ", prediction));
                _Logger.Information("Target" + string.Join(", ", targetValues));
            }
            float[] previousLayerActivations;
            for (int i = Layers.Count - 1; i >= 0; i--)
            {
                if (i == 0)
                {
                    // Special handling for the first layer, using the original inputs
                    previousLayerActivations = InitialInputs;
                }
                else
                {
                    // For other layers, use the activations from the layer before
                    previousLayerActivations = Layers[i - 1].Activations;
                }
                outputErrors = Layers[i].BackpropagateOld(outputErrors, learningRate, previousLayerActivations, lassoLambda);
            }
            return mse;
        }

        public float BatchUpdate(List<TrainingData> trainingData, int epochs=1)
        {            
            foreach (var layer in Layers) {
                layer.initBatchTraining();
            }
            var mseBatchAverage = 0f;
            for (int epoch = 0; epoch < epochs; epoch++)
            {
                float mseSum = 0;
                foreach (var trainData in trainingData)
                {
                    var prediction = FeedForward(trainData.InputData);
                    mseSum += Backpropagate(prediction, trainData.Target, trainData.LearningRate, 0f);
                }

                foreach (var layer in Layers)
                {
                    layer.BatchUpdate(trainingData.Count);
                }
                var mseSumAverage = mseSum / trainingData.Count();
                mseBatchAverage += mseSumAverage;
                Console.WriteLine("mse" + mseSumAverage);
            }
            
            return mseBatchAverage/epochs;
        }

        private float[] CalculateOutputErrors(float[] prediction, float[] targetValues)
        {
            var outputErrors = new float[prediction.Length];
            // Use MSE Error Function
            for (int i = 0; i < prediction.Length; i++)
                //outputErrors[i] = targetValues[i] - prediction[i];
                outputErrors[i] = prediction[i] - targetValues[i];

            return outputErrors;
        }

        private float CalculateMSE(float[] outputErrors)
        {
            float sumSquaredErrors = 0f;
            for (int i = 0; i < outputErrors.Length; i++)
            {
                sumSquaredErrors += outputErrors[i] * outputErrors[i];
            }
            return sumSquaredErrors / outputErrors.Length;
        }

        public void CheckForwardTime()
        {
            Console.WriteLine($"Checking forward Time, "+ Description);
            var averageTotalMilliSeconds = 0.0;
            foreach (var layer in Layers)
            {
                double seconds = (double)layer.ForwardTime / Stopwatch.Frequency;
                double averageMillis = 1000 * seconds / layer.ForwardCount;
                Console.WriteLine($"Layer forward time: {seconds} sec");
                Console.WriteLine($"Average time: {averageMillis} millisec");
                averageTotalMilliSeconds += averageMillis;
            }

            Console.WriteLine($"Network  average forward time: {averageTotalMilliSeconds} millisec");
        }

        public void CheckDeadInputs()
        {
            var sumOfWeights = Layers[0].SumOfWeightsForEachInput();
            for (int input = 0; input < sumOfWeights.Length; input++)
            {
                if (sumOfWeights[input] < 0.0001)
                {
                    Console.WriteLine($"Bad input {input},  {sumOfWeights[input]} ");
                }
            }
        }

        public void CheckNNUERatio()
        {
            var layer = Layers[0];
            if (layer is NNUELayer)
            {
                Console.WriteLine($"NNUE EF, {((NNUELayer)layer).CalcVsSkipped}");
            }
            else
            {
                Console.WriteLine($"NOT NNUE");
            }
        }

        public void Compare(NeuralNetwork network)
        {
            for (int i = 0; i < Layers.Count; i++)
            {
                Console.WriteLine("comparing layer" + i);
                var l1 = Layers[i];
                var l2 = network.Layers[i];
                var isSame = l1.compare(l2);
                if (!isSame)
                {
                    Console.WriteLine("layer differ!");
                    Environment.Exit(0);
                }
            }
        }


        public void CheckWeightsInc()
        {
            foreach (var layer in Layers)
            {
                Console.WriteLine("Update vs NoUpdate " + layer.UpdateVsNoUpdate);
                //Console.WriteLine("INC" + layer._wInc);
                //Console.WriteLine("Dec" + layer._wDec);
                //Console.WriteLine("Nada" + layer._wNothing);
            }
        }

        public void checkFeatureRelevance(string[] labels) {
            // Assuming that an input has high importace if it weights to other neurons have increased
            var layer = Layers[0];
            var weights = layer.Weights;
            Console.WriteLine("\nChecking feature" + labels.Length);
            var inputSumOfWeights = new float[weights.GetLength(0)];
            for (int input = 0; input < weights.GetLength(0); input++)
            {
                for (int neuron = 0; neuron < weights.GetLength(1); neuron++)
                {
                    inputSumOfWeights[input] += Math.Abs(weights[input, neuron]);
                }
                Console.Write(labels[input] + ": " + input + "wsum: " + Math.Round(inputSumOfWeights[input], 3));
            }
        }

        public void checkMaxFeatureRelevance()
        {
            // Assuming that an input has high importance if its weights to other neurons have increased
            var layer = Layers[0];
            var weights = layer.Weights;
            Console.WriteLine("\nChecking feature Max" + _inputLabels!.Length);
            var inputMaxOfWeights = new float[weights.GetLength(0)];

            for (int input = 0; input < weights.GetLength(0); input++)
            {
                for (int neuron = 0; neuron < weights.GetLength(1); neuron++)
                {
                    if (Math.Abs(weights[input, neuron]) > inputMaxOfWeights[input])
                        inputMaxOfWeights[input] = Math.Abs(weights[input, neuron]);
                }
            }

            // Create a list of tuples to store labels and their corresponding max weights
            var labeledWeights = new List<(string label, float maxWeight)>();

            for (int input = 0; input < _inputLabels.Length; input++)
            {
                labeledWeights.Add((_inputLabels[input], inputMaxOfWeights[input]));
            }

            // Sort the list in descending order based on the max weights
            labeledWeights.Sort((x, y) => y.maxWeight.CompareTo(x.maxWeight));

            // Print the sorted results
            foreach (var (label, maxWeight) in labeledWeights)
            {
                Console.Write($"{label}: Max: {Math.Round(maxWeight, 3)} , ");
            }
        }


        public void CheckActivationHistory(bool debug = false)
        {
            int layerCount = 0;
            foreach (var layer in Layers)
            {
                var w = layer.Weights;
                _Logger.Information("\nACTIVATION HISTORY Layer: " + layerCount + "\nL0: " + layer._ActivationsHistory.GetLength(0) +
                    " \nL1: "+  layer._ActivationsHistory.GetLength(1));
                for (int i = 0; i < layer._ActivationsHistory.GetLength(0); i++)
                {
                    if (debug)
                    {
                        _Logger.Information($" Neuron, {i}");
                    }
                    var activationSum = 0f;
                    for (int j = 0; j < layer._ActivationsHistory.GetLength(1); j++)
                    {
                        activationSum += Math.Abs(layer._ActivationsHistory[i, j]);
                        if (debug)
                        {
                            _Logger.Information($", {layer._ActivationsHistory[i,j]}");
                        }
                    }
                    if (activationSum < 0.001)
                    {
                        _Logger.Information($"Neuron{i}, ActSum= {activationSum} HistLength: {layer._ActivationsHistory.GetLength(1)}");
                        for (int j = 0; j < layer._ActivationsHistory.GetLength(1); j++)
                        {
                            //activationSum += Math.Abs(layer._ActivationsHistory[i, j]);
                            {
                                _Logger.Information($", {layer._ActivationsHistory[i, j]}");
                            }
                        }
                    }
                    if (activationSum< 0.000001f && layer.ForwardCount>5)
                    {
                        _Logger.Information($"Potential DEAD Neuron");
                        //Environment.Exit(0);
                    }
                }
                layerCount++;
            }
        }

        public void ResetForwardTime()
        {
            foreach (var layer in Layers)
            {
                layer.ResetForwardTime();
            }
        }
    }
}
