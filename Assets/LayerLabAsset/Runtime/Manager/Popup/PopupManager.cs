using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LayerLabAsset
{

    public enum PopupSameType
    {
        UI,
        System,
        Ignore,
        Queue
    }

    public enum PopupType
    {
    }

    public class PopupManager : Singleton<PopupManager>
    {
        private const string Path = "_UI/Popup/";

        public List<PopupBase> popupSystem { get; set; } = new();
        public List<PopupBase> popupUI { get; set; } = new();
        public List<PopupBase> popupIgnore { get; set; } = new();
        public List<PopupBase> popupQueue { get; set; } = new();

        public bool IsNotOpenPopup => popupSystem.Count == 0 && popupUI.Count == 0 && popupIgnore.Count == 0 && popupQueue.Count == 0;
        public bool IsNotOpenUiPopup => popupUI.Count == 0;

        /// <summary>
        /// 팝업 개수 변경 이벤트
        /// </summary>
        public event Action<int> OnPopupCountChanged;

        public void Init()
        {
        }

        private void NotifyPopupCountChanged()
        {
            int totalCount = popupSystem.Count + popupUI.Count + popupIgnore.Count + popupQueue.Count;
            OnPopupCountChanged?.Invoke(totalCount);
        }

        public PopupBase CreateOnlyLastPopup(PopupType popupType)
        {
            IsActivePopup(popupType, out PopupBase popup);
            if (popup != null) CloseByPopupType(popup.PopupType);
            return Create(popupType);
        }

        /// <summary>
        /// 팝업 생성
        /// 팝업을 생성하기위한 가장 기본적인 함수
        /// </summary>
        public PopupBase Create(PopupType popupType, bool isShow = true, bool isQueue = false)
        {
            PopupBase p = GetPopup(popupType);
            p.PopupType = popupType;
            CreatePopup(p, isShow, isQueue);
            return p;
        }


        /// <summary>
        /// 팝업오브젝트 생성 가져오기
        /// </summary>
        private PopupBase GetPopup(PopupType popupType)
        {
            PopupBase p = Instantiate(Resources.Load<GameObject>($"{Path}{popupType}")).GetComponent<PopupBase>();
            p.transform.SetParent(transform, false);
            p.transform.SetAsFirstSibling();
            return p;
        }


        private PopupBase CreatePopup(PopupBase popup, bool isShow, bool isQueue)
        {
            if (isShow)
            {
                if (isQueue)
                {
                    if (popupQueue.Count <= 0)
                    {
                        popup.Show();
                    }
                }
                else
                {
                    popup.Show();
                }
            }

            if (isQueue)
            {
                popupQueue.Add(popup);
            }
            else
            {
                switch (popup.PopupSameType)
                {
                    case PopupSameType.System:
                        popupSystem.Add(popup);
                        break;
                    case PopupSameType.UI:
                        popupUI.Add(popup);
                        break;
                    case PopupSameType.Ignore:
                        popupIgnore.Add(popup);
                        break;
                    case PopupSameType.Queue:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            NotifyPopupCountChanged();
            return popup;
        }


        private bool CheckAlreadyCreated(PopupType popupType)
        {
            return popupUI.Any(popupBase => popupBase.PopupType == popupType);
        }


        public void CheckClosePopup()
        {
            foreach (var t in popupUI)
            {
                if (t.isMoveToClose)
                {
                    t.Close();
                }
            }

            foreach (var t in popupSystem)
            {
                if (t.isMoveToClose)
                {
                    t.Close();
                }
            }
        }

        /// <summary>
        /// 지정한 타입의 모든패널을 삭제시킨다.
        /// </summary>
        public void CloseAll()
        {
            for (var i = popupUI.Count - 1; i >= 0; i--) RemoveList(PopupSameType.UI, popupUI[i]);
            for (var i = popupIgnore.Count - 1; i >= 0; i--) RemoveList(PopupSameType.Ignore, popupIgnore[i]);
            for (var i = popupSystem.Count - 1; i >= 0; i--) RemoveList(PopupSameType.System, popupSystem[i]);
            for (var i = popupQueue.Count - 1; i >= 0; i--) RemoveList(PopupSameType.Queue, popupQueue[i]);
        }


        /// <summary>
        /// 지정한 타입의 모든패널을 삭제시킨다.
        /// </summary>
        public void CloseAllByType(PopupSameType popupSameType)
        {
            switch (popupSameType)
            {
                case PopupSameType.UI:
                    for (var i = popupUI.Count - 1; i >= 0; i--) RemoveList(popupSameType, popupUI[i]);
                    break;
                case PopupSameType.System:
                    for (var i = popupSystem.Count - 1; i >= 0; i--) RemoveList(popupSameType, popupSystem[i]);
                    break;
                case PopupSameType.Queue:
                    for (var i = popupQueue.Count - 1; i >= 0; i--) RemoveList(popupSameType, popupQueue[i]);
                    break;
                case PopupSameType.Ignore:
                    for (var i = popupIgnore.Count - 1; i >= 0; i--) RemoveList(popupSameType, popupIgnore[i]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(popupSameType), popupSameType, null);
            }
        }


        private void RemoveList(PopupSameType popupSameType, PopupBase popup)
        {
            switch (popupSameType)
            {
                case PopupSameType.UI:
                    popupUI.Remove(popup);
                    break;
                case PopupSameType.System:
                    popupSystem.Remove(popup);
                    break;
                case PopupSameType.Queue:
                    popupQueue.Remove(popup);
                    // Queue에서 제거되면 다음 팝업 표시
                    if (popupQueue.Count > 0)
                    {
                        popupQueue[0].Show();
                    }
                    break;
                case PopupSameType.Ignore:
                    popupIgnore.Remove(popup);
                    break;
            }

            popup.Close();
            NotifyPopupCountChanged();
        }


        /// <summary>
        /// 동일한 패널타입이 리스트에 존재하는지 확인한다.
        /// </summary>
        public void GetActivePopup(PopupType popupType, out PopupBase popup)
        {
            popup = null;

            foreach (var t in popupUI)
            {
                if (t.PopupType == popupType)
                {
                    popup = t;
                }
            }

            foreach (var t in popupSystem)
            {
                if (t.PopupType == popupType)
                {
                    popup = t;
                }
            }
        }

        /// <summary>
        /// 동일한 패널타입이 리스트에 존재하는지 확인한다.
        /// </summary>
        public void IsActivePopup(PopupType popupType, out PopupBase popup)
        {
            foreach (var t in popupUI)
            {
                if (t.PopupType != popupType) continue;
                popup = t;
                return;
            }

            foreach (var t in popupSystem)
            {
                if (t.PopupType != popupType) continue;
                popup = t;
                return;
            }

            foreach (var t in popupIgnore)
            {
                if (t.PopupType != popupType) continue;
                popup = t;
                return;
            }

            popup = null;
        }

        /// <summary>
        /// 지정한 타입과 동일한 패널을 가져온다.
        /// </summary>
        public PopupBase CurrentPopupByType(PopupType popupType)
        {
            foreach (var t in popupUI)
            {
                if (t.PopupType == popupType) return t;
            }

            foreach (var t in popupSystem)
            {
                if (t.PopupType == popupType) return t;
            }

            return popupIgnore.FirstOrDefault(t => t.PopupType == popupType);
        }

        public void CloseLastPopup(out bool success)
        {
            if (popupSystem.Count > 0)
            {
                popupSystem[^1].Close();
                success = true;
                return;
            }

            if (popupUI.Count > 0)
            {
                popupUI[^1].Close();
                success = true;
                return;
            }

            success = false;
        }


        public void CloseByPopupType(PopupType popupType)
        {
            foreach (var t in popupUI)
            {
                if (t.PopupType == popupType)
                {
                    t.Close();
                    break;
                }
            }

            for (var i = 0; i < popupSystem.Count; i++)
            {
                if (popupSystem[i].PopupType == popupType)
                {
                    popupSystem[i].Close();
                    break;
                }
            }

            foreach (var t in popupIgnore)
            {
                if (t.PopupType == popupType)
                {
                    t.Close();
                    break;
                }
            }
        }

        public void RemovePopup(PopupBase popupBase)
        {
            switch (popupBase.PopupSameType)
            {
                case PopupSameType.UI:
                    popupUI.Remove(popupBase);
                    break;
                case PopupSameType.System:
                    popupSystem.Remove(popupBase);
                    break;
                case PopupSameType.Ignore:
                    popupIgnore.Remove(popupBase);
                    break;
                case PopupSameType.Queue:
                    popupQueue.Remove(popupBase);
                    // Queue에서 제거되면 다음 팝업 표시
                    if (popupQueue.Count > 0)
                    {
                        popupQueue[0].Show();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            NotifyPopupCountChanged();
        }
    }
}
