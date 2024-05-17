using UnityEngine;
using DG.Tweening;
namespace Emerge.Chess
{
    public class ChessBoardButtonsUI : MonoBehaviour
    {
        [SerializeField]
        private ChessBoard chessBoard;

        [SerializeField]
        private RectTransform chessBoardButtonPanel;
        [SerializeField]
        private RectTransform arrowImage;
        public bool isButtonPanelOn { set; get; } = false;
        private void Start()
        {
            arrowImage.DOLocalMoveX(arrowImage.localPosition.x + 4f, 1f).SetLoops(-1, LoopType.Yoyo);
        }
        [ContextMenu("Clear Board")]
        public void ClearBoard()
        {
            chessBoard.Despawn();
        }

        [ContextMenu("Reset Board")]
        public void ResetBoard()
        {
            chessBoard.ResetBoard();
        }
        public void ShowHideButtonPanel()
        {
            isButtonPanelOn = !isButtonPanelOn;
            if (isButtonPanelOn)
            { 
                ShowButtonPanel();
            }
            else
            {  
                HideButtonPanel();
            }
        }
        public void ShowButtonPanel()
        {
            chessBoardButtonPanel.gameObject.SetActive(true);
            chessBoardButtonPanel.DOScaleX(1, 0.25f).SetDelay(0.1f).OnComplete(() => { arrowImage.DOScaleX(-1, 0.25f)/*.OnComplete(()=>
            { arrowImage.DOLocalMoveX(arrowImage.position.x + 0.2f, 0.8f).SetLoops(-1, LoopType.Yoyo); })*/; });        
        }
        public void HideButtonPanel()
        {
            chessBoardButtonPanel.DOScaleX(0, 0.25f).SetDelay(0.1f).OnComplete(()=>
            { chessBoardButtonPanel.gameObject.SetActive(false); arrowImage.DOScaleX(1, 0.25f);});
        }
    }
}
