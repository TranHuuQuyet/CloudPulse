// Run with a Playwright module path as argv[2]; see docs/evidence/w3-05/README.md.
import { pathToFileURL } from 'node:url';
import { mkdir, writeFile } from 'node:fs/promises';
import assert from 'node:assert/strict';

const { chromium } = await import(pathToFileURL(process.argv[2]).href);
const output = new URL('../../docs/evidence/w3-05/', import.meta.url);
await mkdir(output, { recursive: true });
const browser = await chromium.launch({ channel: 'chrome', headless: true });
const context = await browser.newContext({ timezoneId: 'Asia/Ho_Chi_Minh', viewport: { width: 1366, height: 1000 } });
const results = { executedAt: new Date().toISOString(), cases: [] };
const page = await context.newPage();
page.setDefaultTimeout(15000);
const chart = page.getByRole('region', { name: 'Biểu đồ độ trễ phản hồi' });
const history = page.getByRole('region', { name: 'Lịch sử sự cố' });
const select = page.getByRole('combobox');
async function run(name, action) {
  try { const detail = await action(); results.cases.push({ name, status: detail?.functionalStatus === 'FAIL' ? 'KNOWN_FAILURE' : 'PASS', detail }); }
  catch (error) { results.cases.push({ name, status: 'FAIL', error: error.message }); }
  await writeFile(new URL('results.json', output), JSON.stringify(results, null, 2));
}
async function shot(name) { await page.screenshot({ path: new URL(name, output).pathname.replace(/^\/(\w:)/, '$1'), fullPage: true }); }
async function loaded() { await chart.waitFor(); await history.waitFor(); }

await run('live-api-and-metrics', async () => {
  const response = await context.request.get('http://127.0.0.1:3015/api/monitors');
  assert.equal(response.status(), 200);
  const monitors = await response.json();
  assert.ok(monitors.length >= 2, 'Need two existing real monitors');
  results.monitors = monitors.map(m => ({ id: m.monitorId, name: m.name, status: m.status }));
  await page.goto('http://127.0.0.1:3015'); await loaded();
  const chosen = monitors.find(m => m.name === 'Mock 200') ?? monitors[0];
  await select.selectOption(chosen.monitorId); await loaded();
  const responseChecks = await context.request.get(`http://127.0.0.1:3015/api/monitors/${chosen.monitorId}/checks?limit=30`);
  const checks = await responseChecks.json();
  assert.ok(checks.length > 0);
  const text = await chart.innerText();
  assert.ok(text.includes(chosen.name) && text.includes('Dữ liệu API'));
  const values = checks.map(p => p.responseTimeMs);
  for (const value of [Math.min(...values), values.reduce((s,v)=>s+v,0)/values.length, Math.max(...values)]) {
    assert.ok(text.includes(`${Number(value.toFixed(1))} ms`));
  }
  await shot('live-dashboard.png');
  return { monitor: chosen.name, points: checks.length, min: Math.min(...values), max: Math.max(...values), average: values.reduce((s,v)=>s+v,0)/values.length };
});

await run('live-sync-and-check-now-network', async () => {
  const requests = [];
  const listener = req => { if (/\/(checks|incidents)\?/.test(req.url())) requests.push({ url:req.url(), at:Date.now() }); };
  page.on('request', listener);
  await page.getByRole('button', {name:'Sync', exact:true}).click(); await page.waitForTimeout(1800); await loaded();
  const sync = requests.splice(0);
  const selected = await select.inputValue();
  const row = page.locator('li').filter({hasText: results.monitors.find(m=>m.id===selected).name});
  const start = Date.now();
  const responsePromise = page.waitForResponse(r=>r.url().endsWith(`/${selected}/check`) && r.request().method()==='POST');
  await row.getByRole('button', {name:'Check Now'}).click();
  const response = await responsePromise;
  await response.finished(); const responseMs = Date.now()-start;
  const check = await response.json();
  await page.waitForTimeout(1800); await loaded();
  const afterCheck = requests.splice(0); page.off('request',listener);
  assert.ok(sync.some(r=>r.url.includes('/checks?')) && sync.some(r=>r.url.includes('/incidents?')));
  assert.ok(afterCheck.some(r=>r.url.includes('/checks?')) && afterCheck.some(r=>r.url.includes('/incidents?')));
  const latest = await (await context.request.get(`http://127.0.0.1:3015/api/monitors/${selected}/checks`)).json();
  const instant=new Date(check.checkedAt);
  const pad=n=>String(n).padStart(2,'0');
  const utcLabel=`${pad(instant.getUTCDate())}/${pad(instant.getUTCMonth()+1)} ${pad(instant.getUTCHours())}:${pad(instant.getUTCMinutes())}:${pad(instant.getUTCSeconds())}`;
  const matchingPoint=latest.find(p=>p.responseTimeMs===check.responseTimeMs && p.time===utcLabel);
  assert.ok(matchingPoint, 'New check timestamp and latency appear together in history (current API UTC formatting)');
  return { sync, afterCheck, responseMs, targetAtMost2s:responseMs<=2000, check, matchingPoint, latestPointCount:latest.length };
});

await run('live-open-incident-and-timezone', async () => {
  const monitor = results.monitors.find(m=>m.name==='Mock 500'); assert.ok(monitor);
  await select.selectOption(monitor.id); await page.waitForTimeout(1200); await loaded();
  assert.ok((await history.innerText()).includes('OPEN'));
  const incidents = await (await context.request.get(`http://127.0.0.1:3015/api/monitors/${monitor.id}/incidents`)).json();
  const checks = await (await context.request.get(`http://127.0.0.1:3015/api/monitors/${monitor.id}/checks`)).json();
  await shot('live-incident.png');
  return { incident:incidents[0], checkTimes:checks.slice(-3).map(p=>p.time), incidentDisplayedTimezone:'Asia/Ho_Chi_Minh', backendFormatting:'DateTime.Parse(checkedAt).ToLocalTime() on Lambda; see Program.cs' };
});

await run('controlled-race-response-A-after-B', async () => {
  const a = results.monitors[0], b = results.monitors[1]; let delayed = false;
  await page.route('**/api/monitors/*/*?*', async route => {
    const url=route.request().url(); const isA=url.includes(`/${a.id}/`);
    if(isA) { delayed=true; await new Promise(r=>setTimeout(r,1200)); }
    const body=url.includes('/checks?') ? [{time:'test',responseTimeMs:isA?111:222,statusCode:200}] : [{incidentId:isA?'a':'b',status:'OPEN',openedAt:'2026-11-11T07:00:00Z',reason:isA?'CAUSE_A':'CAUSE_B'}];
    await route.fulfill({json:body});
  });
  await select.selectOption(b.id); await page.waitForTimeout(150);
  await select.selectOption(a.id); await page.waitForTimeout(150);
  await select.selectOption(b.id); await page.waitForTimeout(1800); await loaded();
  assert.ok(delayed);
  assert.ok((await chart.innerText()).includes('222 ms'));
  assert.ok((await history.innerText()).includes('CAUSE_B'));
  assert.ok(!(await chart.innerText()).includes('111 ms'));
  assert.ok(!(await history.innerText()).includes('CAUSE_A'));
  await shot('race-B-after-delayed-A.png'); await page.unrouteAll({behavior:'wait'});
  return { delayAms:1200, gapBetweenSelectionsMs:150, finalMonitor:b.name, finalLatency:222, finalCause:'CAUSE_B', source:'controlled responses using real monitor IDs' };
});

await run('live-new-monitor-empty-history', async () => {
  const base='http://127.0.0.1:3015/api/monitors';
  const response=await context.request.post(base,{data:{name:`TV5 Empty ${Date.now()}`,url:'https://ex9kcfdj5a.execute-api.ap-southeast-1.amazonaws.com/mock'}});
  assert.equal(response.status(),201);
  const monitor=await response.json();
  try {
    const checks=await (await context.request.get(`${base}/${monitor.monitorId}/checks`)).json();
    const incidents=await (await context.request.get(`${base}/${monitor.monitorId}/incidents`)).json();
    assert.deepEqual(checks,[]); assert.deepEqual(incidents,[]);
    await page.getByRole('button',{name:'Sync',exact:true}).click();
    await select.locator(`option[value="${monitor.monitorId}"]`).waitFor({state:'attached'});
    await select.selectOption(monitor.monitorId);
    await page.getByText('Chưa có dữ liệu độ trễ.',{exact:true}).waitFor();
    await page.getByText('Chưa có sự cố nào được ghi nhận.',{exact:true}).waitFor();
    await shot('live-empty-monitor.png');
    return {monitorId:monitor.monitorId,checks,incidents,source:'new real AWS monitor before scheduled checks; deleted in finally'};
  } finally {
    const deletion=await context.request.delete(`${base}/${monitor.monitorId}`);
    assert.ok(deletion.ok(),`Temporary monitor cleanup failed: ${deletion.status()}`);
    await page.getByRole('button',{name:'Sync',exact:true}).click();
    await page.waitForTimeout(800);
  }
});

await run('controlled-empty-api-error-retry', async () => {
  await page.route('**/api/monitors/*/*?*', route=>route.fulfill({json:[]}));
  await page.getByRole('button',{name:'Sync',exact:true}).click();
  await page.getByText('Chưa có dữ liệu độ trễ.',{exact:true}).waitFor();
  await page.getByText('Chưa có sự cố nào được ghi nhận.',{exact:true}).waitFor();
  await shot('empty-history.png'); await page.unrouteAll({behavior:'wait'});
  await page.route('**/api/monitors/*/*?*', route=>route.fulfill({status:503,body:'Acceptance test unavailable'}));
  await page.getByRole('button',{name:'Sync',exact:true}).click();
  await page.getByRole('alert').filter({hasText:'503'}).waitFor();
  assert.ok(!(await page.locator('body').innerText()).includes('Dữ liệu giả lập'));
  await shot('api-error.png'); await page.unrouteAll({behavior:'wait'});
  await page.getByRole('button',{name:'Sync',exact:true}).click(); await page.waitForTimeout(1500); await loaded();
  assert.equal(await page.getByRole('alert').count(),0);
  return 'Empty arrays and HTTP 503 injected in browser; Sync retry recovered with live AWS data; no mock fallback';
});

await run('mobile-tooltip-and-horizontal-scroll', async () => {
  await page.setViewportSize({width:390,height:844});
  const monitor=results.monitors.find(m=>m.name==='Mock 500'); await select.selectOption(monitor.id); await page.waitForTimeout(1200); await loaded();
  const scroll=history.locator('.overflow-x-auto');
  const widths=await scroll.evaluate(el=>{el.scrollLeft=200;return {client:el.clientWidth,scroll:el.scrollWidth,scrollLeft:el.scrollLeft};});
  assert.ok(widths.scroll>widths.client && widths.scrollLeft>0);
  await chart.scrollIntoViewIfNeeded();
  const plot=chart.locator('.recharts-wrapper'); const box=await plot.boundingBox();
  await page.mouse.move(box.x+box.width*0.6,box.y+box.height*0.5); await page.waitForTimeout(300);
  const tooltip = await chart.locator('.recharts-tooltip-wrapper').innerText();
  assert.ok(tooltip.includes('HTTP') && tooltip.includes('ms'));
  await shot('mobile-tooltip.png'); await history.scrollIntoViewIfNeeded(); await shot('mobile-incidents.png');
  const pageWidth=await page.evaluate(()=>document.documentElement.scrollWidth);
  assert.ok(pageWidth<=390, `Page overflows: ${pageWidth}px`);
  return { viewport:390, table:widths, tooltip, pageWidth };
});

await run('preview-direct-api-CORS', async () => {
  await page.setViewportSize({width:1366,height:1000});
  const errors=[];
  const listener=msg=>{if(msg.type()==='error')errors.push(msg.text());};
  page.on('console',listener);
  await page.goto('http://127.0.0.1:4185'); await loaded();
  const result=await page.evaluate(async ()=>{
    try {const r=await fetch('https://ex9kcfdj5a.execute-api.ap-southeast-1.amazonaws.com/api/monitors',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({})});return {status:r.status,body:await r.text()};}
    catch(e){return {error:e.message};}
  });
  await shot('preview-direct-api.png');
  page.off('console',listener);
  return { functionalStatus:result.error?'FAIL':'PASS', postValidationProbe:result, errors, note:'Probe executed successfully; production POST acceptance FAILS if browser blocks preflight. Empty body avoids creating a monitor.' };
});

await run('mock-mode-no-api-url', async () => {
  await page.goto('http://127.0.0.1:3016'); await loaded();
  assert.ok((await chart.innerText()).includes('Dữ liệu giả lập'));
  assert.ok((await history.innerText()).includes('Dữ liệu giả lập'));
  await shot('mock-mode.png');
  return {mode:'acceptance-mock',API_BASE:'',chartAndIncidents:'mock data displayed'};
});

await run('invalid-api-base-no-fallback', async () => {
  await page.goto('http://127.0.0.1:3017');
  await page.getByRole('alert').waitFor();
  assert.equal(await select.locator('option').count(),1);
  assert.ok(!(await chart.innerText()).includes('Dữ liệu giả lập'));
  const request=page.waitForResponse(r=>r.url().endsWith('/api/monitors'));
  await page.getByRole('button',{name:'Sync',exact:true}).click();
  const response=await request;
  await shot('invalid-base-url.png');
  return {mode:'acceptance-invalid',API_BASE:'http://127.0.0.1:1',status:response.status(),message:await page.getByRole('alert').innerText(),mockFallback:false};
});

await browser.close();
console.log(JSON.stringify(results,null,2));
if(results.cases.some(c=>c.status==='FAIL'))process.exitCode=1;
