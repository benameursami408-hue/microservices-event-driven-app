import clsx from 'clsx'

export default function DataTable({
  columns,
  rows,
  getRowKey,
  renderMobileCard,
  emptyFallback = null,
  className,
}) {
  if (!rows?.length) {
    return emptyFallback
  }

  return (
    <div className={clsx('table-shell', className)}>
      <div className="md:hidden">
        <div className="divide-y divide-slate-200">
          {rows.map((row) => (
            <div key={getRowKey(row)} className="p-4">
              {renderMobileCard ? (
                renderMobileCard(row)
              ) : (
                <div className="space-y-3">
                  {columns.map((column) => (
                    <div key={column.key} className="space-y-1">
                      <div className="text-[11px] font-extrabold uppercase tracking-[0.18em] text-slate-500">{column.header}</div>
                      <div className="text-sm text-slate-700">{column.render(row)}</div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          ))}
        </div>
      </div>

      <div className="hidden overflow-x-auto md:block">
        <table className="table-modern">
          <thead>
            <tr>
              {columns.map((column) => (
                <th key={column.key} className={column.headerClassName}>
                  {column.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={getRowKey(row)}>
                {columns.map((column) => (
                  <td key={column.key} className={column.cellClassName}>
                    {column.render(row)}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
