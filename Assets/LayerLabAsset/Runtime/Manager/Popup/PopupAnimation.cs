#if DOTWEEN_EXISTS
using DG.Tweening;
#endif
using System;
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

#if DOTWEEN_EXISTS
                    rect.DOAnchorPos(_targetPos, 0f).SetUpdate(true);
#else
                    rect.anchoredPosition = _targetPos;
#endif
                }
            }
        }

        public void OpenAnimation()
        {
#if DOTWEEN_EXISTS
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
#else
            if (isScale && rect)
            {
                if (rectCloseButton) rectCloseButton.localScale = Vector3.one;
                rect.localScale = Vector3.one;
            }

            if (isFade && _canvasGroup)
            {
                _canvasGroup.alpha = 1f;
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

                rect.anchoredPosition = _targetPos;
            }
#endif
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

#if DOTWEEN_EXISTS
            if (rectCloseButton)
            {
                rectCloseButton.DOScale(0f, 0.2f).SetEase(Ease.OutCubic).SetUpdate(true);
            }

            rect.DOKill();
            rect.DOAnchorPos(_targetPos, 0.2f).SetEase(Ease.OutCubic).SetUpdate(true).OnComplete(() => { action?.Invoke(); }).SetUpdate(true);
#else
            if (rectCloseButton)
            {
                rectCloseButton.localScale = Vector3.zero;
            }

            rect.anchoredPosition = _targetPos;
            action?.Invoke();
#endif
        }

        private void OnDisable()
        {
#if DOTWEEN_EXISTS
            if (rectCloseButton) rectCloseButton.DOKill();
            if (_canvasGroup) _canvasGroup.DOKill();
            if (rect) rect.DOKill();
#endif
        }
    }
}
