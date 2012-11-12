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
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Design;
using System.Runtime.InteropServices;
using System.Windows.Forms;

//*************************************************************
//
//	Thanks to Michael Dobler for the idea on using standard 
//	Windows API calls to provide listview grouping support
//
//*************************************************************

namespace Mediaportal.TV.Server.Plugins.ServerBlaster.Learn.XPListView
{
  public class XPListView : ListView
  {
    private IntPtr _apiRetVal;
    private bool _autoGroup;
    private ColumnHeader _autoGroupCol;
    private readonly ArrayList _autoGroupList = new ArrayList();
    private string _emptyAutoGroupText = "";
    private readonly XPListViewGroupCollection _groups;
    private readonly XPListViewItemCollection _items;
    private bool _showInGroups;

    public XPListView()
    {
      _items = new XPListViewItemCollection(this);
      _groups = new XPListViewGroupCollection(this);
    }

    #region Designer Properties

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content),
     Description("the items collection of this view"),
     Editor(typeof (XPListViewItemCollectionEditor), typeof (UITypeEditor)),
     Category("Behavior")]
    public new XPListViewItemCollection Items
    {
      get { return _items; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content),
     Description("collection of available groups (manually added)"),
     Editor(typeof (CollectionEditor), typeof (UITypeEditor)),
     Category("Grouping")]
    public XPListViewGroupCollection Groups
    {
      get { return _groups; }
    }

    [Category("Grouping"),
     Description("flag if the grouping view is active"),
     DefaultValue(false)]
    public bool ShowInGroups
    {
      get { return _showInGroups; }
      set
      {
        if (_showInGroups != value)
        {
          _showInGroups = value;
          if (_autoGroup && value == false)
          {
            _autoGroup = false;
            _autoGroupCol = null;
            _autoGroupList.Clear();
          }

          int param = 0;
          int wParam = Convert.ToInt32(value);
          ListViewAPI.SendMessage(Handle, ListViewAPI.LVM_ENABLEGROUPVIEW, wParam, ref param);
        }
      }
    }

    [Category("Grouping"),
     Description("flag if the autogroup mode is active"),
     DefaultValue(false)]
    public bool AutoGroupMode
    {
      get { return _autoGroup; }
      set
      {
        _autoGroup = value;
        if (_autoGroupCol != null)
        {
          AutoGroupByColumn(_autoGroupCol.Index);
        }
      }
    }

    [Category("Grouping"),
     Description("column by with values the listiew is automatically grouped"),
     DefaultValue(typeof (ColumnHeader), ""),
     DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public ColumnHeader AutoGroupColumn
    {
      get { return _autoGroupCol; }
      set
      {
        _autoGroupCol = value;

        if (_autoGroupCol != null)
        {
          AutoGroupByColumn(_autoGroupCol.Index);
        }
      }
    }

    [Category("Grouping"),
     Description("the text that is displayed instead of an empty auto group text"),
     DefaultValue("")]
    public string EmptyAutoGroupText
    {
      get { return _emptyAutoGroupText; }
      set
      {
        _emptyAutoGroupText = value;

        if (_autoGroupCol != null)
        {
          AutoGroupByColumn(_autoGroupCol.Index);
        }
      }
    }

    [Browsable(false),
     Description("readonly array of all automatically created groups"),
     Category("Grouping")]
    public Array Autogroups
    {
      get { return _autoGroupList.ToArray(typeof (String)); }
    }

    #endregion

    public void ShowTiles(int[] columns)
    {
      IntPtr lpcol = Marshal.AllocHGlobal(columns.Length*4);
      Marshal.Copy(columns, 0, lpcol, columns.Length);

      int param = 0;
      _apiRetVal = ListViewAPI.SendMessage(Handle, ListViewAPI.LVM_SETVIEW, ListViewAPI.LV_VIEW_TILE, ref param);

      var apiTileView = new ListViewAPI.LVTILEVIEWINFO();
      apiTileView.cbSize = Marshal.SizeOf(typeof (ListViewAPI.LVTILEVIEWINFO));
      apiTileView.dwMask = ListViewAPI.LVTVIM_COLUMNS | ListViewAPI.LVTVIM_TILESIZE;
      apiTileView.dwFlags = ListViewAPI.LVTVIF_AUTOSIZE;
      apiTileView.cLines = columns.Length;


      _apiRetVal = ListViewAPI.SendMessage(Handle, ListViewAPI.LVM_SETTILEVIEWINFO, 0, ref apiTileView);

      foreach (XPListViewItem itm in Items)
      {
        ListViewAPI.LVTILEINFO apiTile = new ListViewAPI.LVTILEINFO();
        apiTile.cbSize = Marshal.SizeOf(typeof (ListViewAPI.LVTILEINFO));
        apiTile.iItem = itm.Index;
        apiTile.cColumns = columns.Length;
        apiTile.puColumns = (int) lpcol;

        _apiRetVal = ListViewAPI.SendMessage(Handle, ListViewAPI.LVM_SETTILEINFO, 0, ref apiTile);
      }

      //columns = null;
      Marshal.FreeHGlobal(lpcol);
    }


    public void SetTileSize(Size size)
    {
      SuspendLayout();

      int param = 0;
      _apiRetVal = ListViewAPI.SendMessage(Handle, ListViewAPI.LVM_GETVIEW, ListViewAPI.LV_VIEW_TILE,
                                           ref param);
      if ((int) _apiRetVal != ListViewAPI.LV_VIEW_TILE)
      {
        return;
      }

      var apiSize = new ListViewAPI.INTEROP_SIZE();
      apiSize.cx = size.Width;
      apiSize.cy = size.Height;

      var apiTileView = new ListViewAPI.LVTILEVIEWINFO();
      apiTileView.cbSize = Marshal.SizeOf(typeof (ListViewAPI.LVTILEVIEWINFO));
      apiTileView.dwMask = ListViewAPI.LVTVIM_TILESIZE | ListViewAPI.LVTVIM_LABELMARGIN;
      _apiRetVal = ListViewAPI.SendMessage(Handle, ListViewAPI.LVM_GETTILEVIEWINFO, 0, ref apiTileView);
      apiTileView.dwFlags = ListViewAPI.LVTVIF_FIXEDSIZE;
      apiTileView.sizeTile = apiSize;
      _apiRetVal = ListViewAPI.SendMessage(Handle, ListViewAPI.LVM_SETTILEVIEWINFO, 0, ref apiTileView);

      ResumeLayout();
    }


    public void SetTileWidth(int width)
    {
      SuspendLayout();

      int param = 0;
      _apiRetVal = ListViewAPI.SendMessage(Handle, ListViewAPI.LVM_GETVIEW, ListViewAPI.LV_VIEW_TILE, ref param);
      if ((int) _apiRetVal != ListViewAPI.LV_VIEW_TILE)
      {
        return;
      }

      var apiTileView = new ListViewAPI.LVTILEVIEWINFO();
      apiTileView.cbSize = Marshal.SizeOf(typeof (ListViewAPI.LVTILEVIEWINFO));
      apiTileView.dwMask = ListViewAPI.LVTVIM_TILESIZE | ListViewAPI.LVTVIM_LABELMARGIN;
      _apiRetVal = ListViewAPI.SendMessage(Handle, ListViewAPI.LVM_GETTILEVIEWINFO, 0, ref apiTileView);
      apiTileView.dwFlags = ListViewAPI.LVTVIF_FIXEDWIDTH;
      apiTileView.sizeTile.cx = width;
      _apiRetVal = ListViewAPI.SendMessage(Handle, ListViewAPI.LVM_SETTILEVIEWINFO, 0, ref apiTileView);

      ResumeLayout();
    }


    public void SetTileHeight(int height)
    {
      SuspendLayout();

      int param = 0;
      _apiRetVal = ListViewAPI.SendMessage(Handle, ListViewAPI.LVM_GETVIEW, ListViewAPI.LV_VIEW_TILE, ref param);
      if ((int) _apiRetVal != ListViewAPI.LV_VIEW_TILE)
      {
        return;
      }


      var apiTileView = new ListViewAPI.LVTILEVIEWINFO();

      apiTileView.cbSize = Marshal.SizeOf(typeof (ListViewAPI.LVTILEVIEWINFO));
      apiTileView.dwMask = ListViewAPI.LVTVIM_TILESIZE | ListViewAPI.LVTVIM_LABELMARGIN;
      _apiRetVal = ListViewAPI.SendMessage(Handle, ListViewAPI.LVM_GETTILEVIEWINFO, 0, ref apiTileView);
      apiTileView.dwFlags = ListViewAPI.LVTVIF_FIXEDHEIGHT;
      apiTileView.sizeTile.cy = height;
      _apiRetVal = ListViewAPI.SendMessage(Handle, ListViewAPI.LVM_SETTILEVIEWINFO, 0, ref apiTileView);


      ResumeLayout();
    }


    public bool AutoGroupByColumn(int columnID)
    {
      if (columnID >= Columns.Count || columnID < 0)
      {
        return false;
      }

      try
      {
        _autoGroupList.Clear();
        foreach (XPListViewItem itm in Items)
        {
          if (
            !_autoGroupList.Contains(itm.SubItems[columnID].Text == ""
                                       ? _emptyAutoGroupText
                                       : itm.SubItems[columnID].Text))
          {
            _autoGroupList.Add(itm.SubItems[columnID].Text == "" ? EmptyAutoGroupText : itm.SubItems[columnID].Text);
          }
        }

        _autoGroupList.Sort();

        ListViewAPI.ClearListViewGroup(this);
        foreach (string text in _autoGroupList)
        {
          ListViewAPI.AddListViewGroup(this, text, _autoGroupList.IndexOf(text));
        }

        foreach (XPListViewItem itm in Items)
        {
          ListViewAPI.AddItemToGroup(this, itm.Index,
                                     _autoGroupList.IndexOf(itm.SubItems[columnID].Text == ""
                                                              ? _emptyAutoGroupText
                                                              : itm.SubItems[columnID].Text));
        }

        int param = 0;
        ListViewAPI.SendMessage(Handle, ListViewAPI.LVM_ENABLEGROUPVIEW, 1, ref param);
        _showInGroups = true;
        _autoGroup = true;
        _autoGroupCol = Columns[columnID];

        Refresh();

        return true;
      }
      catch (Exception ex)
      {
        throw new SystemException("Error in XPListView.AutoGroupByColumn: " + ex.Message);
      }
    }


    public bool Regroup()
    {
      try
      {
        ListViewAPI.ClearListViewGroup(this);
        foreach (XPListViewGroup grp in Groups)
        {
          ListViewAPI.AddListViewGroup(this, grp.GroupText, grp.GroupIndex);
        }

        foreach (XPListViewItem itm in Items)
        {
          ListViewAPI.AddItemToGroup(this, itm.Index, itm.GroupIndex);
        }

        int param = 0;
        ListViewAPI.SendMessage(Handle, ListViewAPI.LVM_ENABLEGROUPVIEW, 1, ref param);
        _showInGroups = true;
        _autoGroup = false;
        _autoGroupCol = null;
        _autoGroupList.Clear();

        return true;
      }
      catch (Exception ex)
      {
        throw new SystemException("Error in XPListView.Regroup: " + ex.Message);
      }
    }


    public void RedrawItems()
    {
      ListViewAPI.RedrawItems(this, true);
      ArrangeIcons();
    }


    public void UpdateItems()
    {
      ListViewAPI.UpdateItems(this);
    }


    public void SetColumnStyle(int column, Font font, Color foreColor, Color backColor)
    {
      SuspendLayout();

      foreach (XPListViewItem itm in Items)
      {
        if (itm.SubItems.Count > column)
        {
          itm.SubItems[column].Font = font;
          itm.SubItems[column].BackColor = backColor;
          itm.SubItems[column].ForeColor = foreColor;
        }
      }

      ResumeLayout();
    }


    public void SetColumnStyle(int column, Font font, Color foreColor)
    {
      SetColumnStyle(column, font, foreColor, BackColor);
    }


    public void SetColumnStyle(int column, Font font)
    {
      SetColumnStyle(column, font, ForeColor, BackColor);
    }


    public void ResetColumnStyle(int column)
    {
      SuspendLayout();

      foreach (XPListViewItem itm in Items)
      {
        if (itm.SubItems.Count > column)
        {
          itm.SubItems[column].ResetStyle();
        }
      }

      ResumeLayout();
    }


    public void SetBackgroundImage(string ImagePath, ImagePosition Position)
    {
      ListViewAPI.SetListViewImage(this, ImagePath, Position);
    }


    private void _items_ItemAdded(object sender, ListViewItemEventArgs e)
    {
      if (_autoGroup)
      {
        string text = e.Item.SubItems[_autoGroupCol.Index].Text;
        if (!_autoGroupList.Contains(text))
        {
          _autoGroupList.Add(text);
          ListViewAPI.AddListViewGroup(this, text, _autoGroupList.IndexOf(text));
        }
        ListViewAPI.AddItemToGroup(this, e.Item.Index, _autoGroupList.IndexOf(text));
      }
    }


    protected override void OnColumnClick(ColumnClickEventArgs e)
    {
      base.OnColumnClick(e);
      SuspendLayout();
      if (_showInGroups)
      {
        int param = 0;
        ListViewAPI.SendMessage(Handle, ListViewAPI.LVM_ENABLEGROUPVIEW, 0, ref param);
      }
      ListViewItemSorter = new XPListViewItemComparer(e.Column);
      if (Sorting == SortOrder.Descending)
      {
        Sorting = SortOrder.Ascending;
      }
      else
      {
        Sorting = SortOrder.Descending;
      }
      Sort();
      if (_showInGroups)
      {
        int param = 0;
        ListViewAPI.SendMessage(Handle, ListViewAPI.LVM_ENABLEGROUPVIEW, 1, ref param);
        if (_autoGroup)
        {
          AutoGroupByColumn(_autoGroupCol.Index);
        }
        else
        {
          Regroup();
        }
      }
      ResumeLayout();
    }

    protected override void WndProc(ref Message m)
    {
      base.WndProc(ref m);

      switch (m.Msg)
      {
        case ListViewAPI.OCM_NOTIFY:
          var lmsg = (ListViewAPI.NMHDR) m.GetLParam(typeof (ListViewAPI.NMHDR));

          switch (lmsg.code)
          {
            case ListViewAPI.NM_CUSTOMDRAW:
              NotifyListCustomDraw(ref m);
              break;

            case ListViewAPI.LVN_GETDISPINFOW:
              break;

            case ListViewAPI.LVN_ITEMCHANGING:
              break;

            default:
              break;
          }
          break;
      }
    }

    private bool NotifyListCustomDraw(ref Message m)
    {
      m.Result = (IntPtr) ListViewAPI.CDRF_DODEFAULT;
      var nmcd = (ListViewAPI.NMCUSTOMDRAW) m.GetLParam(typeof (ListViewAPI.NMCUSTOMDRAW));
      IntPtr thisHandle = Handle;

      if (nmcd.hdr.hwndFrom != Handle)
      {
        return false;
      }

      switch (nmcd.dwDrawStage)
      {
        case ListViewAPI.CDDS_PREPAINT:
          m.Result = (IntPtr) ListViewAPI.CDRF_NOTIFYITEMDRAW;
          break;
        case ListViewAPI.CDDS_ITEMPREPAINT:
          m.Result = (IntPtr) ListViewAPI.CDRF_NOTIFYSUBITEMDRAW;
          break;
        case (ListViewAPI.CDDS_ITEMPREPAINT | ListViewAPI.CDDS_SUBITEM):
          break;
        default:
          break;
      }
      return false;
    }
  }


  /// <summary>
  /// Only basic support for sorting in this sample - would need to be updated for asc/desc support
  /// </summary>
  public class XPListViewItemComparer : IComparer
  {
    private readonly int col;

    public XPListViewItemComparer()
    {
      col = 0;
    }

    public XPListViewItemComparer(int column)
    {
      col = column;
    }

    #region IComparer Members

    public int Compare(object x, object y)
    {
      return String.Compare(((ListViewItem) x).SubItems[col].Text, ((ListViewItem) y).SubItems[col].Text);
    }

    #endregion
  }
}