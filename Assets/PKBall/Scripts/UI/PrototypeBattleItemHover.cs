using UnityEngine;
using UnityEngine.EventSystems;
namespace PKBall.Prototype
{
    public sealed class PrototypeBattleItemHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IScrollHandler
    {
        private PrototypeBattleView view;
        private bool player, inside, shown;
        private int index, preview;
        private float entered;
        public void Bind(PrototypeBattleView owner,bool own,int slot)
        {view=owner;player=own;index=slot;}
        public void OnPointerEnter(PointerEventData data)
        {inside=true;shown=false;preview=0;entered=Time.unscaledTime;}
        public void OnPointerExit(PointerEventData data)
        {inside=false;shown=false;if(view!=null) view.HideItemDetails(this);}
        public void OnScroll(PointerEventData data)
        {
            var item=view.ItemFor(player,index);if(item==null) return;
            preview=Mathf.Clamp((preview==0?item.level:preview)+(data.scrollDelta.y>0?1:-1),1,4);
            shown=true;view.ShowItemDetails(this,player,index,preview);
        }
        private void Update()
        {
            if(!inside || view==null) return;
            if(Time.unscaledTime-entered<.5f && !shown) return;
            shown=true;view.ShowItemDetails(this,player,index,preview);
        }
        private void OnDisable() {inside=false;shown=false;if(view!=null) view.HideItemDetails(this);}
    }
}
