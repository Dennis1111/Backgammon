using System.Collections.Generic;

namespace Backgammon.Models
{
    public class Move
    {
        // Initialized to an empty list to ensure it's never null
        public List<CheckerMove> CheckerMoves { get; private set; } = [];

        public bool DoubleOffer { get; private set; }

        public Move(List<CheckerMove>? checkerMoves = null, bool doubleOffer = false)
        {
            // Assigns checkerMoves if not null; otherwise, retains the initialized empty list
            CheckerMoves = checkerMoves ?? new List<CheckerMove>();
            DoubleOffer = doubleOffer;
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
            return new Move(new List<CheckerMove>(CheckerMoves), DoubleOffer);
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
