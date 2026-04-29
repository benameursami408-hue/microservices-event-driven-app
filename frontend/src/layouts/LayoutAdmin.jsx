import PageHeader from '../components/PageHeader.jsx'

export default function LayoutAdmin({ title, description, meta, actions, children }) {
  return (
    <div className="space-y-6">
      <PageHeader eyebrow="Espace Admin" title={title} description={description} meta={meta} actions={actions} />
      {children}
    </div>
  )
}
