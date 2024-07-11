namespace Backgammon.Models
{
    public class Move
    {
        // Initialized to an empty list to ensure it's never null
        public List<CheckerMove> CheckerMoves { get; private set; } = [];

        public bool DoubleOffer { get; private set; }
        private readonly int _die1;
        private readonly int _die2;
        private readonly int _player;

        public Move(int die1, int die2, int player, List<CheckerMove>? checkerMoves = null, bool doubleOffer = false)
        {
            // Assigns checkerMoves if not null; otherwise, retains the initialized empty list
            CheckerMoves = checkerMoves ?? new List<CheckerMove>();
            DoubleOffer = doubleOffer;
            _die1 = die1;
            _die2 = die2;
            _player = player;
        }

        public Move AddCheckerMove(CheckerMove checkerMove, bool clone = true)
        {
            var target = clone ? Clone() : this;
            target.CheckerMoves.Add(checkerMove);
            return target;
        }

        public bool HasCheckerMoves() => CheckerMoves.Count > 0;

        public Move Clone()
        {
            // Creates a new Move instance with a copy of CheckerMoves and the same DoubleOffer value
            return new Move(_die1, _die2, _player, new List<CheckerMove>(CheckerMoves), DoubleOffer);
        }

        public bool IsOpponentHit() => CheckerMoves.Exists(move => move.IsHit);

        public bool IsDoubleTiger()
        {
            // Counts checker moves that are hits; if there are 2 or more, it's a "double tiger"
            return CheckerMoves.FindAll(move => move.IsHit).Count >= 2;
        }

        public override string ToString()
        {
            return $"Moves: [{string.Join(", ", CheckerMoves)}], Double Offer: {DoubleOffer}";
        }

        //I store player2 moves different from player1 ie 21 slotting in first move is 13/11 6/5
        // but for player its 12/14 19/20 , to make notations easier lets return a list similar to player 1
        private List<CheckerMove> ConvertPlayer2MovesAsPlayer1()
        {
            List<CheckerMove> checkerMovesForNotation = [];
            foreach (var move in CheckerMoves)
            {
                var from = 25 - move.From;
                var to = move.To == BackgammonBoard.CheckersOffP2 ? BackgammonBoard.CheckersOffP1 :
                    25 - move.To;
                checkerMovesForNotation.Add(new CheckerMove(from, to, move.IsHit, move.IsBearOff));
            }
            return checkerMovesForNotation;
        }

        public string MovesAsStandardNotation()
        {
            var notation = $"{_die1}{_die2}:";
            if (CheckerMoves.Count == 0)
            {
                notation += " Cannot Move";
            }
            else
            {
                var firstCheckerMove = CheckerMoves.First();
                //var player = (firstCheckerMove.From > firstCheckerMove.To) ? BackgammonBoard.Player1 : BackgammonBoard.Player2;

                var movesForNotation = _player == BackgammonBoard.Player2 ?
                    ConvertPlayer2MovesAsPlayer1() :
                    CheckerMoves;
                movesForNotation = [.. movesForNotation.OrderByDescending(move => move.From).ThenBy(move => move.IsHit)];
                if (_die1 != _die2)
                {
                    foreach (var move in movesForNotation)
                    {
                        if (move.IsBearOff)
                        {
                            notation += $" {move.From}/Off";
                        }
                        else if (move.From == BackgammonBoard.OnTheBarP1)
                        {
                            notation += $" Bar/{move.To}";
                        }
                        else
                        {
                            notation += $" {move.From}/{move.To}";
                        }
                        if (move.IsHit)
                        {
                            notation += "*";
                        }
                    }
                    return notation;
                }

                // We have a special case, when we move same checkers 4 times without hitting in the first checker moves we can skip showing
                // the first 2 moves for instance with 44 13/9(2) 9/5(2) can be be replaced by 13/5 (2)
                if (movesForNotation.Count == 4 && !movesForNotation[0].IsHit)
                {
                    if (movesForNotation[0].From == movesForNotation[1].From && movesForNotation[2].From == movesForNotation[3].From
                        && movesForNotation[2].From == movesForNotation[0].To)
                    {
                        if (movesForNotation[3].IsBearOff)
                        {
                            notation += $" {movesForNotation[0].From}/Off";
                        }
                        else
                        {
                            notation += $" {movesForNotation[0].From}/{movesForNotation[3].To}";
                        }
                        if (movesForNotation[3].IsHit)
                        {
                            notation += "*";
                        }
                        notation += "(2)";
                        return notation;
                    }
                }
                
                // Compare with previous checker move to see if its a new 'group'
                var prevFrom = -1;
                // when there are several identical checker moves make a 'Group Count' to se how many moves are the the same 
                var groupCount = 1;
                var groupAttack = false;
                for (int moveCount = 0; moveCount < movesForNotation.Count; moveCount++)
                {
                    var move = movesForNotation[moveCount];
                    if (prevFrom != move.From)
                    {
                        groupCount = 1;
                        // Start a new group with 1 or more moves
                        groupAttack = move.IsHit;

                        if (move.IsBearOff)
                        {
                            notation += $" {move.From}/Off";
                        }
                        else if (move.From == BackgammonBoard.OnTheBarP1)
                        {
                            notation += $" Bar/{move.To}";
                        }
                        else
                        {
                            notation += $" {move.From}/{move.To}";
                        }
                        if (move.IsHit)
                        {
                            notation += "*";
                        }
                    }
                    else
                    {
                        groupCount++;                        
                    }

                    // When the groupcount > 1 and (next move differs or this is the last move) then we should add the groupCount to the notation 
                    if (groupCount > 1)
                    {
                        if (moveCount == movesForNotation.Count - 1) {// The last checker move
                            notation += $"({groupCount})";
                            return notation;
                        }
                        
                        var nextMove = movesForNotation[moveCount + 1];
                        if (nextMove.From != move.From)
                        {
                            notation += $"({groupCount})";
                        }
                    }

                    prevFrom = move.From;
                }
            }
            return notation;
        }

        public class CheckerMove
        {
            public int From { get; }
            public int To { get; }
            public bool IsHit { get; }
            public bool IsBearOff { get; }
            public bool IsRegular => !IsHit && !IsBearOff; // Derived property

            public CheckerMove(int from, int to, bool isHit = false, bool isBearOff = false)
            {
                From = from;
                To = to;
                IsHit = isHit;
                IsBearOff = isBearOff;
            }

            public override string ToString()
            {
                var description = IsRegular ? "Regular" : IsHit ? "Hit" : IsBearOff ? "Bear Off" : "Unknown";
                return $"{From} -> {To}, {description}";
            }
        }
    }
}
