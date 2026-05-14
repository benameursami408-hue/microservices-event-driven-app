import { Badge, EmptyState } from '../ui';

export function DataTable({ rows, columns, actions, emptyTitle = 'No data', emptyMessage, onRowClick, selectedId }) {
  if (!rows.length) {
    return <EmptyState title={emptyTitle} message={emptyMessage} />;
  }

  return (
    <div className="table-wrap">
      <table className="data-table">
        <thead>
          <tr>
            {columns.map(column => <th key={column.key}>{column.label}</th>)}
            {actions && <th className="actions-col">Actions</th>}
          </tr>
        </thead>
        <tbody>
          {rows.map(row => (
            <tr
              key={row.id}
              className={selectedId === row.id ? 'selected' : ''}
              onClick={() => onRowClick?.(row)}
            >
              {columns.map(column => (
                <td key={column.key} data-label={column.label}>
                  {renderCell(row, column)}
                </td>
              ))}
              {actions && (
                <td className="row-actions" data-label="Actions" onClick={event => event.stopPropagation()}>
                  {actions(row)}
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function renderCell(row, column) {
  if (column.render) return column.render(row);
  const value = row[column.key];
  if (column.type === 'badge') return <Badge>{value}</Badge>;
  if (column.type === 'date') return column.format ? column.format(value) : value || '-';
  return value || '-';
}
