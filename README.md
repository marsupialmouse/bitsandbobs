# BITS & BOBS
A demo auction platform built with ASP.NET Core and React. The goal was to build a trivial application that's non-trivial enough to gain something more than surface-level knowledge.

## Features
Being a demo application, functionality is basic, limited, and fanciful:
- **User Authentication & Authorization**: Registration and account management uses ASP.NET Core Identity
- **Auctions**: Users can create, browse, and participate in auctions
- **User Dashboard**: Track auctions you're selling, participating in, or have won
- **Fake Emails**: Rather than sending emails, "emails" are stored in the database and accessed through the UI
- **Responsive Design**: Mobile-friendly interface built with React and Tailwind CSS
- **MCP Server**: Includes an MCP server with basic auction tools

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

## AWS Architecture
The AWS stack fully defined in CloudFormation templates:
- **Frontend**: React application served from S3 through CloudFront distribution
- **Backend**: Runs on an auto-mode Amazon EKS cluster using spot instances and ALB ingress, served through the same CloudFront distribution
- **Database**: DynamoDB
- **Storage**: S3 for static assets and auction images
- **Messaging**: A combination of SQS and SNS
- **CDN**: CloudFront for global content delivery and API proxying

## Deployment / GitHib Actions
The project includes CI workflows for:
- Building and testing the UI
- Building and testing the API
- Linting the CFN templates

In addition, a release workflow:
- Builds and packages the apps
- Packages and deploys the CFN templates
- Deploys the Helm chart to the EKS cluster
- Versions the code

## Getting Started
### Prerequisites
- .NET 10.0 SDK
- Node.js 22.x
- AWS Account (for deployment)
- AWS CLI configured with a profile called "dev"

### Local Development
Thanks to .NET Aspire, getting started with local development is relatively straightforward:
1. **Clone the repository**
``` bash
   git clone https://github.com/marsupialmouse/bitsandbobs.git
```
2. **Application Setup**
``` bash
   cd bitsandbobs/src
   dotnet build
```
3. **Host Setup**

   Set a value to use as the name prefix for the auction images S3 bucket (GitHub username does the trick):
``` bash
   cd BitsAndBobs.AppHost
   dotnet user-secrets set "Aws.BucketNamePrefix" "[your-github-username]"
```
4. **Run**
``` bash
   dotnet run
```
### MCP Server
The MCP server exposes basic tools for listing auctions, getting auction details and bidding on auctions. Some tools are user-specific, so the server relies on JWT bearer tokens. To get a totally-insecure long-lived token, sign in, go to the profile page, and click the "Get MCP Token" button. You can then include the token in your MCP config:
``` json
{
  "mcpServers": {
    "BitsAndBobs": {
      "url": "http://localhost:5135/mcp",
      "headers": {
        "Authorization": "Bearer [insert your token here]"
      }
    }
  }
}
```

### AWS Deployment
This convoluted process is only required when you first configure deployment, after that you can simply run the Release workflow. However, if you destroy and re-create the EKS cluster (leaving it running costs money), you will need to re-run steps 6 and 7 each time you re-create the cluster.

1. **Bootstrap Infrastructure**

   These are resources and roles that are shared or needed for deployment from GitHub actions. The `GitHubOrganization` parameter must match the name of the GitHub account from which you intend to deploy.
``` bash
   ./src/Infrastructure/bootstrap-deploy.ps1 -AwsProfile [YouProfileName] -GitHubOrganization [YourAccountName]
```
2. **Set Secrets/Variable**

   Using the outputs from the Bootstrap stack, set the following secrets in GitHub Actions:
    - `AWS_OIDC_ARN`
    - `AWS_ROLE_DEPLOY_ASSSETS_ARN`
    - `AWS_ROLE_DEPLOY_CFN_ARN`
    - `AWS_ROLE_DEPLOY_VERSION_ARN`

   And variables:
    - `AWS_DEFAULT_REGION`
    - `S3_BUCKET_CFN`

   Create a secret called `JWT_SECRET` with a random string (this is used as the secret for signing JWTs).

   If you'd like to be able to debug the EKS cluster using `kubectl` locally, create an `EKS_ADMIN_ARN` secret using the ARN of your IAM user.


3. **Run Release Workflow**

   The first time the release workflow is run, be sure that the only options selected are "Enable CloudFront Distribution" and "Only deploy the CFN stack". Some other steps in the workflow use resources that form part of the CFN stack, and so can only be run when the stack has been created.


4. **Set More Secrets/Variables**

   Open the CloudFormation console and the GitHub secrets/variables for the envionment (e.g. Production) to which you're deploying:
    - Create a `CF_KVS_ARN` secret using the `KeyValueStoreArn` output from the CDN stack
    - Create a `CF_DOMAIN_NAME` variable using the `DomainName` output from the CDN stack
    - Create a `S3_BUCKET_APP` variable using the `AppBucketName` output from the Storage stack


5. **Run Release Workflow Again**

   This time enable both the EKS cluster and the CloudFront distribution


6. **Set More Secrets**

   Using the output of the "Get load balancer details" step of the "Release" job, set `EKS_LB_ARN` and `EKS_LB_DOMAIN_NAME` GitHub Actions secrets for the environment.


7. **Run Release Workflow Again**

   This is required to update the CloudFront Distribution with the details of the EKS Auto Mode ALB, which is only created when the Helm Chart is deployed to the EKS cluster.


8. **Open the CF domain in your browser and enjoy**

