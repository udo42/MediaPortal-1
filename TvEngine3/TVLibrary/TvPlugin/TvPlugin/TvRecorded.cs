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
using MediaPortal.Services;
using MediaPortal.Threading;
using MediaPortal.Util;
using TvControl;
using TvDatabase;

namespace TvPlugin
{
  public class TvRecorded : RecordedBase
  {
    #region ThumbCacher

    public class RecordingThumbCacher
    {
      private Work work;

      public RecordingThumbCacher()
      {
        work = new Work(new DoWorkHandler(this.PerformRequest));
        work.ThreadPriority = ThreadPriority.BelowNormal;
        GlobalServiceProvider.Get<IThreadPool>().Add(work, QueuePriority.Low);
      }

      private void PerformRequest()
      {
        if (_thumbCreationActive)
        {
          return;
        }
        try
        {
          _thumbCreationActive = true;

          IList<Recording> recordings = Recording.ListAll();
          for (int i = recordings.Count - 1; i >= 0; i--)
          {
            string recFileName = TVUtil.GetFileNameForRecording(recordings[i]);
            string thumbNail = string.Format("{0}\\{1}{2}", Thumbs.TVRecorded,
                                             Path.ChangeExtension(Utils.SplitFilename(recFileName), null),
                                             Utils.GetThumbExtension());

            if ((!TVHome.UseRTSP()) && !Utils.FileExistsInCache(thumbNail))
            {
              //Log.Info("RecordedTV: No thumbnail found at {0} for recording {1} - grabbing from file now", thumbNail, rec.FileName);

              //if (!DvrMsImageGrabber.GrabFrame(rec.FileName, thumbNail))
              //  Log.Info("GUIRecordedTV: No thumbnail created for {0}", Utils.SplitFilename(rec.FileName));
              try
              {
                Thread.Sleep(250);
                //MediaInfoWrapper recinfo = new MediaInfoWrapper(recFileName);
                //if (recinfo.IsH264)
                //{
                //  Log.Info("RecordedTV: Thumbnail creation not supported for h.264 file - {0}", Utils.SplitFilename(recFileName));
                //}
                //else
                //{
                if (VideoThumbCreator.CreateVideoThumb(recFileName, thumbNail, true, true))
                {
                  Log.Info("RecordedTV: Thumbnail successfully created for - {0}", Utils.SplitFilename(recFileName));
                }
                else
                {
                  Log.Info("RecordedTV: No thumbnail created for - {0}", Utils.SplitFilename(recFileName));
                }
                Thread.Sleep(250);
                //}

                // The .NET3 way....
                //
                //MediaPlayer player = new MediaPlayer();
                //player.Open(new Uri(rec.FileName, UriKind.Absolute));
                //player.ScrubbingEnabled = true;
                //player.Play();
                //player.Pause();
                //// Grab the frame 10 minutes after start to respect pre-recording times.
                //player.Position = new TimeSpan(0, 10, 0);
                //System.Threading.Thread.Sleep(5000);
                //RenderTargetBitmap rtb = new RenderTargetBitmap(720, 576, 1 / 200, 1 / 200, PixelFormats.Pbgra32);
                //DrawingVisual dv = new DrawingVisual();
                //DrawingContext dc = dv.RenderOpen();
                //dc.DrawVideo(player, new Rect(0, 0, 720, 576));
                //dc.Close();
                //rtb.Render(dv);
                //PngBitmapEncoder encoder = new PngBitmapEncoder();
                //encoder.Frames.Add(BitmapFrame.Create(rtb));
                //using (FileStream stream = new FileStream(thumbNail, FileMode.OpenOrCreate))
                //{
                //  encoder.Save(stream);
                //}
                //player.Stop();
                //player.Close();
              }
              catch (Exception ex)
              {
                Log.Error("RecordedTV: No thumbnail created for {0} - {1}", Utils.SplitFilename(recFileName),
                          ex.Message);
              }
            }
          }
        }
        finally
        {
          _thumbCreationActive = false;
        }
      }
    }

    #endregion

    #region Variables

    private static bool _thumbCreationActive = false;
    private static bool _createRecordedThumbs = true;
    private const string _skinPrefix = "#TV.RecordedTV";
    private const bool _tvRecordings = true;
    private string _thumbLocaction = Thumbs.TVChannel;
    private g_Player.MediaType _mediaType = g_Player.MediaType.Recording;
    private RecordingThumbCacher thumbworker = null;

    #endregion

    #region Constructor

    public TvRecorded()
    {
      GetID = (int)Window.WINDOW_RECORDEDTV;
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

    #region Serialisation

    protected override void LoadSettings()
    {
      base.LoadSettings();
      using (Settings xmlreader = new MPSettings())
      {
        _createRecordedThumbs = xmlreader.GetValueAsBool("thumbnails", "tvrecordedondemand", true);
      }

      thumbworker = null;
    }

    #endregion

    #region Overrides

    protected override string SerializeName
    {
      get
      {
        return "tvrecorded";
      }
    }

    public override bool Init()
    {
      g_Player.PlayBackStopped += new g_Player.StoppedHandler(OnPlayRecordingBackStopped);
      g_Player.PlayBackEnded += new g_Player.EndedHandler(OnPlayRecordingBackEnded);
      g_Player.PlayBackStarted += new g_Player.StartedHandler(OnPlayRecordingBackStarted);
      g_Player.PlayBackChanged += new g_Player.ChangedHandler(OnPlayRecordingBackChanged);

      bool bResult = Load(GUIGraphicsContext.GetThemedSkinFile(@"\mytvrecordedtv.xml"));
      //LoadSettings();
      GUIWindowManager.Replace((int)Window.WINDOW_RECORDEDTV, this);
      Restore();
      PreInit();
      ResetAllControls();
      return bResult;
    }

    protected override void OnPageLoad()
    {
      base.OnPageLoad();
      if (thumbworker == null)
      {
        if (_createRecordedThumbs)
        {
          _createRecordedThumbs = (!TVHome.UseRTSP());

          if (!_createRecordedThumbs)
          {
            Log.Info("GUIRecordedTV: skipping thumbworker thread - RTSP mode is in use");
          }
          else
          {
            thumbworker = new RecordingThumbCacher();
          }
        }
      }
      else
      {
        Log.Info("GUIRecordedTV: thumbworker already running - didn't start another one");
      }
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

      dlg.AddLocalizedString(655); //Play recorded tv
      dlg.AddLocalizedString(656); //Delete recorded tv
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
        case 656: // delete
          OnDeleteRecording(iItem);
          break;

        case 655: // play
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
      btnViews.AddItem(GUILocalizeStrings.Get(915), index++); // TV Channels
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

      return TVUtil.PlayRecording(rec, stoptime);
    }

    #endregion

  }
}