namespace Backgammon.Util
{
    public static class Constants
    {
        public const float AveragePipPerRoll = 49 / 6;
        public const bool MirrorBoardForPlayer2 = true;
        // We are evaluating winning chances from between 0 to 1 , I used to clip neural outputs but not needed for sigmoid activationfunction
        public const float EvaluationMax = 1f;
        public const float EvaluationMin = 0f;

        public enum PositionType
        {
            NoContact,
            Contact,
            EarlyGame,
            //AcePointTaken,
            HoldingGame,
            ButterFlyAnchor,
            DeucePointAnchor,
            //ThreePointHoldingGame,
            MutualHoldingGame,
            CompletedStage,
            WeakContact,
            Backgame12,
            Backgame13,
            Backgame23,
            OtherBackgame,
            PrimeVsPrime,
            FourPrime,
            FivePrime,
            SixPrime,
            // First two cases
            SixPrimeVsSixPrimeP2Anchor,
            SixPrimeVsSixPrimeP2LooseCheckers,
            SixPrimeVsSixPrimeP1Anchor, // When P2 has no Checkers to escape
            SixPrimeVsSixPrimeP1LooseCheckers, // When P2 has no Checkers to escape
            Crunched,
            BigCrunch,
            BigRaceLead,
            BearOff,
            BearOffVsBackgame,
            BearOffVs1Point,
            BearOffVs1PointDefence,
            BearOffContact,
            BearOffContactDefence,
            BearOffDatabase
        }
    }
}
