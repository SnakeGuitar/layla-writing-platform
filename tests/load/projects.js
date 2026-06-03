/**
 * Load test — Project CRUD endpoints
 *
 * Tests:
 *   POST   /api/projects         (create)
 *   GET    /api/projects         (list user projects)
 *   GET    /api/projects/:id     (get by id)
 *   PUT    /api/projects/:id     (update)
 *   DELETE /api/projects/:id     (delete)
 *   GET    /api/projects/public  (public feed — no auth)
 *
 * Run:
 *   k6 run tests/load/projects.js
 *   k6 run -e BASE_URL=http://host:5000 -e SEED_EMAIL=user@x.com -e SEED_PASS=P4ss! tests/load/projects.js
 */

import http from "k6/http";
import { check, sleep, group } from "k6";
import { Rate, Trend } from "k6/metrics";

const BASE_URL = __ENV.BASE_URL || "http://localhost:5000";

export const options = {
  stages: [
    { duration: "20s", target: 15 },
    { duration: "90s", target: 30 },
    { duration: "20s", target: 0  },
  ],
  thresholds: {
    http_req_duration:       ["p(95)<600"],
    http_req_failed:         ["rate<0.02"],
    create_project_p95:      ["p(95)<700"],
    list_projects_p95:       ["p(95)<400"],
  },
};

const createP95  = new Trend("create_project_p95", true);
const listP95    = new Trend("list_projects_p95", true);
const crudErrors = new Rate("crud_error_rate");

const EMAIL    = __ENV.SEED_EMAIL || "seed@layla-test.io";
const PASSWORD = __ENV.SEED_PASS  || "LoadTest1!";

export function setup() {
  // Ensure seed user exists
  http.post(`${BASE_URL}/api/tokens/register`,
    JSON.stringify({ email: EMAIL, password: PASSWORD, displayName: "Projects Load Seed" }),
    { headers: { "Content-Type": "application/json" } });

  // Login to get token
  const loginRes = http.post(`${BASE_URL}/api/tokens/login`,
    JSON.stringify({ email: EMAIL, password: PASSWORD }),
    { headers: { "Content-Type": "application/json" } });

  const token = loginRes.json("token");
  if (!token) {
    throw new Error(`Setup login failed (${loginRes.status}): ${loginRes.body}`);
  }
  return { token: String(token) };
}

export default function ({ token }) {
  const authHeaders = {
    "Content-Type":  "application/json",
    "Authorization": `Bearer ${token}`,
  };

  group("public feed (no auth)", () => {
    const res = http.get(`${BASE_URL}/api/projects/public`);
    check(res, {
      "public feed: status 200":      (r) => r.status === 200,
      "public feed: returns array":   (r) => Array.isArray(r.json()),
    });
  });

  group("create → list → update → delete lifecycle", () => {
    // Create
    const createRes = http.post(
      `${BASE_URL}/api/projects`,
      JSON.stringify({ title: `Load Project ${__VU}-${Date.now()}`, isPublic: false }),
      { headers: authHeaders },
    );

    createP95.add(createRes.timings.duration);

    const created = check(createRes, {
      "create: status 201 or 200": (r) => r.status === 201 || r.status === 200,
      "create: has id":            (r) => !!r.json("id"),
    });
    crudErrors.add(!created);
    if (!created) return;

    const projectId = createRes.json("id");

    // List
    const listRes = http.get(`${BASE_URL}/api/projects`, { headers: authHeaders });
    listP95.add(listRes.timings.duration);
    check(listRes, {
      "list: status 200":      (r) => r.status === 200,
      "list: is array":        (r) => Array.isArray(r.json()),
    });

    // Get by ID
    const getRes = http.get(`${BASE_URL}/api/projects/${projectId}`, { headers: authHeaders });
    check(getRes, {
      "get by id: status 200": (r) => r.status === 200,
      "get by id: correct id": (r) => r.json("id") === projectId,
    });

    // Update
    const updateRes = http.put(
      `${BASE_URL}/api/projects/${projectId}`,
      JSON.stringify({ title: "Updated Title", isPublic: true }),
      { headers: authHeaders },
    );
    check(updateRes, {
      "update: status 200": (r) => r.status === 200,
    });

    // Delete
    const deleteRes = http.del(`${BASE_URL}/api/projects/${projectId}`, null, { headers: authHeaders });
    check(deleteRes, {
      "delete: status 200 or 204": (r) => r.status === 200 || r.status === 204,
    });
  });

  sleep(0.3);
}
