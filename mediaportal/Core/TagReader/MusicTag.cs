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
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using MediaPortal.GUI.Library;


namespace MediaPortal.TagReader
{
  public class MusicTag
  {
    #region Variables

    internal string m_strArtist = string.Empty;
    internal string m_strArtistSort = string.Empty;
    internal string m_strAlbum = string.Empty;
    internal string m_strAlbumSort = string.Empty;
    internal string m_strGenre = string.Empty;
    internal string m_strTitle = string.Empty;
    internal string m_strTitleSort = string.Empty;
    internal string m_strComment = string.Empty;
    internal int m_iYear = 0;
    internal int m_iDuration = 0;
    internal int m_iTrack = 0;
    internal int m_iNumTrack = 0;
    internal int m_TimesPlayed = 0;
    internal int m_iRating = 0;
    internal byte[] m_CoverArtImageBytes = null;
    internal string m_AlbumArtist = string.Empty;
    internal string m_AlbumArtistSort = string.Empty;
    internal string m_Composer = string.Empty;
    internal string m_ComposerSort = string.Empty;
    internal string m_Conductor = string.Empty;
    internal string m_FileType = string.Empty;
    internal int m_BitRate = 0;
    internal string m_FileName = string.Empty;
    internal string m_Lyrics = string.Empty;
    internal int m_iDiscId = 0;
    internal int m_iNumDisc = 0;
    internal bool m_hasAlbumArtist = false;
    internal DateTime m_dateTimeModified = DateTime.MinValue;
    internal DateTime m_dateTimePlayed = DateTime.MinValue;
    internal string m_Codec = string.Empty;
    internal string m_BitRateMode = string.Empty;
    internal int m_BPM = 0;
    internal int m_Channels = 0;
    internal int m_SampleRate = 0;
    internal string m_Grouping = string.Empty;

    #endregion

    #region ctor

    /// <summary>
    /// empty constructor
    /// </summary>
    public MusicTag() {}

    #endregion

    #region Methods

    /// <summary>
    /// Method to clear the current item
    /// </summary>
    public void Clear()
    {
      m_strArtist = string.Empty;
      m_strArtistSort = string.Empty;
      m_strAlbum = string.Empty;
      m_strAlbumSort = string.Empty;
      m_strGenre = string.Empty;
      m_strTitle = string.Empty;
      m_strTitleSort = string.Empty;
      m_strComment = string.Empty;
      m_FileType = string.Empty;
      m_iYear = 0;
      m_iDuration = 0;
      m_iTrack = 0;
      m_iNumTrack = 0;
      m_TimesPlayed = 0;
      m_iRating = 0;
      m_BitRate = 0;
      m_Composer = string.Empty;
      m_ComposerSort = string.Empty;
      m_Conductor = string.Empty;
      m_AlbumArtist = string.Empty;
      m_AlbumArtistSort = string.Empty;
      m_Lyrics = string.Empty;
      m_iDiscId = 0;
      m_iNumDisc = 0;
      m_hasAlbumArtist = false;
      m_Codec = string.Empty;
      m_BitRateMode = string.Empty;
      m_BPM = 0;
      m_Channels = 0;
      m_SampleRate = 0;
      m_dateTimeModified = DateTime.MinValue;
      m_dateTimePlayed = DateTime.MinValue;
      m_Grouping = string.Empty;
    }

    public bool IsMissingData
    {
      get
      {
        return string.IsNullOrEmpty(Artist)
               || string.IsNullOrEmpty(Album)
               || string.IsNullOrEmpty(Title)
               || string.IsNullOrEmpty(Artist)
               || Track == 0
               || Duration == 0;
      }
    }

    #endregion

    #region Properties

    /// <summary>
    /// Property to get/set the comment field of the music file
    /// </summary>
    public string Comment
    {
      get { return m_strComment; }
      set
      {
        if (value == null) return;
        m_strComment = value.Trim();
      }
    }

    /// <summary>
    /// Property to get/set the Title field of the music file
    /// </summary>
    public string Title
    {
      get { return m_strTitle; }
      set
      {
        if (value == null) return;
        m_strTitle = value.Trim();
      }
    }

    /// <summary>
    /// Property to get/set the Title Sort field of the music file
    /// </summary>
    public string TitleSort
    {
      get { return m_strTitleSort;  }
      set
      {
        if (string.IsNullOrEmpty(value)) return;
        m_strTitleSort = value.Trim();
      }
    }

    /// <summary>
    /// Property to get/set the Artist field of the music file
    /// </summary>
    public string Artist
    {
      get { return m_strArtist; }
      set
      {
        if (value == null) return;
        m_strArtist = value.Trim();
      }
    }

    /// <summary>
    /// Property to get/set the Artist Sort field of the music file
    /// </summary>
    public string ArtistSort
    {
      get { return m_strArtistSort; }
      set
      {
        if (string.IsNullOrEmpty(value)) return;
        m_strArtistSort = value.Trim();
      }
    }


    /// <summary>
    /// Property to get/set the comment Album name of the music file
    /// </summary>
    public string Album
    {
      get { return m_strAlbum; }
      set
      {
        if (value == null) return;
        m_strAlbum = value.Trim();
      }
    }

    /// <summary>
    /// Property to get/set the Album Sort field of the music file
    /// </summary>
    public string AlbumSort
    {
      get { return m_strAlbumSort; }
      set
      {
        if (string.IsNullOrEmpty(value)) return;
        m_strAlbumSort = value.Trim();
      }
    }


    /// <summary>
    /// Property to get/set the Genre field of the music file
    /// </summary>
    public string Genre
    {
      get { return m_strGenre; }
      set
      {
        if (value == null) return;
        m_strGenre = value.Trim();
      }
    }

    /// <summary>
    /// Property to get/set the Year field of the music file
    /// </summary>
    public int Year
    {
      get { return m_iYear; }
      set { m_iYear = value; }
    }

    /// <summary>
    /// Property to get/set the duration in seconds of the music file
    /// </summary>
    public int Duration
    {
      get { return m_iDuration; }
      set { m_iDuration = value; }
    }

    /// <summary>
    /// Property to get/set the Track number field of the music file
    /// </summary>
    public int Track
    {
      get { return m_iTrack; }
      set { m_iTrack = value; }
    }

    /// <summary>
    /// Property to get/set the Total Track number field of the music file
    /// </summary>
    public int TrackTotal
    {
      get { return m_iNumTrack; }
      set { m_iNumTrack = value; }
    }

    /// <summary>
    /// Property to get/set the Disc Id field of the music file
    /// </summary>
    public int DiscID
    {
      get { return m_iDiscId; }
      set { m_iDiscId = value; }
    }

    /// <summary>
    /// Property to get/set the Total Disc number field of the music file
    /// </summary>
    public int DiscTotal
    {
      get { return m_iNumDisc; }
      set { m_iNumDisc = value; }
    }

    /// <summary>
    /// Property to get/set the Track number field of the music file
    /// </summary>
    public int Rating
    {
      get { return m_iRating; }
      set { m_iRating = value; }
    }

    /// <summary>
    /// Property to get/set the number of times this file has been played
    /// </summary>
    public int TimesPlayed
    {
      get { return m_TimesPlayed; }
      set { m_TimesPlayed = value; }
    }

    /// <summary>
    /// Property to get/set the File Type of the music file
    /// </summary>
    public string FileType
    {
      get { return m_FileType; }
      set { m_FileType = value; }
    }

    /// <summary>
    /// Property to get/set the Bit Rate field of the music file
    /// </summary>
    public int BitRate
    {
      get { return m_BitRate; }
      set { m_BitRate = value; }
    }

    /// <summary>
    /// Property to get/set the Album Artist field of the music file
    /// </summary>
    public string AlbumArtist
    {
      get { return m_AlbumArtist; }
      set { m_AlbumArtist = value; }
    }

    /// <summary>
    /// Property to get/set the Album Artist Sort field of the music file
    /// </summary>
    public string AlbumArtistSort
    {
      get { return m_AlbumArtistSort; }
      set
      {
        if (string.IsNullOrEmpty(value)) return;
        m_AlbumArtistSort = value.Trim();
      }
    }

    public bool HasAlbumArtist
    {
      get { return m_hasAlbumArtist; }
      set { m_hasAlbumArtist = value; }
    }

    /// <summary>
    /// Property to get/set the Composer field of the music file
    /// </summary>
    public string Composer
    {
      get { return m_Composer; }
      set { m_Composer = value; }
    }

    /// <summary>
    /// Property to get/set the Composer Sort field of the music file
    /// </summary>
    public string ComposerSort
    {
      get { return m_ComposerSort; }
      set
      {
        if (string.IsNullOrEmpty(value)) return;
        m_ComposerSort = value.Trim();
      }
    }

    /// <summary>
    /// Property to get/set the Conductor field of the music file
    /// </summary>
    public string Conductor
    {
      get { return m_Conductor; }
      set { m_Conductor = value; }
    }

    /// <summary>
    /// Property to get/set the File name of the music file
    /// </summary>
    public string FileName
    {
      get { return m_FileName; }
      set { m_FileName = value; }
    }
    
    /// <summary>
    /// Property to get/set the Lyrics field of the music file
    /// </summary>
    public string Lyrics
    {
      get { return m_Lyrics; }
      set { m_Lyrics = value; }
    }

    /// <summary>
    /// Property to get/set the Title Codec of the music file
    /// </summary>
    public string Codec
    {
      get { return m_Codec; }
      set { m_Codec = value; }
    }

    /// <summary>
    /// Property to get/set the Bit Rate Mode field of the music file (only works for MP3)
    /// </summary>
    public string BitRateMode
    {
      get { return m_BitRateMode; }
      set { m_BitRateMode = value; }
    }

    /// <summary>
    /// Property to get/set the BPM field of the music file
    /// </summary>
    public int BPM
    {
      get { return m_BPM; }
      set { m_BPM = value; }
    }

    /// <summary>
    /// Property to get/set the Channels field of the music file
    /// </summary>
    public int Channels
    {
      get { return m_Channels; }
      set { m_Channels = value; }
    }

    /// <summary>
    /// Property to get/set the Sample Rate field of the music file
    /// </summary>
    public int SampleRate
    {
      get { return m_SampleRate; }
      set { m_SampleRate = value; }
    }

    public byte[] CoverArtImageBytes
    {
      get { return m_CoverArtImageBytes; }
      set { m_CoverArtImageBytes = value; }
    }

    public DateTime DateTimeModified
    {
      get { return m_dateTimeModified; }
      set { m_dateTimeModified = value; }
    }

    /// <summary>
    /// Last UTC time the song was played
    /// </summary>
    public DateTime DateTimePlayed
    {
      get { return m_dateTimePlayed; }
      set { m_dateTimePlayed = value; }
    }

    public string CoverArtFile
    {
      get { return Utils.GetImageFile(m_CoverArtImageBytes, String.Empty); }
    }

    /// <summary>
    /// Property to get/set the Grouping field of the music file
    /// </summary>
    public string Grouping
    {
      get { return m_Grouping; }
      set
      {
        if (string.IsNullOrEmpty(value)) return;
        m_Grouping = value.Trim();
      }
    }


    #endregion
  }
}