using Backgammon.Models;
using Backgammon.Util;
using static Backgammon.Models.BackgammonBoard;
using static Backgammon.Util.Constants.PositionType;
namespace Backgammon.Tests
{
    [TestFixture]
    internal class BackgammonBoardAsPositionType
    {
        [Test]
        public void Test12BackgamePos() {
            var position = BackgammonPositions.OneTwoBackgame;

            var positionType = MapBoardToPositionType(position,Player1);

            Assert.That(positionType, Is.EqualTo(Backgame12), $"Expected 12 Backgame({positionType})");
        }

        [Test]
        public void Test13BackgamePos()
        {
            var position = BackgammonPositions.OneThreeBackgame;

            var positionType = MapBoardToPositionType(position, Player1);

            Assert.That(positionType, Is.EqualTo(Backgame13), $"Expected 13 Backgame({positionType})");
        }

        [Test]
        public void Test23BackgamePos()
        {
            var position = BackgammonPositions.TwoThreeBackgame;

            var positionType = MapBoardToPositionType(position, Player1);

            Assert.That(positionType, Is.EqualTo(Backgame23), $"Expected 23 Backgame({positionType})");
        }

        [Test]
        public void TestOtherBackgamePos()
        {
            var position = BackgammonPositions.TwoFourBackgame;

            var positionType = MapBoardToPositionType(position, Player1);

            Assert.That(positionType, Is.EqualTo(OtherBackgame), $"Expected Other Backgame({positionType})");
        }

        // If both sides has advanced anchor (4,5,6,7 point) I will call it mutual holding game
        [Test]
        public void TestBarPointMutualHoldingGamePos() {
            var position = BackgammonPositions.BarPointMutualHoldingGame;
            var positionType = MapBoardToPositionType(position, Player1);            
            
            Assert.That(positionType, Is.EqualTo(MutualHoldingGame), $"Expected Mutual Holding game({positionType})");
        }

        [Test]
        public void TestBarPointVsButterFlyHoldingGamePos()
        {
            var position = BackgammonPositions.BarPointVsButterflyHoldingGame;
            var player = Player1;
            var positionType = MapBoardToPositionType(position, player);

            Assert.That(positionType, Is.EqualTo(HoldingGame),
                $"Expected Butterfly Anchor game instead of {positionType} ,\n {PositionAsString(position, player)}");
        }

        [Test]
        public void TestDeucePointVsFourPointHoldingGamePos()
        {
            var position = BackgammonPositions.DeucePointVsFourPointHoldingGame;
            var player = Player1;
            var positionType = MapBoardToPositionType(position, player);
            
            Assert.That(positionType, Is.EqualTo(HoldingGame), 
                $"Expected Holding game game instead of {positionType} ,\n {PositionAsString(position, player)}");
        }


        [Test]
        public void TestFourPointVsBarPointMutualHoldingGamePos()
        {
            var position = BackgammonPositions.FourPointVsBarPointMutualHoldingGame;
            var player = Player1;
            var positionType = MapBoardToPositionType(position, player);

            Assert.That(positionType, Is.EqualTo(MutualHoldingGame),
                $"Expected Mutual Holding game instead of {positionType} ,\n {PositionAsString(position, player)}");
        }

        [Test]
        public void TestFourAndTenPointOppSmallCrunchHoldingGame()
        {
            var position = BackgammonPositions.FourAndTenPointOppSmallCrunchHoldingGame;
            var player = Player1;
            var positionType = MapBoardToPositionType(position, player);
            var expectPositionType = HoldingGame;

            Assert.That(positionType, Is.EqualTo(expectPositionType),
                $"Expected {expectPositionType} game instead of {positionType} ,\n {PositionAsString(position, player)}");
        }


        [Test]
        public void TestGoldenPointOppBigRaceLeadHoldingGame()
        {
            var position = BackgammonPositions.GoldenPointOppBigRaceLeadHoldingGame;
            var player = Player1;
            var positionType = MapBoardToPositionType(position, player);
            var expectPositionType = HoldingGame;

            Assert.That(positionType, Is.EqualTo(expectPositionType),
                $"Expected {expectPositionType} instead of {positionType} ,\n {PositionAsString(position, player)}");
        }

        [Test]
        public void TestGoldenPointCloseRaceHoldingGame()
        {
            var position = BackgammonPositions.GoldenPointOppBigRaceLeadHoldingGame;
            var player = Player1;
            var positionType = MapBoardToPositionType(position, player);
            var expectedPositionType = HoldingGame;

            Assert.That(positionType, Is.EqualTo(expectedPositionType),
                $"Expected {expectedPositionType} game instead of {positionType} ,\n {PositionAsString(position, player)}");
        }

        [Test]
        public void TestGoldenPoint4PrimeHoldingGame()
        {
            var position = BackgammonPositions.GoldenPoint4PrimeHoldingGame;
            var player = Player1;
            var positionType = MapBoardToPositionType(position, player);
            var expectedPositionType = FourPrime;

            Assert.That(positionType, Is.EqualTo(expectedPositionType),
                $"Expected {expectedPositionType} game instead of {positionType} ,\n {PositionAsString(position, player)}");
        }

        [Test]
        public void TestGoldenPoint4PrimeOppNotSplitHoldingGame()
        {
            var position = BackgammonPositions.GoldenPoint4PrimeOppNotSplitHoldingGame;
            var player = Player1;
            var positionType = MapBoardToPositionType(position, player);
            var expectedPositionType = FourPrime;

            Assert.That(positionType, Is.EqualTo(expectedPositionType),
                $"Expected {expectedPositionType} game instead of {positionType} ,\n {PositionAsString(position, player)}");
        }

        [Test]
        public void TestGoldenPointHoldingGame()
        {
            var position = BackgammonPositions.GoldenPointMutualHoldingGame;
            var player = Player1;
            var positionType = MapBoardToPositionType(position, player);
            var expectedPositionType = MutualHoldingGame;
            Assert.That(positionType, Is.EqualTo(expectedPositionType),
                $"Expected {expectedPositionType} game instead of {positionType} ,\n {PositionAsString(position, player)}");
        }

        // I should probably change the target to prime as only one side has checkers to escape
        [Test]
        public void TestBearingInWithPrimeVs1PointPlayer()
        {
            var position = BackgammonPositions.BearingInWithPrimeVs1PointPlayer2;
            var player = Player2;
            var positionType = MapBoardToPositionType(position, player);
            var expectedPositionType = PrimeVsPrime;
            Assert.That(positionType, Is.EqualTo(expectedPositionType),
                $"Expected {expectedPositionType} game instead of {positionType} ,\n {PositionAsString(position, player)}");
        }

        // I should probably change the target to prime as only one side has checkers to escape
        [Test]
        public void TestBearingOffWithVs1PointPlayer2()
        {
            var position = BackgammonPositions.BearingOffWithVs1PointPlayer2;
            var player = Player2;
            var positionType = MapBoardToPositionType(position, player);
            var expectedPositionType = BearOffVs1Point;
            Assert.That(positionType, Is.EqualTo(expectedPositionType),
                $"Expected {expectedPositionType} game instead of {positionType} ,\n {PositionAsString(position, player)}");

            player = Player1;
            positionType = MapBoardToPositionType(position, player);
            expectedPositionType = BearOffVs1PointDefence;
            Assert.That(positionType, Is.EqualTo(expectedPositionType),
                $"Expected {expectedPositionType} game instead of {positionType} ,\n {PositionAsString(position, player)}");

            position = MirrorBoard(position);
            player = Player1;
            positionType = MapBoardToPositionType(position, player);
            expectedPositionType = BearOffVs1Point;
            Assert.That(positionType, Is.EqualTo(expectedPositionType),
                $"Expected {expectedPositionType} game instead of {positionType} ,\n {PositionAsString(position, player)}");

            player = Player2;
            positionType = MapBoardToPositionType(position, player);
            expectedPositionType = BearOffVs1PointDefence;
            Assert.That(positionType, Is.EqualTo(expectedPositionType),
                $"Expected {expectedPositionType} game instead of {positionType} ,\n {PositionAsString(position, player)}");


        }


        // When a player has taken many checkers off the position can be treated as crunched, no priming possible..
        [Test]
        public void TestBearOffVs7Off()
        {
            var position = BackgammonPositions.BearOffVs7Off;
            var player = Player1;
            var positionType = MapBoardToPositionType(position, player);
            var expectedPositionType = BigCrunch;
            Assert.That(positionType, Is.EqualTo(expectedPositionType),
                $"Expected {expectedPositionType} game instead of {positionType} ,\n {PositionAsString(position, player)}");
        }

        // When a player has taken many checkers off the position can be treated as crunched, no priming possible..
        [Test]
        public void TestBearOffVs11Off()
        {
            var position = BackgammonPositions.BearOffVs11Off;
            var player = Player1;
            var positionType = MapBoardToPositionType(position, player);
            var expectedPositionType = BigCrunch;
            Assert.That(positionType, Is.EqualTo(expectedPositionType),
                $"Expected {expectedPositionType} game instead of {positionType} ,\n {PositionAsString(position, player)}");
        }

        [Test]
        public void TestBearOffVsOneOff()
        {
            var position = BackgammonPositions.BearOffVsOneOffCloseOut;
            var player = Player1;
            var positionType = MapBoardToPositionType(position, player);
            var expectedPositionType = BearOffContact;
            Assert.That(positionType, Is.EqualTo(expectedPositionType),
                $"Expected {expectedPositionType} game instead of {positionType} ,\n {PositionAsString(position, player)}");
            
            player = Player2;
            positionType = MapBoardToPositionType(position, player);
            expectedPositionType = BearOffContactDefence;
            Assert.That(positionType, Is.EqualTo(expectedPositionType),
                $"Expected {expectedPositionType} game instead of {positionType} ,\n {PositionAsString(position, player)}");
        }

        // When a player has taken many checkers off the position can be treated as crunched, no priming possible..
        [Test]
        public void TestBearOffVs13Off2OnTheBar()
        {
            var position = BackgammonPositions.BearOffVs13Off2OnTheBar;
            var player = Player1;
            var positionType = MapBoardToPositionType(position, player);
            var expectedPositionType = BigCrunch;
            Assert.That(positionType, Is.EqualTo(expectedPositionType),
                $"Expected {expectedPositionType} game instead of {positionType} ,\n {PositionAsString(position, player)}");
        }



        private string PositionAsString(int[] position, int player) {
            var backgammonBoard = new BackgammonBoard();
            backgammonBoard.Position = position;
            backgammonBoard.CurrentPlayer = player;
            return backgammonBoard.ToString();
        }
    }
}
