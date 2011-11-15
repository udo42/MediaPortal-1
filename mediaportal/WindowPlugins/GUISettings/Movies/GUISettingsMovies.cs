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
using DirectShowLib;
using DShowNET;
using DShowNET.Helper;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.GUI.Settings;
using MediaPortal.Profile;
using MediaPortal.Util;
using Action = MediaPortal.GUI.Library.Action;

namespace WindowPlugins.GUISettings.TV
{
  /// <summary>
  /// Summary description for GUISettingsMovies.
  /// </summary>
  public class GUISettingsMovies : GUIInternalWindow
  {
    [SkinControl(24)] protected GUIButtonControl btnVideo = null;
    [SkinControl(25)] protected GUIButtonControl btnAudio = null;
    [SkinControl(31)] protected GUICheckButton btnEnableSubtitles = null;
    [SkinControl(34)] protected GUIButtonControl btnPlayall = null;
    [SkinControl(35)] protected GUIButtonControl btnExtensions = null;
    [SkinControl(40)] protected GUIButtonControl btnFolders = null;
    [SkinControl(41)] protected GUIButtonControl btnDatabase= null;
    [SkinControl(42)] protected GUIButtonControl btnPlaylist= null;
    

    private bool _subtitleSettings;
    private int _selectedOption;
    private string _section = "movies";
    private int _playAll = 3;

    private string _strVideoCodec;
    private string _strH264VideoCodec;
    private string _strAudioCodec;
    private Geometry.Type _aspectRatio;
    private string _strAudioRenderer;
    private string _strAACAudioCodec;
    private string _defaultSubtitleLanguage;
    private CultureInfo _info;
    private CultureInfo _infoAudio;
    private string _defaultAudioLanguage;
    

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
    
    public GUISettingsMovies()
    {
      GetID = (int)Window.WINDOW_SETTINGS_MOVIES;
    }

    #region Serialization

    private void LoadSettings()
    {
      using (Settings xmlreader = new MPSettings())
      {
        _strVideoCodec = xmlreader.GetValueAsString("movieplayer", "mpeg2videocodec", "");
        _strH264VideoCodec = xmlreader.GetValueAsString("movieplayer", "h264videocodec", "");
        _strAudioCodec = xmlreader.GetValueAsString("movieplayer", "mpeg2audiocodec", "");
        string aspectRatioText = xmlreader.GetValueAsString("movieplayer", "defaultar", "Normal");
        _aspectRatio = Utils.GetAspectRatio(aspectRatioText);
        _strAudioRenderer = xmlreader.GetValueAsString("movieplayer", "audiorenderer", "Default DirectSound Device");
        _strAACAudioCodec = xmlreader.GetValueAsString("movieplayer", "aacaudiocodec", "");
        _defaultSubtitleLanguage = xmlreader.GetValueAsString("subtitles", "language", "EN");
        _defaultAudioLanguage = xmlreader.GetValueAsString("movieplayer", "audiolanguage", "EN");

        _subtitleSettings = xmlreader.GetValueAsBool("subtitles", "enabled", false);
        btnEnableSubtitles.Selected = _subtitleSettings;
        _playAll = xmlreader.GetValueAsInt("movies", "playallinfolder", 3);
      }
    }

    private void SaveSettings()
    {
      using (Settings xmlwriter = new MPSettings())
      {
        xmlwriter.SetValue("movieplayer", "mpeg2videocodec", _strVideoCodec);
        xmlwriter.SetValue("movieplayer", "h264videocodec", _strH264VideoCodec);
        xmlwriter.SetValue("movieplayer", "mpeg2audiocodec", _strAudioCodec);
        string aspectRatioText = Utils.GetAspectRatio(_aspectRatio);
        xmlwriter.SetValue("movieplayer", "defaultar", aspectRatioText);
        xmlwriter.SetValue("movieplayer", "audiorenderer", _strAudioRenderer);
        xmlwriter.SetValue("movieplayer", "aacaudiocodec", _strAACAudioCodec);

        if (_info != null)
        {
          xmlwriter.SetValue("subtitles", "language", _info.Name);
        }
        if (_infoAudio != null)
        {
          xmlwriter.SetValue("movieplayer", "audiolanguage", _infoAudio.Name);
        }

        xmlwriter.SetValueAsBool("subtitles", "enabled", btnEnableSubtitles.Selected);
        xmlwriter.SetValue("movies", "playallinfolder", _playAll);
      }
    }

    #endregion

    #region Overrides

    public override bool Init()
    {
      return Load(GUIGraphicsContext.Skin + @"\settings_movies.xml");
    }

    protected override void OnPageLoad()
    {
      base.OnPageLoad();
      LoadSettings();
    }

    protected override void OnPageDestroy(int new_windowId)
    {
      SaveSettings();
      base.OnPageDestroy(new_windowId);
    }

    protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
    {
      if (control == btnVideo)
      {
        _selectedOption = -1;
        OnVideo();
      }
      if (control == btnAudio)
      {
        _selectedOption = -1;
        OnAudio();
      }
      if (control == btnPlayall)
      {
        OnPlayAllVideos();
      }
      if (control == btnExtensions)
      {
        OnExtensions();
      }
      if (control == btnFolders)
      {
        OnFolders();
      }
      if (control == btnDatabase)
      {
        OnDatabase();
      }
      if (control == btnPlaylist)
      {
        OnPlayList();
      }

      base.OnClicked(controlId, control, actionType);
    }

    public override void OnAction(Action action)
    {
      if (action.wID == Action.ActionType.ACTION_HOME || action.wID == Action.ActionType.ACTION_SWITCH_HOME)
      {
        return;
      }

      base.OnAction(action);
    }

    #endregion

    private void OnVideo()
    {
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      if (dlg != null)
      {
        dlg.Reset();
        dlg.SetHeading(GUILocalizeStrings.Get(496)); //Menu

        dlg.AddLocalizedString(6000); // MPEG2
        dlg.AddLocalizedString(6036); // H264
        dlg.AddLocalizedString(6004); // Aspect Ratio
        dlg.AddLocalizedString(1029); // Subtitle

        if (_selectedOption != -1)
          dlg.SelectedLabel = _selectedOption;

        dlg.DoModal(GetID);

        if (dlg.SelectedId == -1)
        {
          return;
        }

        _selectedOption = dlg.SelectedLabel;

        switch (dlg.SelectedId)
        {
          case 6000:
            OnVideoCodec();
            break;
          case 6036:
            OnH264VideoCodec();
            break;
          case 6004:
            OnAspectRatio();
            break;
          case 1029:
            OnSubtitle();
            break;
        }
      }
    }

    private void OnAudio()
    {
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      if (dlg != null)
      {
        dlg.Reset();
        dlg.SetHeading(GUILocalizeStrings.Get(496)); //Menu

        dlg.AddLocalizedString(6001); // Audio codec
        dlg.AddLocalizedString(6039); // AAC codec
        dlg.AddLocalizedString(6002); // Audio render
        dlg.AddLocalizedString(492);  // Audio language

        if (_selectedOption != -1)
          dlg.SelectedLabel = _selectedOption;

        dlg.DoModal(GetID);

        if (dlg.SelectedId == -1)
        {
          return;
        }

        _selectedOption = dlg.SelectedLabel;

        switch (dlg.SelectedId)
        {
          case 6001:
            OnAudioCodec();
            break;
          case 6039:
            OnAACAudioCodec();
            break;
          case 6002:
            OnAudioRenderer();
            break;
          case 492:
            OnAudioLanguage();
            break;
        }
      }
    }

    private void OnVideoCodec()
    {
      //string _strVideoCodec = "";
      //using (Settings xmlreader = new MPSettings())
      //{
      //  _strVideoCodec = xmlreader.GetValueAsString("movieplayer", "mpeg2videocodec", "");
      //}
      ArrayList availableVideoFilters = FilterHelper.GetFilters(MediaType.Video, MediaSubTypeEx.MPEG2);
      while (availableVideoFilters.Contains("CyberLink MPEG Muxer"))
      {
        availableVideoFilters.Remove("CyberLink MPEG Muxer");
      }
      while (availableVideoFilters.Contains("Ulead MPEG Muxer"))
      {
        availableVideoFilters.Remove("Ulead MPEG Muxer");
      }
      while (availableVideoFilters.Contains("PDR MPEG Muxer"))
      {
        availableVideoFilters.Remove("PDR MPEG Muxer");
      }
      while (availableVideoFilters.Contains("Nero Mpeg2 Encoder"))
      {
        availableVideoFilters.Remove("Nero Mpeg2 Encoder");
      }
      availableVideoFilters.Sort();
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      if (dlg != null)
      {
        dlg.Reset();
        dlg.SetHeading(GUILocalizeStrings.Get(496)); //Menu
        int selected = 0;
        int count = 0;
        foreach (string codec in availableVideoFilters)
        {
          dlg.Add(codec); //delete
          if (codec == _strVideoCodec)
          {
            selected = count;
          }
          count++;
        }
        dlg.SelectedLabel = selected;
      }
      dlg.DoModal(GetID);
      
      if (dlg.SelectedLabel < 0)
      {
        OnVideo();
        return;
      }

      _strVideoCodec = (string)availableVideoFilters[dlg.SelectedLabel];
      OnVideo();
    }

    private void OnH264VideoCodec()
    {
      ArrayList availableH264VideoFilters = FilterHelper.GetFilters(MediaType.Video, MediaSubType.H264);
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      if (dlg != null)
      {
        dlg.Reset();
        dlg.SetHeading(GUILocalizeStrings.Get(496)); //Menu
        int selected = 0;
        int count = 0;
        foreach (string codec in availableH264VideoFilters)
        {
          dlg.Add(codec); //delete
          if (codec == _strH264VideoCodec)
          {
            selected = count;
          }
          count++;
        }
        dlg.SelectedLabel = selected;
      }
      
      dlg.DoModal(GetID);
      
      if (dlg.SelectedLabel < 0)
      {
        OnVideo();
        return;
      }
      
      _strH264VideoCodec = (string)availableH264VideoFilters[dlg.SelectedLabel];
      OnVideo();
    }

    private void OnAudioCodec()
    {
      ArrayList availableAudioFilters = FilterHelper.GetFilters(MediaType.Audio, MediaSubType.Mpeg2Audio);
      
      while (availableAudioFilters.Contains("CyberLink MPEG Muxer"))
      {
        availableAudioFilters.Remove("CyberLink MPEG Muxer");
      }
      while (availableAudioFilters.Contains("Ulead MPEG Muxer"))
      {
        availableAudioFilters.Remove("Ulead MPEG Muxer");
      }
      while (availableAudioFilters.Contains("PDR MPEG Muxer"))
      {
        availableAudioFilters.Remove("PDR MPEG Muxer");
      }
      while (availableAudioFilters.Contains("Nero Mpeg2 Encoder"))
      {
        availableAudioFilters.Remove("Nero Mpeg2 Encoder");
      }
      availableAudioFilters.Sort();
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      if (dlg != null)
      {
        dlg.Reset();
        dlg.SetHeading(GUILocalizeStrings.Get(496)); //Menu
        int selected = 0;
        int count = 0;
        foreach (string codec in availableAudioFilters)
        {
          dlg.Add(codec); //delete
          if (codec == _strAudioCodec)
          {
            selected = count;
          }
          count++;
        }
        dlg.SelectedLabel = selected;
      }
      dlg.DoModal(GetID);
      if (dlg.SelectedLabel < 0)
      {
        OnAudio();
        return;
      }

      _strAudioCodec = (string)availableAudioFilters[dlg.SelectedLabel];
      OnAudio();
    }

    private void OnAspectRatio()
    {
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      if (dlg == null)
      {
        return;
      }
      dlg.Reset();
      dlg.SetHeading(941); // Change aspect ratio

      dlg.AddLocalizedString(943); // Normal
      dlg.AddLocalizedString(944); // Original
      dlg.AddLocalizedString(947); // Zoom
      dlg.AddLocalizedString(1190); // Zoom 14:9
      dlg.AddLocalizedString(942); // Stretch
      dlg.AddLocalizedString(945); // Letterbox
      dlg.AddLocalizedString(946); // Non linear stretch

      // set the focus to currently used mode
      dlg.SelectedLabel = (int)_aspectRatio;

      // show dialog and wait for result
      dlg.DoModal(GetID);
      if (dlg.SelectedId == -1)
      {
        OnVideo();
        return;
      }

      _aspectRatio = Utils.GetAspectRatioByLangID(dlg.SelectedId);

      OnVideo();
    }

    private void OnAudioRenderer()
    {
      ArrayList availableAudioFilters = FilterHelper.GetAudioRenderers();
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      if (dlg != null)
      {
        dlg.Reset();
        dlg.SetHeading(GUILocalizeStrings.Get(496)); //Menu
        int selected = 0;
        int count = 0;
        foreach (string codec in availableAudioFilters)
        {
          dlg.Add(codec); //delete
          if (codec == _strAudioRenderer)
          {
            selected = count;
          }
          count++;
        }
        dlg.SelectedLabel = selected;
      }
      dlg.DoModal(GetID);
      if (dlg.SelectedLabel < 0)
      {
        OnAudio();
        return;
      }

      _strAudioRenderer = (string)availableAudioFilters[dlg.SelectedLabel];
      OnAudio();
    }

    private void OnAACAudioCodec()
    {
      ArrayList availableAACAudioFilters = FilterHelper.GetFilters(MediaType.Audio, MediaSubType.AAC);
      availableAACAudioFilters.Sort();
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      
      if (dlg != null)
      {
        dlg.Reset();
        dlg.SetHeading(GUILocalizeStrings.Get(496)); //Menu
        int selected = 0;
        int count = 0;
        
        foreach (string codec in availableAACAudioFilters)
        {
          dlg.Add(codec); //delete
          if (codec == _strAACAudioCodec)
          {
            selected = count;
          }
          count++;
        }
        dlg.SelectedLabel = selected;
      }
      
      dlg.DoModal(GetID);
      
      if (dlg.SelectedLabel < 0)
      {
        OnAudio();
        return;
      }

      _strAACAudioCodec = (string)availableAACAudioFilters[dlg.SelectedLabel];
      OnAudio();
    }

    private void OnSubtitle()
    {
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      
      if (dlg != null)
      {
        dlg.Reset();
        dlg.SetHeading(GUILocalizeStrings.Get(496)); //Menu
        dlg.ShowQuickNumbers = false;
        int selected = 0;
        ArrayList cultures = new ArrayList();
        CultureInfo[] culturesInfos = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
        
        for (int i = 0; i < culturesInfos.Length; ++i)
        {
          cultures.Add(culturesInfos[i]);
        }
        
        cultures.Sort(new CultureComparer());
        
        for (int i = 0; i < cultures.Count; ++i)
        {
          CultureInfo info = (CultureInfo)cultures[i];
          if (info.Name.Equals(_defaultSubtitleLanguage))
          {
            selected = i;
          }
          dlg.Add(info.EnglishName);
        }

        _info = (CultureInfo)cultures[selected];
        dlg.SelectedLabel = selected;
        dlg.DoModal(GetID);
        
        if (dlg.SelectedLabel < 0)
        {
          OnVideo();
          return;
        }

        _defaultSubtitleLanguage = dlg.SelectedLabelText;
        _info = (CultureInfo)cultures[dlg.SelectedLabel];
        OnVideo();
      }
    }

    private void OnAudioLanguage()
    {
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      if (dlg != null)
      {
        dlg.Reset();
        dlg.SetHeading(GUILocalizeStrings.Get(496)); //Menu
        dlg.ShowQuickNumbers = false;
        int selected = 0;
        ArrayList cultures = new ArrayList();
        CultureInfo[] culturesInfos = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
        
        for (int i = 0; i < culturesInfos.Length; ++i)
        {
          cultures.Add(culturesInfos[i]);
        }
        
        cultures.Sort(new CultureComparer());
        
        for (int i = 0; i < cultures.Count; ++i)
        {
          CultureInfo info = (CultureInfo)cultures[i];
          
          if (info.Name.Equals(_defaultAudioLanguage))
          {
            selected = i;
          }
          dlg.Add(info.EnglishName);
        }
        
        _infoAudio = (CultureInfo)cultures[selected];
        dlg.SelectedLabel = selected;
        dlg.DoModal(GetID);
        
        if (dlg.SelectedLabel < 0)
        {
          OnAudio();
          return;
        }

        _defaultAudioLanguage = dlg.SelectedLabelText;
        _infoAudio = (CultureInfo)cultures[dlg.SelectedLabel];
        OnAudio();
      }
    }

    private void OnPlayAllVideos()
    {
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      if (dlg == null)
      {
        return;
      }
      dlg.Reset();
      dlg.SetHeading(1264); // menu

      dlg.AddLocalizedString(1482); // By Name
      dlg.AddLocalizedString(1483); // By Date
      dlg.AddLocalizedString(191); // Shuffle
      dlg.AddLocalizedString(1484); // Always Ask

      dlg.SelectedLabel = _playAll;

      dlg.DoModal(GetID);

      if (dlg.SelectedId == -1)
      {
        return;
      }

      _playAll = dlg.SelectedLabel;
    }

    private void OnExtensions()
    {
      GUISettingsExtensions dlg = (GUISettingsExtensions)GUIWindowManager.GetWindow((int)Window.WINDOW_SETTINGS_EXTENSIONS);
      if (dlg == null)
      {
        return;
      }
      dlg.Section = _section;
      GUIWindowManager.ActivateWindow((int)Window.WINDOW_SETTINGS_EXTENSIONS);
    }
    
    private void OnFolders()
    {
      GUIShareFolders dlg = (GUIShareFolders)GUIWindowManager.GetWindow((int)Window.WINDOW_SETTINGS_FOLDERS);
      if (dlg == null)
      {
        return;
      }
      dlg.Section = _section;
      GUIWindowManager.ActivateWindow((int)Window.WINDOW_SETTINGS_FOLDERS);
    }

    private void OnDatabase()
    {
      GUISettingsMoviesDatabase dlg = (GUISettingsMoviesDatabase)GUIWindowManager.GetWindow((int)Window.WINDOW_SETTINGS_VIDEODATABASE);
      if (dlg == null)
      {
        return;
      }
      GUIWindowManager.ActivateWindow((int)Window.WINDOW_SETTINGS_VIDEODATABASE);
    }

    private void OnPlayList()
    {
      GUISettingsPlaylist dlg = (GUISettingsPlaylist)GUIWindowManager.GetWindow((int)Window.WINDOW_SETTINGS_PLAYLIST);
      if (dlg == null)
      {
        return;
      }
      dlg.Section = _section;
      GUIWindowManager.ActivateWindow((int)Window.WINDOW_SETTINGS_PLAYLIST);
    }
   
  }
}