using Backgammon.Models.NeuralNetwork.ActivationFunctions;
using Serilog;
using System.ComponentModel.Design.Serialization;
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
        // private float learningRate = 0.001f;
        private float _beta1 = 0.9f;
        private float _beta2 = 0.999f;
        private float _epsilon = 1e-8f;

        public int NumberOfOutputs => Activations.Length;
        public IActivationFunction ActivationFunction { get; set; }

        private readonly Random rand = new();

        protected long forwardTime = 0;
        private long _forwardTimeNNUE = 0;
        public long ForwardTime => forwardTime;
        public long ForwardTimeNNUE => _forwardTimeNNUE;
        public long _wInc = 1L;
        public long _wDec = 1L;
        public long _wNothing = 1L;
        public double IncvsDec => _wInc / (double)_wDec;
        public double UpdateVsNoUpdate => (_wInc + _wDec) / (double)_wNothing;

        // For NNUE
        public bool EnableNNUE;
        // First forward pass should be a normal feedforward, also after any weight changes (backprop) we need a normal feedforward again
        private bool _NNUEIsForwardReady = false;
        private float[] _inputsPrevious;
        private float[] _ZPrevious;
        private long _calculations;
        private long _skippedCalculations;
        private bool _firstForwardPass = true;
        private float[,] Z_History;
        private const int Z_History_length=3;
        private int Z_History_index=0;

        public bool FirstForwardPass => _firstForwardPass;
        public void ResetForwardTime()
        {
            forwardTime = 0;
        }

        public Layer(int inputNodes, int outputNodes, IActivationFunction activationFunction, ILogger logger, bool enableNNUE)
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

            // NNUE INIT
            _inputsPrevious = new float[inputNodes];
            _ZPrevious = new float[outputNodes];
            EnableNNUE = enableNNUE;
            _NNUEIsForwardReady = false;
        }

        //When adding inputs reset all related data so we treat the layer as a new one except we keep the old weights
        public void IncreaseInputSize(int newInputSize) {
            var increasedInputSizeWeights = new float[newInputSize, Weights.GetLength(1)];
            InitializeMatrixXavier(increasedInputSizeWeights);
            for (int i = 0; i < Weights.GetLength(0); i++)
            {
                for (int j = 0; j < Weights.GetLength(1); j++)
                {
                    increasedInputSizeWeights[i,j] = Weights[i,j];
                }
            }
            Weights = increasedInputSizeWeights;

            // Mark the layer as needing a reset for the forward pass
            _firstForwardPass = true;
            _NNUEIsForwardReady = false;
        }

        public Layer Clone()
        {
            Layer clone = new Layer(Weights.GetLength(0), Weights.GetLength(1), ActivationFunction, _Logger, EnableNNUE)
            {
                Weights = (float[,])Weights.Clone(),
                Biases = (float[])Biases.Clone(),
                Activations = (float[])Activations.Clone(),
                _batchWeightUpdates = _batchWeightUpdates != null ? (float[,])_batchWeightUpdates.Clone() : null,
                _useBatchTraining = _useBatchTraining,
                _batchBiasUpdates = _batchBiasUpdates != null ? (float[])_batchBiasUpdates.Clone() : null,
                _prevWeightUpdates = _prevWeightUpdates != null ? (float[,])_prevWeightUpdates.Clone() : null,
                _prevBiasUpdates = _prevBiasUpdates != null ? (float[])_prevBiasUpdates.Clone() : null,
                _ActivationsHistory = (float[,])_ActivationsHistory.Clone(),
                Z = (float[])Z.Clone(),
                _mWeights = (float[,])_mWeights.Clone(),
                _vWeights = (float[,])_vWeights.Clone(),
                _mBiases = (float[])_mBiases.Clone(),
                _vBiases = (float[])_vBiases.Clone(),
                _t = _t,
                _beta1 = _beta1,
                _beta2 = _beta2,
                _epsilon = _epsilon,
                forwardTime = forwardTime,
                _forwardTimeNNUE = _forwardTimeNNUE,
                _wInc = _wInc,
                _wDec = _wDec,
                _wNothing = _wNothing,
                _inputsPrevious = (float[])_inputsPrevious.Clone(),
                _ZPrevious = (float[])_ZPrevious.Clone(),
                _calculations = _calculations,
                _skippedCalculations = _skippedCalculations,
                _firstForwardPass = _firstForwardPass,
                EnableNNUE = EnableNNUE
            };

            return clone;
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

            // Compare Weights
            for (int i = 0; i < Weights.GetLength(0); i++)
            {
                for (int j = 0; j < Weights.GetLength(1); j++)
                {
                    if (Weights[i, j] != layer.Weights[i, j])
                    {
                        isSame = false;
                        Console.WriteLine($"Weights differ at [{i}, {j}]: {Weights[i, j]} != {layer.Weights[i, j]}");
                    }
                }
            }

            // Compare Biases
            for (int i = 0; i < Biases.Length; i++)
            {
                if (Biases[i] != layer.Biases[i])
                {
                    isSame = false;
                    Console.WriteLine($"Biases differ at [{i}]: {Biases[i]} != {layer.Biases[i]}");
                }
            }

            // Compare Activations
            for (int i = 0; i < Activations.Length; i++)
            {
                if (Math.Abs(Activations[i] - layer.Activations[i]) > 0.001f)
                {
                    isSame = false;
                    Console.WriteLine($"Activations differ at [{i}]: {Activations[i]} != {layer.Activations[i]}");
                }
            }

            // Compare Batch Weight Updates
            if (_batchWeightUpdates != null && layer._batchWeightUpdates != null)
            {
                for (int i = 0; i < _batchWeightUpdates.GetLength(0); i++)
                {
                    for (int j = 0; j < _batchWeightUpdates.GetLength(1); j++)
                    {
                        if (_batchWeightUpdates[i, j] != layer._batchWeightUpdates[i, j])
                        {
                            isSame = false;
                            Console.WriteLine($"Batch Weight Updates differ at [{i}, {j}]: {_batchWeightUpdates[i, j]} != {layer._batchWeightUpdates[i, j]}");
                        }
                    }
                }
            }

            // Compare Batch Bias Updates
            if (_batchBiasUpdates != null && layer._batchBiasUpdates != null)
            {
                for (int i = 0; i < _batchBiasUpdates.Length; i++)
                {
                    if (_batchBiasUpdates[i] != layer._batchBiasUpdates[i])
                    {
                        isSame = false;
                        Console.WriteLine($"Batch Bias Updates differ at [{i}]: {_batchBiasUpdates[i]} != {layer._batchBiasUpdates[i]}");
                    }
                }
            }

            // Compare Previous Weight Updates
            if (_prevWeightUpdates != null && layer._prevWeightUpdates != null)
            {
                for (int i = 0; i < _prevWeightUpdates.GetLength(0); i++)
                {
                    for (int j = 0; j < _prevWeightUpdates.GetLength(1); j++)
                    {
                        if (_prevWeightUpdates[i, j] != layer._prevWeightUpdates[i, j])
                        {
                            isSame = false;
                            Console.WriteLine($"Previous Weight Updates differ at [{i}, {j}]: {_prevWeightUpdates[i, j]} != {layer._prevWeightUpdates[i, j]}");
                        }
                    }
                }
            }

            // Compare Previous Bias Updates
            if (_prevBiasUpdates != null && layer._prevBiasUpdates != null)
            {
                for (int i = 0; i < _prevBiasUpdates.Length; i++)
                {
                    if (_prevBiasUpdates[i] != layer._prevBiasUpdates[i])
                    {
                        isSame = false;
                        Console.WriteLine($"Previous Bias Updates differ at [{i}]: {_prevBiasUpdates[i]} != {layer._prevBiasUpdates[i]}");
                    }
                }
            }

            /*// Compare Activations History
            for (int i = 0; i < _ActivationsHistory.GetLength(0); i++)
            {
                for (int j = 0; j < _ActivationsHistory.GetLength(1); j++)
                {
                    if (_ActivationsHistory[i, j] != layer._ActivationsHistory[i, j])
                    {
                        isSame = false;
                        Console.WriteLine($"Activations History differ at [{i}, {j}]: {_ActivationsHistory[i, j]} != {layer._ActivationsHistory[i, j]}");
                    }
                }
            }*/

            // Compare Z
            for (int i = 0; i < Z.Length; i++)
            {
                if (Math.Abs(Z[i] - layer.Z[i]) > 0.001)
                {
                    isSame = false;
                    Console.WriteLine($"Z values differ at [{i}]: {Z[i]} != {layer.Z[i]}");
                }
            }

            // Compare mWeights
            for (int i = 0; i < _mWeights.GetLength(0); i++)
            {
                for (int j = 0; j < _mWeights.GetLength(1); j++)
                {
                    if (_mWeights[i, j] != layer._mWeights[i, j])
                    {
                        isSame = false;
                        Console.WriteLine($"mWeights differ at [{i}, {j}]: {_mWeights[i, j]} != {layer._mWeights[i, j]}");
                    }
                }
            }

            // Compare vWeights
            for (int i = 0; i < _vWeights.GetLength(0); i++)
            {
                for (int j = 0; j < _vWeights.GetLength(1); j++)
                {
                    if (_vWeights[i, j] != layer._vWeights[i, j])
                    {
                        isSame = false;
                        Console.WriteLine($"vWeights differ at [{i}, {j}]: {_vWeights[i, j]} != {layer._vWeights[i, j]}");
                    }
                }
            }

            // Compare mBiases
            for (int i = 0; i < _mBiases.Length; i++)
            {
                if (_mBiases[i] != layer._mBiases[i])
                {
                    isSame = false;
                    Console.WriteLine($"mBiases differ at [{i}]: {_mBiases[i]} != {layer._mBiases[i]}");
                }
            }

            // Compare vBiases
            for (int i = 0; i < _vBiases.Length; i++)
            {
                if (_vBiases[i] != layer._vBiases[i])
                {
                    isSame = false;
                    Console.WriteLine($"vBiases differ at [{i}]: {_vBiases[i]} != {layer._vBiases[i]}");
                }
            }

            // Compare time step
            if (_t != layer._t)
            {
                isSame = false;
                Console.WriteLine($"Time step differs: {_t} != {layer._t}");
            }

            /*
            // Compare NNUE properties
            if (_calculations != layer._calculations)
            {
                isSame = false;
                Console.WriteLine($"Calculations differ: {_calculations} != {layer._calculations}");
            }
            if (_skippedCalculations != layer._skippedCalculations)
            {
                isSame = false;
                Console.WriteLine($"Skipped calculations differ: {_skippedCalculations} != {layer._skippedCalculations}");
            }*/

            if (_firstForwardPass != layer._firstForwardPass)
            {
                isSame = false;
                Console.WriteLine($"First forward pass flag differs: {_firstForwardPass} != {layer._firstForwardPass}");
            }

            // Compare NNUE previous inputs
            for (int i = 0; i < _inputsPrevious.Length; i++)
            {
                if (Math.Abs(_inputsPrevious[i] - layer._inputsPrevious[i]) > 0.001f)
                {
                    isSame = false;
                    Console.WriteLine($"Previous inputs differ at [{i}]: {_inputsPrevious[i]} != {layer._inputsPrevious[i]}");
                }
            }

            // Compare NNUE previous Z
            for (int i = 0; i < _ZPrevious.Length; i++)
            {
                if (Math.Abs(_ZPrevious[i]) - Math.Abs(layer._ZPrevious[i]) > 0.0001f)
                {
                    isSame = false;
                    Console.WriteLine($"Previous Z values differ at [{i}]: {_ZPrevious[i]} != {layer._ZPrevious[i]}");
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

        public virtual float[] FeedForward(float[] inputs)
        {
            if (_firstForwardPass || _ZPrevious is null)
            {
                _inputsPrevious = new float[inputs.Length];
                _ZPrevious = new float[Biases.Length];
                Z_History = new float[Z_History_length,Biases.Length];
                Z_History_index = 0;
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            if (EnableNNUE && _NNUEIsForwardReady)
            {
                 FeedForwardNNUE(inputs);
            }
            else
            {
                FeedForwardAVX(inputs);
            }
            
            if (EnableNNUE)
            {
                // Copy Z to Z history
                for (int i = 0; i < Biases.Length; i++)
                {
                    Z_History[Z_History_index, i] = Z[i];
                }
                Z_History_index = (Z_History_index + 1) % Z_History_length; // Update the index in a circular manner
            }

            _NNUEIsForwardReady = true;
            stopwatch.Stop();
            forwardTime += stopwatch.ElapsedTicks;
            _firstForwardPass = false;
            return Activations;
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

            // Temporary code for use and compare with NNUE computation
            Array.Copy(Z, _ZPrevious, Z.Length); // Update _ZPrevious for next pass
            Array.Copy(inputs, _inputsPrevious, inputs.Length); // Update _inputsPrevious for next pass

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

        private static float[] Vector256ToArray(Vector256<float> vector)
        {
            float[] array = new float[Vector256<float>.Count];
            for (int i = 0; i < Vector256<float>.Count; i++)
            {
                array[i] = vector.GetElement(i);
            }
            return array;
        }

        public float[] FeedForwardNNUE(float[] inputs)
        {
            var historyIndex = _forwardCount % _HistoryLength;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Array.Copy(_ZPrevious, Z, _ZPrevious.Length);
            _calculations = 0;
            _skippedCalculations = 0;
            var changedInputs = new bool[inputs.Length];
            int nrOfChangedInputs = 0;
            for (int i = 0; i < inputs.Length; i++)
            {
                changedInputs[i] = _inputsPrevious[i] != inputs[i];
                if (changedInputs[i])
                {
                    nrOfChangedInputs++;
                }
            }
            if (Avx2.IsSupported)
            {
                //var zBefore = (float[])Z.Clone();
                var inputsPreviousBefore = (float[])_inputsPrevious.Clone();
                //var (activation, zSubtract, zAdd, zAddHist) = FeedForwardNNUEOld(inputs, nrOfChangedInputs);
                //if (ForwardCount == 1)
                //{
                    ProcessWithAVX2NNUE(inputs, inputsPreviousBefore, changedInputs);
                //}
                //else
                //{
                    //To use ZHist we must first have 2 forward pass 
                  //  ProcessWithAVX2NNUELock(inputs, inputsPreviousBefore, changedInputs);
                //}
            }
            else
            {
                // To be implemented
                Environment.Exit(0); // Exit if AVX2 is not supported
            }

            Array.Copy(Z, _ZPrevious, Z.Length);
            Array.Copy(inputs, _inputsPrevious, inputs.Length);

            stopwatch.Stop();
            _forwardTimeNNUE += stopwatch.ElapsedTicks;
            _forwardCount++;
            return Activations;
        }

        /*
        private unsafe void ProcessWithAVX2NNUELockZHist(float[] inputs, float[] prevInputs, bool[] changedInputs)
        {
            int vectorSize = Vector256<float>.Count; // The number of floats in a 256-bit vector, which is 8
            List<int> changedIndices = new List<int>();

            // Identify changed indices
            for (int i = 0; i < inputs.Length; i++)
            {
                if (changedInputs[i])
                {
                    changedIndices.Add(i);
                }
            }
            
            // Calculate the correct index in the history to use
            int zHistIndexToUse = (Z_History_index - 2 + Z_History_length) % Z_History_length;
            Console.WriteLine("NNUE ZHist index"+ zHistIndexToUse);
            // Copy Z from the history
            for (int j = 0; j < Biases.Length; j++)
            {
                Z[j] = Z_History[zHistIndexToUse, j];
            }

            //Console.WriteLine("Changed inputs:" + changedIndices.Count + " indexes" + string.Join(",", changedIndices));
            Stopwatch stopwatch = Stopwatch.StartNew();

            Parallel.For(0, Biases.Length, neuron =>
            {
                Vector256<float> zVector = Vector256<float>.Zero; // Initialize the accumulation vector to zero
                int i = 0;

                fixed (float* pInputs = inputs, pInputsPrevious = prevInputs)
                {
                    // Process in chunks of vectorSize
                    for (; i <= changedIndices.Count - vectorSize; i += vectorSize)
                    {
                        float[] changedInputChunk = new float[vectorSize];
                        float[] previousInputChunk = new float[vectorSize];
                        float[] weightsChunk = new float[vectorSize];

                        // Fill the chunks
                        for (int k = 0; k < vectorSize; k++)
                        {
                            int idx = changedIndices[i + k];
                            changedInputChunk[k] = inputs[idx];
                            previousInputChunk[k] = prevInputs[idx];
                            weightsChunk[k] = Weights[idx, neuron]; // Indexing using row idx and column neuron
                        }

                        fixed (float* pChangedInputChunk = changedInputChunk, pPreviousInputChunk = previousInputChunk, pWeightsChunk = weightsChunk)
                        {
                            var inputVector = Avx.LoadVector256(pChangedInputChunk); // Load 8 elements from changed inputs into a SIMD register
                            var previousInputVector = Avx.LoadVector256(pPreviousInputChunk); // Load 8 elements from previous inputs
                            var weightsVector = Avx.LoadVector256(pWeightsChunk); // Load 8 elements from weights

                            _calculations++;

                            // Compute the products separately
                            //var previousProduct = Avx.Multiply(previousInputVector, weightsVector);
                            var inputProduct = Avx.Multiply(inputVector, weightsVector);

                            // Perform the subtraction and addition
                            var zVectorChunk = Vector256<float>.Zero;
                            zVectorChunk = Avx.Add(zVectorChunk, inputProduct); // Add the new contribution

                            // Apply the partial sum to Z at the correct indices
                            for (int k = 0; k < vectorSize; k++)
                            {
                                float zElement = zVectorChunk.GetElement(k);
                                int idx = changedIndices[i + k];
                                Z[neuron] += zElement;
                            }
                        }
                    }

                    // Process any remaining elements that were not part of the full vectorSize chunks
                    for (; i < changedIndices.Count; i++)
                    {
                        int idx = changedIndices[i];
                        _calculations++;
                        Z[neuron] += inputs[idx] * Weights[idx, neuron];
                    }

                    // Apply activation function
                    Activations[neuron] = ActivationFunction.Calculate(Z[neuron]);
                }
            });

            stopwatch.Stop();
            double seconds = (double)stopwatch.ElapsedTicks / Stopwatch.Frequency;
        }*/

        private unsafe void ProcessWithAVX2NNUE(float[] inputs, float[] prevInputs, bool[] changedInputs)
        {
            int vectorSize = Vector256<float>.Count; // The number of floats in a 256-bit vector, which is 8
            List<int> changedIndices = new List<int>();

            // Identify changed indices
            for (int i = 0; i < inputs.Length; i++)
            {
                if (changedInputs[i])
                {
                    changedIndices.Add(i);
                }
            }

            //Console.WriteLine("Changed inputs:" + changedIndices.Count + " indexes" + string.Join(",", changedIndices));
            Stopwatch stopwatch = Stopwatch.StartNew();
            
            // For testing
           

            Parallel.For(0, Biases.Length, neuron =>
            {
                Vector256<float> zVector = Vector256<float>.Zero; // Initialize the accumulation vector to zero
                int i = 0;

                fixed (float* pInputs = inputs, pInputsPrevious = prevInputs)
                {
                    // Process in chunks of vectorSize
                    for (; i <= changedIndices.Count - vectorSize; i += vectorSize)
                    {
                        float[] changedInputChunk = new float[vectorSize];
                        float[] previousInputChunk = new float[vectorSize];
                        float[] weightsChunk = new float[vectorSize];

                        // Fill the chunks
                        for (int k = 0; k < vectorSize; k++)
                        {
                            int idx = changedIndices[i + k];
                            changedInputChunk[k] = inputs[idx];
                            previousInputChunk[k] = prevInputs[idx];
                            weightsChunk[k] = Weights[idx, neuron]; // Indexing using row idx and column neuron
                        }

                        fixed (float* pChangedInputChunk = changedInputChunk, pPreviousInputChunk = previousInputChunk, pWeightsChunk = weightsChunk)
                        {
                            var inputVector = Avx.LoadVector256(pChangedInputChunk); // Load 8 elements from changed inputs into a SIMD register
                            var previousInputVector = Avx.LoadVector256(pPreviousInputChunk); // Load 8 elements from previous inputs
                            var weightsVector = Avx.LoadVector256(pWeightsChunk); // Load 8 elements from weights

                            _calculations++;

                            // Compute the products separately
                            var previousProduct = Avx.Multiply(previousInputVector, weightsVector);
                            var inputProduct = Avx.Multiply(inputVector, weightsVector);

                            // Perform the subtraction and addition
                            var zVectorChunk = Vector256<float>.Zero;
                            zVectorChunk = Avx.Subtract(zVectorChunk, previousProduct); // Subtract the previous contribution
                            zVectorChunk = Avx.Add(zVectorChunk, inputProduct); // Add the new contribution

                            // Apply the partial sum to Z at the correct indices
                            for (int k = 0; k < vectorSize; k++)
                            {
                                float zElement = zVectorChunk.GetElement(k);
                                int idx = changedIndices[i + k];
                                Z[neuron] += zElement;
                            }
                        }
                    }

                    // Process any remaining elements that were not part of the full vectorSize chunks
                    for (; i < changedIndices.Count; i++)
                    {
                        int idx = changedIndices[i];
                        _calculations++;
                        Z[neuron] -= prevInputs[idx] * Weights[idx, neuron];
                        Z[neuron] += inputs[idx] * Weights[idx, neuron];
                    }

                    // Apply activation function
                    Activations[neuron] = ActivationFunction.Calculate(Z[neuron]);
                }
            });

            stopwatch.Stop();
            double seconds = (double)stopwatch.ElapsedTicks / Stopwatch.Frequency;
        }


        /*private unsafe void ProcessWithAVX2NNUENotParallell(float[] inputs, float[] prevInputs, bool[] changedInputs)
        {            
            int vectorSize = Vector256<float>.Count; // The number of floats in a 256-bit vector, which is 8
            List<int> changedIndices = new List<int>();

            // Identify changed indices
            for (int i = 0; i < inputs.Length; i++)
            {
                if (changedInputs[i])
                {
                    changedIndices.Add(i);
                }
            }

            Console.WriteLine("Changed inputs:" + changedIndices.Count + " indexes" + string.Join(",", changedIndices));
            Stopwatch stopwatch = Stopwatch.StartNew();
            // For debugging purposes, we just check one neuron
            for (int neuron = 0; neuron < Biases.Length; neuron++)
            {
                //var z_update_count = 0;
                Vector256<float> zVector = Vector256<float>.Zero; // Initialize the accumulation vector to zero
                int i = 0;

                // List to collect log data for synchronization after parallel execution
                List<string> logData = new List<string>();


                fixed (float* pInputs = inputs, pInputsPrevious = prevInputs)
                {
                    // Process in chunks of vectorSize
                    for (; i <= changedIndices.Count - vectorSize; i += vectorSize)
                    {
                        float[] changedInputChunk = new float[vectorSize];
                        float[] previousInputChunk = new float[vectorSize];
                        float[] weightsChunk = new float[vectorSize];

                        // Fill the chunks
                        for (int k = 0; k < vectorSize; k++)
                        {
                            int idx = changedIndices[i + k];
                            changedInputChunk[k] = inputs[idx];
                            previousInputChunk[k] = prevInputs[idx];
                            weightsChunk[k] = Weights[idx, neuron]; // Indexing using row idx and column j
                            //Console.WriteLine($"Z_subtract, {prevInputs[idx] * Weights[idx, neuron]} , {Z_subtract[idx, neuron]}");
                            //Console.WriteLine($"Z_add, {inputs[idx] * Weights[idx, neuron]} , {Z_add[idx, neuron]}");
                        }

                        fixed (float* pChangedInputChunk = changedInputChunk, pPreviousInputChunk = previousInputChunk, pWeightsChunk = weightsChunk)
                        {
                            var inputVector = Avx.LoadVector256(pChangedInputChunk); // Load 8 elements from changed inputs into a SIMD register
                            var previousInputVector = Avx.LoadVector256(pPreviousInputChunk); // Load 8 elements from previous inputs
                            var weightsVector = Avx.LoadVector256(pWeightsChunk); // Load 8 elements from weights

                            _calculations++;

                            // Compute the products separately
                            var previousProduct = Avx.Multiply(previousInputVector, weightsVector);
                            var inputProduct = Avx.Multiply(inputVector, weightsVector);

                            // Log the products
                            for (int k = 0; k < vectorSize; k++)
                            {
                                int idx = changedIndices[i + k];
                                //logData.Add($"i={i}, k={k}, previousProduct[{k}]: {previousProduct.GetElement(k)}, Z_subtract: {Z_subtract[idx, neuron]}");
                                //logData.Add($"i={i}, k={k}, inputProduct[{k}]: {inputProduct.GetElement(k)}, Z_add: {Z_add[idx, neuron]}");
                            }

                            // Perform the subtraction and addition
                            var zVectorChunk = Vector256<float>.Zero;
                            zVectorChunk = Avx.Subtract(zVectorChunk, previousProduct); // Subtract the previous contribution
                            zVectorChunk = Avx.Add(zVectorChunk, inputProduct); // Add the new contribution

                            // Apply the partial sum to Z_Clone at the correct indices
                            for (int k = 0; k < vectorSize; k++)
                            {
                                float zElement = zVectorChunk.GetElement(k);
                                int idx = changedIndices[i + k];

                                // Applying the update to Z_Clone at the correct index
                                Z[neuron] += zElement;
                            }
                        }
                    }
                    
                    // Process any remaining elements that were not part of the full vectorSize chunks
                    for (; i < changedIndices.Count; i++)
                    {
                        int idx = changedIndices[i];
                        _calculations++;

                        Z[neuron] -= prevInputs[idx] * Weights[idx, neuron];
                        Z[neuron] += inputs[idx] * Weights[idx, neuron];
                    }

                    Activations[neuron] = ActivationFunction.Calculate(Z[neuron]);
                }

                // Output collected log data
                Console.WriteLine(string.Join(Environment.NewLine, logData));
            }
            stopwatch.Stop();
            double seconds = (double)stopwatch.ElapsedTicks / Stopwatch.Frequency;
            Console.WriteLine("NNUE Forwardtime milliseconds" + seconds*1000);
        }*/

        private unsafe void ProcessWithAVX2NNUEDebug(float[] inputs, float[] prevInputs, bool[] changedInputs, float[] Z_Clone, float[,] Z_subtract, float[,] Z_add, float[,] Z_add_Hist)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int vectorSize = Vector256<float>.Count; // The number of floats in a 256-bit vector, which is 8
            List<int> changedIndices = new List<int>();
            //Console.WriteLine("Z Clone[0] before processing: " + Z_Clone[0]);

            // Identify changed indices
            for (int i = 0; i < inputs.Length; i++)
            {
                if (changedInputs[i])
                {
                    changedIndices.Add(i);
                }
            }

            List<float> z_updates = new List<float>();
            List<float> z_updates_expected = new List<float>();
            foreach (var changedInput in changedIndices)
            {
                var diff = Z_add[changedInput, 0] - Z_subtract[changedInput, 0];
                z_updates_expected.Add(diff);
            }

            // Console.WriteLine("Changed inputs:" + changedIndices.Count + " indexes" + string.Join(",", changedIndices));
            // For debugging purposes, we just check one neuron
            for (int neuron = 0; neuron < Biases.Length; neuron++)
            {
                var z_update_count = 0;
                Vector256<float> zVector = Vector256<float>.Zero; // Initialize the accumulation vector to zero
                int i = 0;

                // List to collect log data for synchronization after parallel execution
                List<string> logData = new List<string>();


                fixed (float* pInputs = inputs, pInputsPrevious = prevInputs)
                {
                    // Process in chunks of vectorSize
                    for (; i <= changedIndices.Count - vectorSize; i += vectorSize)
                    {
                        float[] changedInputChunk = new float[vectorSize];
                        float[] previousInputChunk = new float[vectorSize];
                        float[] weightsChunk = new float[vectorSize];

                        // Fill the chunks
                        for (int k = 0; k < vectorSize; k++)
                        {
                            int idx = changedIndices[i + k];
                            changedInputChunk[k] = inputs[idx];
                            previousInputChunk[k] = prevInputs[idx];
                            weightsChunk[k] = Weights[idx, neuron]; // Indexing using row idx and column j
                            //Console.WriteLine($"Z_subtract, {prevInputs[idx] * Weights[idx, neuron]} , {Z_subtract[idx, neuron]}");
                            //Console.WriteLine($"Z_add, {inputs[idx] * Weights[idx, neuron]} , {Z_add[idx, neuron]}");
                        }

                        fixed (float* pChangedInputChunk = changedInputChunk, pPreviousInputChunk = previousInputChunk, pWeightsChunk = weightsChunk)
                        {
                            var inputVector = Avx.LoadVector256(pChangedInputChunk); // Load 8 elements from changed inputs into a SIMD register
                            var previousInputVector = Avx.LoadVector256(pPreviousInputChunk); // Load 8 elements from previous inputs
                            var weightsVector = Avx.LoadVector256(pWeightsChunk); // Load 8 elements from weights

                            _calculations++;

                            // Compute the products separately
                            var previousProduct = Avx.Multiply(previousInputVector, weightsVector);
                            var inputProduct = Avx.Multiply(inputVector, weightsVector);

                            // Log the products
                            for (int k = 0; k < vectorSize; k++)
                            {
                                int idx = changedIndices[i + k];
                                //logData.Add($"i={i}, k={k}, previousProduct[{k}]: {previousProduct.GetElement(k)}, Z_subtract: {Z_subtract[idx, neuron]}");
                                //logData.Add($"i={i}, k={k}, inputProduct[{k}]: {inputProduct.GetElement(k)}, Z_add: {Z_add[idx, neuron]}");
                            }

                            // Perform the subtraction and addition
                            var zVectorChunk = Vector256<float>.Zero;
                            zVectorChunk = Avx.Subtract(zVectorChunk, previousProduct); // Subtract the previous contribution
                            zVectorChunk = Avx.Add(zVectorChunk, inputProduct); // Add the new contribution

                            // Apply the partial sum to Z_Clone at the correct indices
                            for (int k = 0; k < vectorSize; k++)
                            {
                                float zElement = zVectorChunk.GetElement(k);
                                int idx = changedIndices[i + k];

                                /*
                                // Check for unexpected update
                                if (Math.Abs(zElement - z_updates_expected[z_update_count]) > 0.01f)
                                {
                                    logData.Add($"Unexpected update {z_update_count}, {zElement}, {z_updates_expected[z_update_count]}");
                                }*/

                                // Logging the change
                                //logData.Add($"Z Change at index {idx}: {zElement}");

                                // Applying the update to Z_Clone at the correct index
                                Z_Clone[neuron] += zElement;

                                /*
                                // Check for unexpected update
                                if (Math.Abs(Z_Clone[neuron] - Z_add_Hist[z_update_count, neuron]) > 0.01f)
                                {
                                    logData.Add($"Z update count {z_update_count} neuron {neuron}");
                                    logData.Add($"Unexpected Z hist {Z_Clone[neuron]}, {Z_add_Hist[z_update_count, neuron]}");
                                }*/

                                // Increment the z_update_count to move to the next expected update
                                z_update_count++;
                            }

                            /*
                            // Log the zVectorChunk after each operation
                            for (int k = 0; k < vectorSize; k++)
                            {
                                logData.Add($"i={i}, k={k}, zVectorChunk[{k}] after sub/add: {zVectorChunk.GetElement(k)}");
                                z_updates.Add(zVectorChunk.GetElement(k));
                            }*/
                        }
                    }


                    // Print Z_Clone after SIMD operations
                    //logData.Add($"Z_Clone[{neuron}] after SIMD operations: {Z_Clone[neuron]}");
                    //logData.Add($"i {i}");
                    // Process any remaining elements that were not part of the full vectorSize chunks
                    for (; i < changedIndices.Count; i++)
                    {
                        int idx = changedIndices[i];
                        _calculations++;
                        /*
                        logData.Add($"Remaining! i={i}, idx={idx}");
                        logData.Add($"Changed Input: {inputs[idx]}");
                        logData.Add($"Previous Input: {prevInputs[idx]}");
                        logData.Add($"Weight: {Weights[idx, neuron]}");
                        logData.Add($"Z_subtract: {prevInputs[idx] * Weights[idx, neuron]}");
                        logData.Add($"Z_add: {inputs[idx] * Weights[idx, neuron]}");*/

                        Z_Clone[neuron] -= prevInputs[idx] * Weights[idx, neuron];
                        Z_Clone[neuron] += inputs[idx] * Weights[idx, neuron];
                        // Check for unexpected update

                        if (Math.Abs(Z_Clone[neuron] - Z_add_Hist[i, neuron]) > 0.01f)
                        {
                            logData.Add($"Unexpected Z hist {Z_Clone[neuron]}, {Z_add_Hist[i, neuron]}");
                        }

                        //z_update_count++;
                    }

                    // Print Z_Clone after processing remaining elements
                    // logData.Add($"Z_Clone[{neuron}] after processing remaining elements: {Z_Clone[neuron]}");

                    // Check for significant differences and apply activation function
                    if (Math.Abs(Z_Clone[neuron] - Z[neuron]) > 0.001)
                    {
                        logData.Add($"Z Clone {Z_Clone[neuron]} , Z {Z[neuron]}");
                        logData.Add($"Z hist {Z_add_Hist[i - 1, neuron]}");
                        Console.WriteLine(string.Join(Environment.NewLine, logData));
                        Environment.Exit(0);
                    }

                    Activations[neuron] = ActivationFunction.Calculate(Z_Clone[neuron]);
                }

                // Output collected log data
                Console.WriteLine(string.Join(Environment.NewLine, logData));


            }
            stopwatch.Stop();
            Console.WriteLine("NNUE Forwardtime" + stopwatch.ElapsedMilliseconds);

        }

        /// <summary>
        /// Calculate the new activation by finding out which inputs differs from previous inputs
        /// For those inputs that differs we subtract their previous contribution to next layer and add the new contribution
        /// All other inputs remain the same
        /// Z will be updated with new activation level that is used for the Activation Function
        /// After calculating the new activations we update _ZPrevious, _inputsPrevious[i] with current Z, inputs
        /// 
        /// This version is slow but can be valuable for com
        /// </summary>
        /// <param name="inputs"></param>
        /// <returns>The new activation (temporarily Z_subtract Z_add for debug)</returns>
        public (float[], float[,], float[,], float[,]) FeedForwardNNUEOld(float[] inputs, int inputDiffCount)
        {
            _Logger.Information("\nNNUE forward" + inputs.Length);
            _Logger.Information("\nInputs" + string.Join(", ", inputs));
            //_Logger.Information($"Biases: {Biases.Length} Firstpass {_firstPass}");
            var historyIndex = _forwardCount % _HistoryLength;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Array.Copy(_ZPrevious, Z, _ZPrevious.Length);
            var Z_subtract = new float[inputs.Length, Biases.Length];
            var Z_add = new float[inputs.Length, Biases.Length];
            var Z_adding_history = new float[inputDiffCount, Biases.Length];
            _calculations = 0;
            _skippedCalculations = 0;
            Console.WriteLine("Z(0)" + Z[0]);
            for (int j = 0; j < Biases.Length; j++)
            {
                int diffInputCount = 0;

                for (int i = 0; i < inputs.Length; i++)
                {
                    if (inputs[i] != _inputsPrevious[i])
                    {
                        _calculations++;
                        Z[j] -= _inputsPrevious[i] * Weights[i, j];
                        _Logger.Information($"inp{i}:{inputs[i]} * W {Weights[i, j]} ,Z(j): {Z[j]}");
                        Z_subtract[i, j] = _inputsPrevious[i] * Weights[i, j];
                        // For the First Pass add new contribution from input 
                        Z[j] += inputs[i] * Weights[i, j];
                        _Logger.Information($"FIRSTP inp{i}:{inputs[i]} * W {Weights[i, j]} ,Z(j): {Z[j]}");
                        Z_add[i, j] = inputs[i] * Weights[i, j];
                        Z_adding_history[diffInputCount, j] = Z[j];
                        if (j == 0)
                        {
                            Console.WriteLine("Z(j)" + Z[j] + " Diffinputc: " + diffInputCount);
                            Z_adding_history[diffInputCount, j] = Z[j];
                        }
                        diffInputCount++;
                    }
                    else
                    {
                        _skippedCalculations++;
                    }
                }

                // We dont need to add bias since it can't change between two forwardpass
                _Logger.Information($"\n Z(j) {j} , {Z[j]}");
                Activations[j] = ActivationFunction.Calculate(Z[j]); // Apply activation function
                _ActivationsHistory[j, historyIndex] = Activations[j];
                _Logger.Information($"\n ACT(j) {j} , {Activations[j]}");
            }

            Array.Copy(Z, _ZPrevious, Z.Length);                // Update _ZPrevious for next pass
            Array.Copy(inputs, _inputsPrevious, inputs.Length); // Update _inputsPrevious for next pass
            Console.WriteLine("calc" + _calculations + " ,skipped" + _skippedCalculations);

            stopwatch.Stop();
            forwardTime += stopwatch.ElapsedTicks;
            _forwardCount++;
            return (Activations, Z_subtract, Z_add, Z_adding_history);
        }

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

        /*
        private unsafe void ProcessWithAVX2NNUE(float[] inputs)
        {
            int vectorSize = Vector256<float>.Count; // The number of floats in a 256-bit vector, which is 8
            for (int j = 0; j < Biases.Length; j++)
            {
                Vector256<float> zVector = Vector256<float>.Zero; // Initialize the accumulation vector to zero
                int i = 0;

                fixed (float* pInputs = inputs, pWeights = Weights, pZ = Z, pInputsPrevious = _inputsPrevious)
                {
                    for (; i <= inputs.Length - vectorSize; i += vectorSize)
                    {
                        var inputVector = Avx.LoadVector256(pInputs + i); // Load 8 elements from inputs into a SIMD register
                        var previousInputVector = Avx.LoadVector256(pInputsPrevious + i); // Load 8 elements from previous inputs
                        var weightsVector = Avx.LoadVector256(pWeights + i * Biases.Length + j); // Load 8 elements from weights

                        var diff = Avx.CompareNotEqual(inputVector, previousInputVector); // Compare the input vectors
                        int mask = Avx.MoveMask(diff); // Create a bitmask from the comparison result

                        if (mask != 0) // If any elements are different
                        {
                            _calculations++;
                            zVector = Avx.Subtract(zVector, Avx.Multiply(previousInputVector, weightsVector)); // Subtract the previous contribution
                            zVector = Avx.Add(zVector, Avx.Multiply(inputVector, weightsVector)); // Add the new contribution
                        }
                        else
                        {
                            _skippedCalculations++;
                        }
                    }

                    // Sum up the elements of the vector
                    float sum = 0;
                    for (int k = 0; k < vectorSize; k++)
                    {
                        sum += zVector.GetElement(k);
                    }

                    for (; i < inputs.Length; i++)
                    {
                        if (inputs[i] != _inputsPrevious[i])
                        {
                            _calculations++;
                            Z[j] -= _inputsPrevious[i] * Weights[i, j];
                            Z[j] += inputs[i] * Weights[i, j];
                        }
                        else
                        {
                            _skippedCalculations++;
                        }
                    }

                    Z[j] += Biases[j];
                    Activations[j] = ActivationFunction.Calculate(Z[j]);
                    //_ActivationsHistory[j, historyIndex] = Activations[j];
                }
            }
        }*/

        /*
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
        }*/

        // Example in C# for analyzing activations in a layer
        public bool IsNeuronDead()
        {
            var testActivations = new float[] { -1f, 0f, 0.2f, 0.5f, 1.0f };
            return testActivations.All(a => a == 0);
        }

        public float[] Backpropagate(float[] outputErrors, float learningRate, float[] previousLayerActivations, float lassoLambda)
        {
            float[] deltas = new float[outputErrors.Length];
            float deltaClipValue = 1.0f; // Example threshold

            // Calculate delta for each neuron (error * derivative of activation function)
            Parallel.For(0, outputErrors.Length, i =>
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
                    _Logger.Information($"clip extreme, i {i} delta i {deltas[i]} , {deltas.Length} ");
                    deltas[i] = Math.Clamp(deltas[i], -deltaClipValue, deltaClipValue);
                }
            });
            
            // Prepare to calculate errors for the previous layer
            float[] previousLayerErrors = new float[previousLayerActivations.Length];

            if (!_useBatchTraining)
            {
                // Since we will make weight changes dont use nnue next forward pass               
                _NNUEIsForwardReady = false;
            }

            // Update weights, biases, and calculate errors for the previous layer
            Parallel.For(0, Weights.GetLength(0), i =>
            {
                for (int j = 0; j < Weights.GetLength(1); j++)
                {
                    // Lasso regularization adjustment
                    float lassoAdjustment = lassoLambda * (Weights[i, j] > 0 ? 1 : (Weights[i, j] < 0 ? -1 : 0));
                    // Adjust weights by delta * input from previous layer and Lasso regularization
                    var pre = Math.Abs(Weights[i, j]);
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

                    // Accumulate the error for the previous layer's neuron 'i'
                    previousLayerErrors[i] += deltas[j] * Weights[i, j];
                }
            });

            Parallel.For(0, Biases.Length, i =>
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
            });

            // Return the calculated errors for the previous layer
            return previousLayerErrors;
        }

        /*
        public float[] BackpropagateNoParalell(float[] outputErrors, float learningRate, float[] previousLayerActivations, float lassoLambda)
        {
            float[] deltas = new float[outputErrors.Length];
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
                    // Adjust weights by delta * input from previous layer
                    // Weights[i, j] -= learningRate * deltas[j] * previousLayerActivations[i];

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
        }*/

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

        public void BatchUpdate(int batchSize)
        {
            AdamBatchUpdate();
        }

        /*
        public void BatchUpdateOld(int batchSize)
        {
            if (batchSize <= 0) throw new ArgumentException("Batch size must be positive", nameof(batchSize));
            float learnRate = 0.02f;
            float batchDivider = (float)Math.Sqrt(batchSize) / learnRate;
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
        }*/

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
            
            // Since we will make weight changes dont use nnue next forward psss
            _NNUEIsForwardReady = false;


            /* // Initialize moment vectors and timestep if not already done
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
            // Since we will make weight changes dont use nnue next forward psss
            _NNUEIsForwardReady = false;

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

        /*
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
        }*/

        /*
private void CompareNNUEOldAndNew(float[] inputs)
{
    // Save state
    var originalZ = (float[])Z.Clone();
    var originalZPrevious = (float[])_ZPrevious.Clone();
    var originalInputs = (float[])_inputsPrevious.Clone();

    // Run old method
    FeedForwardNNUEOld(inputs);

    // Compare results
    for (int j = 0; j < Biases.Length; j++)
    {
        Console.WriteLine($"Neuron {j}: New Z[j]={Z[j]}, Old Z[j]={originalZ[j]}");
        if (Math.Abs(Z[j] - originalZ[j]) > 1e-5)
        {
            Console.WriteLine($"Discrepancy found at neuron {j}: New Z={Z[j]}, Old Z={originalZ[j]}");
        }
    }

    // Restore state
    Array.Copy(originalZPrevious, _ZPrevious, originalZPrevious.Length);
    Array.Copy(originalInputs, _inputsPrevious, originalInputs.Length);
    Z = originalZ;
}*/

        /*
        private unsafe void ProcessWithAVX2NNUE(float[] inputs, bool[] changedInputs)
        {
            int vectorSize = Vector256<float>.Count; // The number of floats in a 256-bit vector, which is 8
            List<int> changedIndices = new List<int>();
            var zClone = (float[])Z.Clone();

            // Identify changed indices
            for (int i = 0; i < inputs.Length; i++)
            {
                if (changedInputs[i])
                {
                    changedIndices.Add(i);
                }
            }
            Console.WriteLine("Changed inputs count: " + changedIndices.Count);

            for (int j = 0; j < Biases.Length; j++)
            {
                Vector256<float> zVector = Vector256<float>.Zero; // Initialize the accumulation vector to zero
                int i = 0;

                fixed (float* pInputs = inputs, pWeights = Weights, pInputsPrevious = _inputsPrevious)
                {
                    // Process in chunks of vectorSize
                    for (; i <= changedIndices.Count - vectorSize; i += vectorSize)
                    {
                        float[] changedInputChunk = new float[vectorSize];
                        float[] previousInputChunk = new float[vectorSize];
                        float[] weightsChunk = new float[vectorSize];

                        // Fill the chunks
                        for (int k = 0; k < vectorSize; k++)
                        {
                            int idx = changedIndices[i + k];
                            changedInputChunk[k] = inputs[idx];
                            previousInputChunk[k] = _inputsPrevious[idx];
                            weightsChunk[k] = Weights[idx, j];
                        }

                        fixed (float* pChangedInputChunk = changedInputChunk, pPreviousInputChunk = previousInputChunk, pWeightsChunk = weightsChunk)
                        {
                            var inputVector = Avx.LoadVector256(pChangedInputChunk); // Load 8 elements from changed inputs into a SIMD register
                            var previousInputVector = Avx.LoadVector256(pPreviousInputChunk); // Load 8 elements from previous inputs
                            var weightsVector = Avx.LoadVector256(pWeightsChunk); // Load 8 elements from weights
                            if (j == 0)
                            {
                                Console.WriteLine("Input Vector: " + string.Join(", ", Vector256ToArray(inputVector)));
                                Console.WriteLine("Previous Input Vector: " + string.Join(", ", Vector256ToArray(previousInputVector)));
                                Console.WriteLine("Weights Vector: " + string.Join(", ", Vector256ToArray(weightsVector)));
                            }
                            _calculations++;
                            zVector = Avx.Subtract(zVector, Avx.Multiply(previousInputVector, weightsVector)); // Subtract the previous contribution
                            zVector = Avx.Add(zVector, Avx.Multiply(inputVector, weightsVector)); // Add the new contribution
                            if (j == 0)
                            {
                                Console.WriteLine("Z Vector after Subtract/Add: " + string.Join(", ", Vector256ToArray(zVector)));
                            }
                        }
                    }

                    // Apply the partial sum to Z[j]
                    for (int k = 0; k < vectorSize; k++)
                    {
                        Z[j] += zVector.GetElement(k);
                    }
                    if (j == 0)
                    {
                        Console.WriteLine("Z[j] after SIMD accumulation: " + Z[j]);
                    }

                    // Process any remaining elements that were not part of the full vectorSize chunks
                    for (; i < changedIndices.Count; i++)
                    {
                        int idx = changedIndices[i];
                        _calculations++;
                        Z[j] -= _inputsPrevious[idx] * Weights[idx, j];
                        Z[j] += inputs[idx] * Weights[idx, j];
                    }

                    Console.WriteLine("Z[j] after processing remaining elements: " + Z[j]);

                    // Add the bias and apply the activation function
                    Z[j] += Biases[j];
                    Console.WriteLine("Z[j] after adding bias: " + Z[j]);

                    Activations[j] = ActivationFunction.Calculate(Z[j]);
                    Console.WriteLine("Activation[j]: " + Activations[j]);
                }
                // Restore Z
                Z = (float[])zClone.Clone();
            }
        }*/

    }
}