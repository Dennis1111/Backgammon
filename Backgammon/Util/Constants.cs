namespace Backgammon.Util
{
    public static class Constants
    {
        public const bool MirrorBoardForPlayer2 = true;
        // We are evaluating winning chances from between 0 to 1
        public const float EvaluationMax = 1f;
        public const float EvaluationMin = 0f;

        public enum PositionType
        {
            NoContact,
            Contact,
            CompletedStage,
            Backgame12,
            Backgame13,
            Backgame23,
            OtherBackgame,
            FivePrime,
            SixPrime,
            Crunched,
            BearOff,
            BearoffContact,
            BearOffDatabase
        }
    }
}
