import assert from 'node:assert/strict';
import React from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { createServer } from 'vite';

const server = await createServer({ server: { middlewareMode: true }, appType: 'custom' });
try {
  const { default: IncidentHistory } = await server.ssrLoadModule('/src/components/IncidentHistory.jsx');
  const render = (incidents) => renderToStaticMarkup(React.createElement(IncidentHistory, { incidents }));
  const start = '2026-11-06T02:00:00Z';
  const resolved = { id: 'resolved', endpointName: 'Resolved endpoint', status: 'RESOLVED', startedAt: start, resolvedAt: '2026-11-06T02:02:04Z', cause: 'Request Timeout > 5000ms' };
  assert.match(render([]), /Chưa có sự cố/);
  assert.match(render([resolved]), /124s downtime/);
  assert.match(render([{ ...resolved, resolvedAt: 'bad-date' }]), /—/);
  assert.doesNotMatch(render([{ ...resolved, resolvedAt: '2026-11-06T01:00:00Z' }]), /-\d+s downtime/);
  const savedNow = Date.now;
  try {
    Date.now = () => Date.parse('2026-11-06T02:03:00Z');
    const open = { ...resolved, id: 'open', endpointName: 'Open endpoint', status: 'OPEN', startedAt: '2026-11-06T02:01:00Z', resolvedAt: null };
    const html = render([resolved, open]);
    assert.match(html, /120s downtime/);
    assert.match(html, /Đang diễn ra/);
    assert.match(html, /text-red-700/);
    assert.match(html, /text-emerald-700/);
    assert.ok(html.indexOf('Open endpoint') < html.indexOf('Resolved endpoint'));
    const { mockIncidents } = await server.ssrLoadModule('/src/data/mockData.js');
    assert.match(render(mockIncidents), /124s downtime/);
  } finally {
    Date.now = savedNow;
  }
  console.log('IncidentHistory: empty, resolved 124s, invalid/reversed dates, OPEN duration, sorting, status colors and mock data passed.');
} finally {
  await server.close();
}
