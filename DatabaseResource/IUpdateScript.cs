using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update
{
    public interface IUpdateScript
    {
        string Provider { get; }
        string Command { get; }
        string Precondition { get; }
        double Serial { get; }
    }
}
