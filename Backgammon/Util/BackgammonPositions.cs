namespace Backgammon.Util
{
    public static class BackgammonPositions
    {
        public static readonly int[] OneTwoBackgame = [0, 0, 0, 0, 2, 2, 2, 2, 1, -1, 0, 0, 0, 2, -2, -2, 0, -2, -2, -2, -2, -2, 0, 2, 2, 0, 0, 0];
        public static readonly int[] OneTwoBackgameBadTiming = [0, 0, 0, 2, 2, 2, 2, 2, 1, -1, 0, 0, 0, 0, -2, -2, 0, -2, -2, -2, -2, -2, 0, 2, 2, 0, 0, 0];
        public static readonly int[] OneTwoBackgameBearOff = [0, 0, 0, 0, 2, 2, 2, 2, 1, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, -4, -4, -4, -3, 2, 2, 0, 0, 0];

        public static readonly int[] OneThreeBackgame = [0, 0, 0, 0, 2, 2, 2, 2, 1, -1, 0, 0, 0, 2, -2, -2, 0, -2, -2, -2, -2, -2, 2, 0, 2, 0, 0, 0];
        public static readonly int[] OneThreeBackgameBadTiming = [0, 0, 2, 2, 2, 2, 2, 1, 0, -1, 0, 0, 0, 0, -2, -2, 0, -2, -2, -2, -2, -2, 2, 0, 2, 0, 0, 0];
        public static readonly int[] OneThreeBackgameBearOff = [0, 0, 0, 0, 2, 2, 2, 2, 1, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, -4, -4, -4, 2, -3, 2, 0, 0, 0];

        public static readonly int[] TwoThreeBackgame = [0, 0, 0, 0, 0, 2, 2, 2, 2, -1, 0, 0, 0, 2, -2, -2, 0, -2, -2, -2, -2, -2, 3, 2, 0, 0, 0, 0];
        public static readonly int[] TwoThreeBackgameBadTiming = [0, 0, 0, 2, 2, 2, 2, 2, 0, -1, 0, 0, 0, 0, -2, -2, 0, -2, -2, -2, -2, -2, 3, 2, 0, 0, 0, 0];
        public static readonly int[] TwoThreeBackgameBearOff = [0, 0, 0, 0, 2, 2, 2, 2, 1, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, -4, -4, -4, 2, 2, -3, 0, 0, 0];
        public static readonly int[] TwoThreeBackgameWithTiming = [0, 0, 0, 0, 1, 2, 2, 1, 1, 0, 0, 1, 1, 2, -1, -2, 0, -3, -3, -2, -2, -2, 2, 2, 0, 0, 0, 0];

        public static readonly int[] TwoThreeBackGameSoonShot = [0, 0, 0, 0, 1, 2, 2, 1, 1, 0, 0, 1, 1, 2, 0, -1, 0, -3, -3, -2, -2, -2, 2, 2, -2, 0, 0, 0];
        public static readonly int[] TwoFourBackgame = [0, 0, 0, 0, 1, 2, 2, 1, 1, 0, 0, 1, 1, 2, 0, -1, 0, -3, -3, -2, -2, 2, -2, 2, -2, 0, 0, 0];
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
        OneThreeBackgameBearOff,
        OneThreeBackgameBearOff,
        TwoThreeBackgameBearOff
        ];

        public static readonly int[] BarPointMutualHoldingGame =
        [0, 0, 0, 0, 2, 2, 2, -2, 2, 0, 0, 0, -5, 5, 0, 0, 0, 0, -3, -3, 2, -2, 0, 0, 0, 0, 0, 0];
        public static readonly int[] BarPointHoldingGameLittleTiming =
        [0, 0, 2, 2, 2, 3, 2, 2, 0, 0, 0, 0, -2, 0, 0, 0, 0, -2, 2, -4, -3, -1, -2, 0, -1, 0, 0, 0];

        public static readonly int[] BarPointVsButterflyHoldingGame =
            [0, 0, 0, 0, 2, 2, 3, -2, 3, 0, 0, 0, -3, 3, 0, 0, 0, -2, -2, -2, -2, -2, 2, 0, 0, 0, 0, 0];
        public static readonly int[] DeucePointVsFourPointHoldingGame =
            [0, -1, 1, 3, -2, -1, 4, 0, 3, 0, 0, 0, 0, 2, -1, 0, 0, 0, 0, -2, -2, -2, -2, 2, -2, 0, 0, 0];

        public static readonly int[] FourPointVsBarPointMutualHoldingGame =
            [0, 0, 0, 0, 2, 2, 3, -2, 3, 0, 0, 0, -3, 3, 0, 0, 0, -3, -2, -2, -2, 2, -1, 0, 0, 0, 0, 0];

        public static readonly int[] FourAndTenPointOppSmallCrunchHoldingGame =
            [0, 0, 4, 4, -2, 2, 0, 0, 2, 0, -2, 0, 0, 2, 0, 0, 0, 1, 0, -3, -2, -2, -2, 0, -2, 0, 0, 0];
        public static readonly int[] GoldenPointOppBigRaceLeadHoldingGame =
            [0, -1, 0, 0, 0, 0, 5, 0, 3, 0, 1, 0, -2, 4, 0, 0, 0, -3, -4, -5, 2, 0, 0, 0, 0, 0, 0, 0];

        public static readonly int[] GoldenPointCloseRaceHoldingGame =
            [0, -1, -1, 0, 0, 0, 5, 0, 3, 0, 2, 0, -2, 3, 0, 0, 0, -3, -4, -4, 2, 0, 0, 0, 0, 0, 0, 0];

        public static readonly int[] GoldenPoint4PrimeHoldingGame =
            [0, -1, -1, 0, 0, 2, 2, 3, 2, 0, 1, 0, -2, 3, 0, 0, 0, -3, -4, -4, 2, 0, 0, 0, 0, 0, 0, 0];

        public static readonly int[] GoldenPoint4PrimeOppNotSplitHoldingGame =
            [0, -2, 0, 0, 0, 2, 2, 3, 2, 0, 1, 0, -2, 3, 0, 0, 0, -3, -4, -4, 2, 0, 0, 0, 0, 0, 0, 0];
        public static readonly int[] GoldenPointMutualHoldingGame =
            [0, 0, 0, 0, 2, 2, 3, -2, 3, 0, 0, 0, -3, 3, 0, 0, 0, -2, -2, -3, 2, -2, -1, 0, 0, 0, 0, 0];

        public static readonly int[][] HoldingGames =
            [
            BarPointMutualHoldingGame,
            BarPointVsButterflyHoldingGame,
            DeucePointVsFourPointHoldingGame,
            FourAndTenPointOppSmallCrunchHoldingGame,
            FourPointVsBarPointMutualHoldingGame,
            GoldenPointOppBigRaceLeadHoldingGame,
            GoldenPointCloseRaceHoldingGame,
            GoldenPoint4PrimeHoldingGame,
            GoldenPoint4PrimeOppNotSplitHoldingGame,
            GoldenPointMutualHoldingGame,
            ];

        public static readonly int[] BearingInWithPrimeVs1PointPlayer2 =
          [0, 0, 0, 0, 0, 2, 3, 2, 3, 2, 1, 0, 0, 0, 0, 0, 0, 0, -3, -3, -3, -2, -2, -2, 2, 0, 0, 0];

        public static readonly int[] BearingOffWithVs1PointPlayer2 =
          [0, 0, 0, 0, 0, 2, 3, 2, 3, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0, -4, -4, -3, -2, -2, 2, 0, 0, 0];
        
        public static readonly int[] BearOffVs7Off =
            [-1, 2, 2, 2, 3, 3, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -3, -4, 0, 0, 0, -7];

        public static readonly int[] BearOffVs9Off =
            [-1, 2, 2, 2, 2, 3, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -2, -3, 0, 0, 2, -9];

        public static readonly int[] BearOffVs11Off =
            [-1, 2, 2, 2, 2, 3, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -3, 0, 0, 2, -11];

        public static readonly int[] BearOffVsOneOffCloseOut =
            [-1, 2, 2, 2, 2, 3, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -3, -2, -2, -2, -2, -2, 0, 0, -1];
        public static readonly int[] BearOffVsZeroOff =
            [-1, 2, 2, 2, 3, 3, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, -2, -2, -3, -3, -3, 0, 0, 0];

        public static readonly int[] BearOffVs13Off2OnTheBar =
            [-2, 2, 2, 2, 3, 3, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -13];
        public static readonly int[] BearOffVs13Off3off2OnTheBar =
            [-2, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, -13];
        public static readonly int[] BearOffVs13Off4off2OnTheBar =
            [-2, 2, 2, 3, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4, -13];
        public static readonly int[] BearOffVs13Off8off2OnTheBar =
            [-2, 2, 3, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 8, -13];

        public static readonly int[][] BearOff13offGames = [
            BearOffVs13Off2OnTheBar,
            BearOffVs13Off3off2OnTheBar,
            BearOffVs13Off8off2OnTheBar
            ];
        
        public static readonly int[][] BearOffGames =
            [
            BearingInWithPrimeVs1PointPlayer2,
            BearingOffWithVs1PointPlayer2,
            BearOffVs7Off,
            BearOffVs9Off,
            BearOffVs11Off,
            BearOffVs13Off2OnTheBar,
            BearOffVsOneOffCloseOut,
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

        public static readonly int[] TwoThreeDownOpening =
           [0, -2, 0, 0, 0, 0, 5, 0, 3, 0, 1, 1, -5, 3, 0, 0, 0, -3, 0, -5, 0, 0, 0, 0, 2, 0, 0, 0];
        
        public static readonly int[] FourThreeSplitOpening =
            [0, -2, 0, 0, 0, 0, 5, 0, 3, 1, 0, 0, -5, 4, 0, 0, 0, -3, 0, -5, 0, 1, 0, 0, 1, 0, 0, 0];
        
        public static readonly int[] FourThreeDownOpening =
            [0, -2, 0, 0, 0, 0, 5, 0, 3, 1, 1, 0, -5, 3, 0, 0, 0, -3, 0, -5, 0, 0, 0, 0, 2, 0, 0, 0];
        public static readonly int[] FourFiveSplitOpening =
            [0, -2, 0, 0, 0, 0, 5, 0, 4, 0, 0, 0, -5, 4, 0, 0, 0, -3, 0, -5, 1, 0, 0, 0, 1, 0, 0, 0];
        public static readonly int[] FourTwoPOpening =
            [0, -2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, -5, 5, 0, 0, 0, -2, 0, -4, 0, -2, 0, 0, 2, 0, 0, 0];
        public static readonly int[] SixTwoSplitOpening =
            [0, -2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 1, -5, 4, 0, 0, 0, -3, 1, -5, 0, 0, 0, 0, 1, 0, 0, 0];

        public static readonly int[] NackGammon =
            [0, -2, -2, 0, 0, 0, 4, 0, 3, 0, 0, 0, -4, 4, 0, 0, 0, -3, 0, -4, 0, 0, 0, 2, 2, 0, 0, 0];

        public static readonly int[] SixPrimeVsSixPrimeTimingTrouble = 
            [0, -2, 0, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 1, 0, -3, -2, -2, -2, -2, -2, 2, 0, 0, 0];
        public static readonly int[] SixPrimeVsSixPrimeCloseTiming =
            [0, -2, 0, 2, 2, 2, 2, 2, 3, 0, -1, 0, 0, 0, 0, 0, 0, 0, -2, -2, -2, -2, -2, -2, 2, 0, 0, 0];
        public static readonly int[] SixPrimeDeucePointVsSixPrimeTimingTrouble =
            [0, 0, -2, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 1, 0, -3, -2, -2, -2, -2, -2, 2, 0, 0, 0];
        public static readonly int[] SixPrimeVsFivePrimeTimingTrouble =
            [0, 0, -2, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, -3, -3, -3, -2, -2, 2, 0, 0, 0];
        public static readonly int[] FivePrimeDeucePointVsFivePrimeTimingTrouble =
            [0, 0, -2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 2, 1, 0, 0, -3, -3, -3, -2, -2, 2, 0, 0, 0];
        public static readonly int[] ThreePrimeVsThreePrimeBothOnAcePoint =
            [0, -2, 2, 0, 3, 2, 3, 0, 1, 0, 0, 0, -2, 2, 0, 0, 0, -2, 0, -2, -4, -3, 0, 0, 2, 0, 0, 0];
        public static readonly int[] ThreePrimeVsThreePrimeOppRunning =
            [0, -1, 2, 0, 3, 2, 3, 0, 1, -1, 0, 0, -2, 2, 0, 0, 0, -2, 0, -2, -4, -3, 0, 0, 2, 0, 0, 0];
        public static readonly int[] OnTheBar3PrimeAndBlot =
            [-1, 0, 2, 1, 3, 2, 3, 0, 1, -1, 0, 0, 0, 0, 0, 0, 0, 1, -2, -4, -2, -2, 0, -3, 2, 0, 0, 0];

        // Computer split to seldom so setup som positions
        public static readonly int[] Split24to23IntoPrimingGame =
            [0, -2, 0, 0, 2, 2, 3, 0, 1, 2, 0, 0, -2, 3, 0, -2, 0, -2, 0, -2, -3, 0, -2, 1, 1, 0, 0, 0];
        public static readonly int[] Split24to23IntoPrimingGame2 =
            [0, -1, -1, 0, 0, 2, 3, 0, 3, 2, 0, 0, -4, 3, 0, -1, 0, -1, 0, -3, -2, 0, -2, 0, 2, 0, 0, 0];
        public static readonly int[] Split24to21IntoPrimingGame =
            [0, -1, 0, 2, -1, 2, 3, 0, 3, 0, 0, 0, -2, 3, -2, -1, 0, -2, 0, -4, 0,-2, 0, 0, 2, 0, 0, 0];
        public static readonly int[] SixPrimeVs6PrimeOpp3OnTheBar1PointOpen=
            [-3, 0, 2, 2, 2, 2, 3, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -2, -2, -2, -2, -2, -2, 2, 0, 0, 0];
        public static readonly int[] SixPrimeVs6PrimeOnTheBarCloseOut =
            [-2, 2, 2, 2, 2, 2, 2, 1, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, -2, -2, -2, -2, -2, -2, 2, 0, 0, 0];
        
        public static readonly int[] FivePrimeVsFivePrimeMustRunInsteadOfCover =
            [-2, 0, 0, 1, 2, 2, 3, 2, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, -3, -2, -2, -2, -4, 2, 0, 0, 0];
        public static readonly int[] FourPrimeVs3PrimeFillTheGaps =
            [0, -2, 0, 2, 0, 3, 3, 4, 1, 0, 0, 0, 0, 0, 0, 0, 0, -1, -2, -2, -4, -2, 0, -2, 2, 0, 0, 0];
        public static readonly int[] FourPrimeVs3PrimeFillTheGapsSplitted =
            [0, -2, 0, 2, 0, 3, 3, 4, 1, 0, 0, 0, 0, 0, 0, 0, 0, -1, -2, -2, -4, -2, 0, -2, 1, 1, 0, 0];
        // I made a huge blunder when rolling 51 from the bar Hit and slot is best
        public static readonly int[] SixPrimeVsSixPrimeNoAnchorsCriticalTiming =
            [0, 0, -1, 2, 2, 2, 3, 2, 3, 0, 0, 0, 0, 0, 0, 0, 0, -1, -2, -2, -2, -2, -2, -2, -1, 1, 0, 0];

        public static readonly int[][] SplitGames = [
            Split24to23IntoPrimingGame,
            Split24to23IntoPrimingGame2,
            ];

        public static readonly int[][] PrimeVsPrimeGames =
            [
                SixPrimeVsSixPrimeTimingTrouble,
                SixPrimeVsSixPrimeCloseTiming,
                SixPrimeDeucePointVsSixPrimeTimingTrouble,
                SixPrimeVsFivePrimeTimingTrouble,
                FivePrimeDeucePointVsFivePrimeTimingTrouble,
                ThreePrimeVsThreePrimeBothOnAcePoint,
                OnTheBar3PrimeAndBlot,
                SixPrimeVs6PrimeOpp3OnTheBar1PointOpen,
                SixPrimeVs6PrimeOnTheBarCloseOut,
                FivePrimeVsFivePrimeMustRunInsteadOfCover,
                FourPrimeVs3PrimeFillTheGaps,
                FourPrimeVs3PrimeFillTheGapsSplitted,
                SixPrimeVsSixPrimeNoAnchorsCriticalTiming
            ];

        public static readonly int[][] OpeningGames =
            [
            TwoOneSlotOpening,
            TwoOneSplitOpening,
            TwoThreeSplitOpening,
            TwoThreeDownOpening,
            FourThreeSplitOpening,
            FourThreeDownOpening,
            FourFiveSplitOpening,
            FourTwoPOpening,
            SixTwoSplitOpening,          
            NackGammon
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