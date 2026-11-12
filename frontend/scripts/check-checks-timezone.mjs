import assert from 'node:assert/strict';
import { createServer } from 'vite';

const server = await createServer({ server: { middlewareMode: true }, appType: 'custom' });
try {
  const { formatCheckTime, normalizeCheckPoint, getMonitorChecks } = await server.ssrLoadModule('/src/api/client.js');

  assert.equal(formatCheckTime('2026-11-11T08:34:43.8759202Z', '11/11 08:34:43'), '11/11 15:34:43');
  assert.equal(formatCheckTime('2026-11-11T17:30:00Z', '11/11 17:30:00'), '12/11 00:30:00');
  assert.equal(formatCheckTime('2026-11-11T15:34:43+07:00', 'fallback'), '11/11 15:34:43');

  assert.equal(formatCheckTime(null, '09:00'), '09:00');
  assert.equal(formatCheckTime('', '09:00'), '09:00');
  assert.equal(formatCheckTime('not-a-date', '11/11 08:34:43'), '11/11 08:34:43');

  const normalized = normalizeCheckPoint({
    checkedAt: '2026-11-11T08:34:43.8759202Z',
    time: '11/11 08:34:43',
    responseTimeMs: 178,
    statusCode: 200,
  });
  assert.equal(normalized.time, '11/11 15:34:43');
  assert.equal(normalized.responseTimeMs, 178);
  assert.equal(normalized.statusCode, 200);

  const mockPoints = await getMonitorChecks('mock-1');
  assert.ok(mockPoints.length > 0);
  assert.equal(mockPoints[0].time, '09:00');

  console.log('Checks timezone: UTC -> Asia/Ho_Chi_Minh, midnight rollover, missing/invalid fallback, and mock mode passed.');
} finally {
  await server.close();
}
