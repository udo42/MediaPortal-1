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
using System.ComponentModel;
using System.Windows.Forms;
using DirectShowLib.BDA;
using Mediaportal.TV.Server.TVDatabase.Entities;

namespace Mediaportal.TV.Server.SetupControls
{
  public partial class FormTuningDetailCommon : Form
  {
    protected FormTuningDetailCommon()
    {
      InitializeComponent();
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TuningDetail TuningDetail { get; set; }

    private void mpButtonCancel_Click(object sender, EventArgs e)
    {
      DialogResult = DialogResult.Cancel;
      Close();
    }

    protected static TuningDetail CreateInitialTuningDetail()
    {
      const string channelName = "";
      const int channelFrequency = 0;
      const int channelNumber = 0;
      const int country = 31;
      bool isRadio = false;
      bool isTv = false;
      const int tunerSource = 0;
      const int videoInputType = 0;
      const int audioInputType = 0;
      bool isVcrSignal = false;
      const int symbolRate = 0;
      const int modulation = (int)ModulationType.ModNotSet;
      const int polarisation = (int)Polarisation.NotSet;
      const int diseqc = 0;
      const int bandwidth = 8;
      const int pmtPid = -1;
      bool freeToAir = true;
      const int networkId = -1;
      const int serviceId = -1;
      const int transportId = -1;
      const int minorChannel = -1;
      const int majorChannel = -1;
      const string provider = "";
      const int channelType = 0;
      const int idLnbType = 0;
      const int satIndex = -1;
      var innerFecRate = (int)BinaryConvolutionCodeRate.RateNotSet;
      var pilot = (int)Pilot.NotSet;
      var rollOff = (int)RollOff.NotSet;
      const string url = "";
      const int bitrate = 0;

      var initialTuningDetail = new TuningDetail
      {
        Name = channelName,
        Provider = provider,
        ChannelType = channelType,
        ChannelNumber = channelNumber,
        Frequency = channelFrequency,
        CountryId = country,
        NetworkId = networkId,
        TransportId = transportId,
        ServiceId = serviceId,
        PmtPid = pmtPid,
        FreeToAir = true,
        Modulation = modulation,
        Polarisation = polarisation,
        Symbolrate = symbolRate,
        DiSEqC = diseqc,
        Bandwidth = bandwidth,
        MajorChannel = majorChannel,
        MinorChannel = minorChannel,
        VideoSource = videoInputType,
        AudioSource = audioInputType,
        IsVCRSignal = false,
        TuningSource = tunerSource,
        SatIndex = satIndex,
        InnerFecRate = innerFecRate,
        Pilot = pilot,
        RollOff = rollOff,
        Url = url,
        Bitrate = 0,
        IdLnbType = idLnbType
      };

      return initialTuningDetail;
    }
  }
}