using FFMpegCore;
using Proto;
using SkiaSharp;
using System.Diagnostics;
using BaileysCSharp.Core.Helper;
using BaileysCSharp.Core.Models.Sending.Interfaces;
using static Proto.Message.Types;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BaileysCSharp.Core.Models.Sending.Media
{
    public class AudioMessageContent : AnyMediaMessageContent
    {
        private MemoryStream audio;

        public AudioMessageContent()
        {
            Property = "AudioMessage";
        }

        public string Caption { get; set; }
        public byte[] JpegThumbnail { get; set; }
        public uint Seconds { get; set; }
        public ulong FileLength { get; set; }
        public string Mimetype { get; set; }
        /// <summary>A voice note (push to talk): Ogg Opus, shown with a waveform and played inline.</summary>
        public bool Ptt { get; set; }
        /// <summary>A voice note's waveform: up to 64 levels of 0..100.</summary>
        public byte[]? Waveform { get; set; }

        public Stream Audio
        {
            get => audio;

            set => OnLoadImage(value);
        }

        private void OnLoadImage(Stream value)
        {
            if (value is MemoryStream memoryStream)
            {
                audio = memoryStream;
            }
            else
            {
                audio = new MemoryStream();
                value.CopyTo(audio);
            }
            audio.Position = 0;
            FileLength = (ulong)audio.Length;
        }

        public override async Task Process()
        {
            byte[] copy = audio.ToArray();
            audio.Position = 0;
            var temp = Path.GetTempFileName();
            File.WriteAllBytes(temp, copy);
            var mediaInfo = await FFProbe.AnalyseAsync(temp);
            Seconds = (uint)mediaInfo.Duration.TotalSeconds;


            if (Ptt)
            {
                //TODO Ptt


            }
            File.Delete(temp);
        }

        public override IMediaMessage ToMediaMessage()
        {
            var image = new AudioMessage()
            {
                ContextInfo = ContextInfo,
                Mimetype = Mimetype ?? "audio/mp4",
                MediaKeyTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Seconds = Seconds,
                Ptt = Ptt
            };
            if (Waveform is { Length: > 0 })
            {
                image.Waveform = Google.Protobuf.ByteString.CopyFrom(Waveform);
            }
            return image;
        }
    }
}
