export interface ErrorMessageProps {
  message: string
}

export default function ErrorMessage({ message }: ErrorMessageProps) {
  return (
    <div className="flex justify-center pt-16 text-red-600 before:pr-2 before:content-['⚠️']">
      {message}
    </div>
  )
}
