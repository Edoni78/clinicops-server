/**
 * ClinicOps API – full clinic workflow stress test (k6)
 *
 * Default mode (TEST_AS_ADMIN=true): one ClinicAdmin login tests the whole UI flow:
 *   Admin → list doctors → register patient (assign doctor) → vitals
 *        → waiting queue → consult → diagnosis/report → Finished
 *
 * Alternate mode (TEST_AS_ADMIN=false): separate nurse + doctor logins.
 *
 * Prerequisites:
 *   - k6 installed, API running (dotnet run)
 *   - ClinicAdmin user, MFA disabled for load testing
 *   - At least one Doctor in the clinic (for assignment dropdown)
 *
 * Run as clinic admin (recommended):
 *   k6 run -e ADMIN_EMAIL=admin@clinic.com -e ADMIN_PASSWORD=YourPass stress-test/local-test.js
 *
 * Short smoke test:
 *   k6 run --vus 1 --iterations 2 -e ADMIN_EMAIL=... -e ADMIN_PASSWORD=... stress-test/local-test.js
 */

import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';

// =============================================================================
// Configuration (override with -e KEY=value)
// =============================================================================

const BASE_URL = (__ENV.BASE_URL || 'http://localhost:5258').replace(/\/$/, '');

/** true = single ClinicAdmin tests nurse + doctor steps (default) */
const TEST_AS_ADMIN = (__ENV.TEST_AS_ADMIN || 'true').toLowerCase() !== 'false';

const ADMIN_EMAIL = __ENV.ADMIN_EMAIL || __ENV.CLINIC_ADMIN_EMAIL || 'admin@clinic.com';
const ADMIN_PASSWORD = __ENV.ADMIN_PASSWORD || __ENV.CLINIC_ADMIN_PASSWORD || 'Admin123!';

/** Only used when TEST_AS_ADMIN=false */
const NURSE_EMAIL = __ENV.NURSE_EMAIL || 'nurse@clinic.com';
const NURSE_PASSWORD = __ENV.NURSE_PASSWORD || 'Nurse123!';
const DOCTOR_EMAIL = __ENV.DOCTOR_EMAIL || 'doctor@clinic.com';
const DOCTOR_PASSWORD = __ENV.DOCTOR_PASSWORD || 'Doctor123!';

/** If set, registration always assigns this doctor user id */
const ASSIGNED_DOCTOR_USER_ID = __ENV.ASSIGNED_DOCTOR_USER_ID || '';

/** MFA: use non-MFA test users, or pass current 6-digit code */
const MFA_CODE = __ENV.MFA_CODE || '';

const JSON_HEADERS = {
  'Content-Type': 'application/json',
  Accept: 'application/json',
};

// =============================================================================
// Load profile
// =============================================================================

// Use // comments here only (not /** */ inside options — k6 parser can break)
export const options = {
  scenarios: {
    full_clinic_workflow: {
      executor: 'ramping-vus',
      exec: 'fullClinicWorkflow',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 3 },
        { duration: '1m', target: 8 },
        { duration: '1m', target: 12 },
        { duration: '30s', target: 0 },
      ],
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.10'],
    http_req_duration: ['p(95)<3000'],
    checks: ['rate>0.90'],
  },
};

// =============================================================================
// HTTP helpers
// =============================================================================

function authParams(token, name) {
  const headers = { ...JSON_HEADERS };
  if (token) headers.Authorization = `Bearer ${token}`;
  return { headers, tags: { name }, timeout: '30s' };
}

function parseJson(res) {
  try {
    return res.json();
  } catch (e) {
    return null;
  }
}

function checkStatus200(res, label) {
  return check(res, { [`${label}: status 200`]: (r) => r.status === 200 });
}

// =============================================================================
// Authentication
// =============================================================================

function login(email, password, tagPrefix) {
  const loginRes = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({ email, password }),
    authParams(null, `${tagPrefix}_auth_login`)
  );

  if (!checkStatus200(loginRes, `${tagPrefix} login`)) return null;

  const body = parseJson(loginRes);
  if (!body) return null;

  if (body.requiresMfa === true) {
    if (!MFA_CODE || !body.mfaTicket) {
      check(null, {
        [`${tagPrefix}: MFA off or set MFA_CODE`]: () => false,
      });
      return null;
    }

    const mfaRes = http.post(
      `${BASE_URL}/api/auth/mfa/verify-login`,
      JSON.stringify({ mfaTicket: body.mfaTicket, code: MFA_CODE }),
      authParams(null, `${tagPrefix}_auth_mfa`)
    );

    if (!checkStatus200(mfaRes, `${tagPrefix} MFA verify`)) return null;
    const mfaBody = parseJson(mfaRes);
    return (mfaBody && mfaBody.accessToken) || null;
  }

  check(body, {
    [`${tagPrefix}: has accessToken`]: (b) => !!b.accessToken,
  });

  return body.accessToken || null;
}

/** ClinicAdmin for full flow, or nurse/doctor when TEST_AS_ADMIN=false */
function loginForRole(role) {
  if (TEST_AS_ADMIN) {
    return login(ADMIN_EMAIL, ADMIN_PASSWORD, 'admin');
  }
  if (role === 'nurse') {
    return login(NURSE_EMAIL, NURSE_PASSWORD, 'nurse');
  }
  return login(DOCTOR_EMAIL, DOCTOR_PASSWORD, 'doctor');
}

// =============================================================================
// Reception / nurse workflow steps (also run as ClinicAdmin)
// =============================================================================

function fetchDoctorId(token) {
  if (ASSIGNED_DOCTOR_USER_ID) return ASSIGNED_DOCTOR_USER_ID;

  const res = http.get(
    `${BASE_URL}/api/ClinicUser?role=Doctor`,
    authParams(token, 'list_doctors')
  );

  if (!checkStatus200(res, 'doctors list')) return null;

  const doctors = parseJson(res);
  if (!Array.isArray(doctors) || doctors.length === 0) {
    check(null, { 'at least one doctor in clinic': () => false });
    return null;
  }

  return doctors[0].id;
}

function registerPatient(token, doctorUserId, vu, iter) {
  const suffix = `${vu}-${iter}-${Date.now()}`;
  const payload = {
    firstName: 'K6',
    lastName: `Patient${suffix}`,
    dateOfBirth: '1990-06-15T00:00:00.000Z',
    gender: 'F',
    phone: `069${randomIntBetween(1000000, 9999999)}`,
    notes: `k6 full workflow ${suffix}`,
    assignedDoctorUserId: doctorUserId,
  };

  const res = http.post(
    `${BASE_URL}/api/Patient/register`,
    JSON.stringify(payload),
    authParams(token, 'register_patient')
  );

  check(res, {
    'register patient: status 200': (r) => r.status === 200,
    'register patient: has case id': (r) => {
      const d = parseJson(r);
      return d && d.patientCaseId;
    },
    'register patient: status Waiting': (r) => {
      const d = parseJson(r);
      return d && d.patientCaseStatus === 'Waiting';
    },
  });

  const data = parseJson(res);
  if (!data || !data.patientCaseId) return null;

  return {
    patientId: data.id,
    caseId: data.patientCaseId,
    assignedDoctorUserId: data.assignedDoctorUserId || doctorUserId,
  };
}

function submitVitals(token, caseId) {
  const payload = {
    weightKg: 70 + randomIntBetween(0, 30),
    systolicPressure: 110 + randomIntBetween(0, 30),
    diastolicPressure: 70 + randomIntBetween(0, 20),
    temperatureC: 36.5 + randomIntBetween(0, 10) / 10,
    heartRate: 60 + randomIntBetween(0, 40),
  };

  const res = http.post(
    `${BASE_URL}/api/PatientCase/${caseId}/vitals`,
    JSON.stringify(payload),
    authParams(token, 'submit_vitals')
  );

  checkStatus200(res, 'submit vitals');
  return parseJson(res);
}

// =============================================================================
// Doctor workflow steps
// =============================================================================

function listWaitingCases(doctorToken) {
  const res = http.get(
    `${BASE_URL}/api/PatientCase?status=Waiting`,
    authParams(doctorToken, 'doctor_list_waiting')
  );

  if (!checkStatus200(res, 'doctor waiting list')) return [];

  const cases = parseJson(res);
  return Array.isArray(cases) ? cases : [];
}

function getCaseDetail(doctorToken, caseId) {
  const res = http.get(
    `${BASE_URL}/api/PatientCase/${caseId}`,
    authParams(doctorToken, 'doctor_case_detail')
  );

  checkStatus200(res, 'doctor case detail');
  return parseJson(res);
}

function updateCaseStatus(doctorToken, caseId, status) {
  const res = http.patch(
    `${BASE_URL}/api/PatientCase/${caseId}/status?status=${status}`,
    null,
    authParams(doctorToken, `doctor_status_${status}`)
  );

  const ok = check(res, {
    [`doctor status ${status}: 200 or expected 400`]: (r) =>
      r.status === 200 || (status === 'InConsultation' && r.status === 400),
  });

  return ok && res.status === 200;
}

function submitMedicalReport(doctorToken, caseId) {
  const suffix = Date.now();
  const payload = {
    anamneza: `k6 anamneza ${suffix}`,
    diagnosis: `k6 diagnosis ${suffix}`,
    therapy: `k6 therapy ${suffix}`,
  };

  const res = http.post(
    `${BASE_URL}/api/PatientCase/${caseId}/report`,
    JSON.stringify(payload),
    authParams(doctorToken, 'doctor_submit_report')
  );

  checkStatus200(res, 'doctor submit report');
  return parseJson(res);
}

function getMedicalReport(doctorToken, caseId) {
  const res = http.get(
    `${BASE_URL}/api/PatientCase/${caseId}/report`,
    authParams(doctorToken, 'doctor_get_report')
  );

  checkStatus200(res, 'doctor get report');
  return parseJson(res);
}

function runDoctorConsultation(doctorToken, caseId) {
  group('Doctor: consultation & diagnosis', () => {
    getCaseDetail(doctorToken, caseId);

    const started = updateCaseStatus(doctorToken, caseId, 'InConsultation');
    if (!started) {
      // Clinic allows only one InConsultation at a time – try finishing then retry once
      sleep(0.5);
      updateCaseStatus(doctorToken, caseId, 'InConsultation');
    }

    sleep(0.3);

    submitMedicalReport(doctorToken, caseId);
    getMedicalReport(doctorToken, caseId);

    updateCaseStatus(doctorToken, caseId, 'Finished');

    const finishedRes = http.get(
      `${BASE_URL}/api/PatientCase?status=Finished`,
      authParams(doctorToken, 'doctor_list_finished')
    );
    checkStatus200(finishedRes, 'doctor finished list');
  });
}

// =============================================================================
// Full end-to-end scenario (exported for k6 scenarios)
// =============================================================================

export function fullClinicWorkflow() {
  let caseId = null;
  const actorLabel = TEST_AS_ADMIN ? 'ClinicAdmin' : 'Nurse+Doctor';

  const receptionToken = loginForRole('nurse');
  if (!receptionToken) {
    sleep(1);
    return;
  }

  // ---------------------------------------------------------------------------
  // Phase 1 – Reception (nurse section): register + assign doctor + vitals
  // ---------------------------------------------------------------------------
  group(`${actorLabel}: register patient & assign doctor`, () => {
    const doctorId = fetchDoctorId(receptionToken);
    if (!doctorId) return;

    const registered = registerPatient(receptionToken, doctorId, __VU, __ITER);
    if (!registered) return;

    caseId = registered.caseId;
    submitVitals(receptionToken, caseId);

    const waitingRes = http.get(
      `${BASE_URL}/api/PatientCase?status=Waiting`,
      authParams(receptionToken, 'verify_waiting')
    );
    checkStatus200(waitingRes, 'verify waiting queue');
  });

  if (!caseId) {
    sleep(1);
    return;
  }

  sleep(0.5);

  // ---------------------------------------------------------------------------
  // Phase 2 – Doctor section: consult, diagnosis, finish
  // (same admin token when TEST_AS_ADMIN=true)
  // ---------------------------------------------------------------------------
  group(`${actorLabel}: consult, diagnosis & finish`, () => {
    const consultToken = TEST_AS_ADMIN ? receptionToken : loginForRole('doctor');
    if (!consultToken) return;

    const waiting = listWaitingCases(consultToken);
    let targetCaseId = caseId;

    const found = waiting.find((c) => c.id === caseId);
    if (!found && waiting.length > 0) {
      targetCaseId = waiting[0].id;
    } else if (!found && waiting.length === 0) {
      check(null, { 'consult: has waiting case': () => false });
      return;
    }

    runDoctorConsultation(consultToken, targetCaseId);
  });

  sleep(randomIntBetween(1, 2));
}

// =============================================================================
// Optional isolated scenarios (enable in options.scenarios if needed)
// =============================================================================

export function nurseOnlyFlow() {
  const token = loginForRole('nurse');
  if (!token) return;

  const doctorId = fetchDoctorId(token);
  if (!doctorId) return;

  const registered = registerPatient(token, doctorId, __VU, __ITER);
  if (!registered) return;

  submitVitals(token, registered.caseId);
  sleep(1);
}

export function doctorOnlyFlow() {
  const doctorToken = loginForRole('doctor');
  if (!doctorToken) return;

  const waiting = listWaitingCases(doctorToken);
  if (waiting.length === 0) {
    sleep(1);
    return;
  }

  runDoctorConsultation(doctorToken, waiting[0].id);
  sleep(1);
}

// Fallback when running with --vus / --iterations (overrides scenarios)
export default function () {
  fullClinicWorkflow();
}

// =============================================================================
// Setup / teardown
// =============================================================================

export function setup() {
  const res = http.get(`${BASE_URL}/swagger/index.html`, {
    timeout: '10s',
    tags: { name: 'setup_health' },
  });

  if (res.status !== 200) {
    console.warn(`API may be down at ${BASE_URL} (status ${res.status}). Start with: dotnet run`);
  }

  return { baseUrl: BASE_URL };
}

export function teardown(data) {
  const mode = TEST_AS_ADMIN ? 'ClinicAdmin (full UI flow)' : 'Nurse + Doctor';
  console.log(`Done. Tested ${mode} against ${data.baseUrl}`);
}
