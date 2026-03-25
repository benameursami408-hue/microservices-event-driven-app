const KEY = 'read_notifications'

function load() {
  try {
    const raw = localStorage.getItem(KEY)
    const parsed = raw ? JSON.parse(raw) : []
    return new Set(Array.isArray(parsed) ? parsed : [])
  } catch {
    return new Set()
  }
}

function save(set) {
  try {
    localStorage.setItem(KEY, JSON.stringify(Array.from(set)))
  } catch {
    // ignore
  }
}

export function isNotificationRead(id) {
  return load().has(id)
}

export function markNotificationRead(id) {
  const set = load()
  set.add(id)
  save(set)
}

export function markAllNotificationsRead(ids) {
  const set = load()
  for (const id of ids) set.add(id)
  save(set)
}
