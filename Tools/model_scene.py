import math,json
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[1]/'Assets/Casino'
OUT=ROOT/'Models'; OUT.mkdir(exist_ok=True)
COLORS={'cream':(0.76,.73,.63),'plaster':(.91,.87,.76),'wood':(.19,.085,.05),'woodlight':(.32,.17,.09),'gold':(.70,.47,.18),'brasslight':(.96,.75,.39),'teal':(.035,.24,.24),'felt':(.035,.27,.19),'navy':(.035,.07,.09),'burgundy':(.26,.035,.065),'velvet':(.48,.055,.09),'paper':(.93,.9,.79),'black':(.018,.025,.027),'screen':(.03,.48,.49),'light':(1,.83,.48),'carpet':(.105,.065,.09),'carpetline':(.27,.15,.16),'blue':(.06,.29,.55)}
class Model:
 def __init__(self):self.parts=[]
 def add(self,v,f,mat):self.parts.append((np.array(v,float),np.array(f,int),mat))
 def box(self,p,s,mat,angle=0):
  v=np.array([[-1,-1,-1],[1,-1,-1],[1,1,-1],[-1,1,-1],[-1,-1,1],[1,-1,1],[1,1,1],[-1,1,1]],float)*np.array(s)/2
  a=math.radians(angle);r=np.array([[math.cos(a),0,math.sin(a)],[0,1,0],[-math.sin(a),0,math.cos(a)]])
  v=v@r.T+np.array(p)
  self.add(v,[[0,2,1],[0,3,2],[4,5,6],[4,6,7],[0,1,5],[0,5,4],[3,7,6],[3,6,2],[0,4,7],[0,7,3],[1,2,6],[1,6,5]],mat)
 def cyl(self,p,r,h,mat,n=24,scalez=1,top=None):
  top=r if top is None else top
  v=[]
  for y,rad in [(-h/2,r),(h/2,top)]:
   for i in range(n):a=i*math.tau/n;v.append([p[0]+rad*math.cos(a),p[1]+y,p[2]+rad*math.sin(a)*scalez])
  v.extend([[p[0],p[1]-h/2,p[2]],[p[0],p[1]+h/2,p[2]]]);f=[]
  for i in range(n):j=(i+1)%n;f.extend([[i,n+j,n+i],[i,j,n+j],[2*n,j,i],[2*n+1,n+i,n+j]])
  self.add(v,[[a,c,b] for a,b,c in f],mat)
 def leaf(self,p,angle,tilt):
  a=math.radians(angle);d=np.array([math.cos(a),0,math.sin(a)]);c=np.array(p);w=np.array([-d[2],0,d[0]])*.12
  v=[c,c+d*.35+w+np.array([0,tilt*.7,0]),c+d*.9+np.array([0,tilt,0]),c+d*.35-w+np.array([0,tilt*.7,0])];self.add(v,[[0,1,2],[0,2,3],[2,1,0],[3,2,0]],'felt')
 def plant(self,x,z):
  self.cyl((x,.3,z),.23,.6,'wood',top=.35)
  self.cyl((x,.61,z),.36,.05,'gold')
  for a in range(0,360,45): self.leaf((x,.64,z),a,.5); self.leaf((x,.7,z),a+22,1.2)
 def chair(self,x,z,angle=0):
  # Upholstered club chair; oriented to +Z before rotation.
  def b(p,s,m):
   a=math.radians(angle);dx,dz=p[0],p[2];self.box((x+dx*math.cos(a)+dz*math.sin(a),p[1],z-dx*math.sin(a)+dz*math.cos(a)),s,m,angle)
  for dx in [-.27,.27]:
   for dz in [-.24,.24]: b((dx,.25,dz),(.055,.5,.055),'gold')
  b((0,.55,0),(.72,.17,.66),'velvet');b((0,.91,-.28),(.72,.64,.13),'velvet')
  for dx in [-.36,.36]:b((dx,.76,.02),(.12,.24,.64),'woodlight')
  b((0,1.25,-.28),(.72,.04,.14),'gold')
 def lamp(self,x,z):
  self.cyl((x,4.7,z),.015,.6,'gold',12)
  self.cyl((x,4.18,z),.46,.48,'gold',32,top=.18)
  self.cyl((x,3.94,z),.42,.025,'light',32)
 def slot(self,x,z):
  self.box((x,.57,z),(.72,1.14,.6),'wood');self.box((x,1.65,z),(.75,1.15,.38),'gold')
  self.box((x,1.64,z-.205),(.65,.94,.04),'black');self.box((x,1.65,z-.232),(.52,.68,.02),'burgundy')
  for i in [-1,0,1]:
   self.box((x+i*.17,1.65,z-.25),(.14,.29,.02),'paper');self.cyl((x+i*.17,1.65,z-.265),.035,.01,'gold',12)
  self.box((x,1.06,z-.2),(.8,.12,.65),'black');self.box((x,1.13,z-.41),(.49,.015,.12),'teal')
 def room(self,casino):
  self.box((0,-.12,0),(12,.24,12),'wood')
  self.box((0,.005,0),(11.8,.02,11.8),'carpet' if casino else 'cream')
  if casino:
   for i in range(-11,12):
    self.box((i*.52,.021,0),(.016,.008,11.7),'carpetline',25)
   for x in [-4,4]:self.box((x,.025,0),(.035,.008,11.6),'gold')
  else:
   for i in range(-5,6):
    self.box((i,.02,0),(.012,.008,11.8),'plaster');self.box((0,.02,i),(11.8,.008,.012),'plaster')
  for p,s in [((0,2.5,5.9),(12,5,.18)),((-5.9,2.5,0),(.18,5,12)),((5.9,2.5,0),(.18,5,12)),((0,2.5,-5.9),(12,5,.18))]:self.box(p,s,'burgundy' if casino else 'plaster')
  self.box((0,5.05,0),(12,.12,12),'navy' if casino else 'cream')
  for x in [-5.78,5.78]:
   self.box((x,.6,0),(.08,1.2,11.8),'wood');self.box((x,1.23,0),(.1,.055,11.8),'gold')
   self.box((x,4.65,0),(.12,.15,11.8),'woodlight')
  self.box((0,.6,5.76),(11.8,1.2,.08),'wood');self.box((0,1.23,5.7),(11.8,.055,.09),'gold')
  self.box((0,4.65,5.73),(11.8,.15,.12),'woodlight')
  for x in [-5,-3.7,3.7,5]:
   self.box((x,2.9,5.7),(.08,3.3,.1),'gold')
  for x in [-5.5,5.5]:
   for z in [-4,3.5]:
    self.box((x,2.5,z),(.36,5,.4),'woodlight');self.box((x,4.9,z),(.5,.15,.55),'gold')
  for x,z in [(-2,-1.5),(2,-1.5),(-2,2.8),(2,2.8)]: self.lamp(x,z)
  for x in [-4.8,4.8]:self.plant(x,4.4)
  # Inset information screen, clear of furniture.
  self.box((0,2.96,5.55),(6.6,2.25,.15),'woodlight');self.box((0,2.96,5.45),(6.4,2.08,.08),'gold');self.box((0,2.96,5.38),(6.24,1.94,.04),'navy')
  # Side control station and portal architectural frame.
  self.box((-4.25,.6,-.5),(1.35,1.2,.8),'wood');self.box((-4.25,1.25,-.5),(1.48,.1,.9),'gold')
  for x in [3.6,5.2]:self.box((x,1.6,2.7),(.16,3.2,.25),'woodlight');self.box((x,1.6,2.54),(.035,3.2,.04),'light')
  self.box((4.4,3.25,2.7),(1.76,.18,.25),'gold');self.box((4.4,.035,2.3),(1.65,.045,1.6),'teal')
  if not casino:
   self.box((0,.56,.5),(6.3,1.12,1.05),'wood');self.box((0,1.16,.45),(6.5,.12,1.26),'cream')
   self.box((0,.6,-.044),(6.1,.7,.04),'teal')
   for x in [-2.85,0,2.85]:self.box((x,.6,-.074),(.02,.7,.02),'gold')
   # Three readable physical evidence props.
   self.box((-2,1.27,.35),(1.05,.1,.67),'burgundy');self.box((-2,1.33,.32),(.97,.035,.59),'paper')
   self.box((-2,1.355,.32),(.018,.012,.6),'gold')
   for i in range(6):self.box((-2,1.355,.12+i*.07),(.81,.006,.009),'woodlight')
   self.box((0,1.25,.35),(.7,.025,.6),'paper')
   for i in range(4):self.box((0,1.266,.15+i*.08),(.5,.003,.013),'woodlight')
   self.box((2,1.5,.63),(.08,.65,.08),'black');self.box((2,1.76,.56),(.95,.68,.08),'black');self.box((2,1.76,.509),(.84,.56,.015),'screen')
   self.box((2,1.26,.24),(.8,.04,.3),'black')
   for x in [-4.9,-4]:self.chair(x,-3,180)
   # Side window with cool daylight.
   self.box((-5.74,2.8,.7),(.04,2.1,3.3),'gold');self.box((-5.70,2.8,.7),(.03,1.98,3.15),'screen')
   for z in [-.9,.15,1.2,2.25]:self.box((-5.66,2.8,z),(.04,2.04,.04),'wood')
  else:
   # Rounded baccarat table with brass rim and wood pedestal.
   self.cyl((0,.55,.7),.66,1.1,'wood',40,scalez=.7)
   self.cyl((0,1.06,.7),3.25,.19,'wood',64,scalez=.47)
   self.cyl((0,1.17,.7),3.2,.07,'gold',64,scalez=.47)
   self.cyl((0,1.22,.7),3.08,.035,'felt',64,scalez=.46)
   for x in [-1.2,1.2]:
    self.box((x,1.247,.65),(1.7,.008,.025),'gold');self.box((x,1.247,1.65),(1.7,.008,.025),'gold')
    for xx in [x-.85,x+.85]:self.box((xx,1.247,1.15),(.025,.008,1),'gold')
   self.box((0,1.25,.75),(.025,.008,1.8),'gold')
   for x in [-1.6,1.6]:
    self.box((x,1.26,.75),(.22,.01,.34),'paper',-12);self.box((x+.27,1.27,.75),(.22,.01,.34),'paper',10)
   for x in [-2.2,-1.1,0,1.1,2.2]:self.chair(x,2.65,0)
   for x in [-4.7,-3.6]:self.slot(x,3.7)
   for z in [-3.5,-2.4]:self.chair(4.65,z,-90)
   self.cyl((3.9,.65,-3),.48,1.3,'wood',24);self.cyl((3.9,1.33,-3),.58,.065,'gold',32)
 def save(self,name):
  with (OUT/(name+'.obj')).open('w') as f:
   f.write('# Original procedural environment. Coordinates converted for Unity OBJ importer.\nmtllib Environment.mtl\n');offset=1
   combined=[]
   for mat in COLORS:
    chunks=[(v,fs) for v,fs,m in self.parts if m==mat]
    if not chunks:continue
    vv=[];ff=[];off=0
    for v,fs in chunks: vv.extend(v);ff.extend(fs+off);off+=len(v)
    combined.append((np.array(vv),np.array(ff),mat))
   for k,(v,faces,mat) in enumerate(combined):
    f.write(f'o {mat}_{k}\nusemtl {mat}\n')
    for x,y,z in v:f.write(f'v {-x:.5f} {y:.5f} {z:.5f}\n')
    for a,b,c in faces:f.write(f'f {a+offset} {c+offset} {b+offset}\n')
    offset+=len(v)
  return sum(len(f) for v,f,m in self.parts)
 def render(self,name,eye=(0,1.8,-4.6),target=(0,1.9,1.5)):
  W,H=1200,760;eye=np.array(eye,float);fw=np.array(target,float)-eye;fw/=np.linalg.norm(fw);rt=np.cross([0,1,0],fw);rt/=np.linalg.norm(rt);up=np.cross(fw,rt)
  arr=np.full((H,W,3),[13,19,22],dtype=np.uint8);depth=np.full((H,W),np.inf);light=np.array([-.3,.85,-.35]);light/=np.linalg.norm(light);focal=780
  for verts,faces,mat in self.parts:
   rel=verts-eye;cam=np.stack([rel@rt,rel@up,rel@fw],axis=1)
   for tri in faces:
    c=cam[tri]
    if np.any(c[:,2]<.08):continue
    uv=np.stack([W/2+c[:,0]/c[:,2]*focal,H/2-c[:,1]/c[:,2]*focal],axis=1)
    lo=np.maximum(np.floor(uv.min(axis=0)).astype(int),0);hi=np.minimum(np.ceil(uv.max(axis=0)).astype(int),[W-1,H-1])
    if np.any(hi<lo):continue
    a,b,d=uv;den=(b[1]-d[1])*(a[0]-d[0])+(d[0]-b[0])*(a[1]-d[1])
    if abs(den)<1e-8:continue
    yy,xx=np.mgrid[lo[1]:hi[1]+1,lo[0]:hi[0]+1];w0=((b[1]-d[1])*(xx-d[0])+(d[0]-b[0])*(yy-d[1]))/den;w1=((d[1]-a[1])*(xx-d[0])+(a[0]-d[0])*(yy-d[1]))/den;w2=1-w0-w1
    z=1/(w0/c[0,2]+w1/c[1,2]+w2/c[2,2]);db=depth[lo[1]:hi[1]+1,lo[0]:hi[0]+1];mask=(w0>=0)&(w1>=0)&(w2>=0)&(z<db)
    v=verts[tri];n=np.cross(v[1]-v[0],v[2]-v[0]);n/=max(np.linalg.norm(n),1e-6);shade=.64+.36*abs(n@light)
    color=np.array(COLORS[mat])*255*shade
    if mat in ['light','screen']:color=np.array(COLORS[mat])*255
    arr[lo[1]:hi[1]+1,lo[0]:hi[0]+1][mask]=color.clip(0,255).astype(np.uint8);db[mask]=z[mask]
  im=Image.fromarray(arr);draw=ImageDraw.Draw(im);draw.rectangle((0,H-51,W,H),fill=(13,19,22));draw.text((25,H-35),name+' | Vista de los modelos generados; no es una captura de Unity.',fill=(235,215,170));im.save(str(ROOT.parents[1]/'Vistas_modelos'/(name+'.png')))
with (OUT/'Environment.mtl').open('w') as f:
 for name,c in COLORS.items():f.write(f'newmtl {name}\nKd {c[0]} {c[1]} {c[2]}\n')
(OUT/'palette.json').write_text(json.dumps(COLORS))
(ROOT.parents[1]/'Vistas_modelos').mkdir(exist_ok=True)
for name,casino in [('CasaCambio',False),('CasinoHorizonte',True)]:
 m=Model();m.room(casino);print(name,'triangles',m.save(name));m.render(name)
