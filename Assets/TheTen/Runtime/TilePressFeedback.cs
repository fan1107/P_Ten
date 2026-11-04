using UnityEngine;
using UnityEngine.EventSystems;

namespace TheTen
{
    // Button owns selection; this component only forwards pointer visuals.
    public sealed class TilePressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public TileVisual Owner;
        int? pointer;
        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || pointer.HasValue) return;
            if (Owner.BeginPress()) pointer = e.pointerId;
        }
        public void OnPointerUp(PointerEventData e)
        {
            if (pointer != e.pointerId) return;
            pointer = null; Owner.EndPress(false);
        }
        public void OnPointerExit(PointerEventData e)
        {
            if (pointer != e.pointerId) return;
            pointer = null; Owner.EndPress(true);
        }
        void OnDisable() { pointer = null; if (Owner != null) Owner.EndPress(true); }
    }
}
