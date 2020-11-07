using CloudWeaver.Foundation.Types;
using System;
using System.Collections.Generic;
using System.Text;
using TustlerFFMPEG.Types.CodecInfo;
using TustlerFFMPEG.Types.MediaInfo;

namespace TustlerFFMPEG
{
    public class MediaServices
    {
        public static CodecPair GetCodecInfo(FFMPEGServiceInterface avInterface, NotificationsList notifications, string codecName)
        {
            var result = avInterface.Interop.GetCodecInfo(codecName);
            if (result.IsError)
            {
                var ex = result.Exception;
                notifications.Add(NotificationsList.CreateErrorNotification(ex.Context, ex.Message, ex));
                return null;
            }
            else
            {
                return result.Result;
            }
        }

        public static MediaInfo GetMediaInfo(FFMPEGServiceInterface avInterface, NotificationsList notifications, string inputFilePath)
        {
            var result = avInterface.Interop.GetMediaInfo(inputFilePath);
            if (result.IsError)
            {
                var ex = result.Exception;
                notifications.Add(NotificationsList.CreateErrorNotification(ex.Context, ex.Message, ex));
                return null;
            }
            else
            {
                return result.Result;
            }
        }

        public static bool Transcode(FFMPEGServiceInterface avInterface, NotificationsList notifications, string inputFilePath, string outputFilePath)
        {
            var result = avInterface.Interop.Transcode(inputFilePath, outputFilePath);
            if (result.IsError)
            {
                var ex = result.Exception;
                notifications.Add(NotificationsList.CreateErrorNotification(ex.Context, ex.Message, ex));
                return false;
            }
            else
            {
                return result.Result;
            }
        }
    }
}
