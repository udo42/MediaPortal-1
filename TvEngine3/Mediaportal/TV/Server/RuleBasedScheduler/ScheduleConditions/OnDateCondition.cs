using System;
using System.Linq;
using Mediaportal.TV.Server.TVDatabase.Entities;

namespace Mediaportal.TV.Server.RuleBasedScheduler.ScheduleConditions
{
  [Serializable]
  public class OnDateCondition : IScheduleCondition
  {
    private DateTime? _onDate;

    public OnDateCondition(DateTime? onDate)
    {
      _onDate = onDate;
    }

    public OnDateCondition()
    {
    }

    public DateTime? OnDate
    {
      get { return _onDate; }
      set { _onDate = value; }
    }

    #region IScheduleCondition Members

    public IQueryable<Program> ApplyCondition(IQueryable<Program> baseQuery)
    {
      if (_onDate.HasValue)
      {
        return baseQuery.Where(program => (program.StartTime.Equals(_onDate)));
      }
      return baseQuery;
    }

    #endregion
  }
}