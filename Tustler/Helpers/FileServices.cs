using System;
using System.Collections.Generic;
using System.Text;

namespace Tustler.Helpers
{
    /// <summary>
    /// Modified from https://stackoverflow.com/questions/58510/using-net-how-can-you-find-the-mime-type-of-a-file-based-on-the-file-signature/9435701#9435701
    /// </summary>
    /// <see cref="Frederick Samson"/>
    public static class FileServices
    {
        public static string GetMimeType(string sFilePath)
        {
            string sMimeType = TustlerServicesLib.MimeTypeDictionary.GetMimeTypeFromList(sFilePath);

            if (String.IsNullOrEmpty(sMimeType))
            {
                sMimeType = TustlerWinPlatformLib.NativeMethods.GetMimeTypeFromFile(sFilePath);

                if (String.IsNullOrEmpty(sMimeType))
                {
                    sMimeType = TustlerWinPlatformLib.RegistryServices.GetMimeTypeFromRegistry(sFilePath);
                }
            }

            return sMimeType;
        }
    }
}
