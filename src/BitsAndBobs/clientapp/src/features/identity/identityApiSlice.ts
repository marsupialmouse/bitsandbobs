import { api } from '../../api/apiSlice'
import {
  HttpValidationProblemDetails,
  InfoResponse,
  LoginRequest,
  ProblemDetails,
  RegisterRequest,
} from '../../api/ApiGenerated.ts'

export interface ConfirmEmailRequest {
  userId: string
  code: string
  changedEmail?: string
}

export interface ConfirmEmailResponse {
  changedEmail?: string
}

export interface LoginResponse {
  emailAddress: string
}

export const identityApi = api
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
      confirmEmail: builder.mutation<ConfirmEmailResponse, ConfirmEmailRequest>(
        {
          query: (changedEmailParams) => ({
            url: '/identity/confirmEmail',
            params: changedEmailParams,
            method: 'GET',
            responseHandler: 'text',
          }),
          transformResponse: (_, __, arg) => {
            return { changedEmail: arg.changedEmail }
          },
          invalidatesTags: ['Identity'],
        }
      ),
      login: builder.mutation<LoginResponse, LoginRequest>({
        query: (loginRequest) => ({
          url: '/identity/login',
          params: { useCookies: true },
          method: 'POST',
          body: loginRequest,
        }),
        transformResponse: (_, __, arg) => {
          return { emailAddress: arg.email ?? '' }
        },
        transformErrorResponse: (response) => response.data as ProblemDetails,
        invalidatesTags: ['Identity'],
      }),
      logout: builder.mutation<void, void>({
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
  useLoginMutation,
  useLogoutMutation,
  useGetInfoQuery,
} = identityApi
