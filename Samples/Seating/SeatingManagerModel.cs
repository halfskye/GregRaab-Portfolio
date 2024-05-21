using Normal.Realtime;
using Normal.Realtime.Serialization;

namespace Seating
{
    [RealtimeModel]
    public partial class SeatingManagerModel
    {
        [RealtimeProperty(1, true, true)]
        private RealtimeDictionary<SeatModel> _primarySeatData;

        [RealtimeProperty(2, true, true)]
        private RealtimeDictionary<SeatModel> _spectatorSeatData;
    }
}