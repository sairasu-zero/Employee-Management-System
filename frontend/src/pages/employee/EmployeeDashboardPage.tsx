import React, { useState, useEffect, useCallback, useRef } from 'react';
import {
  Calendar,
  FileText,
  Clock,
  Plus,
  CheckCircle,
  Upload,
  Download,
  Trash2,
  RefreshCw,
  AlertCircle,
  CheckCircle2,
  Ban,
} from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import { MetricCard } from '../../components/ui/MetricCard';
import { DataTable } from '../../components/ui/DataTable';
import { StatusBadge } from '../../components/ui/StatusBadge';
import { Modal } from '../../components/ui/Modal';
import { FormInput } from '../../components/ui/FormInput';
import {
  apiGetLeaveRequests,
  apiSubmitLeave,
  apiCancelLeave,
  apiGetEmployeeDocuments,
  apiUploadDocument,
  apiDownloadDocument,
  apiDeleteDocument,
} from '../../services/api';
import { LeaveRequestRecord, EmployeeDocumentRecord } from '../../types';

export const EmployeeDashboardPage: React.FC = () => {
  const { user } = useAuth();
  const [requests, setRequests] = useState<LeaveRequestRecord[]>([]);
  const [documents, setDocuments] = useState<EmployeeDocumentRecord[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [activeTab, setActiveTab] = useState<'leaves' | 'documents'>('leaves');

  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  // Leave Modal State
  const [isLeaveModalOpen, setIsLeaveModalOpen] = useState(false);
  const [leaveType, setLeaveType] = useState('Annual Vacation');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [reason, setReason] = useState('');
  const [isSubmittingLeave, setIsSubmittingLeave] = useState(false);

  // Document Upload State
  const [isDocModalOpen, setIsDocModalOpen] = useState(false);
  const [docName, setDocName] = useState('');
  const [docType, setDocType] = useState('Contract');
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Cancel Leave Confirmation
  const [cancelModal, setCancelModal] = useState<LeaveRequestRecord | null>(null);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);
    try {
      const leavesPromise = apiGetLeaveRequests();
      const docsPromise = user?.id ? apiGetEmployeeDocuments(user.id) : Promise.resolve([]);

      const [leavesRes, docsRes] = await Promise.allSettled([leavesPromise, docsPromise]);
      if (leavesRes.status === 'fulfilled') {
        setRequests(leavesRes.value);
      }
      if (docsRes.status === 'fulfilled') {
        setDocuments(docsRes.value);
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to load records.';
      setErrorMessage(msg);
    } finally {
      setIsLoading(false);
    }
  }, [user?.id]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleSubmitLeave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!startDate || !endDate || !reason.trim()) {
      setErrorMessage('Please fill in start date, end date, and reason.');
      return;
    }

    if (new Date(endDate) < new Date(startDate)) {
      setErrorMessage('End date cannot precede start date.');
      return;
    }

    setIsSubmittingLeave(true);
    setErrorMessage(null);
    try {
      await apiSubmitLeave({
        leaveType,
        startDate: new Date(startDate).toISOString(),
        endDate: new Date(endDate).toISOString(),
        reason: reason.trim(),
      });

      setSuccessMessage('Leave request submitted successfully for manager approval.');
      setIsLeaveModalOpen(false);
      setStartDate('');
      setEndDate('');
      setReason('');
      await loadData();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to submit leave request.';
      setErrorMessage(msg);
    } finally {
      setIsSubmittingLeave(false);
    }
  };

  const handleCancelLeave = async () => {
    if (!cancelModal) return;
    try {
      await apiCancelLeave(cancelModal.id);
      setSuccessMessage('Leave request cancelled.');
      setCancelModal(null);
      await loadData();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to cancel request.';
      setErrorMessage(msg);
    }
  };

  const handleUploadDocument = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedFile) {
      setErrorMessage('Please choose a file to upload.');
      return;
    }
    if (!user?.id) {
      setErrorMessage('Employee account not found.');
      return;
    }

    const formData = new FormData();
    formData.append('file', selectedFile);
    formData.append('documentName', docName.trim() || selectedFile.name);
    formData.append('documentType', docType);

    setIsUploading(true);
    setErrorMessage(null);
    try {
      await apiUploadDocument(user.id, formData);
      setSuccessMessage(`Document '${selectedFile.name}' securely uploaded.`);
      setIsDocModalOpen(false);
      setSelectedFile(null);
      setDocName('');
      await loadData();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Document upload failed.';
      setErrorMessage(msg);
    } finally {
      setIsUploading(false);
    }
  };

  const handleDownload = async (doc: EmployeeDocumentRecord) => {
    try {
      await apiDownloadDocument(doc.id, doc.originalFileName || doc.documentName);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Download failed.';
      setErrorMessage(msg);
    }
  };

  const handleDeleteDoc = async (docId: string) => {
    if (!confirm('Are you sure you want to remove this document?')) return;
    try {
      await apiDeleteDocument(docId);
      setSuccessMessage('Document removed.');
      await loadData();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Deletion failed.';
      setErrorMessage(msg);
    }
  };

  const pendingLeavesCount = requests.filter((r) => r.status === 'Pending').length;
  const approvedLeavesCount = requests.filter((r) => r.status === 'Approved').length;

  const leaveColumns = [
    {
      key: 'leaveType',
      header: 'Request Type',
      sortable: true,
      render: (row: LeaveRequestRecord) => (
        <span style={{ fontWeight: 600, color: 'var(--text-main)' }}>{row.leaveType}</span>
      ),
    },
    {
      key: 'startDate',
      header: 'Effective Period',
      sortable: true,
      render: (row: LeaveRequestRecord) => (
        <span style={{ fontSize: '0.8125rem' }}>
          {new Date(row.startDate).toLocaleDateString()} - {new Date(row.endDate).toLocaleDateString()}
        </span>
      ),
    },
    {
      key: 'days',
      header: 'Days',
      render: (row: LeaveRequestRecord) => `${row.days} day(s)`,
    },
    {
      key: 'reason',
      header: 'Reason',
      render: (row: LeaveRequestRecord) => (
        <span style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)' }}>{row.reason}</span>
      ),
    },
    {
      key: 'status',
      header: 'Review Status',
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

  const docColumns = [
    {
      key: 'documentName',
      header: 'Document Name',
      sortable: true,
      render: (row: EmployeeDocumentRecord) => (
        <div>
          <span style={{ fontWeight: 600, color: 'var(--text-main)', display: 'block' }}>
            {row.documentName}
          </span>
          <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
            {row.originalFileName} ({Math.round(row.fileSize / 1024)} KB)
          </span>
        </div>
      ),
    },
    {
      key: 'documentType',
      header: 'Category',
      sortable: true,
    },
    {
      key: 'uploadedAt',
      header: 'Upload Date',
      sortable: true,
      render: (row: EmployeeDocumentRecord) =>
        new Date(row.uploadedAt).toLocaleDateString(),
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

      {/* Profile Header Banner */}
      <div
        className="table-card"
        style={{
          padding: '1.5rem',
          marginBottom: '1.75rem',
          display: 'flex',
          flexDirection: 'row',
          alignItems: 'center',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: '1rem',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '1.25rem' }}>
          <div className="avatar avatar-lg">{user?.avatarInitials || 'EM'}</div>
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <h2 style={{ fontSize: '1.25rem', fontWeight: 700, color: 'var(--text-main)', margin: 0 }}>
                {user?.fullName || 'Employee User'}
              </h2>
              <StatusBadge status="active" label="Active" size="sm" />
            </div>
            <p style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)', marginTop: '0.2rem', marginBottom: 0 }}>
              {user?.position || 'Staff'} • {user?.department || 'Operations'}
            </p>
            <p style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '0.2rem', marginBottom: 0 }}>
              Employee ID: <strong>{user?.employeeId || 'EMP-0000'}</strong> • Email: {user?.email}
            </p>
          </div>
        </div>

        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => setIsDocModalOpen(true)}
          >
            <Upload size={16} />
            <span>Upload Document</span>
          </button>
          <button
            type="button"
            className="btn btn-primary"
            onClick={() => setIsLeaveModalOpen(true)}
          >
            <Plus size={16} />
            <span>Request Time Off</span>
          </button>
        </div>
      </div>

      {/* Metrics Row */}
      <div className="metrics-grid">
        <MetricCard
          title="Approved Leaves"
          value={approvedLeavesCount.toString()}
          icon={Calendar}
          iconBg="var(--primary-light)"
          iconColor="var(--primary)"
          description="Granted time off periods"
        />

        <MetricCard
          title="Pending Requests"
          value={pendingLeavesCount.toString()}
          icon={Clock}
          iconBg="#FFFBEB"
          iconColor="#D97706"
          description="Awaiting manager sign-off"
        />

        <MetricCard
          title="Personnel Documents"
          value={documents.length.toString()}
          icon={FileText}
          iconBg="#EFF6FF"
          iconColor="#2563EB"
          description="Stored personnel credentials"
        />

        <MetricCard
          title="Security Status"
          value={user?.isMfaEnabled ? 'MFA Protected' : 'MFA Available'}
          icon={CheckCircle}
          iconBg="#ECFDF5"
          iconColor="#059669"
          description="Identity Assurance Grade"
        />
      </div>

      {/* Subsystem Tabs */}
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
            className={`btn ${activeTab === 'leaves' ? 'btn-primary' : 'btn-secondary'} btn-sm`}
            onClick={() => setActiveTab('leaves')}
          >
            <Calendar size={14} />
            <span>My Time-Off Requests ({requests.length})</span>
          </button>
          <button
            type="button"
            className={`btn ${activeTab === 'documents' ? 'btn-primary' : 'btn-secondary'} btn-sm`}
            onClick={() => setActiveTab('documents')}
          >
            <FileText size={14} />
            <span>My Digital Personnel File ({documents.length})</span>
          </button>
        </div>

        <button
          type="button"
          className="btn btn-secondary btn-sm"
          onClick={loadData}
          disabled={isLoading}
        >
          <RefreshCw size={14} className={isLoading ? 'spin' : ''} />
          <span>Refresh</span>
        </button>
      </div>

      {activeTab === 'leaves' ? (
        <DataTable<LeaveRequestRecord>
          title="Leave & Absence Inquiries"
          subtitle="Real-time status of your requested time-off from the central database"
          data={requests}
          columns={leaveColumns}
          keyExtractor={(row) => row.id}
          searchPlaceholder="Search my requests..."
          actions={(row) =>
            row.status === 'Pending' ? (
              <button
                type="button"
                className="btn btn-ghost btn-sm"
                style={{ color: 'var(--status-danger)' }}
                onClick={() => setCancelModal(row)}
              >
                <Ban size={14} />
                <span>Cancel</span>
              </button>
            ) : null
          }
        />
      ) : (
        <DataTable<EmployeeDocumentRecord>
          title="Digital Personnel Documents"
          subtitle="Securely stored verification files, resumes, and identification records"
          data={documents}
          columns={docColumns}
          keyExtractor={(row) => row.id}
          searchPlaceholder="Search my documents..."
          actions={(row) => (
            <div style={{ display: 'flex', gap: '0.4rem', justifyContent: 'flex-end' }}>
              <button
                type="button"
                className="btn btn-secondary btn-sm"
                title="Download Document"
                onClick={() => handleDownload(row)}
              >
                <Download size={14} />
                <span>Download</span>
              </button>
              <button
                type="button"
                className="btn btn-ghost btn-sm"
                title="Delete Document"
                style={{ color: 'var(--status-danger)' }}
                onClick={() => handleDeleteDoc(row.id)}
              >
                <Trash2 size={14} />
              </button>
            </div>
          )}
        />
      )}

      {/* Submit Leave Modal */}
      <Modal
        isOpen={isLeaveModalOpen}
        onClose={() => setIsLeaveModalOpen(false)}
        title="Submit Leave Request"
        subtitle="Your request will be routed to your direct supervisor"
        footer={
          <>
            <button
              type="button"
              className="btn btn-secondary"
              onClick={() => setIsLeaveModalOpen(false)}
              disabled={isSubmittingLeave}
            >
              Cancel
            </button>
            <button
              type="button"
              className="btn btn-primary"
              onClick={handleSubmitLeave}
              disabled={isSubmittingLeave}
            >
              {isSubmittingLeave ? 'Submitting...' : 'Submit Request'}
            </button>
          </>
        }
      >
        <form onSubmit={handleSubmitLeave} style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
          <div className="form-group">
            <label className="form-label">Leave Category *</label>
            <select
              className="form-input"
              value={leaveType}
              onChange={(e) => setLeaveType(e.target.value)}
            >
              <option value="Annual Vacation">Annual Vacation (PTO)</option>
              <option value="Sick Leave">Sick / Medical Leave</option>
              <option value="Personal Emergency">Personal Emergency</option>
              <option value="Maternity / Paternity">Maternity / Paternity</option>
              <option value="Bereavement">Bereavement</option>
              <option value="Work From Home">Remote / Work From Home</option>
            </select>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
            <FormInput
              label="Start Date *"
              name="startDate"
              type="date"
              value={startDate}
              onChange={(e) => setStartDate(e.target.value)}
              required
            />
            <FormInput
              label="End Date *"
              name="endDate"
              type="date"
              value={endDate}
              onChange={(e) => setEndDate(e.target.value)}
              required
            />
          </div>

          <FormInput
            label="Reason / Notes *"
            name="reason"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder="Provide context for your manager..."
            required
          />
        </form>
      </Modal>

      {/* Upload Document Modal */}
      <Modal
        isOpen={isDocModalOpen}
        onClose={() => setIsDocModalOpen(false)}
        title="Upload Personnel Document"
        subtitle="Upload verified PDF, PNG, or JPEG file (up to 10 MB)"
        footer={
          <>
            <button
              type="button"
              className="btn btn-secondary"
              onClick={() => setIsDocModalOpen(false)}
              disabled={isUploading}
            >
              Cancel
            </button>
            <button
              type="button"
              className="btn btn-primary"
              onClick={handleUploadDocument}
              disabled={isUploading}
            >
              {isUploading ? 'Uploading...' : 'Upload File'}
            </button>
          </>
        }
      >
        <form onSubmit={handleUploadDocument} style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
          <FormInput
            label="Document Name"
            name="documentName"
            value={docName}
            onChange={(e) => setDocName(e.target.value)}
            placeholder="e.g. Employment Contract 2026"
          />

          <div className="form-group">
            <label className="form-label">Classification Category</label>
            <select
              className="form-input"
              value={docType}
              onChange={(e) => setDocType(e.target.value)}
            >
              <option value="Contract">Employment Contract</option>
              <option value="Resume">Resume / Curriculum Vitae</option>
              <option value="GovernmentId">Government-issued ID</option>
              <option value="Certificate">Professional Certification</option>
              <option value="Medical">Medical Clearance</option>
              <option value="Other">Other Document</option>
            </select>
          </div>

          <div className="form-group">
            <label className="form-label">Choose File (PDF, PNG, JPEG - max 10MB) *</label>
            <input
              ref={fileInputRef}
              type="file"
              accept=".pdf,.png,.jpg,.jpeg,application/pdf,image/png,image/jpeg"
              onChange={(e) => setSelectedFile(e.target.files?.[0] || null)}
              className="form-input"
              required
            />
            {selectedFile && (
              <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', marginTop: '0.25rem', display: 'block' }}>
                Selected: {selectedFile.name} ({(selectedFile.size / 1024).toFixed(1)} KB)
              </span>
            )}
          </div>
        </form>
      </Modal>

      {/* Cancel Confirmation Modal */}
      <Modal
        isOpen={!!cancelModal}
        onClose={() => setCancelModal(null)}
        title="Cancel Leave Request"
        footer={
          <>
            <button
              type="button"
              className="btn btn-secondary"
              onClick={() => setCancelModal(null)}
            >
              Keep Request
            </button>
            <button
              type="button"
              className="btn btn-danger"
              onClick={handleCancelLeave}
            >
              Confirm Cancellation
            </button>
          </>
        }
      >
        <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>
          Are you sure you want to cancel your <strong>{cancelModal?.leaveType}</strong> request for{' '}
          {cancelModal?.days} day(s)?
        </p>
      </Modal>
    </div>
  );
};

export default EmployeeDashboardPage;
