import PageHeader from '../components/PageHeader.jsx'

export default function LayoutSAV({ title, description, meta, actions, children }) {
  return (
    <div className="space-y-6">
      <PageHeader eyebrow="Espace SAV" title={title} description={description} meta={meta} actions={actions} />
      {children}
    </div>
  )
}
