from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
import numpy as np,shutil
r=Path(__file__).resolve().parents[1]/'Assets/Casino';W,H=2048,640
f=ImageFont.truetype(str(r/'Fonts/CasinoDisplay.ttf'),190);small=ImageFont.truetype(str(r/'Fonts/CasinoBody.ttf'),39)
for name,title,subtitle,c in [('Casino','HORIZONTE','C A S I N O   /   B A C A R A',np.array([28,8,23])),('Cambio','ANDINO','C A S A   D E   C A M B I O',np.array([4,27,35]))]:
 yy,xx=np.mgrid[:H,:W];glow=np.exp(-(((xx-W/2)/(W*.5))**2+((yy-H/2)/(H*.65))**2));a=np.zeros((H,W,3),np.uint8)
 for i in range(3):a[:,:,i]=(c[i]+glow*(22 if i==0 else 12)).clip(0,255)
 im=Image.fromarray(a);d=ImageDraw.Draw(im);gold=(235,188,104)
 for pad,width in [(20,5),(38,2),(52,1)]:d.rectangle((pad,pad,W-pad,H-pad),outline=gold,width=width)
 for x in [115,W-115]:
  for y in [115,H-115]:d.polygon([(x,y-20),(x+20,y),(x,y+20),(x-20,y)],outline=gold,width=4)
 box=d.textbbox((0,0),title,font=f);d.text(((W-(box[2]-box[0]))/2,135),title,font=f,fill=gold,stroke_width=1)
 box=d.textbbox((0,0),subtitle,font=small);d.text(((W-(box[2]-box[0]))/2,410),subtitle,font=small,fill=(240,224,191))
 for x in [W/2-260,W/2+260]:d.line((x-140,510,x+140,510),fill=gold,width=2)
 d.polygon([(W/2,493),(W/2+17,510),(W/2,527),(W/2-17,510)],fill=gold)
 im.save(r/'Branding'/(name+'.png'))
# The font license is already included beside the fonts.
print('Original Art Deco branding generated; licensed fonts copied')
