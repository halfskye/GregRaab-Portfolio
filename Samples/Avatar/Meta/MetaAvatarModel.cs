using System;
using Normal.Realtime;

namespace Avatar.Meta.Models
{
    [RealtimeModel]
    public partial class MetaAvatarModel
    {
        public const string INVALID_META_USER_ID = "";
        [RealtimeProperty(1, true, true)]
        private string _metaUserId = INVALID_META_USER_ID;

        [RealtimeProperty(2, false, true)]
        private byte[] _avatarData = Array.Empty<byte>();
    }
}