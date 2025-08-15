import { ChangeEvent, useState } from 'react'
import { HttpValidationProblemDetails } from '../../../api/ApiGenerated'
import { useUploadImageMutation } from '../auctionsApiSlice.ts'

interface ImageUploadProps {
  onImageUploaded: (imageId: string) => void
  onImageRemoved: () => void
  error?: string
}

export default function ImageUpload({
  onImageUploaded,
  onImageRemoved,
  error,
}: ImageUploadProps) {
  const [uploadImage] = useUploadImageMutation()
  const [imagePreview, setImagePreview] = useState<string | null>(null)
  const [isUploading, setIsUploading] = useState(false)
  const [uploadError, setUploadError] = useState<string | null>(null)
  const displayError = uploadError ?? error

  const handleFileSelect = async (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    if (file.size > 1024 * 1024) {
      setUploadError('Image must be smaller than 1MB')
      return
    }

    setUploadError(null)
    setIsUploading(true)

    try {
      const result = await uploadImage({ file }).unwrap()
      onImageUploaded(result.id)
      setImagePreview(URL.createObjectURL(file))
    } catch (error) {
      const validationErrors = error as HttpValidationProblemDetails
      if (validationErrors.errors?.image) {
        setUploadError(validationErrors.errors.image[0])
      } else {
        setUploadError('Failed to upload image. Please try again.')
      }
    } finally {
      setIsUploading(false)
      e.target.value = '' // Reset file input so the file isn't uploaded with the form
    }
  }

  const handleRemoveImage = () => {
    if (imagePreview) {
      URL.revokeObjectURL(imagePreview)
      setImagePreview(null)
    }
    setUploadError(null)
    onImageRemoved()
  }

  return (
    <div>
      <label className="mb-1 block text-sm font-medium text-gray-700">
        Auction Image
      </label>

      {!imagePreview ? (
        <div className="relative">
          <div>
            <label className="flex cursor-pointer justify-center rounded-md border-2 border-dashed border-gray-300 px-6 pt-5 pb-6 hover:border-gray-400">
              <div className="space-y-1 text-center">
                <svg
                  className="mx-auto h-12 w-12 text-gray-400"
                  stroke="currentColor"
                  fill="none"
                  viewBox="0 0 48 48"
                >
                  <path
                    d="M28 8H12a4 4 0 00-4 4v20m32-12v8m0 0v8a4 4 0 01-4 4H12a4 4 0 01-4-4v-4m32-4l-3.172-3.172a4 4 0 00-5.656 0L28 28M8 32l9.172-9.172a4 4 0 015.656 0L28 28m0 0l4 4m4-24h8m-4-4v8m-12 4h.02"
                    strokeWidth={2}
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  />
                </svg>
                <div className="text-sm text-gray-600">
                  <span className="font-medium text-indigo-600 hover:text-indigo-500">
                    Upload an image
                  </span>
                </div>
                <p className="text-xs text-gray-500">
                  JPEG, PNG, WebP up to 1MB
                </p>
              </div>
              <input
                type="file"
                className="sr-only"
                accept="image/jpeg,image/png,image/webp"
                onChange={(e) => {
                  void handleFileSelect(e)
                }}
                data-testid="image-upload-input"
                disabled={isUploading}
              />
            </label>
          </div>
          {isUploading && (
            <div className="absolute inset-2 flex items-center justify-center rounded-md bg-white/75">
              <div
                className="h-6 w-6 animate-spin rounded-full border-t-2 border-indigo-700"
                role="status"
              />
            </div>
          )}
        </div>
      ) : (
        <div className="relative">
          <img
            src={imagePreview}
            alt="Preview"
            className="h-48 w-full rounded-md object-cover"
          />
          <button
            type="button"
            onClick={handleRemoveImage}
            className="absolute top-2 right-2 rounded-full bg-red-600 p-1 text-white hover:bg-red-700"
            data-testid="image-upload-remove-button"
          >
            <svg
              className="h-4 w-4"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M6 18L18 6M6 6l12 12"
              />
            </svg>
          </button>
        </div>
      )}

      {displayError && (
        <p className="mt-1 text-sm text-red-600">{displayError}</p>
      )}
    </div>
  )
}
