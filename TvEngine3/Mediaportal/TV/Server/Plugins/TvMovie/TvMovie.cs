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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using Castle.Core;
using MediaPortal.Common.Utils;
using Mediaportal.TV.Server.Plugins.Base.Interfaces;
using Mediaportal.TV.Server.Plugins.PowerScheduler.Interfaces.Interfaces;
using Mediaportal.TV.Server.SetupControls;
using Mediaportal.TV.Server.TVControl.Interfaces.Services;
using Mediaportal.TV.Server.TVDatabase.Entities;
using Mediaportal.TV.Server.TVDatabase.TVBusinessLayer;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Logging;
using Microsoft.Win32;
using Timer = System.Timers.Timer;

namespace Mediaportal.TV.Server.Plugins.TvMovie
{
  [Interceptor("PluginExceptionInterceptor")]
  public class TvMovie : ITvServerPlugin, ITvServerPluginStartedAll
  {
    #region Members

    private const long _timerIntervall = 1800000;
    private const string _localMachineRegSubKey = @"Software\Ewe\TVGhost\Gemeinsames";

    private const string _virtualStoreRegSubKey32b =
      @"Software\Classes\VirtualStore\MACHINE\SOFTWARE\Ewe\TVGhost\Gemeinsames";

    private const string _virtualStoreRegSubKey64b =
      @"Software\Classes\VirtualStore\MACHINE\SOFTWARE\Wow6432Node\Ewe\TVGhost\Gemeinsames";

    private TvMovieDatabase _database;
    private bool _isImporting;
    private Timer _stateTimer;

    #endregion

    #region Static properties

    public static string TVMovieProgramPath
    {
      get
      {
        Setting setting = SettingsManagement.GetSetting("TvMovieInstallPath", string.Empty);
        string path = setting.Value;

        if (!File.Exists(path))
        {
          path = GetRegistryValueFromValueName("ProgrammPath");
          setting.Value = path;
        }
        return path;
      }
    }

    /// <summary>
    /// Retrieves or sets the location of TVDaten.mdb - prefers manual configured path, does fallback to registry.
    /// </summary>
    public static string DatabasePath
    {
      get
      {
        string path = SettingsManagement.GetSetting("TvMoviedatabasepath", string.Empty).Value;

        if (!File.Exists(path))
          path = GetRegistryValueFromValueName("DBDatei");

        return path;
      }
      set
      {
        string path = value;

        //If passed path is invalid
        if (!File.Exists(path))
        {
          path = DatabasePath;
        }
        SettingsManagement.SaveSetting("TvMoviedatabasepath", path);
      }
    }

    private static string GetRegistryValueFromValueName(string valueName)
    {
      string value = string.Empty;

      try
      {
        //Try to get value from HKLM first
        using (RegistryKey rkey = Registry.LocalMachine.OpenSubKey(_localMachineRegSubKey))
          if (rkey != null)
            value = string.Format("{0}", rkey.GetValue(valueName));

        //Otherwise try to get it from VirtualStore
        if (string.IsNullOrEmpty(value))
        {
          string virtualStoreSubKey = Check64bit() ? _virtualStoreRegSubKey64b : _virtualStoreRegSubKey32b;

          foreach (String userKeyName in Registry.Users.GetSubKeyNames())
          {
            using (
              RegistryKey rkey = Registry.Users.OpenSubKey(String.Format(@"{0}\{1}", userKeyName, virtualStoreSubKey)))
              if (rkey != null)
                value = string.Format("{0}", rkey.GetValue(valueName));

            if (!String.IsNullOrEmpty(value))
              break;
          }
        }
      }
      catch (Exception ex)
      {
        this.LogError(ex, "TVMovie: Registry lookup for {0} failed", valueName);
      }

      if (string.IsNullOrEmpty(value))
      {
        Log.Info("TVMovie: Registry setting {1} has no value", valueName);
      }

      return value;
    }

    #endregion

    #region IsWow64 check

    [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWow64Process(
      [In] IntPtr hProcess,
      [Out] out bool lpSystemInfo
      );

    public static bool Check64bit()
    {
      //IsWow64Process is not supported under Windows2000
      if (!OSInfo.OSInfo.XpOrLater())
      {
        return false;
      }

      Process p = Process.GetCurrentProcess();
      IntPtr handle = p.Handle;
      bool isWow64;
      bool success = IsWow64Process(handle, out isWow64);
      if (!success)
      {
        throw new Win32Exception();
      }
      return isWow64;
    }

    #endregion

    #region Powerscheduler handling

    private void SetStandbyAllowed(bool allowed)
    {
      if (GlobalServiceProvider.Instance.IsRegistered<IEpgHandler>())
      {
        GlobalServiceProvider.Instance.Get<IEpgHandler>().SetStandbyAllowed(this, allowed, 1800);
        if (!allowed)
          this.LogDebug("TVMovie: Telling PowerScheduler standby is allowed: {0}, timeout is 30 minutes", allowed);
      }
    }

    private void RegisterForEPGSchedule()
    {
      // Register with the EPGScheduleDue event so we are informed when
      // the EPG wakeup schedule is due.
      if (GlobalServiceProvider.Instance.IsRegistered<IEpgHandler>())
      {
        var handler = GlobalServiceProvider.Instance.Get<IEpgHandler>();
        if (handler != null)
        {
          handler.EPGScheduleDue += EPGScheduleDue;
          this.LogDebug("TVMovie: registered with PowerScheduler EPG handler");
          return;
        }
      }
      this.LogDebug("TVMovie: NOT registered with PowerScheduler EPG handler");
    }

    private void EPGScheduleDue()
    {
      SpawnImportThread();
    }

    #endregion

    #region Import methods

    private void ImportThread()
    {
      try
      {
        _isImporting = true;

        try
        {
          _database = new TvMovieDatabase();
          _database.Connect();
        }
        catch (Exception)
        {
          this.LogError("TVMovie: Import enabled but the ClickFinder database was not found.");
          return;
        }

        //this.LogDebug("TVMovie: Checking database");
        try
        {
          if (_database.NeedsImport)
          {
            SetStandbyAllowed(false);

            long updateDuration = _database.LaunchTVMUpdater(true);
            if (updateDuration < 1200)
            {
              // Updating a least a few programs should take more than 20 seconds
              if (updateDuration > 20)
                _database.Import();
              else
                this.LogInfo("TVMovie: Import skipped because there was no new data.");
            }
            else
              this.LogInfo("TVMovie: Import skipped because the update process timed out / has been aborted.");
          }
        }
        catch (Exception ex)
        {
          this.LogInfo("TvMovie plugin error:");
          this.LogError(ex);
        }
      }
      finally
      {
        _isImporting = false;
        SetStandbyAllowed(true);
      }
    }

    private void StartImportThread(object source, ElapsedEventArgs e)
    {
      //TODO: check stateinfo
      SpawnImportThread();
    }

    private void SpawnImportThread()
    {
      try
      {
        if (SettingsManagement.GetSetting("TvMovieEnabled", "false").Value != "true")
        {
          return;
        }
      }
      catch (Exception ex1)
      {
        this.LogError("TVMovie: Error checking enabled status - {0},{1}", ex1.Message, ex1.StackTrace);
      }

      if (!_isImporting)
      {
        try
        {
          var importThread = new Thread(ImportThread);
          importThread.Name = "TV Movie importer";
          importThread.IsBackground = true;
          importThread.Priority = ThreadPriority.Lowest;
          importThread.Start();
        }
        catch (Exception ex2)
        {
          this.LogError("TVMovie: Error spawing import thread - {0},{1}", ex2.Message, ex2.StackTrace);
        }
      }
    }

    private void StartStopTimer(bool startNow)
    {
      if (startNow)
      {
        if (_stateTimer == null)
        {
          _stateTimer = new Timer();
          _stateTimer.Elapsed += StartImportThread;
          _stateTimer.Interval = _timerIntervall;
          _stateTimer.AutoReset = true;

          GC.KeepAlive(_stateTimer);
        }
        _stateTimer.Start();
        _stateTimer.Enabled = true;
      }
      else
      {
        _stateTimer.Enabled = false;
        _stateTimer.Stop();
        this.LogDebug("TVMovie: background import timer stopped");
      }
    }

    #endregion

    public bool MasterOnly
    {
      get { return false; }
    }

    #region ITvServerPlugin Members

    public string Name
    {
      get { return "TV Movie EPG import"; }
    }

    public string Version
    {
      get { return "1.0.3.0"; }
    }

    public string Author
    {
      get { return "rtv"; }
    }

    public void Start(IInternalControllerService controllerService)
    {
      StartStopTimer(true);
    }

    public void Stop()
    {
      if (_database != null)
        _database.Canceled = true;
      if (_stateTimer != null)
      {
        StartStopTimer(false);
        _stateTimer.Dispose();
      }
    }

    public SectionSettings Setup
    {
      get { return new TvMovieSetup(); }
    }

    #endregion

    #region ITvServerPluginStartedAll Members

    public void StartedAll()
    {
      RegisterForEPGSchedule();
    }

    #endregion
  }
}