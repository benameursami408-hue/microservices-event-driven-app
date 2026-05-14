export function mapClientFromApi(client = {}) {
  const fullName = client.fullName || client.name || client.company || '';
  return {
    id: String(client.id),
    technicalId: client.id,
    name: fullName,
    company: client.company || fullName,
    contact: client.contact || fullName,
    email: client.email || '',
    phone: client.phoneNumber || client.phone || '',
    phoneNumber: client.phoneNumber || client.phone || '',
    location: client.address || client.location || '',
    createdAt: client.createdAt
  };
}

export function mapClientToDto(client = {}) {
  return {
    id: client.technicalId || client.id,
    fullName: client.name || client.fullName || client.contact || '',
    email: client.email || '',
    phoneNumber: client.phone || client.phoneNumber || ''
  };
}
