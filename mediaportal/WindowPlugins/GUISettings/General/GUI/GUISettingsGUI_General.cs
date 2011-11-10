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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MediaPortal.Configuration;
using MediaPortal.Database;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using MediaPortal.Profile;
using MediaPortal.Util;
using Action = MediaPortal.GUI.Library.Action;

namespace WindowPlugins.GUISettings
{
  /// <summary>
  /// Summary description for GUISettingsGeneral.
  /// </summary>
  public class GUISettingsGeneral : GUIInternalWindow
  {
    [SkinControl(10)] protected GUIButtonControl btnSkin = null;
    [SkinControl(11)] protected GUIButtonControl btnLanguage = null;
    //[SkinControl(12)] protected GUIToggleButtonControl btnFullscreen = null;
    [SkinControl(13)] protected GUIButtonControl btnScreenSaver = null;
    [SkinControl(14)] protected GUICheckButton btnLanguagePrefix = null;
    [SkinControl(15)] protected GUIButtonControl btnThumbnails = null;
    [SkinControl(16)] protected GUICheckButton btnFileMenu = null;
    [SkinControl(17)] protected GUIButtonControl btnOnScreenDisplay = null;
    [SkinControl(18)] protected GUIButtonControl btnPin= null;
    [SkinControl(20)] protected GUIImage imgSkinPreview = null;

    [SkinControl(30)] protected GUICheckButton cmAllowRememberLastFocusedItem = null;
    [SkinControl(31)] protected GUICheckButton cmAutosize = null;
    [SkinControl(32)] protected GUICheckButton cmHideextensions = null;
    [SkinControl(33)] protected GUICheckButton cmFileexistscache = null;
    [SkinControl(34)] protected GUICheckButton cmEnableguisounds = null;
    [SkinControl(35)] protected GUICheckButton cmMousesupport = null;
    
    [SkinControl(40)] protected GUIButtonControl btnHomeUsage= null;

    // IMPORTANT: the enumeration depends on the correct order of items in homeComboBox.
    // The order is chosen to allow compositing SelectedIndex from bitmapped flags.
    [Flags]
    private enum HomeUsageEnum
    {
      PreferClassic = 0,
      PreferBasic = 1,
      UseBoth = 0,
      UseOnlyOne = 2,
    }
    
    private string selectedLangName;
    private string selectedSkinName;
    private bool selectedFullScreen;
    private bool selectedScreenSaver;
    private ArrayList homeUsage = new ArrayList();
    private static bool settingsChanged = false;
    private int homeSelectedIndex = 0;
    private bool hideTaskBar = false;
    private string pin = string.Empty;

    [DllImport("shlwapi.dll")]
    private static extern bool PathIsNetworkPath(string Path);

    private class CultureComparer : IComparer
    {
      #region IComparer Members

      public int Compare(object x, object y)
      {
        CultureInfo info1 = (CultureInfo)x;
        CultureInfo info2 = (CultureInfo)y;
        return String.Compare(info1.EnglishName, info2.EnglishName, true);
      }

      #endregion
    }

    public GUISettingsGeneral()
    {
      GetID = (int)Window.WINDOW_SETTINGS_SKIN;
    }

    public override bool Init()
    {
      //SkinDirectory = GUIGraphicsContext.Skin.Remove(GUIGraphicsContext.Skin.LastIndexOf(@"\")); 
      return Load(GUIGraphicsContext.Skin + @"\settings_general.xml");
    }

    public static bool SettingsChanged
    {
      get { return settingsChanged; }
      set { settingsChanged = value; }
    }

    protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
    {
      if (control == btnSkin)
      {
        GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
        if (dlg == null)
        {
          return;
        }
        dlg.Reset();
        dlg.SetHeading(166); // menu

        List<string> installedSkins = new List<string>();
        installedSkins = GetInstalledSkins();

        foreach (string skin in installedSkins)
        {
          dlg.Add(skin);
        }
        dlg.SelectedLabel = btnSkin.SelectedItem;
        dlg.DoModal(GetID);
        if (dlg.SelectedId == -1)
        {
          return;
        }
        if (String.Compare(dlg.SelectedLabelText, btnSkin.Label, true) != 0)
        {
          btnSkin.Label = dlg.SelectedLabelText;
          OnSkinChanged();
        }
        return;
      }
      if (control == btnLanguage)
      {
        GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
        if (dlg == null)
        {
          return;
        }
        dlg.Reset();
        dlg.SetHeading(248); // menu
        string[] languages = GUILocalizeStrings.SupportedLanguages();
        foreach (string lang in languages)
        {
          dlg.Add(lang);
        }
        string currentLanguage = btnLanguage.Label;
        dlg.SelectedLabel = 0;
        for (int i = 0; i < languages.Length; i++)
        {
          if (languages[i].ToLower() == currentLanguage.ToLower())
          {
            dlg.SelectedLabel = i;
            break;
          }
        }
        dlg.DoModal(GetID);
        if (dlg.SelectedId == -1)
        {
          return;
        }
        if (String.Compare(dlg.SelectedLabelText, btnLanguage.Label, true) != 0)
        {
          btnLanguage.Label = dlg.SelectedLabelText;
          OnLanguageChanged();
        }
        return;
      }
      using (Settings xmlwriter = new MPSettings())
      {
        if (control == cmAllowRememberLastFocusedItem)
        {
          settingsChanged = true;
        }
        if (control == cmAutosize)
        {
          settingsChanged = true;
        }
        if (control == cmHideextensions)
        {
          settingsChanged = true;
        }
        if (control == cmFileexistscache)
        {
          settingsChanged = true;
        }
        if (control == cmEnableguisounds)
        {
          settingsChanged = true;
        }
        if (control == cmMousesupport)
        {
          settingsChanged = true;
        }
        if (control == btnHomeUsage)
        {
          OnHomeUsage();
        }
        if (control == btnLanguagePrefix)
        {
          settingsChanged = true;
        }
        if (control == btnScreenSaver)
        {
          GUISettingsScreenSaver GUISettingsScreenSaver = (GUISettingsScreenSaver)GUIWindowManager.GetWindow((int)Window.WINDOW_SETTINGS_SCREENSAVER);
          if (GUISettingsScreenSaver == null)
          {
            return;
          }

          GUISettingsScreenSaver.SettingsChanged = settingsChanged;
          GUIWindowManager.ActivateWindow((int)Window.WINDOW_SETTINGS_SCREENSAVER);
        }
        if (control == btnThumbnails)
        {
          GUISettingsThumbnails GUISettingsThumbnails = (GUISettingsThumbnails)GUIWindowManager.GetWindow((int)Window.WINDOW_SETTINGS_THUMBNAILS);
          if (GUISettingsThumbnails == null)
          {
            return;
          }
          GUISettingsThumbnails.SettingsChanged = settingsChanged;
          GUIWindowManager.ActivateWindow((int)Window.WINDOW_SETTINGS_THUMBNAILS);
        }
        if (control == btnFileMenu)
        {
          if (btnFileMenu.Selected)
          {
            btnPin.IsEnabled = true;
          }
          else
          {
            btnPin.IsEnabled = false;
          }
        }
        if (control == btnPin)
        {
          string tmpPin = pin;
          GetStringFromKeyboard(ref tmpPin, 4);

          int number;
          if (Int32.TryParse(tmpPin, out number))
          {
            pin = number.ToString();
          }
        }
        if (control == btnOnScreenDisplay)
        {
          GUISettingsOnScreenDisplay onScreenDisplay = (GUISettingsOnScreenDisplay)GUIWindowManager.GetWindow((int)Window.WINDOW_SETTINGS_ONSCREEN_DISPLAY);

          if (onScreenDisplay == null)
          {
            return;
          }

          GUIWindowManager.ActivateWindow((int)Window.WINDOW_SETTINGS_ONSCREEN_DISPLAY);
        }
        
      }

      base.OnClicked(controlId, control, actionType);
    }

    protected override void OnPageLoad()
    {
      base.OnPageLoad();

      homeUsage.Clear();
      homeUsage.AddRange(new object[]
                                        {
                                          "Classic and Basic, prefer Classic",
                                          "Classic and Basic, prefer Basic",
                                          "only Classic Home",
                                          "only Basic Home"
                                        });

      LoadSettings();
    }
   
    protected override void OnPageDestroy(int newWindowId)
    {
      SaveSettings();

      if (settingsChanged && newWindowId != (int)Window.WINDOW_SETTINGS_SCREENSAVER && 
                             newWindowId != (int)Window.WINDOW_SETTINGS_THUMBNAILS)
      {
        OnRestartMP();
      }
      settingsChanged = false;
      base.OnPageDestroy(newWindowId);
    }

    #region Serialisation

    private void LoadSettings()
    {
      using (Settings xmlreader = new MPSettings())
      {
        btnScreenSaver.Selected = xmlreader.GetValueAsBool("general", "IdleTimer", false);
        string currentLanguage = string.Empty;
        currentLanguage = xmlreader.GetValueAsString("gui", "language", "English");
        btnLanguage.Label = currentLanguage;
        btnLanguagePrefix.Selected = xmlreader.GetValueAsBool("gui", "myprefix", false);
        
        SetSkins();

        // GUI settings
        cmAllowRememberLastFocusedItem.Selected = xmlreader.GetValueAsBool("gui", "allowRememberLastFocusedItem", true);
        cmAutosize.Selected = xmlreader.GetValueAsBool("gui", "autosize", true);
        cmHideextensions.Selected = xmlreader.GetValueAsBool("gui", "hideextensions", true);
        cmFileexistscache.Selected = xmlreader.GetValueAsBool("gui", "fileexistscache", false);
        cmEnableguisounds.Selected = xmlreader.GetValueAsBool("gui", "enableguisounds", true);
        cmMousesupport.Selected = xmlreader.GetValueAsBool("gui", "mousesupport", false);
        
        bool startWithBasicHome = xmlreader.GetValueAsBool("gui", "startbasichome", false);
        bool useOnlyOneHome = xmlreader.GetValueAsBool("gui", "useonlyonehome", false);
        homeSelectedIndex = (int)((useOnlyOneHome ? HomeUsageEnum.UseOnlyOne : HomeUsageEnum.UseBoth) |
                                           (startWithBasicHome ? HomeUsageEnum.PreferBasic : HomeUsageEnum.PreferClassic));

        GUIPropertyManager.SetProperty("#homeScreen", homeUsage[homeSelectedIndex].ToString());
        btnHomeUsage.Label = homeUsage[homeSelectedIndex].ToString();
        hideTaskBar = xmlreader.GetValueAsBool("general", "hidetaskbar", false);

        btnFileMenu.Selected = xmlreader.GetValueAsBool("filemenu", "enabled", true);
        btnPin.IsEnabled = btnFileMenu.Selected;
        pin = Utils.DecryptPin(xmlreader.GetValueAsString("filemenu", "pincode", ""));
      }
    }

    private void SaveSettings()
    {
      using (Settings xmlwriter = new MPSettings())
      {
        //xmlwriter.SetValueAsBool("general", "startfullscreen", btnFullscreen.Selected);
        xmlwriter.SetValueAsBool("general", "IdleTimer", btnScreenSaver.Selected);
        xmlwriter.SetValue("gui", "language", btnLanguage.Label);
        xmlwriter.SetValue("skin", "name", btnSkin.Label);

        xmlwriter.SetValueAsBool("gui", "allowRememberLastFocusedItem", cmAllowRememberLastFocusedItem.Selected);
        xmlwriter.SetValueAsBool("gui", "autosize", cmAutosize.Selected);
        xmlwriter.SetValueAsBool("gui", "hideextensions", cmHideextensions.Selected);
        xmlwriter.SetValueAsBool("gui", "fileexistscache", cmFileexistscache.Selected);
        xmlwriter.SetValueAsBool("gui", "enableguisounds", cmEnableguisounds.Selected);
        xmlwriter.SetValueAsBool("gui", "mousesupport", cmMousesupport.Selected);
        xmlwriter.SetValueAsBool("gui", "myprefix", btnLanguagePrefix.Selected);
        
        xmlwriter.SetValueAsBool("filemenu", "enabled", btnFileMenu.Selected);
        xmlwriter.SetValue("filemenu", "pincode", Utils.EncryptPin(pin));
      }
      Config.SkinName = btnSkin.Label;
    }

    #endregion
    
    private void SetSkins()
    {
      List<string> installedSkins = new List<string>();
      string currentSkin = "";
      using (Settings xmlreader = new MPSettings())
      {
        currentSkin = xmlreader.GetValueAsString("skin", "name", "DefaultWide");
      }
      installedSkins = GetInstalledSkins();

      foreach (string skin in installedSkins)
      {
        if (String.Compare(skin, currentSkin, true) == 0)
        {
          btnSkin.Label = skin;
          imgSkinPreview.SetFileName(Config.GetFile(Config.Dir.Skin, skin, @"media\preview.png"));
        }
      }
    }

    private List<string> GetInstalledSkins()
    {
      List<string> installedSkins = new List<string>();

      try
      {
        DirectoryInfo skinFolder = new DirectoryInfo(Config.GetFolder(Config.Dir.Skin));
        if (skinFolder.Exists)
        {
          DirectoryInfo[] skinDirList = skinFolder.GetDirectories();
          foreach (DirectoryInfo skinDir in skinDirList)
          {
            FileInfo refFile = new FileInfo(Config.GetFile(Config.Dir.Skin, skinDir.Name, "references.xml"));
            if (refFile.Exists)
            {
              installedSkins.Add(skinDir.Name);
            }
          }
        }
      }
      catch (Exception ex)
      {
        Log.Error("GUISettingsGeneral: Error getting installed skins - {0}", ex.Message);
      }
      return installedSkins;
    }

    private void BackupButtons()
    {
      selectedSkinName = btnSkin.Label;
      selectedLangName = btnLanguage.Label;
      //selectedFullScreen = btnFullscreen.Selected;
      selectedScreenSaver = btnScreenSaver.Selected;
    }

    private void RestoreButtons()
    {
      btnSkin.Label = selectedSkinName;
      btnLanguage.Label = selectedLangName;
      
      if (selectedScreenSaver)
      {
        GUIControl.SelectControl(GetID, btnScreenSaver.GetID);
      }
    }

    private void OnSkinChanged()
    {
      // Backup the buttons, needed later
      BackupButtons();

      // Set the skin to the selected skin and reload GUI
      GUIGraphicsContext.Skin = btnSkin.Label;
      SaveSettings();
      GUITextureManager.Clear();
      GUITextureManager.Init();
      GUIFontManager.LoadFonts(GUIGraphicsContext.Skin + @"\fonts.xml");
      GUIFontManager.InitializeDeviceObjects();
      GUIExpressionManager.ClearExpressionCache();
      GUIControlFactory.ClearReferences();
      GUIControlFactory.LoadReferences(GUIGraphicsContext.Skin + @"\references.xml");
      GUIWindowManager.OnResize();
      GUIWindowManager.ActivateWindow(GetID);
      GUIControl.FocusControl(GetID, btnSkin.GetID);

      // Apply the selected buttons again, since they are cleared when we reload
      RestoreButtons();
      using (Settings xmlreader = new MPSettings())
      {
        xmlreader.SetValue("general", "skinobsoletecount", 0);
        bool autosize = xmlreader.GetValueAsBool("gui", "autosize", true);
        if (autosize && !GUIGraphicsContext.Fullscreen)
        {
          try
          {
            GUIGraphicsContext.form.ClientSize = new Size(GUIGraphicsContext.SkinSize.Width, GUIGraphicsContext.SkinSize.Height);
            //Form.ActiveForm.ClientSize = new Size(GUIGraphicsContext.SkinSize.Width, GUIGraphicsContext.SkinSize.Height);
          }
          catch (Exception ex)
          {
            Log.Error("OnSkinChanged exception:{0}", ex.ToString());
            Log.Error(ex);
          }
        }
      }
      if (BassMusicPlayer.Player != null && BassMusicPlayer.Player.VisualizationWindow != null)
      {
        BassMusicPlayer.Player.VisualizationWindow.Reinit();
      }
    }

    private void OnLanguageChanged()
    {
      // Backup the buttons, needed later
      BackupButtons();
      SaveSettings();
      GUILocalizeStrings.ChangeLanguage(btnLanguage.Label);
      GUIFontManager.LoadFonts(GUIGraphicsContext.Skin + @"\fonts.xml");
      GUIFontManager.InitializeDeviceObjects();
      GUIWindowManager.OnResize();
      GUIWindowManager.ActivateWindow(GetID); // without this you cannot change skins / lang any more..
      GUIControl.FocusControl(GetID, btnLanguage.GetID);
      // Apply the selected buttons again, since they are cleared when we reload
      RestoreButtons();
    }

    private void OnHomeUsage()
    {
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      if (dlg == null)
      {
        return;
      }
      dlg.Reset();
      dlg.SetHeading(496); // options
      
      foreach (string home in homeUsage)
      {
        dlg.Add(home);
      }

      dlg.SelectedLabel = homeSelectedIndex;
      
      dlg.DoModal(GetID);
      if (dlg.SelectedLabel == -1)
      {
        return;
      }

      homeSelectedIndex = dlg.SelectedLabel;
      btnHomeUsage.Label = dlg.SelectedLabelText;

      using (Settings xmlwriter = new MPSettings())
      {
        xmlwriter.SetValueAsBool("gui", "useonlyonehome",
                                 (dlg.SelectedLabel & (int)HomeUsageEnum.UseOnlyOne) != 0);
        xmlwriter.SetValueAsBool("gui", "startbasichome",
                                 (dlg.SelectedLabel & (int)HomeUsageEnum.PreferBasic) != 0);
      }
      GUIPropertyManager.SetProperty("#homeScreen", homeUsage[homeSelectedIndex].ToString());

      settingsChanged = true;
    }

    private void GetStringFromKeyboard(ref string strLine, int maxLenght)
    {
      VirtualKeyboard keyboard = (VirtualKeyboard)GUIWindowManager.GetWindow((int)Window.WINDOW_VIRTUAL_KEYBOARD);
      if (null == keyboard)
      {
        return;
      }
      keyboard.Reset();
      keyboard.Text = strLine;

      if (maxLenght > 0)
      {
        keyboard.SetMaxLength(maxLenght);
      }

      keyboard.DoModal(GUIWindowManager.ActiveWindow);
      //strLine = string.Empty;
      if (keyboard.IsConfirmed)
      {
        strLine = keyboard.Text;
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
        if (hideTaskBar)
        {
          // only re-show the startbar if MP is the one that has hidden it.
          Win32API.EnableStartBar(true);
          Win32API.ShowStartBar(true);
        }
        Log.Info("Settings: OnRestart - prepare for restart!");
        File.Delete(Config.GetFile(Config.Dir.Config, "mediaportal.running"));
        Log.Info("Settings: OnRestart - saving settings...");
        Settings.SaveCache();
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