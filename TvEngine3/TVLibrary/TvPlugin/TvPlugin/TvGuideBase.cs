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

#region usings

using System;
using System.Collections.Generic;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.GUI.Video;
using MediaPortal.Player;
using MediaPortal.Profile;
using MediaPortal.Util;
using MediaPortal.Video.Database;
using TvDatabase;

#endregion

namespace TvPlugin
{
  /// <summary>
  /// 
  /// </summary>
  public class TvGuideBase : GuideBase, IMDB.IProgress
  {
    #region constants

    private const string _skinPropertyPrefix = "#TV";
    private const string _settingPrefix = "tvguide";
    private const string _settingPrefix2 = "mytv";

    #endregion

    #region ctor

    public TvGuideBase()
    {
    }

    #endregion

    #region Serialisation

    protected override void LoadSettings()
    {
      using (Settings xmlreader = new MPSettings())
      {
        String channelName = xmlreader.GetValueAsString(SettingPrefix2, "channel", String.Empty);
        TvBusinessLayer layer = new TvBusinessLayer();
        IList<Channel> channels = layer.GetChannelsByName(channelName);
        if (channels != null && channels.Count > 0)
        {
          _currentChannel = channels[0];
        }
        PositionGuideCursorToCurrentChannel();

        _byIndex = xmlreader.GetValueAsBool(SettingPrefix2, "byindex", true);
        _showChannelNumber = xmlreader.GetValueAsBool(SettingPrefix2, "showchannelnumber", false);
        _channelNumberMaxLength = xmlreader.GetValueAsInt(SettingPrefix2, "channelnumbermaxlength", 3);
        _timePerBlock = xmlreader.GetValueAsInt(SettingPrefix, "timeperblock", 30);
        _hdtvProgramText = xmlreader.GetValueAsString(SettingPrefix2, "hdtvProgramText", "(HDTV)");
        _guideContinuousScroll = xmlreader.GetValueAsBool(SettingPrefix2, "continuousScrollGuide", false);
        _loopDelay = xmlreader.GetValueAsInt("gui", "listLoopDelay", 0);

        // Load the genre map.
        if (_genreMap.Count == 0)
        {
          LoadGenreMap(xmlreader);
        }

        // Special genre rules.
        _specifyMpaaRatedAsMovie = xmlreader.GetValueAsBool("genreoptions", "specifympaaratedasmovie", false);
      }

      // Load settings defined by the skin.
      LoadSkinSettings();

      // Load genre colors.
      // If guide colors have not been loaded then attempt to load guide colors.
      if (!_guideColorsLoaded)
      {
        using (Settings xmlreader = new SKSettings())
        {
          _guideColorsLoaded = LoadGuideColors(xmlreader);
        }
      }

      _useNewRecordingButtonColor =
        Utils.FileExistsInCache(GUIGraphicsContext.GetThemedSkinFile(@"\media\tvguide_recButton_Focus_middle.png"));
      _useNewPartialRecordingButtonColor =
        Utils.FileExistsInCache(GUIGraphicsContext.GetThemedSkinFile(@"\media\tvguide_partRecButton_Focus_middle.png"));
      _useNewNotifyButtonColor =
        Utils.FileExistsInCache(GUIGraphicsContext.GetThemedSkinFile(@"\media\tvguide_notifyButton_Focus_middle.png"));
      _useHdProgramIcon =
        Utils.FileExistsInCache(GUIGraphicsContext.GetThemedSkinFile(@"\media\tvguide_hd_program.png"));
    }

    protected override void SaveSettings()
    {
      using (Settings xmlwriter = new MPSettings())
      {
        xmlwriter.SetValue(SettingPrefix2, "channel", _currentChannel.DisplayName);
        xmlwriter.SetValue(SettingPrefix, "timeperblock", _timePerBlock);
      }
    }

    #endregion

    #region overrides

    protected override string SkinPropertyPrefix
    {
      get { return _skinPropertyPrefix; }
    }

    protected override string SettingPrefix
    {
      get { return _settingPrefix; }
    }

    protected override string SettingPrefix2
    {
      get { return _settingPrefix2; }
    }

    protected override string SerializeName
    {
      get { return "tvguidebase"; }
    }

    protected override Channel CurrentChannel
    {
      get { return TVHome.Navigator.Channel; }
      set { TVHome.Navigator.UpdateCurrentChannel(); }
    }

    protected override void Play()
    {
      TVHome.ViewChannelAndCheck(_currentProgram.ReferencedChannel());
    }

    protected override int GroupCount
    {
      get { return TVHome.Navigator.Groups.Count; }
    }

    protected override int CurrentGroupIndex
    {
      get { return TVHome.Navigator.CurrentGroupIndex; }
      set { TVHome.Navigator.SetCurrentGroup(value); }
    }

    protected override string CurrentGroupName
    {
      get { return TVHome.Navigator.CurrentGroup.GroupName; }
    }

    /// <summary>
    /// Shows channel group selection dialog
    /// </summary>
    protected override void OnSelectChannelGroup()
    {
      // only if more groups present and not in singleChannelView
      if (GroupCount > 1 && !_singleChannelView)
      {
        int prevGroup = TVHome.Navigator.CurrentGroup.IdGroup;

        TVHome.OnSelectGroup();

        if (prevGroup != TVHome.Navigator.CurrentGroup.IdGroup)
        {
          GUIWaitCursor.Show();

          // group has been changed
          GetChannels(true);
          PositionGuideCursorToCurrentChannel();
          Update(false);

          SetFocus();
          GUIWaitCursor.Hide();
        }
      }
    }

    #endregion

    #region private members

    protected override string GetChannelLogo(string strChannel)
    {
      string strLogo = Utils.GetCoverArt(Thumbs.TVChannel, strChannel);
      if (string.IsNullOrEmpty(strLogo))
      {
        // Check for a default TV channel logo.
        strLogo = Utils.GetCoverArt(Thumbs.TVChannel, "default");
        if (string.IsNullOrEmpty(strLogo))
        {
          strLogo = "defaultVideoBig.png";
        }
      }
      return strLogo;
    }

    protected override void ShowContextMenu()
    {
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      if (dlg != null)
      {
        dlg.Reset();
        dlg.SetHeading(GUILocalizeStrings.Get(924)); //Menu

        if (_currentChannel != null)
        {
          dlg.AddLocalizedString(938); // View this channel
        }

        if (_currentProgram.IdProgram != 0)
        {
          dlg.AddLocalizedString(1041); //Upcoming episodes
        }

        if (_currentProgram != null && _currentProgram.StartTime > DateTime.Now)
        {
          if (_currentProgram.Notify)
          {
            dlg.AddLocalizedString(1212); // cancel reminder
          }
          else
          {
            dlg.AddLocalizedString(1040); // set reminder
          }
        }

        dlg.AddLocalizedString(939); // Switch mode

        bool isRecordingNoEPG = false;

        if (_currentProgram != null && _currentChannel != null && _currentTitle.Length > 0)
        {
          if (_currentProgram.IdProgram == 0) // no EPG program recording., only allow to stop it.
          {
            isRecordingNoEPG = IsRecordingNoEPG(_currentProgram.ReferencedChannel());
            if (isRecordingNoEPG)
            {
              dlg.AddLocalizedString(629); // stop non EPG Recording
            }
            else
            {
              dlg.AddLocalizedString(264); // start non EPG Recording
            }
          }
          else if (!_currentRecOrNotify)
          {
            dlg.AddLocalizedString(264); // Record
          }

          else
          {
            dlg.AddLocalizedString(637); // Edit Recording
          }
        }
        //dlg.AddLocalizedString(937);// Reload tvguide

        if (GroupCount > 1)
        {
          dlg.AddLocalizedString(971); // Group
        }

        dlg.AddLocalizedString(368); // IMDB

        dlg.DoModal(GetID);
        if (dlg.SelectedLabel == -1)
        {
          return;
        }
        switch (dlg.SelectedId)
        {

          case 1041:
            ShowProgramInfo();
            Log.Debug(SerializeName + ":  show episodes or repeatings for current show");
            break;
          case 368: // IMDB
            OnGetIMDBInfo();
            break;
          case 971: //group
            OnSelectChannelGroup();
            break;
          case 1040: // set reminder
          case 1212: // cancel reminder
            OnNotify();
            break;

          case 938: // view channel

            Log.Debug("viewch channel:{0}", _currentChannel);
            Play();
            if (TVHome.Card.IsTimeShifting && TVHome.Card.IdChannel == _currentProgram.ReferencedChannel().IdChannel)
            {
              g_Player.ShowFullScreenWindow();
            }
            return;


          case 939: // switch mode
            OnSwitchMode();
            break;
          case 629: //stop recording
            Schedule schedule = Schedule.FindNoEPGSchedule(_currentProgram.ReferencedChannel());
            TVUtil.DeleteRecAndEntireSchedWithPrompt(schedule);
            Update(true); //remove RED marker
            break;

          case 637: // edit recording
          case 264: // record
            if (_currentProgram.IdProgram == 0)
            {
              TVHome.StartRecordingSchedule(_currentProgram.ReferencedChannel(), true);
              _currentProgram.IsRecordingOncePending = true;
              Update(true); //remove RED marker
            }
            else
            {
              OnRecordContext();
            }
            break;
        }
      }
    }

    /// <summary>
    /// Handle the selection of a guide entry.
    /// </summary>
    /// <param name="isItemSelected"></param>
    /// <returns>true if a channel was attempted to be tuned; that the channel is or is not playing is not indicated</returns>
    protected override bool OnSelectItem(bool isItemSelected)
    {
      bool tuneAttempted = false;
      CurrentChannel = _currentChannel;
      if (_currentProgram == null)
      {
        return tuneAttempted;
      }

      if (isItemSelected)
      {
        if (_currentProgram.IsRunningAt(DateTime.Now) || _currentProgram.EndTime <= DateTime.Now)
        {
          //view this channel
          if (g_Player.Playing && g_Player.IsTVRecording)
          {
            g_Player.Stop(true);
          }
          try
          {
            string fileName = "";
            bool isRec = _currentProgram.IsRecording;

            Recording rec = null;
            if (isRec)
            {
              rec = Recording.ActiveRecording(_currentProgram.Title, _currentProgram.IdChannel);
            }


            if (rec != null)
            {
              fileName = rec.FileName;
            }

            if (!string.IsNullOrEmpty(fileName)) //are we really recording ?
            {
              Log.Info(SerializeName + ":  clicked on a currently running recording");
              GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
              if (dlg == null)
              {
                return tuneAttempted;
              }

              dlg.Reset();
              dlg.SetHeading(_currentProgram.Title);
              dlg.AddLocalizedString(979); //Play recording from beginning
              dlg.AddLocalizedString(938); //View this channel
              dlg.DoModal(GetID);

              if (dlg.SelectedLabel == -1)
              {
                return tuneAttempted;
              }
              if (_recordingList != null)
              {
                Log.Debug(SerializeName + ":  Found current program {0} in recording list", _currentTitle);
                switch (dlg.SelectedId)
                {
                  case 979: // Play recording from beginning
                    {
                      Recording recDB = Recording.Retrieve(fileName);
                      if (recDB != null)
                      {
                        TVUtil.PlayRecording(recDB);
                      }
                    }
                    return tuneAttempted;

                  case 938: // View this channel
                    {
                      Play();
                      if (g_Player.Playing)
                      {
                        g_Player.ShowFullScreenWindow();
                      }
                    }
                    tuneAttempted = true;
                    return tuneAttempted;
                }
              }
              else
              {
                Log.Info("EPG: _recordingList was not available");
              }


              if (string.IsNullOrEmpty(fileName))
              {
                Play();
                if (g_Player.Playing)
                {
                  g_Player.ShowFullScreenWindow();
                }
                tuneAttempted = true;
              }
            }
            else //not recording
            {
              // clicked the show we're currently watching
              if (CurrentChannel != null && CurrentChannel.IdChannel == _currentChannel.IdChannel && g_Player.Playing && g_Player.IsTV)
              {
                Log.Debug(SerializeName + ":  clicked on a currently running show");
                GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
                if (dlg == null)
                {
                  return tuneAttempted;
                }

                dlg.Reset();
                dlg.SetHeading(_currentProgram.Title);
                dlg.AddLocalizedString(938); //View this channel
                dlg.AddLocalizedString(1041); //Upcoming episodes
                dlg.DoModal(GetID);

                if (dlg.SelectedLabel == -1)
                {
                  return tuneAttempted;
                }

                switch (dlg.SelectedId)
                {
                  case 1041:
                    ShowProgramInfo();
                    Log.Debug(SerializeName + ":  show episodes or repeatings for current show");
                    break;
                  case 938:
                    Log.Debug(SerializeName + ":  switch currently running show to fullscreen");
                    GUIWaitCursor.Show();
                    Play();
                    GUIWaitCursor.Hide();
                    if (g_Player.Playing)
                    {
                      g_Player.ShowFullScreenWindow();
                    }
                    else
                    {
                      Log.Debug(SerializeName + ":  no show currently running to switch to fullscreen");
                    }
                    tuneAttempted = true;
                    break;
                }
              }
              else
              {
                bool isPlayingTV = (g_Player.FullScreen && g_Player.IsTV);
                // zap to selected show's channel
                TVHome.UserChannelChanged = true;
                // fixing mantis 1874: TV doesn't start when from other playing media to TVGuide & select program
                GUIWaitCursor.Show();
                Play();
                GUIWaitCursor.Hide();
                if (g_Player.Playing)
                {
                  if (isPlayingTV) GUIWindowManager.CloseCurrentWindow();
                  g_Player.ShowFullScreenWindow();
                }
                tuneAttempted = true;
              }
            } //end of not recording
          }
          finally
          {
            if (VMR9Util.g_vmr9 != null)
            {
              VMR9Util.g_vmr9.Enable(true);
            }
          }

          return tuneAttempted;
        }
        ShowProgramInfo();
        return tuneAttempted;
      }
      else
      {
        ShowProgramInfo();
      }
      return tuneAttempted;
    }

    protected override void GetChannels(bool refresh)
    {
      if (refresh || _channelList == null)
      {
        if (_channelList != null)
        {
          if (_channelList.Count < _channelCount)
          {
            _previousChannelCount = _channelList.Count;
          }
          else
          {
            _previousChannelCount = _channelCount;
          }
        }
        _channelList = new List<GuideChannel>();
      }

      if (_channelList.Count == 0)
      {
        try
        {
          if (TVHome.Navigator.CurrentGroup != null)
          {
            TvBusinessLayer layer = new TvBusinessLayer();
            IList<Channel> channels = layer.GetTVGuideChannelsForGroup(TVHome.Navigator.CurrentGroup.IdGroup);
            foreach (Channel chan in channels)
            {
              GuideChannel tvGuidChannel = new GuideChannel();
              tvGuidChannel.channel = chan;

              if (tvGuidChannel.channel.VisibleInGuide && tvGuidChannel.channel.IsTv)
              {
                if (_showChannelNumber)
                {

                  if (_byIndex)
                  {
                    tvGuidChannel.channelNum = _channelList.Count + 1;
                  }
                  else
                  {
                    foreach (TuningDetail detail in tvGuidChannel.channel.ReferringTuningDetail())
                      tvGuidChannel.channelNum = detail.ChannelNumber;
                  }
                }
                tvGuidChannel.strLogo = GetChannelLogo(tvGuidChannel.channel.DisplayName);
                _channelList.Add(tvGuidChannel);
              }
            }
          }
        }
        catch { }

        if (_channelList.Count == 0)
        {
          GuideChannel tvGuidChannel = new GuideChannel();
          tvGuidChannel.channel = new Channel(false, true, 0, DateTime.MinValue, false,
                                              DateTime.MinValue, 0, true, "", GUILocalizeStrings.Get(911));
          for (int i = 0; i < 10; ++i)
          {
            _channelList.Add(tvGuidChannel);
          }
        }
      }
    }

    #endregion

    private void OnGetIMDBInfo()
    {
      IMDBMovie movieDetails = new IMDBMovie();
      movieDetails.SearchString = _currentProgram.Title;
      if (IMDBFetcher.GetInfoFromIMDB(this, ref movieDetails, false, false))
      {
        TvBusinessLayer dbLayer = new TvBusinessLayer();

        IList<Program> progs = dbLayer.GetProgramExists(Channel.Retrieve(_currentProgram.IdChannel),
                                                        _currentProgram.StartTime, _currentProgram.EndTime);
        if (progs != null && progs.Count > 0)
        {
          Program prog = (Program)progs[0];
          prog.Description = movieDetails.Plot;
          // prog.Genre = movieDetails.Genre;
          prog.StarRating = (int)movieDetails.Rating;
          prog.Persist();
        }
        GUIVideoInfo videoInfo = (GUIVideoInfo)GUIWindowManager.GetWindow((int)Window.WINDOW_VIDEO_INFO);
        videoInfo.Movie = movieDetails;
        GUIButtonControl btnPlay = (GUIButtonControl)videoInfo.GetControl(2);
        btnPlay.Visible = false;
        GUIWindowManager.ActivateWindow((int)Window.WINDOW_VIDEO_INFO);
      }
      else
      {
        Log.Info("IMDB Fetcher: Nothing found");
      }
    }

    #region IMDB.IProgress

    public bool OnDisableCancel(IMDBFetcher fetcher)
    {
      GUIDialogProgress pDlgProgress =
        (GUIDialogProgress)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_PROGRESS);
      if (pDlgProgress.IsInstance(fetcher))
      {
        pDlgProgress.DisableCancel(true);
      }
      return true;
    }

    public void OnProgress(string line1, string line2, string line3, int percent)
    {
      if (!GUIWindowManager.IsRouted)
      {
        return;
      }
      GUIDialogProgress pDlgProgress =
        (GUIDialogProgress)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_PROGRESS);
      pDlgProgress.ShowProgressBar(true);
      pDlgProgress.SetLine(1, line1);
      pDlgProgress.SetLine(2, line2);
      if (percent > 0)
      {
        pDlgProgress.SetPercentage(percent);
      }
      pDlgProgress.Progress();
    }

    public bool OnSearchStarting(IMDBFetcher fetcher)
    {
      GUIDialogProgress pDlgProgress =
        (GUIDialogProgress)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_PROGRESS);
      // show dialog that we're busy querying www.imdb.com
      pDlgProgress.Reset();
      pDlgProgress.SetHeading(GUILocalizeStrings.Get(197));
      pDlgProgress.SetLine(1, fetcher.MovieName);
      pDlgProgress.SetLine(2, string.Empty);
      pDlgProgress.SetObject(fetcher);
      pDlgProgress.StartModal(GUIWindowManager.ActiveWindow);
      return true;
    }

    public bool OnSearchStarted(IMDBFetcher fetcher)
    {
      GUIDialogProgress pDlgProgress =
        (GUIDialogProgress)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_PROGRESS);
      pDlgProgress.SetObject(fetcher);
      pDlgProgress.DoModal(GUIWindowManager.ActiveWindow);
      if (pDlgProgress.IsCanceled)
      {
        return false;
      }
      return true;
    }

    public bool OnSearchEnd(IMDBFetcher fetcher)
    {
      GUIDialogProgress pDlgProgress =
        (GUIDialogProgress)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_PROGRESS);
      if ((pDlgProgress != null) && (pDlgProgress.IsInstance(fetcher)))
      {
        pDlgProgress.Close();
      }
      return true;
    }

    public bool OnMovieNotFound(IMDBFetcher fetcher)
    {
      Log.Info("IMDB Fetcher: OnMovieNotFound");
      // show dialog...
      GUIDialogOK pDlgOK = (GUIDialogOK)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_OK);
      pDlgOK.SetHeading(195);
      pDlgOK.SetLine(1, fetcher.MovieName);
      pDlgOK.SetLine(2, string.Empty);
      pDlgOK.DoModal(GUIWindowManager.ActiveWindow);
      return true;
    }

    public bool OnDetailsStarting(IMDBFetcher fetcher)
    {
      GUIDialogProgress pDlgProgress =
        (GUIDialogProgress)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_PROGRESS);
      // show dialog that we're downloading the movie info
      pDlgProgress.Reset();
      pDlgProgress.SetHeading(GUILocalizeStrings.Get(198));
      //pDlgProgress.SetLine(0, strMovieName);
      pDlgProgress.SetLine(1, fetcher.MovieName);
      pDlgProgress.SetLine(2, string.Empty);
      pDlgProgress.SetObject(fetcher);
      pDlgProgress.StartModal(GUIWindowManager.ActiveWindow);
      return true;
    }

    public bool OnDetailsStarted(IMDBFetcher fetcher)
    {
      GUIDialogProgress pDlgProgress =
        (GUIDialogProgress)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_PROGRESS);
      pDlgProgress.SetObject(fetcher);
      pDlgProgress.DoModal(GUIWindowManager.ActiveWindow);
      if (pDlgProgress.IsCanceled)
      {
        return false;
      }
      return true;
    }

    public bool OnDetailsEnd(IMDBFetcher fetcher)
    {
      GUIDialogProgress pDlgProgress =
        (GUIDialogProgress)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_PROGRESS);
      if ((pDlgProgress != null) && (pDlgProgress.IsInstance(fetcher)))
      {
        pDlgProgress.Close();
      }
      return true;
    }

    public bool OnActorsStarting(IMDBFetcher fetcher)
    {
      // won't occure
      return true;
    }

    public bool OnActorsStarted(IMDBFetcher fetcher)
    {
      // won't occure
      return true;
    }

    public bool OnActorsEnd(IMDBFetcher fetcher)
    {
      // won't occure
      return true;
    }

    public bool OnActorInfoStarting(IMDBFetcher fetcher)
    {
      // won't occure
      return true;
    }

    public bool OnSelectActor(IMDBFetcher fetcher, out int selected)
    {
      // won't occure
      selected = 0;
      return true;
    }

    public bool OnDetailsNotFound(IMDBFetcher fetcher)
    {
      Log.Info("IMDB Fetcher: OnDetailsNotFound");
      // show dialog...
      GUIDialogOK pDlgOK = (GUIDialogOK)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_OK);
      // show dialog...
      pDlgOK.SetHeading(195);
      pDlgOK.SetLine(1, fetcher.MovieName);
      pDlgOK.SetLine(2, string.Empty);
      pDlgOK.DoModal(GUIWindowManager.ActiveWindow);
      return false;
    }

    public bool OnRequestMovieTitle(IMDBFetcher fetcher, out string movieName)
    {
      movieName = fetcher.MovieName;
      if (GetKeyboard(ref movieName))
      {
        if (movieName == string.Empty)
        {
          return false;
        }
        return true;
      }
      movieName = string.Empty;
      return false;
    }

    public bool OnSelectMovie(IMDBFetcher fetcher, out int selectedMovie)
    {
      GUIDialogSelect pDlgSelect = (GUIDialogSelect)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_SELECT);
      // more then 1 movie found
      // ask user to select 1
      pDlgSelect.Reset();
      pDlgSelect.SetHeading(196); //select movie
      for (int i = 0; i < fetcher.Count; ++i)
      {
        pDlgSelect.Add(fetcher[i].Title);
      }
      pDlgSelect.EnableButton(true);
      pDlgSelect.SetButtonLabel(413); // manual
      pDlgSelect.DoModal(GUIWindowManager.ActiveWindow);

      // and wait till user selects one
      selectedMovie = pDlgSelect.SelectedLabel;
      if (pDlgSelect.IsButtonPressed)
      {
        return true;
      }
      if (selectedMovie == -1)
      {
        return false;
      }
      return true;
    }

    public bool OnScanStart(int total)
    {
      // won't occure
      return true;
    }

    public bool OnScanEnd()
    {
      // won't occure
      return true;
    }

    public bool OnScanIterating(int count)
    {
      // won't occure
      return true;
    }

    public bool OnScanIterated(int count)
    {
      // won't occure
      return true;
    }

    #endregion
  }
}
