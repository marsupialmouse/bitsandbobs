#!/usr/bin/env pwsh
<#
    .SYNOPSIS
        Deploys the bootstrap CFN template
    .DESCRIPTION
        The bootstrap template includes the OIDC provider, S3
        bucket and roles for deploying from GitHub actions, as
        well as shared EKS auto-mode cluster roles.
#>

param(
    # The name of the AWS profile to use
    [Parameter(Mandatory = $true)]
    [Alias("p")]
    [string]
    $AwsProfile,

    # The name of the GitHub account for the OIDC provider (default behaviour is to divine this from git config)
    [Alias("o")]
    [string]
    $GitHubOrganization,

    # The prefix for the CFN bucket name (by default $GitHubOrganization is used)
    [Alias("b")]
    [string]
    $BucketNamePrefix
)

if ([string]::IsNullOrWhiteSpace($GitHubOrganization)) {
    $match = git config --list | Select-String -Pattern "^user\.email=\d+\+([^@]+)@users\.noreply\.github\.com$"
    if ($match -eq $null)
    {
        throw "Unable to find GitHub username; use the -o option."
    }
    $GitHubOrganization = $match.Matches[0].Groups[1]
}

if ([string]::IsNullOrWhiteSpace($BucketNamePrefix))
{
    $BucketNamePrefix = $GitHubOrganization
}

$parameters = @(
    "GitHubOrganization=$GitHubOrganization"
    "BucketNamePrefix=$BucketNamePrefix"
)

aws cloudformation deploy `
    --template-file ./cfn/bootstrap.yaml `
    --stack-name BitsAndBobs-Bootstrap `
    --capabilities CAPABILITY_NAMED_IAM `
    --parameter-overrides $parameters `
    --profile $AwsProfile
