﻿/*
    Little Registry Cleaner
    Copyright (C) 2008 Little Apps (http://www.littleapps.co.cc/)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Little_Registry_Cleaner.Scanners
{
    public class AppInfo
    {
        /// <summary>
        /// Verifies installed programs in add/remove list
        /// </summary>
        public AppInfo()
        {
            try
            {
                using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall"))
                {
                    if (regKey == null)
                        return;

                    foreach (string strProgName in regKey.GetSubKeyNames())
                    {

                        RegistryKey regKey2 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\" + strProgName);

                        ScanDlg.UpdateScanSubKey(regKey2.ToString());

                        // Skip if installed by msi installer
                        if (Convert.ToInt32(regKey2.GetValue("WindowsInstaller")) == 1)
                            continue;

                        // Check display icon
                        string strDisplayIcon = regKey2.GetValue("DisplayIcon") as string;
                        {
                            if (!string.IsNullOrEmpty(strDisplayIcon))
                                if (!Utils.IconExists(strDisplayIcon))
                                    ScanDlg.StoreInvalidKey("Invalid file or folder", regKey2.ToString(), "DisplayIcon");
                        }

                        // Check install location
                        string strInstallLocation = regKey2.GetValue("InstallLocation") as string;
                        if (!string.IsNullOrEmpty(strInstallLocation))
                            if ((!Utils.DirExists(strInstallLocation)) && (!Utils.FileExists(strInstallLocation)))
                                ScanDlg.StoreInvalidKey("Invalid file or folder", regKey2.ToString());

                    }
                }

                using (RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Management\\ARPCache"))
                {
                    if (rk == null)
                        return;

                    foreach (string strSubKey in rk.GetSubKeyNames())
                    {
                        using (RegistryKey rkARPCache = rk.OpenSubKey(strSubKey))
                        {
                            if (rkARPCache != null)
                            {
                                byte[] b = (byte[])rkARPCache.GetValue("SlowInfoCache");

                                GCHandle gcHandle = GCHandle.Alloc(b, GCHandleType.Pinned);
                                IntPtr ptr = gcHandle.AddrOfPinnedObject();
                                Utils.SlowInfoCache objSlowInfoCache = (Utils.SlowInfoCache)Marshal.PtrToStructure(ptr, typeof(Utils.SlowInfoCache));

                                if ((Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\" + strSubKey) == null) && (Utils.FileExists(objSlowInfoCache.Name) == false))
                                    ScanDlg.StoreInvalidKey("Invalid registry key", rkARPCache.ToString());

                                gcHandle.Free();
                                rkARPCache.Close();
                            }
                        }
                    }
                }
            }
            catch (System.Security.SecurityException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
    }
}
