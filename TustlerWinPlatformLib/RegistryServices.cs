#nullable enable
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TustlerWinPlatformLib
{
    public static class RegistryServices
    {
        public static string? GetMimeTypeFromRegistry(string sFileNameOrPath)
        {
            string? sMimeType = null;
            string sExtension = Path.GetExtension(sFileNameOrPath).ToLowerInvariant();
            RegistryKey pKey = Registry.ClassesRoot.OpenSubKey(sExtension);

            if (pKey != null && pKey.GetValue("Content Type") != null)
            {
                sMimeType = pKey.GetValue("Content Type").ToString();
            }

            return sMimeType;
        }

    }
}
