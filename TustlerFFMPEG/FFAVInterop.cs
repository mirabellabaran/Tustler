using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TustlerFFMPEG.Types.CodecInfo;
using TustlerFFMPEG.Types.MediaInfo;

namespace TustlerFFMPEG
{
    public class FFAVInterop : IAVServiceInterface
    {
        // Note that UIntPtr maps to usize in Rust (UIntPtr is word sized)

        [DllImport("ffavwrapper.dll")]
        private static extern Int32 transcode(string inputPath, string outputPath);

        [DllImport("ffavwrapper.dll")]
        private static extern Int32 get_media_info(string filePath, byte[] buffer, UIntPtr bufferLen);

        [DllImport("ffavwrapper.dll")]
        private static extern Int32 get_codec_info(string codecName, byte[] buffer, UIntPtr bufferLen);

        public AVInteropResult<CodecPair> GetCodecInfo(string codecName)
        {
            // e.g. aac, flac, h264
            var json = GetJson(nameof(GetCodecInfo), (byte[] data) => get_codec_info(codecName, data, (UIntPtr)data.Length));

            if (json.IsError)
            {
                return new AVInteropResult<CodecPair>(null, json.Exception);
            }
            else
            {
                var options = new JsonSerializerOptions();
                options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                var codecInfo = JsonSerializer.Deserialize<Types.CodecInfo.CodecPair>(json.Result, options);

                return new AVInteropResult<CodecPair>(codecInfo, null);
            }
        }

        public AVInteropResult<MediaInfo> GetMediaInfo(string inputFilePath)
        {
            //var input = @"C:\Users\Zev\Projects\rust\ffavrunme\temp\temp.avi";
            //var input = @"C:\Users\Zev\Videos\The Shawshank Redemption (1994)\The.Shawshank.Redemption.1994.CD1.AC3.iNTERNAL.DVDRip.XviD-xCZ.avi";

            var json = GetJson(nameof(GetMediaInfo), (byte[] data) => get_media_info(inputFilePath, data, (UIntPtr)data.Length));

            if (json.IsError)
            {
                return new AVInteropResult<MediaInfo>(null, json.Exception);
            }
            else
            {
                var options = new JsonSerializerOptions();
                options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                var mediaInfo = JsonSerializer.Deserialize<Types.MediaInfo.MediaInfo>(json.Result, options);

                return new AVInteropResult<MediaInfo>(mediaInfo, null);
            }
        }   

        public AVInteropResult<bool> Transcode(string inputFilePath, string outputFilePath)
        {
            //var input = @"C:\Users\Zev\Projects\rust\ffavrunme\temp\temp.avi";
            //var output = @"C:\Users\Zev\Projects\rust\ffavrunme\temp\out.wmv";

            var result = transcode(inputFilePath, outputFilePath);

            if (result < 0)
            {
                var ex = HandleError(result, nameof(Transcode));
                return new AVInteropResult<bool>(false, ex);
            }
            else
            {
                return new AVInteropResult<bool>(true, null);
            }
        }

        private static AVInteropException HandleError(int errorCode, string context)
        {
            var message = errorCode switch
            {
                -2 => "FFMPEG initialization error",
                -3 => "Codec not found",
                -4 => "Too many bytes written",
                -5 => "Buffer too small",
                -6 => "Write error",
                -7 => "Serialization error",
                -8 => "Argument conversion to string failed",
                -9 => "Path argument must exist",
                -10 => "Output folder must exist",
                -11 => "Open input path failed",
                -12 => "Open output path failed",
                -13 => "Argument conversion to array failed",
                -14 => "Unknown IO error",
                _ => "Unknown error code"
            };

            return new AVInteropException(context, errorCode, message);
        }

        private static AVInteropResult<string> GetJson(string context, Func<byte[], int> func)
        {
            var data = new byte[5000];
            var result = func(data);

            //Console.WriteLine("Length: {0}", result);
            if (result < 0)
            {
                var ex = HandleError(result, context);
                return new AVInteropResult<string>(null, ex);
            }
            else
            {
                var jsonData = new ReadOnlySpan<byte>(data, 0, result);
                var json = UTF8Encoding.UTF8.GetString(jsonData);

                return new AVInteropResult<string>(json, null);
            }
        }
    }

    //public class MockFFAVInterop : IAVServiceInterface
    //{
    //    public AVInteropResult<CodecPair> GetCodecInfo(string codecName)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public AVInteropResult<MediaInfo> GetMediaInfo(string inputFilePath)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public AVInteropResult<bool> Transcode(string inputFilePath, string outputFilePath)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
