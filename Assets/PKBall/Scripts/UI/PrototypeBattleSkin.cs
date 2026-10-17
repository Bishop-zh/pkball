using UnityEngine;
using UnityEngine.U2D;

namespace PKBall.Prototype
{
    [CreateAssetMenu(menuName = "PKBall/Art/Battle Skin")]
    public sealed class PrototypeBattleSkin : ScriptableObject
    {
        public string skinId = "brass_courtyard";
        public Sprite background, arenaFloor, arenaRim;
        public Sprite playerPanel, enemyPanel, itemTile, healthFrame;
        public Sprite playerHealth, enemyHealth, shieldFill, energyFrame, energyFill;
        public Sprite countBadge, titleStrip, cyanButton, coralButton;
        public SpriteAtlas interfaceAtlas, itemAtlas;
    }
}
