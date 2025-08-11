import { api } from '../../api/apiSlice.ts'
import {
  AddBidRequest,
  AddBidResponse,
  AuctionImageResponse,
  CreateAuctionRequest,
  CreateAuctionResponse,
  GetAuctionResponse,
  GetAuctionsResponse,
  GetUserAuctionsResponse,
  HttpValidationProblemDetails,
  ProblemDetails,
} from '../../api/ApiGenerated.ts'

export interface UploadImageRequest {
  file: File
}

const auctionsApi = api
  .enhanceEndpoints({ addTagTypes: ['Auctions', 'Auction'] })
  .injectEndpoints({
    endpoints: (builder) => ({
      getAuction: builder.query<GetAuctionResponse, string>({
        query: (id) => `/auctions/${id}`,
        providesTags: ['Auction'],
        keepUnusedDataFor: 1,
      }),
      getAuctions: builder.query<GetAuctionsResponse, void>({
        query: () => '/auctions',
        providesTags: ['Auctions'],
        keepUnusedDataFor: 1,
      }),
      getSellerAuctions: builder.query<GetUserAuctionsResponse, void>({
        query: () => '/auctions/seller',
        keepUnusedDataFor: 1,
      }),
      getWonAuctions: builder.query<GetUserAuctionsResponse, void>({
        query: () => '/auctions/won',
        keepUnusedDataFor: 1,
      }),
      getParticipantAuctions: builder.query<GetUserAuctionsResponse, void>({
        query: () => '/auctions/participant',
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
      createAuction: builder.mutation<
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
      addBid: builder.mutation<AddBidResponse, AddBidRequest>({
        query: (request) => ({
          url: `/auctions/${request.auctionId}/bids`,
          method: 'POST',
          body: request,
        }),
        transformErrorResponse: (response) =>
          response.status === 400
            ? (response.data as ProblemDetails)
            : response.data,
        invalidatesTags: ['Auctions', 'Auction'],
      }),
      cancelAuction: builder.mutation<void, string>({
        query: (id) => ({
          url: `/auctions/${id}/cancel`,
          method: 'POST',
        }),
        transformErrorResponse: (response) =>
          response.status === 400
            ? (response.data as ProblemDetails)
            : response.data,
        invalidatesTags: ['Auctions', 'Auction'],
      }),
    }),
    overrideExisting: false,
  })

export const {
  useGetAuctionQuery,
  useGetAuctionsQuery,
  useGetParticipantAuctionsQuery,
  useGetSellerAuctionsQuery,
  useGetWonAuctionsQuery,
  useUploadImageMutation,
  useCreateAuctionMutation,
  useCancelAuctionMutation,
  useAddBidMutation,
} = auctionsApi
