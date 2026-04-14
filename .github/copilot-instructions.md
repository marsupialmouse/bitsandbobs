# Copilot instructions for Bits & Bobs repository

Purpose
- Provide concise, actionable guidance for Copilot/Copilot CLI sessions so assistants can bootstrap quickly and accurately.

1) Build, test, and lint commands
- Backend (ASP.NET Core):
  - Build: cd src && dotnet build
  - Run: cd src && dotnet run
  - Test: use `dotnet test <project>` to run all tests; run a single test project: `dotnet test <project> --filter "FullyQualifiedName=Namespace.Class.TestName"`

- Frontend (React + Vite) located at src/BitsAndBobs/clientapp:
  - Install dependencies: prefer pnpm (pnpm install) but npm/yarn also work
  - Dev server: cd src/BitsAndBobs/clientapp && pnpm run dev
  - Build: cd src/BitsAndBobs/clientapp && pnpm run build
  - Preview: pnpm run preview
  - Lint: pnpm run lint
  - Format: pnpm run format
  - Test (Vitest): pnpm run test
    - Run a single test file: pnpm run test -- src/path/to/file.test.ts
    - Run a single test by name: pnpm run test -- -t "exact test name"

2) High-level architecture (big picture)
- Monorepo layout: code lives under src/. Key folders:
  - BitsAndBobs.AppHost / API projects: ASP.NET minimal APIs, Identity, DynamoDB-backed storage
  - clientapp: React + TypeScript + Vite front-end served separately during development and deployed to S3/CloudFront in production
  - Infrastructure: CloudFormation templates and deployment scripts
  - CI: GitHub Actions workflows build/test both frontend and backend and run CFN linting

- Runtime/Deployment:
  - Frontend built with Vite, served from S3 + CloudFront
  - Backend runs on EKS (Helm + ALB) with DynamoDB and S3 for storage
  - Messaging via MassTransit with SQS/SNS
  - Release workflow packages apps, CFN, and deploys to EKS

3) Key conventions (repo-specific)
- Clientapp uses TypeScript + React 19 + Redux Toolkit and stores sources in src/BitsAndBobs/clientapp
- Linting and formatting are enforced via lint-staged; note: clientapp lint-staged currently references `yarn lint` and `yarn format` in package.json — update to `pnpm run` or use npm-compatible commands when switching package managers
- Node & tool versions: README recommends Node.js 22.x; .NET 10 SDK is required for backend
- MCP server: repo includes an MCP server exposing /mcp endpoints; authentication uses JWT tokens and the README includes example MCP server config
- CI expects specific secrets and variables for AWS deployment (see README section "AWS Deployment")

4) Where to look first (useful entry points)
- Frontend: src/BitsAndBobs/clientapp/package.json and src/BitsAndBobs/clientapp/vite.config.ts
- Backend: src/**/Program.cs and BitsAndBobs.AppHost for auth and host setup
- Infrastructure: src/Infrastructure (CloudFormation templates and bootstrap scripts)
- CI: .github/workflows (build/test/release workflows)

5) AI assistant integration notes
- If modifying package manager scripts, update lint-staged, CI workflow steps, and README usage examples
- MCP server credentials are stored via user-secrets for local dev; README shows how to request an insecure long-lived token for MCP

Files referenced from README that assistants should inspect: 
- src/BitsAndBobs/clientapp/package.json
- src/BitsAndBobs/clientapp/yarn.lock (if present) and pnpm-lock.yaml (if migrating)
- .github/workflows/* (CI changes needed when switching package manager)

Created-by: Copilot CLI
