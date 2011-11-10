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
using System.Globalization;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.Profile;
using MediaPortal.Util;
using Action = MediaPortal.GUI.Library.Action;

namespace WindowPlugins.GUISettings
{
  /// <summary>
  /// Summary description for GUISettingsGeneral.
  /// </summary>
  public class GUISettingsOnScreenDisplay: GUIInternalWindow
  {
    [SkinControl(2)] protected GUIButtonControl btnDisplayTimeout = null;
    [SkinControl(3)] protected GUIButtonControl btnZapDelay = null;
    [SkinControl(4)] protected GUIButtonControl btnZapTimeOut = null;

    private bool settingsSaved = false;
    private int displayTimeout = 0;
    private int zapDelay= 2;
    private int zapTimeout = 5;
    
    
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

    public GUISettingsOnScreenDisplay()
    {
      GetID = (int)Window.WINDOW_SETTINGS_ONSCREEN_DISPLAY;
    }

    public override bool Init()
    {
      return Load(GUIGraphicsContext.Skin + @"\settings_onscreendisplay.xml");
    }
    
    protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
    {
      string getNumber;
      int number;
      if (control == btnDisplayTimeout)
      {
        getNumber = displayTimeout.ToString();
        GetStringFromKeyboard(ref getNumber, 2);
        
        if (Int32.TryParse(getNumber, out number))
        {
          displayTimeout = number;
        }
        SetProperties();
      }
      if (control == btnZapDelay)
      {
        getNumber = zapDelay.ToString();
        GetStringFromKeyboard(ref getNumber, 2);
        
        if (Int32.TryParse(getNumber, out number))
        {
          zapDelay = number;
        }
        SetProperties();
      }
      if (control == btnZapTimeOut)
      {
        getNumber = zapTimeout.ToString();
        GetStringFromKeyboard(ref getNumber, 2);

        if (Int32.TryParse(getNumber, out number))
        {
          zapTimeout = number;
        }
        SetProperties();
      }

      base.OnClicked(controlId, control, actionType);
    }

    protected override void OnPageLoad()
    {
      base.OnPageLoad();
      GUIPropertyManager.SetProperty("#currentmodule", GUILocalizeStrings.Get(1323));
      LoadSettings();
      SetProperties();
    }

    protected override void OnPageDestroy(int newWindowId)
    {
      SaveSettings();

      base.OnPageDestroy(newWindowId);
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

    private void SetProperties()
    {
      GUIPropertyManager.SetProperty("#displayTimeout", displayTimeout + " " + GUILocalizeStrings.Get(2999));
      GUIPropertyManager.SetProperty("#zapDelay", zapDelay + " " + GUILocalizeStrings.Get(2999));
      GUIPropertyManager.SetProperty("#zapTimeout", zapTimeout + " " + GUILocalizeStrings.Get(2999));
    }
    
    #region Serialisation

    private void LoadSettings()
    {
      using (Settings xmlreader = new MPSettings())
      {
        displayTimeout = xmlreader.GetValueAsInt("movieplayer", "osdtimeout", 0);
        zapDelay = xmlreader.GetValueAsInt("movieplayer", "zapdelay", 2);
        zapTimeout = xmlreader.GetValueAsInt("movieplayer", "zaptimeout", 5);
      }
    }

    private void SaveSettings()
    {
      using (Settings xmlwriter = new MPSettings())
      {
        xmlwriter.SetValue("movieplayer", "osdtimeout", displayTimeout);
        xmlwriter.SetValue("movieplayer", "zapdelay", zapDelay);
        xmlwriter.SetValue("movieplayer", "zaptimeout", zapTimeout);
      }
    }

    #endregion

  }
}