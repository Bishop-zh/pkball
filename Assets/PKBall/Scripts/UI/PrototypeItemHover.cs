using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PKBall.Prototype
{
    public sealed class PrototypeItemHover : MonoBehaviour, IPointerEnterHandler,
        IPointerExitHandler, IScrollHandler
    {
        [SerializeField] private PrototypePreparationUI ui;
        [SerializeField] private PrototypeHoverKind kind;
        [SerializeField] private int index;
        private bool waiting;
        private float enteredAt;
        public void Initialize(PrototypePreparationUI owner, PrototypeHoverKind source, int sourceIndex)
        { ui = owner; kind = source; index = sourceIndex; }
        public void OnPointerEnter(PointerEventData eventData)
        { waiting = true; enteredAt = Time.unscaledTime; }
        public void OnPointerExit(PointerEventData eventData)
        { waiting = false; ui.ClearHover(kind, index); }
        public void OnScroll(PointerEventData eventData) =>
            ui.SetHover(kind, index, eventData.scrollDelta.y > 0 ? 1 : -1);

        private void Update()
        {
            if (!waiting || Time.unscaledTime - enteredAt < .55f) return;
            waiting = false;
            ui.SetHover(kind, index, 0);
        }
    }
}
