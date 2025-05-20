using Backgammon.Util;
using static Backgammon.Models.BackgammonBoard;
using Backgammon.Models;
using static Backgammon.Models.Move;

namespace Backgammon.Tests
{
    [TestFixture]
    public class MoveGenerator
    {
        [Test]
        public void TestRolling22FourFiveSplitOpening() {
            var position = BackgammonPositions.FourFiveSplitOpening;
            var die1 = 2;
            var die2 = 2;
            var moveCandidates = GenerateLegalMovesStatic(position, die1, die2, Player2);
            foreach (var cand in moveCandidates) { 
                TestContext.Out.WriteLine(cand.move.MovesAsStandardNotation());
//                Console.WriteLine(cand.move.MovesAsStandardNotation());
            }
            
            Assert.That(moveCandidates.Count, Is.EqualTo(75), $"Expected 75 Move Candidates, got({moveCandidates.Count})");
        }

        [Test]
        public void TestRolling11FourFiveSplitOpening()
        {
            var position = BackgammonPositions.FourFiveSplitOpening;
            var die1 = 1;
            var die2 = 1;
            var moveCandidates = GenerateLegalMovesStatic(position, die1, die2, Player2);
            foreach (var cand in moveCandidates)
            {
                TestContext.Out.WriteLine(cand.move.MovesAsStandardNotation());
                
            }

            Assert.That(moveCandidates.Count, Is.EqualTo(42), $"Expected 42 Move Candidates, got({moveCandidates.Count})");
        }

        [Test]
        public void TestAnnotationRoll22_FourOffWithAHitFirst()
        {            
            List<CheckerMove> checkerMoves = [];
            checkerMoves.Add(new CheckerMove(4, 2, isHit: true));
            checkerMoves.Add(new CheckerMove(4, 2, isHit: false));
            checkerMoves.Add(new CheckerMove(2, 26, isBearOff:true));
            checkerMoves.Add(new CheckerMove(2, 26, isBearOff: true));
            Move move = new Move(2, 2, Player1,checkerMoves);
            var annotation = move.MovesAsStandardNotation();
            var expectedAnnotation = "22: 4/2*(2) 2/Off(2)";
            Assert.That(annotation, Is.EqualTo(expectedAnnotation), $"Expected {expectedAnnotation} , got({annotation})");
        }

        [Test]
        public void TestAnnotationRoll66_HitTwice()
        {
            List<CheckerMove> checkerMoves = [];
            checkerMoves.Add(new CheckerMove(13, 7, isHit: true));
            checkerMoves.Add(new CheckerMove(13, 7, isHit: false));
            checkerMoves.Add(new CheckerMove(7, 1, isHit: true));
            checkerMoves.Add(new CheckerMove(7, 1, isHit:false));
            Move move = new Move(6, 6, Player1, checkerMoves);
            var annotation = move.MovesAsStandardNotation();
            var expectedAnnotation = "66: 13/7*(2) 7/1*(2)";
            Assert.That(annotation, Is.EqualTo(expectedAnnotation), $"Expected {expectedAnnotation} , got({annotation})");
        }



        [Test]
        public void testRolling22HitBearOffAnnotation() { 
            int[] position = [0, 1, -1, 3, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -4, -3, -7, 0, 5, 0];
            var die1 = 2;
            var die2 = 2;
            var moveCandidates = GenerateLegalMovesStatic(position, die1, die2, Player1);
            Assert.That(moveCandidates.Count, Is.EqualTo(33), $"Expected 22 Move Candidates, got({moveCandidates.Count})");
        }

        [Test] 
        public void TestRolling16FromTheBarForcedMove()
        {
            int[] position = [0, 0, 2, 2, 3, 0, 0, -1, 0, 0, 0, -1, -3, 0, 0, 0, 0, -2, 0, -3, -3, 0, -2, 0, 0, 1, 7, 0];
            var die1 = 1;
            var die2 = 6;
            var moveCandidates = GenerateLegalMovesStatic(position, die1, die2, Player1);
            
            var annotation = moveCandidates.First().move.MovesAsStandardNotation();
            var expectedAnnotation = "16: Bar/24 24/18";
            Assert.That(moveCandidates.Count, Is.EqualTo(1), $"Expected 1 posible move, got({moveCandidates.Count})");
            Assert.That(annotation, Is.EqualTo(expectedAnnotation), $"Expected {expectedAnnotation} , got({annotation})");
        }

        [Test]
        public void TestRolling63FromTheBarP21Hit()
        {   
            //Had a bug in the move generator where the hitting play was missing
            int[] position = [-1, -2, 7, 4, 1, 0, 0, 0, 0, 0, 0, -1, 0, -1, 0, 0, 0, 0, 0, 0, 0, -2, -3, -3, -2, 0, 3, 0];
            var die1 = 3;
            var die2 = 6;
            var moveCandidates = GenerateLegalMovesStatic(position, die1, die2, Player2);

            var annotation = moveCandidates.First().move.MovesAsStandardNotation();
            Assert.That(moveCandidates.Count, Is.EqualTo(5), $"Expected 1 possible move, got({moveCandidates.Count})");
        }   


        [Test]
        public void TestRemoveDuplicates()
        {
            //Had a bug in the move generator where the hitting play was missing
            int[] position = [0, -1, 0, 2, 0, 2, 2, 0, 0, 0, 0, 2, 1, 2, 0, 1, 1, 0, 1, 0, 0, 1, -3, -3, -7, 0, 0, -1];
            
            var die1 = 6;
            var die2 = 3;
            var moveCandidates = GenerateLegalMovesStatic(position, die1, die2, Player1, true);
            foreach(var cand in moveCandidates)
            {
                Console.WriteLine(cand.move.MovesAsStandardNotation());
            }
            var annotation = moveCandidates.First().move.MovesAsStandardNotation();
            Assert.That(moveCandidates.Count, Is.EqualTo(57), $"Expected 1 possible move, got({moveCandidates.Count})");
            var allMoveCandidates = GenerateLegalMovesStatic(position, die1, die2, Player1, false);
            Assert.That(allMoveCandidates.Count, Is.EqualTo(66), $"Expected 1 possible move, got({allMoveCandidates.Count})");
        }
    }
}
