using static Backgammon.Models.BackgammonBoard;

namespace Backgammon.Analysis
{
    public static class BoardFeatures
    {
        public static int CountSafePoints(int[] position, int player)
        {
            int safeCount = 0;
            for (int point = 1; point <= 24; point++)
            {
                if ((player == Player1 && position[point] >= 2) || (player == Player2 && position[point] <= -2))
                {
                    safeCount++;
                }
            }
            return safeCount;
        }


        /// <summary>
        /// Return the longest prime
        /// </summary>
        /// <param name="position"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        public static (int longestPrime, int startsAtIndex) CountPrimes(int[] position, int player)
        {
            int longestPrime = 0;
            int currentPrime = 0;
            // When the prime has reached the 1 Point it doesn't have the same classification value
            // For instance A 4 prime that starts at the 1 point just needs a 5 from the bar to escape
            // So just loop to the 23 Point
            // Need some more consideration here...

            int primeStartsAt = 0;// The prime starts with the point nearest to bear off
            if (player == Player2)
            {
                for (int i = MidPointP2; i <= AcePointP2; i++)
                {
                    if (position[i] <= -2)
                    {
                        currentPrime++;
                    }
                    else
                    {
                        if (currentPrime >= longestPrime)
                        {
                            if (currentPrime <= 2 && i - 1 >= FourPointP2)
                            {
                                continue;// We don't want to treat small deep primes like a prime, like if 32 and 2 point is take early it has no prime value
                            }
                            longestPrime = currentPrime;
                            primeStartsAt = i - 1;
                        }
                        currentPrime = 0;
                    }
                }
                return (longestPrime, primeStartsAt);
            }

            for (int i = MidPointP1; i >= AcePointP1; i--)
            {
                if (position[i] >= 2)
                {
                    currentPrime++;
                }
                else
                {
                    if (currentPrime >= longestPrime)
                    {
                        if (currentPrime <= 2 && i + 1 <= FourPointP1)
                        {
                            continue;// We don't want to treat small deep primes like a prime, like if 32 and 2 point is take early it has no prime value
                        }
                        longestPrime = currentPrime;
                        primeStartsAt = i + 1;
                    }
                    currentPrime = 0;
                }
            }
            return (longestPrime, primeStartsAt);
        }

        /// <summary>
        /// Find the max number of points taken within 6 points
        /// It's possible this will not be a prime at all like the sequence xx_x_x would return 4 though only a 2 prime
        /// First create a window a size 6 and count the amount of points, then slide the window forward
        /// </summary>
        /// <param name="position"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        public static (int longestPrime, int startsAtIndex) CountBrokenPrimes(int[] position, int player)
        {
            const int windowSize = 6;
            int currentWindowCount = 0;

            if (player == Player2)
            {
                int primeStartsAtP2 = MidPointP2;
                int largestBrokenPrimeP2 = 0;

                // Initialize the first window of size 6
                for (int i = MidPointP2; i < MidPointP2 + windowSize; i++)
                {
                    if (position[i] <= -2)
                    {
                        currentWindowCount++;
                    }
                }
                largestBrokenPrimeP2 = currentWindowCount;

                // Slide the window across the board
                for (int i = MidPointP2 + windowSize; i <= AcePointP2; i++)
                {
                    // Remove the influence of the element that's exiting the window
                    if (position[i - windowSize] <= -2)
                    {
                        currentWindowCount--;
                    }
                    // Add the influence of the new element entering the window
                    if (position[i] <= -2)
                    {
                        currentWindowCount++;
                    }
                    // Update the largest broken prime if current window count is greater
                    if (currentWindowCount > largestBrokenPrimeP2)
                    {
                        largestBrokenPrimeP2 = currentWindowCount;
                        primeStartsAtP2 = i - windowSize + 1; // Update the start index
                    }
                }
                return (largestBrokenPrimeP2, primeStartsAtP2);
            }

            // Assume Player1 if not Player2
            int primeStartsAtP1 = MidPointP1;
            int largestBrokenPrimeP1 = 0;
            currentWindowCount = 0;

            // Initialize the first window of size 6
            for (int i = MidPointP1; i > MidPointP1 - windowSize; i--)
            {
                if (position[i] >= 2)
                {
                    currentWindowCount++;
                }
            }
            largestBrokenPrimeP1 = currentWindowCount;

            // Slide the window across the board
            for (int i = MidPointP1 - windowSize; i >= AcePointP1; i--)
            {
                // Remove the influence of the element that's exiting the window
                if (position[i + windowSize] >= 2)
                {
                    currentWindowCount--;
                }
                // Add the influence of the new element entering the window
                if (position[i] >= 2)
                {
                    currentWindowCount++;
                }
                // Update the largest broken prime if current window count is greater
                if (currentWindowCount > largestBrokenPrimeP1)
                {
                    largestBrokenPrimeP1 = currentWindowCount;
                    primeStartsAtP1 = i; // Update the start index
                }
            }
            return (largestBrokenPrimeP1, primeStartsAtP1);
        }

        /*public static (int primeLength, int frontPoint) CountPrimesWithPos(int[] position, int player)
        {
            int longestPrime = 0;
            int currentPrime = 0;

            if (player == Player1) { 
            
            }
            for (int i = 1; i <= 24; i++)
            {
                if ((player == Player1 && position[i] >= 2) || (player == Player2 && position[i] <= -2))
                {
                    currentPrime++;
                }
                else
                {
                    if (currentPrime > longestPrime)
                    {

                    }
                    longestPrime = Math.Max(longestPrime, currentPrime);

                    currentPrime = 0;
                }
            }

            longestPrime = Math.Max(longestPrime, currentPrime);
            return longestPrime;
        }*/

        public static int CountBlots(int[] position, int player)
        {
            int blotCount = 0;
            for (int i = 1; i <= 24; i++)
            {
                if ((player == Player1 && position[i] == 1) || (player == Player2 && position[i] == -1))
                {
                    blotCount++;
                }
            }
            return blotCount;
        }



        public static (int deadCheckersP1, int deadCheckersP2) CountBlots(int[] points)
        {
            // Lets check 1 to 3 point for dead checkers and also checkers taken off
            var deadCheckersP1 = points[BearOffP1];
            for (int i = 0; i < 3; i++)
            {
                var checkers = points[AcePointP1 + i];
                if (checkers < 2)
                {
                    break;
                }
                if (checkers > 2)
                {
                    deadCheckersP1 += checkers - 2;
                }
            }

            // Lets check 1 to 3 points for dead checkers
            var deadCheckersP2 = -points[BearOffP2];
            for (int i = 0; i < 3; i++)
            {
                var checkers = -points[AcePointP2 - i];
                if (checkers < 2)
                {
                    break;
                }
                if (checkers > 2)
                {
                    deadCheckersP2 += checkers - 2;
                }
            }
            return (deadCheckersP1, deadCheckersP2);
        }

        public static (int deadCheckersP1, int deadCheckersP2) DeadCheckers(int[] points)
        {
            // Lets check 1 to 3 point for dead checkers and also checkers taken off
            var deadCheckersP1 = points[BearOffP1];
            for (int i = 0; i < 3; i++)
            {
                var checkers = points[AcePointP1 + i];
                if (checkers < 2)
                {
                    break;
                }
                if (checkers > 2)
                {
                    deadCheckersP1 += checkers - 2;
                }
            }

            // Lets check 1 to 3 points for dead checkers
            var deadCheckersP2 = -points[BearOffP2];
            for (int i = 0; i < 3; i++)
            {
                var checkers = -points[AcePointP2 - i];
                if (checkers < 2)
                {
                    break;
                }
                if (checkers > 2)
                {
                    deadCheckersP2 += checkers - 2;
                }
            }
            return (deadCheckersP1, deadCheckersP2);
        }

        public static (int keithP1, int keithP2) KeithPenalty(int[] position)
        {
            int keithPenaltyP1 = 0;
            if (position[AcePointP1] > 1)
            {
                keithPenaltyP1 += (position[AcePointP1] - 1) * 2;
            }
            if (position[DeucePointP1] > 1)
            {
                keithPenaltyP1 += position[AcePointP1] - 1;
            }
            if (position[ThreePointP1] > 3)
            {
                keithPenaltyP1 += position[AcePointP1] - 1;
            }
            if (position[FourPointP1] == 0)
            {
                keithPenaltyP1++;
            }
            if (position[GoldenPointP1] == 0)
            {
                keithPenaltyP1++;
            }
            if (position[SixPointP1] == 0)
            {
                keithPenaltyP1++;
            }

            int keithPenaltyP2 = 0;
            if (-position[AcePointP2] > 1)
            {
                keithPenaltyP2 += (-position[AcePointP1] - 1) * 2;
            }
            if (-position[DeucePointP2] > 1)
            {
                keithPenaltyP2 += -position[AcePointP1] - 1;
            }
            if (-position[ThreePointP2] > 3)
            {
                keithPenaltyP2 += -position[AcePointP1] - 1;
            }
            if (position[FourPointP2] == 0)
            {
                keithPenaltyP2++;
            }
            if (position[GoldenPointP2] == 0)
            {
                keithPenaltyP2++;
            }
            if (position[SixPointP2] == 0)
            {
                keithPenaltyP2++;
            }
            return (keithPenaltyP1, keithPenaltyP2);
        }

        //If any of the points 4567 is take we have a strong holding game (The 6 point will be a very rare case)
        public static bool AdvancedAnchor(int[] position, int player)
        {
            if (player == Player1)
            {
                for (int i = FourPointP2; i >= BarPointP2; i--)
                {
                    if (position[i] >= 2)
                    {
                        return true;
                    }
                }
                return false;
            }

            for (int i = FourPointP1; i <= BarPointP1; i++)
            {
                if (position[i] <= -2)
                {
                    return true;
                }
            }
            return false;
        }

        public static (int itz1, int itz2) CheckersInTheZone(int[] position)
        {
            var CheckersinTheZoneP1 = 0;
            var CheckersinTheZoneP2 = 0;
            for (int i = AcePointP1; i <= AcePointP1 + 10; i++)
            {
                if (position[i] > 0)
                {
                    CheckersinTheZoneP1 += position[i];
                }
            }

            for (int i = AcePointP2; i >= AcePointP2 - 10; i--)
            {
                if (-position[i] > 0)
                {
                    CheckersinTheZoneP2 -= position[i];
                }
            }
            return (CheckersinTheZoneP1, CheckersinTheZoneP2);
        }

        public static (int, int) CheckersInOppHomeBoard(int[] position)
        {
            var checkersInOppHomeBoardP1 = 0;
            for (int i = OnTheBarP1; i >= SixPointP2; i--)
            {
                if (position[i] > 0)
                {
                    checkersInOppHomeBoardP1 += position[i];
                }
            }
            var checkersInOppHomeBoardP2 = 0;
            for (int i = OnTheBarP2; i <= SixPointP1; i++)
            {
                if (position[i] < 0)
                {
                    checkersInOppHomeBoardP2 -= position[i];
                }
            }

            return (checkersInOppHomeBoardP1, checkersInOppHomeBoardP2);
        }

        public static int CountInnerBoardPoints(int[] board, int player)
        {
            int safeCount = 0;
            for (int point = 1; point <= 6; point++)
            {
                if ((player == Player1 && board[point] >= 2) || (player == Player2 && board[point + 18] <= -2))
                {
                    safeCount++;
                }
            }
            return safeCount;
        }

        // When opp have an anchor on 345 point it can be quite valuable to have many outfield blocking point
        // But for consistance lets do a check on 54321 points
        // I feel like it's hard for the nn to learn to take outfield broken prime which is high prio when we were behind
        // It's of cause valuable for blocking a single blot also
        // Search for the most advanced anchor and then find out how blocked it is but if no advanced anchor use a single blot instead
        public static int LastAnchorOrCheckerIsBlocked(int[] position, int blockedPlayer)
        {
            //int[] blockedPoints = new int[6]; Could be valuable to know which points are blocking also
            int blockingCount = 0;
            int blockedPoint = 0;
            if (blockedPlayer == Player1)
            {
                for (int i = GoldenPointP2; i <= AcePointP2; i++)
                {
                    if (position[i] >= 2)
                    {
                        blockedPoint = i;
                        break;
                    }
                }

                //If no anchor is found search for the first blot instead
                if (blockedPoint == 0)
                {
                    for (int i = GoldenPointP2; i <= AcePointP2; i++)
                    {
                        if (position[i] == 1)
                        {
                            blockedPoint = i;
                            break;
                        }
                    }
                }

                // No checker to block have been found
                if (blockedPoint == 0)
                {
                    return 0;
                }

                for (int i = 1; i <= 6; i++)
                {
                    if (position[blockedPoint - i] <= -2)
                    {
                        blockingCount++;
                    }
                }
                return blockingCount;
            }

            // Find out how blocked Player 2 is
            for (int i = GoldenPointP1; i >= AcePointP1; i--)
            {
                if (position[i] <= -2)
                {
                    blockedPoint = i;
                    break;
                }
            }

            // If no anchor is found search for the first blot instead
            if (blockedPoint == 0)
            {
                for (int i = GoldenPointP1; i >= AcePointP1; i--)
                {
                    if (position[i] == -1)
                    {
                        blockedPoint = i;
                        break;
                    }
                }
            }

            // No checker to block have been found
            if (blockedPoint == 0)
            {
                return 0;
            }

            for (int i = 1; i <= 6; i++)
            {
                if (position[blockedPoint + i] >= 2)
                {
                    blockingCount++;
                }
            }
            return blockingCount;
        }

        // To bear off safely its valuable to count the checkers at the 2 last points, often an even number like 22,33 is good (22 can leave a double shot though)
        // 23 is ok (leaves a shot only to large double) while 32 is much worse
        public static (int last, int secondLast) CountCheckersAtTheBack(int[] position, int player)
        {
            bool checkingLastPoint = true;

            if (player == Player1)
            {
                int lastP1 = 0;
                for (int i = SixPointP1; i >= AcePointP1; i--)
                {
                    if (position[i] > 0)
                    {
                        if (checkingLastPoint)
                        {
                            lastP1 = position[i];
                            checkingLastPoint = false;
                        }
                        else
                        {
                            return (lastP1, position[i]);
                        }
                    }
                }
                // Only one point to clear
                return (lastP1, 0);
            }
            int lastP2 = 0;
            checkingLastPoint = true;
            for (int i = SixPointP2; i <= AcePointP2; i++)
            {
                if (position[i] < 0)
                {
                    if (checkingLastPoint)
                    {
                        lastP2 = position[i];
                        checkingLastPoint = false;
                    }
                    else
                    {
                        return (lastP2, position[i]);
                    }
                }
            }
            // Only one point to clear
            return (lastP2, 0);
        }

        /// <summary>
        /// From opponents most backward checker lets find the largest 'Gap'. lets count all points that are not 'safe' as part of gaps
        /// The risk of leaving blots in the BearOff depends on largest gapSize 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="player"></param>
        /// <param name="lastCheckerP1"></param>
        /// <param name="lastCheckerP2"></param>
        /// <returns></returns>
        public static int BearOffMaxGap(int[] position, int player, int lastCheckerP1, int lastCheckerP2)
        {
            int largestGap = 0;

            if (player == Player1)
            {
                for (int i = lastCheckerP1; i > lastCheckerP2; i--)
                {
                    if (position[i] >= 2)
                    {
                        int gapCount = 0;
                        for (int gapPointCand = i - 1; gapPointCand > lastCheckerP2; gapPointCand--)
                        {
                            if (position[gapPointCand] < 2)
                            {
                                gapCount++;
                                if (gapCount > largestGap)
                                {
                                    largestGap = gapCount;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
            else if (player == Player2)
            {
                for (int i = lastCheckerP2; i < lastCheckerP1; i++)
                {
                    if (position[i] <= -2)
                    {
                        int gapCount = 0;
                        for (int gapPointCand = i + 1; gapPointCand < lastCheckerP1; gapPointCand++)
                        {
                            if (position[gapPointCand] > -2)
                            {
                                gapCount++;
                                if (gapCount > largestGap)
                                {
                                    largestGap = gapCount;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }

            return largestGap;
        }
    }
}
