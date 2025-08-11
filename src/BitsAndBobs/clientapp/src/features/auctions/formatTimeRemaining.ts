export default function formatTimeRemaining(endDate: Date): string {
  const now = new Date()
  const end = new Date(endDate)
  const diff = end.getTime() - now.getTime()

  if (diff <= 0) return 'Ended'

  const days = Math.floor(diff / (1000 * 60 * 60 * 24))
  const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60))
  const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60))

  if (days > 0) return `${days.toString()}d ${hours.toString()}h`
  if (hours > 0) return `${hours.toString()}h ${minutes.toString()}m`
  return `${minutes.toString()}m`
}
