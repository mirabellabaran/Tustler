using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TustlerWinPlatformLib
{
    public static class NativeMethods
    {
        [DllImport("urlmon.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = false)]
        private static extern int FindMimeFromData(IntPtr pBC,
            [MarshalAs(UnmanagedType.LPWStr)] string pwzUrl,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I1, SizeParamIndex = 3)] byte[] pBuffer,
            int cbSize,
            [MarshalAs(UnmanagedType.LPWStr)]  string pwzMimeProposed,
            int dwMimeFlags,
            out IntPtr ppwzMimeOut,
            int dwReserved);

        public static string GetMimeTypeFromFile(string sFilePath)
        {
            if (!System.IO.File.Exists(sFilePath))
                throw new FileNotFoundException($"{sFilePath} not found");

            int maxContent = (int)new FileInfo(sFilePath).Length;
            if (maxContent > 4096) maxContent = 4096;
            FileStream fs = File.OpenRead(sFilePath);


            byte[] buf = new byte[maxContent];
            fs.Read(buf, 0, maxContent);
            fs.Close();

            int result = FindMimeFromData(IntPtr.Zero, sFilePath, buf, maxContent, null, 0, out IntPtr mimeout, 0);

            if (result != 0)
                throw Marshal.GetExceptionForHR(result);

            string mime = Marshal.PtrToStringUni(mimeout);
            Marshal.FreeCoTaskMem(mimeout);

            // the fallback value is either text/plain or application/octet-stream, depending on whether the file was text or binary
            // at this point, GetMimeTypeFromList() will have already detected text files from the extension if there is one
            if (!String.IsNullOrEmpty(mime))
            {
                var noExtension = string.IsNullOrEmpty(Path.GetExtension(sFilePath));
                var acceptNewMimeType = noExtension || (!noExtension && (mime != "text/plain" && mime != "application/octet-stream"));
                if (acceptNewMimeType)
                {
                    return mime;
                }
            }

            return null;
        }

    }
}
