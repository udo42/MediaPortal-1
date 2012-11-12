using System;
using System.Runtime.Remoting.Messaging;
using System.ServiceModel;
using Mediaportal.TV.Server.TVControl.Events;
using Mediaportal.TV.Server.TVControl.Interfaces.Events;
using Mediaportal.TV.Server.TVControl.Interfaces.Services;
using Mediaportal.TV.Server.TVLibrary.Interfaces.CiMenu;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Logging;

namespace Mediaportal.TV.Server.TVControl.ServiceAgents
{
  public class EventServiceAgent : ServiceAgent<IEventService>, IEventService, IServerEventCallback
  {
    #region events & delegates

    #region Delegates

    public delegate void CiMenuCallbackDelegate(CiMenu menu);

    public delegate void ConnectionLostDelegate();

    public delegate void HeartbeatRequestReceivedDelegate();

    public delegate void TvServerEventReceivedDelegate(TvServerEventArgs eventArgs);

    #endregion

    public event HeartbeatRequestReceivedDelegate OnHeartbeatRequestReceived;
    public event TvServerEventReceivedDelegate OnTvServerEventReceived;
    public event CiMenuCallbackDelegate OnCiMenuCallbackReceived;

    public event ConnectionLostDelegate OnConnectionLost;

    #endregion

    private static string _hostname;

    #region ctor's

    /// <summary>
    /// EventServiceAgent
    /// </summary>
    public EventServiceAgent(string hostname)
    {
      _hostname = hostname;
      NetTcpBinding binding = ServiceHelper.GetTcpBinding();
      if (!String.IsNullOrWhiteSpace(_hostname))
      {
        var endpoint = new EndpointAddress(ServiceHelper.GetTcpEndpointURL(typeof (IEventService), _hostname));
        var callbackInstance = new InstanceContext(this);
        _channelFactory = new DuplexChannelFactory<IEventService>(callbackInstance, binding, endpoint);
        _channel = _channelFactory.CreateChannel();

        callbackInstance.Faulted += EventServiceAgent_Faulted;
        callbackInstance.Closed += EventServiceAgent_Closed;

        _channelFactory.Faulted += EventServiceAgent_Faulted;
        _channelFactory.Closed += EventServiceAgent_Closed;

        ((IClientChannel) _channel).Faulted += EventServiceAgent_Faulted;
        ((IClientChannel) _channel).Closed += EventServiceAgent_Closed;
      }
    }

    #endregion

    private readonly DuplexChannelFactory<IEventService> _channelFactory;

    private IEventService Channel
    {
      get { return _channel; }
    }

    #region Implementation of IEventService

    public void Subscribe(string username)
    {
      try
      {
        Channel.Subscribe(username);
      }
      catch (CommunicationObjectFaultedException)
      {
        AbortChannel();
      }
    }

    public void Unsubscribe(string username)
    {
      try
      {
        Channel.Unsubscribe(username);
      }
      catch (CommunicationObjectFaultedException)
      {
        AbortChannel();
      }
    }

    private void AbortChannel()
    {
      _channelFactory.Abort();
      _channelFactory.Close();
      Dispose();
    }

    #endregion

    #region Implementation of IServerEventCallback

    public IAsyncResult BeginOnCallbackTvServerEvent(TvServerEventArgs eventArgs, AsyncCallback callback,
                                                     object asyncState)
    {
      Action<TvServerEventArgs> act = (tvServerEventArgs) =>
                                        {
                                          try
                                          {
                                            if (OnTvServerEventReceived != null)
                                            {
                                              OnTvServerEventReceived(eventArgs);
                                            }
                                          }
                                          catch (Exception ex)
                                          {
                                            this.LogError("BeginOnCallbackTvServerEvent exception : {0}", ex);
                                          }
                                        };
      try
      {
        return act.BeginInvoke(eventArgs, callback, asyncState);
      }
      catch (Exception)
      {
        return null;
      }
    }

    public void EndOnCallbackTvServerEvent(IAsyncResult result)
    {
      try
      {
        var act = (Action<TvServerEventArgs>) ((AsyncResult) result).AsyncDelegate;
        act.EndInvoke(result);
      }
      catch (Exception ex)
      {
        this.LogError("EndOnCallbackTvServerEvent exception : {0}", ex);
      }
    }

    public IAsyncResult BeginOnCallbackCiMenuEvent(CiMenu menu, AsyncCallback callback, object asyncState)
    {
      Action<CiMenu> act = (ciMenu) =>
                             {
                               try
                               {
                                 if (OnCiMenuCallbackReceived != null)
                                 {
                                   OnCiMenuCallbackReceived(ciMenu);
                                 }
                               }
                               catch (Exception ex)
                               {
                                 this.LogError("BeginOnCallbackCiMenuEvent exception : {0}", ex);
                               }
                             };
      return act.BeginInvoke(menu, callback, asyncState);
    }

    public void EndOnCallbackCiMenuEvent(IAsyncResult result)
    {
      try
      {
        var act = (Action<CiMenu>) ((AsyncResult) result).AsyncDelegate;
        act.EndInvoke(result);
      }
      catch (Exception ex)
      {
        this.LogError("EndOnCallbackCiMenuEvent exception : {0}", ex);
      }
    }

    public IAsyncResult BeginOnCallbackHeartBeatEvent(AsyncCallback callback, object asyncState)
    {
      Action act = () =>
                     {
                       if (OnHeartbeatRequestReceived != null)
                       {
                         try
                         {
                           OnHeartbeatRequestReceived();
                         }
                         catch (Exception ex)
                         {
                           this.LogError("BeginOnCallbackHeartBeatEvent exception : {0}", ex);
                         }
                       }
                     };
      return act.BeginInvoke(callback, asyncState);
    }

    public void EndOnCallbackHeartBeatEvent(IAsyncResult result)
    {
      try
      {
        var act = (Action) ((AsyncResult) result).AsyncDelegate;
        act.EndInvoke(result);
      }
      catch (Exception ex)
      {
        this.LogError("EndOnCallbackHeartBeatEvent exception : {0}", ex);
      }
    }

    #endregion

    private void EventServiceAgent_Closed(object sender, EventArgs e)
    {
      var comm = sender as ICommunicationObject;
      if (comm != null)
      {
        comm.Abort();
        comm.Close();
        Dispose();
      }
    }

    private void EventServiceAgent_Faulted(object sender, EventArgs e)
    {
      var comm = sender as ICommunicationObject;
      if (comm != null)
      {
        comm.Abort();
        comm.Close();
        Dispose();
      }
    }

    public override void Dispose()
    {
      base.Dispose();
      if (OnConnectionLost != null)
      {
        OnConnectionLost();
      }
    }
  }
}