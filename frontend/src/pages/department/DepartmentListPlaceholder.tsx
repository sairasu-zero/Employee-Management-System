import React, { useState, useEffect, useCallback } from 'react';
import {
  Building2,
  Users,
  DollarSign,
  Plus,
  Briefcase,
  RefreshCw,
} from 'lucide-react';
import { MetricCard } from '../../components/ui/MetricCard';
import { DataTable } from '../../components/ui/DataTable';
import { StatusBadge } from '../../components/ui/StatusBadge';
import { Modal } from '../../components/ui/Modal';
import { FormInput } from '../../components/ui/FormInput';
import { apiGetDepartments, apiCreateDepartment } from '../../services/api';
import { DepartmentRecord } from '../../types';

interface DepartmentItem extends Record<string, unknown> {
  id: string;
  name: string;
  code: string;
  leadManager: string;
  headcount: number;
  budget: string;
  status: 'active' | 'pending';
}

const DEFAULT_DEPARTMENTS: DepartmentItem[] = [
  {
    id: '1',
    name: 'Executive Administration',
    code: 'ADM',
    leadManager: 'Alexander Mitchell',
    headcount: 6,
    budget: '$850,000',
    status: 'active',
  },
  {
    id: '2',
    name: 'Software Engineering',
    code: 'ENG',
    leadManager: 'David Sterling',
    headcount: 48,
    budget: '$4,200,000',
    status: 'active',
  },
  {
    id: '3',
    name: 'Human Resources',
    code: 'HR',
    leadManager: 'Sarah Jenkins',
    headcount: 12,
    budget: '$620,000',
    status: 'active',
  },
  {
    id: '4',
    name: 'Finance & Accounting',
    code: 'FIN',
    leadManager: 'Claire Dupont',
    headcount: 16,
    budget: '$1,100,000',
    status: 'active',
  },
  {
    id: '5',
    name: 'Operations & Infrastructure',
    code: 'OPS',
    leadManager: 'Robert Torres',
    headcount: 24,
    budget: '$1,950,000',
    status: 'active',
  },
];

export const DepartmentListPlaceholder: React.FC = () => {
  const [departments, setDepartments] = useState<DepartmentItem[]>(DEFAULT_DEPARTMENTS);
  const [isLoading, setIsLoading] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [formData, setFormData] = useState({
    name: '',
    code: '',
    leadManager: '',
    budget: '$750,000',
  });
  const [isSubmitting, setIsSubmitting] = useState(false);

  const fetchDepartments = useCallback(async () => {
    setIsLoading(true);
    try {
      const data = await apiGetDepartments();
      if (data && data.length > 0) {
        const mapped: DepartmentItem[] = data.map((dept: DepartmentRecord) => ({
          id: dept.id,
          name: dept.name,
          code: dept.code,
          leadManager: dept.managerName || 'Unassigned',
          headcount: dept.headcount || 0,
          budget: dept.budget || '$1,200,000',
          status: 'active',
        }));
        setDepartments(mapped);
      }
    } catch {
      // Retain fallback list when API is unreachable
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchDepartments();
  }, [fetchDepartments]);

  const handleCreateDepartment = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.name || !formData.code) return;

    setIsSubmitting(true);
    try {
      const created = await apiCreateDepartment({
        name: formData.name.trim(),
        code: formData.code.trim().toUpperCase(),
        description: `Department managed by ${formData.leadManager || 'Leadership'}`,
      });

      if (created) {
        const newItem: DepartmentItem = {
          id: created.id,
          name: created.name,
          code: created.code,
          leadManager: formData.leadManager || 'Unassigned',
          headcount: 0,
          budget: formData.budget || '$500,000',
          status: 'active',
        };
        setDepartments((prev) => [...prev, newItem]);
      }
    } catch {
      // Fallback local append for immediate UI feedback
      const localItem: DepartmentItem = {
        id: `dept_${Date.now()}`,
        name: formData.name,
        code: formData.code.toUpperCase(),
        leadManager: formData.leadManager || 'Unassigned',
        headcount: 1,
        budget: formData.budget || '$500,000',
        status: 'active',
      };
      setDepartments((prev) => [...prev, localItem]);
    } finally {
      setIsSubmitting(false);
      setIsModalOpen(false);
      setFormData({ name: '', code: '', leadManager: '', budget: '$750,000' });
    }
  };

  const columns = [
    {
      key: 'name',
      header: 'Department & Code',
      sortable: true,
      render: (row: DepartmentItem) => (
        <div>
          <div style={{ fontWeight: 600, color: 'var(--text-main)' }}>{row.name}</div>
          <span style={{ fontSize: '0.71875rem', color: 'var(--primary)', fontWeight: 600 }}>
            CODE: {row.code}
          </span>
        </div>
      ),
    },
    {
      key: 'leadManager',
      header: 'Lead Manager / Director',
      sortable: true,
    },
    {
      key: 'headcount',
      header: 'Staff Headcount',
      sortable: true,
      render: (row: DepartmentItem) => (
        <span style={{ fontWeight: 600 }}>{row.headcount} members</span>
      ),
    },
    {
      key: 'budget',
      header: 'Fiscal Budget',
      sortable: true,
    },
    {
      key: 'status',
      header: 'Status',
      sortable: true,
      render: (row: DepartmentItem) => (
        <StatusBadge status={row.status} label="Operational" />
      ),
    },
  ];

  const totalHeadcount = departments.reduce((acc, d) => acc + d.headcount, 0);

  return (
    <div>
      <div className="metrics-grid">
        <MetricCard
          title="Total Departments"
          value={departments.length}
          icon={Building2}
          iconBg="var(--primary-light)"
          iconColor="var(--primary)"
          description="Operational business units"
        />

        <MetricCard
          title="Largest Department"
          value={departments[0]?.name.split(' ')[0] || 'Engineering'}
          icon={Briefcase}
          iconBg="#EFF6FF"
          iconColor="#2563EB"
          description="Primary workforce unit"
        />

        <MetricCard
          title="Total Headcount"
          value={totalHeadcount > 0 ? totalHeadcount : 142}
          icon={Users}
          iconBg="#ECFDF5"
          iconColor="#059669"
          description="Distributed across units"
        />

        <MetricCard
          title="Annualized Budget"
          value="$12.4M"
          icon={DollarSign}
          iconBg="#FFFBEB"
          iconColor="#D97706"
          description="Fiscal Year 2026-2027"
        />
      </div>

      <DataTable<DepartmentItem>
        title="Department Hierarchy"
        subtitle="Organizational unit governance, cost centers, and leadership mapping"
        data={departments}
        columns={columns}
        keyExtractor={(row) => row.id}
        searchPlaceholder="Search departments by title or code..."
        searchKeys={['name', 'code', 'leadManager']}
        pageSize={5}
        isLoading={isLoading}
        headerActions={
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              onClick={fetchDepartments}
              title="Refresh Departments"
            >
              <RefreshCw size={14} /> Refresh
            </button>
            <button
              type="button"
              className="btn btn-primary btn-sm"
              onClick={() => setIsModalOpen(true)}
            >
              <Plus size={16} />
              <span>Create Department</span>
            </button>
          </div>
        }
        actions={() => (
          <button type="button" className="btn btn-secondary btn-sm">
            Manage
          </button>
        )}
      />

      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title="Create New Department"
        subtitle="Define an organizational unit and assign department leadership"
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
              onClick={handleCreateDepartment}
              disabled={isSubmitting || !formData.name || !formData.code}
            >
              {isSubmitting ? 'Saving...' : 'Confirm Creation'}
            </button>
          </>
        }
      >
        <form onSubmit={handleCreateDepartment} style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
          <FormInput
            label="Department Name"
            name="name"
            placeholder="e.g. Legal & Compliance"
            value={formData.name}
            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            required
          />
          <FormInput
            label="Department Code"
            name="code"
            placeholder="e.g. LGL"
            value={formData.code}
            onChange={(e) => setFormData({ ...formData, code: e.target.value })}
            required
          />
          <FormInput
            label="Director / Lead Manager"
            name="lead"
            placeholder="Assign director"
            value={formData.leadManager}
            onChange={(e) => setFormData({ ...formData, leadManager: e.target.value })}
          />
          <FormInput
            label="Annual Fiscal Budget"
            name="budget"
            placeholder="e.g. $750,000"
            value={formData.budget}
            onChange={(e) => setFormData({ ...formData, budget: e.target.value })}
          />
        </form>
      </Modal>
    </div>
  );
};

export default DepartmentListPlaceholder;
