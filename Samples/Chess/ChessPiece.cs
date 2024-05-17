using Emerge.SDK.Core.Tracking;
using EmergeHome.Code.TempUtils;
using EmergeHome.TempUtils;
using UnityEngine;

namespace Emerge.Chess
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ChessPiece : MonoBehaviour
    {
        [SerializeField] private ChessBoard.PlayerColors playerColor = ChessBoard.PlayerColors.Black;
        public ChessBoard.PlayerColors PlayerColor => playerColor;
        public bool IsBlack => playerColor == ChessBoard.PlayerColors.Black;
        public bool IsWhite => playerColor == ChessBoard.PlayerColors.White;
        private bool IsFriendly(ChessPiece other) => other.playerColor == playerColor;

        [SerializeField] private ChessPieceSO chessPieceSO;
        public ChessPieceSO.PieceData PieceData => chessPieceSO.GetPieceDataByPlayerColor(playerColor);
        public ChessBoard.PiecesEnum PieceType => chessPieceSO.PieceType;

        public ChessPieceSO.BoardPosition InitialPosition { private set; get; }

        private const int INVALID_INDEX = -1;
        public int RelativeIndex { get; private set; } = INVALID_INDEX;

        private const int INVALID_ID = -1;
        public int ID { get; private set; } = INVALID_ID;

        private GrabbableObject GrabbableObject { get; set; } = null;

        private bool IsActivelyHeld { get; set; } = false;

        // Release info
        [SerializeField] public MeshFilter releaseDestinationVisual;
        private Vector3 _lastVisualValidPosition;
        private Vector3 _lastValidSquareOnBoard;

        private ChessBoard ChessBoard { get; set; } = null;
        private ChessBoardSettingsSO ChessBoardSettings => ChessBoard.Settings;

        public void Initialize(ChessBoard chessBoard, ChessPieceSO.BoardPosition initialPosition, int id)
        {
            ChessBoard = chessBoard;
            InitialPosition = initialPosition;
            RelativeIndex = GetRelativeIndex();
            ID = id;
        }

        private int GetRelativeIndex()
        {
            if (RelativeIndex == INVALID_INDEX)
            {
                var row = InitialPosition.Row;
                var adjustedRow = InitialPosition.Row <= ChessPieceSO.START_ROW_FAR
                    ? row - 1
                    : row - ChessPieceSO.END_ROW_NEAR + 2;
                RelativeIndex = InitialPosition.ColumnInt + adjustedRow * ChessPieceSO.GRID_SIZE;
            }

            return RelativeIndex;
        }

        private void MoveToPosition(Vector3 position)
        {
            var player = ChessPlayerManager.Instance.GetLocalPlayerByBoard(ChessBoard);
            player.SetChessPiecePosition(RelativeIndex, position);
        }

        private void LerpToPosition(Vector3 position)
        {
            var player = ChessPlayerManager.Instance.GetLocalPlayerByBoard(ChessBoard);
            player.LerpChessPieceToPosition(RelativeIndex, position);
        }

        private void Awake()
        {
            GrabbableObject = this.GetComponent<GrabbableObject>();

            releaseDestinationVisual.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (GrabbableObject)
            {
                GrabbableObject.OnGrabbed += HandleGrabbableGrabbed;
                GrabbableObject.OnReleased += HandleGrabbableReleased;
            }

            _lastValidSquareOnBoard = this.transform.position;
        }

        private void OnDisable()
        {
            if (GrabbableObject)
            {
                GrabbableObject.OnGrabbed -= HandleGrabbableGrabbed;
                GrabbableObject.OnReleased -= HandleGrabbableReleased;
            }
        }

        private void Update()
        {
            if (IsActivelyHeld)
            {
                MoveToPosition(this.transform.position);
            }
        }

        private void LateUpdate()
        {
            if (IsActivelyHeld)
            {
                UpdateReleaseDestination();
            }
        }

        #region Grab / Release Event Handlers

        private void HandleGrabbableGrabbed(object sender, GrabbableObject.GrabbableObjectEventArgs e)
        {
            _lastValidSquareOnBoard = FindReleaseDestination();

            IsActivelyHeld = true;
            releaseDestinationVisual.gameObject.SetActive(true);

            HandleGrabbedFX(e);
            ChessBoard.CloseBoardButtonPanels();
        }

        private void HandleGrabbableReleased(object sender, GrabbableObject.GrabbableObjectEventArgs e)
        {
            HandleReleaseCollisions();

            IsActivelyHeld = false;

            releaseDestinationVisual.gameObject.SetActive(false);

            HandleReleasedFX(e);
        }

        #endregion Grab / Release Event Handlers

        #region Grab / Release / Rollover FX

        private void HandleGrabbedFX(GrabbableObject.GrabbableObjectEventArgs e)
        {
            TactilePulseController.Instance.PulseTravelDistance = 0.1f;
            TactilePulseController.Instance.StartTactilePulseJointTowardsJoint(e.Chirality,
                AbstractHandPartIdentifier.HandPartType.IndexTip,
                AbstractHandPartIdentifier.HandPartType.MiddleJointMCP);
            ChessBoardSettings.RolloverDelayTime = Time.time + 0.35f;

            var fingertip = HandPartManager.Instance.GetHandPart(e.Chirality, AbstractHandPartIdentifier.HandPartType.IndexTip);
            EventAudioManager.Instance.PlayClipAtPoint(ChessBoardSettings.PickAudio, fingertip.position, ChessBoardSettings.PickAudioVolume);
        }

        private void HandleReleasedFX(GrabbableObject.GrabbableObjectEventArgs e)
        {
            TactilePulseController.Instance.PulseTravelDistance = 0.1f;
            TactilePulseController.Instance.StartTactilePulseJointTowardsJoint(e.Chirality,
                AbstractHandPartIdentifier.HandPartType.IndexJointMCP,
                AbstractHandPartIdentifier.HandPartType.IndexTip);

            var fingertip = HandPartManager.Instance.GetHandPart(e.Chirality, AbstractHandPartIdentifier.HandPartType.IndexTip);
            EventAudioManager.Instance.PlayClipAtPoint(ChessBoardSettings.ReleaseAudio, fingertip.position, ChessBoardSettings.ReleaseAudioVolume);
        }

        private void HandleRolloverFX()
        {
            TactilePulseController.Instance.StartStaticTactilePulseAtJoint(
                ObjectGrabManager.instance.ActiveGrabbableChirality,
                AbstractHandPartIdentifier.HandPartType.RingJointMCP, ChessBoardSettings.RolloverTactilePulseDuration);

            EventAudioManager.Instance.PlayClipOneShot(ChessBoardSettings.RolloverAudio, ChessBoardSettings.RolloverAudioVolume);
        }

        #endregion Grab / Release / Rollover FX

        #region Release Logic

        private void UpdateReleaseDestination()
        {
            Vector3 releasePoint = FindReleaseDestination();

            if (_lastVisualValidPosition != releasePoint && Time.time > ChessBoardSettings.RolloverDelayTime)
            {
                // It's a new square
                HandleRolloverFX();
            }

            _lastVisualValidPosition = releasePoint;
            releaseDestinationVisual.transform.position = releasePoint;
        }

        private Vector3 FindReleaseDestination()
        {
            return IsOverBoard(out Vector3 destination) ? destination : _lastValidSquareOnBoard;
        }

        private bool IsOverBoard(out Vector3 boardPosition)
        {
            Ray ray = new Ray(transform.position + new Vector3(0f, 2f, 0f), Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, ChessBoardSettings.RayCastMaxDistanceToBoard, ChessBoardSettings.BoardAndInteractableLayers))
            {
                var hitGameObject = hit.collider.gameObject;

                if (hitGameObject == ChessBoard.gameObject ||
                    hitGameObject == ChessBoard.GetGutterByColor(ChessBoard.PlayerColors.Black).gameObject ||
                    hitGameObject == ChessBoard.GetGutterByColor(ChessBoard.PlayerColors.White).gameObject ||
                    ChessBoard == hitGameObject.GetComponentInParent<ChessBoard>())
                {
                    boardPosition = hit.transform.position;
                    return true;
                }
            }

            boardPosition = Vector3.zero;
            return false;
        }

        private void HandleReleaseCollisions()
        {
            var destination = _lastVisualValidPosition;
            if (FindPossibleCollision(destination, out ChessPiece chessPieceAtDestination))
            {
                var isHandled = HandleCollision(chessPieceAtDestination);
                var revertDestination =
                    !isHandled ||
                    (!ChessBoardSettings.AllowSameColorCapture && IsFriendly(chessPieceAtDestination));

                if (revertDestination)
                {
                    destination = _lastValidSquareOnBoard;
                }
            }

            LerpToPosition(destination);

            _lastValidSquareOnBoard = destination;
        }

        private bool FindPossibleCollision(Vector3 position, out ChessPiece collidingChessPiece)
        {
            collidingChessPiece = null;
            const float distanceSqrForCollision = 0.0001f;
            foreach (var chessPiece in ChessBoard.ActiveChessPieces)
            {
                if (chessPiece != null && chessPiece != this)
                {
                    float distanceSqrFromPiece = (position - chessPiece.transform.position).sqrMagnitude;
                    if (distanceSqrFromPiece <= distanceSqrForCollision)
                    {
                        collidingChessPiece = chessPiece;

                        break;
                    }
                }
            }

            return collidingChessPiece != null;
        }

        private bool HandleCollision(ChessPiece collidingChessPiece)
        {
            var isHandled = false;

            if (ChessBoardSettings.AllowSameColorCapture ||
                !IsFriendly(collidingChessPiece))
            {
                // Handle sending piece to gutter:
                var gutter = ChessBoard.GetGutterByColor(playerColor);

                isHandled = (gutter.GetFirstAvailableSquarePosition(
                    (position => !FindPossibleCollision(position, out _)),
                    out Vector3 gutterPosition
                ));

                if (isHandled)
                {
                    collidingChessPiece.LerpToPosition(gutterPosition);
                }
            }

            return isHandled;
        }

        #endregion
    }
}
