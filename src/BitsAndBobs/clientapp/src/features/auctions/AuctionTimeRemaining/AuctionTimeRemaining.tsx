import useLocalDate, { TimeRemaining } from '../../useLocalDate.ts'
import { useEffect, useState } from 'react'

export interface AuctionTimeRemainingProps {
  endDate: Date
}

const getUpdateDelay = (remaining: TimeRemaining) => {
  if (remaining.total == 0) return 0
  return remaining.days > 0 || remaining.hours > 0 || remaining.minutes > 6
    ? 1000 * 60
    : 1000
}

export default function AuctionTimeRemaining({
  endDate,
}: AuctionTimeRemainingProps) {
  const { formatTimeRemaining, getTimeRemaining } = useLocalDate()
  const [formattedTimeRemaining, setFormattedTimeRemaining] = useState(
    formatTimeRemaining(endDate)
  )

  useEffect(() => {
    const updateLoop = () => {
      const remaining = getTimeRemaining(endDate)
      setFormattedTimeRemaining(formatTimeRemaining(remaining))
      const delay = getUpdateDelay(remaining)
      return delay > 0 ? setTimeout(updateLoop, delay) : null
    }
    const timeout = updateLoop()
    return () => {
      if (timeout) clearTimeout(timeout)
    }
  }, [endDate, getTimeRemaining, formatTimeRemaining])

  return <>{formattedTimeRemaining}</>
}
