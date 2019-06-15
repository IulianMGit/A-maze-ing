using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{

    class QLearning
    {
        Dictionary<QState, Dictionary<QState, double>> Q = new Dictionary<QState, Dictionary<QState, double>>();
        Dictionary<QState, Dictionary<QState, double>> Nsa = new Dictionary<QState, Dictionary<QState, double>>();

        List<QState> initialStateProvider;

        public QLearning(List<QState> _initialStateProvider, Dictionary<QState, Dictionary<QState, double>> _Q, Dictionary<QState, Dictionary<QState, double>> _Nsa)
        {
            this.initialStateProvider = _initialStateProvider;
            this.Q = _Q;
            this.Nsa = _Nsa;
        }

        static QState ArgMax(Dictionary<QState, double> values)
        {
            QState max = values.First().Key;

            foreach (var state in values)
                if (state.Value > values[max]) max = state.Key;

            return max;
        }

        public List<QState> Walk(QState start, QState goal)
        {
            const int maxListLen = 300;
            List<QState> result = new List<QState>();

            result.Add(start);
            QState current = start; QState next;
            Console.Write(current.ToString() + "->");
            while (!current.Equals(goal))
            {
                next = ArgMax(Q[current]);
                Console.Write(next + "->");
                result.Add(next);
                current = next;

                if (result.Count == maxListLen) break;
            }

            return result;
        }

        public void Train(QState goal, int iterations, double learningRate, double discountRate)
        {
            var rng = new Random();
            for (int epoch = 0; epoch < iterations; ++epoch)
            {
                foreach (var item in initialStateProvider)
                    item.SetReward();

                QState prevState = null;
                var state = initialStateProvider[0];
                QState nextState = state.GetRandNextState(null);
                QState nextNextState = null;
                while (true)
                {

                    List<QState> possNextNextStates = nextState.GetAvailableStates();
                    double maxQ = double.MinValue;
                    double maxNrAp = double.MaxValue;

                    for (int j = 0; j < possNextNextStates.Count; ++j)
                    {
                        var possNextState = possNextNextStates[j];
                        double q = Q[nextState][possNextState];
                        if (q > maxQ)
                        {
                            maxQ = q;
                            maxNrAp = (Nsa[nextState][possNextState] - 1) * 1000;
                            nextNextState = possNextState;
                        }
                    }

                    Nsa[state][nextState] += 0.004;

                    Q[state][nextState] = Q[state][nextState] + learningRate / Nsa[state][nextState] * (state.GetReward().ComputeReward() + discountRate * maxQ - 1 * Q[state][nextState]);

                    if (state.Equals(goal)) { Q[state][state] = state.GetReward().ComputeReward(); break; }

                    prevState = state;
                    state = nextState;

                    nextState = nextNextState;
                }
            }

        }
    }
}


//for (int j = 0; j < possNextNextStates.Count; ++j)
//{
//  if (nextNextState == possNextNextStates[j]) continue;

//  var possNextState = possNextNextStates[j];
//  var nrAp = (Nsa[nextState][possNextState] - 1) * 1000;
//  double q = Q[nextState][possNextState];

//  if (nrAp / maxNrAp < 0.2)
//  {
//    maxNrAp = nrAp;
//    maxQ = q;
//    nextNextState = possNextState;
//  }
//}

//          for (int j = 0; j<possNextNextStates.Count; ++j)
//          {
//            if (nextNextState == possNextNextStates[j]) continue;

//            var possNextState = possNextNextStates[j];
//var nrAp = (Nsa[nextState][possNextState] - 1) * 1000;
//double q = Q[nextState][possNextState];

//            if (maxQ - q <= 0.001 && ((maxNrAp - nrAp) >= 20 && (maxNrAp - nrAp) <= 50))
//            {
//              maxNrAp = nrAp;
//              maxQ = q;
//              nextNextState = possNextState;
//            }
//          }