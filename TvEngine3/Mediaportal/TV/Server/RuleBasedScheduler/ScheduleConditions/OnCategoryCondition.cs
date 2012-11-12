using System;
using System.Collections.Generic;
using System.Linq;
using Mediaportal.TV.Server.TVDatabase.Entities;

namespace Mediaportal.TV.Server.RuleBasedScheduler.ScheduleConditions
{
  [Serializable]
  public class OnCategoryCondition : IScheduleCondition
  {
    private IList<ProgramCategory> _categories;

    public OnCategoryCondition(IList<ProgramCategory> categories)
    {
      _categories = categories;
    }

    public OnCategoryCondition()
    {
    }

    public IList<ProgramCategory> Categories
    {
      get { return _categories; }
      set { _categories = value; }
    }

    #region IScheduleCondition Members

    public IQueryable<Program> ApplyCondition(IQueryable<Program> baseQuery)
    {
      return
        baseQuery.Where(
          program =>
          (_categories.Any(categoryDto => categoryDto.IdProgramCategory == program.ProgramCategory.IdProgramCategory)));
    }

    #endregion
  }
}