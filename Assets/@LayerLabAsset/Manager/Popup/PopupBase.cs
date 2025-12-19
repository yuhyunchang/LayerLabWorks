using System;
using UnityEngine;
using UnityEngine.Events;

namespace LayerLabAsset
{
    public enum PanelMoveType
    {
        None,
        LeftToRight,
        RightToLeft,
        TopToBot,
        BotToTop
    }

    public class PopupBase : MonoBehaviour
    {
        protected bool IsShow { get; set; }
        public PopupType PopupType { get; set; }
        [field: SerializeField] public PopupSameType PopupSameType { get; set; }
        
        public bool isMoveToClose;
        [field: SerializeField] public bool IsFrontResourceBar { get; set; }
        private IDisposable Disposable { get; set; }
        private Canvas PopupBaseCanvas { get; set; }
        private PopupAnimation PopupAnimation { get; set; }

        public UnityAction OnCloseEvent { get; set; }
        
        protected virtual void Awake()
        {
            Init();
        }
        
        public void SetCanvasOrder(int currentOrder)
        {
            PopupBaseCanvas.sortingOrder = currentOrder + 1;
        }
        

        /// <summary>
        /// 초기화
        /// </summary>
        protected virtual void Init()
        {
            PopupBaseCanvas = GetComponent<Canvas>();
            TryGetComponent(out PopupAnimation panelAnimation);
            if (panelAnimation == null)
            {
                return;
            }

            PopupAnimation = panelAnimation;
            PopupAnimation.Init();
        }

        /// <summary>
        /// 데이터 세팅이 끝나면 패널을 UI에 표시한다.
        /// </summary>
        public virtual void Show()
        {
            if(IsShow) return;
            IsShow = true;
            
            if (IsFrontResourceBar)
            {
                // ResourceBar.Instance.SetOrderFront();
            }
     
            if (PopupAnimation) PopupAnimation.OpenAnimation();
        }


        /// <summary>
        /// 패널 닫기
        /// </summary>
        public virtual void Close()
        {
            IsShow = false;
            Disposable?.Dispose();
            OnCloseEvent?.Invoke();
            
            if (PopupAnimation)
            {
                PopupAnimation.CloseAnimation(() =>
                {
                    PopupManager.Instance.RemovePopup(this);
                    Destroy(gameObject);
                });
            }
            else
            {
                PopupManager.Instance.RemovePopup(this);
                Destroy(gameObject);
            }
        }
    }
}