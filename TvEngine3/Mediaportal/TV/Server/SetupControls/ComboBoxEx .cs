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

using System.Drawing;
using System.Windows.Forms;

namespace Mediaportal.TV.Server.SetupControls
{
  public class ComboBoxEx : ComboBox
  {
    private ImageList imageList;

    public ComboBoxEx()
    {
      DrawMode = DrawMode.OwnerDrawFixed;
    }

    public ImageList ImageList
    {
      get { return imageList; }
      set { imageList = value; }
    }

    protected override void OnDrawItem(DrawItemEventArgs ea)
    {
      ea.DrawBackground();
      ea.DrawFocusRectangle();

      Size imageSize = imageList.ImageSize;
      Rectangle bounds = ea.Bounds;

      try
      {
        ComboBoxExItem item = (ComboBoxExItem) Items[ea.Index];

        if (item.ImageIndex != -1)
        {
          imageList.Draw(ea.Graphics, bounds.Left, bounds.Top, item.ImageIndex);
          ea.Graphics.DrawString(item.Text, ea.Font, new SolidBrush(ea.ForeColor), bounds.Left + imageSize.Width,
                                 bounds.Top);
        }
        else
        {
          ea.Graphics.DrawString(item.Text, ea.Font, new SolidBrush(ea.ForeColor), bounds.Left, bounds.Top);
        }
      }
      catch
      {
        if (ea.Index != -1)
        {
          ea.Graphics.DrawString(Items[ea.Index].ToString(), ea.Font, new SolidBrush(ea.ForeColor), bounds.Left,
                                 bounds.Top);
        }
        else
        {
          ea.Graphics.DrawString(Text, ea.Font, new SolidBrush(ea.ForeColor), bounds.Left, bounds.Top);
        }
      }

      base.OnDrawItem(ea);
    }
  }

  public class ComboBoxExItem
  {
    private readonly int _id;

    private string _text;

    public ComboBoxExItem()
      : this("")
    {
    }

    public ComboBoxExItem(string text)
      : this(text, -1, -1)
    {
    }

    public ComboBoxExItem(string text, int imageIndex, int id)
    {
      _id = id;
      _text = text;
      ImageIndex = imageIndex;
    }

    public string Text
    {
      get { return _text; }
      set { _text = value; }
    }

    public int ImageIndex { get; set; }

    public int Id
    {
      get { return _id; }
    }

    public override string ToString()
    {
      return _text;
    }
  }
}