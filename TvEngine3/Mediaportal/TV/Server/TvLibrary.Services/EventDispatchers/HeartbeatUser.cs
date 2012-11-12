using System;
using System.Net;

namespace Mediaportal.TV.Server.TVLibrary.EventDispatchers
{
  public class HeartbeatUser
  {
    private readonly string _name;

    public HeartbeatUser()
    {
      _name = Dns.GetHostName();
      LastSeen = DateTime.MinValue;
    }

    public DateTime LastSeen { get; set; }

    public string Name
    {
      get { return _name; }
    }
  }
}