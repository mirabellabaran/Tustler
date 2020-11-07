using System;
using System.Collections.Generic;
using System.Text;
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
}
