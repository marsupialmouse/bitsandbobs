import { api } from '../../api/apiSlice'
import {
  DetailsRequest,
  DetailsResponse,
  ForgotPasswordRequest,
  HttpValidationProblemDetails,
  LoginRequest,
  ProblemDetails,
  RegisterRequest,
  ResetPasswordRequest,
} from '../../api/ApiGenerated.ts'

export interface ChangeEmailRequest {
  newEmail: string
}

export interface ChangePasswordRequest {
  oldPassword: string
  newPassword: string
}

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
      changeEmail: builder.mutation<void, ChangeEmailRequest>({
        query: (changeEmailRequest) => ({
          url: '/identity/manage/info',
          method: 'POST',
          body: changeEmailRequest,
        }),
        transformErrorResponse: (response) =>
          response.status === 400
            ? (response.data as HttpValidationProblemDetails)
            : response.data,
      }),
      changePassword: builder.mutation<void, ChangePasswordRequest>({
        query: (changePasswordRequest) => ({
          url: '/identity/manage/info',
          method: 'POST',
          body: changePasswordRequest,
        }),
        transformErrorResponse: (response) =>
          response.status === 400
            ? (response.data as HttpValidationProblemDetails)
            : response.data,
      }),
      forgotPassword: builder.mutation<void, ForgotPasswordRequest>({
        query: (forgotPasswordRequest) => ({
          url: '/identity/forgotPassword',
          method: 'POST',
          body: forgotPasswordRequest,
        }),
      }),
      resetPassword: builder.mutation<void, ResetPasswordRequest>({
        query: (resetPasswordRequest) => ({
          url: '/identity/resetPassword',
          method: 'POST',
          body: resetPasswordRequest,
        }),
        transformErrorResponse: (response) =>
          response.status === 400
            ? (response.data as HttpValidationProblemDetails)
            : response.data,
      }),
      logout: builder.mutation<void, void>({
        query: () => ({
          url: '/identity/logout',
          method: 'POST',
        }),
        invalidatesTags: ['Identity'],
      }),
      getDetails: builder.query<DetailsResponse, void>({
        query: () => '/identity/details',
        providesTags: ['Identity'],
      }),
      updateDetails: builder.mutation<void, DetailsRequest>({
        query: (detailsRequest) => ({
          url: '/identity/details',
          method: 'POST',
          body: detailsRequest,
        }),
        transformErrorResponse: (response) =>
          response.status === 400
            ? (response.data as HttpValidationProblemDetails)
            : response.data,
        invalidatesTags: ['Identity'],
      }),
    }),
    overrideExisting: false,
  })

export const {
  useRegisterMutation,
  useConfirmEmailMutation,
  useLoginMutation,
  useLogoutMutation,
  useChangeEmailMutation,
  useChangePasswordMutation,
  useForgotPasswordMutation,
  useResetPasswordMutation,
  useGetDetailsQuery,
  useUpdateDetailsMutation,
} = identityApi
