namespace Backgammon.Models
{
    public class GameData
    {
        public List<MoveData> MoveData { get; private set; } = new List<MoveData>();
        public int Score { get; set; }
        public GameData() { 
            MoveData = new List<MoveData>();
        }

        public void AddMoveData(MoveData moveData)
        {
            MoveData.Add(moveData);
        }

        public bool IsEmpty() { 
            return MoveData.Count == 0;
        }
    }
}
