#region Copyright (C) 2005-2010 Team MediaPortal

// Copyright (C) 2005-2010 Team MediaPortal
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
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using Mediaportal.TV.Server.TVControl.ServiceAgents;
using Mediaportal.TV.Server.TVDatabase.Entities;
using Mediaportal.TV.Server.TVDatabase.Entities.Enums;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Logging;
using Mediaportal.TV.TvPlugin.Helper;
using Action = MediaPortal.GUI.Library.Action;

namespace Mediaportal.TV.TvPlugin
{
  public class TvNewScheduleSearch : GUIInternalWindow
  {
    #region enums

    public enum SearchType
    {
      Title = 0,
      KeyWord,
      Genres,
    } ;

    #endregion

    #region variables

    private static SearchType _searchType = SearchType.Title;

    private Action LastAction; // Keeps the Last received Action from the OnAction Methode
    private int LastActionTime; // stores the time of the last action from the OnAction Methode
    public bool _refreshList;
    public string _searchKeyword = string.Empty;
    [SkinControl(9)] protected GUIButtonControl btnSearchGenre;
    [SkinControl(8)] protected GUIButtonControl btnSearchKeyword;
    [SkinControl(7)] protected GUIButtonControl btnSearchTitle;
    [SkinControl(50)] protected GUIListControl listResults;

    #endregion

    public TvNewScheduleSearch()
    {
      this.LogInfo("newsearch ctor");
      GetID = (int)Window.WINDOW_TV_SEARCH;
    }

    public override bool IsTv
    {
      get { return true; }
    }

    public static SearchType SearchFor
    {
      get { return _searchType; }
      set { _searchType = value; }
    }

    ~TvNewScheduleSearch() {}

    public override bool Init()
    {
      this.LogInfo("newsearch init");
      bool bResult = Load(GUIGraphicsContext.Skin + @"\mytvschedulerserverSearch.xml");

      this.LogInfo("newsearch init result:{0}", bResult);
      return bResult;
    }

    public override void OnAction(Action action)
    {
      if (LastActionTime + 100 > Environment.TickCount && action == LastAction)
      {
        return;
        // don't do anything if the keypress is comes to soon after the previos one and the action is the same as before.
      }

      switch (action.wID)
      {
        case Action.ActionType.ACTION_PREVIOUS_MENU:
          {
            GUIWindowManager.ShowPreviousWindow();
            return;
          }
      }
      base.OnAction(action);
    }


    protected override void OnPageLoad()
    {
      _searchKeyword = string.Empty;
      this.LogInfo("newsearch OnPageLoad");
      base.OnPageLoad();
    }

    protected override void OnPageDestroy(int new_windowId)
    {
      this.LogInfo("newsearch OnPageDestroy");
      base.OnPageDestroy(new_windowId);
    }

    protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
    {
      if (control == listResults)
      {
        var msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_ITEM_SELECTED, GetID, 0, listResults.GetID, 0, 0,
                                        null);
        OnMessage(msg);
        int iItem = msg.Param1;
        if (actionType == Action.ActionType.ACTION_SELECT_ITEM)
        {
          OnClick(iItem);
        }
      }

      bool searchButtonClicked = false;
      
      if (control == btnSearchTitle)
      {
        SearchFor = SearchType.Title;
        searchButtonClicked = true;
      }
      if (control == btnSearchGenre)
      {
        SearchFor = SearchType.Genres;
        searchButtonClicked = true;
      }
      if (control == btnSearchKeyword)
      {
        SearchFor = SearchType.KeyWord;
        searchButtonClicked = true;
      }

      if (searchButtonClicked)
      {
        string searchKeyword = _searchKeyword;
        if (GetKeyboard(ref searchKeyword) && !string.IsNullOrEmpty(searchKeyword))
        {
          if (searchKeyword != _searchKeyword)
          {
            _searchKeyword = searchKeyword;
            _refreshList = true;
          }
        }
        return;
      }

      base.OnClicked(controlId, control, actionType);
    }

    private bool GetKeyboard(ref string strLine)
    {
      var keyboard = (VirtualKeyboard)GUIWindowManager.GetWindow((int)Window.WINDOW_VIRTUAL_KEYBOARD);
      if (null == keyboard)
      {
        return false;
      }
      keyboard.Reset();
      keyboard.Text = strLine;
      keyboard.DoModal(GetID);
      if (keyboard.IsConfirmed)
      {
        strLine = keyboard.Text;
        return true;
      }
      return false;
    }

    private GUIListItem GetItem(int index)
    {
      if (index < 0 || index >= listResults.Count)
      {
        return null;
      }
      return listResults[index];
    }

    private void OnClick(int itemNo)
    {
      GUIListItem item = GetItem(itemNo);
      if (item == null)
      {
        return;
      }
      TVProgramInfo.CurrentProgram = item.TVTag as Program;
      if (TVProgramInfo.CurrentProgram != null)
      {
        GUIWindowManager.ActivateWindow((int)Window.WINDOW_TV_PROGRAM_INFO);
      }
      return;
    }

    public override void Process()
    {
      base.Process();
      if (_refreshList)
      {
        Search();
        _refreshList = false;
      }
      TVHome.UpdateProgressPercentageBar();
    }

    private void Search()
    {
      this.LogInfo("newsearch Search:{0} {1}", _searchKeyword, SearchFor);
      GUIControl.ClearControl(GetID, listResults.GetID);
      IList<Program> listPrograms = null;
      StringComparisonEnum stringComparison = StringComparisonEnum.StartsWith;
      stringComparison |= StringComparisonEnum.EndsWith;
      switch (SearchFor)
      {
        case SearchType.Genres:
          StringComparisonEnum stringComparisonCategory = StringComparisonEnum.StartsWith;
            stringComparisonCategory |= StringComparisonEnum.EndsWith;
            listPrograms = ServiceAgents.Instance.ProgramServiceAgent.GetProgramsByTitleAndCategoryAndMediaType(_searchKeyword, "",
                                                                                      MediaTypeEnum.TV,
                                                                                      stringComparisonCategory,
                                                                                      StringComparisonEnum.StartsWith).ToList();          
          break;
        case SearchType.KeyWord:
          listPrograms = ServiceAgents.Instance.ProgramServiceAgent.GetProgramsByDescription("%" + _searchKeyword, stringComparison).ToList();
          break;
        case SearchType.Title:
          listPrograms = ServiceAgents.Instance.ProgramServiceAgent.GetProgramsByTitle(_searchKeyword, stringComparison).ToList();          
          break;
      }
      if (listPrograms == null)
      {
        return;
      }
      if (listPrograms.Count == 0)
      {
        return;
      }
      this.LogInfo("newsearch found:{0} progs", listPrograms.Count);
      foreach (Program program in listPrograms)
      {
        var item = new GUIListItem();
        item.Label = TVUtil.GetDisplayTitle(program);
        string logo = Utils.GetCoverArt(Thumbs.TVChannel, program.Channel.DisplayName);
        if (string.IsNullOrEmpty(logo))                            
        {
          logo = "defaultVideoBig.png";
        }
        item.ThumbnailImage = logo;
        item.IconImageBig = logo;
        item.IconImage = logo;
        item.TVTag = program;
        listResults.Add(item);
      }
    }
  }
}