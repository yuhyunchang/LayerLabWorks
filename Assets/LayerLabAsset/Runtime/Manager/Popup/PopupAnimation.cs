using System;
using System.Collections;
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

        private Coroutine _scaleCoroutine;
        private Coroutine _fadeCoroutine;
        private Coroutine _moveCoroutine;
        private Coroutine _closeButtonCoroutine;


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
                            _targetPos.y = -Screen.height;
                            break;
                        case PanelMoveType.LeftToRight:
                            _targetPos.x = -rect.sizeDelta.x;
                            break;
                        case PanelMoveType.RightToLeft:
                            _targetPos.x = rect.sizeDelta.x;
                            break;
                    }

                    rect.anchoredPosition = _targetPos;
                }
            }
        }

        public void OpenAnimation()
        {
            if (isScale && rect)
            {
                if (rectCloseButton)
                {
                    StopCoroutineSafe(ref _closeButtonCoroutine);
                    _closeButtonCoroutine = StartCoroutine(ScaleTo(rectCloseButton, 1f, 0.2f, EaseType.OutBack, 0.1f));
                }
                StopCoroutineSafe(ref _scaleCoroutine);
                _scaleCoroutine = StartCoroutine(ScaleTo(rect, 1f, 0.2f, EaseType.OutBack));
            }

            if (isFade && _canvasGroup)
            {
                StopCoroutineSafe(ref _fadeCoroutine);
                _fadeCoroutine = StartCoroutine(FadeTo(1f, 0.2f));
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

                StopCoroutineSafe(ref _moveCoroutine);
                _moveCoroutine = StartCoroutine(MoveTo(_targetPos, 0.2f));
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
                StopCoroutineSafe(ref _closeButtonCoroutine);
                _closeButtonCoroutine = StartCoroutine(ScaleTo(rectCloseButton, 0f, 0.2f, EaseType.OutCubic));
            }

            StopCoroutineSafe(ref _moveCoroutine);
            _moveCoroutine = StartCoroutine(MoveTo(_targetPos, 0.2f, action));
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            _scaleCoroutine = null;
            _fadeCoroutine = null;
            _moveCoroutine = null;
            _closeButtonCoroutine = null;
        }

        private void StopCoroutineSafe(ref Coroutine coroutine)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                coroutine = null;
            }
        }

        private enum EaseType { OutCubic, OutBack }

        private IEnumerator ScaleTo(Transform target, float targetScale, float duration, EaseType easeType, float delay = 0f)
        {
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            Vector3 startScale = target.localScale;
            Vector3 endScale = Vector3.one * targetScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = easeType == EaseType.OutBack ? EaseOutBack(t) : EaseOutCubic(t);
                target.localScale = Vector3.LerpUnclamped(startScale, endScale, easedT);
                yield return null;
            }

            target.localScale = endScale;
        }

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            float startAlpha = _canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, EaseOutCubic(t));
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
        }

        private IEnumerator MoveTo(Vector2 targetPos, float duration, Action onComplete = null)
        {
            Vector2 startPos = rect.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, EaseOutCubic(t));
                yield return null;
            }

            rect.anchoredPosition = targetPos;
            onComplete?.Invoke();
        }

        private static float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
