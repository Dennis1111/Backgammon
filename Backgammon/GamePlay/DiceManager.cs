namespace Backgammon.GamePlay
{
    public class DiceManager
    {
        Random _random = new Random();

        public int[] RollDice()
        {
            // Simulate rolling two dice
            int die1 = _random.Next(1, 7);
            int die2 = _random.Next(1, 7);
            if (die1 < die2)
            {
                return [die2, die1];
            }
            else
            {
                // Return the rolled values as an array
                return [die1, die2];
            }
        }
        
        public int[] FirstRoll()
        {
            int die1 = _random.Next(1, 7);
            int die2 = _random.Next(1, 7);
            while (die1==die2)
            {
                die2 = _random.Next(1, 7);
            }

            // Return the rolled values as an array
            return [die1, die2];
        }
    }
}
