using System;
using System.Collections.Generic;
using Mediaportal.TV.Server.TVControl.Interfaces.Services;
using Mediaportal.TV.Server.TVDatabase.Entities;
using Mediaportal.TV.Server.TVDatabase.Entities.Enums;
using Mediaportal.TV.Server.TVDatabase.TVBusinessLayer;

namespace Mediaportal.TV.Server.TVLibrary.Services
{  
  public class ProgramService : IProgramService
  {
    #region IProgramService Members

    public IDictionary<int, NowAndNext> GetNowAndNextForChannelGroup(int idGroup)
    {
      IDictionary<int, NowAndNext> nowAndNexts = ProgramManagement.GetNowAndNextForChannelGroup(idGroup);
      return nowAndNexts;
    }

    public IList<Program> GetProgramsByChannelAndStartEndTimes(int idChannel, DateTime startTime, DateTime endTime)
    {
      IList<Program> programsByChannelAndStartEndTimes = ProgramManagement.GetProgramsByChannelAndStartEndTimes(idChannel, startTime, endTime);
      return programsByChannelAndStartEndTimes;
    }

    public IList<Program> GetProgramsByChannelAndTitleAndStartEndTimes(int idChannel, string title, DateTime startTime, DateTime endTime)
    {
      IList<Program> programsByChannelTitleAndStartEndTimes = ProgramManagement.GetProgramsByChannelAndTitleAndStartEndTimes(idChannel, title, startTime, endTime);
      return programsByChannelTitleAndStartEndTimes;
    }

    public IList<Program> GetProgramsByTitleAndStartEndTimes(string title, DateTime startTime, DateTime endTime)
    {
      IList<Program> programsByTitleAndStartEndTimes = ProgramManagement.GetProgramsByTitleAndStartEndTimes(title, startTime, endTime);
      return programsByTitleAndStartEndTimes;
    }

    public Program SaveProgram(Program program)
    {
      return ProgramManagement.SaveProgram(program);      
    }    

    public IList<Program> GetNowAndNextProgramsForChannel(int idChannel)
    {
      return ProgramManagement.GetNowAndNextProgramsForChannel(idChannel);
    }

    public Program GetProgramAt(DateTime date, int idChannel)
    {
      return ProgramManagement.GetProgramAt(date, idChannel);
    }

    public Program GetProgramByTitleAt(DateTime date, string title)
    {
      return ProgramManagement.GetProgramAt(date, title);
    }

    public IList<Program> GetProgramsByTitleAndTimesInterval(string title, DateTime startTime, DateTime endTime)
    {
      IList<Program> programsByTitleAndTimesInterval = ProgramManagement.GetProgramsByTitleAndTimesInterval(title, startTime, endTime);
      return programsByTitleAndTimesInterval;
    }

    public Program GetProgramsByTitleTimesAndChannel(string programName, DateTime startTime, DateTime endTime, int idChannel)
    {
      Program programsByTitleTimesAndChannel = ProgramManagement.GetProgramsByTitleTimesAndChannel(programName, startTime, endTime, idChannel);
      return programsByTitleTimesAndChannel;
    }

    public IList<Program> RetrieveWeekly(DateTime startTime, DateTime endTime, int idChannel)
    {
      return ProgramManagement.RetrieveWeekly(startTime, endTime, idChannel);
    }

    public IList<Program> RetrieveWeekends(DateTime startTime, DateTime endTime, int idChannel)
    {
      return ProgramManagement.RetrieveWeekends(startTime, endTime, idChannel);
    }

    public IList<Program> RetrieveWorkingDays(DateTime startTime, DateTime endTime, int idChannel)
    {
      return ProgramManagement.RetrieveWorkingDays(startTime, endTime, idChannel);
    }

    public IList<Program> RetrieveDaily(DateTime startTime, DateTime endTime, int idChannel)
    {
      return ProgramManagement.RetrieveDaily(startTime, endTime, idChannel);
    }

    public IList<Program> GetProgramsByState(ProgramState state)
    {
      return ProgramManagement.GetProgramsByState(state);
    }

    public Program GetProgramByTitleAndTimes(string programName, DateTime startTime, DateTime endTime)
    {
      Program programByTitleAndTimes = ProgramManagement.GetProgramByTitleAndTimes(programName, startTime, endTime);
      return programByTitleAndTimes;
    }

    public IList<Program> GetProgramsByDescriptionAndMediaType(string searchCriteria, MediaTypeEnum mediaType, StringComparisonEnum stringComparison)
    {
      IList<Program> programsByDescriptionAndMediaType = ProgramManagement.GetProgramsByDescription(searchCriteria, mediaType, stringComparison);
      return programsByDescriptionAndMediaType;
    }

    public IList<Program> GetProgramsByDescription(string searchCriteria, StringComparisonEnum stringComparison)
    {
      IList<Program> programsByDescription = ProgramManagement.GetProgramsByDescription(searchCriteria, stringComparison);
      return programsByDescription;
    }

    public IList<Program> GetProgramsByTitle(string searchCriteria, StringComparisonEnum stringComparison)
    {
      IList<Program> programsByTitle = ProgramManagement.GetProgramsByTitle(searchCriteria, stringComparison);
      return programsByTitle;
    }

    public IList<Program> GetProgramsByTitleAndMediaType(string searchCriteria, MediaTypeEnum mediaType, StringComparisonEnum stringComparison)
    {
      IList<Program> programsByTitleAndMediaType = ProgramManagement.GetProgramsByTitle(searchCriteria, mediaType, stringComparison);
      return programsByTitleAndMediaType;
    }

    public Program GetProgram(int idProgram)
    {
      Program program = ProgramManagement.GetProgram(idProgram);
      return program;
    }

    public IDictionary<int, IList<Program>> GetProgramsForAllChannels(DateTime startTime, DateTime endTime, IEnumerable<Channel> channels)
    {
      IDictionary<int, IList<Program>> programsForAllChannels = ProgramManagement.GetProgramsForAllChannels(startTime, endTime, channels);
      return programsForAllChannels;
    }

    public IList<Program> GetProgramsByTitleAndCategoryAndMediaType(string categoryCriteriea, string titleCriteria, MediaTypeEnum mediaType, StringComparisonEnum stringComparisonCategory, StringComparisonEnum stringComparisonTitle)
    {
      IList<Program> programsByTitleAndCategoryAndMediaType = ProgramManagement.GetProgramsByTitleAndCategoryAndMediaType(categoryCriteriea, titleCriteria, mediaType, stringComparisonCategory, stringComparisonTitle);
      return programsByTitleAndCategoryAndMediaType;
    }

    public IList<Program> GetProgramsByTimesInterval(DateTime startTime, DateTime endTime)
    {
      IList<Program> programsByTimesInterval = ProgramManagement.GetProgramsByTimesInterval(startTime, endTime);
      return programsByTimesInterval;
    }

    public IList<Program> GetProgramsForSchedule(Schedule schedule)
    {
      IList<Program> programsForSchedule = ProgramManagement.GetProgramsForSchedule(schedule);
      return programsForSchedule;
    }

    public void DeleteAllPrograms()
    {
      ProgramManagement.DeleteAllPrograms();
    }

    public ProgramCategory GetProgramCategoryByName(string category)
    {
      return ProgramManagement.GetProgramCategoryByName(category);
    }

    public IList<string> ListAllDistinctCreditRoles()
    {
      return ProgramManagement.ListAllDistinctCreditRoles();
    }

    public IList<ProgramCategory> ListAllCategories()
    {
      return ProgramManagement.ListAllCategories();
    }

    public IList<ProgramCredit> ListAllCredits()
    {
      return ProgramManagement.ListAllCredits();
    }

    public IList<Program> RetrieveByChannelAndTimesInterval(int channelId, DateTime startTime, DateTime endTime)
    {
      return ProgramManagement.RetrieveByChannelAndTimesInterval(channelId, startTime, endTime);
    }

    public void InitiateInsertPrograms()
    {
      ProgramManagement.InitiateInsertPrograms();
    }

    #endregion
  }
}
