using EmergeHome.Code.TempUtils;
using UnityEngine;

namespace Emerge.Chess
{
    [CreateAssetMenu(fileName = "NewChessBoardSettings", menuName = "ChessPrototype/NewChessBoardSettings")]
    public class ChessBoardSettingsSO : ScriptableObject
    {
        [Header("Rule Settings")]
        [SerializeField] private bool allowSameColorCapture = true;
        public bool AllowSameColorCapture => allowSameColorCapture;

        [Header("Interaction Settings")]
        [SerializeField] private LayerMask boardAndInteractableLayers;
        public LayerMask BoardAndInteractableLayers => boardAndInteractableLayers;
        [SerializeField] private float rayCastMaxDistanceToBoard = 5.0f;
        public float RayCastMaxDistanceToBoard => rayCastMaxDistanceToBoard;

        [Header("Grab / Release / Rollover FX Settings")]
        [SerializeField] private EventAudioTypeEnum pickAudio = EventAudioTypeEnum.Thwap1;
        public EventAudioTypeEnum PickAudio => pickAudio;
        [SerializeField] private float pickAudioVolume = 0.5f;
        public float PickAudioVolume => pickAudioVolume;
        [SerializeField] private EventAudioTypeEnum releaseAudio = EventAudioTypeEnum.Whoosh1;
        public EventAudioTypeEnum ReleaseAudio => releaseAudio;
        [SerializeField] private float releaseAudioVolume = 0.4f;
        public float ReleaseAudioVolume => releaseAudioVolume;
        [SerializeField] private EventAudioTypeEnum rolloverAudio = EventAudioTypeEnum.WoodTap1;
        public EventAudioTypeEnum RolloverAudio => rolloverAudio;
        [SerializeField] private float rolloverAudioVolume = 0.5f;
        public float RolloverAudioVolume => rolloverAudioVolume;
        [SerializeField] private float rolloverTactilePulseDuration = 0.05f;
        public float RolloverTactilePulseDuration => rolloverTactilePulseDuration;

        public float RolloverDelayTime { get; set; } = 0f;
    }
}
