#!/bin/bash
#
# Packages and deploys the bitsandbobs CFN template for local testing.
#

SCRIPT_PATH="${BASH_SOURCE[0]}"
DEPLOY_ENVIRONMENT="$1"
CREATE_EKS_CLUSTER="$2"
AWS_PROFILE="$3"

function usage {
    cat <<EOU
usage: bitsandbobs-deploy.sh Environment CreateEksCluster AwsProfile
  Environment       Development|Staging|Production
  CreateEksCluster  true|false
  AwsProfile        The name of a valid AWS CLI profile
EOU
    exit 1
}

if ! [[ $DEPLOY_ENVIRONMENT =~ ^(Development|Staging|Production)$ ]] ; then
    usage
fi

if ! [[ $CREATE_EKS_CLUSTER =~ ^(true|false)$ ]] ; then
    usage
fi

if [ "$AWS_PROFILE" = "" ] ; then
    usage
fi

trap "rm $(dirname $SCRIPT_PATH)/bitsandbobs.packaged.yaml > /dev/null" EXIT

aws cloudformation package \
    --template-file "$(dirname $SCRIPT_PATH)/bitsandbobs.yaml" \
    --s3-bucket marsupialmouse-bitsandbobs-cfn \
    --s3-prefix local \
    --output-template-file "$(dirname $SCRIPT_PATH)/bitsandbobs.packaged.yaml" \
    --force-upload \
    --profile "$AWS_PROFILE"

aws cloudformation deploy \
    --template-file "$(dirname $SCRIPT_PATH)/bitsandbobs.packaged.yaml" \
    --stack-name "BitsAndBobs-$DEPLOY_ENVIRONMENT" \
    --parameter-overrides Environment=$DEPLOY_ENVIRONMENT CreateEksCluster=$CREATE_EKS_CLUSTER \
    --profile "$AWS_PROFILE"
