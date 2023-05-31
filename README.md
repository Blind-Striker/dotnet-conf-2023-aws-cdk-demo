# Welcome to your CDK C# project!

This is a blank project for CDK development with C#.

The `cdk.json` file tells the CDK Toolkit how to execute your app.

It uses the [.NET CLI](https://docs.microsoft.com/dotnet/articles/core/) to compile and execute your project.

## Useful commands

* `cdk synth`        emits the synthesized CloudFormation template
* `cdk diff`         compare deployed stack with current state
* `cdk deploy`       deploy this stack to your default AWS account/region



## build lambda

`dotnet lambda package`


## scan dynamo

`aws dynamodb scan --table-name event-store --region eu-central-1`

## get api urls
`$env:AWS_DEFAULT_REGION="eu-central-1"; (aws apigateway get-rest-apis | ConvertFrom-Json).items | %{ "https://$($_.id).execute-api.$env:AWS_DEFAULT_REGION.amazonaws.com/$($_.tags.PROD)" }`