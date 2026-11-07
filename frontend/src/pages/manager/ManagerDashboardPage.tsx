import React, { useState, useEffect, useCallback } from 'react';
import {
  Users,
  CheckSquare,
  Clock,
  Target,
  Check,
  X,
  RefreshCw,
  Eye,
  AlertCircle,
  CheckCircle2,
} from 'lucide-react';
import { MetricCard } from '../../components/ui/MetricCard';
import { DataTable } from '../../components/ui/DataTable';
import { StatusBadge } from '../../components/ui/StatusBadge';
import { Modal } from '../../components/ui/Modal';
import {
  apiGetLeaveRequests,
  apiApproveLeave,
  apiRejectLeave,
} from '../../services/api';
import { LeaveRequestRecord } from '../../types';

export const ManagerDashboardPage: React.FC = () => {
  const [requests, setRequests] = useState<LeaveRequestRecord[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  // Modals state
  const [detailsModal, setDetailsModal] = useState<LeaveRequestRecord | null>(null);
  const [approveModal, setApproveModal] = useState<LeaveRequestRecord | null>(null);
  const [rejectModal, setRejectModal] = useState<LeaveRequestRecord | null>(null);
  const [rejectionReason, setRejectionReason] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const fetchLeaveRequests = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);
    try {
      const data = await apiGetLeaveRequests();
      setRequests(data);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to retrieve leave requests.';
      setErrorMessage(msg);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchLeaveRequests();
  }, [fetchLeaveRequests]);

  const handleApprove = async () => {
    if (!approveModal) return;
    setIsSubmitting(true);
    setErrorMessage(null);
    try {
      await apiApproveLeave(approveModal.id);
      setSuccessMessage(`Successfully approved leave request for ${approveModal.employeeName}.`);
      setApproveModal(null);
      await fetchLeaveRequests();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to approve leave request.';
      setErrorMessage(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleReject = async () => {
    if (!rejectModal) return;
    if (!rejectionReason.trim()) {
      setErrorMessage('Please provide an explanatory reason for declining the request.');
      return;
    }
    setIsSubmitting(true);
    setErrorMessage(null);
    try {
      await apiRejectLeave(rejectModal.id, rejectionReason.trim());
      setSuccessMessage(`Successfully declined leave request for ${rejectModal.employeeName}.`);
      setRejectModal(null);
      setRejectionReason('');
      await fetchLeaveRequests();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to decline leave request.';
      setErrorMessage(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  const pendingCount = requests.filter((r) => r.status === 'Pending').length;
  const approvedCount = requests.filter((r) => r.status === 'Approved').length;

  const columns = [
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
      header: 'Requested Period',
      sortable: true,
      render: (row: LeaveRequestRecord) => (
        <span style={{ fontSize: '0.8125rem' }}>
          {new Date(row.startDate).toLocaleDateString()} - {new Date(row.endDate).toLocaleDateString()}
        </span>
      ),
    },
    {
      key: 'days',
      header: 'Duration',
      render: (row: LeaveRequestRecord) => `${row.days} day(s)`,
    },
    {
      key: 'status',
      header: 'Approval State',
      sortable: true,
      render: (row: LeaveRequestRecord) => {
        const badgeStatus =
          row.status === 'Pending'
            ? 'pending'
            : row.status === 'Approved'
            ? 'success'
            : 'danger';

        return <StatusBadge status={badgeStatus} label={row.status} />;
      },
    },
  ];

  return (
    <div>
      {/* Top Notification Alerts */}
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
          title="Total Department Requests"
          value={requests.length.toString()}
          icon={Users}
          iconBg="var(--primary-light)"
          iconColor="var(--primary)"
          description="Total workforce requests logged"
        />

        <MetricCard
          title="Pending Approvals"
          value={pendingCount.toString()}
          icon={Clock}
          iconBg="#FFFBEB"
          iconColor="#D97706"
          description="Requires managerial sign-off"
        />

        <MetricCard
          title="Sprint Availability"
          value={pendingCount === 0 ? "100%" : `${Math.max(70, 100 - pendingCount * 5)}%`}
          icon={Target}
          iconBg="#ECFDF5"
          iconColor="#059669"
          description="Current team capacity"
        />

        <MetricCard
          title="Approved Requests"
          value={approvedCount.toString()}
          icon={CheckSquare}
          iconBg="#EFF6FF"
          iconColor="#2563EB"
          description="Granted team leave requests"
        />
      </div>

      {/* Action Header Banner */}
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: '1rem' }}>
        <button
          type="button"
          className="btn btn-secondary btn-sm"
          onClick={fetchLeaveRequests}
          disabled={isLoading}
        >
          <RefreshCw size={14} className={isLoading ? 'spin' : ''} />
          <span>Refresh Leave Requests</span>
        </button>
      </div>

      <DataTable<LeaveRequestRecord>
        title="Team Leave & PTO Approvals"
        subtitle="Review, approve, or decline vacation and attendance requests within your authorized scope"
        data={requests}
        columns={columns}
        keyExtractor={(row) => row.id}
        searchPlaceholder="Filter requests by employee name or category..."
        searchKeys={['employeeName', 'leaveType', 'status']}
        actions={(row) => (
          <div style={{ display: 'flex', gap: '0.4rem', justifyContent: 'flex-end' }}>
            <button
              type="button"
              className="btn btn-ghost btn-sm"
              title="View Request Details"
              onClick={() => setDetailsModal(row)}
            >
              <Eye size={14} />
            </button>
            {row.status === 'Pending' && (
              <>
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
                    setRejectionReason('');
                  }}
                >
                  <X size={14} />
                  <span>Decline</span>
                </button>
              </>
            )}
          </div>
        )}
      />

      {/* Details Modal */}
      <Modal
        isOpen={!!detailsModal}
        onClose={() => setDetailsModal(null)}
        title="Leave Request Details"
        footer={
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => setDetailsModal(null)}
          >
            Close
          </button>
        }
      >
        {detailsModal && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.875rem' }}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
              <div>
                <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Team Member</span>
                <p style={{ margin: 0, fontWeight: 600 }}>{detailsModal.employeeName}</p>
                <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>
                  {detailsModal.employeeNumber}
                </span>
              </div>
              <div>
                <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Leave Category</span>
                <p style={{ margin: 0, fontWeight: 600 }}>{detailsModal.leaveType}</p>
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
              <div>
                <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Duration</span>
                <p style={{ margin: 0, fontWeight: 500 }}>{detailsModal.days} day(s)</p>
              </div>
              <div>
                <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Status</span>
                <p style={{ margin: 0 }}>
                  <StatusBadge
                    status={
                      detailsModal.status === 'Pending'
                        ? 'pending'
                        : detailsModal.status === 'Approved'
                        ? 'success'
                        : 'danger'
                    }
                    label={detailsModal.status}
                  />
                </p>
              </div>
            </div>

            <div>
              <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Effective Dates</span>
              <p style={{ margin: 0 }}>
                {new Date(detailsModal.startDate).toLocaleDateString()} to{' '}
                {new Date(detailsModal.endDate).toLocaleDateString()}
              </p>
            </div>

            <div>
              <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Reason / Justification</span>
              <p
                style={{
                  margin: '0.25rem 0 0',
                  padding: '0.625rem',
                  backgroundColor: 'var(--bg-main)',
                  borderRadius: 'var(--radius-sm)',
                  fontSize: '0.875rem',
                  color: 'var(--text-secondary)',
                  border: '1px solid var(--border-color)',
                }}
              >
                {detailsModal.reason}
              </p>
            </div>

            {detailsModal.rejectionReason && (
              <div>
                <span style={{ fontSize: '0.75rem', color: 'var(--status-danger)' }}>
                  Declined Explanation
                </span>
                <p
                  style={{
                    margin: '0.25rem 0 0',
                    padding: '0.625rem',
                    backgroundColor: 'rgba(239, 68, 68, 0.05)',
                    borderRadius: 'var(--radius-sm)',
                    fontSize: '0.875rem',
                    color: 'var(--status-danger)',
                    border: '1px solid rgba(239, 68, 68, 0.2)',
                  }}
                >
                  {detailsModal.rejectionReason}
                </p>
              </div>
            )}
          </div>
        )}
      </Modal>

      {/* Approve Confirmation Modal */}
      <Modal
        isOpen={!!approveModal}
        onClose={() => setApproveModal(null)}
        title="Confirm Leave Approval"
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
              onClick={handleApprove}
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Approving...' : 'Confirm Approval'}
            </button>
          </>
        }
      >
        <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', lineHeight: 1.6 }}>
          Are you sure you want to approve the <strong>{approveModal?.leaveType}</strong> leave request for{' '}
          <strong>{approveModal?.employeeName}</strong> ({approveModal?.days} day(s))?
        </p>
        <p style={{ fontSize: '0.8125rem', color: 'var(--text-muted)' }}>
          This will update the leave record status in the central database and log an official audit event.
        </p>
      </Modal>

      {/* Reject Modal with Explanation Input */}
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
              onClick={handleReject}
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Declining...' : 'Decline Request'}
            </button>
          </>
        }
      >
        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
          <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', margin: 0 }}>
            Declining leave request for <strong>{rejectModal?.employeeName}</strong>. Please provide a clear explanation for the employee:
          </p>
          <textarea
            value={rejectionReason}
            onChange={(e) => setRejectionReason(e.target.value)}
            rows={3}
            placeholder="Specify reason for decline (e.g., project deliverable conflict, staffing threshold)..."
            style={{
              width: '100%',
              padding: '0.625rem',
              borderRadius: 'var(--radius-sm)',
              border: '1px solid var(--border-color)',
              backgroundColor: 'var(--bg-main)',
              color: 'var(--text-main)',
              fontSize: '0.875rem',
              resize: 'vertical',
            }}
          />
        </div>
      </Modal>
    </div>
  );
};

export default ManagerDashboardPage;
