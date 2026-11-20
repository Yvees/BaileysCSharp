using BaileysCSharp.Core.NoSQL;

namespace BaileysCSharp.Core.Signal
{
    /// <summary>
    /// The device ids last seen for a user (Baileys v7 "device-list"), so that the sessions of
    /// every device can be moved when the user's phone number turns out to have a LID.
    /// </summary>
    [FolderPrefix("device-list")]
    public class DeviceList : List<uint>
    {
    }
}
