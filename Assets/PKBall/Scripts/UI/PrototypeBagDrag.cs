using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PKBall.Prototype
{
    public sealed class PrototypeBagDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [field: SerializeField] public int Index { get; private set; }
        [field: SerializeField] public bool IsShop { get; private set; }
        [field: SerializeField] public bool IsSlot {get;private set;}
        [SerializeField] private PrototypePreparationUI ui;
        private GameObject ghost;

        public void Initialize(PrototypePreparationUI owner, int index, bool shop = false, bool slot=false)
        { ui = owner; Index = index; IsShop = shop;IsSlot=slot; }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsShop ? !ui.HasShopItem(Index) : IsSlot ? !ui.HasEquippedItem(Index) : !ui.HasBagItem(Index)) return;
            Canvas canvas = GetComponentInParent<Canvas>();
            ghost = new GameObject("DraggingItem", typeof(RectTransform), typeof(Image));
            ghost.transform.SetParent(canvas.transform, false);
            Image image = ghost.GetComponent<Image>();
            Image sourceIcon = transform.Find("Icon")?.GetComponent<Image>();
            image.sprite = sourceIcon == null ? null : sourceIcon.sprite;
            image.color = image.sprite == null ? new Color(0.35f, 0.75f, 0.85f, 0.8f) : Color.white;
            image.raycastTarget = false;
            RectTransform rect = (RectTransform)ghost.transform;
            rect.sizeDelta = new Vector2(96, 96);
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (ghost != null) ghost.transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (ghost != null) Destroy(ghost);
            ghost = null;
        }
    }
}
