using Backgammon.Models.NeuralNetwork.ActivationFunctions;
using Serilog;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Backgammon.Models.NeuralNetwork
{
    public class Layer
    {
        public float[,] Weights { get; set; }
        public float[] Biases { get; set; }
        public float[] Activations { get; set; }

        // Instead of updating the weights directly we sum the weightchanges here for a batchdataset and then use for doing a batchupdate 
        private float[,]? _batchWeightUpdates;
        private bool _useBatchTraining = false;
        private float[]? _batchBiasUpdates;

        // New properties for momentum
        private float[,]? _prevWeightUpdates;
        private float[]? _prevBiasUpdates;
        private readonly float _momentum = 0.3f; // Commonly used momentum value

        // Track the activations to discover dead neuron
        // Temporarily made public (part of JSON) for debugging purpose
        public float[,] _ActivationsHistory { get; set; }
        protected const int _HistoryLength = 20;
        protected int _forwardCount = 0;
        public int ForwardCount => _forwardCount;
        // Outputs before applying the activation function
        protected float[] Z { get; set; }

        internal ILogger _Logger;

        // Adam-specific properties
        private float[,] _mWeights;
        private float[,] _vWeights;
        private float[] _mBiases;
        private float[] _vBiases;
        private int _t = 0; // Time step

        // Adam parameters
        //private float learningRate = 0.001f;
        private float _beta1 = 0.9f;
        private float _beta2 = 0.999f;
        private float _epsilon = 1e-8f;

        public int NumberOfOutputs => Activations.Length;
        public IActivationFunction ActivationFunction { get; set; }

        private readonly Random rand = new();

        protected long forwardTime = 0;
        public long ForwardTime => forwardTime;

        public long _wInc = 1L;
        public long _wDec = 1L;
        public long _wNothing = 1L;
        public double IncvsDec => _wInc / (double)_wDec;
        public double UpdateVsNoUpdate => (_wInc + _wDec) / (double)_wNothing;

        public void ResetForwardTime()
        {
            forwardTime = 0;
        }

        public Layer(int inputNodes, int outputNodes, IActivationFunction activationFunction, ILogger logger)
        {
            _Logger = logger;
            Weights = new float[inputNodes, outputNodes];
            Biases = new float[outputNodes];
            Activations = new float[outputNodes];
            Z = new float[outputNodes];
            ActivationFunction = activationFunction;
            _ActivationsHistory = new float[outputNodes, _HistoryLength];

            InitializeMatrixXavier(Weights);
            InitializeArray(Biases);

            _mWeights = new float[Weights.GetLength(0), Weights.GetLength(1)];
            _vWeights = new float[Weights.GetLength(0), Weights.GetLength(1)];
            _mBiases = new float[Biases.Length];
            _vBiases = new float[Biases.Length];
            //InitializeAdamParameters();
        }

        public void initBatchTraining()
        {
            _useBatchTraining = true;
            // Initialize or reset batch updates; do not clear momentum terms here
            if (_batchWeightUpdates == null)
            {
                _batchWeightUpdates = new float[Weights.GetLength(0), Weights.GetLength(1)];
            }
            else
            {
                Array.Clear(_batchWeightUpdates, 0, _batchWeightUpdates.Length);
            }

            if (_batchBiasUpdates == null)
            {
                _batchBiasUpdates = new float[Weights.GetLength(1)];
            }
            else
            {
                Array.Clear(_batchBiasUpdates, 0, _batchBiasUpdates.Length);
            }

            // Initialize momentum terms if they are null, but do not reset if they already exist
            _prevWeightUpdates ??= new float[Weights.GetLength(0), Weights.GetLength(1)];
            _prevBiasUpdates ??= new float[Biases.Length];
            InitAdam();
        }

        private void InitAdam()
        {
            if (_mWeights == null || _mWeights.Length == 0 || _mWeights.GetLength(0) != Weights.GetLength(0) || _mWeights.GetLength(1) != Weights.GetLength(1))
            {
                // Initialize mWeights and vWeights based on current Weights dimensions
                _mWeights = new float[Weights.GetLength(0), Weights.GetLength(1)];
                _vWeights = new float[Weights.GetLength(0), Weights.GetLength(1)];
            }

            if (_mBiases == null || _mBiases.Length != Biases.Length)
            {
                // Initialize mBiases and vBiases based on current Biases dimensions
                _mBiases = new float[Biases.Length];
                _vBiases = new float[Biases.Length];
            }

            // Initialize matrices to zeros or appropriate starting values as needed
        }

        public bool compare(Layer layer)
        {
            bool isSame = true;
            for (int i = 0; i < Weights.GetLength(0); i++)
            {
                for (int j = 0; j < Weights.GetLength(1); j++)
                {
                    // Assign weights using Xavier initialization (uniform distribution)

                    if (Weights[i, j] != layer.Weights[i, j])
                    {
                        isSame = false;
                        Console.WriteLine($"W {i},  {j} , {Weights[i, j]} != {layer.Weights[i, j]}");
                    }
                }
            }

            for (int i = 0; i < Biases.Length; i++)
            {
                if (Biases[i] != layer.Biases[i])
                {
                    isSame = false;
                    Console.WriteLine($" {i} , {Biases[i]} != {layer.Biases[i]}");
                }
            }

            for (int i = 0; i < layer._ActivationsHistory.GetLength(0); i++)
            {
                for (int j = 0; j < layer._ActivationsHistory.GetLength(1); j++)
                {
                    if (_ActivationsHistory[i, j] != layer._ActivationsHistory[i, j])
                    {
                        isSame = false;
                        Console.WriteLine($"ActHist {i},  {j} , {Weights[i, j]} != {layer.Weights[i, j]}");
                    }
                }
            }
            return isSame;
        }

        public float[] SumOfWeightsForEachInput()
        {
            float[] inputWeightSum = new float[Weights.GetLength(0)];
            for (int i = 0; i < inputWeightSum.Length; i++)
            {
                for (int j = 0; j < NumberOfOutputs; j++)
                {
                    inputWeightSum[i] += Math.Abs(Weights[i, j]);
                }
            }
            return inputWeightSum;
        }

        private void InitializeMatrixXavier(float[,] matrix)
        {
            int nIn = matrix.GetLength(0); // Number of input neurons
            int nOut = matrix.GetLength(1); // Number of output neurons
            float limit = (float)Math.Sqrt(6.0 / (nIn + nOut)); // Xavier/Glorot initialization limit

            for (int i = 0; i < nIn; i++)
            {
                for (int j = 0; j < nOut; j++)
                {
                    // Assign weights using Xavier initialization (uniform distribution)
                    matrix[i, j] = (float)(rand.NextDouble() * 2 * limit - limit);
                }
            }
        }

        private void InitializeArray(float[] array)
        {
            // Initializing biases to zeros is a common practice and works well with Xavier initialization
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = 0f;
            }
        }

        private void InitializeMatrix(float[,] matrix)
        {
            for (int i = 0; i < matrix.GetLength(0); i++)
                for (int j = 0; j < matrix.GetLength(1); j++)
                    matrix[i, j] = (float)(rand.NextDouble() * 0.1 - 0.05);
        }

        public virtual float[] FeedForward(float[] inputs) {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var activations = FeedForwardAVX(inputs);
            stopwatch.Stop();
            forwardTime += stopwatch.ElapsedTicks;
            return activations;
        }

        

        public virtual float[] FeedForwardAVX(float[] inputs)
        {
            int inputLength = inputs.Length;
            int neuronCount = Biases.Length;
            float[] z = new float[neuronCount]; // This array will be used in backpropagation
            float[] activations = new float[neuronCount]; // Local activations array to be returned
            var historyIndex = _forwardCount % _HistoryLength;

            // Parallelize the loop over neurons
            Parallel.For(0, neuronCount, j =>
            {
                float[] weightsForNeuron = GetWeightsForNeuron(j);

                // Choose AVX level based on CPU support
                if (Avx2.IsSupported)
                {
                    ProcessWithAVX2(inputs, weightsForNeuron, ref z[j], inputLength);
                }
                else if (Avx.IsSupported)
                {
                    ProcessWithAVX(inputs, weightsForNeuron, ref z[j], inputLength);
                }
                else
                {
                    ProcessWithoutAVX(inputs, weightsForNeuron, ref z[j], inputLength);
                }

                z[j] += Biases[j]; // Ensure bias is added
                activations[j] = ActivationFunction.Calculate(z[j]); // Compute activation
                Activations[j] = activations[j]; // Update class-level Activations for debugging
                _ActivationsHistory[j, historyIndex] = Activations[j]; // Store history for analysis
            });

            Z = z; // Update class-level Z for backpropagation use
            _forwardCount++;
            return activations; // Return local activations array
        }

        private unsafe void ProcessWithAVX2(float[] inputs, float[] weights, ref float z, int inputLength)
        {
            Vector256<float> zVector = Vector256<float>.Zero;
            int vectorSize = Vector256<float>.Count;
            int i = 0;

            fixed (float* pInputs = inputs, pWeights = weights)
            {
                for (; i <= inputLength - vectorSize; i += vectorSize)
                {
                    var inputVector = Avx.LoadVector256(pInputs + i);
                    var weightsVector = Avx.LoadVector256(pWeights + i);
                    zVector = Avx.Add(zVector, Avx.Multiply(inputVector, weightsVector));
                }

                // Process remaining elements
                float sum = 0;
                for (int j = 0; j < vectorSize; j++)
                {
                    sum += zVector.GetElement(j);
                }

                for (; i < inputLength; i++)
                {
                    sum += inputs[i] * weights[i];
                }

                z = sum;
            }
        }

        /*private unsafe void ProcessWithAVX2(float[] inputs, float[] weights, ref float z, int inputLength)
        {
            Vector256<float> zVector = Vector256<float>.Zero;
            int i = 0;
            fixed (float* pInputs = inputs, pWeights = weights)
            {
                for (; i <= inputLength - Vector256<float>.Count; i += Vector256<float>.Count)
                {
                    var inputVector = Avx.LoadVector256(pInputs + i);
                    var weightsVector = Avx.LoadVector256(pWeights + i);
                    zVector = Avx.Add(zVector, Avx.Multiply(inputVector, weightsVector));
                }
            }
            float sum = 0;
            for (int k = 0; k < Vector256<float>.Count; k++)
            {
                sum += zVector.GetElement(k);
            }
            for (; i < inputLength; i++)
            {
                sum += inputs[i] * weights[i];
            }
            z = sum;
        }*/

        private unsafe void ProcessWithAVX(float[] inputs, float[] weights, ref float z, int inputLength)
        {
            Vector256<float> zVector = Vector256<float>.Zero;
            int i = 0;
            fixed (float* pInputs = inputs, pWeights = weights)
            {
                for (; i <= inputLength - Vector256<float>.Count; i += Vector256<float>.Count)
                {
                    var inputVector = Avx.LoadVector256(pInputs + i);
                    var weightsVector = Avx.LoadVector256(pWeights + i);
                    zVector = Avx.Add(zVector, Avx.Multiply(inputVector, weightsVector));
                }
            }
            float sum = 0;
            for (int k = 0; k < Vector256<float>.Count; k++)
            {
                sum += zVector.GetElement(k);
            }
            for (; i < inputLength; i++)
            {
                sum += inputs[i] * weights[i];
            }
            z = sum;
        }

        private void ProcessWithoutAVX(float[] inputs, float[] weights, ref float z, int inputLength)
        {
            for (int i = 0; i < inputLength; i++)
            {
                z += inputs[i] * weights[i];
            }
        }

        private float[] GetWeightsForNeuron(int neuronIndex)
        {
            float[] weightsForNeuron = new float[Weights.GetLength(0)];
            for (int i = 0; i < weightsForNeuron.Length; i++)
            {
                weightsForNeuron[i] = Weights[i, neuronIndex];
            }
            return weightsForNeuron;
        }
        public virtual float[] FeedForwardOld(float[] inputs)
        {
            //_Logger.Information("\nforward" + inputs.Length);
            //_Logger.Information("\nInputs" + string.Join(", ", inputs));
            var historyIndex = _forwardCount % _HistoryLength;
            //_ActivationsHistory ??= new float[Biases.Length, _HistoryLength];
            //_ActivationsHistory ??= new float[Biases.Length, _HistoryLength];
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            // Ensure 'Z' is initialized with the same length as 'Activations' (and 'Biases')
            Z = new float[Biases.Length];

            for (int j = 0; j < Biases.Length; j++) // For each neuron in the layer
            {
                Z[j] = Biases[j]; // Start with the bias
                //_Logger.Information($"Bias:{j} ,Val: {Z[j]}");
                for (int i = 0; i < inputs.Length; i++) // For each input to the neuron
                {
                    Z[j] += inputs[i] * Weights[i, j]; // Add weighted input
                    //_Logger.Information($"inp{i}:{inputs[i]} * W {Weights[i, j]} ,Z(j): {Z[j]}");
                }
                //_Logger.Information($"\n Z(j) {j} , {Z[j]}");
                // Now, Z[j] contains the weighted sum for this neuron, before activation
                Activations[j] = ActivationFunction.Calculate(Z[j]); // Apply activation function to Z
                //_Logger.Information($"\n ACT(j) {j} , {tempAct}, {Activations[j]}");
                _ActivationsHistory[j, historyIndex] = Activations[j];
                //_Logger.Information($"In:{j} ,H: {historyIndex}, Val: {_ActivationsHistory[j, historyIndex]} ");            
            }
            //_Logger.Information("\nAct" + string.Join(", ", Activations));
            stopwatch.Stop();
            forwardTime += stopwatch.ElapsedTicks;
            _forwardCount++;
            return Activations;
        }

        // Example in C# for analyzing activations in a layer
        public bool IsNeuronDead()
        {
            var testActivations = new float[] { -1f, 0f, 0.2f, 0.5f, 1.0f };
            return testActivations.All(a => a == 0);
        }

        public float[] BackpropagateOld(float[] outputErrors, float learningRate, float[] previousLayerActivations, float lassoLambda)
        {
            float[] deltas = new float[outputErrors.Length];
            //_ActivationsHistory ??= new float[outputErrors.Length, _HistoryLength];
            float deltaClipValue = 1.0f; // Example threshold

            // Calculate delta for each neuron (error * derivative of activation function)
            for (int i = 0; i < outputErrors.Length; i++)
            {
                deltas[i] = outputErrors[i] * ActivationFunction.CalculateDerivative(Z[i]);
                if (float.IsNaN(deltas[i]) || float.IsInfinity(deltas[i]))
                {
                    _Logger.Information($"output errors {outputErrors[i]}");
                    _Logger.Information($"deltas i = {i} delta i {deltas[i]} , nr outputs {deltas.Length} ");
                    throw new InvalidOperationException("NaN or Infinity detected in deltas during backpropagation");
                }

                if (Math.Abs(deltas[i]) > deltaClipValue)
                {
                    _Logger.Information($"output errors {outputErrors[i]}");
                    _Logger.Information($"clip extre, i {i} delta i {deltas[i]} , {deltas.Length} ");
                    if (deltas[i] > deltaClipValue)
                        deltas[i] = deltaClipValue;
                    else if (deltas[i] < -deltaClipValue)
                        deltas[i] = -deltaClipValue;
                }
            }

            // Prepare to calculate errors for the previous layer
            float[] previousLayerErrors = new float[previousLayerActivations.Length];

            // Update weights, biases, and calculate errors for the previous layer
            for (int i = 0; i < Weights.GetLength(0); i++)
            {
                for (int j = 0; j < Weights.GetLength(1); j++)
                {
                    // Lasso regularization adjustment
                    float lassoAdjustment = lassoLambda * (Weights[i, j] > 0 ? 1 : (Weights[i, j] < 0 ? -1 : 0));
                    // Adjust weights by delta * input from previous layer and Lasso regularization
                    var pre = Math.Abs(Weights[i, j]);
                    //Console.WriteLine($"i {i} ,prevLayertActL {previousLayerActivations.Length}" );
                    //Console.WriteLine($"j {j} , deltaL {deltas.Length}");
                    //var delta = deltas[j];
                    //var prev = previousLayerActivations[i];
                    var wUpdate = learningRate * (deltas[j] * previousLayerActivations[i] + lassoAdjustment);
                    if (_useBatchTraining)
                    {
                        _batchWeightUpdates[i, j] += wUpdate;
                    }
                    else
                    {
                        Weights[i, j] -= wUpdate;
                    }
                    if (Math.Abs(Weights[i, j]) > pre)
                    {
                        _wInc++;
                    }
                    else if (Math.Abs(Weights[i, j]) < pre)
                    {
                        _wDec++;
                    }
                    else
                    {
                        _wNothing++;
                    }

                    // Console.WriteLine("W")
                    /*// Adjust weights by delta * input from previous layer
                    Weights[i, j] -= learningRate * deltas[j] * previousLayerActivations[i];*/

                    // Accumulate the error for the previous layer's neuron 'i'
                    previousLayerErrors[i] += deltas[j] * Weights[i, j];
                }
            }

            for (int i = 0; i < Biases.Length; i++)
            {
                if (_useBatchTraining)
                {
                    _batchBiasUpdates[i] += learningRate * deltas[i];
                }
                else
                {
                    // Adjust biases by delta
                    Biases[i] -= learningRate * deltas[i];
                }
            }

            // Return the calculated errors for the previous layer
            return previousLayerErrors;
        }

        private float clipValue(float val, float max)
        {
            if (Math.Abs(val) < max)
            {
                return val;
            }
            if (val < 0f)
            {
                return -max;
            }
            return max;
        }

        public void BatchUpdate(int batchSize) {
           AdamBatchUpdate();
        }

        public void BatchUpdateOld(int batchSize)
        {
            if (batchSize <= 0) throw new ArgumentException("Batch size must be positive", nameof(batchSize));
            float learnRate = 0.02f;
            float batchDivider = (float)Math.Sqrt(batchSize)/learnRate;
            float maxWeightUpdate = 0.1f;
            for (int i = 0; i < Weights.GetLength(0); i++)
            {
                for (int j = 0; j < Weights.GetLength(1); j++)
                {
                    // Calculate the average update by dividing by batchSize
                    // Calculate the average update and apply momentum
                    float averageWeightUpdate = (_batchWeightUpdates[i, j] / batchDivider) + _momentum * _prevWeightUpdates[i, j];
                    //_Logger.Information($"WUPRE, i {i},  j{j} , val {Math.Round(averageWeightUpdate, 3)}");
                    averageWeightUpdate = clipValue(averageWeightUpdate, maxWeightUpdate);
                    _Logger.Information($"WU, i {i},  j{j} , val {Math.Round(averageWeightUpdate, 5)}");
                    Weights[i, j] -= averageWeightUpdate;
                    _prevWeightUpdates[i, j] = averageWeightUpdate; // Store this update for next iteration's momentum
                }
            }

            for (int biasCount = 0; biasCount < Biases.Length; biasCount++)
            {
                // Calculate the average bias update and apply momentum
                float averageBiasUpdate = (_batchBiasUpdates[biasCount] / batchDivider) + _momentum * _prevBiasUpdates[biasCount];
                averageBiasUpdate = clipValue(averageBiasUpdate, maxWeightUpdate);
                Biases[biasCount] -= averageBiasUpdate;
                _prevBiasUpdates[biasCount] = averageBiasUpdate; // Store this update for next iteration's momentum
            }
        }

        private float FindMaxGradient(float[,] gradients)
        {
            float maxGradient = 0f;
            for (int i = 0; i < gradients.GetLength(0); i++)
            {
                for (int j = 0; j < gradients.GetLength(1); j++)
                {
                    float absGradient = Math.Abs(gradients[i, j]);
                    if (absGradient > maxGradient)
                    {
                        maxGradient = absGradient;
                    }
                }
            }
            return maxGradient;
        }

        public void AdamBatchUpdate(float learningRate = 0.0001f, float beta1 = 0.85f, float beta2 = 0.999f, float epsilon = 1e-8f)
        {
            if (_batchWeightUpdates == null || _batchBiasUpdates == null) throw new InvalidOperationException("Batch updates must be computed before calling AdamBatchUpdate");

            /*// Initialize moment vectors and timestep if not already done
            if (_m == null)
            {
                _m = new float[Weights.GetLength(0), Weights.GetLength(1)];
                _v = new float[Weights.GetLength(0), Weights.GetLength(1)];
                _mBias = new float[Biases.Length];
                _vBias = new float[Biases.Length];
                _t = 0;
            }*/

            // Increment the time step
            _t++;

            // Update weights
            for (int i = 0; i < Weights.GetLength(0); i++)
            {
                for (int j = 0; j < Weights.GetLength(1); j++)
                {
                    // Compute the average gradient
                    float grad = _batchWeightUpdates[i, j];

                    // Compute moving averages of the gradients and the squared gradients
                    _mWeights[i, j] = beta1 * _mWeights[i, j] + (1 - beta1) * grad;
                    _vWeights[i, j] = beta2 * _vWeights[i, j] + (1 - beta2) * grad * grad;

                    // Compute bias-corrected estimates
                    float mHat = _mWeights[i, j] / (1 - (float)Math.Pow(beta1, _t));
                    float vHat = _vWeights[i, j] / (1 - (float)Math.Pow(beta2, _t));

                    // Update weights
                    Weights[i, j] -= learningRate * mHat / (float)(Math.Sqrt(vHat) + epsilon);
                }
            }

            // Update biases
            for (int biasCount = 0; biasCount < Biases.Length; biasCount++)
            {
                // Compute the average gradient
                float grad = _batchBiasUpdates[biasCount];

                // Compute moving averages of the gradients and the squared gradients for biases
                _mBiases[biasCount] = beta1 * _mBiases[biasCount] + (1 - beta1) * grad;
                _vBiases[biasCount] = beta2 * _vBiases[biasCount] + (1 - beta2) * grad * grad;

                // Compute bias-corrected estimates
                float mHat = _mBiases[biasCount] / (1 - (float)Math.Pow(beta1, _t));
                float vHat = _vBiases[biasCount] / (1 - (float)Math.Pow(beta2, _t));

                // Update biases
                Biases[biasCount] -= learningRate * mHat / (float)(Math.Sqrt(vHat) + epsilon);
            }
        }

        public void BatchUpdateAdam2(int batchSize)
        {
            if (batchSize <= 0) throw new ArgumentException("Batch size must be positive", nameof(batchSize));

            // Update timestep
            _t++;
            float learningRate = 0.1f; // Fixed learning rate
            float learningRateT = learningRate * (float)Math.Sqrt(1 - Math.Pow(_beta2, _t)) / (float)(1 - Math.Pow(_beta1, _t));

            // Find the maximum gradient for scaling
            var maxWeightGradient = FindMaxGradient(_batchWeightUpdates);
            //var maxBiasGradient = FindMaxGradient(_batchBiasUpdates); // Retain if using for biases
            var desiredMaxScaling = 0.8f; // Maximum scaling factor

            var weightScaling = desiredMaxScaling / maxWeightGradient;
            var biasScaling = desiredMaxScaling / maxWeightGradient; // Use same for starting , Separate scaling for biases if desired

            for (int i = 0; i < Weights.GetLength(0); i++)
            {
                for (int j = 0; j < Weights.GetLength(1); j++)
                {
                    // Update first and second moments
                    _mWeights[i, j] = _beta1 * _mWeights[i, j] + (1 - _beta1) * _batchWeightUpdates[i, j];
                    _vWeights[i, j] = _beta2 * _vWeights[i, j] + (1 - _beta2) * (float)Math.Pow(_batchWeightUpdates[i, j], 2);

                    // Bias-corrected moment estimates
                    float mHat = _mWeights[i, j] / (1 - (float)Math.Pow(_beta1, _t));
                    float vHat = _vWeights[i, j] / (1 - (float)Math.Pow(_beta2, _t));

                    // Update weights with scaled gradients
                    float updateValue = weightScaling * learningRateT * mHat / ((float)Math.Sqrt(vHat) + _epsilon);
                    Weights[i, j] -= updateValue;
                }
            }

            for (int i = 0; i < Biases.Length; i++)
            {
                // Update first and second moments for biases
                _mBiases[i] = _beta1 * _mBiases[i] + (1 - _beta1) * _batchBiasUpdates[i];
                _vBiases[i] = _beta2 * _vBiases[i] + (1 - _beta2) * (float)Math.Pow(_batchBiasUpdates[i], 2);

                // Bias-corrected moment estimates
                float mHatBias = _mBiases[i] / (1 - (float)Math.Pow(_beta1, _t));
                float vHatBias = _vBiases[i] / (1 - (float)Math.Pow(_beta2, _t));

                // Update biases with scaled gradients
                float updateBiasValue = biasScaling * learningRateT * mHatBias / ((float)Math.Sqrt(vHatBias) + _epsilon);
                Biases[i] -= updateBiasValue;
            }
        }

        private float FindMaxGradient(float[] gradients)
        {
            float maxGradient = gradients.Max(x => Math.Abs(x));
            return maxGradient;
        }
        public virtual float[] FeedForwardNoAvx(float[] inputs)
        {
            int inputLength = inputs.Length;
            int neuronCount = Biases.Length;
            float[] z = new float[neuronCount]; // This array will be used in backpropagation
            float[] activations = new float[neuronCount]; // Local activations array to be returned
            var historyIndex = _forwardCount % _HistoryLength;

            // Initialize Z with biases
            Array.Copy(Biases, z, neuronCount);

            Parallel.For(0, neuronCount, j =>
            {
                float[] weightsForNeuron = GetWeightsForNeuron(j);
                Vector<float> zVector = new Vector<float>(0.0f);

                int i = 0;
                for (; i <= inputLength - Vector<float>.Count; i += Vector<float>.Count)
                {
                    var inputVector = new Vector<float>(inputs, i);
                    var weightsVector = new Vector<float>(weightsForNeuron, i);
                    zVector += inputVector * weightsVector;
                }

                // Summation of Vector elements
                for (int k = 0; k < Vector<float>.Count; k++)
                {
                    z[j] += zVector[k];
                }

                // Handle the remaining items in the input array
                for (; i < inputLength; i++)
                {
                    z[j] += inputs[i] * weightsForNeuron[i];
                }

                z[j] += Biases[j]; // Ensure bias is added
                activations[j] = ActivationFunction.Calculate(z[j]); // Compute activation
                Activations[j] = activations[j]; // Update class-level Activations for debugging
                _ActivationsHistory[j, historyIndex] = Activations[j]; // Store history for analysis
            });

            Z = z; // Update class-level Z for backpropagation use
            // Z is not thread safe if we start using thread.. perhaps return a copy or have one Z for each thread
            _forwardCount++;

            return activations; // Return local activations array
        }
    }
}

/*public float[] Backpropagate(float[] outputErrors, float learningRate, float[] previousLayerActivations, float lassoLambda)
        {
            InitAdam();
            t++; // Increment timestep at each call
            
            float[] deltas = new float[outputErrors.Length];

            // Existing delta calculation...
            float deltaClipValue = 1.0f; // Example threshold

            // Calculate delta for each neuron (error * derivative of activation function)
            for (int i = 0; i < outputErrors.Length; i++)
            {
                deltas[i] = outputErrors[i] * ActivationFunction.CalculateDerivative(Z[i]);
                if (float.IsNaN(deltas[i]) || float.IsInfinity(deltas[i]))
                {
                    Console.WriteLine($"output errors {outputErrors[i]}");
                    Console.WriteLine($"deltas i = {i} delta i {deltas[i]} , nr outputs {deltas.Length} ");
                    throw new InvalidOperationException("NaN or Infinity detected in deltas during backpropagation");
                }

                if (Math.Abs(deltas[i]) > deltaClipValue)
                {
                    Console.WriteLine($"output errors {outputErrors[i]}");
                    Console.WriteLine($"clip extre, i {i} delta i {deltas[i]} , {deltas.Length} ");
                    if (deltas[i] > deltaClipValue)
                        deltas[i] = deltaClipValue;
                    else if (deltas[i] < -deltaClipValue)
                        deltas[i] = -deltaClipValue;
                }
            }

            float[] previousLayerErrors = new float[previousLayerActivations.Length];

            for (int i = 0; i < Weights.GetLength(0); i++)
            {
                for (int j = 0; j < Weights.GetLength(1); j++)
                {
                    // Compute gradient
                    float gradient = deltas[j] * previousLayerActivations[i] + lassoLambda * (Weights[i, j] > 0 ? 1 : (Weights[i, j] < 0 ? -1 : 0));

                    // Update first moment vector
                    mWeights[i, j] = beta1 * mWeights[i, j] + (1 - beta1) * gradient;

                    // Update second moment vector
                    vWeights[i, j] = beta2 * vWeights[i, j] + (1 - beta2) * (gradient * gradient);

                    // Compute bias-corrected first moment estimate
                    float mHat = mWeights[i, j] / (float)(1 - Math.Pow(beta1, t));

                    // Compute bias-corrected second moment estimate
                    float vHat = vWeights[i, j] / (float)(1 - Math.Pow(beta2, t));

                    // Update weights
                    Weights[i, j] -= learningRate * mHat / (float)(Math.Sqrt(vHat) + epsilon);

                    // Accumulate error for previous layer
                    previousLayerErrors[i] += deltas[j] * Weights[i, j];
                }
            }

            for (int i = 0; i < Biases.Length; i++)
            {
                // Calculate the gradient for bias; assuming L1 regularization might not directly apply to biases in this context
                float gradient = deltas[i]; // Simple gradient for bias

                // Update first moment vector for biases
                mBiases[i] = beta1 * mBiases[i] + (1 - beta1) * gradient;

                // Update second moment vector for biases
                vBiases[i] = beta2 * vBiases[i] + (1 - beta2) * (gradient * gradient);

                // Compute bias-corrected first moment estimate for biases
                float mHatBiases = mBiases[i] / (float)(1 - Math.Pow(beta1, t));

                // Compute bias-corrected second moment estimate for biases
                float vHatBiases = vBiases[i] / (float)(1 - Math.Pow(beta2, t));

                // Update biases
                Biases[i] -= learningRate * mHatBiases / (float)(Math.Sqrt(vHatBiases) + epsilon);
            }


            return previousLayerErrors;
        }*/
