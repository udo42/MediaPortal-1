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
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Gentle.Common;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using MediaPortal.Profile;
using MediaPortal.Util;
using TvControl;
using TvDatabase;
using Action = MediaPortal.GUI.Library.Action;
using WindowPlugins;
using Layout = MediaPortal.GUI.Library.GUIFacadeControl.Layout;

namespace TvPlugin
{
  public class RadioRecorded : RecordedBase
  {

    #region Variables

    private const string _skinPrefix = "#Radio.Recorded";
    private const bool _tvRecordings = false;
    private string _thumbLocaction = Thumbs.Radio;
    private g_Player.MediaType _mediaType = g_Player.MediaType.Radio;

    #endregion

    #region Constructor

    public RadioRecorded()
    {
      GetID = (int)Window.WINDOW_RECORDEDRADIO;
    }

    #endregion

    #region implementation details

    protected override string SkinPrefix
    {
      get { return _skinPrefix; }
    }

    protected override bool TVRecordings
    {
      get { return _tvRecordings; }
    }

    protected override string ThumbLocation
    {
      get { return _thumbLocaction; }
    }

    protected override g_Player.MediaType MediaType
    {
      get { return _mediaType; }
    }

    #endregion

    #region Overrides

    protected override string SerializeName
    {
      get
      {
        return "radiorecorded";
      }
    }

    public override bool Init()
    {
      g_Player.PlayBackStopped += new g_Player.StoppedHandler(OnPlayRecordingBackStopped);
      g_Player.PlayBackEnded += new g_Player.EndedHandler(OnPlayRecordingBackEnded);
      g_Player.PlayBackStarted += new g_Player.StartedHandler(OnPlayRecordingBackStarted);
      g_Player.PlayBackChanged += new g_Player.ChangedHandler(OnPlayRecordingBackChanged);

      bool bResult = Load(GUIGraphicsContext.GetThemedSkinFile(@"\myradiorecorded.xml"));
      //GUIWindowManager.Replace((int)Window.WINDOW_RECORDEDRADIO, this);
      //Restore();
      //PreInit();
      //ResetAllControls();
      return bResult;
    }
    
    protected override void OnShowContextMenu()
    {
      int iItem = GetSelectedItemNo();
      GUIListItem pItem = GetItem(iItem);
      if (pItem == null)
      {
        return;
      }
      if (pItem.IsFolder)
      {
        return;
      }
      Recording rec = (Recording)pItem.TVTag;

      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      if (dlg == null)
      {
        return;
      }
      dlg.Reset();

      dlg.SetHeading(TVUtil.GetDisplayTitle(rec));

      dlg.AddLocalizedString(208); //Play
      dlg.AddLocalizedString(618); //Delete
      if (rec.TimesWatched > 0)
      {
        dlg.AddLocalizedString(830); //Reset watched status
      }
      if (!rec.Title.Equals("manual", StringComparison.CurrentCultureIgnoreCase))
      {
        dlg.AddLocalizedString(200072); //Upcoming episodes      
      }
      dlg.AddLocalizedString(1048); //Settings

      dlg.DoModal(GetID);
      if (dlg.SelectedLabel == -1)
      {
        return;
      }
      switch (dlg.SelectedId)
      {
        case 618: // delete
          OnDeleteRecording(iItem);
          break;

        case 208: // play
          if (OnSelectedRecording(iItem))
          {
            return;
          }
          break;

        case 1048: // Settings
          TvRecordedInfo.CurrentProgram = rec;
          GUIWindowManager.ActivateWindow((int)Window.WINDOW_TV_RECORDED_INFO);
          break;

        case 200072:
          ShowUpcomingEpisodes(rec);
          break;

        case 830: // Reset watched status
          _iSelectedItem = GetSelectedItemNo();
          ResetWatchedStatus(rec);
          LoadDirectory();
          GUIControl.SelectItemControl(GetID, facadeLayout.GetID, _iSelectedItem);
          break;
      }
    }

    protected override void InitViewSelections()
    {
      btnViews.ClearMenu();

      // Add the view options to the menu.
      int index = 0;
      btnViews.AddItem(GUILocalizeStrings.Get(914), index++); // Recordings
      btnViews.AddItem(GUILocalizeStrings.Get(135), index++); // Genres
      btnViews.AddItem(GUILocalizeStrings.Get(812), index++); // Radio stations
      btnViews.AddItem(GUILocalizeStrings.Get(636), index++); // Date

      // Have the menu select the currently selected view.
      switch (_currentDbView)
      {
        case DBView.Recordings:
          btnViews.SetSelectedItemByValue(0);
          break;
        case DBView.Genre:
          btnViews.SetSelectedItemByValue(1);
          break;
        case DBView.Channel:
          btnViews.SetSelectedItemByValue(2);
          break;
        case DBView.History:
          btnViews.SetSelectedItemByValue(3);
          break;
      }
    }

    #endregion

    #region public method

    protected override bool OnSelectedRecording(int iItem)
    {
      GUIListItem pItem = GetItem(iItem);
      if (pItem == null)
      {
        return false;
      }
      if (pItem.IsFolder)
      {
        if (pItem.Label.Equals(".."))
        {
          _currentLabel = string.Empty;
        }
        else
        {
          _currentLabel = pItem.Label;
          _rootItem = iItem;
        }
        LoadDirectory();
        if (pItem.Label.Equals(".."))
        {
          GUIControl.SelectItemControl(GetID, facadeLayout.GetID, _rootItem);
          _rootItem = 0;
        }
        return false;
      }

      // We have the Station Name in there to retrieve the correct Coverart for the station in the Vis Window
      GUIPropertyManager.RemovePlayerProperties();
      GUIPropertyManager.SetProperty("#Play.Current.ArtistThumb", pItem.Label);
      GUIPropertyManager.SetProperty("#Play.Current.Album", pItem.Label);
      GUIPropertyManager.SetProperty("#Play.Current.Thumb", pItem.ThumbnailImage);
      
      Recording rec = (Recording)pItem.TVTag;
      IList<Recording> itemlist = Recording.ListAll();

      _oActiveRecording = rec;
      _bIsLiveRecording = false;
      TvServer server = new TvServer();
      foreach (Recording recItem in itemlist)
      {
        if (rec.IdRecording == recItem.IdRecording && IsRecordingActual(recItem))
        {
          _bIsLiveRecording = true;
          break;
        }
      }

      int stoptime = rec.StopTime;
      if (_bIsLiveRecording || stoptime > 0)
      {
        GUIResumeDialog.MediaType mediaType = GUIResumeDialog.MediaType.Recording;
        if (_bIsLiveRecording)
          mediaType = GUIResumeDialog.MediaType.LiveRecording;

        GUIResumeDialog.Result result =
          GUIResumeDialog.ShowResumeDialog(TVUtil.GetDisplayTitle(rec), rec.StopTime, mediaType);

        switch (result)
        {
          case GUIResumeDialog.Result.Abort:
            return false;

          case GUIResumeDialog.Result.PlayFromBeginning:
            stoptime = 0;
            break;

          case GUIResumeDialog.Result.PlayFromLivePoint:
            stoptime = -1; // magic -1 is used for the live point
            break;

          default: // from last stop time and on error
            break;
        }
      }

      if (TVHome.Card != null)
      {
        TVHome.Card.StopTimeShifting();
      }

      return TVUtil.PlayRecording(rec, stoptime, g_Player.MediaType.RadioRecording);
    }
    
    #endregion

  }
}
