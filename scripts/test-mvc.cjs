const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');
(async () => {
 const browser = await chromium.launch({channel:'msedge', headless:true});
 const context = await browser.newContext({viewport:{width:1440,height:960}});
 const page = await context.newPage();
 const results=[];
 page.on('pageerror', e => {throw e;});
 async function login(name){
   await page.goto('http://localhost:5014/Cuenta/Login');
   await page.getByLabel('Usuario',{exact:true}).fill(name);
   await page.getByLabel('Contraseña',{exact:true}).fill('Demo123!');
   await Promise.all([page.waitForURL(url => url.pathname === '/' || url.pathname === '/Incidentes'),page.getByRole('button',{name:'Entrar al panel'}).click()]);
   results.push('Login MVC: '+name);
 }
 async function logout(){await Promise.all([page.waitForURL('**/Cuenta/Login'),page.getByRole('button',{name:'Salir',exact:true}).click()]);}
 async function shot(name){await page.screenshot({path:path.join('docs/evidencias',name+'.png'),fullPage:true});}
 await page.goto('http://localhost:5014');
 await page.waitForURL('**/Cuenta/Login**');
 await shot('mvc-01-login');
 await login('usuario');
 await page.getByRole('link',{name:'+ Nuevo incidente'}).click();
 await page.getByLabel('Título',{exact:true}).fill('Prueba MVC: correo no disponible');
 await page.getByLabel('Descripción',{exact:true}).fill('El correo muestra un error al iniciar sesión. Reporte de verificación del frontend.');
 await page.getByLabel('Prioridad',{exact:true}).selectOption({label:'Alta'});
 await Promise.all([page.waitForURL('**/Incidentes/Detalle/*'),page.getByRole('button',{name:'Guardar incidente'}).click()]);
 const detailUrl=page.url(); const id=Number(detailUrl.split('/').pop());
 await shot('mvc-02-creado');
 results.push('Crear desde formulario MVC: incidente #'+id);
 await logout();await login('admin');
 await page.goto(detailUrl);
 await page.getByLabel('Asignar técnico',{exact:true}).selectOption({label:'tecnico'});
 await Promise.all([page.waitForResponse(r=>r.url().includes('/Incidentes/Asignar/') && r.request().method()==='POST'), page.getByRole('button',{name:'Asignar incidente',exact:true}).click()]);
 await page.waitForLoadState('networkidle');
 await shot('mvc-03-asignado');
 results.push('Admin asigna técnico desde MVC');
 await logout();await login('tecnico');
 await shot('mvc-04-listado-tecnico');
 await page.goto(detailUrl);
 for(const estado of ['EnProceso','Resuelto','Cerrado']){
   await page.getByRole('button',{name:'Avanzar a '+estado,exact:true}).click();
   await page.waitForLoadState('networkidle');
   results.push('Técnico avanza a '+estado);
 }
 await page.getByText('Este incidente está cerrado y no puede reabrirse.',{exact:false}).waitFor();
 await shot('mvc-05-cerrado');
 await page.setViewportSize({width:390,height:844});
 await shot('mvc-06-movil');
 const overflow=await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth);
 if(overflow)throw Error('Desbordamiento horizontal en móvil');
 await page.setViewportSize({width:1440,height:960});
 await logout();await login('tecnico2');
 const forbidden=await page.goto(detailUrl);
 if(forbidden.status()!==404)throw Error('Técnico ajeno debe recibir 404');
 await page.getByText('Incidente no encontrado.',{exact:false}).waitFor();
 await shot('mvc-07-acceso-ajeno');
 results.push('Otro técnico recibe 404 desde MVC');
 await logout();await login('admin');await page.goto(detailUrl);
 await page.getByText('Eliminar incidente',{exact:true}).click();
 page.once('dialog',dialog=>dialog.accept());
 await page.getByRole('button',{name:'Confirmar eliminación',exact:true}).click();
 await page.waitForURL(url => url.pathname === '/' || url.pathname === '/Incidentes');
 results.push('Administrador elimina incidente cerrado desde MVC');
 const api=await context.request.get('http://localhost:5080/_test/throw');
 if(api.status()!==404)throw Error('Ruta de prueba filtrada a API normal');
 results.push('Ruta temporal de excepción ausente en API normal: 404');
 const csrf=await context.request.post('http://localhost:5014/Incidentes/Crear',{form:{Titulo:'Sin token CSRF',Descripcion:'No debe crear',Prioridad:'Alta'}});
 if(csrf.status()!==400)throw Error('Formulario sin antiforgery debe devolver 400');
 results.push('POST MVC sin antiforgery rechazado: 400');
 fs.writeFileSync('docs/evidencias/mvc-resultados.json',JSON.stringify({passed:true,results},null,2));
 console.log(JSON.stringify({passed:true,results},null,2));
 await browser.close();
})().catch(e=>{console.error(e);process.exit(1);});
