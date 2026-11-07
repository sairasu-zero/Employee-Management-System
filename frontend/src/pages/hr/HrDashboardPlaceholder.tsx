import React, { useState, useEffect, useCallback } from 'react';
import {
  Users,
  UserPlus,
  CalendarCheck,
  FileCheck,
  Plus,
  RefreshCw,
  Check,
  X,
  AlertCircle,
  CheckCircle2,
} from 'lucide-react';
import { MetricCard } from '../../components/ui/MetricCard';
import { DataTable } from '../../components/ui/DataTable';
import { StatusBadge } from '../../components/ui/StatusBadge';
import { Modal } from '../../components/ui/Modal';
import { FormInput } from '../../components/ui/FormInput';
import {
  apiGetEmployees,
  apiCreateEmployee,
  apiGetPendingLeaves,
  apiApproveLeave,
  apiRejectLeave,
  apiGetDepartments,
} from '../../services/api';
import { EmployeeRecord, LeaveRequestRecord, DepartmentRecord } from '../../types';

export const HrDashboardPlaceholder: React.FC = () => {
  const [employees, setEmployees] = useState<EmployeeRecord[]>([]);
  const [pendingLeaves, setPendingLeaves] = useState<LeaveRequestRecord[]>([]);
  const [departments, setDepartments] = useState<DepartmentRecord[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [activeTab, setActiveTab] = useState<'employees' | 'leaves'>('employees');

  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  // New Employee Modal
  const [isEmployeeModalOpen, setIsEmployeeModalOpen] = useState(false);
  const [newFirstName, setNewFirstName] = useState('');
  const [newLastName, setNewLastName] = useState('');
  const [newEmail, setNewEmail] = useState('');
  const [newDepartmentId, setNewDepartmentId] = useState('');
  const [newPosition, setNewPosition] = useState('');
  const [newEmploymentType, setNewEmploymentType] = useState('FullTime');
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Leave Action Modals
  const [approveModal, setApproveModal] = useState<LeaveRequestRecord | null>(null);
  const [rejectModal, setRejectModal] = useState<LeaveRequestRecord | null>(null);
  const [rejectReason, setRejectReason] = useState('');

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);
    try {
      const [empResult, leaveResult, deptResult] = await Promise.allSettled([
        apiGetEmployees({ pageSize: 50 }),
        apiGetPendingLeaves(),
        apiGetDepartments(),
      ]);

      if (empResult.status === 'fulfilled') {
        setEmployees(empResult.value.items || []);
      }
      if (leaveResult.status === 'fulfilled') {
        setPendingLeaves(leaveResult.value || []);
      }
      if (deptResult.status === 'fulfilled') {
        setDepartments(deptResult.value || []);
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to load HR dashboard records.';
      setErrorMessage(msg);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleCreateEmployee = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newFirstName.trim() || !newLastName.trim() || !newEmail.trim() || !newPosition.trim()) {
      setErrorMessage('Please complete all required employee fields.');
      return;
    }

    setIsSubmitting(true);
    setErrorMessage(null);
    try {
      await apiCreateEmployee({
        firstName: newFirstName.trim(),
        lastName: newLastName.trim(),
        email: newEmail.trim(),
        position: newPosition.trim(),
        employmentType: newEmploymentType,
        departmentId: newDepartmentId || undefined,
        hireDate: new Date().toISOString(),
        status: 'Active',
      });

      setSuccessMessage(`Successfully provisioned new employee: ${newFirstName} ${newLastName}.`);
      setIsEmployeeModalOpen(false);
      setNewFirstName('');
      setNewLastName('');
      setNewEmail('');
      setNewPosition('');
      await loadData();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to register employee.';
      setErrorMessage(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleApproveLeave = async () => {
    if (!approveModal) return;
    setIsSubmitting(true);
    try {
      await apiApproveLeave(approveModal.id);
      setSuccessMessage(`Approved leave for ${approveModal.employeeName}.`);
      setApproveModal(null);
      await loadData();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Approval failed.';
      setErrorMessage(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleRejectLeave = async () => {
    if (!rejectModal) return;
    if (!rejectReason.trim()) {
      setErrorMessage('Please specify an explanation for rejection.');
      return;
    }
    setIsSubmitting(true);
    try {
      await apiRejectLeave(rejectModal.id, rejectReason.trim());
      setSuccessMessage(`Declined leave for ${rejectModal.employeeName}.`);
      setRejectModal(null);
      setRejectReason('');
      await loadData();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Rejection failed.';
      setErrorMessage(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  const employeeColumns = [
    {
      key: 'name',
      header: 'Employee Name & ID',
      sortable: true,
      render: (row: EmployeeRecord) => (
        <div>
          <div style={{ fontWeight: 600, color: 'var(--text-main)' }}>
            {row.fullName || `${row.firstName} ${row.lastName}`}
          </div>
          <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>
            {row.employeeNumber}
          </div>
        </div>
      ),
    },
    {
      key: 'department',
      header: 'Department',
      sortable: true,
      render: (row: EmployeeRecord) => row.departmentName || 'Unassigned',
    },
    {
      key: 'position',
      header: 'Designation',
      sortable: true,
    },
    {
      key: 'hireDate',
      header: 'Joining Date',
      sortable: true,
      render: (row: EmployeeRecord) =>
        row.hireDate ? new Date(row.hireDate).toLocaleDateString() : 'N/A',
    },
    {
      key: 'status',
      header: 'Status',
      sortable: true,
      render: (row: EmployeeRecord) => {
        const s = (row.status || 'active').toLowerCase();
        const badgeStatus =
          s === 'active' ? 'success' : s === 'pending' ? 'pending' : 'neutral';
        return <StatusBadge status={badgeStatus} label={row.status || 'Active'} />;
      },
    },
  ];

  const leaveColumns = [
    {
      key: 'employeeName',
      header: 'Team Member',
      sortable: true,
      render: (row: LeaveRequestRecord) => (
        <div>
          <span style={{ fontWeight: 600, color: 'var(--text-main)', display: 'block' }}>
            {row.employeeName}
          </span>
          <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
            {row.employeeNumber} {row.departmentName ? `• ${row.departmentName}` : ''}
          </span>
        </div>
      ),
    },
    {
      key: 'leaveType',
      header: 'Leave Category',
      sortable: true,
    },
    {
      key: 'startDate',
      header: 'Period',
      render: (row: LeaveRequestRecord) => (
        <span style={{ fontSize: '0.8125rem' }}>
          {new Date(row.startDate).toLocaleDateString()} - {new Date(row.endDate).toLocaleDateString()}
        </span>
      ),
    },
    {
      key: 'days',
      header: 'Days',
      render: (row: LeaveRequestRecord) => `${row.days}d`,
    },
    {
      key: 'reason',
      header: 'Reason',
      render: (row: LeaveRequestRecord) => (
        <span style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)' }}>
          {row.reason}
        </span>
      ),
    },
  ];

  return (
    <div>
      {/* Alert Banners */}
      {errorMessage && (
        <div
          style={{
            padding: '0.75rem 1rem',
            marginBottom: '1rem',
            backgroundColor: 'rgba(239, 68, 68, 0.1)',
            border: '1px solid var(--status-danger)',
            borderRadius: 'var(--radius-md)',
            color: 'var(--status-danger)',
            display: 'flex',
            alignItems: 'center',
            gap: '0.5rem',
            fontSize: '0.875rem',
          }}
        >
          <AlertCircle size={16} />
          <span>{errorMessage}</span>
        </div>
      )}

      {successMessage && (
        <div
          style={{
            padding: '0.75rem 1rem',
            marginBottom: '1rem',
            backgroundColor: 'rgba(16, 185, 129, 0.1)',
            border: '1px solid var(--status-success)',
            borderRadius: 'var(--radius-md)',
            color: 'var(--status-success)',
            display: 'flex',
            alignItems: 'center',
            gap: '0.5rem',
            fontSize: '0.875rem',
          }}
        >
          <CheckCircle2 size={16} />
          <span>{successMessage}</span>
        </div>
      )}

      {/* Metrics Row */}
      <div className="metrics-grid">
        <MetricCard
          title="Active Headcount"
          value={employees.length.toString()}
          icon={Users}
          iconBg="var(--primary-light)"
          iconColor="var(--primary)"
          description="Registered enterprise workforce"
        />

        <MetricCard
          title="Pending Leave Review"
          value={pendingLeaves.length.toString()}
          icon={CalendarCheck}
          iconBg="#FFFBEB"
          iconColor="#D97706"
          description="Awaiting HR or manager action"
        />

        <MetricCard
          title="Active Departments"
          value={departments.length.toString()}
          icon={FileCheck}
          iconBg="#ECFDF5"
          iconColor="#059669"
          description="Corporate organizational divisions"
        />

        <MetricCard
          title="Compliance Verified"
          value="100%"
          icon={UserPlus}
          iconBg="#EFF6FF"
          iconColor="#2563EB"
          description="Identity Assurance Standards"
        />
      </div>

      {/* Navigation Tabs & Actions */}
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: '1rem',
        }}
      >
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button
            type="button"
            className={`btn ${activeTab === 'employees' ? 'btn-primary' : 'btn-secondary'} btn-sm`}
            onClick={() => setActiveTab('employees')}
          >
            <Users size={14} />
            <span>Employee Directory ({employees.length})</span>
          </button>
          <button
            type="button"
            className={`btn ${activeTab === 'leaves' ? 'btn-primary' : 'btn-secondary'} btn-sm`}
            onClick={() => setActiveTab('leaves')}
          >
            <CalendarCheck size={14} />
            <span>Pending Leave Requests ({pendingLeaves.length})</span>
          </button>
        </div>

        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button
            type="button"
            className="btn btn-secondary btn-sm"
            onClick={loadData}
            disabled={isLoading}
          >
            <RefreshCw size={14} className={isLoading ? 'spin' : ''} />
            <span>Refresh</span>
          </button>
          <button
            type="button"
            className="btn btn-primary btn-sm"
            onClick={() => setIsEmployeeModalOpen(true)}
          >
            <Plus size={14} />
            <span>Onboard Employee</span>
          </button>
        </div>
      </div>

      {/* Main Tab Content */}
      {activeTab === 'employees' ? (
        <DataTable<EmployeeRecord>
          title="Workforce Records Directory"
          subtitle="View and verify active personnel across organizational units"
          data={employees}
          columns={employeeColumns}
          keyExtractor={(row) => row.id}
          searchPlaceholder="Search employees by name, role, or ID..."
          searchKeys={['fullName', 'firstName', 'lastName', 'email', 'position', 'employeeNumber']}
        />
      ) : (
        <DataTable<LeaveRequestRecord>
          title="Organizational Leave Approvals"
          subtitle="Pending leave requests requiring Human Resources sign-off"
          data={pendingLeaves}
          columns={leaveColumns}
          keyExtractor={(row) => row.id}
          searchPlaceholder="Filter pending leaves..."
          searchKeys={['employeeName', 'leaveType', 'reason']}
          actions={(row) => (
            <div style={{ display: 'flex', gap: '0.4rem', justifyContent: 'flex-end' }}>
              <button
                type="button"
                className="btn btn-secondary btn-sm"
                style={{ color: 'var(--status-success)' }}
                onClick={() => setApproveModal(row)}
              >
                <Check size={14} />
                <span>Approve</span>
              </button>
              <button
                type="button"
                className="btn btn-ghost btn-sm"
                style={{ color: 'var(--status-danger)' }}
                onClick={() => {
                  setRejectModal(row);
                  setRejectReason('');
                }}
              >
                <X size={14} />
                <span>Decline</span>
              </button>
            </div>
          )}
        />
      )}

      {/* Onboard Employee Modal */}
      <Modal
        isOpen={isEmployeeModalOpen}
        onClose={() => setIsEmployeeModalOpen(false)}
        title="Onboard New Employee Profile"
        footer={
          <>
            <button
              type="button"
              className="btn btn-secondary"
              onClick={() => setIsEmployeeModalOpen(false)}
              disabled={isSubmitting}
            >
              Cancel
            </button>
            <button
              type="button"
              className="btn btn-primary"
              onClick={handleCreateEmployee}
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Registering...' : 'Complete Onboarding'}
            </button>
          </>
        }
      >
        <form onSubmit={handleCreateEmployee} style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
            <FormInput
              label="Legal First Name *"
              value={newFirstName}
              onChange={(e) => setNewFirstName(e.target.value)}
              placeholder="e.g. Maria"
              required
            />
            <FormInput
              label="Legal Last Name *"
              value={newLastName}
              onChange={(e) => setNewLastName(e.target.value)}
              placeholder="e.g. Santos"
              required
            />
          </div>

          <FormInput
            label="Corporate Work Email *"
            type="email"
            value={newEmail}
            onChange={(e) => setNewEmail(e.target.value)}
            placeholder="e.g. maria.santos@erms.local"
            required
          />

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
            <div className="form-group">
              <label className="form-label">Assigned Department</label>
              <select
                className="form-input"
                value={newDepartmentId}
                onChange={(e) => setNewDepartmentId(e.target.value)}
              >
                <option value="">Select Department...</option>
                {departments.map((d) => (
                  <option key={d.id} value={d.id}>
                    {d.name} ({d.code})
                  </option>
                ))}
              </select>
            </div>

            <FormInput
              label="Job Position Title *"
              value={newPosition}
              onChange={(e) => setNewPosition(e.target.value)}
              placeholder="e.g. Senior Security Analyst"
              required
            />
          </div>

          <div className="form-group">
            <label className="form-label">Classification</label>
            <select
              className="form-input"
              value={newEmploymentType}
              onChange={(e) => setNewEmploymentType(e.target.value)}
            >
              <option value="FullTime">Full-Time Regular</option>
              <option value="PartTime">Part-Time</option>
              <option value="Contractual">Contractual</option>
              <option value="Probationary">Probationary</option>
            </select>
          </div>
        </form>
      </Modal>

      {/* Approve Modal */}
      <Modal
        isOpen={!!approveModal}
        onClose={() => setApproveModal(null)}
        title="Approve Leave Request"
        footer={
          <>
            <button
              type="button"
              className="btn btn-secondary"
              onClick={() => setApproveModal(null)}
              disabled={isSubmitting}
            >
              Cancel
            </button>
            <button
              type="button"
              className="btn btn-primary"
              onClick={handleApproveLeave}
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Confirming...' : 'Approve Request'}
            </button>
          </>
        }
      >
        <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>
          Grant approval for <strong>{approveModal?.employeeName}</strong>'s request for{' '}
          <strong>{approveModal?.days} day(s)</strong> of {approveModal?.leaveType} leave?
        </p>
      </Modal>

      {/* Reject Modal */}
      <Modal
        isOpen={!!rejectModal}
        onClose={() => setRejectModal(null)}
        title="Decline Leave Request"
        footer={
          <>
            <button
              type="button"
              className="btn btn-secondary"
              onClick={() => setRejectModal(null)}
              disabled={isSubmitting}
            >
              Cancel
            </button>
            <button
              type="button"
              className="btn btn-danger"
              onClick={handleRejectLeave}
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Declining...' : 'Decline Request'}
            </button>
          </>
        }
      >
        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
          <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', margin: 0 }}>
            Specify the official explanation for declining <strong>{rejectModal?.employeeName}</strong>'s leave request:
          </p>
          <textarea
            value={rejectReason}
            onChange={(e) => setRejectReason(e.target.value)}
            rows={3}
            placeholder="Reason for decline..."
            className="form-input"
            style={{ resize: 'vertical' }}
          />
        </div>
      </Modal>
    </div>
  );
};

export default HrDashboardPlaceholder;
