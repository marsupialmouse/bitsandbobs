export interface NoContentProps {
  message: string
}

export default function NoContent({ message }: NoContentProps) {
  return (
    <div className="flex justify-center pt-16 text-gray-600">{message}</div>
  )
}
