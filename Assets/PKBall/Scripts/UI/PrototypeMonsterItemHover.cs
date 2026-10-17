using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PKBall.Prototype
{
    public sealed class PrototypeMonsterItemHover : MonoBehaviour, IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField] private PrototypePreparationUI ui;
        [SerializeField] private int tier;
        [SerializeField] private int itemIndex;
        private bool waiting;
        private float enteredAt;

        public void Initialize(PrototypePreparationUI owner, int monsterTier, int index)
        { ui = owner; tier = monsterTier; itemIndex = index; }
        public void OnPointerEnter(PointerEventData eventData)
        { waiting = true; enteredAt = Time.unscaledTime; }
        public void OnPointerExit(PointerEventData eventData)
        { waiting = false; ui.ClearMonsterItemHover(tier); }
        private void Update()
        {
            if (!waiting || Time.unscaledTime - enteredAt < .45f) return;
            waiting = false;
            ui.ShowMonsterItemHover(tier, itemIndex);
        }
    }
}
