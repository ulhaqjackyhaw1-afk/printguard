import React, { useState, useEffect } from 'react';
import { 
  Printer, Users, Monitor, FileText, CheckCircle2, XCircle, 
  Clock, ShieldAlert, BarChart3, Settings, LogOut, Search,
  Download, Plus, RefreshCw, AlertTriangle
} from 'lucide-react';
import { api } from './services/api';

export default function App() {
  const [activeTab, setActiveTab] = useState('dashboard');
  const [dashboardData, setDashboardData] = useState(null);
  const [users, setUsers] = useState([]);
  const [printers, setPrinters] = useState([]);
  const [computers, setComputers] = useState([]);
  const [printJobs, setPrintJobs] = useState([]);
  const [auditLogs, setAuditLogs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');

  // Load initial data
  useEffect(() => {
    loadData();
  }, [activeTab]);

  const loadData = async () => {
    setLoading(true);
    try {
      if (activeTab === 'dashboard') {
        const d = await api.getDashboard();
        setDashboardData(d);
      } else if (activeTab === 'users') {
        const u = await api.getUsers(searchQuery);
        setUsers(u);
      } else if (activeTab === 'printers') {
        const p = await api.getPrinters();
        setPrinters(p);
      } else if (activeTab === 'computers') {
        const c = await api.getComputers();
        setComputers(c);
      } else if (activeTab === 'history') {
        const j = await api.getPrintHistory();
        setPrintJobs(j);
      } else if (activeTab === 'audit') {
        const a = await api.getAuditLogs();
        setAuditLogs(a);
      }
    } finally {
      setLoading(false);
    }
  };

  const exportHistoryCSV = () => {
    const headers = ["Job ID", "Date", "Employee", "Department", "Computer", "Document", "Printer", "Pages", "Copies", "Color Mode", "Status"];
    const rows = printJobs.map(j => [
      j.serverJobId,
      new Date(j.submittedAt).toLocaleString(),
      j.userName,
      j.departmentName,
      j.computerHostname,
      `"${j.documentName.replace(/"/g, '""')}"`,
      j.printerName,
      j.totalPages,
      j.copies,
      j.colorMode,
      j.status
    ]);
    const csvContent = "data:text/csv;charset=utf-8," + [headers.join(','), ...rows.map(e => e.join(','))].join('\n');
    const encodedUri = encodeURI(csvContent);
    const link = document.createElement("a");
    link.setAttribute("href", encodedUri);
    link.setAttribute("download", `printguard-history-${new Date().toISOString().slice(0,10)}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  return (
    <div className="flex h-screen bg-[#F8FAFC]">
      {/* Sidebar Navigation */}
      <aside className="w-64 bg-white border-r border-slate-200 flex flex-col justify-between">
        <div>
          {/* Logo */}
          <div className="h-16 flex items-center px-6 border-b border-slate-100 gap-3">
            <div className="w-9 h-9 rounded-lg bg-blue-600 flex items-center justify-center text-white shadow-sm shadow-blue-500/30">
              <Printer className="w-5 h-5" />
            </div>
            <div>
              <h1 className="font-bold text-slate-900 text-lg leading-tight">PrintGuard</h1>
              <span className="text-[11px] font-medium text-blue-600 bg-blue-50 px-1.5 py-0.5 rounded">Central Admin</span>
            </div>
          </div>

          {/* Navigation Items */}
          <nav className="p-4 space-y-1">
            <NavItem 
              icon={<BarChart3 className="w-4 h-4"/>} 
              label="Overview" 
              active={activeTab === 'dashboard'} 
              onClick={() => setActiveTab('dashboard')} 
            />
            <NavItem 
              icon={<Users className="w-4 h-4"/>} 
              label="Employees" 
              active={activeTab === 'users'} 
              onClick={() => setActiveTab('users')} 
            />
            <NavItem 
              icon={<Printer className="w-4 h-4"/>} 
              label="Printers" 
              active={activeTab === 'printers'} 
              onClick={() => setActiveTab('printers')} 
            />
            <NavItem 
              icon={<Monitor className="w-4 h-4"/>} 
              label="Computers" 
              active={activeTab === 'computers'} 
              onClick={() => setActiveTab('computers')} 
            />
            <NavItem 
              icon={<FileText className="w-4 h-4"/>} 
              label="Print History" 
              active={activeTab === 'history'} 
              onClick={() => setActiveTab('history')} 
            />
            <NavItem 
              icon={<ShieldAlert className="w-4 h-4"/>} 
              label="Audit Logs" 
              active={activeTab === 'audit'} 
              onClick={() => setActiveTab('audit')} 
            />
          </nav>
        </div>

        {/* User Footer */}
        <div className="p-4 border-t border-slate-100 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-full bg-slate-200 text-slate-700 font-bold flex items-center justify-center text-xs">
              AD
            </div>
            <div>
              <p className="text-xs font-semibold text-slate-800">Admin IT</p>
              <p className="text-[10px] text-slate-400">admin@company.local</p>
            </div>
          </div>
          <button className="text-slate-400 hover:text-red-500 p-1.5 rounded-md hover:bg-slate-50 transition">
            <LogOut className="w-4 h-4" />
          </button>
        </div>
      </aside>

      {/* Main Content Area */}
      <main className="flex-1 flex flex-col overflow-hidden">
        {/* Top Header */}
        <header className="h-16 bg-white border-b border-slate-200 flex items-center justify-between px-8">
          <div>
            <h2 className="text-xl font-bold text-slate-800 capitalize">
              {activeTab === 'dashboard' ? 'Dashboard Overview' : activeTab}
            </h2>
            <p className="text-xs text-slate-500">Print Management, Employee Authentication & Monitoring System</p>
          </div>

          <div className="flex items-center gap-3">
            <button 
              onClick={loadData}
              className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-lg transition"
            >
              <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
              Refresh
            </button>
          </div>
        </header>

        {/* Tab Content */}
        <div className="flex-1 overflow-y-auto p-8">
          {activeTab === 'dashboard' && dashboardData && (
            <div className="space-y-6">
              {/* Summary Cards */}
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                <StatCard 
                  title="Total Employees" 
                  value={dashboardData.totalEmployees} 
                  subtext="Registered in company" 
                  icon={<Users className="w-5 h-5 text-blue-600" />} 
                  bg="bg-blue-50"
                />
                <StatCard 
                  title="Active Printers" 
                  value={dashboardData.totalPrinters} 
                  subtext="Centralized network spoolers" 
                  icon={<Printer className="w-5 h-5 text-indigo-600" />} 
                  bg="bg-indigo-50"
                />
                <StatCard 
                  title="Online Computers" 
                  value={dashboardData.onlineComputers} 
                  subtext="Heartbeat active (5m)" 
                  icon={<Monitor className="w-5 h-5 text-emerald-600" />} 
                  bg="bg-emerald-50"
                />
                <StatCard 
                  title="Total Printed Pages" 
                  value={dashboardData.totalPrintedPages.toLocaleString()} 
                  subtext={`${dashboardData.colorPages} Color / ${dashboardData.monochromePages} Mono`} 
                  icon={<FileText className="w-5 h-5 text-amber-600" />} 
                  bg="bg-amber-50"
                />
              </div>

              {/* Analytics & Ranking Grid */}
              <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                {/* Daily Activity Chart Card */}
                <div className="lg:col-span-2 bg-white p-6 rounded-card border border-slate-200 shadow-sm">
                  <div className="flex justify-between items-center mb-6">
                    <div>
                      <h3 className="font-bold text-slate-800 text-sm">Printing Activity (Last 7 Days)</h3>
                      <p className="text-xs text-slate-400">Total volume of print jobs released</p>
                    </div>
                    <span className="text-xs font-semibold px-2.5 py-1 bg-blue-50 text-blue-600 rounded-full">Weekly Trend</span>
                  </div>

                  <div className="h-48 flex items-end gap-4 pt-4 border-b border-slate-100">
                    {dashboardData.dailyActivity.map((d, i) => (
                      <div key={i} className="flex-1 flex flex-col items-center gap-2 group">
                        <span className="text-[10px] text-slate-400 opacity-0 group-hover:opacity-100 transition font-medium">{d.totalPages}p</span>
                        <div 
                          className="w-full bg-blue-500 group-hover:bg-blue-600 rounded-t transition"
                          style={{ height: `${(d.totalPages / 1100) * 140}px` }}
                        ></div>
                        <span className="text-[10px] text-slate-500 font-medium">{d.date.slice(5)}</span>
                      </div>
                    ))}
                  </div>
                </div>

                {/* Color vs Mono Distribution */}
                <div className="bg-white p-6 rounded-card border border-slate-200 shadow-sm flex flex-col justify-between">
                  <div>
                    <h3 className="font-bold text-slate-800 text-sm mb-1">Color vs Monochrome</h3>
                    <p className="text-xs text-slate-400 mb-6">Ratio of physical toner consumption</p>

                    <div className="space-y-4">
                      <div>
                        <div className="flex justify-between text-xs font-medium mb-1">
                          <span className="text-slate-600">Black & White (Mono)</span>
                          <span className="font-bold text-slate-900">{Math.round((dashboardData.monochromePages / dashboardData.totalPrintedPages) * 100)}%</span>
                        </div>
                        <div className="w-full h-2.5 bg-slate-100 rounded-full overflow-hidden">
                          <div className="bg-slate-700 h-full rounded-full" style={{ width: `${(dashboardData.monochromePages / dashboardData.totalPrintedPages) * 100}%` }}></div>
                        </div>
                      </div>

                      <div>
                        <div className="flex justify-between text-xs font-medium mb-1">
                          <span className="text-slate-600">Color Printing</span>
                          <span className="font-bold text-slate-900">{Math.round((dashboardData.colorPages / dashboardData.totalPrintedPages) * 100)}%</span>
                        </div>
                        <div className="w-full h-2.5 bg-slate-100 rounded-full overflow-hidden">
                          <div className="bg-blue-500 h-full rounded-full" style={{ width: `${(dashboardData.colorPages / dashboardData.totalPrintedPages) * 100}%` }}></div>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div className="p-3 bg-blue-50/50 rounded-lg border border-blue-100 text-[11px] text-blue-700 mt-6">
                    💡 Tip: Requiring authorization on color printers reduces non-essential printing costs by ~34%.
                  </div>
                </div>
              </div>

              {/* Leaderboards */}
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                {/* Top Employees */}
                <div className="bg-white p-6 rounded-card border border-slate-200 shadow-sm">
                  <h3 className="font-bold text-slate-800 text-sm mb-4">Top Printing Employees</h3>
                  <div className="space-y-3">
                    {dashboardData.topEmployees.map((emp, i) => (
                      <div key={i} className="flex items-center justify-between p-3 rounded-lg bg-slate-50 border border-slate-100">
                        <div className="flex items-center gap-3">
                          <div className="w-7 h-7 rounded-full bg-blue-100 text-blue-600 font-bold flex items-center justify-center text-xs">
                            {i + 1}
                          </div>
                          <div>
                            <p className="text-xs font-bold text-slate-800">{emp.fullName}</p>
                            <p className="text-[10px] text-slate-500">{emp.department}</p>
                          </div>
                        </div>
                        <div className="text-right">
                          <p className="text-xs font-bold text-slate-900">{emp.totalPages} pages</p>
                          <p className="text-[10px] text-slate-400">{emp.totalJobs} jobs</p>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>

                {/* Most Used Printers */}
                <div className="bg-white p-6 rounded-card border border-slate-200 shadow-sm">
                  <h3 className="font-bold text-slate-800 text-sm mb-4">Most Used Printers</h3>
                  <div className="space-y-3">
                    {dashboardData.mostUsedPrinters.map((prn, i) => (
                      <div key={i} className="flex items-center justify-between p-3 rounded-lg bg-slate-50 border border-slate-100">
                        <div className="flex items-center gap-3">
                          <Printer className="w-5 h-5 text-slate-500" />
                          <div>
                            <p className="text-xs font-bold text-slate-800">{prn.printerName}</p>
                            <p className="text-[10px] text-emerald-600 font-medium">Status: Normal</p>
                          </div>
                        </div>
                        <div className="text-right">
                          <p className="text-xs font-bold text-slate-900">{prn.totalPages} pages</p>
                          <p className="text-[10px] text-slate-400">{prn.totalJobs} total jobs</p>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Employees Tab */}
          {activeTab === 'users' && (
            <div className="bg-white rounded-card border border-slate-200 shadow-sm">
              <div className="p-5 border-b border-slate-100 flex items-center justify-between">
                <div className="relative w-72">
                  <Search className="w-4 h-4 text-slate-400 absolute left-3 top-2.5" />
                  <input 
                    type="text" 
                    placeholder="Search employee name or username..." 
                    className="w-full pl-9 pr-4 py-2 text-xs border border-slate-200 rounded-lg focus:outline-none focus:border-blue-500"
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                  />
                </div>
                <button className="flex items-center gap-1.5 px-3 py-2 text-xs font-semibold text-white bg-blue-600 hover:bg-blue-700 rounded-lg shadow-sm transition">
                  <Plus className="w-3.5 h-3.5" />
                  Add Employee
                </button>
              </div>

              <table className="w-full text-left text-xs">
                <thead className="bg-slate-50 border-b border-slate-100 text-slate-500 uppercase font-semibold">
                  <tr>
                    <th className="px-6 py-3">Employee</th>
                    <th className="px-6 py-3">Username</th>
                    <th className="px-6 py-3">Department</th>
                    <th className="px-6 py-3">Position</th>
                    <th className="px-6 py-3">Status</th>
                    <th className="px-6 py-3 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {users.map((u) => (
                    <tr key={u.id} className="hover:bg-slate-50/50">
                      <td className="px-6 py-3.5">
                        <div className="font-semibold text-slate-800">{u.fullName}</div>
                        <div className="text-[10px] text-slate-400">{u.employeeId || 'No ID'} • {u.email}</div>
                      </td>
                      <td className="px-6 py-3.5 font-mono text-slate-600">{u.username}</td>
                      <td className="px-6 py-3.5 text-slate-700">{u.department || 'Unassigned'}</td>
                      <td className="px-6 py-3.5 text-slate-500">{u.position || '-'}</td>
                      <td className="px-6 py-3.5">
                        <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold ${u.isActive ? 'bg-emerald-50 text-emerald-600' : 'bg-red-50 text-red-600'}`}>
                          {u.isActive ? 'ACTIVE' : 'INACTIVE'}
                        </span>
                      </td>
                      <td className="px-6 py-3.5 text-right space-x-2">
                        <button className="text-blue-600 hover:underline font-medium">Edit</button>
                        <button className="text-slate-500 hover:underline">Reset Pwd</button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* Printers Tab */}
          {activeTab === 'printers' && (
            <div className="bg-white rounded-card border border-slate-200 shadow-sm">
              <div className="p-5 border-b border-slate-100 flex items-center justify-between">
                <h3 className="font-bold text-slate-800 text-sm">Managed Central Printers</h3>
                <button className="flex items-center gap-1.5 px-3 py-2 text-xs font-semibold text-white bg-blue-600 hover:bg-blue-700 rounded-lg shadow-sm transition">
                  <Plus className="w-3.5 h-3.5" />
                  Add Network Printer
                </button>
              </div>

              <table className="w-full text-left text-xs">
                <thead className="bg-slate-50 border-b border-slate-100 text-slate-500 uppercase font-semibold">
                  <tr>
                    <th className="px-6 py-3">Printer Name</th>
                    <th className="px-6 py-3">Model</th>
                    <th className="px-6 py-3">IP Address</th>
                    <th className="px-6 py-3">Location</th>
                    <th className="px-6 py-3">Print Server</th>
                    <th className="px-6 py-3">Volume</th>
                    <th className="px-6 py-3">Status</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {printers.map((p) => (
                    <tr key={p.id} className="hover:bg-slate-50/50">
                      <td className="px-6 py-3.5 font-bold text-slate-800 flex items-center gap-2">
                        <Printer className="w-4 h-4 text-blue-600" />
                        {p.name}
                      </td>
                      <td className="px-6 py-3.5 text-slate-600">{p.model}</td>
                      <td className="px-6 py-3.5 font-mono text-slate-700">{p.ipAddress}</td>
                      <td className="px-6 py-3.5 text-slate-500">{p.location}</td>
                      <td className="px-6 py-3.5 font-mono text-slate-500">{p.printerServer}</td>
                      <td className="px-6 py-3.5 text-slate-700">{p.totalPages} pages ({p.totalJobs} jobs)</td>
                      <td className="px-6 py-3.5">
                        <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-50 text-emerald-600">
                          ONLINE
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* Computers Tab */}
          {activeTab === 'computers' && (
            <div className="bg-white rounded-card border border-slate-200 shadow-sm">
              <div className="p-5 border-b border-slate-100 flex items-center justify-between">
                <div>
                  <h3 className="font-bold text-slate-800 text-sm">Registered Client PCs</h3>
                  <p className="text-xs text-slate-400">Endpoints equipped with PrintGuard.Service & UI</p>
                </div>
              </div>

              <table className="w-full text-left text-xs">
                <thead className="bg-slate-50 border-b border-slate-100 text-slate-500 uppercase font-semibold">
                  <tr>
                    <th className="px-6 py-3">Hostname</th>
                    <th className="px-6 py-3">IP Address</th>
                    <th className="px-6 py-3">OS Version</th>
                    <th className="px-6 py-3">Agent</th>
                    <th className="px-6 py-3">Last Seen</th>
                    <th className="px-6 py-3">Status</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {computers.map((c) => (
                    <tr key={c.id} className="hover:bg-slate-50/50">
                      <td className="px-6 py-3.5 font-bold text-slate-800 flex items-center gap-2">
                        <Monitor className="w-4 h-4 text-slate-500" />
                        {c.hostname}
                      </td>
                      <td className="px-6 py-3.5 font-mono text-slate-600">{c.ipAddress}</td>
                      <td className="px-6 py-3.5 text-slate-500">{c.windowsVersion}</td>
                      <td className="px-6 py-3.5 text-slate-600">v{c.agentVersion}</td>
                      <td className="px-6 py-3.5 text-slate-500">{new Date(c.lastSeenAt).toLocaleString()}</td>
                      <td className="px-6 py-3.5">
                        <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold ${c.status === 'ONLINE' ? 'bg-emerald-50 text-emerald-600' : 'bg-slate-100 text-slate-600'}`}>
                          {c.status}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* Print History Tab */}
          {activeTab === 'history' && (
            <div className="bg-white rounded-card border border-slate-200 shadow-sm">
              <div className="p-5 border-b border-slate-100 flex items-center justify-between">
                <div>
                  <h3 className="font-bold text-slate-800 text-sm">Print Activity Logs</h3>
                  <p className="text-xs text-slate-400">Complete audit trail of all held and released documents</p>
                </div>
                <button 
                  onClick={exportHistoryCSV}
                  className="flex items-center gap-1.5 px-3 py-2 text-xs font-semibold text-slate-700 bg-slate-100 hover:bg-slate-200 rounded-lg transition"
                >
                  <Download className="w-3.5 h-3.5" />
                  Export to CSV
                </button>
              </div>

              <table className="w-full text-left text-xs">
                <thead className="bg-slate-50 border-b border-slate-100 text-slate-500 uppercase font-semibold">
                  <tr>
                    <th className="px-6 py-3">Job ID</th>
                    <th className="px-6 py-3">Time</th>
                    <th className="px-6 py-3">Employee</th>
                    <th className="px-6 py-3">Document</th>
                    <th className="px-6 py-3">Printer</th>
                    <th className="px-6 py-3">Pages</th>
                    <th className="px-6 py-3">Mode</th>
                    <th className="px-6 py-3">Status</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {printJobs.map((j) => (
                    <tr key={j.id} className="hover:bg-slate-50/50">
                      <td className="px-6 py-3.5 font-mono text-slate-600">{j.serverJobId}</td>
                      <td className="px-6 py-3.5 text-slate-500">{new Date(j.submittedAt).toLocaleTimeString()}</td>
                      <td className="px-6 py-3.5">
                        <div className="font-semibold text-slate-800">{j.userName || 'Unauthenticated'}</div>
                        <div className="text-[10px] text-slate-400">{j.departmentName || j.computerHostname}</div>
                      </td>
                      <td className="px-6 py-3.5 font-medium text-slate-700 max-w-xs truncate" title={j.documentName}>
                        {j.documentName}
                      </td>
                      <td className="px-6 py-3.5 text-slate-600">{j.printerName}</td>
                      <td className="px-6 py-3.5 text-slate-700">{j.totalPages}p × {j.copies}</td>
                      <td className="px-6 py-3.5">
                        <span className={`px-2 py-0.5 rounded text-[10px] font-bold ${j.colorMode === 'COLOR' ? 'bg-purple-50 text-purple-600' : 'bg-slate-100 text-slate-600'}`}>
                          {j.colorMode}
                        </span>
                      </td>
                      <td className="px-6 py-3.5">
                        <StatusBadge status={j.status} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* Audit Logs Tab */}
          {activeTab === 'audit' && (
            <div className="bg-white rounded-card border border-slate-200 shadow-sm">
              <div className="p-5 border-b border-slate-100">
                <h3 className="font-bold text-slate-800 text-sm">System & Administrative Audit Logs</h3>
                <p className="text-xs text-slate-400">Security events and configuration modifications</p>
              </div>

              <div className="divide-y divide-slate-100">
                {auditLogs.map((log) => (
                  <div key={log.id} className="p-4 flex items-center justify-between hover:bg-slate-50/50 text-xs">
                    <div className="flex items-center gap-3">
                      <div className="w-8 h-8 rounded-full bg-slate-100 text-slate-600 flex items-center justify-center font-bold">
                        {log.adminName ? 'AD' : 'EM'}
                      </div>
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="font-bold text-slate-800">{log.action}</span>
                          <span className="text-slate-400">•</span>
                          <span className="text-slate-600">{log.description}</span>
                        </div>
                        <div className="text-[10px] text-slate-400 mt-0.5">
                          Actor: {log.adminName || log.userName || 'System'} • Target: {log.entityType} ({log.entityId || '-'})
                        </div>
                      </div>
                    </div>
                    <span className="text-slate-400 font-mono text-[11px]">{new Date(log.createdAt).toLocaleString()}</span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </main>
    </div>
  );
}

function NavItem({ icon, label, active, onClick }) {
  return (
    <button 
      onClick={onClick}
      className={`w-full flex items-center gap-3 px-3.5 py-2.5 rounded-lg text-xs font-semibold transition ${
        active 
          ? 'bg-blue-50 text-blue-600 shadow-sm' 
          : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900'
      }`}
    >
      {icon}
      {label}
    </button>
  );
}

function StatCard({ title, value, subtext, icon, bg }) {
  return (
    <div className="bg-white p-5 rounded-card border border-slate-200 shadow-sm flex items-center justify-between">
      <div>
        <p className="text-xs font-medium text-slate-500 mb-1">{title}</p>
        <p className="text-2xl font-bold text-slate-900 leading-tight">{value}</p>
        <p className="text-[11px] text-slate-400 mt-1">{subtext}</p>
      </div>
      <div className={`w-12 h-12 rounded-xl ${bg} flex items-center justify-center`}>
        {icon}
      </div>
    </div>
  );
}

function StatusBadge({ status }) {
  let style = "bg-slate-100 text-slate-700";
  if (status === 'COMPLETED') style = "bg-emerald-50 text-emerald-700 border border-emerald-200";
  if (status === 'AUTHORIZED') style = "bg-blue-50 text-blue-700 border border-blue-200";
  if (status === 'HELD') style = "bg-amber-50 text-amber-700 border border-amber-200";
  if (status === 'DENIED' || status === 'FAILED') style = "bg-red-50 text-red-700 border border-red-200";

  return (
    <span className={`px-2 py-0.5 rounded-md text-[10px] font-bold ${style}`}>
      {status}
    </span>
  );
}
