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
using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms;
using Mediaportal.TV.Server.TVControl.ServiceAgents;
using Mediaportal.TV.Server.TVDatabase.Entities;
using Mediaportal.TV.Server.TVDatabase.Entities.Enums;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Logging;
using WebEPG.config.WebEPG;
using ChannelMap = WebEPG.config.WebEPG.ChannelMap;

//using System.Data;

namespace Mediaportal.TV.Server.Plugins.WebEPGImport.Config
{
  public delegate fSelection GetGrabberSelectorCallback();

  public partial class WebEPGMappingControl : UserControl
  {
    private Dictionary<string, ChannelMap> _channelMapping;
    private Hashtable _hChannelConfigInfo;
    //private MergedChannelDetails _mergeConfig;
    private bool _isTvMapping;
    private readonly ListViewColumnSorter lvwColumnSorter;

    public WebEPGMappingControl()
    {
      InitializeComponent();

      lvMapping.Columns.Add("EPG Name", 100, HorizontalAlignment.Left);
      lvMapping.Columns.Add("Channel Name", 100, HorizontalAlignment.Left);
      lvMapping.Columns.Add("Channel ID", 80, HorizontalAlignment.Left);
      lvMapping.Columns.Add("Grabber", 120, HorizontalAlignment.Left);

      lvwColumnSorter = new ListViewColumnSorter();
      lvMapping.ListViewItemSorter = lvwColumnSorter;
    }

    [Browsable(true)]
    public bool IsTvMapping
    {
      get { return _isTvMapping; }
      set
      {
        if (_isTvMapping != value)
        {
          _isTvMapping = value;
          UpdateChannelsFrameTitle();
        }
      }
    }

    public Dictionary<string, ChannelMap> ChannelMapping
    {
      get { return _channelMapping; }
      set { _channelMapping = value; }
    }

    public Hashtable HChannelConfigInfo
    {
      get { return _hChannelConfigInfo; }
      set { _hChannelConfigInfo = value; }
    }

    public event EventHandler SelectGrabberClick;
    public event EventHandler AutoMapChannels;

    public void LoadGroups()
    {
      // load all distinct groups
      try
      {
        GroupComboBox.Items.Clear();
        GroupComboBox.Items.Add(new CBChannelGroup("", -1));
        GroupComboBox.Tag = "";
        if (IsTvMapping)
        {
          IList<ChannelGroup> channelGroups = ServiceAgents.Instance.ChannelGroupServiceAgent.ListAllChannelGroups();
          foreach (ChannelGroup cg in channelGroups)
          {
            GroupComboBox.Items.Add(new CBChannelGroup(cg.GroupName, cg.IdGroup));
          }
        }
        else
        {
          IList<ChannelGroup> channelGroups =
            ServiceAgents.Instance.ChannelGroupServiceAgent.ListAllChannelGroupsByMediaType(MediaTypeEnum.Radio);
          foreach (ChannelGroup cg in channelGroups)
          {
            GroupComboBox.Items.Add(new CBChannelGroup(cg.GroupName, cg.IdGroup));
          }
        }
      }
      catch (Exception e)
      {
        this.LogError("Failed to load groups {0}", e.Message);
      }
    }

    public void DoImportChannels()
    {
      this.LogInfo("WebEPG Config: Button: Import");
      try
      {
        this.LogInfo("WebEPG Config: Importing from TV Server Database");
        getTvServerChannels();
        RedrawList(null);
      }
      catch (Exception ex)
      {
        this.LogError(ex, "WebEPG Config: Import failed");
        MessageBox.Show("An error occured while trying to import channels. See log for more details.", "Import Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
      }
    }

    public void OnGrabberSelected(Object source, GrabberSelectedEventArgs e)
    {
      //this.Activate(); -> form control
      GrabberSelectionInfo id = e.Selection;
      if (tcMappingDetails.SelectedIndex == 0) // single mapping
      {
        if (id != null)
        {
          if (UpdateGrabberDetails(id.ChannelId, id.GrabberId))
          {
            foreach (ListViewItem channel in lvMapping.SelectedItems)
            {
              if (_channelMapping.ContainsKey(channel.Text))
              {
                ChannelMap channelMap = _channelMapping[channel.Text];
                channelMap.id = id.ChannelId;
                channelMap.grabber = id.GrabberId;
                _channelMapping.Remove(channel.Text);
                _channelMapping.Add(channel.Text, channelMap);
              }
            }
          }

          UpdateList();
        }
      }
      else // merged mapping
      {
        DataGridViewRow row = null;
        if (dgvMerged.SelectedRows.Count == 1)
        {
          row = dgvMerged.SelectedRows[0];
        }
        else
        {
          row = dgvMerged.CurrentRow;
        }
        if (row != null)
        {
          //MergedChannel channelDetails = (MergedChannel)dgvMerged.SelectedRows[0].DataBoundItem;
          //dgvMerged.BeginEdit(false);
          dgvMerged.CurrentCell = row.Cells["idColumn"];
          dgvMerged.NotifyCurrentCellDirty(true);
          dgvMerged.NotifyCurrentCellDirty(false);
          row.Cells["idColumn"].Value = id.ChannelId;
          row.Cells["grabberColumn"].Value = id.GrabberId;
          //dgvMerged.EndEdit();
        }
      }
    }

    public void OnChannelMappingChanged()
    {
      RedrawList(null);
    }


    private void OnAutoMapChannels()
    {
      if (AutoMapChannels != null)
      {
        AutoMapChannels(this, EventArgs.Empty);
      }
    }

    private void UpdateChannelsFrameTitle()
    {
      if (gbChannels != null)
      {
        gbChannels.Text = (IsTvMapping ? "TV" : "Radio") + " Channel Mapping";
      }
    }

    private void getTvServerChannels()
    {
      var chGroup = (CBChannelGroup) GroupComboBox.SelectedItem;

      IList<Channel> channels = new List<Channel>();
      MediaTypeEnum mediaType = MediaTypeEnum.Radio;
      if (IsTvMapping)
      {
        mediaType = MediaTypeEnum.TV;
      }
      if (chGroup != null && chGroup.idGroup != -1)
      {
        ServiceAgents.Instance.ChannelServiceAgent.GetAllChannelsByGroupIdAndMediaType(chGroup.idGroup, mediaType);
      }
      else
      {
        channels = ServiceAgents.Instance.ChannelServiceAgent.ListAllChannelsByMediaType(mediaType);
      }

      foreach (Channel chan in channels)
      {
        if (!_channelMapping.ContainsKey(chan.DisplayName))
        {
          var channel = new ChannelMap {displayName = chan.DisplayName};
          _channelMapping.Add(chan.DisplayName, channel);
        }
      }
    }

    private void RedrawList(string selectName)
    {
      int selectedIndex = 0;
      if (lvMapping.SelectedIndices.Count > 0)
      {
        selectedIndex = lvMapping.SelectedIndices[0];
      }

      lvMapping.Items.Clear();

      //add all channels
      foreach (ChannelMap channel in _channelMapping.Values)
      {
        var channelItem = new ListViewItem(channel.displayName);
        string name = string.Empty;
        if (channel.id != null)
        {
          var info = (ChannelConfigInfo) _hChannelConfigInfo[channel.id];
          if (info != null)
          {
            name = info.FullName;
          }
        }
        else
        {
          if (channel.merged != null)
          {
            name = "[Merged]";
          }
        }
        channelItem.SubItems.Add(name);
        channelItem.SubItems.Add(channel.id);
        channelItem.SubItems.Add(channel.grabber);
        lvMapping.Items.Add(channelItem);
      }

      if (lvMapping.Items.Count > 0)
      {
        if (lvMapping.Items.Count > selectedIndex)
        {
          lvMapping.Items[selectedIndex].Selected = true;
        }
        else
        {
          lvMapping.Items[lvMapping.Items.Count - 1].Selected = true;
        }
      }

      lvMapping.Sort();
      tbCount.Text = lvMapping.Items.Count.ToString();
      lvMapping.Select();
    }

    private void UpdateList()
    {
      //update existing channels
      foreach (ListViewItem channel in lvMapping.Items)
      {
        if (_channelMapping.ContainsKey(channel.Text))
        {
          ChannelMap channelDetails = _channelMapping[channel.Text];
          string name = string.Empty;
          if (channelDetails.id != null)
          {
            var info = (ChannelConfigInfo) _hChannelConfigInfo[channelDetails.id];
            if (info != null)
            {
              name = info.FullName;
            }
          }
          else
          {
            if (channelDetails.merged != null)
            {
              name = "[Merged]";
            }
          }
          channel.SubItems[1].Text = name;
          channel.SubItems[2].Text = channelDetails.id;
          channel.SubItems[3].Text = channelDetails.grabber;
        }
        else
        {
          int selectedIndex = 0;
          if (lvMapping.SelectedIndices.Count > 0)
          {
            selectedIndex = lvMapping.SelectedIndices[0];
          }

          lvMapping.Items.Remove(channel);

          if (lvMapping.Items.Count > 0)
          {
            if (lvMapping.Items.Count > selectedIndex)
            {
              lvMapping.Items[selectedIndex].Selected = true;
            }
            else
            {
              lvMapping.Items[lvMapping.Items.Count - 1].Selected = true;
            }
          }
        }
      }
      lvMapping.Select();
    }

    private void UpdateMergedList(ChannelMap channelMap)
    {
      bsMergedChannel.DataSource = (channelMap == null) ? null : channelMap.merged;
      bsMergedChannel.ResetBindings(false);
    }

    private void OnSelectGrabberClick()
    {
      if (SelectGrabberClick != null)
      {
        SelectGrabberClick(this, EventArgs.Empty);
      }
    }

    private void DisplaySelectedChannelGrabberInfo()
    {
      if (lvMapping.SelectedItems.Count == 1 && _channelMapping.ContainsKey(lvMapping.SelectedItems[0].Text))
      {
        ChannelMap channel = _channelMapping[lvMapping.SelectedItems[0].Text];
        DisplayChannelGrabberInfo(channel);
      }
      else
      {
        DisplayChannelGrabberInfo(null);
      }
    }

    private bool UpdateGrabberDetails(string channelId, string grabberId)
    {
      tbChannelName.Text = null;
      tbGrabSite.Text = null;
      tbGrabDays.Text = null;

      if (channelId != null && grabberId != null)
      {
        tbChannelName.Tag = channelId;
        var info = (ChannelConfigInfo) _hChannelConfigInfo[channelId];
        if (info != null)
        {
          tbChannelName.Text = info.FullName;
          this.LogInfo("WebEPG Config: Selection: {0}", info.FullName);

          var gInfo = (GrabberConfigInfo) info.GrabberList[grabberId];
          if (gInfo != null)
          {
            tbGrabSite.Text = gInfo.GrabberName;
            //tbGrabSite.Tag = gInfo.GrabberID;
            tbGrabDays.Text = gInfo.GrabDays.ToString();
            return true;
          }
          else
          {
            tbGrabSite.Text = "(Unknown)";
          }
        }
      }
      return false;
    }

    private void DisplayChannelGrabberInfo(ChannelMap channel)
    {
      if (channel == null)
      {
        tcMappingDetails.SelectedIndex = 0;
        UpdateGrabberDetails(null, null);
        UpdateMergedList(null);
      }
      else
      {
        if (channel.merged != null && channel.merged.Count > 0)
        {
          tcMappingDetails.SelectedIndex = 1;
          UpdateMergedList(channel);
        }
        else
        {
          tcMappingDetails.SelectedIndex = 0;
          UpdateGrabberDetails(channel.id, channel.grabber);
        }
      }

      lvMapping.Select();
    }

    #region Event handlers

    private void WebEPGMappingControl_Load(object sender, EventArgs e)
    {
      UpdateChannelsFrameTitle();
    }

    private void bImport_Click(object sender, EventArgs e)
    {
      DoImportChannels();
    }

    private void bRemove_Click(object sender, EventArgs e)
    {
      foreach (ListViewItem channel in lvMapping.SelectedItems)
      {
        if (_channelMapping.ContainsKey(channel.Text))
        {
          _channelMapping.Remove(channel.Text);
        }
      }

      tbCount.Text = lvMapping.Items.Count.ToString();
      UpdateList();
    }

    private void bClearMapping_Click(object sender, EventArgs e)
    {
      foreach (ListViewItem channel in lvMapping.SelectedItems)
      {
        if (_channelMapping.ContainsKey(channel.Text))
        {
          ChannelMap channelMap = _channelMapping[channel.Text];
          channelMap.id = null;
          channelMap.grabber = null;
          channelMap.merged = null;
          _channelMapping.Remove(channel.Text);
          _channelMapping.Add(channel.Text, channelMap);
        }
      }

      UpdateList();
      DisplaySelectedChannelGrabberInfo();
    }

    private void bChannelID_Click(object sender, EventArgs e)
    {
      OnSelectGrabberClick();
    }


    private void lvMapping_SelectedIndexChanged(object sender, EventArgs e)
    {
      DisplaySelectedChannelGrabberInfo();
    }

    private void lvMapping_ColumnClick(object sender, ColumnClickEventArgs e)
    {
      // Determine if clicked column is already the column that is being sorted.
      if (e.Column == lvwColumnSorter.SortColumn)
      {
        // Reverse the current sort direction for this column.
        if (lvwColumnSorter.Order == SortOrder.Ascending)
        {
          lvwColumnSorter.Order = SortOrder.Descending;
        }
        else
        {
          lvwColumnSorter.Order = SortOrder.Ascending;
        }
      }
      else
      {
        // Set the column number that is to be sorted; default to ascending.
        lvwColumnSorter.SortColumn = e.Column;
        lvwColumnSorter.Order = SortOrder.Ascending;
      }

      // Perform the sort with these new sort options.
      lvMapping.Sort();
    }

    //private void bMergedAdd_Click(object sender, EventArgs e)
    //{
    //  lvMerged.SelectedItems.Clear();
    //  //_mergeConfig = new MergedChannelDetails(tGrabbers, null, this.bMergedOk_Click);
    //  //_mergeConfig.MinimizeBox = false;
    //  //_mergeConfig.Show();
    //}

    //private void bMergedOk_Click(object sender, EventArgs e)
    //{
    //  if (lvMapping.SelectedItems.Count == 1)
    //  {
    //    ChannelMap channelMap = _channelMapping[lvMapping.SelectedItems[0].Text];
    //    if (lvMerged.SelectedItems.Count == 1)
    //    {
    //      MergedChannel channelDetails = (MergedChannel) lvMerged.SelectedItems[0].Tag;

    //      channelDetails.id = _mergeConfig.ChannelDetails.id;
    //      channelDetails.grabber = _mergeConfig.ChannelDetails.grabber;
    //      channelDetails.start = _mergeConfig.ChannelDetails.start;
    //      channelDetails.end = _mergeConfig.ChannelDetails.end;
    //    }
    //    else
    //    {
    //      channelMap.merged.Add(_mergeConfig.ChannelDetails);
    //    }
    //    UpdateMergedList(channelMap);
    //  }
    //  _mergeConfig.Close();
    //}

    //private void bMergedRemove_Click(object sender, EventArgs e)
    //{
    //  if (lvMerged.SelectedItems.Count == 1 && lvMapping.SelectedItems.Count == 1)
    //  {
    //    ChannelMap channelMap = _channelMapping[lvMapping.SelectedItems[0].Text];
    //    channelMap.merged.Remove((MergedChannel) lvMerged.SelectedItems[0].Tag);
    //    UpdateMergedList(channelMap);
    //  }
    //}

    //private void bMergedEdit_Click(object sender, EventArgs e)
    //{
    //  if (lvMerged.SelectedItems.Count == 1 && lvMapping.SelectedItems.Count == 1)
    //  {
    //    //MergedChannel channel = (MergedChannel) lvMerged.SelectedItems[0].Tag;
    //    //_mergeConfig = new MergedChannelDetails(tGrabbers, channel, this.bMergedOk_Click);
    //    //_mergeConfig.MinimizeBox = false;
    //    //_mergeConfig.Show();
    //  }
    //}

    private void tcMappingDetails_Selecting(object sender, TabControlCancelEventArgs e)
    {
      if (tcMappingDetails.SelectedIndex == 1)
      {
        if (lvMapping.SelectedItems.Count == 1)
        {
          if (_channelMapping.ContainsKey(lvMapping.SelectedItems[0].Text))
          {
            ChannelMap channelMap = _channelMapping[lvMapping.SelectedItems[0].Text];
            if (channelMap.merged == null || channelMap.merged.Count == 0)
            {
              channelMap.merged = new List<MergedChannel>();
              if (channelMap.id != null)
              {
                var channel = new MergedChannel();
                channel.id = channelMap.id;
                channelMap.id = null;
                channel.grabber = channelMap.grabber;
                channelMap.grabber = null;
                channelMap.merged.Add(channel);
              }
              //_channelMapping.Remove(channel.Text);
              //_channelMapping.Add(channel.Text, channelMap);
            }
            UpdateMergedList(channelMap);
            UpdateList();
          }
        }
        else
        {
          e.Cancel = true;
          MessageBox.Show("Only one channel can be mapped to multiple channels at a time.", "Multiple Selection Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
      }
      else
      {
        if (lvMapping.SelectedItems.Count == 1)
        {
          if (_channelMapping.ContainsKey(lvMapping.SelectedItems[0].Text))
          {
            if (_channelMapping[lvMapping.SelectedItems[0].Text].merged == null ||
                _channelMapping[lvMapping.SelectedItems[0].Text].merged.Count <= 1)
            {
              ChannelMap channelMap = _channelMapping[lvMapping.SelectedItems[0].Text];
              if (channelMap.merged != null)
              {
                if (channelMap.merged.Count > 0)
                {
                  channelMap.id = channelMap.merged[0].id;
                  channelMap.grabber = channelMap.merged[0].grabber;
                }
                channelMap.merged = null;
              }
              UpdateMergedList(channelMap);
              UpdateList();
            }
            else
            {
              e.Cancel = true;
              MessageBox.Show("Cannot convert multiple channels to single channel. Please remove one.",
                              "Multiple Channel Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
          }
        }
      }
    }

    private void dgvMerged_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
    {
      if (dgvMerged.IsCurrentRowDirty)
      {
        DateTime time;
        DataGridViewRow row = dgvMerged.Rows[e.RowIndex];
        string errors = "";
        string newValue = row.Cells["startColumn"].FormattedValue.ToString();
        if (newValue != "" &&
            !DateTime.TryParseExact(newValue, "H:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
        {
          errors += "\nStart time is not valid";
          e.Cancel = true;
        }
        newValue = row.Cells["endColumn"].FormattedValue.ToString();
        if (newValue != "" &&
            !DateTime.TryParseExact(newValue, "H:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
        {
          errors += "\nend time is not valid";
          e.Cancel = true;
        }

        row.ErrorText = errors.TrimStart('\n');
      }
    }

    private void dgvMerged_CellEndEdit(object sender, DataGridViewCellEventArgs e)
    {
      string columnName = dgvMerged.Columns[e.ColumnIndex].Name;
      if (columnName == "startColumn" || columnName == "endColumn")
      {
        //dgvMerged.Rows[e.RowIndex].Cells[e.ColumnIndex].ErrorText = "";
        dgvMerged.Rows[e.RowIndex].ErrorText = "";
      }
    }

    private void dgvMerged_CellContentClick(object sender, DataGridViewCellEventArgs e)
    {
      string columnName = dgvMerged.Columns[e.ColumnIndex].Name;

      if (columnName == "ChooseGrabberColumn")
      {
        dgvMerged.CurrentCell = dgvMerged.Rows[e.RowIndex].Cells[e.ColumnIndex];
        OnSelectGrabberClick();
      }
    }

    private void bAutoMap_Click(object sender, EventArgs e)
    {
      //if (lvMapping.Items.Count == 0)
      //{
      //  DoImportChannels();
      //}
      Cursor.Current = Cursors.WaitCursor;
      OnAutoMapChannels();
      UpdateList();
      Cursor.Current = Cursors.Default;
    }

    #endregion

    #region Nested type: CBChannelGroup

    private class CBChannelGroup
    {
      public readonly string groupName;
      public readonly int idGroup;

      public CBChannelGroup(string groupName, int idGroup)
      {
        this.groupName = groupName;
        this.idGroup = idGroup;
      }

      public override string ToString()
      {
        return groupName;
      }
    }

    #endregion
  }
}