using UnityEngine;
using UnityEngine.UI;

namespace LayerLabAsset
{
    public class UICanvasView : MonoBehaviour
    {
        [field: SerializeField] public Canvas[] Canvas { get; private set; }
        [field: SerializeField] public RectTransform Rect { get; private set; }
        [field: SerializeField] public GraphicRaycaster GraphicRaycaster { get; private set; }

        
        private void OnValidate()
        {
            if(Rect == null) Rect = GetComponent<RectTransform>();
            if(Canvas == null) Canvas = GetComponentsInChildren<Canvas>(true);
            if(GraphicRaycaster == null) GraphicRaycaster = GetComponent<GraphicRaycaster>();
        }

        public void SetView(bool isOn)
        {
            foreach (var t in Canvas)
                t.enabled = isOn;

            if(GraphicRaycaster) GraphicRaycaster.enabled = isOn;
        }
    }
}
