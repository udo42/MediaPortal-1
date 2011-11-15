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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using MediaPortal.Configuration;
using MediaPortal.Database;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.Picture.Database;
using MediaPortal.Player;
using MediaPortal.ProcessPlugins.MiniDisplayPlugin;
using MediaPortal.Profile;
using MediaPortal.Util;
using Action = MediaPortal.GUI.Library.Action;

namespace MediaPortal.GUI.Settings
{
  /// <summary>
  /// Change and tweak settings like Video codecs, skins, etc from inside MP
  /// </summary>
  [PluginIcons("WindowPlugins.GUISettings.Settings.gif", "WindowPlugins.GUISettings.SettingsDisabled.gif")]
  public class GUISettings : GUIInternalWindow, ISetupForm, IShowPlugin
  {
    [SkinControl(11)] protected GUIButtonControl btnMiniDisplay = null;
    [SkinControl(10)] protected GUIButtonControl btnTV = null;

    [DllImport("shlwapi.dll")]
    private static extern bool PathIsNetworkPath(string Path);

    private static bool _settingsChanged = false;
    
    public GUISettings()
    {
      GetID = (int)Window.WINDOW_SETTINGS;
    }

    public override bool Init()
    {
      return Load(GUIGraphicsContext.Skin + @"\settings.xml");
    }

    public override void OnAction(Action action)
    {
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

    public override bool OnMessage(GUIMessage message)
    {
      switch (message.Message)
      {
        case GUIMessage.MessageType.GUI_MSG_WINDOW_INIT:
          {
            base.OnMessage(message);
            btnMiniDisplay.Visible = MiniDisplayHelper.IsSetupAvailable();
            btnTV.Visible = Util.Utils.UsingTvServer;
            return true;
          }

        case GUIMessage.MessageType.GUI_MSG_WINDOW_DEINIT: {}
          break;
      }
      return base.OnMessage(message);
    }

    protected override void OnPageLoad()
    {
      if (!IsGUISettingsWindow(GUIWindowManager.GetPreviousActiveWindow()))
      {
        _settingsChanged = false;
      }

      base.OnPageLoad();
    }

    protected override void OnPageDestroy(int new_windowId)
    {
      if (_settingsChanged && !IsGUISettingsWindow(new_windowId))
      {
        OnRestartMP();
      }

      base.OnPageDestroy(new_windowId);
    }

    public static bool IsGUISettingsWindow(int windowId)
    {
      if (windowId == (int)Window.WINDOW_SETTINGS_DVD ||
          windowId == (int)Window.WINDOW_SETTINGS_DYNAMIC_REFRESHRATE ||
          windowId == (int)Window.WINDOW_SETTINGS_EXTENSIONS ||
          windowId == (int)Window.WINDOW_SETTINGS_FOLDERS ||
          windowId == (int)Window.WINDOW_SETTINGS_GENERALMP ||
          windowId == (int)Window.WINDOW_SETTINGS_GUI ||
          windowId == (int)Window.WINDOW_SETTINGS_MOVIES ||
          windowId == (int)Window.WINDOW_SETTINGS_MUSIC ||
          windowId == (int)Window.WINDOW_SETTINGS_MUSICDATABASE ||
          windowId == (int)Window.WINDOW_SETTINGS_MUSICNOWPLAYING ||
          windowId == (int)Window.WINDOW_SETTINGS_ONSCREEN_DISPLAY ||
          windowId == (int)Window.WINDOW_SETTINGS_PICTURES ||
          windowId == (int)Window.WINDOW_SETTINGS_PICTURES_SLIDESHOW ||
          windowId == (int)Window.WINDOW_SETTINGS_PICTURESDATABASE ||
          windowId == (int)Window.WINDOW_SETTINGS_PLAYLIST ||
          windowId == (int)Window.WINDOW_SETTINGS_RECORDINGS ||
          windowId == (int)Window.WINDOW_SETTINGS_SCREEN ||
          windowId == (int)Window.WINDOW_SETTINGS_SCREENSAVER ||
          windowId == (int)Window.WINDOW_SETTINGS_SKIN ||
          windowId == (int)Window.WINDOW_SETTINGS_SKIPSTEPS ||
          windowId == (int)Window.WINDOW_SETTINGS_SORT_CHANNELS ||
          windowId == (int)Window.WINDOW_SETTINGS_THUMBNAILS ||
          windowId == (int)Window.WINDOW_SETTINGS_TV ||
          windowId == (int)Window.WINDOW_SETTINGS_TV_EPG ||
          windowId == (int)Window.WINDOW_SETTINGS_VIDEODATABASE ||
          windowId == (int)Window.WINDOW_SETTINGS_VOLUME ||
        // Minidisplay (no enum values in GUIWindow)
          windowId == 9000 ||
          windowId == 9001 ||
          windowId == 9002 ||
          windowId == 9003 ||
          windowId == 9004 ||
          windowId == 9005 ||
          windowId == 9006)
      {
        return true;
      }
      return false;
    }

    public static bool SettingsChanged
    {
      get { return _settingsChanged; }
      set { _settingsChanged = value; }
    }

    #region RestartMP

    private void OnRestartMP()
    {
      GUIDialogYesNo dlgYesNo = (GUIDialogYesNo)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_YES_NO);
      if (null == dlgYesNo)
      {
        return;
      }
      dlgYesNo.SetHeading(927);

      dlgYesNo.SetLine(1, "Settings changed!");
      dlgYesNo.SetLine(2, "Do you want to restart MediaPortal?");
      dlgYesNo.DoModal(GetID);

      if (!dlgYesNo.IsConfirmed)
      {
        return;
      }
      else
      {
        // Will be changed to one liner after SE patch
        bool hideTaskBar = false;

        using (MediaPortal.Profile.Settings xmlreader = new MPSettings())
        {
          hideTaskBar = xmlreader.GetValueAsBool("general", "hidetaskbar", false);
        }

        if (hideTaskBar)
        {
          // only re-show the startbar if MP is the one that has hidden it.
          Win32API.EnableStartBar(true);
          Win32API.ShowStartBar(true);
        }
        Log.Info("Settings: OnRestart - prepare for restart!");
        File.Delete(Config.GetFile(Config.Dir.Config, "mediaportal.running"));
        Log.Info("Settings: OnRestart - saving settings...");
        Profile.Settings.SaveCache();
        DisposeDBs();
        VolumeHandler.Dispose();
        Log.Info("Main: OnSuspend - Done");
        Process restartScript = new Process();
        restartScript.EnableRaisingEvents = false;
        restartScript.StartInfo.WorkingDirectory = Config.GetFolder(Config.Dir.Base);
        restartScript.StartInfo.FileName = Config.GetFile(Config.Dir.Base, @"restart.vbs");
        Log.Debug("Settings: OnRestart - executing script {0}", restartScript.StartInfo.FileName);
        restartScript.Start();
        try
        {
          // Maybe the scripting host is not available therefore do not wait infinitely.
          if (!restartScript.HasExited)
          {
            restartScript.WaitForExit();
          }
        }
        catch (Exception ex)
        {
          Log.Error("Settings: OnRestart - WaitForExit: {0}", ex.Message);
        }
      }
    }

    private void DisposeDBs()
    {
      string dbPath = FolderSettings.DatabaseName;
      bool isRemotePath = (!string.IsNullOrEmpty(dbPath) && PathIsNetworkPath(dbPath));
      if (isRemotePath)
      {
        Log.Info("Settings: disposing FolderDatabase3 sqllite database.");
        FolderSettings.Dispose();
      }

      dbPath = PictureDatabase.DatabaseName;
      isRemotePath = (!string.IsNullOrEmpty(dbPath) && PathIsNetworkPath(dbPath));
      if (isRemotePath)
      {
        Log.Info("Settings: disposing PictureDatabase sqllite database.");
        PictureDatabase.Dispose();
      }

      dbPath = MediaPortal.Video.Database.VideoDatabase.DatabaseName;
      isRemotePath = (!string.IsNullOrEmpty(dbPath) && PathIsNetworkPath(dbPath));
      if (isRemotePath)
      {
        Log.Info("Settings: disposing VideoDatabaseV5.db3 sqllite database.");
        MediaPortal.Video.Database.VideoDatabase.Dispose();
      }

      dbPath = MediaPortal.Music.Database.MusicDatabase.Instance.DatabaseName;
      isRemotePath = (!string.IsNullOrEmpty(dbPath) && PathIsNetworkPath(dbPath));
      if (isRemotePath)
      {
        Log.Info("Settings: disposing MusicDatabase db3 sqllite database.");
        MediaPortal.Music.Database.MusicDatabase.Dispose();
      }
    }

    #endregion

    #region ISetupForm Members

    public bool CanEnable()
    {
      return true;
    }

    public bool HasSetup()
    {
      return false;
    }

    public string PluginName()
    {
      return "Settings";
    }

    public bool DefaultEnabled()
    {
      return true;
    }

    public int GetWindowId()
    {
      return GetID;
    }

    public bool GetHome(out string strButtonText, out string strButtonImage, out string strButtonImageFocus,
                        out string strPictureImage)
    {
      strButtonText = GUILocalizeStrings.Get(5);
      strButtonImage = "";
      strButtonImageFocus = "";
      strPictureImage = "";
      return true;
    }

    public string Author()
    {
      return "Frodo";
    }

    public string Description()
    {
      return "Configure MediaPortal using graphical user interface";
    }

    public void ShowPlugin() {}

    #endregion

    #region IShowPlugin Members

    public bool ShowDefaultHome()
    {
      return true;
    }
    
    #endregion
  }
}