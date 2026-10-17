using UnityEngine;

namespace PKBall.Prototype
{
    public enum PrototypeEncounterTier { Normal, Elite, Boss }

    public sealed class PrototypeEncounter
    {
        public string name, monsterId;
        public PrototypeBroadcastSide character;
        public float damageBonus, damageReduction, startingShield;
        public PrototypeEncounterTier tier;
        public int day;
        public float health;
        public float speed;
        public float impactBonus;
        public float impactTakenMultiplier;
        public float weaponDamageMultiplier;
        public int rewardGold;
        public bool playerLike;
        public string[] itemIds;
        public string[] lootItemIds; // Optional exclusive monster loot pool; null uses equipped items.
        public string visualId;
        public string combatStyle;

        public string Description(PrototypeRun run)
        {
            string items = "";
            foreach (string id in itemIds)
            {
                var item = run.Find(id);
                if (item != null) items += (items.Length == 0 ? "" : "、") + item.displayName;
            }
            return $"{name} [{tier}]  生命 {health:0} / 速度 {speed:0}\n" +
                $"撞击 +{impactBonus * 100f:0}%  武器伤害 ×{weaponDamageMultiplier:0.00}\n" +
                $"打法：{combatStyle}\n道具：{items}  胜利奖励 {rewardGold} 金";
        }
    }

    public interface IPrototypeOpponentProvider
    {
        PrototypeEncounter GetOpponent(PrototypeRun run, int tier);
    }

    public sealed class PrototypeMonsterProvider : IPrototypeOpponentProvider
    {
        public PrototypeEncounter GetOpponent(PrototypeRun run, int tier) =>
            PrototypeEncounterCatalog.Get(run, tier, false);
    }

    // Replace this provider with an asynchronous player snapshot service later.
    public sealed class PrototypeGhostPlayerProvider : IPrototypeOpponentProvider
    {
        public PrototypeEncounter GetOpponent(PrototypeRun run, int tier) =>
            PrototypeEncounterCatalog.Get(run, tier, true);
    }

    public static class PrototypeEncounterCatalog
    {
        private sealed class MonsterProfile
        {
            public readonly string name, visualId, combatStyle, loadout;
            public readonly float health, speed, impact, weapon;

            public MonsterProfile(string name, string visualId, string combatStyle, string loadout,
                float health, float speed, float impact, float weapon)
            {
                this.name = name; this.visualId = visualId; this.combatStyle = combatStyle;
                this.loadout = loadout; this.health = health; this.speed = speed;
                this.impact = impact; this.weapon = weapon;
            }
        }

        // Each tier owns its candidate pool; old variant indices remain stable in run saves.
        private static readonly MonsterProfile[][] Monsters =
        {
            new[]
            {
                new MonsterProfile("绿角冲锋兽", "monster_normal", "高速撞击", "ram,boots,antler_guard,spear,padding,antler_guard", -8f, 12f, .08f, 1f),
                new MonsterProfile("孢子矿怪", "monster_spore", "厚血近战", "pickaxe,spore_lantern,apprentice_venom,padding,shield_emitter,spore_lantern", 17f, -5f, .02f, .9f),
                new MonsterProfile("电羽球", "monster_spark", "远程追击", "crossbow,afterburner,storm_feather,boots,padding,storm_feather", -15f, 17f, 0f, .84f),
                new MonsterProfile("青竹剑灵", "monster_bamboo_spirit", "飞剑成长与剑匣连携", "apprentice_sword,sword_case,green_flying_sword,green_flying_sword,boots,padding", -4f, 8f, .02f, 1f),
                new MonsterProfile("赤丹药修", "monster_red_alchemist", "丹炉生产与血葫芦输出", "random_brewer,blood_gourd,red_potion_furnace,health_lab,padding,shield_emitter", 12f, -3f, 0f, 1f)
            },
            new[]
            {
                new MonsterProfile("赤角斗兽", "monster_elite", "长矛与护甲", "spear,plating,ram,red_duel_banner,red_duel_banner,padding", 10f, 0f, .05f, 1f),
                new MonsterProfile("锯齿野猪", "monster_saw", "重锤冲锋", "hammer,boots,afterburner,saw_flywheel,saw_flywheel,antler_guard", -5f, 16f, .08f, .95f),
                new MonsterProfile("冰壳蟹", "monster_frost", "远程与防守", "crossbow,shield_emitter,padding,frost_mirror,frost_mirror,plating", 21f, -9f, -.02f, .88f),
                new MonsterProfile("八团史莱姆", "monster_slime", "一核心七粘液，缓慢膨胀并造成自身生命比例伤害", "slime_core,slime_mucus,slime_mucus,slime_mucus,slime_mucus,slime_mucus,slime_mucus,slime_mucus", 21f, -9f, -.02f, .88f),
                new MonsterProfile("瘟疫炼金师", "monster_plague_alchemist", "毒喷管与蒸馏生产", "plague_heart,plague_distiller,plague_sprayer,plague_sprayer,padding,boots", 5f, 2f, 0f, 1f),
                new MonsterProfile("圣钟守卫", "monster_saint_guard", "圣钟叠盾与裁决转伤", "saint_seal,judgement_hammer,holy_bell,holy_bell,plating,padding", 20f, -8f, .02f, 1f)
            },
            new[]
            {
                new MonsterProfile("王冠构造体", "monster_boss", "核心重击", "hammer,core,plating,crown_command,crown_command,shield_emitter", 15f, -4f, .06f, 1f),
                new MonsterProfile("熔炉巨灵", "monster_furnace", "巨剑突进", "flame_axe,afterburner,ram,furnace_bellows,furnace_bellows,furnace_armor", -12f, 16f, .1f, 1.02f),
                new MonsterProfile("雷霆机械龙", "monster_storm", "电弧炮与护盾", "arc_cannon,shield_emitter,core,storm_capacitor,storm_capacitor,plating", 5f, 4f, .02f, .9f),
                new MonsterProfile("小死神", "monster_reaper", "镰刀斩杀与短暂无敌，骨珠提供少量护盾", "death_scythe,death_cloak,bone_talisman,padding", 15f, -4f, .06f, 1f),
                new MonsterProfile("灰烬骑士", "monster_ash_knight", "巨剑灼烧与破盾余火", "ember_crown,furnace_armor,ash_greatsword,ash_greatsword,plating,padding", 15f, -4f, .05f, 1f),
                new MonsterProfile("霜山龟灵", "monster_frost_tortoise", "龟甲再生与最大生命打击", "tortoise_core,mountain_seal,frost_shell,frost_shell,padding,plating", 30f, -12f, .01f, 1f)
            }
        };

        public static int VariantCount(int tier) => Monsters[Mathf.Clamp(tier,0,2)].Length;
        public static string BuiltinId(int tier, int variant)
        { tier=Mathf.Clamp(tier,0,2); return "builtin:"+tier+":"+Monsters[tier][Mathf.Clamp(variant,0,VariantCount(tier)-1)].visualId; }
        public static int BuiltinVariant(int tier, string id)
        { for(int i=0;i<VariantCount(tier);i++)if(BuiltinId(tier,i)==id)return i;return -1; }
        // Only three current candidate snapshots are retained; UI queries share a read-only scaled side.
        private static readonly PrototypeMonsterPreset[] cachedPresets=new PrototypeMonsterPreset[3];
        private static readonly PrototypeBroadcastSide[] cachedCharacters=new PrototypeBroadcastSide[3];
        private static readonly int[] cachedDays=new int[3];
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {System.Array.Clear(cachedPresets,0,3);System.Array.Clear(cachedCharacters,0,3);System.Array.Clear(cachedDays,0,3);}
        private static PrototypeEncounter Custom(PrototypeRun run, int tier, bool ghost, PrototypeMonsterPreset preset)
        {
            if(!ReferenceEquals(cachedPresets[tier],preset) || cachedDays[tier]!=run.Day || cachedCharacters[tier]==null)
            {
                cachedPresets[tier]=preset;cachedDays[tier]=run.Day;cachedCharacters[tier]=preset.ScaleToDay(run.Day);
            }
            var side=cachedCharacters[tier];
            var stats=new PrototypeStatSnapshot {health=side.health,speed=side.speed,damageBonus=side.damageBonus,damageReduction=side.damageReduction,startingShield=side.shield};
            var ids=side.slots==null?new string[0]:System.Array.ConvertAll(side.slots,slot=>slot?.itemId??"");
            if(side.slots!=null)foreach(var slot in side.slots)
            {
                if(slot==null || string.IsNullOrEmpty(slot.itemId))continue;
                var d=run.Find(slot.itemId);if(d==null)continue;
                float scale=Mathf.Pow(2,d.ResolveLevel(slot.level)-1);
                stats.health+=Mathf.RoundToInt((slot.healthBonus>=0?slot.healthBonus:d.healthBonus)*slot.monsterHealthScale)*scale;
                stats.speed+=(slot.speedBonus>=0?slot.speedBonus:d.speedBonus)*scale;
                stats.impactBonus+=d.impactBonus*scale;stats.impactTakenReduction+=d.impactTakenReduction*scale;
                if(slot.absorbed!=null)foreach(var absorbed in slot.absorbed)
                {
                    var source=absorbed==null?null:run.Find(absorbed.itemId);
                    if(source==null || source.kind!=PrototypeItemKind.Equipment)continue;
                    float fraction=PrototypeEffectMath.Absorption(slot.level)*absorbed.count;
                    float sourceScale=Mathf.Pow(2,source.ResolveLevel(absorbed.level)-1);
                    stats.health+=source.healthBonus*sourceScale*fraction*slot.monsterHealthScale;stats.speed+=source.speedBonus*sourceScale*fraction;
                    stats.impactBonus+=source.impactBonus*sourceScale*fraction;stats.impactTakenReduction+=source.impactTakenReduction*sourceScale*fraction;
                }
            }
            return new PrototypeEncounter {name=ghost?"镜像对手·"+side.name:side.name,monsterId=preset.id,character=side,
                tier=(PrototypeEncounterTier)tier,day=run.Day,health=stats.health,speed=stats.speed,impactBonus=stats.impactBonus,
                impactTakenMultiplier=stats.ImpactTakenMultiplier,weaponDamageMultiplier=(side.slots!=null&&side.slots.Length>0&&side.slots[0]!=null?side.slots[0].monsterOutputScale:1)*(1+side.damageBonus),damageBonus=side.damageBonus,
                damageReduction=side.damageReduction,startingShield=side.shield,rewardGold=tier==0?3:tier==1?6:10,
                playerLike=ghost,itemIds=ids,visualId=side.visualId,combatStyle="导入角色构筑"};
        }

        public static PrototypeEncounter Get(PrototypeRun run, int tier, bool ghost, int variant = -1)
        {
            int day = Mathf.Max(run.Day, 1);
            tier = Mathf.Clamp(tier, 0, 2);
            if (variant < 0)
            {
                var custom=run.CustomMonsterOffer(tier);
                if(custom!=null)return Custom(run,tier,ghost,custom);
                variant=run.MonsterOfferVariant(tier);
            }
            MonsterProfile profile = Monsters[tier][Mathf.Clamp(variant, 0, VariantCount(tier) - 1)];
            string[] equipment = profile.loadout.Split(',');
            float health = tier == 0 ? 204f + 24f * (day - 1) :
                tier == 1 ? 294f + 45f * (day - 1) : 408f + 72f * (day - 1);
            float speed = tier == 0 ? 88f + 2f * (day - 1) :
                tier == 1 ? 103f + 3f * (day - 1) : 113f + 4f * (day - 1);
            float impact = 0f;
            float reduction = 0f;
            if (profile != null)
            {
                health += profile.health*.6f;
                speed += profile.speed;
                impact += profile.impact;
            }
            foreach (string id in equipment)
            {
                var item = run.Find(id);
                if (item == null) continue;
                float itemScale=Mathf.Pow(2,item.ResolveLevel(1)-1);
                health += item.healthBonus*itemScale;
                speed += item.speedBonus*itemScale;
                impact += item.impactBonus*itemScale;
                reduction += item.impactTakenReduction*itemScale;
            }
            return new PrototypeEncounter
            {
                name = ghost ? "镜像对手·" + profile.name : profile.name,
                monsterId = BuiltinId(tier,variant),
                visualId = ghost ? "rival" : profile.visualId,
                combatStyle = ghost ? "玩家镜像构筑" : profile.combatStyle,
                tier = (PrototypeEncounterTier)tier,
                day = day,
                health = health,
                speed = speed,
                impactBonus = impact,
                impactTakenMultiplier = Mathf.Clamp(1f - reduction, .5f, 1f),
                weaponDamageMultiplier = (tier == 0 ? .55f + .04f * (day - 1) :
                    tier == 1 ? .9f + .06f * (day - 1) : 1.2f + .1f * (day - 1)) *
                    profile.weapon,
                rewardGold = tier == 0 ? 3 : tier == 1 ? 6 : 10,
                playerLike = ghost,
                itemIds = equipment
            };
        }
    }
}
