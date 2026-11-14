import assert from "node:assert/strict";

const vietnamClockFormat = new Intl.DateTimeFormat("en-GB", {
  hour: "2-digit",
  minute: "2-digit",
  second: "2-digit",
  hourCycle: "h23",
  timeZone: "Asia/Ho_Chi_Minh",
});

function formatNavbarTime(date) {
  return `${vietnamClockFormat.format(date)} (UTC+7)`;
}

// Test 1: Given UTC 06:36:46, must format to 13:36:46 (UTC+7)
const utcDate = new Date("2026-11-14T06:36:46Z");
assert.equal(formatNavbarTime(utcDate), "13:36:46 (UTC+7)");

// Test 2: Midnight rollover: UTC 17:05:01 -> Next day 00:05:01 (UTC+7)
const midnightDate = new Date("2026-11-14T17:05:01Z");
assert.equal(formatNavbarTime(midnightDate), "00:05:01 (UTC+7)");

// Test 3: Standard single digit hour/minute/second padding
const paddedDate = new Date("2026-11-14T01:02:03Z");
assert.equal(formatNavbarTime(paddedDate), "08:02:03 (UTC+7)");

console.log("Navbar timezone tests passed: UTC -> Asia/Ho_Chi_Minh (UTC+7), midnight rollover, and padding verified.");
