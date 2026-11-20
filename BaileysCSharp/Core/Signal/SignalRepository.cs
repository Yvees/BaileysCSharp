using Google.Protobuf;
using Proto;
using BaileysCSharp.Core.Models.SenderKeys;
using BaileysCSharp.Core.Models.Sessions;
using BaileysCSharp.Core.NoSQL;
using BaileysCSharp.Core.Stores;
using BaileysCSharp.LibSignal;
using static BaileysCSharp.Core.Utils.JidUtils;
using BaileysCSharp.Core.Types;

namespace BaileysCSharp.Core.Signal
{
    public class SignalRepository : IDisposable
    {
        public void Dispose() {}
        public SignalStorage Storage { get; set; }
        public SignalRepository(AuthenticationState auth)
        {
            Auth = auth;
            Storage = new SignalStorage(Auth);
            LidMapping = new LidMappingStore(Auth.Keys);
        }
        public AuthenticationState Auth { get; }
        public LidMappingStore LidMapping { get; }

        public byte[] DecryptGroupMessage(string group, string authorJid, byte[] content)
        {
            var senderName = JidToSignalSenderKeyName(group, authorJid);
            var session = new GroupCipher(Storage, senderName);
            return session.Decrypt(content);
        }

        /// <summary>
        /// The jid whose Signal session serves this device. As in Baileys v7, a phone number jid
        /// whose LID is known uses the LID session, so a device has one session however it is
        /// addressed. Two sessions for one device make it fail to decrypt whatever was sent with
        /// the session it does not hold ("waiting for this message").
        /// </summary>
        public string SignalJid(string jid)
        {
            return IsJidUser(jid) ? LidMapping.GetLidForPn(jid) ?? jid : jid;
        }

        public bool HasSession(string jid)
        {
            var address = new ProtocolAddress(SignalJid(jid));
            return Auth.Keys.Get<SessionRecord>(address.ToString())?.GetOpenSession() != null;
        }

        public void DeleteSession(string jid)
        {
            var address = new ProtocolAddress(SignalJid(jid));
            Auth.Keys.Set<SessionRecord>(address.ToString(), null);
        }

        public void RememberDevices(string user, IEnumerable<uint> devices)
        {
            var list = new DeviceList();
            list.AddRange(devices.Distinct().OrderBy(x => x));
            Auth.Keys.Set(user, list);
        }

        /// <summary>
        /// Moves the sessions of every known device of the phone number user to the LID
        /// (Baileys v7 migrateSession) and deletes the phone number sessions. Unlike v7, a LID
        /// session that already exists is kept: it is the one the device has been using with us,
        /// and replacing it with the phone number session would break decryption on that device.
        /// Returns how many sessions were moved.
        /// </summary>
        public int MigrateSession(string pnJid, string lidJid)
        {
            var pn = JidDecode(pnJid);
            var lidUser = JidUser(lidJid);
            if (pn == null || !IsJidUser(pnJid) || !IsLidUser(lidJid) || string.IsNullOrWhiteSpace(lidUser))
            {
                return 0;
            }

            var devices = new HashSet<uint>() { pn.Device ?? 0 };
            devices.UnionWith(Auth.Keys.Get<DeviceList>(pn.User) ?? []);
            devices.UnionWith(Auth.Keys.Get<DeviceList>(lidUser) ?? []);

            var moved = 0;
            foreach (var device in devices)
            {
                var pnAddress = new ProtocolAddress() { Name = pn.User, DeviceID = device }.ToString();
                var pnSession = Auth.Keys.Get<SessionRecord>(pnAddress);
                if (pnSession == null)
                {
                    continue;
                }

                var lidAddress = new ProtocolAddress() { Name = lidUser, DeviceID = device }.ToString();
                if (Auth.Keys.Get<SessionRecord>(lidAddress) == null && pnSession.GetOpenSession() != null)
                {
                    Auth.Keys.Set(lidAddress, pnSession);
                    moved++;
                }

                Auth.Keys.Set<SessionRecord>(pnAddress, null);
            }

            return moved;
        }

        public CipherMessage EncryptMessage(string jid, byte[] data)
        {
            var address = new ProtocolAddress(SignalJid(jid));
            var cipher = new SessionCipher(Storage, address);

            var enc = cipher.Encrypt(data);
            return new CipherMessage(enc.Type == 3 ? "pkmsg" : "msg", enc.Data);
        }

        public byte[] DecryptMessage(string user, string type, byte[] ciphertext)
        {
            var addr = new ProtocolAddress(SignalJid(user));
            var session = new SessionCipher(Storage, addr);
            byte[] result;
            if (type == "pkmsg")
            {
                result = session.DecryptPreKeyWhisperMessage(ciphertext);
            }
            else
            {
                result = session.DecryptWhisperMessage(ciphertext);
            }
            return result;
        }

        public void ProcessSenderKeyDistributionMessage(string author, Message.Types.SenderKeyDistributionMessage senderKeyDistributionMessage)
        {
            var builder = new GroupSessionBuilder(Storage);
            var senderName = JidToSignalSenderKeyName(senderKeyDistributionMessage.GroupId, author);
            var senderMsg = Proto.SenderKeyDistributionMessage.Parser.ParseFrom(senderKeyDistributionMessage.AxolotlSenderKeyDistributionMessage.ToByteArray().Skip(1).ToArray());
            Auth.Keys.Set(senderName, new SenderKeyRecord());
            builder.Process(senderName, senderMsg);
        }

        internal void InjectE2ESession(string jid, E2ESession session)
        {
            var addr = new ProtocolAddress(SignalJid(jid));
            var sessionBuilder = new SessionBuilder(Storage, addr);
            sessionBuilder.InitOutGoing(session);
        }

        public GroupCipherMessage EncryptGroupMessage(string group, string meId, byte[] bytes)
        {
            var senderName = JidToSignalSenderKeyName(group, meId);
            var builder = new GroupSessionBuilder(Storage);

            var senderKey = Auth.Keys.Get<SenderKeyRecord>(senderName);
            if (senderKey == null)
            {
                Auth.Keys.Set(senderName, new SenderKeyRecord());
            }

            var senderKeyDistributionMessage = builder.Create(senderName);

            var session = new GroupCipher(Storage, senderName);
            var ciphertext = session.Encrypt(bytes);


            return new GroupCipherMessage()
            {
                CipherText = ciphertext,
                SenderKeyDistributionMessage = new byte[] { 51 }.Concat(senderKeyDistributionMessage.ToByteArray()).ToArray()
            };
        }
    }

    public class GroupCipherMessage
    {
        public byte[] CipherText { get; set; }
        public byte[] SenderKeyDistributionMessage { get; set; }
    }
}
