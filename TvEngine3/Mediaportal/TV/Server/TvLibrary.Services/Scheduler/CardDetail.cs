#region Copyright (C) 2005-2011 Team MediaPortal

// Copyright (C) 2005-2011 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Mediaportal.TV.Server.TVDatabase.Entities;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Implementations.Channels;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Interfaces;

namespace Mediaportal.TV.Server.TVLibrary.Scheduler
{
  public static class IComparableExtensions
  {
    public static void SortStable<T>(this List<T> list) where T : IComparable<T>
    {
      List<T> listStableOrdered = list.OrderBy(x => x, new ComparableComparer<T>()).ToList();
      list.Clear();
      list.AddRange(listStableOrdered);
    }

    #region Nested type: ComparableComparer

    private class ComparableComparer<T> : IComparer<T> where T : IComparable<T>
    {
      #region IComparer<T> Members

      public int Compare(T x, T y)
      {
        return x.CompareTo(y);
      }

      #endregion
    }

    #endregion
  }
  /// <summary>
  /// Class which can be used to sort Cards bases on priority
  /// </summary>
  public class CardDetail : IComparable<CardDetail>
  {
    private readonly Card _card;
    private readonly int _cardId;
    private readonly IChannel _detail;
    private readonly long _frequency = -1;
    private readonly int _priority;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="id">card id</param>
    /// <param name="card">card dataaccess object</param>
    /// <param name="detail">tuning detail</param>
    /// <param name="sameTransponder">indicates whether it is the same transponder</param>
    /// <param name="numberOfOtherUsers"></param>
    /// <param name="isChannelTimeshiftingOnOtherMux"> </param>
    /// <param name="isParked"> </param>
    public CardDetail(int id, Card card, IChannel detail, bool sameTransponder, int numberOfOtherUsers, long? channelTimeshiftingOnOtherMux)
    {
      SameTransponder = sameTransponder;
      _cardId = id;
      _card = card;
      _detail = detail;
      _priority = _card.Priority;
      NumberOfOtherUsers = numberOfOtherUsers;
      ChannelTimeshiftingOnOtherMux = channelTimeshiftingOnOtherMux;

      var dvbTuningDetail = detail as DVBBaseChannel;
      if (dvbTuningDetail != null)
      {
        _frequency = dvbTuningDetail.Frequency;
      }
      else
      {
        var analogTuningDetail = detail as AnalogChannel;
        if (analogTuningDetail != null)
        {
          _frequency = analogTuningDetail.Frequency;
        }
      }
    }

    /// <summary>
    /// gets the id of the card
    /// </summary>
    public int Id
    {
      get { return _cardId; }
    }

    /// <summary>
    /// gets/sets the priority
    /// </summary>
    /// <value>The priority.</value>
    public int Priority
    {
      get { return _priority; }
    }

    /// <summary>
    /// gets the card
    /// </summary>
    public Card Card
    {
      get { return _card; }
    }

    /// <summary>
    /// gets the tuning detail
    /// </summary>
    public IChannel TuningDetail
    {
      get { return _detail; }
    }

    /// <summary>
    /// returns if it is the same transponder
    /// </summary>
    public bool SameTransponder { get; set; }

    /// <summary>
    /// gets the number of other users
    /// </summary>
    public int NumberOfOtherUsers { get; set; }

    public long? ChannelTimeshiftingOnOtherMux { get; set; }

    public long Frequency
    {
      get { return _frequency; }
    }

    // higher priority means that this one should be more to the front of the list

    #region IComparable<CardDetail> Members

    public int CompareTo(CardDetail other)
    {
      if (SameTransponder == other.SameTransponder)
      {
        if (!SameTransponder && (NumberOfOtherUsers != other.NumberOfOtherUsers))
        {
          if (NumberOfOtherUsers > other.NumberOfOtherUsers)
          {
            return 1;
          }
          if (NumberOfOtherUsers < other.NumberOfOtherUsers)
          {
            return -1;
          }
          return 0;
        }

        if (Priority > other.Priority)
        {
          return -1;
        }
        if (Priority < other.Priority)
        {
          return 1;
        }

        if (ChannelTimeshiftingOnOtherMux.HasValue && !other.ChannelTimeshiftingOnOtherMux.HasValue)
        {
          return 1;
        }

        return 0;
      }

      if (SameTransponder)
      {
        return -1;
      }
      return 1;
    }

    #endregion
  }
}