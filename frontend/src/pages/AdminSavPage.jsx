import AdminTeamUsers from './AdminTeamUsers.jsx'

export default function AdminSavPage() {
  return (
    <AdminTeamUsers
      roleKey="SAV"
      title="Equipe SAV"
      description="Gerez uniquement les comptes du service apres-vente pour des tests rapides et lisibles."
    />
  )
}
