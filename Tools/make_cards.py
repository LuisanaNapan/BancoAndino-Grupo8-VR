from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
r=Path(__file__).resolve().parents[1]/'Assets/Casino/Branding';font=ImageFont.truetype(str(r.parent/'Fonts/CasinoDisplay.ttf'),180)
for val in [7,6]:
 im=Image.new('RGB',(560,800),(246,242,227));d=ImageDraw.Draw(im);d.rounded_rectangle((7,7,552,792),radius=34,outline=(182,139,64),width=9);color=(151,31,47) if val==7 else (17,38,47)
 d.text((40,20),str(val),font=font,fill=color);box=d.textbbox((0,0),str(val),font=font);d.text(((560-(box[2]-box[0]))/2,295),str(val),font=font,fill=color)
 f=ImageFont.truetype(str(r.parent/'Fonts/CasinoControls.ttf'),35);caption='BANCA' if val==7 else 'JUGADOR';box=d.textbbox((0,0),caption,font=f);d.text(((560-(box[2]-box[0]))/2,640),caption,font=f,fill=color)
 im.save(r/f'Card{val}.png')
im=Image.new('RGB',(560,800),(12,53,65));d=ImageDraw.Draw(im)
for pad in [10,25,46]:d.rounded_rectangle((pad,pad,560-pad,800-pad),radius=30,outline=(229,184,99),width=4)
for y in range(85,740,65):
 for x in range(70,520,65):d.polygon([(x,y-14),(x+14,y),(x,y+14),(x-14,y)],outline=(175,135,65),width=2)
im.save(r/'CardBack.png')
print('High contrast faces and patterned card backs generated')
