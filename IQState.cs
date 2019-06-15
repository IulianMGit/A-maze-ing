using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
  public abstract class QState
  {
    public abstract List<QState> GetAvailableStates();
    public abstract QState GetRandNextState(QState prevState);
    public abstract IQReward GetReward();
    public abstract void SetReward();
    public abstract override bool Equals(object obj);
    public abstract override string ToString();
  }
}
