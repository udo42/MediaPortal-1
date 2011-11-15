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
using System.Drawing;
using System.Globalization;
using System.IO;
using MediaPortal.Configuration;
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
    
    private string _selectedLangName;
    private string _selectedSkinName;
    private bool _selectedScreenSaver;
    private ArrayList _homeUsage = new ArrayList();
    private int _homeSelectedIndex = 0;
    private string _pin = string.Empty;

    
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
      return Load(GUIGraphicsContext.Skin + @"\settings_general.xml");
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
      if (control == cmAllowRememberLastFocusedItem)
      {
        SettingsChanged(true);
      }
      if (control == cmAutosize)
      {
        SettingsChanged(true);
      }
      if (control == cmHideextensions)
      {
        SettingsChanged(true);
      }
      if (control == cmFileexistscache)
      {
        SettingsChanged(true);
      }
      if (control == cmEnableguisounds)
      {
        SettingsChanged(true);
      }
      if (control == cmMousesupport)
      {
        SettingsChanged(true);
      }
      if (control == btnHomeUsage)
      {
        OnHomeUsage();
      }
      if (control == btnLanguagePrefix)
      {
        SettingsChanged(true);
      }
      if (control == btnScreenSaver)
      {
        GUISettingsScreenSaver guiSettingsScreenSaver = (GUISettingsScreenSaver)GUIWindowManager.GetWindow((int)Window.WINDOW_SETTINGS_SCREENSAVER);
        if (guiSettingsScreenSaver == null)
        {
          return;
        }

        GUIWindowManager.ActivateWindow((int)Window.WINDOW_SETTINGS_SCREENSAVER);
      }
      if (control == btnThumbnails)
      {
        GUISettingsThumbnails guiSettingsThumbnails = (GUISettingsThumbnails)GUIWindowManager.GetWindow((int)Window.WINDOW_SETTINGS_THUMBNAILS);
        if (guiSettingsThumbnails == null)
        {
          return;
        }
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
        SettingsChanged(true);
      }
      if (control == btnPin)
      {
        string tmpPin = _pin;
        GetStringFromKeyboard(ref tmpPin, 4);

        int number;
        if (Int32.TryParse(tmpPin, out number))
        {
          _pin = number.ToString();
        }
        SettingsChanged(true);
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
        
      base.OnClicked(controlId, control, actionType);
    }

    protected override void OnPageLoad()
    {
      base.OnPageLoad();

      _homeUsage.Clear();
      _homeUsage.AddRange(new object[]
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
      base.OnPageDestroy(newWindowId);
    }

    public override void OnAction(Action action)
    {
      if (action.wID == Action.ActionType.ACTION_HOME || action.wID == Action.ActionType.ACTION_SWITCH_HOME)
      {
        return;
      }

      base.OnAction(action);
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
        _homeSelectedIndex = (int)((useOnlyOneHome ? HomeUsageEnum.UseOnlyOne : HomeUsageEnum.UseBoth) |
                                           (startWithBasicHome ? HomeUsageEnum.PreferBasic : HomeUsageEnum.PreferClassic));

        GUIPropertyManager.SetProperty("#homeScreen", _homeUsage[_homeSelectedIndex].ToString());
        btnHomeUsage.Label = _homeUsage[_homeSelectedIndex].ToString();
        
        btnFileMenu.Selected = xmlreader.GetValueAsBool("filemenu", "enabled", true);
        btnPin.IsEnabled = btnFileMenu.Selected;
        _pin = Utils.DecryptPin(xmlreader.GetValueAsString("filemenu", "pincode", ""));
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
        xmlwriter.SetValue("filemenu", "pincode", Utils.EncryptPin(_pin));
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
      _selectedSkinName = btnSkin.Label;
      _selectedLangName = btnLanguage.Label;
      _selectedScreenSaver = btnScreenSaver.Selected;
    }

    private void RestoreButtons()
    {
      btnSkin.Label = _selectedSkinName;
      btnLanguage.Label = _selectedLangName;
      
      if (_selectedScreenSaver)
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
      
      foreach (string home in _homeUsage)
      {
        dlg.Add(home);
      }

      dlg.SelectedLabel = _homeSelectedIndex;
      
      dlg.DoModal(GetID);
      if (dlg.SelectedLabel == -1)
      {
        return;
      }

      _homeSelectedIndex = dlg.SelectedLabel;
      btnHomeUsage.Label = dlg.SelectedLabelText;

      using (Settings xmlwriter = new MPSettings())
      {
        xmlwriter.SetValueAsBool("gui", "useonlyonehome",
                                 (dlg.SelectedLabel & (int)HomeUsageEnum.UseOnlyOne) != 0);
        xmlwriter.SetValueAsBool("gui", "startbasichome",
                                 (dlg.SelectedLabel & (int)HomeUsageEnum.PreferBasic) != 0);
      }
      GUIPropertyManager.SetProperty("#homeScreen", _homeUsage[_homeSelectedIndex].ToString());

      SettingsChanged(true);
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
      
      if (keyboard.IsConfirmed)
      {
        strLine = keyboard.Text;
      }
    }

    private void SettingsChanged(bool settingsChanged)
    {
      MediaPortal.GUI.Settings.GUISettings.SettingsChanged = settingsChanged;
    }
  }
}