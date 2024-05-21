using Normal.Realtime;

namespace Seating
{
    [RealtimeModel]
    public partial class SeatModel
    {
        public const int INVALID_CLIENT_ID = -1;

        [RealtimeProperty(1, true, false)]
        private int _clientId = INVALID_CLIENT_ID;
    }
}