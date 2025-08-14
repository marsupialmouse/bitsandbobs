# BITS & BOBS
A demo auction platform built with ASP.NET Core and React. The goal was to build a trivial application that's non-trivial enough that something more than surface-level knowledge is required.

## Features
Being a demo application, functionality is basic, limited, and fanciful:
- **User Authentication & Authorization**: Registration and account management uses ASP.NET Core Identity
- **Auctions**: Users can create, browse, and participate in auctions
- **User Dashboard**: Track auctions you're selling, participating in, or have won
- **Fake Emails**: Rather than sending emails, "emails" are stored in the database and accessed through the UI
- **Responsive Design**: Mobile-friendly interface built with React and Tailwind CSS

## Technology Stack
### Backend
- **ASP.NET** with minimal APIs
- **ASP.NET Identity** for authentication and user management
- **Amazon DynamoDB** for data storage (single table)
- **Amazon S3** for image storage
- **MassTransit** with **Amazon SQS/SNS** for async messaging

### Frontend
- **React** with **TypeScript**
- **Redux Toolkit** for state management
- **Tailwind CSS** for styling
- **Vite** for build tooling and development server

## AWS Architecture & Deployment
Deployment is automated using GitHub Actions, with the AWS stack fully defined in CloudFormation templates:
- **Frontend**: React application served from S3 through CloudFront distribution
- **Backend**: Runs on an auto-mode Amazon EKS cluster using spot instances and ALB ingress
- **Database**: DynamoDB
- **Storage**: S3 for static assets and auction images
- **CDN**: CloudFront for global content delivery and API proxying

A robot wrote most of this.
