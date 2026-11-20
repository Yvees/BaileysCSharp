using BaileysCSharp.Core.Helper;
using System.Text.Json;

namespace BaileysCSharp.Core.Signal
{
    public class JidWidhDevice
    {
        public string User { get; set; }
        public uint? Device { get; set; }
        /// <summary>"lid" or "s.whatsapp.net": how the user was addressed when the device was found.</summary>
        public string? Server { get; set; }


        public override string ToString()
        {
            return JsonSerializer.Serialize(this, JsonHelper.Options);
        }
    }
}
