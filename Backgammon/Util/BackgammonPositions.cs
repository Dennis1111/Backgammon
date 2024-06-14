namespace Backgammon.Util
{
    public static class BackgammonPositions
    {
        public static readonly int[] OneTwoBackgame = [0, 0, 0, 0, 2, 2, 2, 2, 1, -1, 0, 0, 0, 2, -2, -2, 0, -2, -2, -2, -2, -2, 0, 2, 2, 0, 0, 0];
        public static readonly int[] OneTwoBackgameBadTiming = [0, 0, 0, 2, 2, 2, 2, 2, 1, -1, 0, 0, 0, 0, -2, -2, 0, -2, -2, -2, -2, -2, 0, 2, 2, 0, 0, 0];
        public static readonly int[] OneTwoBackgameBearoff = [0, 0, 0, 0, 2, 2, 2, 2, 1, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, -4, -4, -4, -3, 2, 2, 0, 0, 0];

        public static readonly int[] OneThreeBackgame = [0, 0, 0, 0, 2, 2, 2, 2, 1, -1, 0, 0, 0, 2, -2, -2, 0, -2, -2, -2, -2, -2, 2, 0, 2, 0, 0, 0];
        public static readonly int[] OneThreeBackgameBadTiming = [0, 0, 2, 2, 2, 2, 2, 1, 0, -1, 0, 0, 0, 0, -2, -2, 0, -2, -2, -2, -2, -2, 2, 0, 2, 0, 0, 0];
        public static readonly int[] OneThreeBackgameBearoff = [0, 0, 0, 0, 2, 2, 2, 2, 1, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, -4, -4, -4, 2, -3, 2, 0, 0, 0];

        public static readonly int[] TwoThreeBackgame = [0, 0, 0, 0, 0, 2, 2, 2, 2, -1, 0, 0, 0, 2, -2, -2, 0, -2, -2, -2, -2, -2, 3, 2, 0, 0, 0, 0];
        public static readonly int[] TwoThreeBackgameBadTiming = [0, 0, 0, 2, 2, 2, 2, 2, 0, -1, 0, 0, 0, 0, -2, -2, 0, -2, -2, -2, -2, -2, 3, 2, 0, 0, 0, 0];
        public static readonly int[] TwoThreeBackgameBearoff = [0, 0, 0, 0, 2, 2, 2, 2, 1, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, -4, -4, -4, 2, 2, -3, 0, 0, 0];
        public static readonly int[] TwoThreeBackgameWithTiming = [0, 0, 0, 0, 1, 2, 2, 1, 1, 0, 0, 1, 1, 2, -1, -2, 0, -3, -3, -2, -2, -2, 2, 2, 0, 0, 0, 0];

        public static readonly int[] TwoThreeBackGameSoonShot = [0, 0, 0, 0, 1, 2, 2, 1, 1, 0, 0, 1, 1, 2, 0, -1, 0, -3, -3, -2, -2, -2, 2, 2, -2, 0, 0, 0];

        // If you want to keep an array of all configurations together
        public static readonly int[][] BackGames =
        [
        OneTwoBackgame,
        OneTwoBackgameBadTiming,
        OneThreeBackgame,
        OneThreeBackgameBadTiming,
        TwoThreeBackgame,
        TwoThreeBackgameBadTiming,
        TwoThreeBackgameWithTiming,
        TwoThreeBackGameSoonShot,
        OneThreeBackgameBearoff,
        OneThreeBackgameBearoff,
        TwoThreeBackgameBearoff
        ];

        public static readonly int[] BarpointHoldingGame =
        [0, 0, 0, 0, 2, 2, 2, -2, 2, 0, 0, 0, -5, 5, 0, 0, 0, 0, -3, -3, 2, -2, 0, 0, 0, 0, 0, 0];

        public static readonly int[] ButterflyHoldingGame =
            [0, 0, 0, 0, 2, 2, 3, -2, 3, 0, 0, 0, -3, 3, 0, 0, 0, -2, -2, -2, -2, -2, 2, 0, 0, 0, 0, 0];

        public static readonly int[] FourPointHoldingGame =
            [0, 0, 0, 0, 2, 2, 3, -2, 3, 0, 0, 0, -3, 3, 0, 0, 0, -3, -2, -2, -2, 2, -1, 0, 0, 0, 0, 0];

        public static readonly int[] FivePointHoldingGame =
            [0, 0, 0, 0, 2, 2, 3, -2, 3, 0, 0, 0, -3, 3, 0, 0, 0, -2, -2, -3, 2, -2, -1, 0, 0, 0, 0, 0];

        public static readonly int[][] HoldingGames =
            [
            BarpointHoldingGame,
            ButterflyHoldingGame,
            FourPointHoldingGame,
            FivePointHoldingGame
            ];

        public static readonly int[] BearoffVs1Point =
          [0, 0, 0, 0, 0, 2, 3, 2, 3, 2, 1, 0, 0, 0, 0, 0, 0, 0, -3, -3, -3, -2, -2, -2, 2, 0, 0, 0];

        public static readonly int[] BearoffVs7Off =
            [-1, 2, 2, 2, 3, 3, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -3, -4, 0, 0, 0, -7];

        public static readonly int[] BearoffVs9Off =
            [-1, 2, 2, 2, 2, 3, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -2, -3, 0, 0, 2, -9];

        public static readonly int[] BearoffVs11Off =
            [-1, 2, 2, 2, 2, 3, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -3, 0, 0, 2, -11];

        public static readonly int[] BearOffVsOneOff =
            [-1, 2, 2, 2, 2, 3, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -3, -2, -2, -2, -2, -2, 0, 0, -1];
        public static readonly int[] BearOffVsZeroOff =
            [-1, 2, 2, 2, 3, 3, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, -2, -2, -3, -3, -3, 0, 0, 0];

        public static readonly int[] BearoffVs13Off2OnTheBar =
            [-2, 2, 2, 2, 3, 3, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -13];
        public static readonly int[] BearoffVs13Off3off2OnTheBar =
            [-2, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, -13];
        public static readonly int[] BearoffVs13Off4off2OnTheBar =
            [-2, 2, 2, 3, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4, -13];
        public static readonly int[] BearoffVs13Off8off2OnTheBar =
            [-2, 2, 3, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 8, -13];

        public static readonly int[][] BearOff13offGames = [
            BearoffVs13Off2OnTheBar,
            BearoffVs13Off3off2OnTheBar,
            BearoffVs13Off8off2OnTheBar
            ];
        
        public static readonly int[][] BearOffGames =
            [
            BearoffVs1Point,
            BearoffVs7Off,
            BearoffVs9Off,
            BearoffVs11Off,
            BearoffVs13Off2OnTheBar,
            BearOffVsOneOff,
            BearOffVsZeroOff
            ];

        public static readonly int[] TwoOffVsSixOff =
        [0, 2, 2, 2, 2, 2, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, -2, -1, -2, -2, -1, 0, 2, -6];

        public static readonly int[] FourOffVsSixOff =
            [0, 1, 2, 2, 1, 2, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, -2, -1, -2, -2, -1, 0, 4, -6];

        public static readonly int[] EightOffVsSixOff =
            [0, 0, 2, 1, 1, 1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, -2, -1, -2, -2, -1, 0, 8, -6];

        public static readonly int[] EightOffVsEightOff =
            [0, 0, 2, 1, 1, 1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -2, -1, -1, -1, 0, -1, -1, 0, 8, -8];

        public static readonly int[] TenOffVsOneInOutfield =
            [0, 1, 2, 2, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, -14];

        public static readonly int[] EightOffSaveGammon =
            [0, 1, 2, 2, 2, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, -1, -2, -3, -3, -2, -2, 0, 0, 8, 0];

        public static readonly int[] SevenOffSaveGammon =
            [0, 1, 2, 2, 2, 1, -1, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, -1, -2, -3, -3, -2, -2, 0, 0, 7, 0];

        public static readonly int[] LotsOffGammon =
            [0, 1, 2, 2, 2, 1, 0, -1, 0, 0, 0, -1, 0, -1, 0, 0, -1, 0, -1, -1, -2, -3, -2, -2, 0, 0, 7, 0];

        public static readonly int[] SomeBackgammons =
            [0, 1, 2, 2, 1, -3, -2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, -1, -2, -2, -2, -2, 0, 0, 9, 0];

        public static readonly int[] StartingToBearOff =
            [0, 0, 1, 2, 3, 4, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, -3, -3, -3, -2, -1, -2, 0, 0, 0];
        public static readonly int[] LongRace =
            [0, 0, 0, 0, 3, 3, 2, 0, 1, 2, 2, 2, 0, 0, -3, -3, -1, 0, -1, -1, -1, -1, -2, -1, -1, 0, 0, 0];

        public static readonly int[][] BearOffGamesNoContact =
            [
            TwoOffVsSixOff,
            FourOffVsSixOff,
            EightOffVsSixOff,
            EightOffVsEightOff,
            TenOffVsOneInOutfield,
            EightOffSaveGammon,
            SevenOffSaveGammon,
            LotsOffGammon,
            SomeBackgammons,
            StartingToBearOff
            ];

        public static readonly int[] ThirteenOffFishing =
         [0, -1, 2, 2, 2, 3, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, -1, 0, 0, -13];

        public static readonly int[] ThirteenOffForcedFishing =
            [-1, 1, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, -1, 0, 0, -13];

        public static readonly int[] TwelveOffFishing =
            [0, -1, 2, 2, 2, 3, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, -1, -1, 0, 0, -12];

        public static readonly int[] CoupClassique =
            [0, 0, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, -3, 1, 0, 0, -12];

        public static readonly int[][] FishingGames =
            [
            ThirteenOffFishing,
            ThirteenOffForcedFishing,
            TwelveOffFishing,
            CoupClassique
            ];

        public static readonly int[] TwoOneSlotOpening =
            [0, -2, 0, 0, 0, 1, 4, 0, 3, 0, 0, 1, -5, 4, 0, 0, 0, -3, 0, -5, 0, 0, 0, 0, 2, 0, 0, 0];

        public static readonly int[] TwoOneSplitOpening =
            [0, -2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 1, -5, 4, 0, 0, 0, -3, 0, -5, 0, 0, 0, 1, 1, 0, 0, 0];

        public static readonly int[] TwoThreeSplitOpening =
           [0, -2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 1, -5, 4, 0, 0, 0, -3, 0, -5, 0, 1, 0, 0, 1, 0, 0, 0];

        public static readonly int[] SixTwoSplitOpening =
            [0, -2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 1, -5, 4, 0, 0, 0, -3, 1, -5, 0, 0, 0, 0, 1, 0, 0, 0];

        public static readonly int[] FourThreeSplitOpening =
            [0, -2, 0, 0, 0, 0, 5, 0, 3, 1, 0, 0, -5, 4, 0, 0, 0, -3, 0, -5, 0, 1, 0, 0, 1, 0, 0, 0];

        public static readonly int[] FourTwoPOpening =
            [0, -2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, -5, 5, 0, 0, 0, -2, 0, -4, 0, -2, 0, 0, 2, 0, 0, 0];

        public static readonly int[] Nackgammon =
            [0, -2, -2, 0, 0, 0, 4, 0, 3, 0, 0, 0, -4, 4, 0, 0, 0, -3, 0, -4, 0, 0, 0, 2, 2, 0, 0, 0];


        public static readonly int[][] OpeningGames =
            [
            TwoOneSlotOpening,
            TwoOneSplitOpening,
            TwoThreeSplitOpening,
            FourThreeSplitOpening,
            SixTwoSplitOpening,
            FourTwoPOpening,
            Nackgammon
            ];

        public static readonly int[] SevenOffGame =
            [0, 0, -1, 0, 2, 2, 2, 2, 3, 2, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, -3, -4, 1, 0, 0, -7];

        public static readonly int[] FiveOffRollThePrime =
            [0, -1, 0, 0, 1, 2, 2, 3, 2, 2, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -3, -3, -3, 0, 0, -5];
        public static readonly int[] TwoOffRollThePrime =
            [0, -1, 0, 0, 0, 2, 2, 2, 3, 2, 3, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, -3, -3, -3, -3, 0, 0, -2];
        public static readonly int[] OneOffRollThePrime =
            [0, -1, 0, 0, 1, 2, 2, 2, 2, 2, 2, 1, 0, 1, 0, 0, 0, 0, 0, -1, -1, -2, -3, -3, -3, 0, 0, -1];
        public static readonly int[] OneOffRollThePrimeFrom4P =
            [0, -1, 0, 1, 2, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, -1, -1, -2, -3, -3, -3, 0, 0, -1];
        public static readonly int[] OneOffRollThePrimeFrom3P =
            [0, -1, 0, 2, 2, 2, 2, 2, 2, 2, 1   , 0, 0, 0, 0, 0, 0, 0, 0, -1, -1, -2, -3, -3, -3, 0, 0, -1];
        public static readonly int[] OneOffRollThePrimeFrom2P =
            [0, -1, 2, 2, 2, 2, 2, 2, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, -1, -2, -3, -3, -3, 0, 0, -1];
        public static readonly int[] SixPrime1PointSlotted =
            [-1, 1, 2, 2, 2, 2, 2, 3, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, -1, -2, -3, -3, -3, 0, 0, -1];
        public static readonly int[] FivePrime1PointSlotted =
            [-1, 1, 2, 2, 2, 3, 3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, -1, -2, -3, -3, -3, 0, 0, -1];
        
        public static readonly int[][] RollThePrime =
            [
            SevenOffGame,
            FiveOffRollThePrime,
            TwoOffRollThePrime,
            OneOffRollThePrime,
            OneOffRollThePrimeFrom4P,
            OneOffRollThePrimeFrom3P,
            OneOffRollThePrimeFrom2P,
            SixPrime1PointSlotted,
            FivePrime1PointSlotted
            //  AddOneBearoff,
            ];

        public static int[][] MergeArrays(int[][] firstArray, int[][] secondArray)
        {
            return firstArray.Concat(secondArray).ToArray();
        }
    }
}