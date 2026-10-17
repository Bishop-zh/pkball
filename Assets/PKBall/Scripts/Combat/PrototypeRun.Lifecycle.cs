using System;
using System.Collections.Generic;

namespace PKBall.Prototype
{
    public enum PrototypeRunPhase { Preparation, Aiming, Fighting, Resolving, Ended }

    public sealed partial class PrototypeRun
    {
        public System.Collections.ObjectModel.ReadOnlyCollection<PrototypeItemInstance> Bag { get; }
        public System.Collections.ObjectModel.ReadOnlyCollection<PrototypeItemInstance> PendingRewards { get; }
        public System.Collections.ObjectModel.ReadOnlyCollection<PrototypeItemInstance> Equipped { get; }
        public System.Collections.ObjectModel.ReadOnlyCollection<PrototypeItemDefinition> Shop { get; }
        public System.Collections.ObjectModel.ReadOnlyCollection<PrototypeSlotEffect> SlotEffects { get; }
        public System.Collections.ObjectModel.ReadOnlyCollection<PrototypeStatModifier> PermanentModifiers { get; }
        public System.Collections.ObjectModel.ReadOnlyCollection<PrototypePendingModifier> PendingBattleModifiers { get; }
        public PrototypeRunPhase Phase { get; private set; }
        public string ActiveBattleId { get; private set; }
        private readonly HashSet<string> committedBattles = new HashSet<string>();
        private bool combatActive => Phase == PrototypeRunPhase.Fighting || Phase == PrototypeRunPhase.Resolving;
        public bool CanEditPreparation => Phase == PrototypeRunPhase.Preparation && !GameOver && !Parked && (!Cleared || Endless);

        public void Restock() { if (CanEditPreparation) RestockCore(); }
        public void AddOre(int count) { if (CanEditPreparation) AddOreCore(count); }
        public void EndDay(bool won) { if (CanEditPreparation) EndDayCore(won); }

        public bool BeginAiming()
        {
            if (Phase == PrototypeRunPhase.Aiming) return true;
            if (!CanEditPreparation || !CanFight) return false;
            Phase = PrototypeRunPhase.Aiming;
            return true;
        }

        public void CancelAiming()
        {
            if (Phase == PrototypeRunPhase.Aiming) Phase = PrototypeRunPhase.Preparation;
        }

        public void ResolveCombat(bool won) => ResolveCombat(ActiveBattleId, won);

        public bool ResolveCombat(string battleId, bool won)
        {
            if (Phase != PrototypeRunPhase.Fighting || string.IsNullOrEmpty(battleId) ||
                battleId != ActiveBattleId || committedBattles.Contains(battleId) || GameOver || Parked ||
                (Stage != PrototypeDayStage.Monster && Stage != PrototypeDayStage.Rival)) return false;
            Phase = PrototypeRunPhase.Resolving;
            try
            {
                ResolveCombatCore(won);
                return true;
            }
            finally
            {
                Phase = GameOver || (Cleared && !Endless) ? PrototypeRunPhase.Ended : PrototypeRunPhase.Preparation;
                Save();
            }
        }
    }
}
