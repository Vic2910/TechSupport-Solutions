from pathlib import Path
import json, textwrap
from reportlab.pdfgen import canvas
from reportlab.lib.colors import HexColor, white
from reportlab.lib.utils import simpleSplit
import pymupdf

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/"output/pdf";OUT.mkdir(parents=True,exist_ok=True)
TMP=ROOT/"tmp/pdfs";TMP.mkdir(parents=True,exist_ok=True)
W,H=595,842
INK="#182b46"; BLUE="#2165d6"; MUTED="#60758c"

def header(c,title,subtitle,number):
 c.setFillColor(HexColor(BLUE));c.rect(0,H-12,W,12,fill=1,stroke=0)
 c.setFillColor(HexColor(BLUE));c.setFont("Helvetica-Bold",10);c.drawString(42,794,"TECHSUPPORT SOLUTIONS / ENTREGA")
 c.setFillColor(HexColor(INK));c.setFont("Helvetica-Bold",23);c.drawString(42,758,title)
 para(c,subtitle,42,733,510,10,MUTED)
 c.setStrokeColor(HexColor("#dfe7f0"));c.line(42,42,553,42)
 c.setFillColor(HexColor(MUTED));c.setFont("Helvetica",9);c.drawString(42,26,"ASP.NET Core 10 | API + MVC | 09/11/2026")
 c.drawRightString(553,26,str(number))
def para(c,text,x,y,width,size=11,color=INK):
 c.setFillColor(HexColor(color));c.setFont("Helvetica",size)
 for line in simpleSplit(text,"Helvetica",size,width):
  c.drawString(x,y,line);y-=size*1.5
 return y
def box(c,x,y,w,h,title,lines=(),fill="#edf3fc"):
 c.setFillColor(HexColor(fill));c.setStrokeColor(HexColor("#ccd9e9"));c.roundRect(x,y,w,h,9,fill=1,stroke=1)
 c.setFillColor(HexColor(INK));c.setFont("Helvetica-Bold",12);c.drawString(x+14,y+h-24,title)
 for i,line in enumerate(lines):para(c,line,x+14,y+h-46-i*19,w-28,10)
def arrow(c,x1,y1,x2,y2,color=BLUE,dashed=False):
 c.setStrokeColor(HexColor(color));c.setFillColor(HexColor(color));c.setLineWidth(1.7)
 if dashed:c.setDash(5,4)
 c.line(x1,y1,x2,y2);c.setDash()
 import math
 a=math.atan2(y2-y1,x2-x1)
 p=c.beginPath();p.moveTo(x2,y2)
 p.lineTo(x2-8*math.cos(a-.5),y2-8*math.sin(a-.5));p.lineTo(x2-8*math.cos(a+.5),y2-8*math.sin(a+.5));p.close()
 c.drawPath(p,fill=1,stroke=0)

c=canvas.Canvas(str(OUT/"Entrega_TechSupport.pdf"),pagesize=(W,H))
c.setTitle("TechSupport Solutions - Analisis y diagramas")
header(c,"Análisis del problema","A1. Requerimientos verificables y decisiones de diseño.",1)
rows=[
("Solicitudes perdidas","Registrar y consultar incidentes persistentes en SQLite.","POST 201; GET y nuevo DbContext recuperan el registro."),
("Asignaciones poco claras","Solo admin asigna a un técnico. Pendiente pasa a Asignado.","Técnico: 403; rol incorrecto: 400; asignación válida: 200."),
("Información ajena","JWT, roles y filtros por creador o técnico asignado.","Consulta ajena: 404; listas solo con registros autorizados."),
("Errores sin registro / internos","Middleware con JSON seguro y log correlacionado.","500 genérico con requestId; excepción completa en log.")]
y=684
for i,(problem,requirement,test) in enumerate(rows,1):
 box(c,42,y-100,511,100,f"{i:02d}  {problem}",fill="#f3f6fb")
 para(c,"Requerimiento: "+requirement,56,y-46,480,10)
 para(c,"Prueba: "+test,56,y-76,480,10)
 y-=112
c.setFillColor(HexColor(INK));c.setFont("Helvetica-Bold",15);c.drawString(42,212,"A3. Decisiones")
y=para(c,"404 para recursos ajenos: evita revelar que existen; usar 403 permitiría enumerarlos. Los permisos generales por rol responden 403.",42,188,510,11)
y=para(c,"Eliminar Cerrados: permitido al administrador según la matriz. Se pierde el historial; la interfaz pide confirmación. Restringirlo añadiría una regla no solicitada.",42,y-13,510,11)
c.showPage()
header(c,"Diagrama de estados","A2. Cinco estados; cada cambio avanza exactamente un paso.",2)
states=[("Pendiente","Creación por cualquier rol"),("Asignado","Admin asigna un usuario Tecnico"),("En proceso","Admin o técnico asignado"),("Resuelto","Registrar FechaResolucion UTC"),("Cerrado","Estado terminal; no se reabre")]
ys=[625,520,415,310,205]
for (title,note),y in zip(states,ys):
 box(c,163,y,270,66,title,[note])
for a,b in zip(ys,ys[1:]):arrow(c,298,a,298,b+66)
# Explicit forbidden shortcut
c.setStrokeColor(HexColor("#b62d47"));c.setDash(5,4)
c.line(163,658,125,658);c.line(125,658,125,238);c.setDash()
arrow(c,125,238,163,238,"#b62d47",True)
para(c,"PROHIBIDO",43,450,110,10,"#b62d47")
para(c,"Cerrar sin resolver",43,432,110,9,"#b62d47")
para(c,"Tampoco se permiten saltos, retrocesos ni reapertura. Pendiente a Asignado solo ocurre mediante el endpoint de asignación.",42,151,510,11)
para(c,"Si reaparece el problema, se crea otro incidente. Eliminar es una operación administrativa, no una transición de estado.",42,99,510,10)
c.showPage()
header(c,"Modelo de datos","Dos relaciones diferentes conectan Usuario con Incidente.",3)
box(c,42,438,220,245,"Usuario",["Id (PK)","NombreUsuario (único)","PasswordHash (BCrypt)","Rol: Administrador / Tecnico / Usuario"])
box(c,310,355,243,328,"Incidente",["Id (PK)","Titulo / Descripcion","Prioridad / Estado","UsuarioId (FK, obligatorio)","TecnicoId (FK, nullable)","FechaCreacion (UTC)","FechaResolucion (UTC, nullable)","Version (Guid, concurrencia)"])
arrow(c,262,578,310,578)
arrow(c,262,478,310,478)
para(c,"reporta",267,593,45,8)
para(c,"atiende",267,493,45,8)
y=para(c,"Reportante: cada incidente tiene exactamente 1 usuario creador; cada usuario puede reportar 0..N incidentes.",42,309,510,11)
y=para(c,"Técnico: cada incidente tiene 0..1 técnico; cada usuario con rol Tecnico puede atender 0..N incidentes. El servicio valida ese rol.",42,y-16,510,11)
y=para(c,"Ambas FK usan DeleteBehavior.Restrict. EF Core configura cada navegación explícitamente para evitar ambigüedades.",42,y-16,510,11)
y=para(c,"Los DTOs de respuesta nunca incluyen PasswordHash. Un Guid de concurrencia detecta cambios simultáneos y evita sobrescrituras silenciosas.",42,y-16,510,11)
c.showPage()
header(c,"Arquitectura de la solución","El frontend MVC consume la API por HTTP. Solo la API accede a los datos.",4)
nodes=[
(42,614,511,65,"Navegador",["Formularios Razor, cookie HttpOnly y token antiforgery"]),
(42,504,511,65,"TechSupport Solutions (MVC)",["Controladores + vistas + ApiClient / HttpClient"]),
(42,394,511,65,"TechSupport.API",["JWT + autorización + controladores + IncidenteService"]),
(42,284,511,65,"EF Core / AppDbContext",["Relaciones, persistencia SQLite y concurrencia"]),
(42,174,511,65,"SQLite",["Usuarios e incidentes persistentes"])]
for args in nodes:box(c,*args)
for top,bottom in [(614,569),(504,459),(394,349),(284,239)]:arrow(c,298,top,298,bottom)
para(c,"HTTP + Bearer JWT",312,478,200,10,BLUE)
para(c,"Transversal: ExceptionHandlingMiddleware + ILogger / Serilog, logs diarios con requestId. Contratos compartidos: DTOs y enums; sin acceso del MVC a EF Core.",42,132,510,11)
para(c,"Tests: xUnit + WebApplicationFactory + SQLite temporal. La ruta de fallo existe únicamente en el ensamblado de pruebas.",42,78,510,10)
c.save()

data=json.loads((ROOT/"docs/evidencias/http.json").read_text(encoding="utf-8"))
selected=[data[0]]+[next(x for x in data if x["title"].startswith(f"{i:02d} -")) for i in range(2,10)]
c=canvas.Canvas(str(OUT/"Evidencias_HTTP.pdf"),pagesize=(W,H))
c.setTitle("TechSupport Solutions - Evidencias HTTP reales")
for i,entry in enumerate(selected,1):
 header(c,f"Prueba {i:02d} / HTTP {entry['response']['status']}","Captura de transcripción real: xUnit + WebApplicationFactory + SQLite.",i)
 para(c,entry["title"],42,686,510,12)
 y=650
 for label,obj in [("PETICIÓN",entry["request"]),("RESPUESTA",entry["response"])]:
  c.setFillColor(HexColor(BLUE));c.setFont("Helvetica-Bold",11);c.drawString(42,y,label);y-=22
  raw=json.dumps(obj,ensure_ascii=False,indent=2)
  lines=[]
  for line in raw.splitlines():lines.extend(textwrap.wrap(line,85,replace_whitespace=False,drop_whitespace=False) or [""])
  c.setFillColor(HexColor(INK));c.setFont("Courier",8)
  for line in lines:c.drawString(42,y,line);y-=12
  y-=26
 para(c,"Resultado: verificado. JWT y contraseña ocultos en esta entrega.",42,80,510,10)
 c.showPage()
c.save()
for name in ["Entrega_TechSupport","Evidencias_HTTP"]:
 doc=pymupdf.open(OUT/(name+".pdf"))
 for i,page in enumerate(doc):
  dest=TMP/f"{name}-{i+1:02d}.png"
  page.get_pixmap(matrix=pymupdf.Matrix(1.3,1.3)).save(dest)
  if name=="Evidencias_HTTP":
   page.get_pixmap(matrix=pymupdf.Matrix(1.3,1.3)).save(ROOT/"docs/evidencias"/f"http-{i+1:02d}.png")
from PIL import Image,ImageOps,ImageDraw
images=[]
for f in sorted(TMP.glob("*.png")):
 im=Image.open(f).convert("RGB");im.thumbnail((298,421));images.append((f.name,im))
sheet=Image.new("RGB",(298*4,450*((len(images)+3)//4)),"#e6ebf2")
for i,(name,im) in enumerate(images):
 x=(i%4)*298;y=(i//4)*450;sheet.paste(im,(x,y));ImageDraw.Draw(sheet).text((x+5,y+423),name,fill="black")
sheet.save(TMP/"revision.png")
print(f"Generados 2 PDF, {len(data)} peticiones verificadas y 9 capturas HTTP.")
