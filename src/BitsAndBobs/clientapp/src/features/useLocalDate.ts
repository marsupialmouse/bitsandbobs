import { useSelector } from 'react-redux'
import { selectLocalTimeOffset } from './usercontext/userContextSlice.ts'

export interface TimeRemaining {
  total: number
  days: number
  hours: number
  minutes: number
  seconds: number
  milliseconds: number
}
export default function useLocalDate() {
  const localTimeOffset = useSelector(selectLocalTimeOffset)

  // We wrap all the dates with new Date() because the API actually passes the dates as strings, eventhough the
  // generated types have Date...
  const toLocalDate = (date: Date) =>
    localTimeOffset == 0
      ? new Date(date)
      : new Date(new Date(date).getTime() + localTimeOffset)

  const getTimeRemaining = (endDate: Date): TimeRemaining => {
    const now = new Date()
    const end = new Date(toLocalDate(endDate))
    const diff = end.getTime() - now.getTime()

    if (diff <= 0)
      return {
        total: 0,
        days: 0,
        hours: 0,
        minutes: 0,
        seconds: 0,
        milliseconds: 0,
      }

    return {
      total: diff,
      days: Math.floor(diff / (1000 * 60 * 60 * 24)),
      hours: Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60)),
      minutes: Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60)),
      seconds: Math.floor((diff % (1000 * 60)) / 1000),
      milliseconds: Math.floor(diff % 1000),
    }
  }

  const formatTimeRemaining = (
    endDate: Date | TimeRemaining | string,
    endedMessage = 'Ended'
  ) => {
    if (typeof endDate === 'string') endDate = new Date(endDate)
    const { total, days, hours, minutes, seconds } =
      'total' in endDate ? endDate : getTimeRemaining(endDate)

    if (total <= 0) return endedMessage

    if (days > 0) return `${days.toString()}d ${hours.toString()}h`

    if (hours > 0) return `${hours.toString()}h ${minutes.toString()}m`

    if (minutes > 0 && minutes < 5 && seconds > 0)
      return `${minutes.toString()}m ${seconds.toString()}s`

    if (minutes > 0) return `${minutes.toString()}m`

    if (seconds > 0) return `${seconds.toString()}s`

    return endedMessage
  }

  const formatDate = (date: Date) => toLocalDate(date).toLocaleDateString()

  const formatDateWithTime = (date: Date) => {
    const localDate = toLocalDate(date)
    return `${localDate.toLocaleDateString()} at ${localDate.toLocaleTimeString()}`
  }

  return {
    toLocalDate,
    formatDate,
    formatDateWithTime,
    formatTimeRemaining,
    getTimeRemaining,
  }
}
