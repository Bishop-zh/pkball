using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PKBall.Prototype
{
    public sealed partial class PrototypeRun
    {
        public static readonly string[] SlotOrder = { "FR", "R1", "R2", "R3", "R4", "L4", "L3", "L2", "L1", "FL" };
        private readonly List<PrototypeItemInstance> bag = new List<PrototypeItemInstance>();
        private readonly List<PrototypeItemInstance> pendingRewards = new List<PrototypeItemInstance>();
        private readonly PrototypeItemInstance[] equipped = new PrototypeItemInstance[10];
        private readonly PrototypeSlotEffect[] slotEffects = new PrototypeSlotEffect[10];
        private readonly PrototypeItemDefinition[] shop = new PrototypeItemDefinition[4];
        private readonly List<PrototypeStatModifier> permanentModifiers = new List<PrototypeStatModifier>();
        public string RunId { get; private set; } = Guid.NewGuid().ToString("N");
        public bool Started { get; private set; }
        public bool Parked { get; private set; }
        public bool HasActiveRun => Started && !Parked && !GameOver;
        public int DefeatCost => Mathf.Min(8, 1 + (Day - 1) / 3);
        public int LastDefeatCost { get; private set; }
        public int Day { get; private set; } = 1;
        public int Gold { get; private set; } = 24;
        public int Xp { get; private set; }
        public int Slots { get; private set; } = 8;
        public int Level { get; private set; } = 1;
        public const float BaseHealth=300;
        public float BodyHealth { get; private set; } = BaseHealth;
        public int UpgradeCost => 25 + (Level - 1) * 5;
        private readonly List<PrototypePendingModifier> pendingBattleModifiers = new List<PrototypePendingModifier>();
        private readonly Dictionary<string,int> combatProducts = new Dictionary<string,int>();
        private int combatGold;
        private float oreCredit;
        private bool combatDirty;
        private readonly Dictionary<string,float> permanentCombatGrants = new Dictionary<string,float>();
        public int OreCount {get{int n=0;foreach(var i in bag)if(i.definition.itemId=="gold_ore")n+=i.quantity;foreach(var i in pendingRewards)if(i.definition.itemId=="gold_ore")n+=i.quantity;return n;}}
        public event Action<string> Feedback;
        public event Action<PrototypeItemDefinition> ItemAnimationFeedback;
        public int CombatOre { get; private set; }
        public int Endurance { get; private set; } = 10;
        public int Wins { get; private set; }
        public bool LastStandUsed { get; private set; }
        public bool Endless { get; private set; }
        public bool GameOver { get; private set; }
        public bool Cleared => Wins >= 10;
        public PrototypeDayStage Stage { get; private set; }
        public bool EventChosen { get; private set; }
        public bool ShopAvailable { get; private set; }
        public int SelectedMonsterTier { get; private set; } = -1;
        private readonly int[] monsterOffers = { -1, -1, -1 };
        private readonly string[] monsterOfferIds = new string[3];
        private readonly PrototypeMonsterPreset[] customMonsterOffers = new PrototypeMonsterPreset[3];
        public int MonsterOfferVariant(int tier) => monsterOffers[Mathf.Clamp(tier, 0, 2)];
        public PrototypeMonsterPreset CustomMonsterOffer(int tier) => customMonsterOffers[Mathf.Clamp(tier,0,2)];
        public string MonsterOfferId(int tier) => monsterOfferIds[Mathf.Clamp(tier,0,2)];
        public bool IsEventStage => Stage != PrototypeDayStage.Monster && Stage != PrototypeDayStage.Rival;
        public int RivalTier => (Day * 37 + 3) % 10 < 6 ? 0 :
            (Day * 37 + 3) % 10 < 9 ? 1 : 2;
        public int Version { get; private set; }
        public readonly List<string> ActivityLog=new List<string>();
        private readonly Dictionary<string,int> productionLog=new Dictionary<string,int>();
        private string notice="白板开局：先购买武器，再安排格位。";
        public string Notice {get=>notice;private set {notice=value;AddLog(value);}}
        public void AddLog(string text)
        {if(string.IsNullOrEmpty(text))return;ActivityLog.Add($"第 {Day} 天 · "+text);if(ActivityLog.Count>80)ActivityLog.RemoveAt(0);}
        public bool HasEnchantableSlot
        {get{for(int i=0;i<10;i++)if(IsSlotOpen(i)&&slotEffects[i]==PrototypeSlotEffect.None)return true;return false;}}
        public PrototypeItemRarity MaxShopRarity=>Level>=7?PrototypeItemRarity.Legendary:Level>=4?PrototypeItemRarity.Epic:Level>=2?PrototypeItemRarity.Rare:PrototypeItemRarity.Common;
        public int ShopTierLevel=>Mathf.Min(Level,7);
        public bool IsShopEligible(PrototypeItemDefinition item)=>item!=null && item.kind==PrototypeItemKind.Equipment && item.acquisition!=PrototypeItemAcquisition.WildOnly && !item.bossExclusive && item.rarity<=MaxShopRarity && Day>=item.minShopDay && (item.itemId!="break_core" || Level>=10);
        public int ShopRarityWeight(PrototypeItemRarity rarity)
        {int maturity=ShopTierLevel;return rarity==PrototypeItemRarity.Common?12-maturity:rarity==PrototypeItemRarity.Rare?3+maturity:rarity==PrototypeItemRarity.Epic?1+maturity/2:Mathf.Max(1,maturity/3);}
        public string ShopRarityHint
        {get{string result="";for(int i=0;i<= (int)MaxShopRarity;i++){bool available=false;foreach(var item in catalogue)if(IsShopEligible(item)&&(int)item.rarity==i){available=true;break;}
            if(available)result+=(result.Length==0?"":" / ")+PrototypeItemDefinition.RarityName((PrototypeItemRarity)i);}
            return $"等级 {Level} · 可刷：{result}"+(Level>=7?"（概率已封顶，保留低稀有度）":" · 2/4/7级解锁，另受天数限制");}}
        private long nextId = 1;
        private int pickaxeHits;
        private readonly PrototypeItemDefinition[] catalogue;
        private readonly Dictionary<string, PrototypeItemDefinition> byId = new Dictionary<string, PrototypeItemDefinition>();
        private readonly System.Random random = new System.Random();
        private readonly string savePath;
        private readonly bool transient;
        private bool loadNeedsUpgrade;

        public PrototypeRun(bool fresh = false, bool transient = false, string saveOverride = null, bool detached = false)
        {
            Bag=bag.AsReadOnly();PendingRewards=pendingRewards.AsReadOnly();Equipped=Array.AsReadOnly(equipped);
            Shop=Array.AsReadOnly(shop);SlotEffects=Array.AsReadOnly(slotEffects);
            PermanentModifiers=permanentModifiers.AsReadOnly();PendingBattleModifiers=pendingBattleModifiers.AsReadOnly();
            this.transient = transient;
            slotEffects[0] = PrototypeSlotEffect.Vital;
            slotEffects[9] = PrototypeSlotEffect.Swift;
            catalogue = PrototypeItemRepository.All;
            foreach (var item in catalogue)
                if (item != null && !string.IsNullOrEmpty(item.itemId)) byId[item.itemId] = item;
            savePath = saveOverride ?? Path.Combine(Application.persistentDataPath, "pkball-prototype-run.json");
            if (fresh || transient || detached || !LoadOrPreserveRejectedSave()) RestockCore();
            bool offersCreated = EnsureMonsterOffers();
            if (!transient && !detached && (fresh || offersCreated || loadNeedsUpgrade)) Save();
            if(!transient && !detached && GameOver) PrototypeRunHistory.Store(this,"失败");
        }

        private bool EnsureMonsterOffers()
        {
            bool created=false;
            for(int tier=0;tier<3;tier++)
            {
                if(customMonsterOffers[tier]!=null && customMonsterOffers[tier].character!=null)continue;
                int stable=PrototypeEncounterCatalog.BuiltinVariant(tier,monsterOfferIds[tier]);
                if(stable>=0) {monsterOffers[tier]=stable;continue;}
                if(monsterOffers[tier]>=0 && monsterOffers[tier]<PrototypeEncounterCatalog.VariantCount(tier))
                {monsterOfferIds[tier]=PrototypeEncounterCatalog.BuiltinId(tier,monsterOffers[tier]);created=true;continue;}
                RollMonsterOffer(tier);created=true;
            }
            return created;
        }
        private void RollMonsterOffer(int tier)
        {
            var imported=new List<PrototypeMonsterPreset>(PrototypeMonsterPool.All((PrototypeEncounterTier)tier));
            int builtin=PrototypeEncounterCatalog.VariantCount(tier);
            int choice=random.Next(builtin+imported.Count);
            customMonsterOffers[tier]=null;
            if(choice<builtin)
            {monsterOffers[tier]=choice;monsterOfferIds[tier]=PrototypeEncounterCatalog.BuiltinId(tier,choice);}
            else
            {
                var snapshot=imported[choice-builtin].Copy();customMonsterOffers[tier]=snapshot;
                monsterOffers[tier]=-1;monsterOfferIds[tier]=snapshot.id;
            }
        }
        private void RollMonsterOffers()
        {for(int tier=0;tier<3;tier++)RollMonsterOffer(tier);}

        public PrototypeItemDefinition Find(string id) =>
            id != null && byId.TryGetValue(id, out var item) ? item : null;

        public PrototypeEncounter CurrentEncounter
        {
            get
            {
                IPrototypeOpponentProvider provider = Stage == PrototypeDayStage.Rival
                    ? (IPrototypeOpponentProvider)new PrototypeGhostPlayerProvider()
                    : new PrototypeMonsterProvider();
                int tier = Stage == PrototypeDayStage.Rival ? RivalTier :
                    Mathf.Max(0, SelectedMonsterTier);
                return provider.GetOpponent(this, tier);
            }
        }

        public PrototypeEventKind EventAt(int choice) =>
            FilteredEvent(choice);
        private PrototypeEventKind FilteredEvent(int choice)
        {
            var kind=PrototypeDayRules.EventAt(Day,Stage,choice);
            if(kind==PrototypeEventKind.Enchant && !HasEnchantableSlot) return PrototypeEventKind.MasterTraining;
            return kind;
        }

        public bool ChooseEvent(int choice, int preferredSlot = -1)
        {
            if (!CanEditPreparation) return false;
            if (!IsEventStage || EventChosen || GameOver || (Cleared && !Endless) ||
                choice < 0 || choice > 2) return false;
            PrototypeEventKind kind = EventAt(choice);
            switch (kind)
            {
                case PrototypeEventKind.Shop:
                    ShopAvailable = true; RestockCore(); break;
                case PrototypeEventKind.Gold: Gold += 6 + Day / 2; break;
                case PrototypeEventKind.Ore: AddOreCore(3); break;
                case PrototypeEventKind.Pickaxe: RewardItem("pickaxe"); break;
                case PrototypeEventKind.Weapon:
                    string[] weapons = { "spear", "crossbow", "pickaxe", "rapier", "hammer" };
                    RewardItem(weapons[(Day + (int)Stage * 2) % weapons.Length]); break;
                case PrototypeEventKind.Injections:
                    string[] potions = { "injector", "strength_injector", "speed_injector", "gold_potion" };
                    RewardItem(potions[(Day + (int)Stage) % potions.Length]);
                    RewardItem(potions[(Day + (int)Stage + 1) % potions.Length]); break;
                case PrototypeEventKind.Enchant:
                    int slot = preferredSlot >= 0 && IsSlotOpen(preferredSlot) && slotEffects[preferredSlot]==PrototypeSlotEffect.None ? preferredSlot : -1;
                    if(slot<0){var empty=new List<int>();for(int i=0;i<10;i++)if(IsSlotOpen(i)&&slotEffects[i]==PrototypeSlotEffect.None)empty.Add(i);
                        if(empty.Count==0)return false;slot=empty[random.Next(empty.Count)];}
                    PrototypeSlotEffect[] effects = { PrototypeSlotEffect.Sharpened,
                        PrototypeSlotEffect.Echo, PrototypeSlotEffect.Guarded,
                        PrototypeSlotEffect.Prosperity, PrototypeSlotEffect.Vital,
                        PrototypeSlotEffect.Swift };
                    slotEffects[slot] = effects[random.Next(effects.Length)];
                    AddLog("附魔成功："+PrototypeDayRules.SlotDescription(slotEffects[slot]));
                    break;
                case PrototypeEventKind.MasterTraining:
                    int trained=0;var seen=new HashSet<long>();
                    foreach(var tool in equipped) if(tool!=null && tool.definition.HasTag("学徒") && seen.Add(tool.instanceId)) {tool.AddActivationGrowth(3*tool.LevelMultiplier,3);trained++;}
                    foreach(var tool in bag) if(tool.definition.HasTag("学徒") && seen.Add(tool.instanceId)) {tool.AddActivationGrowth(3*tool.LevelMultiplier,3);trained++;}
                    AddLog($"宗师训练：{trained} 件学徒工具各增加 3 次成长");break;
                case PrototypeEventKind.Rest:
                    BodyHealth+=20;AddLog("生命休整：本体生命永久 +20");break;
                case PrototypeEventKind.Trade:
                    Gold+=10;AddLog("商旅资助：获得 10 金币");break;
            }
            EventChosen = true;
            Notice = PrototypeDayRules.EventDescription(kind, Day) +
                "";
            Changed(); return true;
        }

        private void RewardItem(string id, bool monsterDrop = false)
        {
            var definition = Find(id);
            if (definition == null || ((definition.acquisition==PrototypeItemAcquisition.WildOnly || definition.bossExclusive) && !monsterDrop)) return;
            var item = NewItem(definition);
            if (bag.Count < 12) bag.Add(item);
            else pendingRewards.Add(item);
            AddLog("获得："+item.Name);ItemAnimationFeedback?.Invoke(definition);
        }

        public bool AdvanceEvent()
        {
            if (!CanEditPreparation) return false;
            if (!IsEventStage || !EventChosen || GameOver) return false;
            Stage++;
            EventChosen = false;
            ShopAvailable = false;
            Notice = Stage == PrototypeDayStage.Monster ? "选择今天的野怪" :
                Stage == PrototypeDayStage.Rival ? "即将匹配镜像对手" : "选择下一个事件";
            Changed(); return true;
        }

        public bool SelectMonster(int tier)
        {
            if (!CanEditPreparation) return false;
            if (Stage != PrototypeDayStage.Monster || tier < 0 || tier > 2 || GameOver) return false;
            SelectedMonsterTier = tier;
            Notice = "已选择 " + CurrentEncounter.name + "；查看数值后进入战斗";
            Changed(); return true;
        }

        public bool CanFight => !GameOver && !Parked && (!Cleared || Endless) &&
            (Stage == PrototypeDayStage.Rival ||
             (Stage == PrototypeDayStage.Monster && SelectedMonsterTier >= 0));

        public void EnterEndless()
        {
            if (!Cleared || Endless || GameOver) return;
            Endless = true;
            Phase=PrototypeRunPhase.Preparation;
            Notice = "已进入无尽模式；第 10 天以后沿用第 10 天怪物表";
            Changed();
        }

        private void ResolveCombatCore(bool won)
        {
            if (GameOver || Parked || (Stage != PrototypeDayStage.Monster && Stage != PrototypeDayStage.Rival)) return;
            CommitCombat();
            LastDefeatCost=0;
            if (!won && Stage==PrototypeDayStage.Rival) ApplyDefeat();
            AddLog(won ? (Stage==PrototypeDayStage.Monster?"野怪战斗胜利":"匹配战斗胜利") : Stage==PrototypeDayStage.Monster?"野怪战败：不扣精力，无额外奖励":$"匹配失败，精力 -{LastDefeatCost}，剩余 {Endurance}/10");
            if (GameOver)
            {
                Notice = $"本局结束：战败扣除 {LastDefeatCost} 精力，保命机会已使用。";
                Changed();
                if (!transient) PrototypeRunHistory.Store(this, "失败");
                return;
            }
            if (Stage == PrototypeDayStage.Monster)
            {
                if (won)
                {
                    var encounter=CurrentEncounter;Gold += encounter.rewardGold;
                    var loot=new List<string>();foreach(var id in encounter.lootItemIds??encounter.itemIds){var item=Find(id);if(item!=null && item.kind==PrototypeItemKind.Equipment)loot.Add(id);}
                    if(loot.Count>0)RewardItem(loot[random.Next(loot.Count)], true);
                    AddLog("野怪额外奖励：金币 +"+encounter.rewardGold);

                }
                Stage = PrototypeDayStage.EventThree;
                SelectedMonsterTier = -1;
                Notice = won ? "野怪获胜，获得战利品；继续事件" :
                    "野怪战败：不扣精力，不获得额外经济和掉落；继续事件";
                Changed();
            }
            else
            {
                if (won) Wins++;
                EndDayCore(won);
                if (Cleared && !Endless)
                {
                    Notice = "已达 10 胜，通关！可选择无尽模式继续。";
                    if (!transient) PrototypeRunHistory.Store(this, "通关");
                }
            }
        }

        private void ApplyDefeat()
        {
            int lost = DefeatCost;
            LastDefeatCost = lost;
            Endurance -= lost;
            if (Endurance > 0) return;
            if (!LastStandUsed)
            {
                Endurance = 1;
                LastStandUsed = true;
                AddLog("保命触发：保留 1 点精力，本局机会已用");
            }
            else { Endurance = 0; GameOver = true; }
        }

        public bool IsSlotOpen(int index)
        {
            if (index < 0 || index >= 10) return false;
            int wing = (Slots - 2) / 2;
            return index == 0 || index == 9 || (index >= 1 && index <= wing) ||
                   (index >= 9 - wing && index <= 8);
        }

        private void RestockCore()
        {
            List<PrototypeItemDefinition> choices = new List<PrototypeItemDefinition>();
            foreach (var item in catalogue)
            {
                if (!IsShopEligible(item)) continue;
                int rarityWeight = ShopRarityWeight(item.rarity);
                for (int weight = 0; weight < Mathf.Max(1, item.shopWeight) * rarityWeight; weight++)
                    choices.Add(item);
            }
            if (choices.Count == 0) return;
            for (int i = 0; i < shop.Length; i++) shop[i] = choices[random.Next(choices.Count)];
            if (Day == 1)
            {
                shop[0] = Find("spear"); shop[1] = Find("crossbow"); shop[2] = Find("health_lab");
            }
        }

        public bool Buy(int index)
        {
            if (!CanEditPreparation) return false;
            if (index < 0 || index >= shop.Length || shop[index] == null) return false;
            var definition = shop[index];
            if (!IsShopEligible(definition)) { Notice = "此道具不可从商店购买"; return false; }
            if (Gold < definition.price) { Notice = "金币不足"; return false; }
            if (bag.Count >= 12) { Notice = "背包已满（12件）"; return false; }
            Gold -= definition.price;
            bag.Add(NewItem(definition)); shop[index] = null;
            Notice = "购入：" + definition.displayName;
            Changed(); return true;
        }

        public bool BuyToSlot(int shopIndex, int slotIndex)
        {
            if (!CanEditPreparation) return false;
            if (shopIndex < 0 || shopIndex >= shop.Length || shop[shopIndex] == null ||
                !IsSlotOpen(slotIndex)) return false;
            var definition = shop[shopIndex];
            if (!IsShopEligible(definition)) { Notice = "此道具不可从商店购买"; return false; }
            if (definition.kind != PrototypeItemKind.Equipment)
            { Notice = "此商品不能装备到格位"; return false; }
            if (Gold < definition.price) { Notice = "金币不足"; return false; }
            var old = equipped[slotIndex];
            if (old != null && old.definition.itemId == definition.itemId && old.level == 1)
            {
                Gold -= definition.price;
                Merge(NewItem(definition), old);
                shop[shopIndex] = null;
                Notice = "购买并合成：" + old.Name;
                Changed(); return true;
            }
            if (equipped[slotIndex] != null && bag.Count >= 12)
            { Notice = "背包已满，无法替换格位"; return false; }
            Gold -= definition.price;
            equipped[slotIndex] = NewItem(definition);
            shop[shopIndex] = null;
            if (old != null) bag.Add(old);
            Notice = "购买并装备：" + definition.displayName + " → " + SlotOrder[slotIndex];
            Changed(); return true;
        }

        public bool ClaimGenerated()
        {
            if (!CanEditPreparation) return false;
            int count = 0;
            while (bag.Count < 12 && pendingRewards.Count > 0)
            {
                bag.Add(pendingRewards[0]); pendingRewards.RemoveAt(0); count++;
            }
            Notice = count > 0 ? "领取产物 " + count + " 件" :
                pendingRewards.Count > 0 ? "背包已满，请先腾出空间" : "暂无待领取产物";
            if (count > 0) Changed();
            return count > 0;
        }

        public void AddGold(int amount, string reason)
        {
            if (!CanEditPreparation) return;
            if (amount <= 0) return;
            Gold += amount;
            Notice = reason + "：金币 +" + amount;
            Changed();
        }

        public bool Reroll()
        {
            if (!CanEditPreparation) return false;
            if (Gold < 2) { Notice = "刷新需要 2 金币"; return false; }
            Gold -= 2; RestockCore(); Notice = "商店已刷新"; Changed(); return true;
        }

        public bool Equip(int bagIndex, int slotIndex)
        {
            if (!CanEditPreparation) return false;
            if (bagIndex < 0 || bagIndex >= bag.Count || !IsSlotOpen(slotIndex)) return false;
            var item = bag[bagIndex]; var old = equipped[slotIndex];
            if (item.definition.itemId == "break_core" && old != null) return Breakthrough(bagIndex, slotIndex);
            if (old != null && CanMerge(item, old)) return MergeIntoSlot(bagIndex, slotIndex);
            if (old != null && old.definition.itemId == "super_injector" && item.definition.kind == PrototypeItemKind.Injection)
                return AbsorbPotion(bagIndex, old);
            if (old != null && old.definition.itemId == "devourer" &&
                item.definition.kind == PrototypeItemKind.Equipment)
                return SwallowIntoSlot(bagIndex, slotIndex);
            if (item.definition.kind != PrototypeItemKind.Equipment)
            { Notice = "消耗品请点使用或拖到角色中心"; return false; }
            equipped[slotIndex] = item; bag.RemoveAt(bagIndex);
            if (old != null) bag.Add(old);
            Notice = "装备：" + item.Name + " → " + SlotOrder[slotIndex];
            Changed(); return true;
        }

        public bool MergeBag(int sourceIndex, int targetIndex)
        {
            if (!CanEditPreparation) return false;
            if (sourceIndex == targetIndex || sourceIndex < 0 || targetIndex < 0 ||
                sourceIndex >= bag.Count || targetIndex >= bag.Count) return false;
            var source = bag[sourceIndex]; var target = bag[targetIndex];
            if (target.definition.itemId == "super_injector" && source.definition.kind == PrototypeItemKind.Injection) return AbsorbPotion(sourceIndex,target);
            if (target.definition.itemId == "devourer" && source.definition.kind == PrototypeItemKind.Equipment) return AbsorbEquipment(sourceIndex,target);
            if (!CanMerge(source, target)) { Notice = "需要两件同名、同等级的装备"; return false; }
            Merge(source, target); bag.RemoveAt(sourceIndex);
            Notice = "合成成功：" + target.Name;ItemAnimationFeedback?.Invoke(target.definition); Changed(); return true;
        }

        private bool MergeIntoSlot(int bagIndex, int slotIndex)
        {
            var source = bag[bagIndex]; var target = equipped[slotIndex];
            Merge(source, target); bag.RemoveAt(bagIndex);
            Notice = "合成成功：" + target.Name;ItemAnimationFeedback?.Invoke(target.definition); Changed(); return true;
        }

        private static bool CanMerge(PrototypeItemInstance a, PrototypeItemInstance b) =>
            a.definition.kind == PrototypeItemKind.Equipment && !a.definition.bossItem && a.definition.fixedLevel == 0 &&
            a.definition.itemId == b.definition.itemId && a.level == b.level && a.level < 3;

        private static void Merge(PrototypeItemInstance source, PrototypeItemInstance target)
        {
            target.MergeModifiersFrom(source);
            target.level++;
            target.investedGold += source.investedGold;
            target.productionProgress += source.productionProgress;
            target.permanentDamage += source.permanentDamage;
            target.lastSwallowDay = Math.Max(target.lastSwallowDay, source.lastSwallowDay);
            target.inheritedHealth += source.inheritedHealth; target.inheritedSpeed += source.inheritedSpeed;
            target.inheritedImpact += source.inheritedImpact; target.inheritedReduction += source.inheritedReduction;
            target.GoldRemainder += source.GoldRemainder;
            foreach (var program in source.inherited) PrototypeEffectMath.MergeProgram(target.inherited,program,1);
            foreach (var recipe in source.recipes) PrototypeEffectMath.MergeRecipe(target.recipes,recipe.stat,recipe.lifetime,recipe.amount);
        }

        private bool Breakthrough(int bagIndex, int slotIndex)
        {
            var target = equipped[slotIndex];
            if (target.level != 3 || target.breakthroughGranted)
            { Notice = "突破核心只能用于三级装备"; return false; }
            target.level = 4; target.breakthroughGranted = true;
            target.investedGold += bag[bagIndex].investedGold; bag.RemoveAt(bagIndex);
            GrantPermanent(PrototypeStat.Health,10,"breakthrough:" + target.instanceId);
            Notice = "突破成功：" + target.Name + "；本局生命永久 +10";ItemAnimationFeedback?.Invoke(Find("break_core"));
            Changed(); return true;
        }

        private bool SwallowIntoSlot(int bagIndex,int slotIndex) => AbsorbEquipment(bagIndex,equipped[slotIndex]);
        private bool AbsorbEquipment(int bagIndex,PrototypeItemInstance target)
        {
            var source=bag[bagIndex]; if (ReferenceEquals(source,target)) return false;
            float r=PrototypeEffectMath.Absorption(target.level);var d=source.definition;
            target.inheritedHealth += (d.healthBonus*source.LevelMultiplier+source.inheritedHealth)*r;
            target.inheritedSpeed += (d.speedBonus*source.LevelMultiplier+source.inheritedSpeed)*r;
            target.inheritedImpact += (d.impactBonus*source.EffectMultiplier+source.permanentDamage+source.inheritedImpact)*r;
            target.inheritedReduction += (d.impactTakenReduction*source.EffectMultiplier+source.inheritedReduction)*r;
            // Flatten programs rather than keeping references to consumed instances.
            if (d.itemId != "devourer")
                PrototypeEffectMath.MergeProgram(target.inherited,source.CreateInheritedProgram(),r);
            foreach (var program in source.inherited) PrototypeEffectMath.MergeProgram(target.inherited,program,r);
            foreach (var recipe in source.recipes) PrototypeEffectMath.MergeRecipe(target.recipes,recipe.stat,recipe.lifetime,recipe.amount*r);
            bag.RemoveAt(bagIndex);Notice=$"{target.Name} 吸收 {source.Name} 的 {r*100:0}% 效果（不限次数）";ItemAnimationFeedback?.Invoke(target.definition);
            Changed();return true;
        }
        private bool AbsorbPotion(int bagIndex,PrototypeItemInstance target)
        {
            var source=bag[bagIndex];var d=source.definition;
            PrototypeEffectMath.MergeRecipe(target.recipes,d.injectionStat,d.potionLifetime,d.injectionAmount*PrototypeEffectMath.Absorption(target.level));
            bag.RemoveAt(bagIndex);Notice=target.Name+" 已保存配方："+d.displayName;ItemAnimationFeedback?.Invoke(target.definition);Changed();return true;
        }
        public void GrantPermanent(PrototypeStat stat,float amount,string source)
        {
            if (amount<=0) return;
            if (stat == PrototypeStat.Health) BodyHealth+=amount;
            if (combatActive)
            {
                string key=source+"|"+(int)stat;
                permanentCombatGrants[key]=permanentCombatGrants.TryGetValue(key,out float old) ? old+amount : amount;
                combatDirty=true;return;
            }
            AddPermanentRecord(stat,amount,source);
        }
        private void AddPermanentRecord(PrototypeStat stat,float amount,string source)
        {
            for (int i=0;i<permanentModifiers.Count;i++) if (permanentModifiers[i].sourceId==source && permanentModifiers[i].stat==stat)
            { var m=permanentModifiers[i];m.amount+=amount;permanentModifiers[i]=m;return; }
            permanentModifiers.Add(new PrototypeStatModifier {sourceId=source,stat=stat,amount=amount,scope=PrototypeModifierScope.RunPermanent});
        }
        public bool UseInjection(int bagIndex)
        {
            if (!CanEditPreparation) return false;
            if (combatActive || bagIndex<0 || bagIndex>=bag.Count) return false;
            var item=bag[bagIndex];var d=item.definition;
            if (d.kind!=PrototypeItemKind.Injection) { Notice="这不是药剂";return false; }
            if (d.potionLifetime==PrototypePotionLifetime.NextBattle)
                pendingBattleModifiers.Add(new PrototypePendingModifier {stat=d.injectionStat,amount=d.injectionAmount});
            else if (d.injectionStat==PrototypeStat.Gold) Gold+=Mathf.RoundToInt(d.injectionAmount);
            else GrantPermanent(d.injectionStat,d.injectionAmount,d.itemId);
            string effect=PrototypeItemDefinition.StatName(d.injectionStat)+" +"+PrototypeItemDefinition.FormatAmount(d.injectionStat,d.injectionAmount)+(d.potionLifetime==PrototypePotionLifetime.NextBattle?"（下一场）":"（本局）");
            bag.RemoveAt(bagIndex);Notice=d.displayName+"："+effect;Feedback?.Invoke(effect);ItemAnimationFeedback?.Invoke(d);
            Changed();return true;
        }

        public bool Unequip(int slotIndex)
        {
            if (!CanEditPreparation) return false;
            if (!IsSlotOpen(slotIndex) || equipped[slotIndex] == null) return false;
            if (bag.Count >= 12) { Notice = "背包已满"; return false; }
            bag.Add(equipped[slotIndex]);
            Notice = "卸下：" + equipped[slotIndex].Name;
            equipped[slotIndex] = null; Changed(); return true;
        }

        public bool Sell(int bagIndex)
        {
            if (!CanEditPreparation) return false;
            if (bagIndex < 0 || bagIndex >= bag.Count) return false;
            var item = bag[bagIndex]; int price = item.SellPrice;
            Gold += price; bag.RemoveAt(bagIndex);
            Notice = "出售：" + item.Name + "，获得 " + price + " 金币";Feedback?.Invoke("金币 +"+price);ItemAnimationFeedback?.Invoke(item.definition);
            Changed(); return true;
        }

        // Existing saved upgrade button is rebound to a gold-funded level upgrade.
        public bool Expand()
        {
            if (!CanEditPreparation) return false;
            if (combatActive || Gold<UpgradeCost) {Notice="升级金币不足";return false;}
            Gold-=UpgradeCost;Level++;BodyHealth*=1.1f;
            if (Level==10) RewardItem("break_core");
            Notice=$"升至 {Level} 级；本体生命 +10%，商店等级 {ShopTierLevel}（最高7，低稀有度保留）";Changed();return true;
        }
        public bool StartCombat(string battleId = null)
        {
            if (Phase != PrototypeRunPhase.Preparation && Phase != PrototypeRunPhase.Aiming) return false;
            if (GameOver || Parked || (Cleared && !Endless)) return false;
            battleId = string.IsNullOrEmpty(battleId) ? Guid.NewGuid().ToString("N") : battleId;
            if (committedBattles.Contains(battleId)) return false;
            ActiveBattleId = battleId;
            pickaxeHits=CombatOre=0;oreCredit=0;combatGold=0;combatProducts.Clear();permanentCombatGrants.Clear();
            Phase=PrototypeRunPhase.Fighting;combatDirty=false;pendingBattleModifiers.Clear();
            // A consumed next-battle potion cannot be replayed on reload of an in-progress fight.
            Version++;Save();
            return true;
        }
        public void MarkCombatDirty() { combatDirty=true; }
        public void RecordCombatGold(int amount) {combatGold+=amount;combatDirty=true;}
        public void RecordProduction(PrototypeItemInstance producer,string productId,bool randomPotion,int count)
        {
            if (count<=0) return;
            string[] ids={"shield_potion","swift_potion","attack_potion","guard_potion","giant_potion"};
            if (!randomPotion)
            {
                var product=Find(productId);
                if (product==null || (product.acquisition==PrototypeItemAcquisition.WildOnly || product.bossExclusive)) return;
                combatProducts[productId]=combatProducts.TryGetValue(productId,out int old)?old+count:count;
                LogProduction(producer,productId,count);
            }
            else for (int n=0;n<count;n++)
            { string id=ids[random.Next(ids.Length)];combatProducts[id]=combatProducts.TryGetValue(id,out int old)?old+1:1;LogProduction(producer,id,1); }
            combatDirty=true;
        }
        private void LogProduction(PrototypeItemInstance producer,string id,int count)
        {string key=(producer?.definition.displayName??"生产道具")+" → "+(Find(id)?.displayName??id);productionLog[key]=productionLog.TryGetValue(key,out var old)?old+count:count;}
        public void RecordPickaxeHit(PrototypeItemInstance weapon)
        {
            if (weapon?.definition.itemId == "pickaxe") RecordOreCredit(1);
        }
        public int RecordOreCredit(float credit)
        {
            oreCredit+=credit;
            int whole=Mathf.FloorToInt(oreCredit);oreCredit-=whole;CombatOre+=whole;combatDirty=true;return whole;
        }
        public void CheckpointCombat()
        {
            if(!combatActive) return;
            FlushPermanentGrowth();Save();combatDirty=false;
        }
        private void FlushPermanentGrowth()
        {
            foreach(var pair in permanentCombatGrants)
            {int split=pair.Key.LastIndexOf('|');AddPermanentRecord((PrototypeStat)int.Parse(pair.Key.Substring(split+1)),pair.Value,pair.Key.Substring(0,split));}
            permanentCombatGrants.Clear();
        }
        public void CommitCombat()
        {
            if (!combatActive || !committedBattles.Add(ActiveBattleId)) return;
            if (Phase != PrototypeRunPhase.Resolving) Phase=PrototypeRunPhase.Preparation;
            FlushPermanentGrowth();
            foreach(var entry in productionLog)AddLog(entry.Key+" ×"+entry.Value);productionLog.Clear();
            if(CombatOre>0)AddLog("矿镐生产：金矿 ×"+CombatOre);
            if(combatGold>0)AddLog("战斗经济：金币 +"+combatGold);
            Gold+=combatGold;combatGold=0;
            AddOreCore(CombatOre);CombatOre=0;
            foreach (var pair in combatProducts) for (int n=0;n<pair.Value;n++)
            {
                var d=Find(pair.Key);if(d==null || (d.acquisition==PrototypeItemAcquisition.WildOnly || d.bossExclusive)) continue;var item=NewItem(d);
                if (bag.Count<12) bag.Add(item);else pendingRewards.Add(item);
            }
            combatProducts.Clear();Changed();combatDirty=false;
        }

        private void AddOreCore(int count)
        {
            if(count<=0)return;
            foreach(var item in bag)if(item.definition.itemId=="gold_ore"){item.quantity+=count;return;}
            foreach(var item in pendingRewards)if(item.definition.itemId=="gold_ore"){item.quantity+=count;return;}
            var d=Find("gold_ore");if(d==null)throw new InvalidOperationException("Missing gold ore definition");
            var ore=NewItem(d);ore.quantity=count;if(bag.Count<12)bag.Add(ore);else pendingRewards.Add(ore);
        }
        public bool SellOre()
        {
            if (!CanEditPreparation) return false;
            int count=OreCount;if(count<=0)return false;
            bag.RemoveAll(i=>i.definition.itemId=="gold_ore");pendingRewards.RemoveAll(i=>i.definition.itemId=="gold_ore");
            Gold+=count*2;Notice=$"出售金矿 ×{count}，获得 {count*2} 金币";Feedback?.Invoke($"金币 +{count*2}");ItemAnimationFeedback?.Invoke(Find("gold_ore"));Changed();return true;
        }
        public bool MoveBag(int source,int target)
        {
            if (!CanEditPreparation) return false;
            if(source<0||source>=bag.Count||source==target)return false;
            if(target>=0&&target<bag.Count){var a=bag[source];var b=bag[target];
                if(a.definition.itemId=="gold_ore" && b.definition.itemId=="gold_ore"){b.quantity+=a.quantity;bag.RemoveAt(source);Changed();return true;}
                if(CanMerge(a,b)||(b.definition.itemId=="super_injector"&&a.definition.kind==PrototypeItemKind.Injection)||(b.definition.itemId=="devourer"&&a.definition.kind==PrototypeItemKind.Equipment))return MergeBag(source,target);
                bag[source]=b;bag[target]=a;
            }else{var item=bag[source];bag.RemoveAt(source);bag.Add(item);}Changed();return true;
        }
        public bool MoveEquipped(int source,int target)
        {
            if (!CanEditPreparation) return false;if(!IsSlotOpen(source)||!IsSlotOpen(target)||source==target||equipped[source]==null)return false;var item=equipped[source];equipped[source]=equipped[target];equipped[target]=item;Changed();return true;}
        public bool SellEquipped(int slot)
        {
            if (!CanEditPreparation) return false;
            if(!IsSlotOpen(slot)||equipped[slot]==null)return false;var item=equipped[slot];int price=item.SellPrice;equipped[slot]=null;
            Gold+=price;Notice=$"出售：{item.Name}，获得 {price} 金币";Feedback?.Invoke($"金币 +{price}");ItemAnimationFeedback?.Invoke(item.definition);Changed();return true;
        }
        private void EndDayCore(bool won)
        {
            const int baseIncome = 30;
            int prosperity = 0;
            for (int i = 0; i < equipped.Length; i++)
                if (equipped[i] != null && slotEffects[i] == PrototypeSlotEffect.Prosperity)
                    prosperity += 2;
            Gold += baseIncome + (won ? 4 : 0) + prosperity;
            Day++;
            Stage = PrototypeDayStage.EventOne;
            EventChosen = false;
            ShopAvailable = false;
            SelectedMonsterTier = -1;
            RollMonsterOffers();
            CombatOre = 0;
            RestockCore();
            Notice = $"日终收入 {baseIncome} 金币" + (won ? "，胜利奖励 4 金币" : "") +
                (prosperity > 0 ? "，格位收益 +" + prosperity : "") +
                (pendingRewards.Count > 0 ? "，背包已满，产物在待领取区" : "");
            Changed();
        }

        public void MarkStarted() { if (Started || combatActive) return; Started=true; Parked=false; Changed(); }
        public void Park()
        {
            if(Parked) return;
            if (combatActive) throw new InvalidOperationException("战斗中不能封存");
            if (!transient) PrototypeRunHistory.Store(this, GameOver ? "失败" : "封存");
            Parked=true; Changed(); FlushSave();
        }
        public void HealEndurance(int amount)
        {
            if (!CanEditPreparation) return;
            if (GameOver || amount <= 0) return;
            Endurance=Mathf.Min(10,Endurance+amount); Changed();
            // LastStandUsed is deliberately never changed by healing.
        }
        public static PrototypeRun FromHistorySnapshot(string json, string saveOverride = null)
        {
            var run=new PrototypeRun(false,false,saveOverride,true);
            if(!run.ImportSnapshot(json)) return null;
            run.Started=true; run.Parked=false; run.Persist(); return run;
        }
        public bool ImportSnapshot(string json)
        { return !combatActive && Phase != PrototypeRunPhase.Aiming && TryLoad(json); }
        public void Persist() { Save(); FlushSave(); }
        public void FlushSave() {PrototypeRunSaveWriter.Flush(savePath);}
        public PrototypeStatSnapshot CalculateStats() => PrototypeStatCalculator.Calculate(this);

        public void GetWeapons(out PrototypeItemInstance melee, out PrototypeItemInstance ranged)
        {
            melee = null; ranged = null;
            for (int i = 0; i < equipped.Length; i++)
            {
                var item = equipped[i];
                if (item == null || item.definition.weapon == null) continue;
                if (item.definition.weapon.kind == PrototypeWeaponKind.Melee && melee == null) melee = item;
                else if (item.definition.weapon.kind == PrototypeWeaponKind.Ranged && ranged == null) ranged = item;
            }
        }

        public bool Adjacent(string idA, string idB)
        {
            List<int> active = new List<int>(10);
            for (int i = 0; i < 10; i++) if (IsSlotOpen(i)) active.Add(i);
            for (int i = 0; i < active.Count; i++)
            {
                var a = equipped[active[i]]; var b = equipped[active[(i + 1) % active.Count]];
                if (a == null || b == null) continue;
                if ((HasIdentity(a,idA) && HasIdentity(b,idB)) ||
                    (HasIdentity(a,idB) && HasIdentity(b,idA))) return true;
            }
            return false;
        }

        private static bool HasIdentity(PrototypeItemInstance item,string id)
        {
            if(item.definition.itemId==id) return true;
            foreach(var p in item.inherited) if(p.definitionId==id) return true;
            return false;
        }
        private PrototypeItemInstance NewItem(PrototypeItemDefinition definition) => new PrototypeItemInstance
        {
            instanceId = nextId++, definition = definition, level = definition.ResolveLevel(1),
            investedGold = definition.price
        };

        private void Changed() { Version++; Save(); }

    }
}
