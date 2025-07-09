import { api } from '../../api/apiSlice'
import { EmailResponse } from '../../api/ApiGenerated.ts'

const emailApi = api
  .enhanceEndpoints({ addTagTypes: ['Emails'] })
  .injectEndpoints({
    endpoints: (builder) => ({
      getRecentEmails: builder.query<EmailResponse[], string>({
        query: (emailAddress) => `/emails/recent/${emailAddress}`,
        providesTags: ['Emails'],
      }),
      getRecentUserEmails: builder.query<EmailResponse[], void>({
        query: () => '/emails/recent',
        providesTags: ['Emails'],
      }),
    }),
    overrideExisting: false,
  })

export const { useGetRecentEmailsQuery, useGetRecentUserEmailsQuery } = emailApi
