using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LayerLabAsset
{
    public enum ButtonAnimation
    {
        None,
        Scale
    }

    public class UIButton : Selectable, IPointerClickHandler, IPointerExitHandler, IPointerEnterHandler
    {
        [field: SerializeField] public bool IsScaleAnim { get; set; } = true;

        [field: SerializeField] public bool IsCustomCenterPosition { get; set; }
        public RectTransform customCenterPoint;

        //ScaleValue
        private float _scaleValue;

        //UnityEvent
        public UnityEvent onClick = new();
        public UnityEvent onDown = new();
        public UnityEvent onUp = new();
        public UnityEvent onEnter = new();
        public UnityEvent onExit = new();

        private Coroutine _scaleCoroutine;
        private bool _isScaleReset;

        protected override void Start()
        {
            SetButton();
            base.Start();
        }

        private void SetButton()
        {
            SetButtonNavigation();
            SetButtonScalePower();
        }

        private void SetButtonNavigation()
        {
        }

        public void SetButtonScalePower()
        {
            if (IsScaleAnim)
            {
                _scaleValue = 1.03f;
            }
            else
            {
                _scaleValue = 1f;
            }
        }


        #region 버튼 이벤트

        /// <summary>
        /// OnDown
        /// </summary>
        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            if (!interactable)
                return;

            _isScaleReset = false;

            if (IsScaleAnim)
            {
                StopScaleCoroutine();
                _scaleCoroutine = StartCoroutine(ScaleTo(_scaleValue, 0.1f, EaseType.OutCubic));
            }

            onDown?.Invoke();
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);

            if (IsScaleAnim && !_isScaleReset)
            {
                _isScaleReset = true;
                StopScaleCoroutine();
                _scaleCoroutine = StartCoroutine(ScaleTo(1f, 0.5f, EaseType.OutElastic));
            }

            onUp?.Invoke();
        }


        /// <summary>
        /// OnClick
        /// </summary>
        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (!interactable)
                return;

            onClick?.Invoke();
        }

        #endregion


        public override void OnPointerEnter(PointerEventData eventData)
        {
            if (!interactable)
                return;

            onEnter?.Invoke();
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            if (!interactable)
                return;

            if (IsScaleAnim && !_isScaleReset)
            {
                _isScaleReset = true;
                StopScaleCoroutine();
                _scaleCoroutine = StartCoroutine(ScaleTo(1f, 0.5f, EaseType.OutElastic));
            }

            onExit?.Invoke();
        }


        protected override void OnDestroy()
        {
            base.OnDestroy();
            onClick.RemoveAllListeners();
            StopScaleCoroutine();
        }

        private void StopScaleCoroutine()
        {
            if (_scaleCoroutine != null)
            {
                StopCoroutine(_scaleCoroutine);
                _scaleCoroutine = null;
            }
        }

        private enum EaseType { OutCubic, OutElastic }

        private IEnumerator ScaleTo(float targetScale, float duration, EaseType easeType)
        {
            Vector3 startScale = transform.localScale;
            Vector3 endScale = Vector3.one * targetScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = easeType == EaseType.OutElastic ? EaseOutElastic(t) : EaseOutCubic(t);
                transform.localScale = Vector3.LerpUnclamped(startScale, endScale, easedT);
                yield return null;
            }

            transform.localScale = endScale;
            _scaleCoroutine = null;
        }

        private static float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        private static float EaseOutElastic(float t)
        {
            if (t == 0f) return 0f;
            if (t == 1f) return 1f;
            float p = 0.3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - p / 4f) * (2f * Mathf.PI) / p) + 1f;
        }
    }
}
