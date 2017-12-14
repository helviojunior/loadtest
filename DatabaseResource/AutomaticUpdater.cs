using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SafeTrend.Data.Update;

namespace SafeTrend.Data
{
    public class AutomaticUpdater
    {
        public void Run(DbBase ctx, IEnumerable<IUpdateScript> scripts)
        {
            UpdateDatabase(ctx, scripts, -1);
        }

        public void Run(DbBase ctx, IEnumerable<IUpdateScript> scripts, double minSerial)
        {
            UpdateDatabase(ctx, scripts, minSerial);
        }

        private void UpdateDatabase(DbBase ctx, IEnumerable<IUpdateScript> scripts, double minSerial)
        {

            foreach (IUpdateScript item in scripts)
            {
                if (item.Serial < minSerial)
                    continue;

                Object trans = ctx.BeginTransaction();
                try
                {
                    if (!string.IsNullOrEmpty(item.Precondition))
                    {
                        var preConditionResult = ctx.ExecuteScalar<Int64>(item.Precondition, trans);
                        if (preConditionResult == 0)
                            continue;
                    }
                    ctx.ExecuteNonQuery(item.Command, trans);
                    ctx.Commit();
                }
                catch (Exception ex)
                {
                    ctx.Rollback();
                    throw new Exception(ex.Message + " on execute script " + item.GetType().Name + " ("+ item.Serial + ")", ex);
                }

            }
        }
    }
}
