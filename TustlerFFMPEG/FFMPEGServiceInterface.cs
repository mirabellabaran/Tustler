using CloudWeaver.Foundation.Types;
using System;
using System.Collections.Generic;
using System.Text;
using TustlerFFMPEG.Types.CodecInfo;
using TustlerFFMPEG.Types.MediaInfo;
//using TustlerInterfaces;
//using TustlerServicesLib;

namespace TustlerFFMPEG
{
    public interface IAVServiceInterface
    {
        public abstract AVInteropResult<CodecPair> GetCodecInfo(string codecName);
        public abstract AVInteropResult<MediaInfo> GetMediaInfo(string inputFilePath);
        public abstract AVInteropResult<bool> Transcode(string inputFilePath, string outputFilePath);
    }

    public class FFMPEGServiceInterface
    {
        private  IRuntimeOptions options;

        /// <summary>
        /// For serialization
        /// </summary>
        public FFMPEGServiceInterface()
        {
        }

        public FFMPEGServiceInterface(IRuntimeOptions options)
        {
            this.RuntimeOptions = options;
        }

        public IRuntimeOptions RuntimeOptions
        {
            get
            {
                return options;
            }
            set
            {
                options = value;
                Reinitialize();
            }
        }

        public void Reinitialize()
        {
            if (options.IsMocked)
            {
                EnableMocking(options);
            }
            else
            {
                DisableMocking();
            }
        }

        public IAVServiceInterface Interop
        {
            get;
            internal set;
        }

        private void EnableMocking(IRuntimeOptions options)
        {
            Interop = new MockFFAVInterop();
        }

        private void DisableMocking()
        {
            Interop = new FFAVInterop();
        }
    }
}
