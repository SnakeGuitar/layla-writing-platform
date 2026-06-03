/**
 * Load test — SignalR real-time connections (ManuscriptHub)
 *
 * Protocol flow per VU:
 *   1. HTTP login → get JWT
 *   2. POST /hubs/manuscript/negotiate?negotiateVersion=1 → get connectionToken
 *   3. WebSocket connect ws://host/hubs/manuscript?id=<connectionToken>
 *   4. Send JSON handshake: {"protocol":"json","version":1}\x1e
 *   5. Receive empty handshake response: {}\x1e
 *   6. Invoke JoinChapterGroup with a projectId and chapterId
 *   7. Send 3 simulated text-changed events (350 ms apart, matching debounce)
 *   8. Close connection
 *
 * Run:
 *   k6 run tests/load/signalr.js
 *   k6 run -e BASE_URL=http://host:5000 -e WS_URL=ws://host:5000 tests/load/signalr.js
 *
 * Note: Set WS_URL separately because k6 ws.connect() requires the ws:// scheme.
 */

import http from "k6/http";
import ws from "k6/ws";
import { check, sleep } from "k6";
import { Counter, Rate, Trend } from "k6/metrics";

const BASE_URL = __ENV.BASE_URL || "http://localhost:5000";
const WS_URL   = __ENV.WS_URL   || "ws://localhost:5000";

export const options = {
  stages: [
    { duration: "30s", target: 10 },
    { duration: "1m",  target: 25 },
    { duration: "30s", target: 0  },
  ],
  thresholds: {
    http_req_duration:          ["p(95)<600"],
    signalr_connect_duration:   ["p(95)<1500"],
    signalr_handshake_errors:   ["count<5"],
    signalr_session_errors:     ["rate<0.05"],
  },
};

const connectDuration   = new Trend("signalr_connect_duration", true);
const handshakeErrors   = new Counter("signalr_handshake_errors");
const sessionErrors     = new Rate("signalr_session_errors");

// SignalR record separator — terminates every message in the JSON protocol.
const RS = "\x1e";

const EMAIL    = __ENV.SEED_EMAIL || "signalr-seed@layla-test.io";
const PASSWORD = __ENV.SEED_PASS  || "LoadTest1!";

export function setup() {
  const jsonHeaders = { "Content-Type": "application/json" };

  http.post(`${BASE_URL}/api/tokens/register`,
    JSON.stringify({ email: EMAIL, password: PASSWORD, displayName: "SignalR Seed" }),
    { headers: jsonHeaders });

  const loginRes = http.post(`${BASE_URL}/api/tokens/login`,
    JSON.stringify({ email: EMAIL, password: PASSWORD }),
    { headers: jsonHeaders });

  const token = String(loginRes.json("token") || "");
  if (!token) throw new Error(`Login failed: ${loginRes.body}`);

  const authHeaders = { "Content-Type": "application/json", Authorization: `Bearer ${token}` };

  const projRes = http.post(`${BASE_URL}/api/projects`,
    JSON.stringify({ title: "SignalR Load Novel" }),
    { headers: authHeaders });
  const projectId = String(projRes.json("id") || "");

  return { token, projectId, chapterId: "bench-chapter-01" };
}

export default function ({ token, projectId, chapterId }) {
  if (!token || !projectId) {
    console.warn("Setup data missing, skipping VU iteration.");
    sleep(1);
    return;
  }

  // ── Step 1: Negotiate ─────────────────────────────────────────────────────

  const negotiateRes = http.post(
    `${BASE_URL}/hubs/manuscript/negotiate?negotiateVersion=1`,
    null,
    { headers: { Authorization: `Bearer ${token}` } },
  );

  const negotiateOk = check(negotiateRes, {
    "negotiate: status 200": (r) => r.status === 200,
    "negotiate: has connectionToken": (r) => {
      const body = r.json();
      return !!(body && (body.connectionToken || body.connectionId));
    },
  });

  if (!negotiateOk) {
    sessionErrors.add(true);
    return;
  }

  const negotiateBody = negotiateRes.json();
  const connectionToken = String(negotiateBody.connectionToken || negotiateBody.connectionId || "");

  // ── Step 2: WebSocket session ─────────────────────────────────────────────

  const wsUrl = `${WS_URL}/hubs/manuscript?id=${encodeURIComponent(connectionToken)}`;
  const startTime = Date.now();

  const res = ws.connect(wsUrl, { headers: { Authorization: `Bearer ${token}` } }, (socket) => {
    let handshook = false;
    let invocationId = 1;

    socket.on("open", () => {
      connectDuration.add(Date.now() - startTime);

      // Step 3: Send handshake
      socket.send(JSON.stringify({ protocol: "json", version: 1 }) + RS);
    });

    socket.on("message", (data) => {
      const messages = String(data).split(RS).filter(Boolean);

      for (const msg of messages) {
        let parsed;
        try {
          parsed = JSON.parse(msg);
        } catch {
          continue;
        }

        // Handshake ACK — empty object or type-less message
        if (!handshook && (Object.keys(parsed).length === 0 || parsed.type === undefined)) {
          handshook = true;

          // Step 4: Join chapter group
          socket.send(
            JSON.stringify({
              type: 1,
              invocationId: String(invocationId++),
              target: "JoinChapterGroupAsync",
              arguments: [projectId, chapterId],
            }) + RS,
          );

          // Step 5: Simulate 3 text-changed broadcasts
          for (let i = 0; i < 3; i++) {
            socket.setTimeout(() => {
              socket.send(
                JSON.stringify({
                  type: 1,
                  invocationId: String(invocationId++),
                  target: "SendTextChangedAsync",
                  arguments: [projectId, chapterId, `{\\rtf1 VU ${__VU} edit ${i}}`],
                }) + RS,
              );
            }, (i + 1) * 350);
          }

          // Close after all messages sent
          socket.setTimeout(() => socket.close(), 1600);
        }

        // Hub invocation result (type: 3) — optionally check for errors
        if (parsed.type === 3 && parsed.error) {
          console.warn(`Hub error on invocation ${parsed.invocationId}: ${parsed.error}`);
        }
      }
    });

    socket.on("error", (e) => {
      handshakeErrors.add(1);
      console.warn(`WebSocket error for VU ${__VU}: ${e}`);
    });

    socket.on("close", () => {
      if (!handshook) handshakeErrors.add(1);
    });

    // Fallback timeout: close after 5 s regardless
    socket.setTimeout(() => socket.close(), 5000);
  });

  const connected = check(res, {
    "signalr: ws session completed without error": (r) => r && r.status === 101,
  });

  sessionErrors.add(!connected);
  sleep(0.5);
}
