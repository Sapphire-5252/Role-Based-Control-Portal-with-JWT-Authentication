const API = '/api';

// KPI definitions
const ALL_INDICATORS = [
    { key: '2_5', label: '2.5 – Rate of Graduates Obtaining Microcredentials & Licenses', unit: '%', types: ['college', 'admin'],
      desc: 'Percentage of graduates who obtained at least one industry microcredential or professional licence during the reporting period.' },
    { key: '3_1', label: '3.1 – Job Offer Post Work-Placement', unit: '%', types: ['college', 'admin'],
      desc: 'Percentage of students who received a confirmed job offer within 3 months of completing their mandatory work-placement.' },
    { key: '3_3', label: '3.3 – Joint Industry Courses', unit: '%', types: ['college', 'admin'],
      desc: 'Percentage of active courses that are co-developed or co-delivered with at least one industry partner.' },
    { key: '3_4', label: '3.4 – Industry Contributions (AED)', unit: '#', types: ['college', 'admin'],
      desc: 'Total monetary value (AED) of cash donations, equipment, or in-kind contributions from industry partners.' },
    { key: '4_4', label: '4.4 – Student Participation Rate in Research', unit: '%', types: ['college', 'research_office', 'admin'],
      desc: 'Percentage of enrolled students who actively participated in at least one funded research project.' },
    { key: '4_5', label: '4.5 – Impact of Research', unit: '#', types: ['college', 'research_office', 'admin'],
      desc: 'Count of research outputs with verifiable real-world impact.' },
    { key: '5_1', label: '5.1 – Global University and Subject Rankings', unit: '#', types: ['college', 'admin'],
      desc: 'Number of recognised global ranking lists in which the university appears.' },
    { key: '5_3', label: '5.3 – Student Participation in International Dual/Joint Degrees', unit: '%', types: ['cgs', 'admin'],
      desc: 'Percentage of enrolled graduate students in dual or joint degree programmes with international partners.' },
    { key: '6_1', label: '6.1 – Academic Events with Student Participation', unit: '#', types: ['college', 'uod', 'admin'],
      desc: 'Total number of academic conferences, symposia, seminars with student participants.' },
    { key: '6_2', label: '6.2 – Events & Initiatives for the Community', unit: '#', types: ['college', 'uod', 'admin'],
      desc: 'Total number of community outreach events and engagement initiatives.' }
];

let currentUser = null;
let allRecords = [];
let pendingDeleteId = null;

// ==================== HELPER FUNCTIONS ====================
function getHeaders() {
    return { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + sessionStorage.getItem('authToken') };
}

async function apiFetch(url, options = {}) {
    const res = await fetch(url, { ...options, headers: { ...getHeaders(), ...(options.headers || {}) } });
    if (res.status === 401) { logout(); return res; }
    return res;
}

function currentYm() {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
}

function myIndicators() {
    if (!currentUser) return [];
    const ut = currentUser.role === 'admin' ? 'admin' : currentUser.userType;
    return ALL_INDICATORS.filter(i => i.types.includes(ut));
}

function fmt(val, unit) {
    if (val == null || val === '') return '—';
    if (unit === '%') return parseFloat(val).toFixed(2) + '%';
    return parseFloat(val).toLocaleString();
}

function fmtDate(dt) {
    if (!dt) return '—';
    try { return new Date(dt).toLocaleDateString('en-GB'); }
    catch { return String(dt); }
}

function displayFullName(user) {
    switch ((user.username || '').toUpperCase()) {
        case 'CGS': return 'College of Graduate Studies';
        case 'UOD': return 'Unit of Development';
        default: return user.fullName || '—';
    }
}

// ==================== LOGIN ====================
document.getElementById('loginForm').addEventListener('submit', async e => {
    e.preventDefault();
    const errEl = document.getElementById('loginError');
    errEl.style.display = 'none';
    try {
        const res = await fetch(`${API}/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                username: document.getElementById('username').value,
                password: document.getElementById('password').value
            })
        });
        const body = await res.json();
        if (!res.ok) { errEl.textContent = body.message || 'Login failed'; errEl.style.display = 'block'; return; }
        sessionStorage.setItem('authToken', body.token);
        currentUser = { username: body.username, fullName: body.fullName, role: body.role, userType: body.userType, collegeName: body.collegeName };
        initApp();
    } catch {
        errEl.textContent = 'Cannot connect to server.'; errEl.style.display = 'block';
    }
});

function logout() {
    sessionStorage.removeItem('authToken'); currentUser = null; allRecords = [];
    document.getElementById('appPage').style.display = 'none';
    document.getElementById('loginPage').style.display = '';
    document.getElementById('loginForm').reset();
}

// ==================== APP INIT ====================
function initApp() {
    document.getElementById('loginPage').style.display = 'none';
    document.getElementById('appPage').style.display = '';

    const isAdmin = currentUser.role === 'admin';
    const label = isAdmin ? 'Administrator' : (currentUser.collegeName || currentUser.userType || currentUser.username);
    document.getElementById('headerUser').textContent = `${currentUser.fullName} (${label})`;

    const nav = document.getElementById('appNav');
    const tabs = [
        { id: 'dashboard', label: 'Dashboard' },
        { id: 'records', label: isAdmin ? 'All Records' : 'My Records' },
        ...(isAdmin ? [{ id: 'consolidated', label: 'Consolidated' }, { id: 'users', label: 'Users' }] : [])
    ];
    nav.innerHTML = tabs.map((t, i) =>
        `<button class="nav-tab${i === 0 ? ' active' : ''}" onclick="showTab('${t.id}',this)">${t.label}</button>`
    ).join('');

    document.getElementById('recordsTitle').textContent = isAdmin ? 'All Records' : 'My Records';

    const ym = currentYm();
    const cInput = document.getElementById('consolidatedMonth');
    if (cInput) cInput.value = ym;

    buildTableHeads();
    loadRecords();
    if (isAdmin) loadUsers();
    showTab('dashboard', nav.querySelector('.nav-tab'));
}

// ==================== TABS ====================
function showTab(id, btn) {
    document.querySelectorAll('.tab-content').forEach(el => el.style.display = 'none');
    document.querySelectorAll('.nav-tab').forEach(el => el.classList.remove('active'));
    const tab = document.getElementById('tab-' + id);
    if (tab) tab.style.display = '';
    if (btn) btn.classList.add('active');
    if (id === 'consolidated') loadConsolidated();
}

// ==================== TABLE HEADS ====================
function buildTableHeads() {
    const isAdmin = currentUser.role === 'admin';
    const inds = myIndicators();

    document.getElementById('recordsHead').innerHTML = `
        <tr>
            <th>ID</th>
            ${isAdmin ? '<th>College / Unit</th>' : ''}
            <th>Month</th>
            ${inds.map(i => `<th>${i.key.replace('_', '.')}</th>`).join('')}
            <th>Updated</th>
            <th>Actions</th>
        </tr>`;

    document.getElementById('dashboardHead').innerHTML = `
        <tr>
            <th>Month</th>
            ${inds.map(i => `<th>${i.key.replace('_', '.')}</th>`).join('')}
            <th>Updated</th>
        </tr>`;
}

// ==================== RECORDS ====================
async function loadRecords() {
    try {
        const res = await apiFetch(`${API}/data`);
        if (!res.ok) return;
        const data = await res.json();
        allRecords = data.items ?? data;
        renderRecords();
        renderDashboard();
    } catch { console.error('Failed to load records'); }
}

function renderRecords() {
    const isAdmin = currentUser.role === 'admin';
    const inds = myIndicators();
    const tbody = document.getElementById('recordsBody');
    if (!allRecords.length) {
        tbody.innerHTML = `<tr><td colspan="20" class="empty-row">No records found.</td></tr>`;
        return;
    }
    tbody.innerHTML = allRecords.map(r => `
        <tr>
            <td>${r.id}</td>
            ${isAdmin ? `<td>${r.collegeName || r.collegeKey || r.userType || '—'}</td>` : ''}
            <td>${r.submissionMonth}</td>
            ${inds.map(i => `<td>${fmt(r['ind_' + i.key + '_R'], i.unit)}</td>`).join('')}
            <td>${fmtDate(r.updatedAt || r.createdAt)}</td>
            <td class="action-cell">
                <button class="btn btn-sm btn-secondary" onclick="openEditModal(${r.id})">Edit</button>
                <button class="btn btn-sm btn-danger" onclick="openDeleteModal(${r.id})">Del</button>
            </td>
        </tr>
    `).join('');
}

function renderDashboard() {
    const inds = myIndicators();
    const recent = [...allRecords].sort((a, b) => b.submissionMonth.localeCompare(a.submissionMonth)).slice(0, 10);

    const kpiGrid = document.getElementById('kpiGrid');
    if (currentUser.role !== 'admin' && recent.length) {
        const latest = recent[0];
        kpiGrid.innerHTML = inds.map(i => `
            <div class="kpi-card">
                <div class="kpi-label">${i.key.replace('_', '.')} – ${(i.label.split('–')[1] || '').trim()}</div>
                <div class="kpi-value">${fmt(latest['ind_' + i.key + '_R'], i.unit)}</div>
                <div class="kpi-month">${latest.submissionMonth}</div>
            </div>
        `).join('');
    } else {
        kpiGrid.innerHTML = '';
    }

    const tbody = document.getElementById('dashboardBody');
    if (!recent.length) {
        tbody.innerHTML = `<tr><td colspan="20" class="empty-row">No submissions yet.</td></tr>`;
        return;
    }
    tbody.innerHTML = recent.map(r => `
        <tr>
            <td>${r.submissionMonth}</td>
            ${inds.map(i => `<td>${fmt(r['ind_' + i.key + '_R'], i.unit)}</td>`).join('')}
            <td>${fmtDate(r.updatedAt || r.createdAt)}</td>
        </tr>
    `).join('');
}

// ==================== USERS ====================
async function loadUsers() {
    try {
        const res = await apiFetch(`${API}/users`);
        if (!res.ok) return;
        const users = await res.json();
        document.getElementById('usersBody').innerHTML = users.map(u => `
            <tr>
                <td>${u.id}</td>
                <td>${u.username}</td>
                <td>${displayFullName(u)}</td>
                <td><span class="badge badge-${u.role}">${u.role}</span></td>
                <td>${u.userType || '—'}</td>
                <td>${u.collegeName || '—'}</td>
            </tr>
        `).join('');
    } catch { console.error('Failed to load users'); }
}

// ==================== CONSOLIDATED ====================
async function loadConsolidated() {
    const monthVal = document.getElementById('consolidatedMonth')?.value;
    if (!monthVal) return;
    const content = document.getElementById('consolidatedContent');
    content.innerHTML = '<p style="padding:20px;color:#64748b">Loading...</p>';
    try {
        const res = await apiFetch(`${API}/data/consolidated/${monthVal}`);
        if (!res.ok) { content.innerHTML = '<p class="alert alert-error" style="margin:16px">Failed to load.</p>'; return; }
        renderConsolidated(await res.json(), monthVal);
    } catch { content.innerHTML = '<p class="alert alert-error" style="margin:16px">Network error.</p>'; }
}

function renderConsolidated(data, month) {
    const collegeRows = ALL_INDICATORS.filter(i => i.types.includes('college')).map(i => {
        const kpi = data['ind_' + i.key];
        if (!kpi || kpi.count === 0) return '';
        const val = kpi.avg != null ? (i.unit === '%' ? kpi.avg.toFixed(2) + '%' : Number(kpi.avg).toLocaleString()) : '—';
        return `<tr><td><strong>${i.key.replace('_', '.')}</strong></td><td>${(i.label.split('–')[1] || i.label).trim()}</td><td>${i.unit}</td><td>${val}</td><td>${kpi.count} colleges</td></tr>`;
    }).join('');

    let uodSection = '', researchSection = '', cgsSection = '';

    if (data.uodInd_6_1 && data.uodInd_6_1.avg != null) {
        uodSection = `<div class="card"><div class="card-header"><h2>UOD Submission – ${month}</h2></div><div class="card-body table-wrap"><table class="data-table"><thead><tr><th>KPI</th><th>Value</th></tr></thead><tbody>
            <tr><td><strong>6.1</strong> – Academic Events</td><td>${Number(data.uodInd_6_1.avg).toLocaleString()}</td></tr>
            <tr><td><strong>6.2</strong> – Community Events</td><td>${Number(data.uodInd_6_2?.avg || 0).toLocaleString()}</td></tr>
        </tbody></table></div></div>`;
    }

    if (data.researchInd_4_4 && data.researchInd_4_4.avg != null) {
        researchSection = `<div class="card"><div class="card-header"><h2>Research Office – ${month}</h2></div><div class="card-body table-wrap"><table class="data-table"><thead><tr><th>KPI</th><th>Value</th></tr></thead><tbody>
            <tr><td><strong>4.4</strong> – Student Participation in Research</td><td>${data.researchInd_4_4.avg.toFixed(2)}%</td></tr>
            <tr><td><strong>4.5</strong> – Research Impact</td><td>${Number(data.researchInd_4_5?.avg || 0).toLocaleString()}</td></tr>
        </tbody></table></div></div>`;
    }

    if (data.ind_5_3 && data.ind_5_3.avg != null) {
        cgsSection = `<div class="card"><div class="card-header"><h2>CGS Submission – ${month}</h2></div><div class="card-body table-wrap"><table class="data-table"><tbody>
            <tr><td><strong>5.3</strong> – International Dual/Joint Degrees</td><td>${data.ind_5_3.avg.toFixed(2)}%</td></tr>
        </tbody></table></div></div>`;
    }

    document.getElementById('consolidatedContent').innerHTML = `
        <div class="card">
            <div class="card-header"><h2>College Averages – ${month}</h2><span class="badge badge-info">${data.collegeCount} colleges</span></div>
            <div class="card-body table-wrap">
                <table class="data-table"><thead><tr><th>KPI</th><th>Indicator</th><th>Type</th><th>Avg / Value</th><th>Count</th></tr></thead>
                <tbody>${collegeRows || '<tr><td colspan="5" class="empty-row">No college data for this month.</td></tr>'}</tbody></table>
            </div>
        </div>
        ${uodSection}${researchSection}${cgsSection}`;
}

// ==================== MODAL ====================
function openAddModal() {
    const ym = currentYm();
    const existing = allRecords.find(r => r.submissionMonth === ym);
    if (existing) { openEditModal(existing.id); return; }
    document.getElementById('recordId').value = '';
    document.getElementById('modalTitle').textContent = 'New Submission';
    document.getElementById('auditInfo').textContent = '';
    setMonthDisplay(ym);
    buildForm(null);
    showModal();
}

function openEditModal(id) {
    const rec = allRecords.find(r => r.id === id);
    if (!rec) return;
    document.getElementById('recordId').value = id;
    document.getElementById('modalTitle').textContent = `Edit – ${rec.submissionMonth}`;
    setMonthDisplay(rec.submissionMonth);
    buildForm(rec);
    const who = rec.updatedBy || rec.createdBy || '';
    const when = fmtDate(rec.updatedAt || rec.createdAt);
    document.getElementById('auditInfo').textContent = who ? `Last saved by ${who} on ${when}` : '';
    showModal();
}

function buildForm(rec) {
    const inds = myIndicators();
    document.getElementById('indicatorGroups').innerHTML = inds.map(i => {
        const isPct = i.unit === '%';
        const d = rec ? (rec['ind_' + i.key + '_D'] ?? '') : '';
        const n = rec ? (rec['ind_' + i.key + '_N'] ?? '') : '';
        const mech = rec ? (rec['ind_' + i.key + '_Mech'] || '') : '';
        return `<div class="ind-group">
            <div class="ind-title">${i.label} <span class="ind-badge">${i.unit}</span></div>
            ${i.desc ? `<div class="ind-desc">${i.desc}</div>` : ''}
            <div class="ind-row">
                <div class="form-group">
                    <label>${isPct ? 'Denominator (Total)' : 'Value / Count'}</label>
                    <input type="number" step="any" min="0" id="ind_${i.key}_D" value="${d}" placeholder="0">
                </div>
                ${isPct ? `<div class="form-group">
                    <label>Numerator</label>
                    <input type="number" step="any" min="0" id="ind_${i.key}_N" value="${n}" placeholder="0">
                </div>` : ''}
            </div>
            <div class="mech-group">
                <label>Mechanism of Collecting the Data</label>
                <textarea id="ind_${i.key}_Mech" maxlength="255" rows="2" placeholder="Describe how this data is collected…">${escapeHtml(mech)}</textarea>
            </div>
        </div>`;
    }).join('');
}

function escapeHtml(str) {
    if (!str) return '';
    return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

function setMonthDisplay(ym) {
    document.getElementById('submissionMonth').value = ym;
    const d = new Date(ym + '-01');
    document.getElementById('submissionMonthDisplay').textContent =
        d.toLocaleDateString('en-GB', { month: 'long', year: 'numeric' });
}

function showModal() {
    document.getElementById('formError').style.display = 'none';
    document.getElementById('modal').style.display = 'flex';
}
function closeModal() { document.getElementById('modal').style.display = 'none'; }

// ==================== SAVE ====================
document.getElementById('dataForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    const errEl = document.getElementById('formError');
    errEl.style.display = 'none';

    const id = document.getElementById('recordId').value;
    const inds = myIndicators();
    const body = { submissionMonth: document.getElementById('submissionMonth').value };

    for (const i of inds) {
        const dVal = document.getElementById('ind_' + i.key + '_D')?.value;
        body['ind_' + i.key + '_D'] = dVal !== '' && dVal != null ? parseFloat(dVal) : null;
        if (i.unit === '%') {
            const nVal = document.getElementById('ind_' + i.key + '_N')?.value;
            body['ind_' + i.key + '_N'] = nVal !== '' && nVal != null ? parseFloat(nVal) : null;
        }
        const mechEl = document.getElementById('ind_' + i.key + '_Mech');
        if (mechEl) {
            body['ind_' + i.key + '_Mech'] = mechEl.value.trim() || null;
        }
    }

    try {
        const res = await apiFetch(id ? `${API}/data/${id}` : `${API}/data`, {
            method: id ? 'PUT' : 'POST',
            body: JSON.stringify(body)
        });

        let rb = {};
        try { rb = await res.json(); } catch { }

        if (!res.ok) {
            errEl.textContent = rb.message || `Error ${res.status}`;
            errEl.style.display = 'block';
            return;
        }
        closeModal();
        await loadRecords();
        if (currentUser.role === 'admin') loadUsers();
    } catch {
        errEl.textContent = 'Network error. Check connection and try again.';
        errEl.style.display = 'block';
    }
});

// ==================== DELETE ====================
function openDeleteModal(id) {
    pendingDeleteId = id;
    const rec = allRecords.find(r => r.id === id);
    document.getElementById('deleteRecordLabel').textContent = rec ? `#${id} (${rec.submissionMonth})` : `#${id}`;
    document.getElementById('deleteModal').style.display = 'flex';
}
function closeDeleteModal() { document.getElementById('deleteModal').style.display = 'none'; pendingDeleteId = null; }

async function confirmDelete() {
    if (!pendingDeleteId) return;
    try {
        const res = await apiFetch(`${API}/data/${pendingDeleteId}`, { method: 'DELETE' });
        closeDeleteModal();
        if (res.ok) await loadRecords();
    } catch { console.error('Delete failed'); }
}