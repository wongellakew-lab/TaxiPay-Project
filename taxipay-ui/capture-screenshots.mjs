/* Capture TaxiPay UI screenshots by injecting real sessions into headless Chrome via CDP. */
import { execFile } from 'node:child_process';
import http from 'node:http';

const CHROME = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
const PORT = 9333;
const OUT = 'C:\\Users\\wonge\\Desktop\\FullStackDevelopment\\TaxiPayApi\\screenshots';
const API = 'http://localhost:5205/api';
const UI = 'http://localhost:4200';

function httpJson(method, url, body, headers = {}) {
  return new Promise((resolve, reject) => {
    const data = body ? JSON.stringify(body) : null;
    const req = http.request(url, { method, headers: { 'Content-Type': 'application/json', ...headers } }, res => {
      let buf = '';
      res.on('data', c => (buf += c));
      res.on('end', () => {
        try { resolve(JSON.parse(buf)); } catch { resolve(buf); }
      });
    });
    req.on('error', reject);
    if (data) req.write(data);
    req.end();
  });
}

const sleep = ms => new Promise(r => setTimeout(r, ms));

let wsId = 0;
const pending = new Map();
function send(ws, method, params = {}, sessionId) {
  return new Promise((resolve, reject) => {
    const id = ++wsId;
    pending.set(id, { resolve, reject });
    ws.write(JSON.stringify({ id, method, params, ...(sessionId ? { sessionId } : {}) }));
  });
}

// Minimal WebSocket client over raw TCP (no deps): RFC6455 client handshake + frames
import net from 'node:net';
import crypto from 'node:crypto';

function connectWs(host, port, path) {
  return new Promise((resolve, reject) => {
    const key = crypto.randomBytes(16).toString('base64');
    const socket = net.connect(port, host, () => {
      socket.write(
        `GET ${path} HTTP/1.1\r\nHost: ${host}:${port}\r\nUpgrade: websocket\r\nConnection: Upgrade\r\n` +
        `Sec-WebSocket-Key: ${key}\r\nSec-WebSocket-Version: 13\r\n\r\n`
      );
    });
    let handshaken = false;
    let buffer = Buffer.alloc(0);
    const ws = {
      socket,
      write(obj) {
        const payload = Buffer.from(JSON.stringify(obj));
        const mask = crypto.randomBytes(4);
        let header;
        if (payload.length < 126) {
          header = Buffer.from([0x81, 0x80 | payload.length]);
        } else if (payload.length < 65536) {
          header = Buffer.alloc(4);
          header[0] = 0x81; header[1] = 0x80 | 126; header.writeUInt16BE(payload.length, 2);
        } else {
          header = Buffer.alloc(10);
          header[0] = 0x81; header[1] = 0x80 | 127; header.writeBigUInt64BE(BigInt(payload.length), 2);
        }
        const masked = Buffer.from(payload.map((b, i) => b ^ mask[i % 4]));
        socket.write(Buffer.concat([header, mask, masked]));
      },
      onMessage: null,
    };
    socket.on('data', chunk => {
      buffer = Buffer.concat([buffer, chunk]);
      if (!handshaken) {
        const idx = buffer.indexOf('\r\n\r\n');
        if (idx === -1) return;
        const head = buffer.slice(0, idx).toString();
        if (!/101/.test(head.split('\r\n')[0])) return reject(new Error('WS handshake failed: ' + head.split('\r\n')[0]));
        buffer = buffer.slice(idx + 4);
        handshaken = true;
        resolve(ws);
      }
      // Parse frames
      while (buffer.length >= 2) {
        const len0 = buffer[1] & 0x7f;
        let offset = 2, payloadLen = len0;
        if (len0 === 126) { if (buffer.length < 4) return; payloadLen = buffer.readUInt16BE(2); offset = 4; }
        else if (len0 === 127) { if (buffer.length < 10) return; payloadLen = Number(buffer.readBigUInt64BE(2)); offset = 10; }
        if (buffer.length < offset + payloadLen) return;
        const payload = buffer.slice(offset, offset + payloadLen);
        buffer = buffer.slice(offset + payloadLen);
        if ((buffer[0] ?? 0) === 0) {} // no-op
        const opcode = len0 !== undefined ? null : null;
        const firstByte = payload.length >= 0 ? null : null;
        void opcode; void firstByte;
        if (ws.onMessage && payload.length) {
          try { ws.onMessage(JSON.parse(payload.toString('utf8'))); } catch { /* ignore */ }
        }
      }
    });
    socket.on('error', reject);
  });
}

async function main() {
  // 1. Login to all roles via the API (retry on 429 rate limiting — auth-strict = 5 req/min)
  async function loginWithRetry(label, fn, attempts = 8) {
    for (let i = 1; i <= attempts; i++) {
      const res = await fn();
      if (res && res.accessToken) return res;
      console.warn(`${label} attempt ${i} failed (${res?.status || res?.title || 'no token'}), waiting 20s...`);
      await sleep(20000);
    }
    throw new Error(`${label} login failed after ${attempts} attempts`);
  }

  const passenger = await loginWithRetry('passenger', () => httpJson('POST', `${API}/passengers/login`, { phone: '0955566778', password: 'Pass@123' }));
  await sleep(3000);
  const driver = await loginWithRetry('driver', () => httpJson('POST', `${API}/drivers/login`, { phone: '0988990011', password: 'Driver@123' }));
  await sleep(3000);
  const admin = await loginWithRetry('admin', () => httpJson('POST', `${API}/admins/login`, { email: 'admin@taxipay.com', password: 'Admin@123' }));

  const driverId = driver.driver.id;
  const driverCode = driver.driver.driverId;
  const passengerId = passenger.passenger.id;

  const sessions = {
    passenger: {
      id: passengerId, firstName: passenger.passenger.firstName, lastName: passenger.passenger.lastName,
      phone: passenger.passenger.phone, role: 'passenger', token: passenger.accessToken,
      tokenExpiresAt: passenger.tokenExpiresAt, passengerData: passenger.passenger,
    },
    driver: {
      id: driverId, driverCode, firstName: driver.driver.fullName.split(' ')[0], lastName: driver.driver.fullName.split(' ')[1] || '',
      phone: driver.driver.phone, role: 'driver', token: driver.accessToken,
      tokenExpiresAt: driver.tokenExpiresAt, driverData: driver.driver,
    },
    admin: {
      id: admin.admin.id, firstName: admin.admin.firstName, lastName: admin.admin.lastName,
      phone: admin.admin.phone, email: admin.admin.email, role: 'admin', token: admin.accessToken,
      tokenExpiresAt: admin.tokenExpiresAt, adminData: admin.admin,
    },
  };
  console.log('Logged in: passenger=%s driver=%s(%s) admin=%s', passengerId.slice(0, 8), driverCode, driverId.slice(0, 8), admin.admin.email);

  // 2. Launch Chrome headless with CDP
  const proc = execFile(CHROME, [
    '--headless=new', '--disable-gpu', '--no-first-run', '--no-default-browser-check',
    `--remote-debugging-port=${PORT}`, '--window-size=1440,900', '--hide-scrollbars', '--user-data-dir=' + OUT + '\\chrome-profile',
    'about:blank',
  ]);
  proc.stderr.on('data', () => {});

  // Wait for CDP endpoint
  let version = null;
  for (let i = 0; i < 40; i++) {
    try { version = await httpJson('GET', `http://127.0.0.1:${PORT}/json/version`); break; } catch { await sleep(500); }
  }
  if (!version) throw new Error('CDP did not come up');
  console.log('CDP ready:', version.Browser);

  const targets = await httpJson('GET', `http://127.0.0.1:${PORT}/json/list`);
  const page = targets.find(t => t.type === 'page');
  const ws = await connectWs('127.0.0.1', PORT, new URL(page.webSocketDebuggerUrl).pathname);
  ws.onMessage = msg => {
    if (msg.id && pending.has(msg.id)) {
      const { resolve, reject } = pending.get(msg.id);
      pending.delete(msg.id);
      msg.error ? reject(new Error(JSON.stringify(msg.error))) : resolve(msg.result);
    }
  };
  await send(ws, 'Page.enable');
  await send(ws, 'Runtime.enable');

  async function evalJs(expression) {
    const r = await send(ws, 'Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true });
    if (r.exceptionDetails) throw new Error('Eval failed: ' + JSON.stringify(r.exceptionDetails.exception?.description || r.exceptionDetails));
    return r.result?.value;
  }

  async function setSession(session) {
    await evalJs(`localStorage.setItem('taxipay_auth_user', ${JSON.stringify(JSON.stringify(session))})`);
  }
  async function clearSession() {
    await evalJs(`localStorage.removeItem('taxipay_auth_user')`);
  }

  async function navigate(url, waitMs = 9000) {
    await send(ws, 'Page.navigate', { url });
    await sleep(waitMs);
  }

  async function shot(name) {
    const { data } = await send(ws, 'Page.captureScreenshot', { format: 'png' });
    const { writeFileSync } = await import('node:fs');
    writeFileSync(`${OUT}\\${name}.png`, Buffer.from(data, 'base64'));
    console.log('captured', name);
  }

  // 3. Capture flow
  // Login page (fresh, no session)
  await clearSession();
  await navigate(`${UI}/login`, 9000);
  await shot('login-page');

  // Passenger dashboard
  await setSession(sessions.passenger);
  await navigate(`${UI}/passenger/dashboard`, 10000);
  await shot('passenger-dashboard');

  // Pay trip page
  await navigate(`${UI}/passenger/pay`, 8000);
  await shot('pay-trip');

  // Driver dashboard
  await setSession(sessions.driver);
  await navigate(`${UI}/driver/dashboard`, 10000);
  await shot('driver-dashboard');

  // Admin dashboard
  await setSession(sessions.admin);
  await navigate(`${UI}/admin/dashboard`, 10000);
  await shot('admin-dashboard');

  console.log('DONE');
  proc.kill();
  process.exit(0);
}

main().catch(e => { console.error(e); process.exit(1); });
