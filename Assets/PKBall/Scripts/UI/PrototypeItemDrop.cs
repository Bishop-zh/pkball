using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PKBall.Prototype
{
    public sealed class PrototypeItemDrop : MonoBehaviour, IDropHandler
    {
        [SerializeField] private PrototypePreparationUI ui;
        [SerializeField] private PrototypeDropKind kind;
        [SerializeField] private int index;

        public void Initialize(PrototypePreparationUI owner, PrototypeDropKind dropKind, int targetIndex)
        { ui = owner; kind = dropKind; index = targetIndex; }

        public void OnDrop(PointerEventData eventData)
        {
            PrototypeBagDrag source = eventData.pointerDrag == null ? null :
                eventData.pointerDrag.GetComponent<PrototypeBagDrag>();
            if (source == null) return;
            if(source.IsSlot)ui.DropEquipped(source.Index,kind,index);
            else if (source.IsShop) ui.DropShop(source.Index, kind, index);
            else ui.DropItem(source.Index, kind, index);
        }
    }
}
