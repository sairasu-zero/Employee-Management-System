import React, { useState, useEffect, useCallback } from 'react';
import {
  Users,
  UserCheck,
  UserPlus,
  Plus,
  Mail,
  Building,
  RefreshCw,
} from 'lucide-react';
import { MetricCard } from '../../components/ui/MetricCard';
import { DataTable } from '../../components/ui/DataTable';
import { StatusBadge } from '../../components/ui/StatusBadge';
import { Modal } from '../../components/ui/Modal';
import { FormInput } from '../../components/ui/FormInput';
import { apiGetEmployees, apiCreateEmployee } from '../../services/api';
import { EmployeeRecord } from '../../types';

interface EmployeeDirectoryItem extends Record<string, unknown> {
  id: string;
  name: string;
  employeeId: string;
  department: string;
  position: string;
  email: string;
  status: 'active' | 'inactive' | 'pending';
}

const DEFAULT_DIRECTORY: EmployeeDirectoryItem[] = [
  {
    id: '1',
    name: 'Alexander Mitchell',
    employeeId: 'EMP-ADM-01',
    department: 'Executive Administration',
    position: 'Chief Systems Administrator',
    email: 'alexander.mitchell@erms.local',
    status: 'active',
  },
  {
    id: '2',
    name: 'Sarah Jenkins',
    employeeId: 'EMP-HR-04',
    department: 'Human Resources',
    position: 'HR Operations Lead',
    email: 'sarah.jenkins@erms.local',
    status: 'active',
  },
  {
    id: '3',
    name: 'David Sterling',
    employeeId: 'EMP-ENG-12',
    department: 'Software Engineering',
    position: 'Engineering Director',
    email: 'david.sterling@erms.local',
    status: 'active',
  },
  {
    id: '4',
    name: 'Elena Rostova',
    employeeId: 'EMP-DEV-88',
    department: 'Software Engineering',
    position: 'Senior Frontend Engineer',
    email: 'elena.rostova@erms.local',
    status: 'active',
  },
  {
    id: '5',
    name: 'Marcus Vance',
    employeeId: 'EMP-DEV-89',
    department: 'Software Engineering',
    position: 'Backend .NET Engineer',
    email: 'marcus.vance@erms.local',
    status: 'pending',
  },
  {
    id: '6',
    name: 'Claire Dupont',
    employeeId: 'EMP-FIN-09',
    department: 'Finance & Accounting',
    position: 'Lead Financial Analyst',
    email: 'claire.dupont@erms.local',
    status: 'active',
  },
  {
    id: '7',
    name: 'Robert Torres',
    employeeId: 'EMP-OPS-22',
    department: 'Operations',
    position: 'Facilities & Logistics Coordinator',
    email: 'robert.torres@erms.local',
    status: 'inactive',
  },
];

export const EmployeeListPlaceholder: React.FC = () => {
  const [directory, setDirectory] = useState<EmployeeDirectoryItem[]>(DEFAULT_DIRECTORY);
  const [isLoading, setIsLoading] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedEmp, setSelectedEmp] = useState<EmployeeDirectoryItem | null>(null);

  // Form state
  const [formData, setFormData] = useState({
    name: '',
    empId: '',
    email: '',
    dept: '',
    position: 'Specialist',
  });
  const [isSubmitting, setIsSubmitting] = useState(false);

  const fetchEmployees = useCallback(async () => {
    setIsLoading(true);
    try {
      const result = await apiGetEmployees({ pageSize: 50 });
      if (result && result.items && result.items.length > 0) {
        const mapped: EmployeeDirectoryItem[] = result.items.map((emp: EmployeeRecord) => ({
          id: emp.id,
          name: emp.fullName || `${emp.firstName} ${emp.lastName}`.trim(),
          employeeId: emp.employeeNumber,
          department: emp.departmentName || 'General',
          position: emp.position,
          email: emp.email,
          status: (emp.status?.toLowerCase() === 'active' ? 'active' : emp.status?.toLowerCase() === 'pending' ? 'pending' : 'inactive') as 'active' | 'inactive' | 'pending',
        }));
        setDirectory(mapped);
      }
    } catch {
      // Retain default demo directory when API is unreachable
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchEmployees();
  }, [fetchEmployees]);

  const handleCreateEmployee = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.name || !formData.email) return;

    setIsSubmitting(true);
    const nameParts = formData.name.trim().split(' ');
    const firstName = nameParts[0];
    const lastName = nameParts.length > 1 ? nameParts.slice(1).join(' ') : firstName;

    try {
      const created = await apiCreateEmployee({
        firstName,
        lastName,
        email: formData.email,
        position: formData.position || 'Staff',
        employmentType: 'FullTime',
        status: 'Active',
        hireDate: new Date().toISOString(),
      });

      if (created) {
        const newItem: EmployeeDirectoryItem = {
          id: created.id,
          name: created.fullName || `${created.firstName} ${created.lastName}`,
          employeeId: created.employeeNumber,
          department: formData.dept || 'Engineering',
          position: created.position,
          email: created.email,
          status: 'active',
        };
        setDirectory((prev) => [newItem, ...prev]);
      }
    } catch {
      // Fallback local append for immediate UI feedback
      const localNewItem: EmployeeDirectoryItem = {
        id: `emp_${Date.now()}`,
        name: formData.name,
        employeeId: formData.empId || `EMP-${Math.floor(100 + Math.random() * 900)}`,
        department: formData.dept || 'General',
        position: formData.position || 'Staff',
        email: formData.email,
        status: 'active',
      };
      setDirectory((prev) => [localNewItem, ...prev]);
    } finally {
      setIsSubmitting(false);
      setIsModalOpen(false);
      setFormData({ name: '', empId: '', email: '', dept: '', position: 'Specialist' });
    }
  };

  const columns = [
    {
      key: 'name',
      header: 'Employee Name & ID',
      sortable: true,
      render: (row: EmployeeDirectoryItem) => (
        <div>
          <div style={{ fontWeight: 600, color: 'var(--text-main)' }}>{row.name}</div>
          <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>{row.employeeId}</div>
        </div>
      ),
    },
    {
      key: 'department',
      header: 'Department & Position',
      sortable: true,
      render: (row: EmployeeDirectoryItem) => (
        <div>
          <div style={{ fontWeight: 500, color: 'var(--text-main)' }}>{row.department}</div>
          <div style={{ fontSize: '0.78125rem', color: 'var(--text-secondary)' }}>{row.position}</div>
        </div>
      ),
    },
    {
      key: 'email',
      header: 'Work Email',
      render: (row: EmployeeDirectoryItem) => (
        <span style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)' }}>
          {row.email}
        </span>
      ),
    },
    {
      key: 'status',
      header: 'Status',
      sortable: true,
      render: (row: EmployeeDirectoryItem) => (
        <StatusBadge
          status={row.status}
          label={row.status.charAt(0).toUpperCase() + row.status.slice(1)}
        />
      ),
    },
  ];

  return (
    <div>
      {/* Metric Cards Row */}
      <div className="metrics-grid">
        <MetricCard
          title="Total Workforce"
          value={directory.length}
          change={{ value: 4.8, isPositive: true, period: 'last month' }}
          icon={Users}
          iconBg="var(--primary-light)"
          iconColor="var(--primary)"
          description="Active corporate records"
        />

        <MetricCard
          title="Active Personnel"
          value={directory.filter((e) => e.status === 'active').length}
          change={{ value: 'Full Status', isPositive: true }}
          icon={UserCheck}
          iconBg="#ECFDF5"
          iconColor="#059669"
          description="Provisioned and active"
        />

        <MetricCard
          title="Pending Onboarding"
          value={directory.filter((e) => e.status === 'pending').length}
          change={{ value: 2, isPositive: false, period: 'this week' }}
          icon={UserPlus}
          iconBg="#FFFBEB"
          iconColor="#D97706"
          description="Awaiting HR credential issuance"
        />
      </div>

      {/* Directory Table */}
      <DataTable<EmployeeDirectoryItem>
        title="Workforce Records Directory"
        subtitle="Search, inspect, and manage personnel records across all departments"
        data={directory}
        columns={columns}
        keyExtractor={(row) => row.id}
        searchPlaceholder="Search directory by name, ID, department, or email..."
        searchKeys={['name', 'employeeId', 'department', 'position', 'email']}
        pageSize={10}
        isLoading={isLoading}
        headerActions={
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              onClick={fetchEmployees}
              title="Refresh Directory"
            >
              <RefreshCw size={14} /> Refresh
            </button>
            <button
              type="button"
              className="btn btn-primary btn-sm"
              onClick={() => setIsModalOpen(true)}
            >
              <Plus size={16} /> Add Employee
            </button>
          </div>
        }
        actions={(row) => (
          <button
            type="button"
            className="btn btn-secondary btn-sm"
            onClick={() => setSelectedEmp(row)}
          >
            Profile
          </button>
        )}
      />

      {/* View Profile Modal */}
      <Modal
        isOpen={!!selectedEmp}
        onClose={() => setSelectedEmp(null)}
        title={selectedEmp?.name || 'Employee Profile'}
        subtitle={`Employee ID: ${selectedEmp?.employeeId}`}
        footer={
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => setSelectedEmp(null)}
          >
            Close
          </button>
        }
      >
        {selectedEmp && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.875rem' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
              <Mail size={18} style={{ color: 'var(--primary)' }} />
              <span>{selectedEmp.email}</span>
            </div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
              <Building size={18} style={{ color: 'var(--primary)' }} />
              <span>{selectedEmp.department} &mdash; {selectedEmp.position}</span>
            </div>
            <div>
              <span style={{ fontWeight: 600 }}>Status: </span>
              <StatusBadge status={selectedEmp.status} />
            </div>
          </div>
        )}
      </Modal>

      {/* Add Employee Modal */}
      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title="Add Employee Record"
        subtitle="Enter new personnel details for ERMS directory indexing"
        footer={
          <>
            <button
              type="button"
              className="btn btn-secondary"
              onClick={() => setIsModalOpen(false)}
              disabled={isSubmitting}
            >
              Cancel
            </button>
            <button
              type="button"
              className="btn btn-primary"
              onClick={handleCreateEmployee}
              disabled={isSubmitting || !formData.name || !formData.email}
            >
              {isSubmitting ? 'Saving...' : 'Create Record'}
            </button>
          </>
        }
      >
        <form onSubmit={handleCreateEmployee} style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
          <FormInput
            label="Full Name"
            name="name"
            placeholder="First and last name"
            value={formData.name}
            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            required
          />
          <FormInput
            label="Employee ID (Optional)"
            name="empId"
            placeholder="e.g. EMP-106 (Auto-generated if empty)"
            value={formData.empId}
            onChange={(e) => setFormData({ ...formData, empId: e.target.value })}
          />
          <FormInput
            label="Work Email"
            name="email"
            type="email"
            placeholder="name@organization.com"
            value={formData.email}
            onChange={(e) => setFormData({ ...formData, email: e.target.value })}
            required
          />
          <FormInput
            label="Department"
            name="dept"
            placeholder="e.g. Software Engineering"
            value={formData.dept}
            onChange={(e) => setFormData({ ...formData, dept: e.target.value })}
          />
          <FormInput
            label="Position / Title"
            name="position"
            placeholder="e.g. Senior Backend Engineer"
            value={formData.position}
            onChange={(e) => setFormData({ ...formData, position: e.target.value })}
          />
        </form>
      </Modal>
    </div>
  );
};

export default EmployeeListPlaceholder;
