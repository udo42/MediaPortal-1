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
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MediaPortal.Configuration;
using MediaPortal.Database;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using MediaPortal.Util;
using Microsoft.DirectX.Direct3D;
using Microsoft.Win32;
using Action = MediaPortal.GUI.Library.Action;

namespace MediaPortal.GUI.Settings
{
  /// <summary>
  /// Summary description for Class1.
  /// </summary>
  public class GUISettingsGeneralMP : GUIInternalWindow
  {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class DISPLAY_DEVICE
    {
      public int cb = 0;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
      public string DeviceName = new String(' ', 32);
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
      public string DeviceString = new String(' ', 128);
      public int StateFlags = 0;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
      public string DeviceID = new String(' ', 128);
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
      public string DeviceKey = new String(' ', 128);
    }
      
    [DllImport("user32.dll")]
      public static extern bool EnumDisplayDevices(string lpDevice,
                                                   int iDevNum, [In, Out] DISPLAY_DEVICE lpDisplayDevice, int dwFlags);
    

    [SkinControl(2)] protected GUIButtonControl btnLog = null;
    [SkinControl(3)] protected GUIButtonControl btnProcess = null;
    [SkinControl(4)] protected GUICheckButton cmWatchdog = null;
    [SkinControl(5)] protected GUICheckButton cmAutoRestart = null;

    [SkinControl(10)] protected GUICheckButton cmStartfullscreen = null;
    [SkinControl(11)] protected GUICheckButton cmUsefullscreensplash = null;
    [SkinControl(12)] protected GUICheckButton cmAlwaysontop = null;
    [SkinControl(13)] protected GUICheckButton cmHidetaskbar = null;
    [SkinControl(14)] protected GUICheckButton cmAutostart = null;
    [SkinControl(15)] protected GUICheckButton cmMinimizeonstartup = null;
    [SkinControl(16)] protected GUICheckButton cmMinimizeonexit = null;
    [SkinControl(17)] protected GUICheckButton cmTurnoffmonitor = null;
    [SkinControl(18)] protected GUICheckButton cmTurnmonitoronafterresume = null;
    [SkinControl(19)] protected GUICheckButton cmEnables3trick = null;
    [SkinControl(20)] protected GUICheckButton cmUseS3Hack = null;
    [SkinControl(21)] protected GUICheckButton cmRestartonresume = null;
    [SkinControl(22)] protected GUICheckButton cmShowlastactivemodule = null;
    [SkinControl(23)] protected GUICheckButton cmUsescreenselector = null;
    [SkinControl(24)] protected GUIButtonControl btnShowScreens = null;

    [SkinControl(30)] protected GUIButtonControl btnDelayStartup= null;
    [SkinControl(31)] protected GUICheckButton cmDelayStartup = null;
    [SkinControl(32)] protected GUICheckButton cmDelayResume = null;


    private enum Controls
    {
      CONTROL_DELAYINSEC = 6
    } ;
    
    private enum Priority
    {
      High = 0,
      AboveNormal = 1,
      Normal = 2,
      BelowNormal =3
    }

    private int iDelay = 10;
    private string loglevel = "2"; // 0= Error, 1= warning, 2 = info, 3 = debug
    private string priority = "Normal";
    private int iStartUpDelay = 0;
    private bool settingsChanged = false;
    private bool hideTaskBar = false;

    private int screennumber = 0; // 0 is the primary screen
    private ArrayList screenCollection = new ArrayList();

    [DllImport("shlwapi.dll")]
    private static extern bool PathIsNetworkPath(string Path);

    public GUISettingsGeneralMP()
    {
      GetID = (int)Window.WINDOW_SETTINGS_GENERALMP;
    }
    
    public override bool Init()
    {
      return Load(GUIGraphicsContext.Skin + @"\settings_generalMP.xml");
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
            GUIPropertyManager.SetProperty("#currentmodule", GUILocalizeStrings.Get(100008));
            LoadSettings();
            
            GUIControl.ClearControl(GetID, (int)Controls.CONTROL_DELAYINSEC);
            for (int i = 1; i <= 100; ++i)
            {
              GUIControl.AddItemLabelControl(GetID, (int)Controls.CONTROL_DELAYINSEC, i.ToString());
            }
            
            GUIControl.SelectItemControl(GetID, (int)Controls.CONTROL_DELAYINSEC, iDelay - 1);
            
            return true;
          }

        case GUIMessage.MessageType.GUI_MSG_WINDOW_DEINIT:
          {
            SaveSettings();
            
            // Ask to restart MP if settings changed
            if (settingsChanged)
            {
              OnRestartMP();
            }
          }
          break;

        case GUIMessage.MessageType.GUI_MSG_CLICKED:
          {
            int iControl = message.SenderControlId;
            if (iControl == (int)Controls.CONTROL_DELAYINSEC)
            {
              string strLabel = message.Label;
              iDelay = Int32.Parse(strLabel);
              settingsChanged = true;
            }
          }
          break;
      }
      return base.OnMessage(message);
    }

    protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
    {
      if (control == btnLog)
      {
        OnLog();
      }
      if (control == btnProcess)
      {
        OnProcess();
      }
      if (control == cmWatchdog)
      {
        settingsChanged = true;
      }
      if (control == cmAutoRestart)
      {
        if (!cmAutoRestart.Selected)
        {
          GUIControl.HideControl(GetID, (int)Controls.CONTROL_DELAYINSEC);
        }
        else
        {
          GUIControl.ShowControl(GetID, (int)Controls.CONTROL_DELAYINSEC);
        }
        settingsChanged = true;
      }
      // Startup/Resume
      if (control == cmStartfullscreen)
      {
        settingsChanged = true;
      }
      if (control == cmUsefullscreensplash)
      {
        settingsChanged = true;
      }
      if (control == cmAlwaysontop)
      {
        settingsChanged = true;
      }
      if (control == cmHidetaskbar)
      {
        settingsChanged = true;
      }
      if (control == cmAutostart)
      {
        settingsChanged = true;
      }
      if (control == cmMinimizeonstartup)
      {
        settingsChanged = true;
      }
      if (control == cmMinimizeonexit)
      {
        settingsChanged = true;
      }
      if (control == cmTurnoffmonitor)
      {
        settingsChanged = true;
      }
      if (control == cmTurnmonitoronafterresume)
      {
        settingsChanged = true;
      }
      if (control == cmEnables3trick)
      {
        settingsChanged = true;
      }
      if (control == cmUseS3Hack)
      {
        settingsChanged = true;
      }
      if (control == cmRestartonresume)
      {
        settingsChanged = true;
      }
      if (control == cmShowlastactivemodule)
      {
        settingsChanged = true;
      }
      if (control == cmUsescreenselector)
      {
        settingsChanged = true;

        if (cmUsescreenselector.Selected)
        {
          btnShowScreens.Visible = true;
        }
        else
        {
          btnShowScreens.Visible = false;
        }
      }
      // Delay at startup
      if (control == btnDelayStartup)
      {
        OnStartUpDelay();
      }
      if (control == cmDelayStartup)
      {
        settingsChanged = true;
      }
      if (control == cmDelayResume)
      {
        settingsChanged = true;
      }
      if (control == btnShowScreens)
      {
        OnShowScreens();
      }
      
      base.OnClicked(controlId, control, actionType);
    }

    #region Serialisation

    private void LoadSettings()
    {
      using (Profile.Settings xmlreader = new Profile.MPSettings())
      {
        loglevel = xmlreader.GetValueAsString("general", "loglevel", "2"); // set loglevel to 2:info 3:debug
        priority = xmlreader.GetValueAsString("general", "ThreadPriority", "Normal");
        cmWatchdog.Selected = xmlreader.GetValueAsBool("general", "watchdogEnabled", false);
        cmAutoRestart.Selected = xmlreader.GetValueAsBool("general", "restartOnError", true);
        if (!cmAutoRestart.Selected)
        {
          GUIControl.HideControl(GetID, (int)Controls.CONTROL_DELAYINSEC);
        }
        else
        {
          GUIControl.ShowControl(GetID, (int)Controls.CONTROL_DELAYINSEC);
        }
        iDelay = xmlreader.GetValueAsInt("general", "restart delay", 10);

        // startup/Resume settings
        cmStartfullscreen.Selected = xmlreader.GetValueAsBool("general", "startfullscreen", true);
        cmUsefullscreensplash.Selected = xmlreader.GetValueAsBool("general", "usefullscreensplash", true);
        cmAlwaysontop.Selected = xmlreader.GetValueAsBool("general", "alwaysontop", false);
        cmHidetaskbar.Selected = xmlreader.GetValueAsBool("general", "hidetaskbar", false);
        hideTaskBar = cmHidetaskbar.Selected;
        cmAutostart.Selected = xmlreader.GetValueAsBool("general", "autostart", false);
        cmMinimizeonstartup.Selected = xmlreader.GetValueAsBool("general", "minimizeonstartup", false);
        cmMinimizeonexit.Selected = xmlreader.GetValueAsBool("general", "minimizeonexit", false);
        cmTurnoffmonitor.Selected = xmlreader.GetValueAsBool("general", "turnoffmonitor", false);
        cmTurnmonitoronafterresume.Selected = xmlreader.GetValueAsBool("general", "turnmonitoronafterresume", false);
        cmEnables3trick.Selected = xmlreader.GetValueAsBool("general", "enables3trick", true);
        cmUseS3Hack.Selected = xmlreader.GetValueAsBool("general", "useS3Hack", false);
        cmRestartonresume.Selected = xmlreader.GetValueAsBool("general", "restartonresume", false);
        cmShowlastactivemodule.Selected = xmlreader.GetValueAsBool("general", "showlastactivemodule", false);
        cmUsescreenselector.Selected = xmlreader.GetValueAsBool("screenselector", "usescreenselector", false);

        screennumber = xmlreader.GetValueAsInt("screenselector", "screennumber", 0);

        // Delay startup
        iStartUpDelay = xmlreader.GetValueAsInt("general", "delay", 0);
        string property = iStartUpDelay + " sec";
        GUIPropertyManager.SetProperty("#delayStartup", property);

        if (iStartUpDelay == 0)
        {
          cmDelayStartup.Visible = false;
          cmDelayResume.Visible = false;
        }
        else
        {
          cmDelayStartup.Visible = true;
          cmDelayResume.Visible = true;
        }
        cmDelayStartup.Selected = xmlreader.GetValueAsBool("general", "delay startup", false);
        cmDelayResume.Selected = xmlreader.GetValueAsBool("general", "delay resume", false);

        GetScreens();
        GUIPropertyManager.SetProperty("#defScreen", screenCollection[screennumber].ToString());

        if (cmUsescreenselector.Selected)
        {
          btnShowScreens.Visible = true;
        }
        else
        {
          btnShowScreens.Visible = false;
        }

      }
    }

    private void SaveSettings()
    {
      using (Profile.Settings xmlwriter = new Profile.MPSettings())
      {
        xmlwriter.SetValueAsBool("general", "startfullscreen", cmStartfullscreen.Selected);
        xmlwriter.SetValueAsBool("general", "usefullscreensplash", cmUsefullscreensplash.Selected);
        xmlwriter.SetValueAsBool("general", "alwaysontop", cmAlwaysontop.Selected);
        try
        {
          if (cmAlwaysontop.Selected) // always on top
          {
            using (RegistryKey subkey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
            {
              subkey.SetValue("ForegroundLockTimeout", 0);
            }
          }
        }
        catch (Exception) { }
        xmlwriter.SetValueAsBool("general", "hidetaskbar", cmHidetaskbar.Selected);
        xmlwriter.SetValueAsBool("general", "autostart", cmAutostart.Selected);
        try
        {
          if (cmAutostart.Selected) // autostart on boot
          {
            string fileName = Config.GetFile(Config.Dir.Base, "MediaPortal.exe");
            using (
              RegistryKey subkey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run",
                                                                   true)
              )
            {
              subkey.SetValue("MediaPortal", fileName);
            }
          }
          else
          {
            using (
              RegistryKey subkey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run",
                                                                   true)
              )
            {
              subkey.DeleteValue("MediaPortal", false);
            }
          }
        }
        catch (Exception) { }
        xmlwriter.SetValueAsBool("general", "minimizeonexit", cmMinimizeonexit.Selected);
        xmlwriter.SetValueAsBool("general", "turnoffmonitor", cmTurnoffmonitor.Selected);
        xmlwriter.SetValueAsBool("general", "turnmonitoronafterresume", cmTurnmonitoronafterresume.Selected);
        xmlwriter.SetValueAsBool("general", "enables3trick", cmEnables3trick.Selected);
        xmlwriter.SetValueAsBool("general", "useS3Hack", cmUseS3Hack.Selected);
        xmlwriter.SetValueAsBool("general", "restartonresume", cmRestartonresume.Selected);
        xmlwriter.SetValueAsBool("general", "showlastactivemodule", cmShowlastactivemodule.Selected);
        xmlwriter.SetValueAsBool("screenselector", "usescreenselector", cmUsescreenselector.Selected);
        xmlwriter.SetValueAsBool("general", "minimizeonstartup", cmMinimizeonstartup.Selected);
        xmlwriter.SetValue("general", "restart delay", iDelay.ToString());
        xmlwriter.SetValueAsBool("general", "watchdogEnabled", cmWatchdog.Selected);
        xmlwriter.SetValueAsBool("general", "restartOnError", cmAutoRestart.Selected);
        xmlwriter.SetValueAsBool("general", "delay startup", cmDelayStartup.Selected);
        xmlwriter.SetValueAsBool("general", "delay resume", cmDelayResume.Selected);
      }
    }

    #endregion

    private int GetPriority(string priority)
    {
      switch (priority)
      {
        case "High":
          return (int)Priority.High;

        case "AboveNormal":
          return (int)Priority.AboveNormal;;

        case "Normal":
          return (int)Priority.Normal;;

        case "BelowNormal":
          return (int)Priority.BelowNormal;;

        default:
          return (int)Priority.Normal;;
      }
    }

    private void OnLog()
    {
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      if (dlg == null)
      {
        return;
      }
      dlg.Reset();
      dlg.SetHeading(496); // Options

      dlg.Add("Error");
      dlg.Add("Warning");
      dlg.Add("Information");
      dlg.Add("Debug");

      dlg.SelectedLabel = Convert.ToInt16(loglevel);
      
      // Show dialog menu
      dlg.DoModal(GetID);

      if (dlg.SelectedLabel == -1)
      {
        return;
      }

      using (Profile.Settings xmlwriter = new Profile.MPSettings())
      {
        xmlwriter.SetValue("general", "loglevel", dlg.SelectedLabel);
        settingsChanged = true;
      }
      loglevel = dlg.SelectedLabel.ToString();
    }

    private void OnProcess()
    {
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      if (dlg == null)
      {
        return;
      }
      dlg.Reset();
      dlg.SetHeading(496); // Options

      dlg.Add("High");
      dlg.Add("AboveNormal");
      dlg.Add("Normal");
      dlg.Add("BelowNormal");

      dlg.SelectedLabel = GetPriority(priority);
      
      // Show dialog menu
      dlg.DoModal(GetID);

      if (dlg.SelectedLabel == -1)
      {
        return;
      }

      using (Profile.Settings xmlwriter = new Profile.MPSettings())
      {
        xmlwriter.SetValue("general", "ThreadPriority", dlg.SelectedLabelText);
        settingsChanged = true;
      }

      priority = dlg.SelectedLabelText;
    }

    private void OnStartUpDelay()
    {
      string seconds = iStartUpDelay.ToString();
      GetNumberFromKeyboard(ref seconds);
      iStartUpDelay = Convert.ToInt32(seconds);

      string property = iStartUpDelay + " sec";
      GUIPropertyManager.SetProperty("#delayStartup", property);

      if (iStartUpDelay == 0)
      {
        cmDelayStartup.Visible = false;
        cmDelayResume.Visible = false;
      }
      else
      {
        cmDelayStartup.Visible = true;
        cmDelayResume.Visible = true;
      }

      using (Profile.Settings xmlwriter = new Profile.MPSettings())
      {
        xmlwriter.SetValue("general", "delay", iStartUpDelay);
        settingsChanged = true;
      }
    }

    private void OnShowScreens()
    {
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      if (dlg == null)
      {
        return;
      }
      dlg.Reset();
      dlg.SetHeading(496); // Options

      foreach (string screen in screenCollection)
      {
        dlg.Add(screen);
      }

      if (screennumber < screenCollection.Count)
      {
        dlg.SelectedLabel = screennumber;
      }
      
      // Show dialog menu
      dlg.DoModal(GetID);

      if (dlg.SelectedLabel == -1)
      {
        return;
      }

      using (Profile.Settings xmlwriter = new Profile.MPSettings())
      {
        xmlwriter.SetValue("screenselector", "screennumber", dlg.SelectedLabel);
        settingsChanged = true;
      }

      priority = dlg.SelectedLabelText;
    }

    private void GetNumberFromKeyboard(ref string strLine)
    {
      VirtualKeyboard keyboard = (VirtualKeyboard)GUIWindowManager.GetWindow((int)Window.WINDOW_VIRTUAL_KEYBOARD);
      if (null == keyboard)
      {
        return;
      }
      keyboard.Reset();
      keyboard.Text = strLine;
      
      keyboard.DoModal(GUIWindowManager.ActiveWindow);
      //strLine = string.Empty;
      if (keyboard.IsConfirmed)
      {
        int number;
        if (Int32.TryParse(keyboard.Text, out number))
        {
          iStartUpDelay = number;
          strLine = keyboard.Text;
        }
      }
    }

    public void GetScreens()
    {
      screenCollection.Clear();
      foreach (Screen screen in Screen.AllScreens)
      {
        int dwf = 0;
        DISPLAY_DEVICE info = new DISPLAY_DEVICE();
        string monitorname = null;
        info.cb = Marshal.SizeOf(info);
        if (EnumDisplayDevices(screen.DeviceName, 0, info, dwf))
        {
          monitorname = info.DeviceString;
        }
        if (monitorname == null)
        {
          monitorname = "";
        }

        foreach (AdapterInformation adapter in Manager.Adapters)
        {
          if (screen.DeviceName.StartsWith(adapter.Information.DeviceName.Trim()))
          {
            screenCollection.Add(string.Format("{0} ({1}x{2}) on {3}",
                                             monitorname, screen.Bounds.Width, screen.Bounds.Height,
                                             adapter.Information.Description));
          }
        }
      }
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

      dlgYesNo.SetLine(1, GUILocalizeStrings.Get(1470));
      dlgYesNo.SetLine(2, GUILocalizeStrings.Get(1471));
      dlgYesNo.DoModal(GetID);

      if (!dlgYesNo.IsConfirmed)
      {
        return;
      }
      else
      {
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

      dbPath = MediaPortal.Picture.Database.PictureDatabase.DatabaseName;
      isRemotePath = (!string.IsNullOrEmpty(dbPath) && PathIsNetworkPath(dbPath));
      if (isRemotePath)
      {
        Log.Info("Settings: disposing PictureDatabase sqllite database.");
        MediaPortal.Picture.Database.PictureDatabase.Dispose();
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
    
  }
}