using BaileysCSharp.Core.NoSQL;

namespace BaileysCSharp.Core.Signal
{
    /// <summary>
    /// One direction of a LID / phone number pair, as Baileys v7 keeps it: stored under the
    /// phone number user it holds the LID user, stored under "&lt;lidUser&gt;_reverse" the phone number user.
    /// </summary>
    [FolderPrefix("lid-mapping")]
    public class LidMappingEntry
    {
        public string User { get; set; } = "";
    }
}
