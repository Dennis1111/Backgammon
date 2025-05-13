using Backgammon.GamePlay;
using Backgammon.Models;
using static Backgammon.Models.Move;

namespace Backgammon.WebApp.Services
{
    public class GameStateService
    {
        public event Action? StateChanged;
        public int[] Board { get; set; } = new int[28]; // 0-23: board points, 24-25: bars, 26-27: bear-off
        public int[] Dice { get; set; } = new int[2]; // Current dice values
        public bool MoveIsComplete { get; set; } = false; // True if all possible checkers have moved

        public int CurrentPlayer { get; set; } = BackgammonBoard.Player1; // We will use Player1 as the human player
        public bool HumanTurn => CurrentPlayer == BackgammonBoard.Player1; // True if it's the human player's turn
        public int ScorePlayer1 { get; set; } = 0;
        public int ScorePlayer2 { get; set; } = 0;
        public bool IsGameOver { get; set; } = false; // True if the game is over
        public string GameOverMessage { get; set; } = "";
        public List<Move> MovesMade { get; set; } = []; // List of moves made in the game
        public bool IsInitialized { get; private set; } = false;

        // Used to track the first used die in the current move
        // public int FirstUsedDice => CurrentMove.Count==0 ? -1: CurrentMove[0].DiceUsed; // Used to track the first used die in the current move
        private List<MoveSnapshot> CurrentMove { get; set; } = [];

        private List<(Move move, int[] board)> ValidMoves = [];
        private static readonly DiceManager _diceManager = new();
        private GameSimulator _gameSimulator = new(); // Instance of the game simulator
        private bool IsFirstRoll => MovesMade.Count == 0; // True if it's the first roll of the game

        private readonly SemaphoreSlim _gameLock = new SemaphoreSlim(1, 1);

        public async void InitializeGame()
        {
            //Fix for Blazor calling OnInitialized() more than once
            //if (IsInitialized)
            //    return;

            Board = new BackgammonBoard().Position;
            //Board = [0, -2, 3, 3, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -4, -4, -4, -1, 0, 0, 0, 7, 0];
            MoveIsComplete = false;
            IsGameOver = false;
            MovesMade = [];
            CurrentMove = [];
            //MoveIsComplete = false;
            Dice = _diceManager.FirstRoll();
            if (Dice[0] > Dice[1])
            {
                CurrentPlayer = BackgammonBoard.Player1;
                // Human begins wait for interaction
                Console.WriteLine($"First roll: {Dice[0]} {Dice[1]} - Player 1 starts");
                NotifyStateChanged();
            }
            else
            {
                Console.WriteLine($"First roll: {Dice[0]} {Dice[1]} - Computer starts");
                CurrentPlayer = BackgammonBoard.Player2;
                await GenerateComputerMove();
            }
            
            PrintBackgammonBoard(Board);
            IsInitialized = true;            
        }

        public async Task<bool> TryMakeCheckerMove(int pointId)
        {
            await _gameLock.WaitAsync();  // Acquire the lock
            try
            {
                if (IsGameOver)
                {
                    Console.WriteLine("Game is over, no moves possible");
                    return false;
                }
                Console.WriteLine($"TryMakeCheckerMove from: {pointId} - CurrentPlayer: {CurrentPlayer} - HumanTurn: {HumanTurn} - MoveIsComplete: {MoveIsComplete}");
                if (!HumanTurn || MoveIsComplete)
                {
                    return false;
                }

                // For human to make a move the point must have a checker on the point
                if (Board[pointId] <= 0)
                {
                    return false;
                }

                if (CurrentMove.Count == 0)
                {
                    ValidMoves = BackgammonBoard.GenerateLegalMovesStatic(Board, Dice[0], Dice[1], CurrentPlayer);
                }

                // This should be change to add an empty move and change player turn though perhaps this function should ony be called when you can make a move
                if (ValidMoves.Count == 0)
                {
                    CurrentPlayer = BackgammonBoard.Player2;
                    MoveIsComplete = true;
                    Console.WriteLine("No moves possible");
                    // Update moves made
                    await GenerateComputerMove();
                    return false;
                }

                var dice = getNextDice();
                Console.WriteLine($"Dice: {Dice[0]} {Dice[1]} - Dice to use: {dice}");
                int moveCheckerTo = pointId - dice;

                // In the first checker move we assume the player want's to move with other dice when the first one can't be used from the selected point
                if (Board[moveCheckerTo] <= -2 && CurrentMove.Count == 0 && Dice[0] != Dice[1])
                {
                    if (Dice[0] == dice)
                    {
                        dice = Dice[1];
                    }
                    else
                    {
                        dice = Dice[0];
                    }
                    moveCheckerTo = pointId - Dice[1];
                }

                bool bearingOff = false;
                if (moveCheckerTo < BackgammonBoard.AcePointP1)
                {
                    moveCheckerTo = BackgammonBoard.BearOffP1;
                    bearingOff = true;
                }

                var candidate = new CheckerMove(pointId, moveCheckerTo);
                bool isValidMove = BackgammonBoard.isValidCheckerMove([.. ValidMoves.Select(item => item.move)], 
                    [.. CurrentMove.Select(item => item.CheckerMove)], 
                    candidate);
                
                if (!isValidMove)
                {
                    return false;
                }
                
                CheckerMove cmv;
                int[] newPos;

                // Should update BackgammonBoard so i only need one function
                if (bearingOff)
                {
                    Console.WriteLine("Bear Off");
                    (cmv, newPos) = BackgammonBoard.BearOff(Board, pointId, CurrentPlayer);
                }
                else
                {
                    Console.WriteLine("Regular move");
                    (cmv, newPos) = BackgammonBoard.MoveChecker(Board, pointId, dice, CurrentPlayer);
                }
                Console.WriteLine($"CheckerMove: {cmv}");
                PrintBackgammonBoard(newPos);
                MoveSnapshot state = new(cmv, Board, dice);
                CurrentMove.Add(state);
                
                Board = newPos;

                if (CurrentMove.Count == ValidMoves[0].move.CheckerMoves.Count)
                {
                    MoveIsComplete = true;
                }

                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                // Handle any exceptions that might occur
                Console.WriteLine($"Error during move: {ex.Message}");
                return false;
            }
            finally { 
                _gameLock.Release();  // Always release the lock
            }
            return true;
        }

        private static void PrintBackgammonBoard(int[] board)
        {
            Console.WriteLine("Backgammon Board:");
            BackgammonBoard temp = new BackgammonBoard();
            temp.Position = board;
            Console.WriteLine(temp.ToString());
        }

        public void RevertLastCheckerMove()
        {
            _gameLock.Wait(); // Acquire the lock synchronously
            try
            {
                MoveSnapshot lastMove = CurrentMove.Last();
                Board = lastMove.BoardBefore;
                CurrentMove.RemoveAt(CurrentMove.Count - 1);
                MoveIsComplete = false;
                NotifyStateChanged();
            }
            finally
            {
                _gameLock.Release();  // Always release the lock
            }
            
        }

        public void CompleteHumanMove()
        {
            _gameLock.Wait(); // Acquire the lock synchronously
            try
            {
                Move move = new Move(Dice[0], Dice[1], CurrentPlayer);
                foreach (var item in CurrentMove)
                {
                    move.AddCheckerMoveClean(item.CheckerMove);
                }
                MovesMade.Add(move);
                
                Console.WriteLine($"CompleteHumanMove - CurrentPlayer: {CurrentPlayer} - MoveIsComplete: {MoveIsComplete}");
                Console.WriteLine($"Human Move {move.MovesAsStandardNotation()}");
                CurrentPlayer = BackgammonBoard.Player2;
                
                //Reset variables for starting a new move
                MoveIsComplete = false;
                CurrentMove.Clear();//Should contain no checker moves when starting next move 
                IsGameOver = BackgammonBoard.GameEndedStatic(Board);
                if (IsGameOver)
                {
                    UpdateGameOverInfoAndScore();
                }
                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                // Handle any exceptions that might occur
                Console.WriteLine($"Error during complete human move: {ex.Message}");                
            }
            finally
            {
                _gameLock.Release();  // Acquire the lock
            }
        }

        public void SwapPlayerTurn() {
            _gameLock.Wait(); // Acquire the lock synchronously
            try
            {
                if (CurrentPlayer == BackgammonBoard.Player2)
                {
                    CurrentPlayer = BackgammonBoard.Player1;
                }
                else {
                    CurrentPlayer = BackgammonBoard.Player2;
                }
                Dice = _diceManager.RollDice();
                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                // Handle any exceptions that might occur
                Console.WriteLine($"Error during changing turn: {ex.Message}");
            }
            finally
            {
                _gameLock.Release();  // Always release the lock
            }
        }

        public async Task GenerateComputerMove()
        {
            await _gameLock.WaitAsync();  // Acquire the lock
            try
            {
                if (IsGameOver)
                {
                    Console.WriteLine("Game is over, no moves possible");
                    return;
                }
                
                if (HumanTurn)
                {
                    Console.WriteLine("Tried To Gen Computer Move but wrong turn" + CurrentPlayer);
                    return;
                }
                
                while (!HumanTurn && !IsGameOver)
                {
                    Console.WriteLine("GenerateComputerMove" + CurrentPlayer);
                    if (!IsFirstRoll)
                    {
                        Dice = _diceManager.RollDice();
                        Console.WriteLine("Computer Rolling Dice" + Dice.ToString());
                    }
                    NotifyStateChanged();
                    await Task.Delay(300);
                    var moveData = _gameSimulator.GenerateComputerMove(Board, Dice[0], Dice[1], CurrentPlayer);
                    if (moveData != null)
                    {
                        Board = moveData.BoardAfter;
                        Console.WriteLine("Computer moved" + moveData.Move.MovesAsStandardNotation());
                        PrintBackgammonBoard(Board);
                        NotifyStateChanged();
                        await Task.Delay(200);
                        CurrentPlayer = BackgammonBoard.Player1;
                        NotifyStateChanged();
                        MovesMade.Add(moveData.Move);
                    }
                    else
                    {
                        // Computer can't move
                        MovesMade.Add(new Move(Dice[0], Dice[1], CurrentPlayer, null));
                    }

                    await Task.Delay(200);

                    IsGameOver = BackgammonBoard.GameEndedStatic(Board);
                    if (IsGameOver)
                    {
                        UpdateGameOverInfoAndScore();
                        NotifyStateChanged();
                        return;
                    }

                    CurrentPlayer = BackgammonBoard.Player1;
                    Dice = _diceManager.RollDice();
                    NotifyStateChanged();
                    var humanLegalMoves = BackgammonBoard.GenerateLegalMovesStatic(Board, Dice[0], Dice[1], CurrentPlayer);

                    // When human can't move add an empty move and swap turns
                    if (humanLegalMoves.Count == 0)
                    { 
                        Move emptyMove = new Move(Dice[0], Dice[1], CurrentPlayer, null);
                        MovesMade.Add(emptyMove);
                        await Task.Delay(2000);
                        NotifyStateChanged();
                        CurrentPlayer = BackgammonBoard.Player2;
                    }
                    // When human can't move add an empty move and swap turns
                    else if (humanLegalMoves.Count == 1)
                    {
                        Move forcedMove = humanLegalMoves[0].move;
                        Board = humanLegalMoves[0].board;
                        MovesMade.Add(forcedMove);
                        await Task.Delay(2000);
                        NotifyStateChanged();
                        CurrentPlayer = BackgammonBoard.Player2;
                    }
                    IsGameOver = BackgammonBoard.GameEndedStatic(Board);                
                }
                if (IsGameOver)
                {
                    UpdateGameOverInfoAndScore();
                }
                
                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                // Handle any exceptions that might occur
                Console.WriteLine($"Error during move: {ex.Message}");
            }
            finally
            {
                _gameLock.Release();  // Always release the lock
            }
        }

        private void UpdateGameOverInfoAndScore() {
            var score = BackgammonBoard.Score(Board, cube: 1);
            if (score > 0)
            {
                ScorePlayer1 += score;
            }
            else
            {
                ScorePlayer2 -= score;
            }
            GameOverMessage = GetGameOverMessage(score);
        }

        // Check if there are no possible moves for human player
        public int ValidMovesCount() {
            //_gameLock.Wait(); // Acquire the lock synchronously
            try
            {
                var checkValidMoves = BackgammonBoard.GenerateLegalMovesStatic(Board, Dice[0], Dice[1], CurrentPlayer);
                return checkValidMoves.Count;
            }
            catch (Exception ex)
            {
                // Handle any exceptions that might occur
                Console.WriteLine($"Error during move: {ex.Message}");
                return -1;
            }
            finally
            {
                //_gameLock.Release();
            }
        }

        private int getNextDice()
        {
            if (Dice[0] == Dice[1])
                return Dice[0];

            if (CurrentMove.Count == 0)
            {
                return Dice[0];
            }

            var firstUsedDice = CurrentMove[0].DiceUsed;
            if (firstUsedDice == Dice[0])
            {
                return Dice[1];
            }
            else
            {
                return Dice[0];
            }
        }

        public void SwapDiceOrder()
        {
            _gameLock.Wait(); // Acquire the lock synchronously
            // Swap the dice values
            (Dice[0], Dice[1]) = (Dice[1], Dice[0]);
            _gameLock.Release();  // Always release the lock
        }

        //A helper class to track changes during a move making it easy ro revert to previous state
        private class MoveSnapshot
        {
            internal int[] BoardBefore { get; set; }
            internal CheckerMove CheckerMove { get; set; }
            internal int DiceUsed { get; set; }
            
            public MoveSnapshot(CheckerMove checkerMove, int[] boardBefore, int diceUsed)
            {
                CheckerMove = checkerMove;
                BoardBefore = boardBefore;
                DiceUsed = diceUsed;
            }            
        }

        private string GetGameOverMessage(int score)
        {
            switch (score)
            {
                case 1:
                    return "You win 1 point";
                case 2:
                    return "You win a gammon (2 points)";
                case 3:
                    return "You win a backgammon (3 points)";
                case -1:
                    return "You lose 1 point";
                case -2:
                    return "You lose a gammon (2 points)";
                case -3:
                    return "You lose a backgammon (3 points)";
                default:
                    return "";
            }
        }

        private void inspectGamesList()
        {
            Console.WriteLine("The games list");
            foreach (var move in MovesMade)
            {
                Console.WriteLine(move.MovesAsStandardNotation());
            }
        }

        private void NotifyStateChanged() => StateChanged?.Invoke();
    }
}
