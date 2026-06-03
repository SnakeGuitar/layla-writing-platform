/**
 * Combined load scenario — simulates a realistic mix of concurrent users:
 *   40% — browsing the public catalog (no auth)
 *   30% — logging in and working with their projects
 *   20% — reading/writing manuscript chapters
 *   10% — connected via SignalR (collaborative editing)
 *
 * Run:
 *   k6 run tests/load/scenarios.js
 *   k6 run -e BASE_URL=http://host:5000 -e TARGET_VUS=100 tests/load/scenarios.js
 *
 * Adjust TARGET_VUS to scale the test (default 50).
 */

import http from "k6/http";
import ws from "k6/ws";
import { check, sleep, group } from "k6";
import { Rate, Trend, Counter } from "k6/metrics";

const BASE_URL  = __ENV.BASE_URL   || "http://localhost:5000";
const WS_URL    = __ENV.WS_URL     || "ws://localhost:5000";
const TARGET    = parseInt(__ENV.TARGET_VUS || "50", 10);

export const options = {
  scenarios: {
    public_browsing: {
      executor:        "ramping-vus",
      startVUs:        0,
      stages:          [
        { duration: "30s", target: Math.round(TARGET * 0.4) },
        { duration: "2m",  target: Math.round(TARGET * 0.4) },
        { duration: "30s", target: 0 },
      ],
      exec:            "publicBrowsing",
      gracefulRampDown: "10s",
    },
    authenticated_work: {
      executor:        "ramping-vus",
      startVUs:        0,
      stages:          [
        { duration: "40s", target: Math.round(TARGET * 0.3) },
        { duration: "2m",  target: Math.round(TARGET * 0.3) },
        { duration: "30s", target: 0 },
      ],
      exec:            "authenticatedWork",
      gracefulRampDown: "10s",
    },
    manuscript_editing: {
      executor:        "ramping-vus",
      startVUs:        0,
      stages:          [
        { duration: "40s", target: Math.round(TARGET * 0.2) },
        { duration: "2m",  target: Math.round(TARGET * 0.2) },
        { duration: "30s", target: 0 },
      ],
      exec:            "manuscriptEditing",
      gracefulRampDown: "10s",
    },
    realtime_collab: {
      executor:        "ramping-vus",
      startVUs:        0,
      stages:          [
        { duration: "50s", target: Math.round(TARGET * 0.1) },
        { duration: "2m",  target: Math.round(TARGET * 0.1) },
        { duration: "30s", target: 0 },
      ],
      exec:            "realtimeCollab",
      gracefulRampDown: "15s",
    },
  },
  thresholds: {
    http_req_duration:       ["p(95)<600", "p(99)<1200"],
    http_req_failed:         ["rate<0.02"],
    error_rate:              ["rate<0.03"],
  },
};

const errorRate = new Rate("error_rate");
const RS        = "\x1e";

const EMAIL    = __ENV.SEED_EMAIL || "scenarios-seed@layla-test.io";
const PASSWORD = __ENV.SEED_PASS  || "LoadTest1!";

// ── setup ─────────────────────────────────────────────────────────────────────

export function setup() {
  const jsonHeaders = { "Content-Type": "application/json" };

  http.post(`${BASE_URL}/api/tokens/register`,
    JSON.stringify({ email: EMAIL, password: PASSWORD, displayName: "Scenarios Seed" }),
    { headers: jsonHeaders });

  const loginRes = http.post(`${BASE_URL}/api/tokens/login`,
    JSON.stringify({ email: EMAIL, password: PASSWORD }),
    { headers: jsonHeaders });

  const token = String(loginRes.json("token") || "");
  if (!token) throw new Error(`Seed login failed: ${loginRes.body}`);

  const authHeaders = { "Content-Type": "application/json", Authorization: `Bearer ${token}` };

  const projRes = http.post(`${BASE_URL}/api/projects`,
    JSON.stringify({ title: "Scenarios Load Novel", isPublic: true }),
    { headers: authHeaders });
  const projectId = String(projRes.json("id") || "");

  return { token, projectId, chapterId: "scenario-chapter-01" };
}

// ── scenario functions ────────────────────────────────────────────────────────

export function publicBrowsing() {
  group("public catalog", () => {
    const res = http.get(`${BASE_URL}/api/projects/public`);
    const ok = check(res, {
      "catalog: 200":        (r) => r.status === 200,
      "catalog: <400ms":     (r) => r.timings.duration < 400,
    });
    errorRate.add(!ok);
  });
  sleep(1 + Math.random());
}

export function authenticatedWork({ token, projectId }) {
  if (!token) { sleep(1); return; }

  const authHeaders = { "Content-Type": "application/json", Authorization: `Bearer ${token}` };

  group("project work", () => {
    // List projects
    const listRes = http.get(`${BASE_URL}/api/projects`, { headers: authHeaders });
    const ok = check(listRes, { "projects: 200": (r) => r.status === 200 });
    errorRate.add(!ok);

    if (projectId && Math.random() < 0.5) {
      // Get specific project
      const getRes = http.get(`${BASE_URL}/api/projects/${projectId}`, { headers: authHeaders });
      check(getRes, { "get project: 200": (r) => r.status === 200 });
    }
  });
  sleep(0.5 + Math.random() * 0.5);
}

export function manuscriptEditing({ token, projectId }) {
  if (!token || !projectId) { sleep(1); return; }

  const authHeaders = { "Content-Type": "application/json", Authorization: `Bearer ${token}` };

  group("manuscript read", () => {
    const res = http.get(`${BASE_URL}/api/manuscripts/${projectId}`, { headers: authHeaders });
    const ok = check(res, { "manuscript list: 200": (r) => r.status === 200 });
    errorRate.add(!ok);
  });
  sleep(0.5);
}

export function realtimeCollab({ token, projectId, chapterId }) {
  if (!token || !projectId) { sleep(2); return; }

  const negotiateRes = http.post(
    `${BASE_URL}/hubs/manuscript/negotiate?negotiateVersion=1`,
    null,
    { headers: { Authorization: `Bearer ${token}` } },
  );

  if (negotiateRes.status !== 200) {
    errorRate.add(true);
    return;
  }

  const body = negotiateRes.json();
  const connToken = String((body && (body.connectionToken || body.connectionId)) || "");
  if (!connToken) { errorRate.add(true); return; }

  const wsUrl = `${WS_URL}/hubs/manuscript?id=${encodeURIComponent(connToken)}`;

  ws.connect(wsUrl, { headers: { Authorization: `Bearer ${token}` } }, (socket) => {
    let handshook = false;

    socket.on("open", () => {
      socket.send(JSON.stringify({ protocol: "json", version: 1 }) + RS);
    });

    socket.on("message", (data) => {
      if (handshook) return;
      const messages = String(data).split(RS).filter(Boolean);
      for (const msg of messages) {
        try {
          const parsed = JSON.parse(msg);
          if (Object.keys(parsed).length === 0 || parsed.type === undefined) {
            handshook = true;
            socket.send(
              JSON.stringify({
                type: 1, invocationId: "1",
                target: "JoinChapterGroupAsync",
                arguments: [projectId, chapterId],
              }) + RS,
            );
            socket.setTimeout(() => socket.close(), 2000);
          }
        } catch {}
      }
    });

    socket.on("error", () => errorRate.add(true));
    socket.setTimeout(() => socket.close(), 4000);
  });

  sleep(1);
}
