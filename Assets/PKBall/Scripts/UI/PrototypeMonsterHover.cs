using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PKBall.Prototype
{
    public sealed class PrototypeMonsterHover : MonoBehaviour, IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField] private PrototypePreparationUI ui;
        [SerializeField] private int tier;
        private bool waiting;
        private float enteredAt;

        public void Initialize(PrototypePreparationUI owner, int index)
        { ui = owner; tier = index; }
        public void ResetHover() { waiting = false; }
        public void OnPointerEnter(PointerEventData eventData)
        { waiting = true; enteredAt = Time.unscaledTime; }
        public void OnPointerExit(PointerEventData eventData)
        { waiting = false; ui.ClearMonsterHover(tier); }
        private void Update()
        {
            if (!waiting || Time.unscaledTime - enteredAt < .65f) return;
            waiting = false;
            ui.ShowMonsterHover(tier);
        }
    }
}
