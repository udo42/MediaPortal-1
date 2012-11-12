using System;
using System.Diagnostics;
using System.Reflection;

namespace MediaPortal.Common.Utils
{
  public class Singleton<T> where T : class
  {
    public static T Instance
    {
      get
      {
        try
        {
          return SingletonCreator.Instance;
        }
        catch (Exception)
        {
#if DEBUG
          Debugger.Launch();
#endif
          //TODO gibman: log here once log4net is introduced                    
        }
        return null;
      }
    }

    #region Nested type: SingletonCreator

    private static class SingletonCreator
    {
      internal static readonly T Instance =
        typeof (T).InvokeMember(typeof (T).Name,
                                BindingFlags.CreateInstance |
                                BindingFlags.Instance |
                                BindingFlags.Public |
                                BindingFlags.NonPublic,
                                null, null, null) as T;

      static SingletonCreator()
      {
      }
    }

    #endregion
  }
}