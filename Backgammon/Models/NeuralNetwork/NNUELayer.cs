using Backgammon.Models.NeuralNetwork.ActivationFunctions;
using Serilog;
using System.Diagnostics;

namespace Backgammon.Models.NeuralNetwork
{
    // A Neural Network for effecient update when two input sequences are quite similiar
    // For manu games the position after a player has moved have only small changes
    internal class NNUELayer : Layer
    {
        private float[] _ZPrevious;
        private float[] _inputsPrevious;
        private bool _firstPass;
        private long _calculations;
        private long _skippedCalculations;
        public double CalcVsSkipped => _skippedCalculations / (double)_calculations;
        //private readonly ILogger _nnueLogger; // Custom logger for game simulation

        public NNUELayer(int inputNodes, int outputNodes, IActivationFunction activationFunction, ILogger logger) : base(inputNodes, outputNodes, activationFunction, logger)
        {
            _inputsPrevious = new float[inputNodes];
            _ZPrevious = new float[inputNodes];
            //_ZPreviousContribution = new float[inputNodes, outputNodes];
            _firstPass = true;
            _calculations = 0;
            _skippedCalculations = 0;
            //_nnueLogger = CreateLogger("\\Users\\Dennis\\Source\\Repos\\Backgammon\\Backgammon\\data\\logs\\nnuedebug.log"); // Initialize the custom logger
        }

        private ILogger CreateLogger(string logFilePath)
        {
            return new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        public void ResetFirstPass() {
            _firstPass = true;
        }

        public override float[] FeedForward(float[] inputs)
        {
            _Logger.Information("\nNNUE forward" + inputs.Length);
            _Logger.Information("\nInputs" + string.Join(", ", inputs));
            _Logger.Information($"Biases: {Biases.Length} Firstpass {_firstPass}" );
            var historyIndex = _forwardCount % _HistoryLength;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (_firstPass)
            {
                Z = new float[Biases.Length];
                _inputsPrevious = new float[inputs.Length];
                _ZPrevious = new float[Biases.Length]; // Only this is needed for NNUE optimization
            } else 
            {
                // Copy _ZPrevious to Z to use as the starting point
                Array.Copy(_ZPrevious, Z, _ZPrevious.Length);
            }

            for (int j = 0; j < Biases.Length; j++)
            {
                if (_firstPass)
                {
                    // In the firstPass we do a normal forward pass without NNUE
                    Z[j] = Biases[j];
                    _Logger.Information($"Bias:{j} ,Val: {Z[j]}");
                }
                else {
                    // If the Bias have changed during backprop we must update forward calc
                }

                for (int i = 0; i < inputs.Length; i++)
                {
                    if (_firstPass || inputs[i] != _inputsPrevious[i])
                    {
                        _calculations++;
                        if (!_firstPass)
                        {
                            // Subtract previous contribution and add new one
                            Z[j] -= _inputsPrevious[i] * Weights[i, j];
                            _Logger.Information($"inp{i}:{inputs[i]} * W {Weights[i, j]} ,Z(j): {Z[j]}");
                        }
                        // For the First Pass add new contribution from input 
                        Z[j] += inputs[i] * Weights[i, j];
                        _Logger.Information($"FIRSTP inp{i}:{inputs[i]} * W {Weights[i, j]} ,Z(j): {Z[j]}");
                    }
                    _skippedCalculations++;
                }
                _Logger.Information($"\n Z(j) {j} , {Z[j]}");
                Activations[j] = ActivationFunction.Calculate(Z[j]); // Apply activation function
                _ActivationsHistory[j, historyIndex] = Activations[j];
                _Logger.Information($"\n ACT(j) {j} , {Activations[j]}");
            }

            Array.Copy(Z, _ZPrevious, Z.Length); // Update _ZPrevious for next pass
            Array.Copy(inputs, _inputsPrevious, inputs.Length); // Update _inputsPrevious for next pass

            stopwatch.Stop();
            forwardTime += stopwatch.ElapsedTicks;
            _forwardCount++;
            _firstPass = false; // No longer the first pass
            return Activations;
        }


/*      public override float[] FeedForwardOld(float[] inputs)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            // Ensure 'Z' is initialized with the same length as 'Activations' (and 'Biases')
            if (_firstPass)
            {
                // When network is loaded from json we need to create this 
                Z = new float[Biases.Length];
                _inputsPrevious = new float[inputs.Length];
                //_ZPreviousContribution = new float[inputs.Length, Biases.Length];
            }
            var Z_debug = new float[Biases.Length];
            var Z_sum_debug = new float[inputs.Length, Biases.Length];
            for (int j = 0; j < Biases.Length; j++) // For each neuron in the layer
            {
                Z[j] = Biases[j]; // Start with the bias
                Z_debug[j] = Z[j];
                // The NNUE way is to use the previous Z[j] and make updates when inputs differ
                if (!_firstPass)
                {
                    Z[j] = _ZPrevious[j]; // Start with the bias
                }
                for (int i = 0; i < inputs.Length; i++) // For each input to the neuron
                {
                    
                    var sameZ = Z[j] == Z_debug[j];
                    _logger.Information($" START ZDEBUG is same ? {sameZ}, i {i} ,j {j}");
                    _logger.Information($" Z{Z[j]}, {Z_debug[j]} ");
                    // z_i_j = How much does input[i] add to Z(j), the input for the activation func
                    var z_i_j = inputs[i] * Weights[i, j];

                    // for debugging store data as in a normal feedforward
                    {
                        Z_debug[j] += z_i_j; // Add weighted input
                        Z_sum_debug[i, j] = Z_debug[j];
                    }
                    
                    if (!_firstPass) {
                        //Ensure that ZPrevious is set correct
                        if (inputs[i] == _inputsPrevious[i])
                        {
                            bool isSame = z_i_j == _ZPreviousContribution[i, j];
                            _logger.Information($"i {i}, j {j} ,{isSame} inp {inputs[i]}");
                            _logger.Information($" zij {z_i_j} ,{_ZPreviousContribution[i, j]}");
                        }
                        else {
                            _logger.Information($"i {i}, j {j} ,NOT SAME inp {inputs[i]}");
                            _logger.Information($" zij {z_i_j} ,{_ZPreviousContribution[i, j]}");
                        }
                    }

                    if (_firstPass)
                    {
                        Z[j] += z_i_j; // Add weighted input
                        _ZPreviousContribution[i, j] = z_i_j;
                        _calculations++;
                    }
                    else if (inputs[i] != _inputsPrevious[i])
                    {
                        _logger.Information($"Diff inputs NNUE UPDATE");
                        // since we're not recalculating the whole sum we need the difference between
                        // previous z_i_j and update the Z[j] value
                        var diff = z_i_j - _ZPreviousContribution[i, j];
                        Z[j] += diff; // Add weighted input
                        //Z[j] shuld always be same as Z debug [j]
                        _logger.Information($"ZDEBUG is same ? {Z[j] == Z_debug[j]}");
                        _logger.Information($"i={i} j={j} Z(j) { Z[j]} , {Z_debug[j]}");
                        _ZPreviousContribution[i, j] = z_i_j;
                        _calculations++;
                    }
                    else
                    {
                        //Z[j] += _Z 
                        _logger.Information($"Same inputs NO UPDATE");
                        // if inputs are the same we expect z_i_j to be equal with _ZPreviousContribution[i, j] = z_i_j;
                        //if (z_i_j != _ZPreviousContribution[i, j]) {
                            _logger.Information($"i {i}, j {j}");
                            _logger.Information($"Not equal Z{z_i_j }, {_ZPreviousContribution[i, j]}");
                            _logger.Information($"inputs {inputs[i]} != {_inputsPrevious[i]}");
                        //}
                        _skippedCalculations++;
                    }
                }

                // Now, Z[j] contains the weighted sum for this neuron, before activation
                Activations[j] = ActivationFunction.Calculate(Z[j]); // Apply activation function to Z
            }
            
            for (int i = 0; i < inputs.Length; i++)
            { // For each input to the neuron
                _inputsPrevious[i] = inputs[i];
            } 

            stopwatch.Stop();
            forwardTime += stopwatch.ElapsedTicks;
            _firstPass = false;
            return Activations;
        }*/
    }
}
