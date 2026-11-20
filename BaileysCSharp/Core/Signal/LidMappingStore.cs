using System.Collections.Concurrent;
using BaileysCSharp.Core.NoSQL;
using static BaileysCSharp.Core.Utils.JidUtils;

namespace BaileysCSharp.Core.Signal
{
    /// <summary>
    /// LID / phone number pairs (Baileys v7 lid-mapping.ts), kept in the key store so they
    /// survive restarts. Lookups keep the device: the phone number jid "pn:7" maps to "lid:7".
    /// </summary>
    public class LidMappingStore
    {
        private readonly BaseKeyStore keys;
        private readonly ConcurrentDictionary<string, string> lidByPnUser = new();
        private readonly ConcurrentDictionary<string, string> pnByLidUser = new();

        public LidMappingStore(BaseKeyStore keys)
        {
            this.keys = keys;
        }

        /// <summary>
        /// Stores the pair; the two jids may come in either order. Returns true when the pair
        /// was not known yet.
        /// </summary>
        public bool Store(string lid, string pn)
        {
            if (!IsLidUser(lid) && IsLidUser(pn))
            {
                (lid, pn) = (pn, lid);
            }

            var lidUser = JidUser(lid);
            var pnUser = JidUser(pn);
            if (!IsLidUser(lid) || !IsJidUser(pn) || string.IsNullOrWhiteSpace(lidUser) || string.IsNullOrWhiteSpace(pnUser))
            {
                return false;
            }

            if (LidUserFor(pnUser) == lidUser)
            {
                return false;
            }

            keys.Set(pnUser, new LidMappingEntry() { User = lidUser });
            keys.Set(ReverseKey(lidUser), new LidMappingEntry() { User = pnUser });
            lidByPnUser[pnUser] = lidUser;
            pnByLidUser[lidUser] = pnUser;
            return true;
        }

        /// <summary>"pn:7@s.whatsapp.net" → "lid:7@lid", or null when no LID is known.</summary>
        public string? GetLidForPn(string pn)
        {
            var decoded = JidDecode(pn);
            if (decoded == null || !IsJidUser(pn))
            {
                return null;
            }

            var lidUser = LidUserFor(decoded.User);
            return lidUser == null ? null : JidEncode(lidUser, "lid", decoded.Device);
        }

        /// <summary>"lid:7@lid" → "pn:7@s.whatsapp.net", or null when no phone number is known.</summary>
        public string? GetPnForLid(string lid)
        {
            var decoded = JidDecode(lid);
            if (decoded == null || !IsLidUser(lid))
            {
                return null;
            }

            var pnUser = PnUserFor(decoded.User);
            return pnUser == null ? null : JidEncode(pnUser, "s.whatsapp.net", decoded.Device);
        }

        private string? LidUserFor(string pnUser)
        {
            if (lidByPnUser.TryGetValue(pnUser, out var cached))
            {
                return cached;
            }

            var stored = keys.Get<LidMappingEntry>(pnUser)?.User;
            if (!string.IsNullOrWhiteSpace(stored))
            {
                lidByPnUser[pnUser] = stored;
                pnByLidUser[stored] = pnUser;
                return stored;
            }

            return null;
        }

        private string? PnUserFor(string lidUser)
        {
            if (pnByLidUser.TryGetValue(lidUser, out var cached))
            {
                return cached;
            }

            var stored = keys.Get<LidMappingEntry>(ReverseKey(lidUser))?.User;
            if (!string.IsNullOrWhiteSpace(stored))
            {
                pnByLidUser[lidUser] = stored;
                lidByPnUser[stored] = lidUser;
                return stored;
            }

            return null;
        }

        private static string ReverseKey(string lidUser) => $"{lidUser}_reverse";
    }
}
