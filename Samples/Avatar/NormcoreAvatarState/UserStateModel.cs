using Normal.Realtime;

namespace Avatar
{
    [RealtimeModel]
    public partial class UserStateModel
    {
        [RealtimeProperty(1, true, true)] private bool _isPlayerActive;
        [RealtimeProperty(2, true, true)] private string _avatarImageURL = null;
        [RealtimeProperty(3, true)] private string _avatarName = null;
        [RealtimeProperty(4, true)] private string _avatarUserID = null;
    }
}