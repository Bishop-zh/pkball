using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PKBall.Prototype
{
    public sealed partial class PrototypeRun
    {
        [Serializable] private sealed class SavedItem
        {
            public long id; public string definitionId; public int level;public int quantity=1;
            public int investedGold; public float permanentDamage; public bool breakthroughGranted;
            public int lastSwallowDay; public int productionProgress;
            public float growth, cooldownMultiplier, inheritedHealth, inheritedSpeed, inheritedImpact, inheritedReduction, goldRemainder;
            public List<PrototypeItemModifier> modifiers; public bool modifierMigrationDone;
            public List<PrototypeInheritedProgram> inherited;
            public List<PrototypePotionRecipe> recipes;
        }
        [Serializable] private sealed class SaveData
        {
            public int version = 8;
            public string activeBattleId;
            public List<string> committedBattles;
            public List<string> activityLog; public List<string> productionSources;public List<int> productionAmounts;
            public string runId; public bool started, parked; public int lastDefeatCost;
            public int level; public float bodyHealth;
            public List<PrototypePendingModifier> nextBattle;
            public bool combatActive;public int combatGold,combatOre;
            public List<string> productIds=new List<string>();public List<int> productCounts=new List<int>(); public int day, gold, xp, slots, oreCount; public long nextId;
            public int endurance, wins, stage, selectedMonsterTier;
            public int[] monsterOffers;
            public string[] monsterOfferIds;
            public PrototypeMonsterPreset[] customMonsterOffers;
            public bool lastStandUsed, endless, gameOver, eventChosen, shopAvailable;
            public int[] slotEffects;
            public List<SavedItem> bag = new List<SavedItem>();
            public List<SavedItem> pendingRewards = new List<SavedItem>();
            public List<SavedItem> equipped = new List<SavedItem>();
            public List<string> shop = new List<string>();
            public List<PrototypeStatModifier> permanent = new List<PrototypeStatModifier>();
        }

        private static SavedItem ToSaved(PrototypeItemInstance item) => item == null ? null : new SavedItem
        {
            id = item.instanceId, definitionId = item.definition.itemId, level = item.level,quantity=item.quantity,
            investedGold = item.investedGold, permanentDamage = item.permanentDamage,
            breakthroughGranted = item.breakthroughGranted, lastSwallowDay = item.lastSwallowDay,
            productionProgress = item.productionProgress, growth=item.growth, cooldownMultiplier=item.cooldownMultiplier,
            inheritedHealth=item.inheritedHealth,inheritedSpeed=item.inheritedSpeed,inheritedImpact=item.inheritedImpact,
            inheritedReduction=item.inheritedReduction,goldRemainder=item.GoldRemainder,
            modifiers=new List<PrototypeItemModifier>(item.modifiers),modifierMigrationDone=item.modifierMigrationDone,
            inherited=new List<PrototypeInheritedProgram>(item.inherited),recipes=new List<PrototypePotionRecipe>(item.recipes)
        };

        private PrototypeItemInstance FromSaved(SavedItem saved)
        {
            var definition=saved==null?null:Find(saved.definitionId);if(definition==null) return null;
            var item=new PrototypeItemInstance {instanceId=saved.id,definition=definition,quantity=Mathf.Max(1,saved.quantity),level=definition.ResolveLevel(saved.level),
                investedGold=saved.investedGold,permanentDamage=saved.permanentDamage,breakthroughGranted=saved.breakthroughGranted,
                lastSwallowDay=saved.lastSwallowDay,productionProgress=saved.productionProgress,growth=saved.growth,
                cooldownMultiplier=saved.cooldownMultiplier>0?saved.cooldownMultiplier:1,
                inheritedHealth=saved.inheritedHealth,inheritedSpeed=saved.inheritedSpeed,inheritedImpact=saved.inheritedImpact,
                inheritedReduction=saved.inheritedReduction,GoldRemainder=saved.goldRemainder};
            if(saved.modifiers!=null) item.modifiers.AddRange(saved.modifiers);
            item.modifierMigrationDone=saved.modifierMigrationDone;
            item.EnsureModifiers();
            if(saved.inherited!=null) item.inherited.AddRange(saved.inherited);
            if(saved.recipes!=null) item.recipes.AddRange(saved.recipes);
            return item;
        }

        public string ExportSnapshot()
        {
                SaveData data = new SaveData
                { activeBattleId=ActiveBattleId, committedBattles=new List<string>(committedBattles), activityLog=new List<string>(ActivityLog),productionSources=new List<string>(productionLog.Keys),productionAmounts=new List<int>(productionLog.Values), runId=RunId, started=Started, parked=Parked, lastDefeatCost=LastDefeatCost, day = Day, gold = Gold, xp = Xp, slots = Slots, oreCount = 0, nextId = nextId,
                    endurance = Endurance, wins = Wins, stage = (int)Stage,
                    selectedMonsterTier = SelectedMonsterTier, lastStandUsed = LastStandUsed,
                    monsterOffers = (int[])monsterOffers.Clone(),monsterOfferIds=(string[])monsterOfferIds.Clone(),
                    customMonsterOffers=(PrototypeMonsterPreset[])customMonsterOffers.Clone(),
                    endless = Endless, gameOver = GameOver, eventChosen = EventChosen,
                    shopAvailable = ShopAvailable, slotEffects = new int[10], level=Level,bodyHealth=BodyHealth,
                    nextBattle=new List<PrototypePendingModifier>(pendingBattleModifiers),combatActive=Phase==PrototypeRunPhase.Fighting && !committedBattles.Contains(ActiveBattleId),combatGold=combatGold,combatOre=CombatOre };
                foreach(var product in combatProducts) {data.productIds.Add(product.Key);data.productCounts.Add(product.Value);}
                for (int i = 0; i < 10; i++) data.slotEffects[i] = (int)slotEffects[i];
                foreach (var item in bag) data.bag.Add(ToSaved(item));
                foreach (var item in pendingRewards) data.pendingRewards.Add(ToSaved(item));
                foreach (var item in equipped) data.equipped.Add(ToSaved(item));
                foreach (var item in shop) data.shop.Add(item == null ? "" : item.itemId);
                data.permanent.AddRange(permanentModifiers);
                return JsonUtility.ToJson(data,true);
        }
        private void Save()
        {
            if (transient || Phase == PrototypeRunPhase.Resolving) return;
            try { PrototypeRunSaveWriter.Enqueue(savePath,ExportSnapshot()); }
            catch (Exception error) { Debug.LogError("PKBall save failed: " + error); }
        }

        private List<PrototypeItemInstance> StageSavedItems(List<SavedItem> items, bool allowEmpty, HashSet<long> ids, ref long maximumId)
        {
            var result = new List<PrototypeItemInstance>();
            if (items == null) return result;
            foreach (var saved in items)
            {
                // JsonUtility can materialize a serialized null slot as an empty inline object.
                if (saved == null || (allowEmpty && saved.id == 0 && string.IsNullOrEmpty(saved.definitionId)))
                {
                    if (!allowEmpty) throw new FormatException("Null inventory item");
                    result.Add(null);
                    continue;
                }
                if (saved.id <= 0 || saved.id == long.MaxValue || !ids.Add(saved.id))
                    throw new FormatException("Invalid or duplicate item instance ID: " + saved.id);
                if (Find(saved.definitionId) == null)
                    throw new FormatException("Unknown item definition: " + saved.definitionId);
                maximumId = Math.Max(maximumId, saved.id);
                result.Add(FromSaved(saved));
            }
            return result;
        }

        private bool LoadOrPreserveRejectedSave()
        {
            if (TryLoad()) return true;
            if (File.Exists(savePath))
            {
                // Preserve rejected bytes before constructor checkpoints can replace the file.
                // If preservation fails, propagate the IO error and do not create a new save.
                string recoveryPath = savePath + ".rejected-" + Guid.NewGuid().ToString("N") + ".json";
                File.Copy(savePath, recoveryPath, false);
                Debug.LogWarning("PKBall rejected save preserved at: " + recoveryPath);
            }
            return false;
        }

        private bool TryLoad(string snapshot = null)
        {
            if (snapshot == null && !File.Exists(savePath)) return false;
            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(snapshot ?? File.ReadAllText(savePath));
                if (data == null || (data.version < 1 || data.version > 8) || data.slots < 4 || data.slots > 10 || data.gold < 0)
                    return false;
                var loadedBattles = new HashSet<string>();
                if (data.version >= 8)
                {
                    if (data.committedBattles != null) foreach (var id in data.committedBattles)
                        if (string.IsNullOrEmpty(id) || !loadedBattles.Add(id)) return false;
                    if (data.combatActive && (string.IsNullOrEmpty(data.activeBattleId) || loadedBattles.Contains(data.activeBattleId))) return false;
                }
                if ((data.equipped != null && data.equipped.Count > equipped.Length) ||
                    (data.shop != null && data.shop.Count > shop.Length) || data.nextId == long.MaxValue)
                    return false;
                // Validate identities and deserialize every item before touching the current inventory.
                var ids = new HashSet<long>();
                long maximumId = 0;
                var loadedBag = StageSavedItems(data.bag, false, ids, ref maximumId);
                var loadedRewards = StageSavedItems(data.pendingRewards, false, ids, ref maximumId);
                var loadedEquipment = StageSavedItems(data.equipped, true, ids, ref maximumId);
                var loadedMonsters = new PrototypeMonsterPreset[3];
                if (data.version >= 7 && data.customMonsterOffers != null)
                {
                    if (data.customMonsterOffers.Length != 3) return false;
                    for (int i=0;i<3;i++)
                    {
                        var preset=data.customMonsterOffers[i];
                        if (preset==null) continue;
                        // JsonUtility also materializes null inline monster slots as empty objects.
                        if (string.IsNullOrEmpty(preset.id))
                        {
                            if(data.monsterOfferIds!=null && data.monsterOfferIds.Length==3 && Guid.TryParseExact(data.monsterOfferIds[i],"N",out _))
                                throw new FormatException("Custom monster snapshot is missing its identity");
                            continue;
                        }
                        preset.Validate();
                        if ((int)preset.tier != i) return false;
                        loadedMonsters[i]=preset.Copy();
                    }
                }
                bag.Clear(); pendingRewards.Clear(); permanentModifiers.Clear(); pendingBattleModifiers.Clear();
                Array.Clear(equipped,0,equipped.Length); Array.Clear(shop,0,shop.Length);
                Phase=PrototypeRunPhase.Preparation; combatDirty=false; combatGold=0; CombatOre=0; oreCredit=0;
                committedBattles.Clear(); committedBattles.UnionWith(loadedBattles);
                ActiveBattleId=data.activeBattleId;
                combatProducts.Clear(); permanentCombatGrants.Clear();
                loadNeedsUpgrade=data.version<8 || string.IsNullOrEmpty(data.runId);
                RunId=string.IsNullOrEmpty(data.runId) ? Guid.NewGuid().ToString("N") : data.runId;
                Started=data.version<3 || data.started; Parked=data.parked; LastDefeatCost=data.lastDefeatCost;
                Day = Mathf.Max(1, data.day); Gold = data.gold; Xp = Mathf.Max(0, data.xp);
                Slots = Mathf.Max(8,data.slots);
                Level=Mathf.Max(1,data.level);BodyHealth=data.bodyHealth>0?data.bodyHealth:120;
                if(data.version<4)BodyHealth+=380*Mathf.Pow(1.1f,Level-1);
                ActivityLog.Clear();if(data.activityLog!=null)ActivityLog.AddRange(data.activityLog.GetRange(Mathf.Max(0,data.activityLog.Count-80),Mathf.Min(80,data.activityLog.Count)));
                productionLog.Clear();if(data.productionSources!=null && data.productionAmounts!=null)for(int i=0;i<Mathf.Min(data.productionSources.Count,data.productionAmounts.Count);i++)productionLog[data.productionSources[i]]=data.productionAmounts[i];
                if(data.nextBattle!=null) pendingBattleModifiers.AddRange(data.nextBattle); int legacyOre=data.version<5?Mathf.Max(0,data.oreCount):0;
                Endurance = data.version>=3 || data.endurance > 0 || data.gameOver ? Mathf.Clamp(data.endurance, 0, 10) : 10;
                Wins = Mathf.Max(0, data.wins);
                Stage = (PrototypeDayStage)Mathf.Clamp(data.stage, 0, 5);
                SelectedMonsterTier = data.selectedMonsterTier >= 0 && data.selectedMonsterTier <= 2 &&
                    Stage == PrototypeDayStage.Monster ? data.selectedMonsterTier : -1;
                for(int i=0;i<3;i++){monsterOffers[i]=-1;monsterOfferIds[i]=null;customMonsterOffers[i]=null;}
                if(data.version>=7)
                {
                    if(data.monsterOfferIds!=null && data.monsterOfferIds.Length==3)Array.Copy(data.monsterOfferIds,monsterOfferIds,3);
                    Array.Copy(loadedMonsters,customMonsterOffers,3);
                }
                if (data.monsterOffers != null && data.monsterOffers.Length == 3)
                    for (int i = 0; i < 3; i++) monsterOffers[i] = data.monsterOffers[i];
                LastStandUsed = data.lastStandUsed;
                Endless = data.endless;
                GameOver = data.gameOver || Endurance==0;
                EventChosen = data.eventChosen;
                ShopAvailable = data.shopAvailable;
                if (data.slotEffects != null && data.slotEffects.Length == 10)
                    for (int i = 0; i < 10; i++) slotEffects[i] =
                        (PrototypeSlotEffect)Mathf.Clamp(data.slotEffects[i], 0, 6);
                nextId = Math.Max(Math.Max(1, data.nextId), maximumId + 1);
                bag.AddRange(loadedBag);
                pendingRewards.AddRange(loadedRewards);
                for (int i = 0; i < loadedEquipment.Count; i++) equipped[i] = loadedEquipment[i];
                if (data.shop != null)
                    for (int i = 0; i < Mathf.Min(4, data.shop.Count); i++) {var loaded=Find(data.shop[i]);shop[i]=loaded!=null && loaded.acquisition!=PrototypeItemAcquisition.WildOnly && !loaded.bossExclusive ? loaded : null;}
                if (data.permanent != null) permanentModifiers.AddRange(data.permanent);
                if(data.version==1) foreach(var m in permanentModifiers) if(m.stat==PrototypeStat.Health) BodyHealth+=m.amount;
                if(legacyOre>0)AddOreCore(legacyOre);
                if(data.combatActive)
                {
                    Phase=PrototypeRunPhase.Fighting;
                    if (string.IsNullOrEmpty(ActiveBattleId)) ActiveBattleId="legacy:"+RunId+":"+Day+":"+Stage;
                    combatGold=Mathf.Max(0,data.combatGold);CombatOre=Mathf.Max(0,data.combatOre);
                    if(data.productIds!=null && data.productCounts!=null)
                        for(int i=0;i<Mathf.Min(data.productIds.Count,data.productCounts.Count);i++)
                        {var product=Find(data.productIds[i]);if(product!=null && !(product.acquisition==PrototypeItemAcquisition.WildOnly || product.bossExclusive))combatProducts[data.productIds[i]]=Mathf.Max(0,data.productCounts[i]);}
                    CommitCombat();
                }
                Notice = "已恢复第 " + Day + " 天的本局进度";
                if (GameOver || (Cleared && !Endless)) Phase=PrototypeRunPhase.Ended;
                return true;
            }
            catch (Exception error) { Debug.LogWarning("PKBall save load failed: " + error); return false; }
        }
    }
}
