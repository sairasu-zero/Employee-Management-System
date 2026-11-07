import React, { useState, useMemo } from 'react';
import {
  Search,
  ChevronLeft,
  ChevronRight,
  ArrowUpDown,
  ArrowUp,
  ArrowDown,
  Inbox,
  Loader2,
} from 'lucide-react';
import { DataTableProps } from '../../types';

export function DataTable<T extends Record<string, unknown>>({
  data,
  columns,
  keyExtractor,
  searchPlaceholder = 'Search records...',
  searchKeys,
  pageSize = 10,
  pageSizeOptions = [5, 10, 25, 50],
  onRowClick,
  actions,
  isLoading = false,
  emptyMessage = 'No matching records found',
  emptyIcon: EmptyIcon = Inbox,
  title,
  subtitle,
  headerActions,
}: DataTableProps<T>): React.ReactElement {
  const [searchTerm, setSearchTerm] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [perPage, setPerPage] = useState(pageSize);
  const [sortConfig, setSortConfig] = useState<{
    key: string;
    direction: 'asc' | 'desc';
  } | null>(null);

  // Filter data based on search term
  const filteredData = useMemo(() => {
    if (!searchTerm.trim()) return data;

    const query = searchTerm.toLowerCase();

    return data.filter((row) => {
      if (searchKeys && searchKeys.length > 0) {
        return searchKeys.some((k) => {
          const val = row[k as string];
          return val !== undefined && val !== null && String(val).toLowerCase().includes(query);
        });
      }

      // Search across all string/number columns
      return Object.values(row).some((val) => {
        if (val === null || val === undefined) return false;
        return String(val).toLowerCase().includes(query);
      });
    });
  }, [data, searchTerm, searchKeys]);

  // Sort filtered data
  const sortedData = useMemo(() => {
    if (!sortConfig) return filteredData;

    return [...filteredData].sort((a, b) => {
      const aVal = a[sortConfig.key];
      const bVal = b[sortConfig.key];

      if (aVal === bVal) return 0;
      if (aVal === null || aVal === undefined) return 1;
      if (bVal === null || bVal === undefined) return -1;

      if (typeof aVal === 'number' && typeof bVal === 'number') {
        return sortConfig.direction === 'asc' ? aVal - bVal : bVal - aVal;
      }

      const aString = String(aVal).toLowerCase();
      const bString = String(bVal).toLowerCase();

      if (aString < bString) return sortConfig.direction === 'asc' ? -1 : 1;
      if (aString > bString) return sortConfig.direction === 'asc' ? 1 : -1;
      return 0;
    });
  }, [filteredData, sortConfig]);

  // Pagination calculation
  const totalItems = sortedData.length;
  const totalPages = Math.max(1, Math.ceil(totalItems / perPage));
  const currentSafePage = Math.min(currentPage, totalPages);

  const paginatedData = useMemo(() => {
    const startIndex = (currentSafePage - 1) * perPage;
    return sortedData.slice(startIndex, startIndex + perPage);
  }, [sortedData, currentSafePage, perPage]);

  // Handle column header click for sorting
  const handleSort = (columnKey: string, sortable?: boolean) => {
    if (!sortable) return;

    setSortConfig((prev) => {
      if (prev?.key === columnKey) {
        if (prev.direction === 'asc') {
          return { key: columnKey, direction: 'desc' };
        }
        return null; // Reset sort
      }
      return { key: columnKey, direction: 'asc' };
    });
  };

  const getSortIcon = (columnKey: string, sortable?: boolean) => {
    if (!sortable) return null;
    if (sortConfig?.key === columnKey) {
      return sortConfig.direction === 'asc' ? (
        <ArrowUp size={14} className="text-primary" />
      ) : (
        <ArrowDown size={14} className="text-primary" />
      );
    }
    return <ArrowUpDown size={14} className="text-muted" />;
  };

  const startIndex = totalItems === 0 ? 0 : (currentSafePage - 1) * perPage + 1;
  const endIndex = Math.min(currentSafePage * perPage, totalItems);

  return (
    <div className="table-card">
      {(title || searchPlaceholder || headerActions) && (
        <div className="table-header-bar">
          <div className="table-title-group">
            {title && <h3>{title}</h3>}
            {subtitle && <p>{subtitle}</p>}
          </div>

          <div className="table-controls">
            <div className="table-search-input">
              <Search size={16} className="text-secondary" />
              <input
                type="text"
                placeholder={searchPlaceholder}
                value={searchTerm}
                onChange={(e) => {
                  setSearchTerm(e.target.value);
                  setCurrentPage(1);
                }}
              />
            </div>

            {headerActions}
          </div>
        </div>
      )}

      <div className="table-wrapper">
        <table className="data-table">
          <thead>
            <tr>
              {columns.map((col) => (
                <th
                  key={String(col.key)}
                  onClick={() => handleSort(String(col.key), col.sortable)}
                  className={col.sortable ? 'sortable' : ''}
                  style={{
                    width: col.width,
                    textAlign: col.align || 'left',
                  }}
                >
                  <div
                    className="th-content"
                    style={{
                      justifyContent:
                        col.align === 'right'
                          ? 'flex-end'
                          : col.align === 'center'
                          ? 'center'
                          : 'flex-start',
                    }}
                  >
                    <span>{col.header}</span>
                    {getSortIcon(String(col.key), col.sortable)}
                  </div>
                </th>
              ))}
              {actions && (
                <th style={{ textAlign: 'right', width: '120px' }}>Actions</th>
              )}
            </tr>
          </thead>

          <tbody>
            {isLoading ? (
              <tr>
                <td
                  colSpan={columns.length + (actions ? 1 : 0)}
                  className="table-empty-state"
                >
                  <Loader2 size={32} className="spinner" style={{ color: 'var(--primary)' }} />
                  <p style={{ color: 'var(--text-secondary)', marginTop: '0.5rem' }}>
                    Loading records...
                  </p>
                </td>
              </tr>
            ) : paginatedData.length === 0 ? (
              <tr>
                <td
                  colSpan={columns.length + (actions ? 1 : 0)}
                  className="table-empty-state"
                >
                  <div className="empty-state-icon">
                    <EmptyIcon size={24} />
                  </div>
                  <h4 style={{ fontWeight: 600, color: 'var(--text-main)' }}>
                    No Records Found
                  </h4>
                  <p style={{ color: 'var(--text-secondary)', fontSize: '0.8125rem' }}>
                    {emptyMessage}
                  </p>
                </td>
              </tr>
            ) : (
              paginatedData.map((row, rowIndex) => (
                <tr
                  key={String(keyExtractor(row))}
                  onClick={() => onRowClick && onRowClick(row)}
                  style={{ cursor: onRowClick ? 'pointer' : 'default' }}
                >
                  {columns.map((col) => {
                    const cellValue = row[col.key as string];
                    return (
                      <td
                        key={String(col.key)}
                        style={{ textAlign: col.align || 'left' }}
                      >
                        {col.render
                          ? col.render(row, rowIndex)
                          : cellValue !== undefined && cellValue !== null
                          ? String(cellValue)
                          : '—'}
                      </td>
                    );
                  })}

                  {actions && (
                    <td
                      style={{ textAlign: 'right' }}
                      onClick={(e) => e.stopPropagation()}
                    >
                      {actions(row)}
                    </td>
                  )}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <div className="table-footer-bar">
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <span>
            Showing <strong>{startIndex}</strong> to <strong>{endIndex}</strong> of{' '}
            <strong>{totalItems}</strong> entries
          </span>

          {pageSizeOptions && pageSizeOptions.length > 1 && (
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
              <span>Show</span>
              <select
                value={perPage}
                onChange={(e) => {
                  setPerPage(Number(e.target.value));
                  setCurrentPage(1);
                }}
                style={{
                  padding: '0.2rem 0.5rem',
                  borderRadius: 'var(--radius-sm)',
                  border: '1px solid var(--border-color)',
                  fontSize: '0.8125rem',
                  outline: 'none',
                }}
              >
                {pageSizeOptions.map((opt) => (
                  <option key={opt} value={opt}>
                    {opt}
                  </option>
                ))}
              </select>
            </div>
          )}
        </div>

        <div className="pagination-controls">
          <button
            type="button"
            className="page-btn"
            disabled={currentSafePage <= 1}
            onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
            aria-label="Previous page"
          >
            <ChevronLeft size={16} />
          </button>

          {Array.from({ length: totalPages }, (_, i) => i + 1)
            .filter((p) => {
              if (totalPages <= 5) return true;
              return (
                p === 1 ||
                p === totalPages ||
                Math.abs(p - currentSafePage) <= 1
              );
            })
            .map((p, idx, arr) => {
              const prev = arr[idx - 1];
              const showEllipsis = prev && p - prev > 1;

              return (
                <React.Fragment key={p}>
                  {showEllipsis && <span style={{ padding: '0 0.25rem' }}>…</span>}
                  <button
                    type="button"
                    className={`page-btn ${p === currentSafePage ? 'active' : ''}`}
                    onClick={() => setCurrentPage(p)}
                  >
                    {p}
                  </button>
                </React.Fragment>
              );
            })}

          <button
            type="button"
            className="page-btn"
            disabled={currentSafePage >= totalPages}
            onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
            aria-label="Next page"
          >
            <ChevronRight size={16} />
          </button>
        </div>
      </div>
    </div>
  );
}

export default DataTable;
