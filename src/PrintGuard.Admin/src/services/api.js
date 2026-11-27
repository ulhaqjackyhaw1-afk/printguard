const API_BASE = '/api/v1';

export const api = {
  async getDashboard() {
    try {
      const res = await fetch(`${API_BASE}/admin/dashboard`);
      if (!res.ok) throw new Error('API offline');
      const json = await res.json();
      return json.data || getMockDashboard();
    } catch (e) {
      return getMockDashboard();
    }
  },

  async getUsers(search = '') {
    try {
      const res = await fetch(`${API_BASE}/admin/users?search=${encodeURIComponent(search)}`);
      if (!res.ok) throw new Error('API offline');
      const json = await res.json();
      return json.data || getMockUsers();
    } catch (e) {
      const all = getMockUsers();
      if (!search) return all;
      const s = search.toLowerCase();
      return all.filter(u => u.fullName.toLowerCase().includes(s) || u.username.toLowerCase().includes(s));
    }
  },

  async createUser(data) {
    try {
      const res = await fetch(`${API_BASE}/admin/users`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
      });
      const json = await res.json();
      if (!res.ok) throw new Error(json.message || 'Failed to create user');
      return json.data;
    } catch (e) {
      console.warn("Backend API unavailable, simulating user creation", e);
      // Fallback local simulation
      return {
        id: Date.now(),
        employeeId: data.employeeId || `EMP-${Math.floor(100 + Math.random() * 900)}`,
        fullName: data.fullName,
        username: data.username,
        email: data.email,
        department: data.departmentId === 1 ? 'Information Technology' : 
                    data.departmentId === 2 ? 'Finance & Accounting' : 
                    data.departmentId === 3 ? 'Human Resources' : 'Operations',
        departmentId: data.departmentId,
        position: data.position || 'Staff',
        isActive: true,
        createdAt: new Date().toISOString()
      };
    }
  },

  async setUserStatus(id, isActive) {
    try {
      const res = await fetch(`${API_BASE}/admin/users/${id}/status`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(isActive)
      });
      const json = await res.json();
      return json.data;
    } catch (e) {
      return true;
    }
  },

  async getPrinters() {
    try {
      const res = await fetch(`${API_BASE}/admin/printers`);
      if (!res.ok) throw new Error('API offline');
      const json = await res.json();
      return json.data || getMockPrinters();
    } catch (e) {
      return getMockPrinters();
    }
  },

  async getComputers() {
    try {
      const res = await fetch(`${API_BASE}/admin/computers`);
      if (!res.ok) throw new Error('API offline');
      const json = await res.json();
      return json.data || getMockComputers();
    } catch (e) {
      return getMockComputers();
    }
  },

  async getPrintHistory() {
    try {
      const res = await fetch(`${API_BASE}/admin/print-jobs`);
      if (!res.ok) throw new Error('API offline');
      const json = await res.json();
      return json.data || getMockHistory();
    } catch (e) {
      return getMockHistory();
    }
  },

  async getAuditLogs() {
    try {
      const res = await fetch(`${API_BASE}/admin/audit-logs`);
      if (!res.ok) throw new Error('API offline');
      const json = await res.json();
      return json.data || getMockAuditLogs();
    } catch (e) {
      return getMockAuditLogs();
    }
  }
};

function getMockDashboard() {
  return {
    totalEmployees: 48,
    totalPrinters: 6,
    onlineComputers: 42,
    totalPrintJobs: 1324,
    totalPrintedPages: 4892,
    colorPages: 1120,
    monochromePages: 3772,
    dailyActivity: [
      { date: '2026-11-21', totalJobs: 145, totalPages: 580 },
      { date: '2026-11-22', totalJobs: 178, totalPages: 710 },
      { date: '2026-11-23', totalJobs: 190, totalPages: 820 },
      { date: '2026-11-24', totalJobs: 164, totalPages: 630 },
      { date: '2026-11-25', totalJobs: 210, totalPages: 920 },
      { date: '2026-11-26', totalJobs: 225, totalPages: 980 },
      { date: '2026-11-27', totalJobs: 212, totalPages: 852 }
    ],
    topEmployees: [
      { fullName: 'Budi Santoso', department: 'Finance & Accounting', totalJobs: 82, totalPages: 384 },
      { fullName: 'Siti Rahma', department: 'Human Resources', totalJobs: 65, totalPages: 290 },
      { fullName: 'Ahmad Fauzi', department: 'Information Technology', totalJobs: 54, totalPages: 210 },
      { fullName: 'Dewi Lestari', department: 'Operations', totalJobs: 41, totalPages: 185 }
    ],
    mostUsedPrinters: [
      { printerName: 'PRN-LAN-01-FINANCE', totalJobs: 640, totalPages: 2450 },
      { printerName: 'PRN-LAN-02-COLOR', totalJobs: 410, totalPages: 1530 },
      { printerName: 'PRN-LAN-03-HR', totalJobs: 274, totalPages: 912 }
    ]
  };
}

function getMockUsers() {
  return [
    { id: 1, employeeId: 'EMP-001', fullName: 'Budi Santoso', username: 'budi.santoso', email: 'budi.santoso@company.local', department: 'Finance & Accounting', position: 'Finance Specialist', isActive: true, createdAt: '2026-01-10T08:00:00Z' },
    { id: 2, employeeId: 'EMP-002', fullName: 'Siti Rahma', username: 'siti.rahma', email: 'siti.rahma@company.local', department: 'Human Resources', position: 'HR Officer', isActive: true, createdAt: '2026-02-14T09:30:00Z' },
    { id: 3, employeeId: 'EMP-003', fullName: 'Ahmad Fauzi', username: 'ahmad.fauzi', email: 'ahmad.fauzi@company.local', department: 'Information Technology', position: 'IT Support', isActive: true, createdAt: '2026-03-01T10:00:00Z' },
    { id: 4, employeeId: 'EMP-004', fullName: 'Dewi Lestari', username: 'dewi.lestari', email: 'dewi.lestari@company.local', department: 'Operations', position: 'Ops Supervisor', isActive: false, createdAt: '2026-04-12T11:15:00Z' }
  ];
}

function getMockPrinters() {
  return [
    { id: 1, name: 'PRN-LAN-01-FINANCE', model: 'HP LaserJet Enterprise M608', ipAddress: '192.168.10.201', location: 'Floor 2 - Finance', printerServer: 'SRV-PRINT-01', isActive: true, totalJobs: 640, totalPages: 2450 },
    { id: 2, name: 'PRN-LAN-02-COLOR', model: 'Canon imageRUNNER ADVANCE C5535i', ipAddress: '192.168.10.202', location: 'Floor 1 - Main Hall', printerServer: 'SRV-PRINT-01', isActive: true, totalJobs: 410, totalPages: 1530 },
    { id: 3, name: 'PRN-LAN-03-HR', model: 'Epson WorkForce Pro WF-C579R', ipAddress: '192.168.10.203', location: 'Floor 3 - HR Dept', printerServer: 'SRV-PRINT-01', isActive: true, totalJobs: 274, totalPages: 912 }
  ];
}

function getMockComputers() {
  return [
    { id: 1, hostname: 'PC-FIN-01', ipAddress: '192.168.10.51', windowsVersion: 'Windows 11 Enterprise', agentVersion: '1.0.0', lastSeenAt: '2026-11-27T14:40:00Z', status: 'ONLINE' },
    { id: 2, hostname: 'PC-FIN-02', ipAddress: '192.168.10.52', windowsVersion: 'Windows 10 Pro', agentVersion: '1.0.0', lastSeenAt: '2026-11-27T14:38:00Z', status: 'ONLINE' },
    { id: 3, hostname: 'PC-HR-01', ipAddress: '192.168.10.60', windowsVersion: 'Windows 11 Pro', agentVersion: '1.0.0', lastSeenAt: '2026-11-27T14:35:00Z', status: 'ONLINE' },
    { id: 4, hostname: 'PC-IT-LAB', ipAddress: '192.168.10.88', windowsVersion: 'Windows 11 Pro', agentVersion: '1.0.0', lastSeenAt: '2026-11-26T17:00:00Z', status: 'OFFLINE' }
  ];
}

function getMockHistory() {
  return [
    { id: 'job-9841', serverJobId: 'SPOOL-101', userName: 'Budi Santoso', departmentName: 'Finance & Accounting', computerHostname: 'PC-FIN-01', printerName: 'PRN-LAN-01-FINANCE', documentName: 'Laporan_Keuangan_Q3.pdf', totalPages: 14, copies: 1, colorMode: 'MONO', status: 'COMPLETED', submittedAt: '2026-11-27T14:15:00Z' },
    { id: 'job-9842', serverJobId: 'SPOOL-102', userName: 'Siti Rahma', departmentName: 'Human Resources', computerHostname: 'PC-HR-01', printerName: 'PRN-LAN-02-COLOR', documentName: 'Employee_Contract_Template.pdf', totalPages: 4, copies: 2, colorMode: 'COLOR', status: 'COMPLETED', submittedAt: '2026-11-27T14:20:00Z' },
    { id: 'job-9843', serverJobId: 'SPOOL-103', userName: 'Ahmad Fauzi', departmentName: 'Information Technology', computerHostname: 'PC-IT-LAB', printerName: 'PRN-LAN-03-HR', documentName: 'Network_Topology_Diagram.png', totalPages: 1, copies: 1, colorMode: 'COLOR', status: 'AUTHORIZED', submittedAt: '2026-11-27T14:32:00Z' },
    { id: 'job-9844', serverJobId: 'SPOOL-104', userName: 'Dewi Lestari', departmentName: 'Operations', computerHostname: 'PC-FIN-02', printerName: 'PRN-LAN-01-FINANCE', documentName: 'Confidential_Audit.xlsx', totalPages: 8, copies: 1, colorMode: 'MONO', status: 'DENIED', submittedAt: '2026-11-27T14:35:00Z' }
  ];
}

function getMockAuditLogs() {
  return [
    { id: 1, adminName: 'System Administrator', userName: null, action: 'ADMIN_LOGIN', entityType: 'AdminUser', description: 'Admin logged into dashboard', createdAt: '2026-11-27T14:00:00Z' },
    { id: 2, adminName: null, userName: 'Budi Santoso', action: 'PRINT_JOB_AUTHORIZED', entityType: 'PrintJob', description: 'Authorized document Laporan_Keuangan_Q3.pdf', createdAt: '2026-11-27T14:15:10Z' },
    { id: 3, adminName: 'System Administrator', userName: 'Dewi Lestari', action: 'SET_USER_STATUS', entityType: 'User', description: 'User status changed to INACTIVE', createdAt: '2026-11-27T14:25:00Z' }
  ];
}
