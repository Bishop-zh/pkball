using System;
using UnityEngine;
namespace PKBall.Prototype
{
    public enum PrototypeItemAnimationPreset { Auto, None, Sweep, Thrust, Smash, Recoil, Burst, Orbit, Ring, Shield, Heal, Poison, Flame, Forge, Absorb, Growth, Crescent }
    [Serializable]
    public sealed class PrototypeItemAnimation
    {
        public PrototypeItemAnimationPreset preset=PrototypeItemAnimationPreset.Auto;
        public Color tint=Color.white;
        public float duration=.55f, scale=1f, intensity=1f;
        public int count=1;
        public bool showIcon=true;
        public float rotation;
        public PrototypeItemAnimation Clone() => (PrototypeItemAnimation)MemberwiseClone();
    }
    public static class PrototypeItemAnimationDefaults
    {
        public static PrototypeItemAnimation Resolve(PrototypeItemDefinition definition)
        {
            if(definition==null)return DefaultFor(null);
            var value=definition.animation;
            if(value==null)return DefaultFor(definition);
            var result=value.Clone();
            if(result.preset==PrototypeItemAnimationPreset.Auto)result.preset=DefaultFor(definition).preset;
            result.duration=Mathf.Clamp(result.duration,.05f,10f);result.scale=Mathf.Clamp(result.scale,.1f,8f);
            result.intensity=Mathf.Clamp(result.intensity,0,3);result.count=Mathf.Clamp(result.count,1,24);
            return result;
        }
        public static PrototypeItemAnimation DefaultFor(PrototypeItemDefinition d)
        {
            var a=new PrototypeItemAnimation {preset=PrototypeItemAnimationPreset.Burst};
            if(d==null)return a;
            if(d.itemId=="death_scythe")a.preset=PrototypeItemAnimationPreset.Crescent;
            else if(d.itemId=="slime_core")a.preset=PrototypeItemAnimationPreset.Growth;
            else if(d.itemId=="green_flying_sword")a.preset=PrototypeItemAnimationPreset.Orbit;
            else if(d.productionInterval>0)a.preset=PrototypeItemAnimationPreset.Forge;
            else if(d.weapon!=null)
            {
                a.duration=d.weapon.animationDuration;
                a.preset=d.weapon.kind==PrototypeWeaponKind.Melee?(d.weapon.attackShape==PrototypeAttackShape.Thrust?PrototypeItemAnimationPreset.Thrust:d.weapon.attackShape==PrototypeAttackShape.Smash?PrototypeItemAnimationPreset.Smash:PrototypeItemAnimationPreset.Sweep):PrototypeItemAnimationPreset.Recoil;
                if(d.weapon.visualStyle==PrototypeAttackVisual.Shockwave)a.preset=PrototypeItemAnimationPreset.Ring;
                if(d.weapon.visualStyle==PrototypeAttackVisual.Volley){a.preset=PrototypeItemAnimationPreset.Orbit;a.count=Mathf.Clamp(d.weapon.visualCount,1,24);}
            }
            else if(d.effects!=null)foreach(var e in d.effects)
            {
                if(e.kind==PrototypeEffectKind.Shield || e.kind==PrototypeEffectKind.Invulnerability){a.preset=PrototypeItemAnimationPreset.Shield;a.tint=new Color(1,.8f,.25f);break;}
                if(e.kind==PrototypeEffectKind.Heal || e.kind==PrototypeEffectKind.Regeneration){a.preset=PrototypeItemAnimationPreset.Heal;a.tint=new Color(.3f,1,.6f);break;}
                if(e.kind==PrototypeEffectKind.Poison){a.preset=PrototypeItemAnimationPreset.Poison;a.tint=new Color(.5f,1,.2f);break;}
                if(e.kind==PrototypeEffectKind.Burn){a.preset=PrototypeItemAnimationPreset.Flame;a.tint=new Color(1,.35f,.1f);break;}
                if(e.kind==PrototypeEffectKind.GrowBattleMaxHealth){a.preset=PrototypeItemAnimationPreset.Growth;break;}
                if(e.kind==PrototypeEffectKind.ReplayPotions){a.preset=PrototypeItemAnimationPreset.Absorb;break;}
            }
            if(d.kind==PrototypeItemKind.Injection)a.preset=PrototypeItemAnimationPreset.Heal;
            return a;
        }
    }
}
