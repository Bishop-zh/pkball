using System.Collections.Generic;
using UnityEngine;

namespace PKBall.Prototype
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class PrototypeActor : MonoBehaviour
    {
        private sealed class RuntimeItem
        {
            public PrototypeItemInstance item;
            public int slotIndex, produced;
            public float readyAt, nextProduction, cooldownFactor = 1f, damageFactor = 1f;
            public int tokenCursor;
            public readonly Dictionary<string,float> triggerReadyAt = new Dictionary<string,float>();
        }
        public int Id { get; private set; }
        public float BaseMaximumHealth { get; private set; }
        public float BonusMaximumHealth => Buffs.Sum(PrototypeStat.Health);
        public float MaximumHealth => BaseMaximumHealth + BonusMaximumHealth;
        public float Health { get; private set; }
        public PrototypeDamageKind LastDamageKind { get; private set; }
        public float Energy { get; private set; }
        private float baseSpeedStat;
        public float SpeedStat => baseSpeedStat + Buffs.Sum(PrototypeStat.Speed);
        public float ImpactDamageBonus => impactDamageBase + Buffs.Sum(PrototypeStat.ImpactBonus);
        public float ImpactTakenMultiplier => Mathf.Clamp(impactTakenBase - Buffs.Sum(PrototypeStat.ImpactTakenReduction), .01f, 1f);
        public float Radius => radius;
        public int SlimeGrowthActivations { get; private set; }
        public float SlimeVisualScale => 1f+1.2f*(1f-Mathf.Exp(-SlimeGrowthActivations*.12f));
        public float EffectiveMass => massSnapshot;
        public float TargetSpeed => balance.TargetSpeed(SpeedStat, massHealthSnapshot);
        public Vector3 Velocity => body.velocity;
        public bool IsAlive => Health > 0;
        private float invulnerableUntil;
        public bool IsInvulnerable => battle != null && battle.Elapsed < invulnerableUntil;
        public void GrantInvulnerability(float seconds) { if(seconds>0) invulnerableUntil=Mathf.Max(invulnerableUntil,battle.Elapsed+seconds); }
        public Transform Ring => ring;
        public PrototypeCombatState State { get; private set; } = new PrototypeCombatState();
        public PrototypeBuffManager Buffs => State.Buffs;
        private PrototypeBattle battle;
        private PrototypeBalance balance;
        private Rigidbody body;
        private Transform ring;
        private readonly HashSet<RuntimeItem> activating = new HashSet<RuntimeItem>();
        private readonly List<RuntimeItem> weapons = new List<RuntimeItem>(10);
        private float impactDamageBase, impactTakenBase, radius, massSnapshot, massHealthSnapshot;
        private float baseDamageBonus, baseDamageReduction;
        private float damageBonus => baseDamageBonus + Buffs.Sum(PrototypeStat.DamageBonus);
        private float damageReduction => 1-(1-baseDamageReduction)*Buffs.DamageTakenFraction();
        private bool launched, wheelSpearSynergy;
        private Vector3 lastPosition, preCollisionVelocity;
        private float nextWallReturnAt;
        private int wallBounceCount;
        public void Initialize(PrototypeBattle owner, PrototypeBalance config, int id, float health,
            float speed, float impactBonus, float impactTaken, float collisionRadius,
            PrototypeWeaponDefinition meleeDefinition, PrototypeWeaponDefinition rangedDefinition,
            float meleeFactor = 1, float rangedFactor = 1, bool speedOnMeleeHit = false,
            float impactEffectFactor = 1, bool meleeOnRight = false)
        {
            battle=owner; balance=config; Id=id; BaseMaximumHealth=Health=health; massHealthSnapshot=health;
            Energy=config.maximumEnergy; baseSpeedStat=speed; impactDamageBase=impactBonus; impactTakenBase=impactTaken;
            radius=collisionRadius; wheelSpearSynergy=speedOnMeleeHit;
            body=GetComponent<Rigidbody>(); body.useGravity=false; massSnapshot=balance.EffectiveMass(health);
            body.mass=massSnapshot; body.drag=0; body.angularDrag=0; body.interpolation=RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            body.constraints=RigidbodyConstraints.FreezePositionY|RigidbodyConstraints.FreezeRotation;
            body.position=transform.position;body.velocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.isKinematic=true;
            var collider=GetComponent<SphereCollider>(); collider.radius=radius; collider.material=battle.BounceMaterial;
            ring=new GameObject("AttackFacing").transform; ring.SetParent(transform,false);
            lastPosition=transform.position;
        }
        public void ConfigureStats(PrototypeStatSnapshot stats)
        { baseDamageBonus=stats.damageBonus; baseDamageReduction=stats.damageReduction; State.AddShield(stats.startingShield); }
        public void AddEquipment(PrototypeItemInstance item, int slot, float bonus = 0, float slotDamage = 1, float slotCooldown = 1)
        {
            if (item == null) return;
            weapons.Add(new RuntimeItem {item=item, slotIndex=slot, damageFactor=(1+bonus)*slotDamage,
                cooldownFactor=slotCooldown, readyAt=Duration(item,slotCooldown),
                nextProduction=item.definition.productionInterval > 0 ? item.definition.ProductionInterval(item.level) : float.PositiveInfinity});
        }
        // Kept for integrations that still construct individual weapon definitions.
        public void AddWeapon(PrototypeWeaponDefinition definition, PrototypeItemInstance item, float damageFactor,
            float effectFactor, int slotIndex, float cooldownFactor = 1)
        {
            if (item != null) AddEquipment(item,slotIndex,damageFactor/item.LevelMultiplier-1,1,cooldownFactor);
        }
        public PrototypeItemInstance ItemAt(int slot)
        { foreach (var runtime in weapons) if (runtime.slotIndex == slot) return runtime.item; return null; }
        private float WeaponDamage(RuntimeItem runtime,PrototypeItemDefinition d,float scale,float accruedDamage)
        {
            if(d.weapon==null) return 0;
            float typeBuff=Buffs.Sum(d.weapon.kind==PrototypeWeaponKind.Melee?PrototypeStat.MeleeDamageBonus:PrototypeStat.RangedDamageBonus);
            return (d.weapon.damage*scale+accruedDamage)*runtime.damageFactor*(1+damageBonus+typeBuff)*runtime.item.encounterOutputScale;
        }
        public float DirectDamageAt(int slot) => GetEffectValueAt(slot, PrototypeEffectKind.Damage);
        public float GetEffectValueAt(int slot, PrototypeEffectKind kind)
        {
            foreach (var runtime in weapons) if (runtime.slotIndex == slot)
            {
                float value = ProgramValue(runtime, runtime.item.definition, runtime.item.LevelMultiplier, null, kind);
                foreach (var program in runtime.item.inherited)
                {
                    var definition = battle.Run.Find(program.definitionId);
                    if (definition != null) value += ProgramValue(runtime, definition, program.scale, program, kind);
                }
                return value;
            }
            return 0;
        }
        private float ProgramValue(RuntimeItem runtime, PrototypeItemDefinition definition, float scale, PrototypeInheritedProgram program, PrototypeEffectKind kind)
        {
            if (program == null) runtime.item.EnsureModifiers(); else program.EnsureModifiers(definition);
            float value = kind == PrototypeEffectKind.Damage ? WeaponDamage(runtime, definition, scale,
                program == null ? runtime.item.GrowthAmount("weapon") : program.GrowthAmount("weapon")) : 0;
            if (definition.effects == null) return value;
            int effectLevel=program==null?runtime.item.level:program.level;
            float ratioScale = program == null ? 1 : scale / Mathf.Pow(2, program.level-1);
            for (int i=0; i<definition.effects.Length; i++)
            {
                var effect = definition.effects[i];
                if(effect.trigger != PrototypeEffectTrigger.Release)continue;
                if (kind == PrototypeEffectKind.Damage && effect.kind == PrototypeEffectKind.TargetMaxHealthDamage)
                    value += battle.OpponentOf(this).MaximumHealth * (effect.BaseAt(effectLevel) * ratioScale + (program == null ? runtime.item.GrowthAmount(effect) : program.GrowthAmount(PrototypeModifierLedger.EffectKey(i, effect))));
                else if (kind == PrototypeEffectKind.Damage && effect.kind == PrototypeEffectKind.OwnMaxHealthDamage)
                    value += MaximumHealth * (effect.BaseAt(effectLevel) * ratioScale + (program == null ? runtime.item.GrowthAmount(effect) : program.GrowthAmount(PrototypeModifierLedger.EffectKey(i, effect))));
                else if(kind==PrototypeEffectKind.Damage && effect.kind==PrototypeEffectKind.ConsumeShieldDamage)
                    value+=State.Shield*Mathf.Clamp01(effect.BaseAt(effectLevel)*ratioScale+(program==null?runtime.item.GrowthAmount(effect):program.GrowthAmount(PrototypeModifierLedger.EffectKey(i,effect))))*(1+damageBonus)*runtime.item.encounterOutputScale;
                else if (effect.kind == kind)
                {
                    string key = PrototypeModifierLedger.EffectKey(i, effect);
                    float amount = (effect.BaseAt(effectLevel) * ratioScale + (program == null ? runtime.item.GrowthAmount(key) : program.GrowthAmount(key))) * ((int)effect.kind <= (int)PrototypeEffectKind.Regeneration ? runtime.item.encounterOutputScale : 1f);
                    value += amount * (kind == PrototypeEffectKind.Damage ? 1+damageBonus : 1);
                }
            }
            return value;
        }
        private static float Duration(PrototypeItemInstance item, float slotFactor) => item.BaseCooldown * slotFactor;
        public void Launch(Vector3 direction,float power)
        {
            if (!IsAlive || direction.sqrMagnitude < .01f) return;
            body.isKinematic=false;direction.y=0; body.velocity=direction.normalized*TargetSpeed*Mathf.Clamp(power,.5f,1);
            launched=true; lastPosition=transform.position;
        }
        private void UpdateFacing()
        {
            var target=battle == null ? null : battle.OpponentOf(this);
            if (target == null) return;
            var toward=target.transform.position-transform.position; toward.y=0;
            if (toward.sqrMagnitude > .001f) ring.rotation=Quaternion.LookRotation(toward,Vector3.up);
        }
        public void MotionTick(float dt)
        {
            if (!launched || !IsAlive || battle == null || !battle.IsFighting) return;
            UpdateFacing();
            KeepInsideCircularArena();
            var velocity=body.velocity; velocity.y=0; preCollisionVelocity=velocity;
            float speed=velocity.magnitude;
            if (speed > .1f) body.velocity=velocity.normalized*Mathf.Min(Mathf.Lerp(speed,TargetSpeed,Mathf.Clamp01(balance.speedRecovery*dt)),balance.maximumSpeed);
            float travelled=Vector3.Distance(lastPosition,transform.position); lastPosition=transform.position;
            if (Energy > 0) Energy=Mathf.Max(0,Energy-(balance.energyPerSecond+balance.energyPerSpeed*speed)*dt);
            else ReceiveDamage(balance.zeroEnergyDamagePerMetre*travelled,PrototypeDamageKind.Exhaustion);
        }
        public void CombatTick(float now)
        {
            if (!launched || !IsAlive) return;
            Health=Mathf.Min(Health,MaximumHealth);
            State.Advance(this,now);
            if (!IsAlive) return;
            var target=battle.OpponentOf(this);
            foreach (var runtime in weapons)
            {
                if (!IsAlive || !target.IsAlive) break;
                var d=runtime.item.definition;
                if (Id == 1 && now >= runtime.nextProduction)
                {
                    // Production is independent of weapon activation and target distance.
                    runtime.nextProduction += d.ProductionInterval(runtime.item.level);
                    if(Id==1)battle.Run.RecordProduction(runtime.item, d.generatedItemId, d.randomPotionProducer,1);
                    runtime.produced++;
                    battle.Presentation?.ShowActivation(this,this,d,runtime.item.level);
                }
                if (!runtime.item.HasActive || now < runtime.readyAt) continue;
                if (Activate(runtime,target,false)) runtime.readyAt=now+Duration(runtime.item,runtime.cooldownFactor);
            }
        }
        private bool CanActivate(PrototypeItemDefinition d,PrototypeActor target)
        {
            if (d.weapon == null) return true;
            float edge=Vector3.Distance(transform.position,target.transform.position)-radius-target.radius;
            return edge <= d.weapon.range && (d.weapon.kind != PrototypeWeaponKind.Melee || battle.HasLineOfSight(this,target));
        }
        private bool Activate(RuntimeItem runtime,PrototypeActor target,bool extra, bool suppressHitTriggers = false)
        {
            var item=runtime.item; var d=item.definition;
            if (!target.IsAlive || !CanActivate(d,target) || !activating.Add(runtime)) return false;
            item.EnsureModifiers();
            ExecuteProgram(runtime,d,item.LevelMultiplier,target,null,PrototypeEffectTrigger.Release,suppressHitTriggers);
            item.AddActivationGrowth(item.LevelMultiplier);
            if (d.itemId == "devourer") foreach (var program in item.inherited)
            {
                PrototypeItemDefinition inheritedDefinition=battle.Run.Find(program.definitionId);
                if (inheritedDefinition == null || !CanActivate(inheritedDefinition,target)) continue;
                float originalCooldown = inheritedDefinition.weapon == null ? inheritedDefinition.activationCooldown : inheritedDefinition.weapon.cooldown;
                program.EnsureModifiers(inheritedDefinition);
                float improvedCooldown = Mathf.Max(1, originalCooldown * program.CooldownFactor);
                program.cadenceCredit += originalCooldown / improvedCooldown;
                int repetitions = Mathf.FloorToInt(program.cadenceCredit);program.cadenceCredit -= repetitions;
                for(int n=0;n<repetitions;n++)
                {
                    ExecuteProgram(runtime,inheritedDefinition,program.scale,target,program,PrototypeEffectTrigger.Release,suppressHitTriggers);
                    program.AddActivationGrowth(inheritedDefinition, program.scale);
                }
                battle.Presentation?.ShowActivation(this,target,inheritedDefinition,program.level);
                if (Id == 1 && inheritedDefinition.productionInterval > 0)
                {
                    // Convert the original rate to the gear's six-second cycle; keep fractional output.
                    program.productionCredit += (program.scale/Mathf.Pow(2,program.level-1)) * Duration(item,runtime.cooldownFactor) / inheritedDefinition.ProductionInterval(program.level);
                    int count=Mathf.FloorToInt(program.productionCredit);
                    if (count > 0) { program.productionCredit-=count; if(Id==1)battle.Run.RecordProduction(item,inheritedDefinition.generatedItemId,inheritedDefinition.randomPotionProducer,count); runtime.produced+=count; }
                }
            }
            if(d.itemId=="devourer" && item.recipes.Count>0) ReplayPotions(item);
            battle.Presentation?.ShowActivation(this,target,d,item.level);
            if (Id == 1) battle.Run.MarkCombatDirty();
            activating.Remove(runtime);
            return true;
        }
        private void ExecuteProgram(RuntimeItem runtime,PrototypeItemDefinition d,float scale,
            PrototypeActor target,PrototypeInheritedProgram inherited, PrototypeEffectTrigger trigger = PrototypeEffectTrigger.Release, bool suppressHitTriggers = false)
        {
            var item=runtime.item;
            if(trigger==PrototypeEffectTrigger.Release && d.itemId=="slime_core") SlimeGrowthActivations++;
            // Reaction-driven extra releases inherit this flag across their entire synchronous tag/token chain.
            suppressHitTriggers |= trigger != PrototypeEffectTrigger.Release;
            if (inherited == null) item.EnsureModifiers(); else inherited.EnsureModifiers(d);
            PrototypeAttackPayload payload=new PrototypeAttackPayload {item=item,sourceDefinitionId=d.itemId,suppressHitTriggers=suppressHitTriggers,kind=PrototypeDamageKind.Ranged,visual=d.weapon,hitSoundId=PrototypeAudioLibrary.ResolveHit(d),hitVolume=d.hitVolume,hitVisual=d.weapon==null?d.hitVisual:d.weapon.hitVisual};
            if (d.weapon != null && trigger == PrototypeEffectTrigger.Release)
            {
                payload.damage=WeaponDamage(runtime,d,scale,inherited == null ? item.GrowthAmount("weapon") : inherited.GrowthAmount("weapon"));
                payload.knockback=d.weapon.knockbackImpulse*scale;
                payload.kind=d.weapon.kind == PrototypeWeaponKind.Melee ? PrototypeDamageKind.Melee : PrototypeDamageKind.Ranged;
                var toward=(target.transform.position-transform.position).normalized;
                payload.push=d.weapon.attackShape == PrototypeAttackShape.Sweep ? (toward+ring.right*.3f).normalized : toward;
                payload.produceOre=d.itemId == "pickaxe";payload.oreCredit=scale;
            }
            bool triggeredVisual=false;
            if (d.effects != null) for (int effectIndex=0; effectIndex<d.effects.Length; effectIndex++)
            {
                var e = d.effects[effectIndex];
                if(e.trigger != trigger)continue;
                string effectKey = PrototypeModifierLedger.EffectKey(effectIndex, e);
                string triggerKey=d.itemId+":"+(inherited==null?item.level:inherited.level)+":"+effectKey;
                if(trigger != PrototypeEffectTrigger.Release)
                {
                    if(runtime.triggerReadyAt.TryGetValue(triggerKey,out float ready) && battle.Elapsed < ready)continue;
                    runtime.triggerReadyAt[triggerKey]=battle.Elapsed+Mathf.Max(.01f,e.triggerCooldown);
                }
                if(trigger!=PrototypeEffectTrigger.Release && !triggeredVisual){battle.Presentation?.ShowActivation(this,target,d,inherited==null?item.level:inherited.level);triggeredVisual=true;}
                int effectLevel=inherited==null?item.level:inherited.level;
                float accrued = inherited==null ? item.GrowthAmount(effectKey) : inherited.GrowthAmount(effectKey);
                // The ledger already stores the absorbed contribution. Scale only the definition's base.
                float contribution = inherited==null ? 1 : scale/Mathf.Pow(2,inherited.level-1);
                float value=(e.BaseAt(effectLevel)*contribution+accrued)*((int)e.kind <= (int)PrototypeEffectKind.Regeneration ? item.encounterOutputScale : 1f);
                float ratioValue=e.BaseAt(effectLevel)*contribution+accrued;
                bool damageEffect = e.kind == PrototypeEffectKind.Damage || e.kind == PrototypeEffectKind.TargetMaxHealthDamage || e.kind == PrototypeEffectKind.OwnMaxHealthDamage || e.kind == PrototypeEffectKind.ConsumeShieldDamage;
                float critical = damageEffect && e.criticalChance > 0 && battle.NextCombatRandom() < Mathf.Clamp01(e.criticalChance) ? Mathf.Max(1,e.criticalMultiplier) : 1;
                switch (e.kind)
                {
                    case PrototypeEffectKind.Damage: payload.damage+=value*(1+damageBonus)*critical; break;
                    // Ratio effects remain the declared percentage across ranks; absorption scales their contribution.
                    case PrototypeEffectKind.TargetMaxHealthDamage:
                        payload.damage+=target.MaximumHealth * ratioValue * critical; break;
                    case PrototypeEffectKind.Invulnerability: GrantInvulnerability(ratioValue); break;
                    case PrototypeEffectKind.GrowBattleMaxHealth:
                        AddBattleHealth("item:"+item.instanceId+":"+(inherited==null?d.itemId:inherited.definitionId)+":"+effectKey,
                            MaximumHealth*ratioValue); break;
                    case PrototypeEffectKind.OwnMaxHealthDamage:
                        payload.damage+=MaximumHealth*ratioValue*critical; break;
                    case PrototypeEffectKind.Shield: AddShield(value); break;
                    case PrototypeEffectKind.Heal: Heal(value); break;
                    case PrototypeEffectKind.Poison: payload.poison+=value; break;
                    case PrototypeEffectKind.Burn: payload.burn+=value; break;
                    case PrototypeEffectKind.Regeneration: State.AddRegeneration(value); break;
                    case PrototypeEffectKind.MasterCooldown:
                        float accruedReduction = inherited == null ? item.GrowthAmount(effectKey) : inherited.GrowthAmount(effectKey);
                        float baseReduction = Mathf.Clamp01(e.HasLevelAmounts || e.amount > 0 ? e.BaseAt(effectLevel) : PrototypeEffectMath.MasterReduction(effectLevel));
                        float reduction = Mathf.Clamp01(1-Mathf.Pow(1-baseReduction,contribution)+accruedReduction);
                        ReduceApprentices(e.matchTag,reduction); break;
                    case PrototypeEffectKind.TriggerApprentices:
                        if (e.HasLevelAmounts || e.amount > 0)
                        {
                            TriggerApprentices(runtime, target, Mathf.Max(0, Mathf.FloorToInt(value)),suppressHitTriggers);
                            break;
                        }
                        int repeats=1;
                        if (inherited != null)
                        {
                            inherited.triggerCredit+=scale/Mathf.Pow(2,inherited.level-1);
                            repeats=Mathf.FloorToInt(inherited.triggerCredit);inherited.triggerCredit-=repeats;
                        }
                        int tokenLevel=inherited == null ? item.level : inherited.level;
                        for(int n=0;n<repeats;n++) TriggerApprentices(runtime,target,tokenLevel == 4 ? int.MaxValue : (1 << (tokenLevel-1)),suppressHitTriggers);
                        break;
                    case PrototypeEffectKind.ConsumeShieldDamage:
                        float consumed=State.Shield*Mathf.Clamp01(ratioValue);
                        State.Buffs.RemoveStatus(PrototypeEffectKind.Shield,consumed);
                        payload.damage+=consumed*(1+damageBonus)*item.encounterOutputScale*critical;
                        break;
                    case PrototypeEffectKind.TriggerTaggedEquipment:
                        TriggerTagged(runtime,target,e.matchTag,Mathf.Max(0,Mathf.FloorToInt(value)),suppressHitTriggers);break;
                    case PrototypeEffectKind.ReplayPotions: if(inherited==null) ReplayPotions(item); break;
                }
                if(trigger != PrototypeEffectTrigger.Release)
                {
                    float growthDelta=e.GrowthAt(effectLevel)*contribution;
                    if(inherited==null)item.AddGrowth(effectKey,e.kind,growthDelta);
                    else PrototypeModifierLedger.Add(inherited.modifiers,effectKey,e.kind,growthDelta);
                    if(Id==1 && growthDelta!=0)battle.Run.MarkCombatDirty();
                }
            }
            if (payload.damage > 0 || payload.poison > 0 || payload.burn > 0)
            {
                if(d.itemId=="green_flying_sword" && trigger==PrototypeEffectTrigger.Release)
                    battle.ReleaseFlyingSword(this,target,payload,d.weapon==null?10:d.weapon.projectileSpeed);
                else if (d.weapon != null && d.weapon.kind == PrototypeWeaponKind.Melee && trigger == PrototypeEffectTrigger.Release)
                {
                    battle.ScheduleAttack(this,target,payload,d.weapon.hitDelay);
                    if (wheelSpearSynergy && d.itemId == "spear") Buffs.AddOrRefresh("wheel-spear",PrototypeStat.Speed,4,
                        battle.StepIndex,Mathf.CeilToInt(2/Time.fixedDeltaTime));
                }
                else battle.FirePayload(this,target,payload,d.weapon == null ? 16 : d.weapon.projectileSpeed);
            }
        }
        // Hit triggers belong to the item/program that produced the successful attack.
        public void NotifyItemHit(PrototypeAttackPayload payload, PrototypeActor target, bool burningBeforeHit)
        {
            if(!IsAlive || target==null || payload.item==null || payload.suppressHitTriggers)return;
            foreach(var runtime in weapons)if(ReferenceEquals(runtime.item,payload.item))
            {
                var d=runtime.item.definition;
                if(string.IsNullOrEmpty(payload.sourceDefinitionId)||payload.sourceDefinitionId==d.itemId)
                {
                    ExecuteProgram(runtime,d,runtime.item.LevelMultiplier,target,null,PrototypeEffectTrigger.Hit);
                    if(burningBeforeHit)ExecuteProgram(runtime,d,runtime.item.LevelMultiplier,target,null,PrototypeEffectTrigger.BurningHit);
                }
                else foreach(var program in runtime.item.inherited)if(program.definitionId==payload.sourceDefinitionId)
                {
                    var source=battle.Run.Find(program.definitionId);if(source==null)continue;
                    ExecuteProgram(runtime,source,program.scale,target,program,PrototypeEffectTrigger.Hit);
                    if(burningBeforeHit)ExecuteProgram(runtime,source,program.scale,target,program,PrototypeEffectTrigger.BurningHit);
                }
                return;
            }
        }
        private void TriggerShieldBroken()
        {
            var target=battle.OpponentOf(this);if(target==null || !target.IsAlive)return;
            foreach(var runtime in weapons)
            {
                ExecuteProgram(runtime,runtime.item.definition,runtime.item.LevelMultiplier,target,null,PrototypeEffectTrigger.ShieldBroken);
                foreach(var program in runtime.item.inherited)
                {var d=battle.Run.Find(program.definitionId);if(d!=null)ExecuteProgram(runtime,d,program.scale,target,program,PrototypeEffectTrigger.ShieldBroken);}
            }
        }
        private void TriggerTagged(RuntimeItem source,PrototypeActor target,string tag,int count, bool suppressHitTriggers)
        {
            if(string.IsNullOrEmpty(tag))return;
            // Scan slots beside the source first, then walk outward. Each matching slot executes at most once.
            for(int distance=1;distance<10 && count>0;distance++)
                for(int direction=-1;direction<=1 && count>0;direction+=2)
                {
                    int slot=source.slotIndex+distance*direction;
                    foreach(var candidate in weapons)if(candidate.slotIndex==slot && candidate!=source && candidate.item.HasTag(tag))
                    {if(Activate(candidate,target,true,suppressHitTriggers))count--;break;}
                }
        }
        private void ReduceApprentices(string tag,float rate)
        {
            foreach(var r in weapons)
            {
                if(r.item.definition.HasTag("学徒") && r.item.definition.HasTag(tag))
                {
                    float old=Duration(r.item,r.cooldownFactor);
                    float original=r.item.definition.weapon==null?r.item.definition.activationCooldown:r.item.definition.weapon.cooldown;
                    r.item.ReduceCooldown(rate);
                    float current=Duration(r.item,r.cooldownFactor);
                    if(r.readyAt>battle.Elapsed) r.readyAt=battle.Elapsed+(r.readyAt-battle.Elapsed)*current/old;
                }
                foreach(var program in r.item.inherited)
                {
                    var d=battle.Run.Find(program.definitionId);
                    if(d==null || !d.HasTag("学徒") || !d.HasTag(tag)) continue;
                    float original=d.weapon==null?d.activationCooldown:d.weapon.cooldown;
                    program.ReduceCooldown(d, rate);
                }
            }
        }
        private void TriggerApprentices(RuntimeItem token,PrototypeActor target,int count, bool suppressHitTriggers)
        {
            // Stable bounded candidate scan. Only apprentice programs execute; no token recursion.
            int start=token.tokenCursor;
            for(int offset=0;offset<weapons.Count && count>0;offset++)
            {
                int index=(start+offset)%weapons.Count;var r=weapons[index];if(r==token) continue;
                if(r.item.definition.HasTag("学徒") && Activate(r,target,true,suppressHitTriggers)) {count--;token.tokenCursor=(index+1)%weapons.Count;}
                foreach(var program in r.item.inherited)
                {
                    if(count<=0) break;var d=battle.Run.Find(program.definitionId);
                    if(d==null || !d.HasTag("学徒") || !CanActivate(d,target)) continue;
                    ExecuteProgram(r,d,program.scale,target,program,PrototypeEffectTrigger.Release,suppressHitTriggers);program.AddActivationGrowth(d, program.scale);
                    battle.Presentation?.ShowActivation(this,target,d,program.level);count--;token.tokenCursor=(index+1)%weapons.Count;
                }
            }
        }
        private void ReplayPotions(PrototypeItemInstance item)
        {
            foreach (var recipe in item.recipes)
            {
                if (recipe.stat == PrototypeStat.Gold)
                {
                    if (Id != 1) continue;
                    item.GoldRemainder+=recipe.amount;int gold=Mathf.FloorToInt(item.GoldRemainder);
                    if (gold>0) { item.GoldRemainder-=gold;battle.Run.RecordCombatGold(gold); }
                }
                else if (recipe.lifetime == PrototypePotionLifetime.RunPermanent)
                {
                    if (Id==1) battle.Run.GrantPermanent(recipe.stat,recipe.amount,"注射器:"+item.instanceId);
                    ApplyStat(recipe.stat,recipe.amount,"item:"+item.instanceId+":recipe");
                }
                else ApplyStat(recipe.stat,recipe.amount,"item:"+item.instanceId+":recipe");
            }
            battle.Presentation?.Pulse(this,PrototypeDamageKind.Heal);
        }
        private void AddBattleHealth(string sourceId, float amount)
        {
            if (amount <= 0) return;
            Buffs.AddStack(sourceId, PrototypeStat.Health, amount);
            float actual=amount*balance.RecoveryMultiplier(battle.Elapsed);
            Health += actual;
            battle.Presentation?.Number(this,actual,PrototypeDamageKind.Heal);
        }
        public void ApplyStat(PrototypeStat stat,float amount,string sourceId="recipe")
        {
            switch (stat)
            {
                case PrototypeStat.Health: AddBattleHealth(sourceId, amount);break;
                case PrototypeStat.DamageReduction: Buffs.AddReduction(sourceId,amount);break;
                case PrototypeStat.StartingShield: AddShield(amount);break;
                default: Buffs.AddStack(sourceId,stat,amount);break;
            }
        }
        public void RecordProduced(PrototypeItemInstance item,int amount)
        {foreach(var r in weapons) if(ReferenceEquals(r.item,item)) {r.produced+=amount;return;}}
        public float CooldownRemaining(int index) { foreach (var r in weapons) if (r.slotIndex==index) return Mathf.Max(0,r.readyAt-battle.Elapsed); return 0; }
        public float CooldownDuration(int index) { foreach (var r in weapons) if (r.slotIndex==index) return r.item.HasActive ? Duration(r.item,r.cooldownFactor) : 0; return 0; }
        public float ProductionRemaining(int index) { foreach (var r in weapons) if (r.slotIndex==index) return float.IsInfinity(r.nextProduction) ? 0 : Mathf.Max(0,r.nextProduction-battle.Elapsed); return 0; }
        public int ProducedCount(int index) { foreach (var r in weapons) if (r.slotIndex==index) return r.produced; return 0; }
        public void AddShield(float amount)
        { if (!IsAlive || amount<=0) return;amount*=balance.RecoveryMultiplier(battle.Elapsed);State.AddShield(amount);battle.Presentation?.Number(this,amount,PrototypeDamageKind.Shield); }
        public void Heal(float amount)
        {
            if (!IsAlive || amount<=0) return;
            float actual=Mathf.Min(amount*balance.RecoveryMultiplier(battle.Elapsed),MaximumHealth-Health);Health+=actual;State.CleanseByHealing(actual);
            if (actual>0) {battle.Presentation?.Number(this,actual,PrototypeDamageKind.Heal);battle.Presentation?.Pulse(this,PrototypeDamageKind.Heal);}
        }
        public void ReceiveDamage(float damage,PrototypeDamageKind kind)
        {
            if (!IsAlive || damage<=0 || (IsInvulnerable && kind!=PrototypeDamageKind.SuddenDeath)) return;
            if (kind==PrototypeDamageKind.Impact || kind==PrototypeDamageKind.Melee || kind==PrototypeDamageKind.Ranged) damage*=1-Mathf.Clamp01(damageReduction);
            float shieldBefore=State.Shield;float remaining=State.Absorb(damage,kind);
            float actual=Mathf.Min(Health,remaining);Health-=actual;
            if(actual>0)LastDamageKind=kind;
            battle.ReportDamage(this,actual,kind);
            if (actual>0) battle.Presentation?.Number(this,actual,kind);
            if (State.Shield < shieldBefore) battle.Presentation?.Number(this,shieldBefore-State.Shield,PrototypeDamageKind.ShieldAbsorbed);
            if(IsAlive && shieldBefore>0 && State.Shield<=0)TriggerShieldBroken();
            if (!IsAlive) body.velocity=Vector3.zero;
        }
        private void KeepInsideCircularArena()
        {
            if (battle.CircularArenaRadius <= 0) return;
            Vector3 flat = new Vector3(body.position.x, 0, body.position.z);
            float limit = battle.CircularArenaRadius - radius;
            if (flat.sqrMagnitude < limit * limit) return;
            Vector3 outward = flat.normalized;
            body.position = outward * (limit - .025f) + Vector3.up * body.position.y;
            Vector3 velocity = body.velocity;
            if (Vector3.Dot(velocity, outward) <= 0) return;
            Vector3 toward = battle.OpponentOf(this).transform.position - body.position;
            toward.y = 0;
            Vector3 reflected = Vector3.Reflect(velocity, outward).normalized;
            Vector3 tangent = new Vector3(-outward.z, 0, outward.x);
            float offset = ((wallBounceCount++ + Id) & 1) == 0 ? .12f : -.12f;
            Vector3 heading = (reflected * .8f + toward.normalized * .2f - outward * .35f + tangent * offset).normalized;
            if (Vector3.Dot(heading, outward) > -.25f) heading = (heading - outward * .8f).normalized;
            body.velocity = heading * Mathf.Min(balance.maximumSpeed, Mathf.Max(TargetSpeed * .85f, velocity.magnitude));
        }

        public float ImpactMultiplierFor(Vector3 worldDirection)
        {
            float multiplier=1;
            foreach(var r in weapons)
            {
                multiplier+=ImpactContribution(r.item.definition.weapon,r.item.EffectMultiplier,worldDirection);
                foreach(var p in r.item.inherited) multiplier+=ImpactContribution(battle.Run.Find(p.definitionId)?.weapon,p.scale,worldDirection);
            }
            return multiplier;
        }
        private float ImpactContribution(PrototypeWeaponDefinition weapon,float scale,Vector3 direction)
        {
            if(weapon==null || !weapon.modifiesImpact || Vector3.Angle(ring.forward,direction)>weapon.meleeHalfAngle) return 0;
            return (weapon.impactMultiplier-1)*scale;
        }

        public void SpendImpactEnergy(float strength)
        {
            Energy = Mathf.Max(0f, Energy - balance.impactEnergyBase - balance.impactEnergyPerStrength * strength);
        }

        public void ApplyKnockback(Vector3 direction, float impulse)
        {
            if (!IsAlive || impulse <= 0f || direction.sqrMagnitude < .001f) return;
            direction.y = 0f;
            body.AddForce(direction.normalized * impulse, ForceMode.Impulse);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (battle == null || !battle.IsFighting) return;
            if (collision.collider.name.EndsWith("Wall")) { ReturnFromWall(collision); return; }
            PrototypeActor other = collision.collider.GetComponent<PrototypeActor>();
            if (other != null && Id < other.Id)
            {
                float normalSpeed = collision.contactCount > 0
                    ? Mathf.Abs(Vector3.Dot(collision.relativeVelocity, collision.GetContact(0).normal))
                    : collision.relativeVelocity.magnitude;
                battle.ResolveActorCollision(this, other, normalSpeed);
            }
        }

        public void ShowPassiveCollision(PrototypeActor other)
        {
            foreach(var runtime in weapons)
            {
                var d=runtime.item.definition;
                if(d.HasActive || d.productionInterval>0)continue;
                // Static equipment reacts to a real collision; materials have no invented combat trigger.
                if(d.kind==PrototypeItemKind.Equipment && (d.healthBonus!=0 || d.speedBonus!=0 || d.impactBonus!=0 || d.impactTakenReduction!=0))
                    battle.Presentation?.ShowActivation(this,other,d,runtime.item.level);
            }
        }
        private void OnCollisionStay(Collision collision)
        {
            if (battle != null && battle.IsFighting && collision.collider.name.EndsWith("Wall"))
                ReturnFromWall(collision);
        }

        private void ReturnFromWall(Collision collision)
        {
            if (battle.Elapsed < nextWallReturnAt) return;
            PrototypeActor opponent = battle.OpponentOf(this);
            if (opponent == null) return;
            Vector3 inward = collision.collider.name.StartsWith("East") ? Vector3.left :
                collision.collider.name.StartsWith("West") ? Vector3.right :
                collision.collider.name.StartsWith("North") ? Vector3.back : Vector3.forward;
            Vector3 toward = opponent.transform.position - transform.position;
            toward.y = 0f;
            Vector3 reflected = Vector3.Reflect(preCollisionVelocity.sqrMagnitude > 0.01f
                ? preCollisionVelocity : body.velocity, inward);
            Vector3 heading = (reflected.normalized * (1f - balance.wallReturnStrength) +
                               toward.normalized * balance.wallReturnStrength + inward * 0.45f).normalized;
            Vector3 tangent = new Vector3(-inward.z, 0f, inward.x);
            float offset = ((wallBounceCount++ + Id) & 1) == 0 ? .16f : -.16f;
            heading = (heading + tangent * offset).normalized;
            if (Vector3.Dot(heading, inward) < 0.35f)
                heading = (heading + inward * 0.8f).normalized;
            body.velocity = heading * Mathf.Max(TargetSpeed * 0.85f, body.velocity.magnitude);
            body.position += inward * 0.035f;
            nextWallReturnAt = battle.Elapsed + balance.wallReturnCooldown;
        }
    }
}
