using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backgammon.Models
{
    public interface IBackgammonPositionEvaluator
    {
        float[] Evaluate(int[] position, int player);
    }
}
