"""Deterministic budget model, NOT a physics/win-rate estimator.
Release-only items; guaranteed hits, no travel, criticals, inherited programs,
hit triggers, master synergies or collisions. Run from the project root.
"""
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def catalogue(path):
    return {e['item']['itemId']: e for e in json.loads(path.read_text(encoding='utf-8-sig'))['items']}


class Fighter:
    def __init__(self, ids, data, level=1, training=False):
        self.hp = 300.0
        self.shield = self.poison = self.burn = self.regen = 0.0
        self.training = training
        self.totals = dict(direct=0., poison=0., burn=0., heal=0., shield=0.)
        self.items = []
        for id in ids:
            e = data[id]
            if any(f.get('trigger', 0) != 0 or f['kind'] > 5 for f in e['item'].get('effects', [])):
                raise ValueError('Unsupported triggered/synergy item: ' + id)
            if id == 'green_flying_sword':
                raise ValueError('Persistent summons require the engine simulation')
            cd = (e['weapon']['cooldown'] if e.get('hasWeapon') else e['item']['activationCooldown'])
            self.items.append(dict(entry=e, cd=cd, next=cd, casts=0, level=level))

    def damage(self, value, kind):
        if kind not in ('poison', 'overtime'):
            conversion = 2 if kind == 'burn' else 1
            consumed = min(self.shield, value / conversion)
            self.shield -= consumed
            value -= consumed * conversion
        if kind in self.totals:
            self.totals[kind] += value
        self.hp -= value

    def heal(self, value):
        actual = value if self.training else min(value, max(0, 300-self.hp))
        self.totals['heal'] += actual
        self.hp += actual
        self.poison = max(0, self.poison-actual*.1)

    def tick(self, step, recovery):
        if step % 10 == 0:
            self.damage(self.poison, 'poison')
        if step % 5 == 0:
            self.damage(self.burn, 'burn')
            self.burn = max(0, self.burn-max(1, math.ceil(self.burn*.05)))
        if step % 10 == 0 and (self.hp > 0 or self.training):
            self.heal(self.regen * recovery)

    def cast(self, time, other, recovery):
        for runtime in self.items:
            if time+1e-6 < runtime['next']:
                continue
            runtime['next'] = time+runtime['cd']
            entry, casts, level = runtime['entry'], runtime['casts'], runtime['level']
            multiplier = 2 ** (level-1)
            weapon = entry.get('weapon') if entry.get('hasWeapon') else None
            if weapon:
                other.damage((weapon['damage']+casts*weapon['growthPerActivation'])*multiplier, 'direct')
            for effect in entry['item'].get('effects', []):
                levels, growths = effect.get('levelAmounts'), effect.get('levelGrowthAmounts')
                amount = levels[level-1] if levels and len(levels) == 4 else effect['amount']*multiplier
                growth = growths[level-1] if growths and len(growths) == 4 else effect['growthPerActivation']*multiplier
                value, kind = amount+casts*growth, effect['kind']
                if kind == 0: other.damage(value, 'direct')
                elif kind == 1:
                    self.shield += value*recovery
                    self.totals['shield'] += value*recovery
                elif kind == 2: self.heal(value*recovery)
                elif kind == 3: other.poison += value
                elif kind == 4: other.burn += value
                elif kind == 5: self.regen += value
            runtime['casts'] += 1


def simulate(left, right, data, level=1, training=False, duration=60):
    a, b = Fighter(left, data, level, training), Fighter(right, data, level, training)
    for step in range(1, int(duration*10)+1):
        time = step/10
        progress = 0 if training else max(0, min(1, (time-30)/30))
        recovery = 1-.9*progress
        a.tick(step, recovery); b.tick(step, recovery)
        if training or (a.hp > 0 and b.hp > 0):
            # Simultaneous release budget; physics engine resolves actual hit timing.
            a.cast(time, b, recovery); b.cast(time, a, recovery)
        if not training and time >= 30:
            a.damage((3+42*progress)*.1, 'overtime'); b.damage((3+42*progress)*.1, 'overtime')
        if not training and min(a.hp, b.hp) <= 0:
            break
    return a, b, time


def main():
    current = catalogue(ROOT/'Assets/PKBall/Resources/Data/items.json')
    old = catalogue(ROOT/'Artifacts/Balance/items-before.json')
    rows = []
    for id in ['greatsword', 'shield_emitter', 'apprentice_shield', 'apprentice_spring', 'apprentice_venom', 'apprentice_flame', 'flame_axe', 'poison_dagger', 'master_spring', 'plague_heart', 'saint_seal']:
        for name, data in [('before', old), ('after', current)]:
            # Master support effect is not modeled; evaluate its heal component alone.
            data = json.loads(json.dumps(data))
            for entry in data.values():
                entry['item']['effects'] = [f for f in entry['item'].get('effects', []) if f.get('trigger', 0) == 0 and f['kind'] <= 5]
            for level in [1, 2, 4]:
                a,b,_ = simulate([id], [], data, level, True, 30)
                rows.append(dict(item=id, version=name, level=level, **{k: round((b if k in ('direct','poison','burn') else a).totals[k], 3) for k in a.totals}, regen_at_30=round(a.regen,3)))
    duels=[]
    cases=[(['greatsword']*3, ['apprentice_spring']),
           (['greatsword']*3, ['apprentice_spring']*3),
           (['greatsword','shield_emitter'], ['greatsword','apprentice_spring']),
           (['greatsword','apprentice_venom'], ['greatsword','shield_emitter']),
           (['shield_emitter'], ['shield_emitter']),
           (['greatsword','apprentice_spring'], ['greatsword','apprentice_spring'])]
    for left,right in cases:
        for level in [1,2]:
            a,b,t=simulate(left,right,current,level)
            duels.append(dict(left=left,right=right,level=level,seconds=t,left_hp=round(max(0,a.hp),2),right_hp=round(max(0,b.hp),2)))
    after={r['item']:r for r in rows if r['version']=='after' and r['level']==1}
    assert after['apprentice_spring']['heal'] < after['greatsword']['direct']/3
    assert after['apprentice_venom']['poison'] < after['greatsword']['direct']/3
    assert after['apprentice_flame']['burn'] < after['greatsword']['direct']/3
    assert after['shield_emitter']['shield'] >= after['greatsword']['direct']
    assert duels[0]['right_hp']==0 and duels[0]['seconds']<30
    out=ROOT/'Artifacts/Balance';out.mkdir(exist_ok=True)
    (out/'simulation.json').write_text(json.dumps(dict(assumptions=__doc__,budgets=rows,duels=duels),ensure_ascii=False,indent=2),encoding='utf-8')
    lines=['# 30 秒数值预算模拟','', '假设：300 生命、零历史成长、保证命中、无走位/碰撞/飞行延迟/暴击/命中触发/师徒联动。只用于比较收益，不是实战胜率。治疗预算假设始终缺血；对局则限制到生命上限。决胜使用 30～60 秒、3→45 失血/秒、恢复 100%→10%。','', '| 道具 | 版本 | 等级 | 直伤 | 毒伤 | 火伤 | 治疗上限 | 获盾 | 30秒再生/秒 |','|---|---|---:|---:|---:|---:|---:|---:|---:|']
    for r in rows:
        lines.append('| '+ ' | '.join(str(r[k]) for k in ['item','version','level','direct','poison','burn','heal','shield','regen_at_30'])+' |')
    lines += ['', '## 简化对局', '', '| 左方 | 右方 | 等级 | 结束秒数 | 左剩余生命 | 右剩余生命 |', '|---|---|---:|---:|---:|---:|']
    for r in duels:
        lines.append(f"| {' + '.join(r['left'])} | {' + '.join(r['right'])} | {r['level']} | {r['seconds']} | {r['left_hp']} | {r['right_hp']} |")
    (out/'simulation.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
    print(json.dumps(dict(level_one=list(after.values()),duels=duels),ensure_ascii=False,indent=2))


if __name__ == '__main__':
    main()
