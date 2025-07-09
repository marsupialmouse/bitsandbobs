export default function Loading() {
  return (
    <div className="flex min-h-screen items-center justify-center">
      <div
        className="h-6 w-6 animate-spin rounded-full border-t-2 border-indigo-700"
        role="status"
      />
    </div>
  )
}
