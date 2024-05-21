using System;
using Normal.Realtime;
using Normal.Realtime.Serialization;

namespace Avatar.ReadyPlayerMe.Models
{
    [RealtimeModel]
    public partial class ReadyPlayerMeAvatarModel
    {
        public const string INVALID_RPM_USER_ID = "";
        [RealtimeProperty(1, true, true)]
        private string _rpmUserId = INVALID_RPM_USER_ID;
        
        [RealtimeProperty(2, false, true)]
        private byte[] _avatarData = Array.Empty<byte>();
    }
}


