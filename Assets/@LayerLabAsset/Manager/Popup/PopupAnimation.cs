using System;
using DG.Tweening;
using UnityEngine;

namespace LayerLabAsset
{
    public class PopupAnimation : MonoBehaviour
    {
        [SerializeField] private RectTransform rect;
        [SerializeField] private RectTransform rectCloseButton;

        [SerializeField] private bool isScale;
        [SerializeField] private bool isFade;

        [SerializeField] private PanelMoveType panelMoveType = PanelMoveType.None;

        private Vector2 _targetPos;
        private CanvasGroup _canvasGroup;


        public void Init()
        {

            _canvasGroup = gameObject.TryGetComponent<CanvasGroup>(out var cg) ? cg : gameObject.AddComponent<CanvasGroup>();

            if (isFade)
            {
                _canvasGroup.alpha = 0f;
            }

            if (rect != null)
            {
                if (isScale)
                {
                    rect.localScale = Vector3.one * 0.8f;
                    if (rectCloseButton) rectCloseButton.localScale = Vector3.zero;
                }

                if (panelMoveType != PanelMoveType.None)
                {
                    _targetPos = rect.anchoredPosition;
                    switch (panelMoveType)
                    {
                        case PanelMoveType.TopToBot:
                            _targetPos.y = rect.sizeDelta.y;
                            break;
                        case PanelMoveType.BotToTop:
                            _targetPos.y = -Screen.height; //-rect.sizeDelta.y;
                            break;
                        case PanelMoveType.LeftToRight:
                            _targetPos.x = -rect.sizeDelta.x;
                            break;
                        case PanelMoveType.RightToLeft:
                            _targetPos.x = rect.sizeDelta.x;
                            break;
                    }

                    rect.DOAnchorPos(_targetPos, 0f).SetUpdate(true);
                }
            }
        }

        public void OpenAnimation()
        {
            if (isScale && rect)
            {
                if (rectCloseButton) rectCloseButton.DOScale(1f, 0.2f).SetEase(Ease.OutBack).SetUpdate(true).SetDelay(0.1f);
                rect.DOScale(1f, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
            }

            if (isFade && _canvasGroup)
            {
                _canvasGroup.DOKill();
                _canvasGroup.DOFade(1f, 0.2f).SetEase(Ease.OutCubic).SetUpdate(true);
            }

            if (panelMoveType != PanelMoveType.None)
            {
                switch (panelMoveType)
                {
                    case PanelMoveType.TopToBot:
                    case PanelMoveType.BotToTop:
                        _targetPos.y = 0;
                        break;
                    case PanelMoveType.LeftToRight:
                    case PanelMoveType.RightToLeft:
                        _targetPos.x = 0;
                        break;
                }
          
                rect.DOAnchorPos(_targetPos, 0.2f).SetEase(Ease.OutCubic).SetUpdate(true);
            }
        }

        public void CloseAnimation(Action action)
        {


            if (panelMoveType == PanelMoveType.None)
            {
                action?.Invoke();
                return;
            }

            _targetPos = rect.anchoredPosition;
            switch (panelMoveType)
            {
                case PanelMoveType.TopToBot:
                    _targetPos.y = rect.sizeDelta.y;
                    break;
                case PanelMoveType.BotToTop:
                    _targetPos.y = -Screen.height;
                    break;
                case PanelMoveType.LeftToRight:
                    _targetPos.x = -rect.sizeDelta.x;
                    break;
                case PanelMoveType.RightToLeft:
                    _targetPos.x = rect.sizeDelta.x;
                    break;
            }

            if (rectCloseButton)
            {
                rectCloseButton.DOScale(0f, 0.2f).SetEase(Ease.OutCubic).SetUpdate(true);
            }

            rect.DOKill();
            rect.DOAnchorPos(_targetPos, 0.2f).SetEase(Ease.OutCubic).SetUpdate(true).OnComplete(() => { action?.Invoke(); }).SetUpdate(true);
        }

        private void OnDisable()
        {
            rectCloseButton.DOKill();
            _canvasGroup.DOKill();
            rect.DOKill();
        }
    }
}
