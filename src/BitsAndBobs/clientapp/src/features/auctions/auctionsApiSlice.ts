import { api } from '../../api/apiSlice.ts'
import {
  AuctionImageResponse,
  CreateAuctionRequest,
  CreateAuctionResponse,
  GetAuctionsResponse,
  HttpValidationProblemDetails,
} from '../../api/ApiGenerated.ts'

export interface UploadImageRequest {
  file: File
}

const auctionsApi = api
  .enhanceEndpoints({ addTagTypes: ['Auctions', 'Auction'] })
  .injectEndpoints({
    endpoints: (builder) => ({
      getAuctions: builder.query<GetAuctionsResponse, void>({
        query: () => '/auctions',
        providesTags: ['Auctions'],
        keepUnusedDataFor: 1,
      }),
      uploadImage: builder.mutation<AuctionImageResponse, UploadImageRequest>({
        query: (request) => {
          const formData = new FormData()
          formData.append('file', request.file)
          return {
            url: '/auctions/images',
            method: 'POST',
            body: formData,
            formData: true,
          }
        },
        transformErrorResponse: (response) =>
          response.status === 400
            ? (response.data as HttpValidationProblemDetails)
            : response.data,
      }),
      createAction: builder.mutation<
        CreateAuctionResponse,
        CreateAuctionRequest
      >({
        query: (createAuctionRequest) => ({
          url: '/auctions',
          method: 'POST',
          body: createAuctionRequest,
        }),
        transformErrorResponse: (response) =>
          response.status === 400
            ? (response.data as HttpValidationProblemDetails)
            : response.data,
        invalidatesTags: ['Auctions'],
      }),
    }),
    overrideExisting: false,
  })

export const {
  useGetAuctionsQuery,
  useUploadImageMutation,
  useCreateActionMutation,
} = auctionsApi
