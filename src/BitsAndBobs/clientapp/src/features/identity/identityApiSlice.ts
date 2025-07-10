import { api } from '../../api/apiSlice'
import {
  HttpValidationProblemDetails,
  InfoResponse,
  LoginRequest,
  RegisterRequest,
} from '../../api/ApiGenerated.ts'

export interface ConfirmEmailRequest {
  userId: string
  code: string
  changedEmail?: string
}

const identityApi = api
  .enhanceEndpoints({ addTagTypes: ['Identity'] })
  .injectEndpoints({
    endpoints: (builder) => ({
      register: builder.mutation<void, RegisterRequest>({
        query: (registerRequest) => ({
          url: '/identity/register',
          method: 'POST',
          body: registerRequest,
        }),
        transformErrorResponse: (response) =>
          response.status === 400
            ? (response.data as HttpValidationProblemDetails)
            : response.data,
        invalidatesTags: ['Identity'],
      }),
      confirmEmail: builder.mutation<void, ConfirmEmailRequest>({
        query: (changedEmailParams) => ({
          url: '/identity/confirmEmail',
          params: changedEmailParams,
          method: 'GET',
          responseHandler: 'text',
        }),
        invalidatesTags: ['Identity'],
      }),
      login: builder.mutation<void, LoginRequest>({
        query: (loginRequest) => ({
          url: '/identity/login',
          params: { useCookies: true },
          method: 'POST',
          body: loginRequest,
        }),
        transformErrorResponse: (response) =>
          response.status === 400
            ? (response.data as HttpValidationProblemDetails)
            : response.data,
        invalidatesTags: ['Identity'],
      }),
      logout: builder.mutation({
        query: () => ({
          url: '/identity/signout',
          method: 'POST',
        }),
        invalidatesTags: ['Identity'],
      }),
      getInfo: builder.query<InfoResponse, void>({
        query: () => '/identity/info',
        providesTags: ['Identity'],
      }),
    }),
    overrideExisting: false,
  })

export const {
  useRegisterMutation,
  useConfirmEmailMutation,
  useGetInfoQuery,
  useLogoutMutation,
} = identityApi
