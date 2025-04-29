
using Backgammon.Util.AI;
using Backgammon.Utils;

namespace Backgammon.Models
{
    internal class BearoffDatabaseEvaluator : IBackgammonPositionEvaluator
    {
        private readonly Dictionary<string, float[]> _bearOffDatabase;
        public Dictionary<string, float[]> BearOffDatabase => _bearOffDatabase;

        public BearoffDatabaseEvaluator(Dictionary<string, float[]> bearOffDatabase) { 
            _bearOffDatabase = bearOffDatabase;
        }

        public float[] Evaluate(int[] position, int player)
        {
            string key;
            if (player == BackgammonBoard.Player1) {
                key = BearOffUtility.ConvertBearOffBoardToString(position);
                return _bearOffDatabase[key];
            }
            
            position = BackgammonBoard.MirrorBoard(position);
            key = BearOffUtility.ConvertBearOffBoardToString(position);
            var evaluation = _bearOffDatabase[key];
            return ScoreUtility.MirrorScore(evaluation);          
        }
    }
}
