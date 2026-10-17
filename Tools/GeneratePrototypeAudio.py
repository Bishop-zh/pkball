"""Rebuild original procedural prototype SFX; no third-party recordings are used."""
from pathlib import Path
import math, random, struct, wave, json
ROOT=Path(__file__).resolve().parents[1]
RATE=22050
SOUNDS=[('sword_swing','刀剑挥击','release',.24),('heavy_swing','重锤挥击','release',.34),('arrow_fire','弓弩发射','release',.15),('gun_fire','轻型射击','release',.16),('cannon_fire','炮弹发射','release',.48),('laser_fire','激光发射','release',.28),('metal_hit','金属命中','hit',.28),('poison_hit','毒液飞溅','both',.36),('fire_hit','火焰灼烧','both',.40),('shield_gain','护盾共鸣','both',.48),('heal_chime','治疗铃音','both',.60)]
def generate(identifier,duration):
    rng=random.Random(identifier);output=[];filtered=0;phase=0
    for i in range(int(duration*RATE)):
        t=i/RATE;u=t/duration;noise=rng.uniform(-1,1);filtered=.82*filtered+.18*noise
        attack=min(1,t/.008);tail=(1-u)**2
        if identifier in ('sword_swing','heavy_swing'):
            value=filtered*math.sin(math.pi*u)**1.2*2.2+noise*.10*tail
            if identifier=='heavy_swing':value+=.28*math.sin(2*math.pi*(65*t))*tail
        elif identifier=='arrow_fire':value=.7*filtered*math.exp(-t*24)+.18*math.sin(2*math.pi*620*t)*math.exp(-t*32)
        elif identifier=='gun_fire':value=.4*noise*math.exp(-t*32)+.35*math.sin(2*math.pi*140*t)*math.exp(-t*28)
        elif identifier=='cannon_fire':value=.45*filtered*math.exp(-t*9)+.45*math.sin(2*math.pi*(100*t-55*t*t))*math.exp(-t*10)
        elif identifier=='laser_fire':
            phase+=2*math.pi*(1600*(1-u)+240)/RATE;value=(.4*math.sin(phase)+.08*math.sin(2*phase))*tail
        elif identifier=='metal_hit':value=sum(math.sin(2*math.pi*f*t)*a for f,a in [(700,.26),(1177,.15),(2130,.08)])*math.exp(-t*17)+noise*.15*math.exp(-t*90)
        elif identifier=='poison_hit':value=filtered*.65*math.exp(-t*8)+.2*math.sin(2*math.pi*(260*t+460*t*t))*tail
        elif identifier=='fire_hit':value=filtered*.8*tail+noise*.09*math.exp(-t*14)
        elif identifier=='shield_gain':value=(.27*math.sin(2*math.pi*440*t)+.13*math.sin(2*math.pi*880*t)+.10*math.sin(2*math.pi*1320*t))*tail
        else:
            value=0
            for k,f in enumerate([523.25,659.25,783.99]):
                elapsed=t-k*.11
                if elapsed>=0:value+=.22*math.sin(2*math.pi*f*elapsed)*math.exp(-elapsed*8)*min(1,elapsed/.006)
            value*=tail
        output.append(max(-.65,min(.65,value*attack))*min(1,(duration-t)/.015))
    return output
if __name__=='__main__':
    dest=ROOT/'Assets/PKBall/Resources/Audio/Sfx';dest.mkdir(parents=True,exist_ok=True)
    entries=[]
    for identifier,name,phase,duration in SOUNDS:
        samples=generate(identifier,duration)
        with wave.open(str(dest/(identifier+'.wav')),'wb') as file:
            file.setnchannels(1);file.setsampwidth(2);file.setframerate(RATE);file.writeframes(b''.join(struct.pack('<h',round(v*32767)) for v in samples))
        entries.append(dict(id=identifier,displayName=name,phase=phase,volume=.55))
    (ROOT/'Assets/PKBall/Resources/Data/sounds.json').write_text(json.dumps(dict(sounds=entries),ensure_ascii=False,indent=2),encoding='utf-8')
    print('Created',len(entries),'original mono PCM WAV effects')