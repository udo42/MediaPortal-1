using System;

namespace Mediaportal.TV.Server.TVDatabase.Entities
{
  [AttributeUsage(AttributeTargets.All)]
  public class ProgramAttribute : Attribute 
  {
    public ProgramAttribute(string displayName, int languageId)
    {
      DisplayName = displayName;
      LanguageId = languageId;
    }

    public string DisplayName { get; private set; }

    public int LanguageId { get; private set; }
  }
}
