namespace PKBall.Prototype
{
    public enum PrototypeDayStage { EventOne, EventTwo, Monster, EventThree, EventFour, Rival }
    public enum PrototypeEventKind { Shop, Gold, Ore, Pickaxe, Weapon, Injections, Enchant, MasterTraining, Rest, Trade }
    public enum PrototypeSlotEffect { None, Vital, Swift, Sharpened, Echo, Guarded, Prosperity }

    public static class PrototypeDayRules
    {
        private static readonly PrototypeEventKind[] ExtraEvents =
        {
            PrototypeEventKind.Gold, PrototypeEventKind.Ore, PrototypeEventKind.Pickaxe,
            PrototypeEventKind.Weapon, PrototypeEventKind.Injections, PrototypeEventKind.Enchant,
            PrototypeEventKind.MasterTraining, PrototypeEventKind.Rest, PrototypeEventKind.Trade
        };

        public static PrototypeEventKind EventAt(int day, PrototypeDayStage stage, int choice)
        {
            if (choice == 0) return PrototypeEventKind.Shop;
            int seed = (day * 7 + (int)stage * 3) % ExtraEvents.Length;
            return ExtraEvents[(seed + choice - 1) % ExtraEvents.Length];
        }

        public static string EventDescription(PrototypeEventKind kind, int day)
        {
            switch (kind)
            {
                case PrototypeEventKind.Shop: return "进入商店：挑选 4 件商品，可拖拽购买";
                case PrototypeEventKind.Gold: return "金币补给：立即获得 " + (6 + day / 2) + " 金币";
                case PrototypeEventKind.Ore: return "矿藏：获得 3 块金矿，可出售";
                case PrototypeEventKind.Pickaxe: return "矿工遗物：获得一把 1 级矿镐";
                case PrototypeEventKind.Weapon: return "武器箱：随机获得一件 1 级武器";
                case PrototypeEventKind.Injections: return "药剂工坊：获得 2 支随机针剂";
                case PrototypeEventKind.Enchant: return "附魔台：强化一个开放的格位";
                case PrototypeEventKind.MasterTraining:return "宗师训练：当前所有学徒工具各增加 3 次成长";
                case PrototypeEventKind.Rest:return "生命休整：本体生命永久 +20";
                case PrototypeEventKind.Trade:return "商旅资助：获得 10 金币";
                default: return "未知事件";
            }
        }

        public static string SlotDescription(PrototypeSlotEffect effect)
        {
            switch (effect)
            {
                case PrototypeSlotEffect.Vital: return "活力：装备后生命 +8，生命属性再提高 20%";
                case PrototypeSlotEffect.Swift: return "迅捷：装备后速度 +4，速度属性再提高 20%";
                case PrototypeSlotEffect.Sharpened: return "锋利附魔：该格武器伤害 +20%";
                case PrototypeSlotEffect.Echo: return "回响附魔：该格武器冷却缩短 15%";
                case PrototypeSlotEffect.Guarded: return "守护：装备后撞击减伤 +5%";
                case PrototypeSlotEffect.Prosperity: return "富饶：每回合结算金币 +2";
                default: return "普通格位：无加成";
            }
        }
    }
}
