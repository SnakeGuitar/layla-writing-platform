/**
 * Load test — Authentication endpoints
 *
 * Tests:
 *   POST /api/tokens/login    (login with existing credentials)
 *   POST /api/tokens/register (register a new unique user)
 *
 * Run:
 *   k6 run tests/load/auth.js
 *   k6 run -e BASE_URL=http://your-host:5000 tests/load/auth.js
 */

import http from "k6/http";
import { check, sleep } from "k6";
import { Counter, Rate, Trend } from "k6/metrics";

const BASE_URL = __ENV.BASE_URL || "http://localhost:5000";

export const options = {
  stages: [
    { duration: "30s", target: 20 },
    { duration: "1m",  target: 50 },
    { duration: "30s", target: 0  },
  ],
  thresholds: {
    http_req_duration:        ["p(95)<500", "p(99)<1000"],
    http_req_failed:          ["rate<0.01"],
    login_success_rate:       ["rate>0.95"],
    register_success_rate:    ["rate>0.90"],
  },
};

const loginSuccessRate   = new Rate("login_success_rate");
const registerSuccessRate = new Rate("register_success_rate");
const loginDuration      = new Trend("login_duration_ms", true);
const registerDuration   = new Trend("register_duration_ms", true);

const headers = { "Content-Type": "application/json" };

/**
 * setup() runs once before the test.
 * Creates the seed user that all VUs will log in with.
 */
export function setup() {
  const seedEmail    = `seed_load_${Date.now()}@layla-test.io`;
  const seedPassword = "LoadTest1!";

  const res = http.post(
    `${BASE_URL}/api/tokens/register`,
    JSON.stringify({ email: seedEmail, password: seedPassword, displayName: "Load Seed" }),
    { headers },
  );

  if (res.status !== 200 && res.status !== 201) {
    console.warn(`Seed user registration failed (${res.status}): ${res.body}`);
  }

  return { seedEmail, seedPassword };
}

export default function ({ seedEmail, seedPassword }) {
  const scenario = Math.random() < 0.7 ? "login" : "register";

  if (scenario === "login") {
    runLogin(seedEmail, seedPassword);
  } else {
    runRegister();
  }

  sleep(0.5);
}

function runLogin(email, password) {
  const res = http.post(
    `${BASE_URL}/api/tokens/login`,
    JSON.stringify({ email, password }),
    { headers, tags: { scenario: "login" } },
  );

  loginDuration.add(res.timings.duration);

  const ok = check(res, {
    "login: status 200":       (r) => r.status === 200,
    "login: has token":        (r) => !!r.json("token"),
    "login: has userId":       (r) => !!r.json("userId"),
    "login: duration < 500ms": (r) => r.timings.duration < 500,
  });

  loginSuccessRate.add(ok);
}

function runRegister() {
  const email    = `user_${__VU}_${Date.now()}@layla-test.io`;
  const password = "LoadTest1!";

  const res = http.post(
    `${BASE_URL}/api/tokens/register`,
    JSON.stringify({ email, password, displayName: `VU ${__VU}` }),
    { headers, tags: { scenario: "register" } },
  );

  registerDuration.add(res.timings.duration);

  const ok = check(res, {
    "register: status 200 or 201": (r) => r.status === 200 || r.status === 201,
    "register: duration < 800ms":  (r) => r.timings.duration < 800,
  });

  registerSuccessRate.add(ok);
}
