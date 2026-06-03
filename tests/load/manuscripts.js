/**
 * Load test — Manuscript (chapter) read/write endpoints
 *
 * Tests:
 *   GET  /api/manuscripts/:projectId           (list manuscripts)
 *   GET  /api/manuscripts/:projectId/:chapId   (get chapter content)
 *   PUT  /api/manuscripts/:projectId/:chapId   (save chapter RTF content)
 *
 * Setup creates one project and one manuscript so all VUs hit the same
 * "hot" document, simulating concurrent collaborative editing.
 *
 * Run:
 *   k6 run tests/load/manuscripts.js
 *   k6 run -e BASE_URL=http://host:5000 tests/load/manuscripts.js
 */

import http from "k6/http";
import { check, sleep, group } from "k6";
import { Rate, Trend } from "k6/metrics";

const BASE_URL = __ENV.BASE_URL || "http://localhost:5000";

export const options = {
  stages: [
    { duration: "20s", target: 10 },
    { duration: "2m",  target: 20 },
    { duration: "20s", target: 0  },
  ],
  thresholds: {
    http_req_duration:  ["p(95)<800"],
    http_req_failed:    ["rate<0.02"],
    chapter_read_p95:   ["p(95)<500"],
    chapter_write_p95:  ["p(95)<900"],
  },
};

const chapterReadP95  = new Trend("chapter_read_p95", true);
const chapterWriteP95 = new Trend("chapter_write_p95", true);
const writeErrors     = new Rate("chapter_write_error_rate");

const EMAIL    = __ENV.SEED_EMAIL || "ms-seed@layla-test.io";
const PASSWORD = __ENV.SEED_PASS  || "LoadTest1!";

export function setup() {
  const jsonHeaders = { "Content-Type": "application/json" };

  // Ensure user exists
  http.post(`${BASE_URL}/api/tokens/register`,
    JSON.stringify({ email: EMAIL, password: PASSWORD, displayName: "Manuscript Seed" }),
    { headers: jsonHeaders });

  // Login
  const loginRes = http.post(`${BASE_URL}/api/tokens/login`,
    JSON.stringify({ email: EMAIL, password: PASSWORD }),
    { headers: jsonHeaders });

  const token = String(loginRes.json("token") || "");
  if (!token) throw new Error(`Login failed: ${loginRes.body}`);

  const authHeaders = { "Content-Type": "application/json", Authorization: `Bearer ${token}` };

  // Create project
  const projRes = http.post(`${BASE_URL}/api/projects`,
    JSON.stringify({ title: "Manuscript Load Test Novel", isPublic: false }),
    { headers: authHeaders });
  const projectId = String(projRes.json("id") || "");
  if (!projectId) throw new Error(`Project creation failed: ${projRes.body}`);

  // Create manuscript
  const msRes = http.post(`${BASE_URL}/api/manuscripts/${projectId}`,
    JSON.stringify({ title: "Chapter 1: The Beginning" }),
    { headers: authHeaders });
  const manuscriptId = String(msRes.json("id") || msRes.json("_id") || "");

  // Fetch chapters if manuscript was created
  let chapterId = "";
  if (manuscriptId) {
    const chapRes = http.get(`${BASE_URL}/api/manuscripts/${projectId}/${manuscriptId}`,
      { headers: authHeaders });
    const chapters = chapRes.json("chapters");
    if (Array.isArray(chapters) && chapters.length > 0) {
      chapterId = String(chapters[0].id || chapters[0]._id || "");
    }
  }

  return { token, projectId, manuscriptId, chapterId };
}

export default function ({ token, projectId, manuscriptId, chapterId }) {
  if (!projectId || !manuscriptId) {
    console.warn("Setup did not produce a project/manuscript — skipping iteration.");
    sleep(1);
    return;
  }

  const authHeaders = { "Content-Type": "application/json", Authorization: `Bearer ${token}` };

  group("list manuscripts", () => {
    const res = http.get(`${BASE_URL}/api/manuscripts/${projectId}`, { headers: authHeaders });
    check(res, {
      "list manuscripts: status 200":    (r) => r.status === 200,
      "list manuscripts: returns array": (r) => Array.isArray(r.json()),
    });
  });

  if (chapterId) {
    group("read chapter content", () => {
      const res = http.get(
        `${BASE_URL}/api/manuscripts/${projectId}/${chapterId}`,
        { headers: authHeaders },
      );
      chapterReadP95.add(res.timings.duration);
      check(res, { "read chapter: status 200": (r) => r.status === 200 });
    });

    group("write chapter content (concurrent)", () => {
      const rtfContent = `{\\rtf1 VU ${__VU} at ${Date.now()} — concurrent edit simulation}`;
      const res = http.put(
        `${BASE_URL}/api/manuscripts/${projectId}/${chapterId}`,
        JSON.stringify({ content: rtfContent }),
        { headers: authHeaders },
      );
      chapterWriteP95.add(res.timings.duration);

      const ok = check(res, {
        "write chapter: status 200":       (r) => r.status === 200,
        "write chapter: duration < 900ms": (r) => r.timings.duration < 900,
      });
      writeErrors.add(!ok);
    });
  }

  sleep(0.5);
}
