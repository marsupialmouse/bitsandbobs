#!/usr/bin/env pwsh
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Development", "Staging", "Production")]
    [string]$Environment,

    [Parameter(Mandatory = $true)]
    [string]$AwsProfile,

    [switch]$CreateEksCluster,
    [switch]$DeployHelm
)

$ErrorActionPreference = "Stop"
$ScriptPath = $PSScriptRoot
$PackagedTemplate = Join-Path $ScriptPath "cfn/bitsandbobs.packaged.yaml"

function Get-EksLoadBalancerInfo {
    $lbDomainName = kubectl get ingress bitsandbobs -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>$null
    if ([string]::IsNullOrWhiteSpace($lbDomainName)) {
        return @{ DomainName = ""; Arn = "" }
    }

    $lbArn = aws elbv2 describe-load-balancers `
        --query "LoadBalancers[?DNSName=='$lbDomainName'].LoadBalancerArn" `
        --output text `
        --profile $AwsProfile 2>$null

    return @{
        DomainName = $lbDomainName
        Arn = if ([string]::IsNullOrWhiteSpace($lbArn)) { "" } else { $lbArn }
    }
}

function Invoke-CfnDeploy {
    param($LbArn = "", $LbDomainName = "")

    $parameters = "Environment=$Environment", "CreateEksCluster=$($CreateEksCluster.ToString().ToLower())"
    if ($LbArn) { $parameters += "EksLoadBalancerArn=$LbArn" }
    if ($LbDomainName) { $parameters += "EksLoadBalancerDomainName=$LbDomainName" }

    aws cloudformation deploy `
        --template-file $PackagedTemplate `
        --stack-name "BitsAndBobs-$Environment" `
        --parameter-overrides $parameters `
        --profile $AwsProfile
}

try {
    # Get existing LB info if we have an EKS cluster
    $lbInfo = @{ DomainName = ""; Arn = "" }
    if ($CreateEksCluster) {
        $clusterName = kubectl config view --minify -o jsonpath='{.clusters[0].name}' 2>$null | ForEach-Object { $_.Split('/')[-1] }
        if ($clusterName -eq "EksCluster-$Environment") {
            $lbInfo = Get-EksLoadBalancerInfo
        }
    }

    # Package template
    aws cloudformation package `
        --template-file (Join-Path $ScriptPath "cfn/bitsandbobs.yaml") `
        --s3-bucket "marsupialmouse-bitsandbobs-cfn" `
        --s3-prefix "local" `
        --output-template-file $PackagedTemplate `
        --force-upload `
        --profile $AwsProfile

    # Deploy stack
    Invoke-CfnDeploy -LbArn $lbInfo.Arn -LbDomainName $lbInfo.DomainName

    # Deploy Helm if requested
    if ($CreateEksCluster -and $DeployHelm) {
        aws eks update-kubeconfig --region "ap-southeast-2" --name "EksCluster-$Environment" --profile $AwsProfile
        helm upgrade bitsandbobs (Join-Path $ScriptPath "helm/bitsandbobs") --install --wait

        # Check if LB info changed and redeploy
        $existingLbInfo = $lbInfo
        $lbInfo = Get-EksLoadBalancerInfo

        if ($lbInfo.Arn -and $lbInfo.Arn -ne $existingLbInfo.Arn) {
            Invoke-CfnDeploy -LbArn $lbInfo.Arn -LbDomainName $lbInfo.DomainName
        }
    }
}
finally {
    if (Test-Path $PackagedTemplate) {
        Remove-Item $PackagedTemplate -Force
    }
}
