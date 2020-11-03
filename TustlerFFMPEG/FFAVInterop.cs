using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TustlerFFMPEG
{
    namespace Types
    {
        public enum MediaType
        {
            Unknown,
            Video,
            Audio,
            Data,
            Subtitle,
            Attachment,
        }

        namespace CodecInfo
        {
            public enum CodecInfoType
            {
                Encoder,
                Decoder
            }

            public class VideoCodecInfo
            {
                [JsonPropertyName("rates")]
                public String Rates { get; set; }

                [JsonPropertyName("formats")]
                public String Formats { get; set; }
            }

            public class AudioCodecInfo
            {
                [JsonPropertyName("rates")]
                public String Rates { get; set; }

                [JsonPropertyName("formats")]
                public String Formats { get; set; }

                [JsonPropertyName("channel_layouts")]
                public String ChannelLayouts { get; set; }
            }

            public class CodecInfo
            {
                [JsonPropertyName("codec_type")]
                public CodecInfoType CodecType { get; set; }

                [JsonPropertyName("id")]
                public String Id { get; set; }

                [JsonPropertyName("name")]
                public String Name { get; set; }

                [JsonPropertyName("description")]
                public String Description { get; set; }

                [JsonPropertyName("medium")]
                public MediaType Medium { get; set; }

                [JsonPropertyName("capabilities")]
                public String Capabilities { get; set; }

                [JsonPropertyName("profiles")]
                public String Profiles { get; set; }

                [JsonPropertyName("audio")]
                public AudioCodecInfo Audio { get; set; }

                [JsonPropertyName("video")]
                public VideoCodecInfo Video { get; set; }
            }

            public class CodecPair
            {
                [JsonPropertyName("encoder")]
                public CodecInfo Encoder { get; set; }

                [JsonPropertyName("decoder")]
                public CodecInfo Decoder { get; set; }
            }

        }

        namespace MediaInfo
        {
            public class Rational
            {
                [JsonPropertyName("numerator")]
                public int Numerator { get; set; }

                [JsonPropertyName("denominator")]
                public int Denominator { get; set; }
            }

            public class VideoStreamInfo
            {
                [JsonPropertyName("bit_rate")]
                public int BitRate { get; set; }

                [JsonPropertyName("max_bit_rate")]
                public int MaxBitRate { get; set; }

                [JsonPropertyName("delay")]
                public int Delay { get; set; }

                [JsonPropertyName("width")]
                public int Width { get; set; }

                [JsonPropertyName("height")]
                public int Height { get; set; }

                [JsonPropertyName("format")]
                public String Format { get; set; }

                [JsonPropertyName("has_b_frames")]
                public bool HasBFrames { get; set; }

                [JsonPropertyName("aspect_ratio")]
                public Rational AspectRatio { get; set; }

                [JsonPropertyName("color_space")]
                public String ColorSpace { get; set; }

                [JsonPropertyName("color_range")]
                public String ColorRange { get; set; }

                [JsonPropertyName("color_primaries")]
                public String ColorPrimaries { get; set; }

                [JsonPropertyName("color_transfer_characteristic")]
                public String ColorTransferCharacteristic { get; set; }

                [JsonPropertyName("chroma_location")]
                public String ChromaLocation { get; set; }

                [JsonPropertyName("references")]
                public int References { get; set; }

                [JsonPropertyName("intra_dc_precision")]
                public byte IntraDCPrecision { get; set; }
            }

            public class AudioStreamInfo
            {
                [JsonPropertyName("bit_rate")]
                public int BitRate { get; set; }

                [JsonPropertyName("max_bit_rate")]
                public int MaxBitRate { get; set; }

                [JsonPropertyName("delay")]
                public int Delay { get; set; }

                [JsonPropertyName("rate")]
                public int Rate { get; set; }

                [JsonPropertyName("channels")]
                public short Channels { get; set; }

                [JsonPropertyName("format")]
                public String Format { get; set; }

                [JsonPropertyName("frames")]
                public int Frames { get; set; }

                [JsonPropertyName("align")]
                public int Align { get; set; }

                [JsonPropertyName("channel_layout")]
                public String ChannelLayout { get; set; }

                [JsonPropertyName("frame_start")]
                public Nullable<int> FrameStart { get; set; }
            }

            public class StreamInfo
            {
                [JsonPropertyName("index")]
                public int Index { get; set; }

                [JsonPropertyName("codec_medium")]
                public MediaType CodecMedium { get; set; }

                [JsonPropertyName("codec_id")]
                public String CodecId { get; set; }

                [JsonPropertyName("time_base")]
                public Rational TimeBase { get; set; }

                [JsonPropertyName("start_time")]
                public long StartTime { get; set; }

                [JsonPropertyName("duration")]
                public long Duration { get; set; }

                [JsonPropertyName("duration_seconds")]
                public double DurationInSeconds { get; set; }

                [JsonPropertyName("frames")]
                public long Frames { get; set; }

                [JsonPropertyName("disposition")]
                public String Disposition { get; set; }

                [JsonPropertyName("discard")]
                public String Discard { get; set; }

                [JsonPropertyName("rate")]
                public Rational Rate { get; set; }

                [JsonPropertyName("audio")]
                public AudioStreamInfo Audio { get; set; }

                [JsonPropertyName("video")]
                public VideoStreamInfo Video { get; set; }
            }

            public class MediaInfo
            {
                [JsonPropertyName("metadata")]
                public Dictionary<string, string> Metadata { get; set; }

                [JsonPropertyName("best_video_idx")]
                public Nullable<int> BestVideoIndex { get; set; }

                [JsonPropertyName("best_audio_idx")]
                public Nullable<int> BestAudioIndex { get; set; }

                [JsonPropertyName("best_subtitle_idx")]
                public Nullable<int> BestSubtitleIndex { get; set; }

                [JsonPropertyName("duration")]
                public Double Duration { get; set; }

                [JsonPropertyName("streams")]
                public List<StreamInfo> Streams { get; set; }
            }
        }
    }

    public class FFAVInterop
    {
        // Note that UIntPtr maps to usize in Rust (UIntPtr is word sized)

        [DllImport("ffavwrapper.dll")]
        private static extern Int32 transcode(string inputPath, string outputPath);

        [DllImport("ffavwrapper.dll")]
        private static extern Int32 get_media_info(string filePath, byte[] buffer, UIntPtr bufferLen);

        [DllImport("ffavwrapper.dll")]
        private static extern Int32 get_codec_info(string codecName, byte[] buffer, UIntPtr bufferLen);

        public static void GetCodecInfo()
        {
            var json = GetJson((byte[] data) => get_codec_info("h264", data, (UIntPtr)data.Length));

            if (json.Length > 0)
            {
                var options = new JsonSerializerOptions();
                options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                var codecInfo = JsonSerializer.Deserialize<Types.CodecInfo.CodecPair>(json, options);
                Console.WriteLine("Codec Info:\n\t{0}\n\t{1}", codecInfo.Decoder.Name, codecInfo.Decoder.Description);
            }
        }

        public static void GetMediaInfo()
        {
            var input = @"C:\Users\Zev\Projects\rust\ffavrunme\temp\temp.avi";
            //var input = @"C:\Users\Zev\Videos\The Shawshank Redemption (1994)\The.Shawshank.Redemption.1994.CD1.AC3.iNTERNAL.DVDRip.XviD-xCZ.avi";

            var json = GetJson((byte[] data) => get_media_info(input, data, (UIntPtr)data.Length));

            if (json.Length > 0)
            {
                var options = new JsonSerializerOptions();
                options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                var mediaInfo = JsonSerializer.Deserialize<Types.MediaInfo.MediaInfo>(json, options);
                Console.WriteLine("Media Info:\n\tStreams: {0}\n", mediaInfo.Streams.Count);
                foreach (var item in mediaInfo.Metadata)
                {
                    Console.WriteLine("\t{0}: {1}", item.Key, item.Value);
                }
            }
        }

        public static void Transcode()
        {
            var input = @"C:\Users\Zev\Projects\rust\ffavrunme\temp\temp.avi";
            var output = @"C:\Users\Zev\Projects\rust\ffavrunme\temp\out.wmv";

            var result = transcode(input, output);

            if (result < 0)
            {
                HandleError(result);
            }
            else
            {
                Console.WriteLine("Transcode:\tsuccessful");
            }
        }

        public static void HandleError(int errorCode)
        {
            var message = errorCode switch
            {
                -2 => "FFMPEGInitializationError",
                -3 => "CodecNotFound",
                -4 => "TooManyBytesWritten",
                -5 => "BufferTooSmall",
                -6 => "WriteError",
                -7 => "SerializationError",
                -8 => "ArgumentConversionToStringFailed",
                -9 => "PathArgumentMustExist",
                -10 => "OutputFolderMustExist",
                -11 => "OpenInputPathFailed",
                -12 => "OpenOutputPathFailed",
                -13 => "ArgumentConversionToArrayFailed",
                -14 => "UnknownIOError",
                _ => "UnknownErrorCode"
            };

            Console.WriteLine("Error:\t{0}", message);
        }

        public static ReadOnlySpan<byte> GetJson(Func<byte[], int> func)
        {
            var data = new byte[5000];
            var result = func(data);

            Console.WriteLine("Length: {0}", result);
            if (result < 0)
            {
                HandleError(result);
                return ReadOnlySpan<byte>.Empty;
            }
            else
            {
                var jsonData = new ReadOnlySpan<byte>(data, 0, result);
                var json = UTF8Encoding.UTF8.GetString(jsonData);
                Console.WriteLine("Data: {0}", json);

                return jsonData;
            }
        }
    }
}
