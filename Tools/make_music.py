import numpy as np, wave
from pathlib import Path
r=Path(__file__).resolve().parents[1]/'Assets/Casino/Audio';sr=32000;bpm=108;beat=60/bpm;bars=8;dur=bars*4*beat
rng=np.random.default_rng(108);mix=np.zeros((round(dur*sr),2),np.float64)
def note(midi,length,kind):
 t=np.arange(round(length*sr))/sr;f=440*2**((midi-69)/12)
 if kind=='keys':
  y=(np.sin(2*np.pi*f*t)+.28*np.sin(2*np.pi*2*f*t)+.1*np.sin(2*np.pi*3*f*t))*np.exp(-2.8*t)
 elif kind=='bass':y=(np.sin(2*np.pi*f*t)+.25*np.sin(2*np.pi*2*f*t))*np.exp(-5*t)
 else:y=(np.sin(2*np.pi*f*t)+.35*np.sin(2*np.pi*3*f*t)*np.exp(-10*t))*np.exp(-4*t)
 return y*np.minimum(t/.008,1)*np.minimum((length-t)/.04,1)
def add(y,time,vol,pan=0):
 st=round(time*sr);n=min(len(y),len(mix)-st)
 if n>0:mix[st:st+n,0]+=y[:n]*vol*(1-pan*.4);mix[st:st+n,1]+=y[:n]*vol*(1+pan*.4)
chords=[[62,65,69,72,76],[55,65,69,71,76],[60,64,67,71,74],[57,61,67,70,76],[53,57,60,64,67],[59,62,65,69,74],[52,55,59,62,66],[57,61,64,67,70]]
roots=[38,43,36,45,41,35,40,45]
for bar,(ch,root) in enumerate(zip(chords,roots)):
 offset=bar*4*beat
 for hit in [0,1.65,2.5]:
  for j,m in enumerate(ch):add(note(m,1.5,'keys'),offset+hit*beat+j*.008,.06,-.45)
 for k,interval in enumerate([0,7,12,7]):add(note(root+interval,.42,'bass'),offset+k*beat,.21,.05)
 for k in range(8):
  t=np.arange(round(.07*sr))/sr;noise=rng.normal(size=len(t));noise=np.r_[0,np.diff(noise)];add(noise*np.exp(-t*55),offset+(k*.5+(.10 if k%2 else 0))*beat,.012,.55)
 for k in [0,2]:
  t=np.arange(round(.18*sr))/sr;add(np.sin(2*np.pi*(58*t-18*t*t))*np.exp(-t*25),offset+k*beat,.12)
 for k in [1,3]:
  t=np.arange(round(.16*sr))/sr;add(rng.normal(size=len(t))*np.exp(-t*35),offset+k*beat,.015,-.2)
 for k,j in [(0.5,2),(1.25,4),(2.75,3),(3.5,1)]:add(note(ch[j]+12,.65,'vibes'),offset+k*beat,.035,.5)
# Soft limiter and tiny boundary ramps preserve the original musical loop.
mix=np.tanh(mix*1.6);mix*=.85/max(np.max(np.abs(mix)),.85);fade=round(.012*sr);mix[:fade]*=np.linspace(0,1,fade)[:,None];mix[-fade:]*=np.linspace(1,0,fade)[:,None]
def write(name,y):
 with wave.open(str(r/name),'wb') as f:f.setnchannels(2 if y.ndim==2 else 1);f.setsampwidth(2);f.setframerate(sr);f.writeframes((np.clip(y,-1,1)*32767).astype('<i2').tobytes())
write('lounge_original.wav',mix)
t=np.arange(round(.38*sr))/sr;noise=rng.normal(size=len(t));paper=np.convolve(noise,np.ones(5)/5,mode='same')*(np.sin(np.pi*t/.38)**2)*.22;write('paper.wav',paper)
y=np.zeros(round(.65*sr))
for st,m in [(0,76),(.07,81),(.15,88)]:
 v=note(m,.42,'vibes')*.25;i=round(st*sr);n=min(len(v),len(y)-i);y[i:i+n]+=v[:n]
write('register.wav',y)
print('Original lounge loop',round(dur,2),'seconds; peak',round(float(abs(mix).max()),3))
