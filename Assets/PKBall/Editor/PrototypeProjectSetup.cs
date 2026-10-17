using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PKBall.Prototype.Editor
{
    public static class PrototypeProjectSetup
    {
        private const string Root = "Assets/PKBall";
        private const string ScenePath = Root + "/Scenes/PrototypeBattle.unity";

        [MenuItem("PKBall/创建或更新战斗原型场景")]
        public static void Create()
        {
            EnsureFolder(Root + "/Scenes");
            EnsureFolder(Root + "/Resources/Weapons");
            EnsureFolder(Root + "/Resources/Items");
            EnsureFolder(Root + "/Resources/Visuals");
            EnsureBalance();
            EnsurePrototypeMaterial();
            EnsureWeapon("PlayerMelee", "冲刺矛", PrototypeWeaponKind.Melee, 1.4f, 8f, .4f, 0f,
                new Color(1f, 0.72f, 0.22f), true, 1.3f);
            EnsureWeapon("Pickaxe", "矿镐", PrototypeWeaponKind.Melee, 1.8f, 5f, .35f, 0f,
                new Color(0.8f, 0.67f, 0.38f), false, 1f);
            EnsureWeapon("Hammer", "重锤", PrototypeWeaponKind.Melee, 2.1f, 12f, .38f, 0f,
                new Color(.96f, .54f, .29f), true, 1.15f);
            EnsureWeapon("Rapier", "轻剑", PrototypeWeaponKind.Melee, .85f, 5f, .3f, 0f,
                new Color(.7f, .85f, .95f), false, 1f);
            EnsureWeapon("PlayerRanged", "追踪弩", PrototypeWeaponKind.Ranged, 4.2f, 7f, 14f, 10f,
                new Color(0.32f, 0.84f, 1f), false, 1f);
            EnsureWeapon("EnemyMelee", "重拳", PrototypeWeaponKind.Melee, 1.5f, 16f, .4f, 0f,
                new Color(1f, 0.42f, 0.35f), true, 1.2f);
            EnsureWeapon("EnemyRanged", "蓄能枪", PrototypeWeaponKind.Ranged, 4.8f, 9f, 14f, 9f,
                new Color(1f, 0.7f, 0.4f), false, 1f);
            EnsureWeapon("Greatsword", "巨刃", PrototypeWeaponKind.Melee, 2.5f, 18f, .42f, 0f,
                new Color(.95f, .64f, .36f), false, 1f);
            EnsureWeapon("Halberd", "破阵戟", PrototypeWeaponKind.Melee, 1.8f, 15f, .6f, 0f,
                new Color(.76f, .88f, 1f), true, 1.2f);
            EnsureWeapon("ArcCannon", "电弧炮", PrototypeWeaponKind.Ranged, 5f, 12f, 14f, 10f,
                new Color(.34f, .87f, 1f), false, 1f);
            EnsureWeapon("Repulsor", "斥力锤", PrototypeWeaponKind.Melee, 2f, 8f, .45f, 0f,
                new Color(.37f, .85f, 1f), false, 1f);
            ConfigureWeaponShape("PlayerMelee", PrototypeAttackShape.Thrust, 25f, .7f);
            ConfigureWeaponShape("Pickaxe", PrototypeAttackShape.Smash, 48f, .8f);
            ConfigureWeaponShape("Hammer", PrototypeAttackShape.Smash, 55f, 2.1f);
            ConfigureWeaponShape("Rapier", PrototypeAttackShape.Sweep, 85f, .45f);
            ConfigureWeaponShape("EnemyMelee", PrototypeAttackShape.Smash, 60f, .9f);
            ConfigureWeaponShape("Greatsword", PrototypeAttackShape.Sweep, 95f, 1.2f);
            ConfigureWeaponShape("Halberd", PrototypeAttackShape.Thrust, 32f, 1.3f);
            ConfigureWeaponShape("Repulsor", PrototypeAttackShape.Smash, 58f, 3.5f);
            EnsureItem("spear", "冲刺矛", "近战攻击；等级伤害 8/16/32", "spear", 10, 0, 0, 0f, 0f, "PlayerMelee");
            EnsureItem("crossbow", "追踪弩", "追踪弹丸；等级伤害 7/14/28", "crossbow", 11, 0, 0, 0f, 0f, "PlayerRanged");
            EnsureItem("pickaxe", "矿镐", "近战命中可在战后获得金矿", "pickaxe", 11, 0, 0, 0f, 0f, "Pickaxe");
            EnsureItem("hammer", "重锤", "短距离重击；增加正面撞击", "hammer", 13, 0, 0, 0f, 0f, "Hammer");
            EnsureItem("rapier", "轻剑", "快速短距离挥击", "rapier", 11, 0, 0, 0f, 0f, "Rapier");
            EnsureItem("bulwark", "防壁", "高生命与撞击减伤", "bulwark", 14, 20, 0, 0f, .04f, null);
            EnsureItem("afterburner", "推进器", "速度与撞击增幅", "afterburner", 14, 0, 10, .05f, 0f, null);
            EnsureItem("health_lab", "生命培育舱", "装备后定期生产生命针剂", "health_lab", 12, 0, 0, 0f, 0f, null);
            EnsureItem("strength_lab", "力量蒸馏器", "装备后定期生产力量针剂", "strength_lab", 13, 0, 0, 0f, 0f, null);
            EnsureItem("speed_lab", "迅捷离心机", "装备后定期生产迅捷针剂", "speed_lab", 13, 0, 0, 0f, 0f, null);
            EnsureItem("padding", "缓冲护垫", "降低受到的撞击伤害", "padding", 10, 0, 0, 0f, 0.06f, null);
            EnsureItem("boots", "疾风轮", "提高速度；相邻冲刺矛时命中获得短加速", "boots", 10, 0, 8, 0f, 0f, null);
            EnsureItem("plating", "重型装甲", "提高生命，也提高质量", "plating", 12, 12, 0, 0f, 0f, null);
            EnsureItem("ram", "撞角", "提高撞击伤害", "ram", 12, 0, 0, 0.12f, 0f, null);
            EnsureItem("injector", "生命针剂", "拖到角色身上获得本局永久生命", "injector", 5, 0, 0, 0f, 0f, null);
            EnsureItem("strength_injector", "力量针剂", "拖到角色身上获得本局永久撞击伤害", "strength_injector", 6, 0, 0, 0f, 0f, null);
            EnsureItem("speed_injector", "迅捷针剂", "拖到角色身上获得本局永久速度", "speed_injector", 6, 0, 0, 0f, 0f, null);
            EnsureItem("core", "动力核心", "高速撞击构筑强化", "core", 15, 0, 12, 0.08f, 0f, null);
            EnsureItem("devourer", "吞并齿轮", "装备后将一级装备拖到本格吞并；每天一次，永久强化该齿轮的撞击加成", "devourer", 13, 0, 0, 0f, 0f, null);
            EnsureItem("break_core", "突破核心", "将一件三级装备升到四级", "break_core", 25, 0, 0, 0f, 0f, null);
            EnsureItem("gold_ore", "金矿", "战斗后出售换取金币", "gold_ore", 2, 0, 0, 0f, 0f, null);
            EnsureItem("greatsword", "巨刃", "宽角度挥砍；高级武器预留 3D 模型", "greatsword", 24, 0, 0, 0f, 0f, "Greatsword");
            EnsureItem("halberd", "破阵戟", "长距离直线穿刺；高级武器预留 3D 模型", "halberd", 24, 0, 0, 0f, 0f, "Halberd");
            EnsureItem("arc_cannon", "电弧炮", "远程追踪；高级武器预留 3D 模型", "arc_cannon", 26, 0, 0, 0f, 0f, "ArcCannon");
            EnsureItem("repulsor", "斥力锤", "强击退重击；高级武器预留 3D 模型", "repulsor", 25, 0, 0, 0f, 0f, "Repulsor");
            ConfigureItem("spear", PrototypeItemKind.Equipment, 10, 0, 0, 0f, 0f);
            ConfigureItem("crossbow", PrototypeItemKind.Equipment, 11, 0, 0, 0f, 0f);
            ConfigureItem("pickaxe", PrototypeItemKind.Equipment, 11, 0, 0, 0f, 0f);
            ConfigureItem("hammer", PrototypeItemKind.Equipment, 13, 0, 0, 0f, 0f);
            ConfigureItem("rapier", PrototypeItemKind.Equipment, 11, 0, 0, 0f, 0f);
            ConfigureItem("bulwark", PrototypeItemKind.Equipment, 14, 20, 0, 0f, .04f);
            ConfigureItem("afterburner", PrototypeItemKind.Equipment, 14, 0, 10, .05f, 0f);
            ConfigureItem("health_lab", PrototypeItemKind.Equipment, 12, 0, 0, 0f, 0f);
            ConfigureItem("strength_lab", PrototypeItemKind.Equipment, 13, 0, 0, 0f, 0f);
            ConfigureItem("speed_lab", PrototypeItemKind.Equipment, 13, 0, 0, 0f, 0f);
            ConfigureGenerator("health_lab", "injector");
            ConfigureGenerator("strength_lab", "strength_injector");
            ConfigureGenerator("speed_lab", "speed_injector");
            ConfigureItem("padding", PrototypeItemKind.Equipment, 10, 0, 0, 0f, .06f);
            ConfigureItem("boots", PrototypeItemKind.Equipment, 10, 0, 8, 0f, 0f);
            ConfigureItem("plating", PrototypeItemKind.Equipment, 12, 12, 0, 0f, 0f);
            ConfigureItem("ram", PrototypeItemKind.Equipment, 12, 0, 0, .12f, 0f);
            ConfigureItem("core", PrototypeItemKind.Equipment, 15, 0, 12, .08f, 0f);
            ConfigureItem("devourer", PrototypeItemKind.Equipment, 13, 0, 0, 0f, 0f);
            ConfigureItem("injector", PrototypeItemKind.Injection, 5, 0, 0, 0f, 0f,
                PrototypeStat.Health, 10f);
            ConfigureItem("strength_injector", PrototypeItemKind.Injection, 6, 0, 0, 0f, 0f,
                PrototypeStat.ImpactBonus, .02f);
            ConfigureItem("speed_injector", PrototypeItemKind.Injection, 6, 0, 0, 0f, 0f,
                PrototypeStat.Speed, 3f);
            ConfigureItem("break_core", PrototypeItemKind.Material, 25, 0, 0, 0f, 0f);
            ConfigureItem("gold_ore", PrototypeItemKind.Material, 2, 0, 0, 0f, 0f);
            ConfigureItem("greatsword", PrototypeItemKind.Equipment, 24, 0, 0, 0f, 0f);
            ConfigureItem("halberd", PrototypeItemKind.Equipment, 24, 0, 0, 0f, 0f);
            ConfigureItem("arc_cannon", PrototypeItemKind.Equipment, 26, 0, 0, 0f, 0f);
            ConfigureItem("repulsor", PrototypeItemKind.Equipment, 25, 0, 0, 0f, 0f);
            ConfigureAvailability("greatsword", PrototypeItemRarity.Legendary, 7, 1);
            ConfigureAvailability("halberd", PrototypeItemRarity.Legendary, 7, 1);
            ConfigureAvailability("arc_cannon", PrototypeItemRarity.Legendary, 7, 1);
            ConfigureAvailability("repulsor", PrototypeItemRarity.Legendary, 7, 1);
            ConfigureAvailability("break_core", PrototypeItemRarity.Epic, 5, 1);
            EnsureVisual("player", .22f);
            EnsureVisual("rival", .22f);
            EnsureVisual("monster", .22f);
            EnsureVisual("player_warrior", .22f);
            EnsureVisual("player_mage", .22f);
            EnsureVisual("player_engineer", .22f);
            EnsureVisual("monster_normal", .22f);
            EnsureVisual("monster_elite", .22f);
            EnsureVisual("monster_boss", .22f);
            EnsureVisual("monster_spore", .22f);
            EnsureVisual("monster_spark", .22f);
            EnsureVisual("monster_saw", .22f);
            EnsureVisual("monster_frost", .22f);
            EnsureVisual("monster_furnace", .22f);
            EnsureVisual("monster_storm", .22f);

            PrototypeSceneAuthoring.CreateScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("PKBall prototype scene and editable balance assets are ready: " + ScenePath);
        }

        private static void EnsureBalance()
        {
            string path = Root + "/Resources/PrototypeBalance.asset";
            PrototypeBalance existing = AssetDatabase.LoadAssetAtPath<PrototypeBalance>(path);
            if (existing != null)
            {
                if (Mathf.Approximately(existing.arenaHalfWidth, 8f)) existing.arenaHalfWidth = 5.8f;
                if (Mathf.Approximately(existing.arenaHalfDepth, 5f)) existing.arenaHalfDepth = 3.8f;
                if (existing.wallReturnStrength <= 0f) existing.wallReturnStrength = 0.65f;
                if (existing.wallReturnCooldown <= 0f) existing.wallReturnCooldown = 0.12f;
                EditorUtility.SetDirty(existing);
                return;
            }
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<PrototypeBalance>(), path);
        }

        private static void EnsurePrototypeMaterial()
        {
            string path = Root + "/Resources/PrototypeLit.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = AssetDatabase.LoadAssetAtPath<Shader>(
                    "Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader");
            if (shader == null) throw new System.InvalidOperationException("URP Lit shader was not found.");
            AssetDatabase.CreateAsset(new Material(shader), path);
        }

        private static void EnsureWeapon(string fileName, string displayName, PrototypeWeaponKind kind,
            float cooldown, float damage, float range, float projectileSpeed, Color color,
            bool modifiesImpact, float impactMultiplier)
        {
            string path = Root + "/Resources/Weapons/" + fileName + ".asset";
            PrototypeWeaponDefinition weapon = AssetDatabase.LoadAssetAtPath<PrototypeWeaponDefinition>(path);
            bool created = weapon == null;
            if (created) weapon = ScriptableObject.CreateInstance<PrototypeWeaponDefinition>();
            weapon.displayName = displayName;
            weapon.kind = kind;
            weapon.cooldown = cooldown;
            weapon.damage = damage;
            weapon.range = range;
            weapon.projectileSpeed = projectileSpeed;
            weapon.prototypeColor = color;
            weapon.modifiesImpact = modifiesImpact;
            weapon.impactMultiplier = impactMultiplier;
            if (created) AssetDatabase.CreateAsset(weapon, path);
            else EditorUtility.SetDirty(weapon);
        }

        private static void ConfigureWeaponShape(string fileName, PrototypeAttackShape shape,
            float halfAngle, float knockback)
        {
            PrototypeWeaponDefinition weapon = AssetDatabase.LoadAssetAtPath<PrototypeWeaponDefinition>(
                Root + "/Resources/Weapons/" + fileName + ".asset");
            weapon.attackShape = shape;
            weapon.meleeHalfAngle = halfAngle;
            weapon.knockbackImpulse = knockback;
            EditorUtility.SetDirty(weapon);
        }

        private static void EnsureItem(string id, string label, string description, string icon,
            int price, int health, int speed, float impact, float reduction, string weaponName)
        {
            string path = Root + "/Resources/Items/" + id + ".asset";
            PrototypeItemDefinition item = AssetDatabase.LoadAssetAtPath<PrototypeItemDefinition>(path);
            bool created = item == null;
            if (created) item = ScriptableObject.CreateInstance<PrototypeItemDefinition>();
            item.itemId = id; item.displayName = label; item.description = description;
            item.iconId = icon; item.price = price; item.healthBonus = health;
            item.speedBonus = speed; item.impactBonus = impact;
            item.impactTakenReduction = reduction;
            if (weaponName != null)
                item.weapon = AssetDatabase.LoadAssetAtPath<PrototypeWeaponDefinition>(
                    Root + "/Resources/Weapons/" + weaponName + ".asset");
            if (created) AssetDatabase.CreateAsset(item, path);
            else EditorUtility.SetDirty(item);
        }

        private static void ConfigureItem(string id, PrototypeItemKind kind, int price,
            int health, int speed, float impact, float reduction,
            PrototypeStat injectionStat = PrototypeStat.Health, float injectionAmount = 10f)
        {
            PrototypeItemDefinition item = AssetDatabase.LoadAssetAtPath<PrototypeItemDefinition>(
                Root + "/Resources/Items/" + id + ".asset");
            item.kind = kind; item.price = price; item.healthBonus = health;
            item.speedBonus = speed; item.impactBonus = impact;
            item.impactTakenReduction = reduction;
            item.injectionStat = injectionStat; item.injectionAmount = injectionAmount;
            EditorUtility.SetDirty(item);
        }

        private static void ConfigureGenerator(string id, string productId)
        {
            PrototypeItemDefinition item = AssetDatabase.LoadAssetAtPath<PrototypeItemDefinition>(
                Root + "/Resources/Items/" + id + ".asset");
            item.generatedItemId = productId;
            EditorUtility.SetDirty(item);
        }

        private static void ConfigureAvailability(string id, PrototypeItemRarity rarity,
            int minDay, int weight)
        {
            PrototypeItemDefinition item = AssetDatabase.LoadAssetAtPath<PrototypeItemDefinition>(
                Root + "/Resources/Items/" + id + ".asset");
            item.rarity = rarity;
            item.minShopDay = minDay;
            item.shopWeight = weight;
            EditorUtility.SetDirty(item);
        }

        private static void EnsureVisual(string id, float scale)
        {
            string path = Root + "/Resources/Visuals/" + id + ".asset";
            PrototypeVisualDefinition visual = AssetDatabase.LoadAssetAtPath<PrototypeVisualDefinition>(path);
            bool created = visual == null;
            if (created) visual = ScriptableObject.CreateInstance<PrototypeVisualDefinition>();
            visual.visualId = id;
            visual.portrait = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Resources/Art/" + id + ".png");
            visual.spriteScale = scale;
            if (created) AssetDatabase.CreateAsset(visual, path);
            else EditorUtility.SetDirty(visual);
        }

        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
        }

        public static void VerifyPrototypeRules()
        {
            PrototypeRun mergeRun = new PrototypeRun(true, true);
            if (!mergeRun.Buy(0)) throw new System.Exception("Could not buy first spear");
            mergeRun.Restock();
            if (!mergeRun.Buy(0) || !mergeRun.MergeBag(1, 0))
                throw new System.Exception("Level 1 spear merge failed");
            if (mergeRun.Bag.Count != 1 || mergeRun.Bag[0].level != 2 ||
                mergeRun.Bag[0].LevelMultiplier != 2f)
                throw new System.Exception("Merge did not preserve expected item instance");
            if (!mergeRun.Equip(0, 1)) throw new System.Exception("Weapon should equip in any open slot");
            mergeRun.GetWeapons(out PrototypeItemInstance melee, out _);
            if (melee == null || melee.level != 2)
                throw new System.Exception("Battle weapon was not upgraded");

            PrototypeRun levelThree = new PrototypeRun(true, true);
            levelThree.AddGold(50, "rule test");
            for (int i = 0; i < 4; i++)
            {
                levelThree.Restock();
                if (!levelThree.Buy(0)) throw new System.Exception("Could not buy four level-one spears");
            }
            if (!levelThree.MergeBag(1, 0) || !levelThree.MergeBag(2, 1) ||
                !levelThree.MergeBag(1, 0) || levelThree.Bag.Count != 1 ||
                levelThree.Bag[0].level != 3 || levelThree.Bag[0].LevelMultiplier != 4f)
                throw new System.Exception("Level 2 + level 2 did not produce level 3 / 4x damage");

            PrototypeRun injectionRun = new PrototypeRun(true, true);
            PrototypeCheckFixtures.Shop(injectionRun)[2] = injectionRun.Find("injector");
            if (!injectionRun.Buy(2) || !injectionRun.UseInjection(0))
                throw new System.Exception("Injection flow failed");
            if (injectionRun.CalculateStats().health != 130f ||
                injectionRun.PermanentModifiers.Count != 1)
                throw new System.Exception("Injection modifier was lost");
            PrototypeRun economyRun = new PrototypeRun(true, true);
            var uniqueLoadouts = new System.Collections.Generic.HashSet<string>();
            for (int tier = 0; tier < 3; tier++)
            {
                int variant = economyRun.MonsterOfferVariant(tier);
                PrototypeEncounter offer = PrototypeEncounterCatalog.Get(economyRun, tier, false);
                if (variant < 0 || variant >= PrototypeEncounterCatalog.VariantCount(tier) ||
                    offer.tier != (PrototypeEncounterTier)tier ||
                    string.IsNullOrEmpty(offer.visualId) || offer.itemIds.Length < 2)
                    throw new System.Exception("Monster offer or equipment profile is invalid");
                for (int entry = 0; entry < PrototypeEncounterCatalog.VariantCount(tier); entry++)
                {
                    PrototypeEncounter candidate = PrototypeEncounterCatalog.Get(economyRun, tier, false, entry);
                    if (Resources.Load<Texture2D>("Art/" + candidate.visualId) == null)
                        throw new System.Exception("Monster portrait is missing: " + candidate.visualId);
                    foreach (string id in candidate.itemIds)
                        if (economyRun.Find(id) == null)
                            throw new System.Exception("Monster equipment is missing: " + id);
                    if (!uniqueLoadouts.Add(string.Join(",", candidate.itemIds)))
                        throw new System.Exception("Monster equipment profiles are duplicated");
                }
            }
            if (!economyRun.BuyToSlot(0, 1) || !economyRun.BuyToSlot(1, 8) ||
                economyRun.Equipped[1]?.definition.itemId != "spear" ||
                economyRun.Equipped[8]?.definition.itemId != "crossbow")
                throw new System.Exception("Drag purchase / unrestricted weapon slots failed");
            PrototypeCheckFixtures.Shop(economyRun)[0] = economyRun.Find("spear");
            economyRun.AddGold(10, "rule test");
            if (!economyRun.BuyToSlot(0, 1) || economyRun.Equipped[1].level != 2)
                throw new System.Exception("Drag purchase into matching slot did not merge");
            int beforeIncome = economyRun.Gold;
            economyRun.EndDay(false);
            if (economyRun.Gold != beforeIncome + 12)
                throw new System.Exception("Fixed round income was not awarded");
            foreach (var offer in economyRun.Shop)
                if (offer != null && offer.kind == PrototypeItemKind.Injection)
                    throw new System.Exception("Shop offered a direct injection");
            PrototypeRun producerRun = new PrototypeRun(true, true);
            if (!producerRun.BuyToSlot(2, 1)) throw new System.Exception("Could not equip needle producer");
            producerRun.EndDay(false);
            if (producerRun.PendingRewards.Count != 0) throw new System.Exception("Producer paid too early");
            producerRun.EndDay(false);
            if (producerRun.PendingRewards.Count != 1 || !producerRun.ClaimGenerated() ||
                producerRun.Bag[0].definition.itemId != "injector")
                throw new System.Exception("Producer or reward claim failed");
            PrototypeRun swallowRun = new PrototypeRun(true, true);
            PrototypeCheckFixtures.Shop(swallowRun)[3] = swallowRun.Find("devourer");
            if (!swallowRun.Buy(3) || !swallowRun.Equip(0, 1) ||
                !swallowRun.Buy(0) || !swallowRun.Equip(0, 1) ||
                swallowRun.CalculateStats().impactBonus < .0099f)
                throw new System.Exception("Item-instance growth from swallow was not applied");
            PrototypeBuffManager buffs = new PrototypeBuffManager();
            buffs.AddOrRefresh("wheel", PrototypeStat.Speed, 4f, 10, 100);
            buffs.AddOrRefresh("wheel", PrototypeStat.Speed, 4f, 50, 100);
            if (buffs.Count != 1 || buffs.Sum(PrototypeStat.Speed) != 4f)
                throw new System.Exception("Buff refresh duplicated a modifier");
            buffs.Advance(149);
            if (buffs.Count != 1) throw new System.Exception("Buff expired too early");
            buffs.Advance(150);
            if (buffs.Count != 0) throw new System.Exception("Buff did not expire");
            string savePath = System.IO.Path.Combine(Application.temporaryCachePath,
                "pkball-rule-test-" + System.Guid.NewGuid().ToString("N") + ".json");
            try
            {
                PrototypeRun saved = new PrototypeRun(true, false, savePath);
                PrototypeCheckFixtures.Shop(saved)[2] = saved.Find("injector");
                if (!saved.Buy(2) || !saved.UseInjection(0))
                    throw new System.Exception("Save test could not apply injection");
                saved.AddGold(50, "rule test");
                for (int i = 0; i < 4; i++)
                {
                    saved.Restock();
                    if (!saved.Buy(0)) throw new System.Exception("Save test could not buy spear");
                }
                if (!saved.MergeBag(1, 0) || !saved.MergeBag(2, 1) ||
                    !saved.MergeBag(1, 0) || !saved.Equip(0, 0))
                    throw new System.Exception("Save test could not equip level-three spear");
                PrototypeCheckFixtures.Shop(saved)[3] = saved.Find("health_lab");
                if (!saved.BuyToSlot(3, 1)) throw new System.Exception("Save test could not buy producer");
                saved.EndDay(false);
                saved.EndDay(false);
                if (!saved.ChooseEvent(0)) throw new System.Exception("Save test could not choose event");
                PrototypeRun restored = new PrototypeRun(false, false, savePath);
                if (restored.CalculateStats().health != 138f || restored.PermanentModifiers.Count != 1)
                    throw new System.Exception("Permanent modifier was lost on reload");
                if (restored.Equipped[0] == null || restored.Equipped[0].level != 3 ||
                    restored.Equipped[0].investedGold != 40 ||
                    restored.Equipped[0].LevelMultiplier != 4f)
                    throw new System.Exception("Merged item level or investment was lost on reload");
                if (restored.PendingRewards.Count != 1 || restored.Equipped[1].productionProgress != 0)
                    throw new System.Exception("Generated reward was lost on reload");
                if (!restored.EventChosen || !restored.ShopAvailable ||
                    restored.SlotEffects[0] != PrototypeSlotEffect.Vital)
                    throw new System.Exception("Day phase or slot effect was lost on reload");
                for (int tier = 0; tier < 3; tier++)
                    if (saved.MonsterOfferVariant(tier) != restored.MonsterOfferVariant(tier))
                        throw new System.Exception("Daily monster offer changed after save reload");
            }
            finally
            {
                if (System.IO.File.Exists(savePath)) System.IO.File.Delete(savePath);
            }
            PrototypeRun flow = new PrototypeRun(true, true);
            if (!flow.ChooseEvent(0) || !flow.ShopAvailable || !flow.AdvanceEvent() ||
                flow.Stage != PrototypeDayStage.EventTwo)
                throw new System.Exception("First event transition failed");
            if (!flow.ChooseEvent(1, 0) || !flow.AdvanceEvent() ||
                flow.Stage != PrototypeDayStage.Monster || !flow.SelectMonster(2))
                throw new System.Exception("Monster selection failed");
            if (flow.CurrentEncounter.tier != PrototypeEncounterTier.Boss ||
                flow.CurrentEncounter.health <= PrototypeEncounterCatalog.Get(flow, 0, false).health)
                throw new System.Exception("Monster difficulty table is invalid");
            flow.ResolveCombat(false);
            if (flow.Stage != PrototypeDayStage.EventThree || flow.Endurance != 9)
                throw new System.Exception("Monster defeat did not consume run endurance");
            if (!flow.ChooseEvent(0) || !flow.AdvanceEvent() ||
                !flow.ChooseEvent(0) || !flow.AdvanceEvent() ||
                flow.Stage != PrototypeDayStage.Rival)
                throw new System.Exception("Late event transitions failed");
            flow.ResolveCombat(true);
            if (flow.Day != 2 || flow.Wins != 1 || flow.Stage != PrototypeDayStage.EventOne)
                throw new System.Exception("Rival victory did not advance the day");
            PrototypeRun slots = new PrototypeRun(true, true);
            if (!slots.BuyToSlot(0, 0) || slots.CalculateStats().health != 128f)
                throw new System.Exception("Intrinsic vitality slot was not applied");
            if (!slots.Unequip(0) || !slots.Equip(0, 1) || slots.CalculateStats().health != 120f)
                throw new System.Exception("Slot bonus persisted after moving the item");
            PrototypeRun winning = new PrototypeRun(true, true);
            for (int i = 0; i < 10; i++) CompletePrototypeDay(winning, true);
            if (!winning.Cleared || winning.Wins != 10 || winning.Day != 11 || winning.Endless)
                throw new System.Exception("Ten victories did not clear the run");
            winning.EnterEndless();
            if (!winning.Endless || !winning.CanFight && winning.Stage == PrototypeDayStage.Rival)
                throw new System.Exception("Endless mode was not enabled");
            PrototypeRun survival = new PrototypeRun(true, true);
            for (int i = 0; i < 7; i++) CompletePrototypeDay(survival, false);
            if (!survival.LastStandUsed || survival.Endurance != 1 || survival.GameOver)
                throw new System.Exception("Last stand did not protect the player once");
            CompletePrototypeDay(survival, false);
            if (!survival.GameOver || survival.Endurance != 0)
                throw new System.Exception("Run should end after the second lethal defeat");
            Debug.Log("PKBall rules verified: merge, slots, event flow, monsters, endurance, economy, production, buffs, save reload");
        }

        private static void CompletePrototypeDay(PrototypeRun run, bool rivalWon)
        {
            if (!run.ChooseEvent(0) || !run.AdvanceEvent() ||
                !run.ChooseEvent(0) || !run.AdvanceEvent() ||
                !run.SelectMonster(0)) throw new System.Exception("Early day phases failed");
            run.ResolveCombat(true);
            if (!run.ChooseEvent(0) || !run.AdvanceEvent() ||
                !run.ChooseEvent(0) || !run.AdvanceEvent() ||
                run.Stage != PrototypeDayStage.Rival)
                throw new System.Exception("Late day phases failed");
            run.ResolveCombat(rivalWon);
        }
    }
}

