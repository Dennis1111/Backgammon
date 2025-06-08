using static Backgammon.Models.BackgammonBoard;
using static Backgammon.Util.Constants;
using static Backgammon.Analysis.BoardFeatures;

namespace Backgammon.Analysis
{
    public static class BoardClassifier
    {
        /// <summary>
        /// We use different neural networks for different kind of positions, its not obvious how to best split into categories
        /// but categories that differs a lot in evaluations are backgames, crunched positions, strong primes lets start from the end of the game
        /// 1. No contact positions can be divided into
        /// 1A Pure bear off positions where both sides is bearing off, (currently my bear off database handles up to 7 seven remaining checkers)
        /// so the neural network has to dela with the remainder
        /// 1B Only one side is allowed to bear off (here crossovers are important to save gammon or win the race)
        /// 
        /// 2. Bearing off vs contact
        /// 2A1 Bearing off vs the 1Point is one of the most common scenarios and I think it needs it's own network (however once opp has crunched -> CrunchedNet)
        /// The bearing off player has evaluate playing safe vs going for gammons
        /// 2A2 Defending against bearing Off from the 1point, The defending side must master playing pure, avoid crunching, keep the anchor or run to save gammons
        /// 
        /// 2B1 Bearing off vs Other contact (for instance vs deuce point, three point, closed board with checkers on the bar (perhaps another category))
        /// 2B2 Defending against Bearing off vs Other contact (for instance vs deuce point, three point, closed board with checkers on the bar (perhaps another category))
        /// 3. Completed stage. There is still contact but the player at the completed is normally favourite and how much depends on remaining contact + race
        /// Should we have 2 nets here also ?
        /// 
        /// 4. Backgames We have different networks for 12,13,23 as they are the strongest backgames but also very sensitive to timing.
        /// 
        /// 5. Other backgames. Any backgame that also involves the 4 or 5 point
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="player"></param>
        /// <returns>The PositionType</returns>
        public static PositionType MapBoardToPositionType(int[] position, int player)
        {
            // var backgammonBoard = new BackgammonBoard();
            //backgammonBoard.Position = position;
            // Console.WriteLine(backgammonBoard);
            var (stillContact, contactDistance) = StillContact(position);

            if (!stillContact)
            {
                if (IsBearOffAllowed(position, Player1) && IsBearOffAllowed(position, Player2))
                {
                    return PositionType.BearOff;
                }
                return PositionType.NoContact;
            }

            (var deadCheckersP1, var deadCheckersP2) = DeadCheckers(position);

            if (deadCheckersP2 >= 6 || deadCheckersP1 >= 6)
            {
                return PositionType.BigCrunch;
            }

            var innerBoardStrengthP1 = CountInnerBoardPoints(position, Player1);
            var innerBoardStrengthP2 = CountInnerBoardPoints(position, Player2);

            if (deadCheckersP1 == 0 && innerBoardStrengthP1 <= 1 && deadCheckersP2 == 0 && innerBoardStrengthP2 <= 1)
            {
                return PositionType.EarlyGame;
            }

            // 3 dead checkers is a small crunch start but should already greatly affect the Game Strategy,
            // playing pure with more risks
            if ((deadCheckersP1 >= 3 && innerBoardStrengthP1 < 4) || (deadCheckersP2 >= 3 && innerBoardStrengthP2 < 4))
            {
                return PositionType.Crunched;
            }

            var isBackgame = IsAnyBackgame(position);

            // 12, 13, 23 Backgames are the backgames with most critical timing
            if (isBackgame)
            {
                if (IsOneTwoBackgame(position))
                    return PositionType.Backgame12;
                if (IsOneThreeBackgame(position))
                    return PositionType.Backgame13;
                if (IsTwoThreeBackgame(position))
                    return PositionType.Backgame23;
                return PositionType.OtherBackgame;
            }

            var isBearOffAllowedP1 = IsBearOffAllowed(position, Player1);
            var isBearOffAllowedP2 = IsBearOffAllowed(position, Player2);

            // 2. Bear Off with contact
            if (isBearOffAllowedP1 || isBearOffAllowedP2)
            {
                //var backgammonBoard = new BackgammonBoard();
                //backgammonBoard.Position = position;
                //Console.WriteLine(backgammonBoard);


                if (isBackgame)
                {
                    return PositionType.BearOffVsBackgame;
                }

                if (position[AcePointP1] <= -2)
                {
                    if (player == Player1)
                    {
                        return PositionType.BearOffVs1Point;
                    }
                    else
                    {
                        return PositionType.BearOffVs1PointDefence;
                    }
                }

                if (position[AcePointP2] >= 2)
                {
                    if (player == Player2)
                    {
                        return PositionType.BearOffVs1Point;
                    }
                    else
                    {
                        return PositionType.BearOffVs1PointDefence;
                    }
                }

                if ((isBearOffAllowedP1 && player == Player1) || (isBearOffAllowedP2 && player == Player2))
                {
                    return PositionType.BearOffContact;
                }
                else
                {
                    return PositionType.BearOffContactDefence;
                }
            }

            var (checkersInOppHomeBoardP1, checkersInOppHomeBoardP2) = CheckersInOppHomeBoard(position);
            var (primeP1, primeStartsAtP1) = CountPrimes(position, Player1);
            var (primeP2, primeStartsAtP2) = CountPrimes(position, Player2);

            var advancedAnchorP1 = AdvancedAnchor(position, Player1);
            var advancedAnchorP2 = AdvancedAnchor(position, Player2);

            // In a mutual holding game the priming value is not so valuable
            if (advancedAnchorP1 && advancedAnchorP2)
            {
                return PositionType.MutualHoldingGame;
            }

            bool player1IsPrimed = primeP2 >= 3 && primeStartsAtP2 >= GoldenPointP2 && checkersInOppHomeBoardP1 > 0;
            bool player2IsPrimed = primeP1 >= 3 && primeStartsAtP1 <= GoldenPointP1 && checkersInOppHomeBoardP2 > 0;
            // 6 and 5 primes are outstanding strong so I think we should have neural nets for those primes
            // Atleast one player must have checkers that need to escape
            // 1. Six Prime P1 vs Six Prime
            // 1a. Six Prime P1 opp anchor vs Six Prime
            // 1b. Six Prime P1 and 1 or more to escape vs Six Prime
            // 1c. Six Prime P1 no trapped Vs Six Prime

            // SixPrimeContactP1VsSixPrimeContactP2
            // SixPrimeContactP1VsFivePrimeContactP2
            // SixPrimeContactP1_Ch1 P1 has A SixPrime and opponent has 1 Checker to escape
            // SixPrimeContactP1_Ch2 P1 has A SixPrime and opponent has 2 or more Checkers to escape
            // SixPrimeContactP2_Ch1 P2 has A SixPrime and opponent has 1 Checker to escape
            // SixPrimeContactP2_Ch2 P2 has A SixPrime and opponent has 2 or more Checker to escape

            // Haven't thought this through enough, when do we want 'prime vs prime' ?
            // Perhaps should have different prime VS prime Categories (primeVs6Prime, primeVs5Prime, primeVsPrime)
            if ((primeP1 == 6 && player2IsPrimed) || (primeP2 == 6 && player1IsPrimed))
            {
                // A 4 prime vs 6Prime still has good priming chances
                if (Math.Min(primeP1, primeP2) > 4 && player2IsPrimed && player1IsPrimed)
                {
                    return PositionType.PrimeVsPrime;
                }
                return PositionType.SixPrime;
            }

            if (((primeP1 == 5 && player2IsPrimed) || (primeP2 == 5 && player1IsPrimed)) && Math.Min(primeP2, primeP1) >= 4)
            {
                if (Math.Min(primeP1, primeP2) > 4 && player2IsPrimed && player1IsPrimed)
                {
                    return PositionType.PrimeVsPrime;
                }
                return PositionType.FivePrime;
            }

            if (player1IsPrimed && player2IsPrimed)
            {
                return PositionType.PrimeVsPrime;
            }

            if ((primeP1 == 4 && player2IsPrimed) || (primeP2 == 4 && player2IsPrimed))
            {
                return PositionType.FourPrime;
            }

            if (contactDistance < 6)
            {
                // When the contact is lower then 6 there will be much fewer shots
                return PositionType.WeakContact;
            }

            if (advancedAnchorP1 || advancedAnchorP2)
            {
                return PositionType.HoldingGame;
            }

            if (position[ThreePointP2] >= 2 || position[ThreePointP1] <= -2)
            {
                return PositionType.ButterFlyAnchor;
            }

            if (position[DeucePointP2] >= 2 || position[DeucePointP1] <= -2)
            {
                return PositionType.DeucePointAnchor;
            }

            var (pipCountP1, pipCountP2) = PipCountStatic(position);

            // Should maybe also take into account player on roll (worth 4 pip) and pip wastage
            // The race affects the strategy a lot but its hard to decide when we want to override other position types
            if (Math.Abs(pipCountP1 - pipCountP2) > 20)
            {
                return PositionType.BigRaceLead;
            }

            if (IsCompletedStage(position))
            {
                return PositionType.CompletedStage;
            }

            return PositionType.Contact;
        }

        public static bool IsOneTwoBackgame(int[] position)
        {
            if (position[AcePointP1] <= -2 && position[DeucePointP1] <= -2)
                return true;
            if (position[AcePointP2] >= 2 && position[DeucePointP2] >= 2)
                return true;
            return false;
        }

        public static bool IsOneThreeBackgame(int[] position)
        {
            if (position[AcePointP1] <= -2 && position[ThreePointP1] <= -2)
                return true;
            if (position[AcePointP2] >= 2 && position[ThreePointP2] >= 2)
                return true;
            return false;
        }

        public static bool IsTwoThreeBackgame(int[] position)
        {
            if (position[DeucePointP1] <= -2 && position[ThreePointP1] <= -2)
                return true;
            if (position[DeucePointP2] >= 2 && position[ThreePointP2] >= 2)
                return true;
            return false;
        }

        // We will call it a Backgame if opponent has any two points from ace to golden in opponents board
        public static bool IsAnyBackgame(int[] position)
        {
            var player2BackgamePoints = 0;
            var player1BackgamePoints = 0;
            for (int i = 0; i < 5; i++)
            {
                if (position[AcePointP1 + i] <= -2)
                    player2BackgamePoints++;
                if (position[AcePointP2 - i] >= 2)
                    player1BackgamePoints++;
            }
            return player1BackgamePoints >= 2 || player2BackgamePoints >= 2;
        }

        //
        public static bool IsCrunched(int[] position)
        {
            var player1CompletedStage = true;
            for (int i = OnTheBarP1; i > MidPointP1; i--)
            {
                if (position[i] > 0)
                {
                    player1CompletedStage = false;
                    break;
                }
            }

            if (player1CompletedStage)
            {
                return true;
            }

            for (int i = OnTheBarP2; i < MidPointP2; i++)
            {
                if (position[i] < 0)
                {
                    return false;
                }
            }
            return true;
        }

        // When a players last checker has reached the midpoint or further its a completed stage
        public static bool IsCompletedStage(int[] position)
        {
            var player1CompletedStage = true;
            for (int i = OnTheBarP1; i > MidPointP1; i--)
            {
                if (position[i] > 0)
                {
                    player1CompletedStage = false;
                    break;
                }
            }

            if (player1CompletedStage)
            {
                return true;
            }

            for (int i = OnTheBarP2; i < MidPointP2; i++)
            {
                if (position[i] < 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
