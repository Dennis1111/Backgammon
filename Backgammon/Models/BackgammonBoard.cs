using System.Text;
using static Backgammon.Models.Move;

namespace Backgammon.Models
{
    public class BackgammonBoard
    {
        public const int Player1 = 0;
        public const int Player2 = 1;
        public const int OnTheBarP1 = 25;
        public const int OnTheBarP2 = 0;
        public const int BearOffP1 = 26;
        public const int BearOffP2 = 27;
        public const int AcePointP1 = 1;
        public const int DeucePointP1 = 2;
        public const int ThreePointP1 = 3;
        public const int FourPointP1 = 4;
        public const int GoldenPointP1 = 5;
        public const int SixPointP1 = 6;
        public const int BarPointP1 = 7;
        public const int EightPointP1 = 8;
        public const int MidPointP1 = 13;

        public const int AcePointP2 = 24;
        public const int DeucePointP2 = 23;
        public const int ThreePointP2 = 22;
        public const int FourPointP2 = 21;
        public const int GoldenPointP2 = 20;
        public const int SixPointP2 = 19;
        public const int BarPointP2 = 18;
        public const int EightPointP2 = 17;
        public const int MidPointP2 = 12;

        // Represents the state of the board
        public int[] Position { get; set; }

        // Current player (Player1 or Player2)
        public int CurrentPlayer { get; set; }

        // Added properties for dice and doubling cube
        public int Die1 { get; set; }
        public int Die2 { get; set; }
        public int DoublingCube { get; private set; } = 1; // Initial value of 1
        // public bool IsCubeOffered { get; private set; } = false; // Tracks if the cube is offered

        public BackgammonBoard()
        {
            // Initialize the board with 28 points for the 24 standard points,
            // plus positions for the bar and bear-off
            Position = new int[28];
            ResetBoard();
        }

        public static string BoardAsString(int[] position, int player, int die1, int die2)
        {
            var (pipCountP1, pipCountP2) = PipCountStatic(position);
            var boardStr = new StringBuilder();

            // Create the top half of the board
            for (int row = 0; row < 6; row++)
            {
                if (row == 1)
                    boardStr.Append(new string('-', 20)).Append("\n");

                for (int i = 0; i < 12; i++)
                {
                    boardStr.Append(FormatPoint(position, 13 + i, row));
                    if (i == 5)
                    {
                        boardStr.Append("|").Append(FormatPoint(position, OnTheBarP1, row)).Append("|");
                    }
                    if (i == 11)
                    {
                        boardStr.Append("|");
                    }
                }

                if (row == 0)
                    boardStr.Append("pips:").Append(pipCountP2);
                if (row == 1)
                    boardStr.Append("off:").Append(-position[BearOffP2]);

                if (row == 4 && player == Player2)
                    boardStr.Append("rolled: ").Append(die1).Append(die2);
                boardStr.Append("\n");
            }

            boardStr.Append(new string('-', 20)).Append("\n");

            // Create the bottom half of the board
            for (int row = 5; row >= 0; row--)
            {
                if (row == 0)
                    boardStr.Append(new string('-', 20)).Append("\n");
                for (int i = 0; i < 12; i++)
                {
                    boardStr.Append(FormatPoint(position, 12 - i, row));
                    if (i == 5)
                    {
                        boardStr.Append("|").Append(FormatPoint(position, OnTheBarP2, row)).Append("|");
                    }
                    if (i == 11)
                    {
                        boardStr.Append("|");
                    }
                }

                if (row == 4 && player == Player1)
                    boardStr.Append("rolled: ").Append(die1).Append(die2);

                if (row == 0)
                    boardStr.Append("pips:").Append(pipCountP1);
                if (row == 1)
                    boardStr.Append("off:").Append(position[BearOffP1]);
                boardStr.Append("\n");
            }

            return boardStr.ToString();
        }

        public override string ToString()
        {
            var (pipCountP1, pipCountP2) = this.PipCount();
            var boardStr = new StringBuilder();

            // Create the top half of the board
            for (int row = 0; row < 6; row++)
            {
                if (row == 1)
                    boardStr.Append(new string('-', 20)).Append("\n");

                for (int i = 0; i < 12; i++)
                {
                    boardStr.Append(FormatPoint(13 + i, row));
                    if (i == 5)
                    {
                        boardStr.Append("|").Append(FormatPoint(OnTheBarP1, row)).Append("|");
                    }
                    if (i == 11)
                    {
                        boardStr.Append("|");
                    }
                }

                if (row == 0)
                    boardStr.Append("pips:").Append(pipCountP2);
                if (row == 1)
                    boardStr.Append("off:").Append(-Position[BearOffP2]);

                if (row == 4 && CurrentPlayer == Player2)
                    boardStr.Append("rolled: ").Append(Die1).Append(Die2);
                boardStr.Append("\n");
            }

            boardStr.Append(new string('-', 20)).Append("\n");

            // Create the bottom half of the board
            for (int row = 5; row >= 0; row--)
            {
                if (row == 0)
                    boardStr.Append(new string('-', 20)).Append("\n");
                for (int i = 0; i < 12; i++)
                {
                    boardStr.Append(FormatPoint(12 - i, row));
                    if (i == 5)
                    {
                        boardStr.Append("|").Append(FormatPoint(OnTheBarP2, row)).Append("|");
                    }
                    if (i == 11)
                    {
                        boardStr.Append("|");
                    }
                }

                if (row == 4 && CurrentPlayer == Player1)
                    boardStr.Append("rolled: ").Append(Die1).Append(Die2);

                if (row == 0)
                    boardStr.Append("pips:").Append(pipCountP1);
                if (row == 1)
                    boardStr.Append("off:").Append(Position[BearOffP1]);
                boardStr.Append("\n");
            }

            return boardStr.ToString();
        }

        private string FormatPoint(int point, int row)
        {
            return FormatPoint(Position, point, row);
        }

        private static string FormatPoint(int[] pos, int point, int row)
        {
            int numCheckers = pos[point];
            if (row == 0)
            {
                if (Math.Abs(numCheckers) > 0)
                    return Math.Abs(numCheckers).ToString();
                return " ";
            }
            else if (Math.Abs(numCheckers) > row - 1) // Check if any checkers
                return numCheckers > 0 ? "x" : "o";
            else
                return " ";
        }

        //Return the score from player1 1s perspective
        public static int Score(int[] position, int cube)
        {
            var score = 0;
            var scoreAsVector = ScoreAsVector(position);
            if (scoreAsVector[2] == 1)
                return 3 * cube;
            if (scoreAsVector[1] == 1)
                return 2 * cube;
            if (scoreAsVector[0] == 1)
                return 1 * cube;
            if (scoreAsVector[5] == 1)
                return -3 * cube;
            if (scoreAsVector[4] == 1)
                return -2 * cube;
            if (scoreAsVector[3] == 1)
                return -1 * cube;
            return score;
        }

        public static float[] ScoreAsVector(int[] position)
        {
            // Initialize the score vector as a float array
            float[] scoreVector = new float[6];

            if (!GameEndedStatic(position))
            {
                // If the game hasn't ended, return the default score vector [0,0,0,0,0,0]
                return scoreVector;
            }

            if (position[BearOffP1] == 15)
            {
                // Player 1 wins
                scoreVector[0] = 1.0f; // Win for player 1
                if (!SavedGammon(position, Player2))
                {
                    if (position[BearOffP2] == 0)
                    {
                        scoreVector[1] = 1.0f; // Gammon for player 1
                    }
                    // Must include the bar point 0,1,2,3,4,5,6
                    if (position.Take(7).Any(value => value < 0))
                    {
                        scoreVector[2] = 1.0f; // Backgammon for player 1
                    }
                }
            }
            else
            {
                // Player 2 wins
                scoreVector[3] = 1.0f; // Win for player 2
                if (!SavedGammon(position, Player1))
                {
                    if (position[BearOffP1] == 0)
                    {
                        scoreVector[4] = 1.0f; // Gammon for player 2
                    }
                    // Must include bar point 19,20,21,22,23,24,25
                    if (position.Skip(19).Take(7).Any(value => value > 0))
                    {
                        scoreVector[5] = 1.0f; // Backgammon for player 2
                    }
                }
            }
            return scoreVector;
        }

        public static int[] MirrorBoard(int[] position)
        {
            // Pre-allocate an array of size 28 to include all positions
            int[] mirroredBoard = new int[28];

            // Mirror the main points from AcePointP1 to AcePointP2
            for (int i = AcePointP1; i <= AcePointP2; i++)
            {
                mirroredBoard[AcePointP2 - i + AcePointP1] = -position[i];
            }

            // Handle special positions: Bar and Borne-off checkers
            mirroredBoard[OnTheBarP1] = -position[OnTheBarP2]; // Player 1's bar checkers become Player 2's
            mirroredBoard[OnTheBarP2] = -position[OnTheBarP1]; // Player 2's bar checkers become Player 1's

            // Switch the borne-off checkers
            mirroredBoard[BearOffP1] = -position[BearOffP2]; // Player 2's borne-off checkers
            mirroredBoard[BearOffP2] = -position[BearOffP1]; // Player 1's borne-off checkers

            return mirroredBoard;
        }

        private void ResetBoard()
        {
            // Reset the board to the starting position
            Position = new int[28]; // Clear the board
            //The bottom (Player 1) side
            Position[AcePointP1] = -2;
            Position[SixPointP1] = 5;
            Position[EightPointP1] = 3;
            Position[MidPointP2] = -5;

            //The top (Player 2) side
            Position[AcePointP2] = 2;
            Position[SixPointP2] = -5; // Player 2 starting position
            Position[EightPointP2] = -3;
            Position[MidPointP1] = 5;

            CurrentPlayer = Player1; // Player 1 starts
        }

        public (int pipsPlayer1, int pipsPlayer2) PipCount()
        {
            // Directly uses the instance's Points array
            return PipCountStatic(this.Position);
        }

        internal static (int pipsPlayer1, int pipsPlayer2) PipCountStatic(int[] position)
        {
            int pipsPlayer1 = 0;
            int pipsPlayer2 = 0;
            for (int i = 1; i <= 24; i++)
            {
                if (position[i] > 0)
                {
                    pipsPlayer1 += i * position[i];
                }
                else if (position[i] < 0)
                {
                    pipsPlayer2 -= (25 - i) * position[i];
                }
            }
            return (pipsPlayer1, pipsPlayer2);
        }

        internal static (int pipsPlayer1, int pipsPlayer2) PipCountBackgameTiming(int[] position)
        {
            int pipsPlayer1 = 0;
            int pipsPlayer2 = 0;
            for (int i = 1; i <= 18; i++)
            {
                if (position[i] > 0)
                {
                    pipsPlayer1 += i * position[i];
                }
            }

            for (int i = 1; i <= 18; i++)
            {
                var checkers = position[AcePointP2 - i + 1];
                if (checkers > 0)
                {
                    pipsPlayer2 += i * checkers;
                }
            }
            return (pipsPlayer1, pipsPlayer2);
        }

        // Instance method to check if the game has ended
        public bool GameEnded()
        {
            return GameEndedStatic(Position);
        }

        // Static method to check if a game has ended given a points array
        public static bool GameEndedStatic(int[] position)
        {
            return position[BearOffP1] == 15 || position[BearOffP2] == -15;
        }

        public static (int, int) LastCheckers(int[] position)
        {
            int lastCheckerP1 = 0;
            for (int i = OnTheBarP1; i >= AcePointP1; i--)
            {
                if (position[i] > 0)
                {
                    lastCheckerP1 = i;
                    break;
                }
            }

            int lastCheckerP2 = 0;
            for (int i = OnTheBarP2; i <= AcePointP2; i++)
            {
                if (position[i] < 0)
                {
                    lastCheckerP2 = i;
                    break;
                }
            }

            // Adjusted logic based on 0-based indexing in C#
            return (lastCheckerP1, lastCheckerP2);
        }
       

        // Static method to check if there's still contact between the players' checkers
        public static (bool, int) StillContact(int[] position)
        {
            if (GameEndedStatic(position))
                return (false, 0);

            int lastCheckerP1 = 0;
            for (int i = OnTheBarP1; i >= AcePointP1; i--)
            {
                if (position[i] > 0)
                {
                    lastCheckerP1 = i;
                    break;
                }
            }

            int lastCheckerP2 = 0;
            for (int i = OnTheBarP2; i <= AcePointP2; i++)
            {
                if (position[i] < 0)
                {
                    lastCheckerP2 = i;
                    break;
                }
            }
            
            // Adjusted logic based on 0-based indexing in C#
            return (lastCheckerP1 > lastCheckerP2, lastCheckerP1 - lastCheckerP2);
        }

        // Internal static method to check if a gammon has been saved for a specific player
        internal static bool SavedGammon(int[] position, int player)
        {
            if (player == Player1)
            {
                return position[BearOffP1] > 0;
            }
            else // Assuming any value not Player1 is Player2 for simplicity
            {
                return position[BearOffP2] < 0;
            }
        }

        // Internal static method to check if a gammon has been saved for both players
        internal static (bool savedForPlayer1, bool savedForPlayer2) SavedGammonForBoth(int[] position)
        {
            return (position[BearOffP1] > 0, position[BearOffP2] < 0);
        }

        internal static bool SavedBackgammon(int[] position, int player)
        {
            if (SavedGammon(position, player))
            {
                return true;
            }

            var (stillContact, _) = StillContact(position);
            if (stillContact)
            {
                return false; // As long as there is contact, you can be sent back
            }

            if (player == Player1)
            {
                for (int i = 0; i < 7; i++)
                {
                    // Assuming 1-based indexing for board positions, adjust if your setup is different
                    if (position[OnTheBarP1 - i] > 0)
                    {
                        return false; // Player 1 has checkers in the last six points or on the bar
                    }
                }
            }
            else // Assuming any value not Player1 is Player2 for simplicity
            {
                for (int i = 0; i < 7; i++)
                {
                    // Adjust the range if your board indexing or direction is different
                    if (position[OnTheBarP2 + i] < 0)
                    {
                        return false; // Player 2 has checkers in the first six points or on the bar
                    }
                }
            }

            return true; // No checkers in the last six points or on the bar, and no contact
        }

        internal static (bool isValid, int checkersP1, int checkersP2) ValidateBoard(int[] position)
        {
            int checkersP1 = 0;
            int checkersP2 = 0;
            for (int point = 0; point < position.Length; point++)
            {
                if (position[point] > 0)
                {
                    checkersP1 += position[point];
                }
                else if (position[point] < 0)
                {
                    checkersP2 -= position[point]; // Subtract to make positive, since P2's checkers are negative
                }
            }

            bool isValid = checkersP1 == 15 && checkersP2 == 15 && position.Length == 28;
            return (isValid, checkersP1, checkersP2);
        }

        // Method for performing a bear-off move, updated to use the new CheckerMove class
        public static (CheckerMove checkerMove, int[] updatedPoints) BearOff(int[] position, int fromPos, int player)
        {
            int[] pointsCopy = (int[])position.Clone();
            CheckerMove checkerMove;

            if (player == Player1)
            {
                pointsCopy[fromPos] -= 1;
                pointsCopy[BearOffP1] += 1;
                checkerMove = new CheckerMove(fromPos, BearOffP1, isBearOff: true);
            }
            else // Assuming Player2
            {
                pointsCopy[fromPos] += 1; // Adjust if necessary based on how Player2's checkers are represented
                pointsCopy[BearOffP2] -= 1;
                checkerMove = new CheckerMove(fromPos, BearOffP2, isBearOff: true);
            }

            var (isValidPosition, checkersP1, checkersP2) = ValidateBoard(pointsCopy);
            if (!isValidPosition)
            {
                Console.WriteLine("INV: " + string.Join(", ", position));
                Console.WriteLine("INV: " + string.Join(", ", pointsCopy));
                throw new InvalidOperationException($"Invalid board state after bear off: P1 Checkers = {checkersP1}, P2 Checkers = {checkersP2}");
            }

            return (checkerMove, pointsCopy);
        }

        /// <summary>
        /// Return the new position when moving a checker as long as it is not a bearoff 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="fromPos"></param>
        /// <param name="die"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static (CheckerMove checkerMove, int[] updatedPoints) MoveChecker(int[] position, int fromPos, int die, int player)
        {
            int[] pointsCopy = (int[])position.Clone();
            int targetPos;
            CheckerMove checkerMove;

            if (player == Player1)
            {
                pointsCopy[fromPos] -= 1;
                targetPos = fromPos - die;
                if (pointsCopy[targetPos] == -1) // Hit opponent
                {
                    pointsCopy[targetPos] = 1;
                    pointsCopy[OnTheBarP2] -= 1;
                    checkerMove = new CheckerMove(fromPos, targetPos, isHit: true);
                }
                else
                {
                    pointsCopy[targetPos] += 1;
                    checkerMove = new CheckerMove(fromPos, targetPos, isHit: false);
                }
            }
            else
            {
                pointsCopy[fromPos] += 1;
                targetPos = fromPos + die;
                if (pointsCopy[targetPos] == 1)
                {
                    pointsCopy[targetPos] = -1;
                    pointsCopy[OnTheBarP1] += 1;
                    checkerMove = new CheckerMove(fromPos, targetPos, isHit: true);
                }
                else
                {
                    pointsCopy[targetPos] -= 1;
                    checkerMove = new CheckerMove(fromPos, targetPos, isHit: false);
                }
            }

            var (isValidPosition, checkersP1, checkersP2) = ValidateBoard(pointsCopy);
            if (!isValidPosition)
            {
                Console.WriteLine("origin" + string.Join(", ", position));
                Console.WriteLine("cp" + string.Join(", ", pointsCopy));
                throw new InvalidOperationException($"Invalid board state after move: P1 Checkers = {checkersP1}, P2 Checkers = {checkersP2}");
            }

            return (checkerMove, pointsCopy);
        }

        internal static bool AnyCheckersOnTheBar(int[] position, int player)
        {
            if (player == Player1)
            {
                return position[OnTheBarP1] > 0;
            }
            else // Assuming Player2
            {
                return position[OnTheBarP2] < 0;
            }
        }

        internal static bool IsBearOffAllowed(int[] position, int player)
        {
            if (player == Player1)
            {
                // Iterate from 'on the bar' position down to just above the home board start, excluding index 6
                for (int i = 25; i >= 7; i--)
                {
                    if (position[i] > 0) return false; // Found a Player 1 checker outside the designated bear-off zone
                }
                return true; // No checkers found outside the bear-off zone, bearing off is allowed
            }
            else // Player 2
            {
                // Adjust Player 2's logic as needed based on their 'on the bar' position and home board
                for (int i = 0; i <= 18; i++)
                {
                    if (position[i] < 0) return false; // Found a Player 2 checker outside the designated bear-off zone
                }
                return true; // No checkers found outside the bear-off zone, bearing off is allowed
            }
        }

        // Method to determine if the checker is the last one in the bear-off zone
        private static bool IsLastChecker(int[] position, int point, int player)
        {
            if (player == Player1)
            {
                for (int index = point + 1; index <= 6; index++) // For Player 1, check home board points 1 through 6
                {
                    if (position[index] > 0)
                    {
                        return false; // Found another Player 1 checker in the bear-off zone
                    }
                }
            }
            else // Player 2
            {
                for (int index = point - 1; index >= 19; index--) // For Player 2, check home board points 24 through 19
                {
                    if (position[index] < 0)
                    {
                        return false; // Found another Player 2 checker in the bear-off zone
                    }
                }
            }
            return true; // No other checkers found in the bear-off zone, so it's the last checker
        }

        public static bool IsValidCheckerMoveFromTheBar(int[] position, int die, int player)
        {
            if (player == Player1)
            {
                // For PLAYER_1, check if the target position (starting from the bar position minus the die roll)
                // has less than or equal to one opponent checker, making it a valid move.
                return position[OnTheBarP1 - die] >= -1;
            }
            else
            {
                // For the opponent (not PLAYER_1), check if the target position (starting from the bar position plus the die roll)
                // has less than or equal to one of PLAYER_1's checkers, making it a valid move.
                return position[OnTheBarP2 + die] <= 1;
            }
        }


        // Method to check if a checker move is valid
        internal static bool IsValidCheckerMove(int[] position, int point, int die, int player, bool isBearOffAllowed)
        {
            if (player == Player1)
            {
                if (position[point] <= 0) return false;
                if (point - die == AcePointP1 - 1) return isBearOffAllowed;
                if (point - die < AcePointP1 - 1 && isBearOffAllowed)
                    return IsLastChecker(position, point, player);
                if (point - die < AcePointP1) return false;
                return position[point - die] > -2;
            }
            else // Player 2
            {
                if (position[point] >= 0) return false;
                if (point + die == AcePointP2 + 1) return isBearOffAllowed;
                if (point + die > AcePointP2 + 1 && isBearOffAllowed)
                    return IsLastChecker(position, point, player);
                if (point + die > AcePointP2) return false;
                return position[point + die] < 2; // Check if the target point is not blocked by more than 1 opponent checker
            }
        }

        // Method to generate legal checker moves for a given die roll and player
        /// <summary>
        /// Given a position and die find the next possible checker move. If the player is on the bar we have bear in that checker before doing anything else.
        /// If not on the bar we start searching from the checker that has longest distance to move (searchFrom = 1).
        /// When we he have checked all possible moves from point 1 we want to call this method with searchFrom increased
        /// </summary>
        /// <param name="position"></param>
        /// <param name="die"></param>
        /// <param name="player"></param>
        /// <param name="searchFrom"></param>
        /// <returns></returns>
        private static List<(CheckerMove move, int[] board, int searchFrom)> GenerateLegalCheckerMoves(int[] position, int die, int player, int searchFrom = 1)
        {
            //Console.WriteLine("Gen checkers moves: with die: " + die + " from" + searchFrom);
            //Console.WriteLine(BoardAsString(position, player, 0, 0));
            var movesAndBoards = new List<(CheckerMove move, int[] board, int searchFrom)>();
            if (AnyCheckersOnTheBar(position, player))
            {
                if (IsValidCheckerMoveFromTheBar(position, die, player))
                {
                    int pointFrom = player == Player1 ? OnTheBarP1 : OnTheBarP2;
                    var (move, board) = MoveChecker(position, pointFrom, die, player);
                    searchFrom = 1; // Reset search start when moving from the bar
                    movesAndBoards.Add((move, board, searchFrom));
                }
            }
            else
            {
                bool isBearOffAllowed = IsBearOffAllowed(position, player);
                // When the bear off is not allowed we can't move checkers from the Acepoint (that would bear off a checker)
                // so we only need to scan 23 points (when not on the bar)
                int searchTo = isBearOffAllowed ? 24 : 23;

                for (int point = searchFrom; point <= searchTo; point++)
                {
                    int pointFrom = player == Player1 ? 25 - point : point;

                    if (IsValidCheckerMove(position, pointFrom, die, player, isBearOffAllowed))
                    {
                        CheckerMove move;
                        int[] board;
                        if (isBearOffAllowed && (player == Player1 ? pointFrom - die < 1 : pointFrom + die > 24))
                        {
                            (move, board) = BearOff(position, pointFrom, player);
                        }
                        else
                        {
                            (move, board) = MoveChecker(position, pointFrom, die, player);
                        }
                        //Console.WriteLine("Adding gen legal checkerMove: " + move.ToString());
                        movesAndBoards.Add((move, board, point));
                    }
                }
            }
            return movesAndBoards;
        }

        /// <summary>
        /// From a gives position and list of dies find all Legal Moves for the player that is to move
        /// If N moves is possible to do it's not allowed to do M moves where N > M with some possible exceptions in the bear off
        /// (as an example with one remaining checker on the 6 point and rolling 16 Both 6/off and 6/5 5/off is allowed)
        /// </summary>
        /// <param name="movesWithBoards"></param>
        /// <param name="dies"></param>
        /// <param name="player"></param>
        /// <param name="incSearchFrom">
        /// When true we will not make two checker moves from the same point
        /// This is useful since if we calls the function with die1, die2 and then with die2, die1 
        /// </param>
        /// <returns>All the legal moves and true if it was possible to make a move with all dies</returns>
        private static (List<(Move move, int[] board, int searchFrom)>, bool) GenerateLegalMovesHelper(
           List<(Move move, int[] board, int searchFrom)> movesWithBoards, List<int> dies, int player, bool incSearchFrom = false)
        {

            List<(Move move, int[] board, int searchFrom)> incompleteMoves = new List<(Move move, int[] board, int searchFrom)>();
            while (dies.Count > 0)
            {
                int die = dies[0];
                dies = dies.GetRange(1, dies.Count - 1); // Remove the first die from the remaining list
                List<(Move move, int[] board, int searchFrom)> tempMovesWithBoards = new List<(Move move, int[] board, int searchFrom)>();

                foreach (var moveWithBoard in movesWithBoards)
                {
                    var (tempMove, tempBoard, searchFrom) = moveWithBoard;
                    
                    //Console.WriteLine("tempBoard: " + BoardAsString(tempBoard, player, die, die));
                    //Console.WriteLine("tempMove: " + tempMove);
                    if (dies.Count == 0 && incSearchFrom)
                    {
                        var moveFrom = tempMove.CheckerMoves.Last().From;
                        bool moveFromTheBar = moveFrom == OnTheBarP1 || moveFrom == OnTheBarP2;                        
                        if (!moveFromTheBar)
                        {
                            searchFrom += 1;
                        }
                    }
                    //Console.WriteLine("tempMove: " + tempMove+ "searchFrom: " + searchFrom);
                    //int nextSearchFrom = 1;
                    var checkerMovesAndBoards = GenerateLegalCheckerMoves(tempBoard, die, player, searchFrom);

                    if (checkerMovesAndBoards.Count > 0)
                    {
                        foreach (var (checkerMove, board, newSearchFrom) in checkerMovesAndBoards)
                        {
                            var newMove = tempMove.AddCheckerMoveOld(checkerMove);
                            //Console.WriteLine("Adding move: " + newMove.ToString());
                            tempMovesWithBoards.Add((newMove, board, newSearchFrom));
                        }
                    }
                    else if (tempMove.HasCheckerMoves())
                    {
                        incompleteMoves.Add(moveWithBoard);
                    }
                }
                //This assignment should be forgotten after return so feels as bad code
                movesWithBoards = tempMovesWithBoards;
                //Console.WriteLine("tempMovesWithBoards: " + movesWithBoards.Count);
            }

            if (movesWithBoards.Count == 0)
            {
                return (incompleteMoves, false);
            }
            return (movesWithBoards, true);
        }

        // Method to generate all legal moves for a given pair of non-double dice rolls
        private static List<(Move move, int[] board, int searchFrom)> GenerateLegalMovesNonDouble(int[] position, int die1, int die2, int player)
        {
            Move emptyMove = new(die1, die2, player);
            int searchFrom = 1; // Start looping from the ace point
            var movesWithBoardsInit = new List<(Move move, int[] board, int searchFrom)> { (emptyMove, position, searchFrom) };

            // Generate moves for die1 followed by die2
            var (movesWithBoards, complete) = GenerateLegalMovesHelper(movesWithBoardsInit, new List<int> { die1, die2 }, player);

            // Generate moves for die2 followed by die1, with increment search_from to avoid duplicates
            // incSearchFrom = true will avoid making two two checker, moves from the same point , no need to do it again
            // As an example if we already have added 15/13 15/12 we don't need to now add 15/13 15/12
            var (movesWithBoardsReversed, completeReversed) = GenerateLegalMovesHelper(movesWithBoardsInit, new List<int> { die2, die1 }, player, true);
            List<(Move move, int[] board, int searchFrom)> legalMoves;
            if (complete && completeReversed)
            {
                legalMoves = movesWithBoards;
                legalMoves.AddRange(movesWithBoardsReversed);
            }
            else if (complete)
            {
                legalMoves = movesWithBoards;
            }
            else if (completeReversed)
            {
                legalMoves = movesWithBoardsReversed;
            }
            else
            {
                // If neither sequence is complete, combine both sets of moves as they are
                legalMoves = movesWithBoards;
                legalMoves.AddRange(movesWithBoardsReversed);
            }
            return legalMoves;
        }

        private static List<(Move move, int[] board, int searchFrom)> GenerateLegalMovesDouble(int[] position, int die, int player)
        {
            Move emptyMove = new(die, die, player);
            int searchFrom = 1;
            var movesWithBoardsInit = new List<(Move move, int[] board, int searchFrom)> { (emptyMove, position, searchFrom) };
            var dies = new List<int> { die, die, die, die }; // Four times the same die value

            var (movesWithBoards, complete) = GenerateLegalMovesHelper(movesWithBoardsInit, dies, player);

            if (complete || movesWithBoards.Count == 0)
            {
                // Found moves with 4 checker moves or no moves at all possible
                return movesWithBoards;
            }

            // If it's not possible to move all 4 dies, try to find all legal moves with length 3, then 2, then 1
            for (int numCheckerMoves = 3; numCheckerMoves > 0; numCheckerMoves--)
            {
                List<(Move move, int[] board, int searchFrom)> boardsWithMaxCheckerMoves = [];
                foreach (var moveWithBoard in movesWithBoards)
                {
                    var move = moveWithBoard.move;
                    if (move.CheckerMoves.Count == numCheckerMoves)
                    {
                        boardsWithMaxCheckerMoves.Add(moveWithBoard);
                    }
                }
                if (boardsWithMaxCheckerMoves.Count > 0)
                {
                    return boardsWithMaxCheckerMoves;
                }
            }

            return [];
        }

        // Method to remove duplicate board states from a list of moves and associated board states
        private static List<(Move move, int[] board, int searchFrom)> RemoveDuplicateBoards(List<(Move move, int[] board, int searchFrom)> movesAndBoards)
        {
            var uniqueBoards = new List<(Move move, int[] board, int searchFrom)>();

            foreach (var candidate in movesAndBoards)
            {
                // Check if the candidate board is already in uniqueBoards based on board state
                if (!uniqueBoards.Any(existing => existing.board.SequenceEqual(candidate.board)))
                {
                    uniqueBoards.Add(candidate);
                }
            }
            return uniqueBoards;
        }

        private static List<(Move move, int[] board)> RemoveSearchFrom(List<(Move move, int[] board, int searchFrom)> moveAndBoardsList)
        {
            var result = new List<(Move move, int[] board)>();
            foreach (var elem in moveAndBoardsList)
            {
                result.Add((elem.move, elem.board));
            }
            return result;
        }

        // Static method to generate all legal moves based on the dice rolls and player
        // When two moves results in the same board state there is no need for ai to evaluate same pos twice
        // For instance lets say you roll 6 3 and you can move 21/18 18/12 or 21/15 15/12 we would get the same final position
        // unless we also hit our opponent
        
        public static List<(Move move, int[] board)> GenerateLegalMovesStatic(int[] boardAsPoints, int die1, int die2, int player, bool removeDuplicates = true)
        {
            List<(Move move, int[] board, int searchFrom)> legalMoves;

            if (die1 == die2)
            {
                // Assumes GenerateLegalMovesDouble is implemented and does not generate duplicates for double rolls
                legalMoves = GenerateLegalMovesDouble(boardAsPoints, die1, player);
            }
            else
            {
                legalMoves = GenerateLegalMovesNonDouble(boardAsPoints, die1, die2, player);
                if (removeDuplicates)
                {
                    legalMoves = RemoveDuplicateBoards(legalMoves);
                }
            }
            return RemoveSearchFrom(legalMoves);
        }

        // Instance method to generate legal moves for the current board state
        // SearchFrom should be removed
        public List<(Move move, int[] board)> GenerateLegalMoves(int die1, int die2, int player, bool removeDuplicates = true)
        {
            return GenerateLegalMovesStatic(this.Position, die1, die2, player, removeDuplicates);
        }

        /*public static bool isValidCheckerMove(List<Move> validMoves, List<CheckerMove> currentMove, CheckerMove candidate)
        {
            // Combine currentMove and candidate into a single list
            var extendedMove = new List<CheckerMove>(currentMove) { candidate };

            // Check if extendedMove is a subset of any validMove's CheckerMoves
            return validMoves.Any(validMove =>
                extendedMove.All(checkerMove =>
                    validMove.CheckerMoves.Any(validCheckerMove =>
                        validCheckerMove.From == checkerMove.From && validCheckerMove.To == checkerMove.To)));
        }*/
        
        public static bool isValidCheckerMove(List<Move> validMoves, List<CheckerMove> currentMove, CheckerMove candidate)
        {
            var extendedMove = new List<CheckerMove>(currentMove) { candidate };
            Console.WriteLine("extendedMove: " + string.Join(", ", extendedMove));
            foreach (var validMove in validMoves)
            {
                if (IsSubSet(extendedMove, validMove.CheckerMoves))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsSubSet(List<CheckerMove> candidate, List<CheckerMove> validSet)
        {
            foreach (var move in candidate)
            {
                bool found = validSet.Any(validMove =>
                    validMove.From == move.From && validMove.To == move.To);
                if (!found)
                    return false;
            }
            return true;
        }
    }
}
