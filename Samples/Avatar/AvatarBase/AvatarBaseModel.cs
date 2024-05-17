using System;
using Normal.Realtime;
using Normal.Realtime.Serialization;

namespace Emerge.Connect.Avatar.Models
{
    [RealtimeModel]
    public partial class AvatarBaseModel
    {
        public const string INVALID_USER_ID = "";
        [RealtimeProperty(1, true, true)]
        private string _userId = INVALID_USER_ID;
    
        [RealtimeProperty(2, false, true)]
        private byte[] _avatarData = Array.Empty<byte>();
    }
}


