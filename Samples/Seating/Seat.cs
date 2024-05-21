using Tracking;
using UnityEngine;

namespace Seating
{
    public class Seat : MonoBehaviour
    {
        [SerializeField] private Transform TactileHardwareAnchor = null;
        [SerializeField] private Transform SeatLocation = null;

        public Transform ActiveSeatLocation
        {
            get
            {
                if (SeatingManager.ShouldAnchorToTactileHardware)
                {
                    return !TactileHardwareAnchor.IsNullOrDestroyed() ? TactileHardwareAnchor : this.transform;
                }

                return !SeatLocation.IsNullOrDestroyed() ? SeatLocation : this.transform;
            }
        }
    }
}
