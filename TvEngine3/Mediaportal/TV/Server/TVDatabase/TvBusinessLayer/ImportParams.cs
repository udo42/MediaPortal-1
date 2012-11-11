using System.Threading;
using Mediaportal.TV.Server.TVDatabase.Entities.Enums;

namespace Mediaportal.TV.Server.TVDatabase.TVBusinessLayer
{
  public class ImportParams
  {
    public string ConnectString;
    public ThreadPriority Priority;
    public DeleteBeforeImportOption ProgamsToDelete;
    public ProgramList ProgramList;
    public int SleepTime;
  } ;
}
