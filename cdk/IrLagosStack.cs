using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Amazon.CDK;
using Constructs;
using DynamoDb = Amazon.CDK.AWS.DynamoDB;
using IAM = Amazon.CDK.AWS.IAM;
using Lambda = Amazon.CDK.AWS.Lambda;
using SNS = Amazon.CDK.AWS.SNS;
using APIGateway = Amazon.CDK.AWS.APIGateway;

namespace IrLagos
{
    public class IrLagosStack : Stack
    {
        internal IrLagosStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // sns topic for events
            var sns = new SNS.Topic(this, "sns-topic", new SNS.TopicProps
            {
                TopicName = "event-topic",
                DisplayName = "event-topic",
            });

            sns.ApplyRemovalPolicy(RemovalPolicy.DESTROY);


            // dynamodb event store 
            var dynamodb = new DynamoDb.Table(this, "event-store", new DynamoDb.TableProps
            {
                TableName = "event-store",
                PartitionKey = new DynamoDb.Attribute
                {
                    Name = "Id",
                    Type = DynamoDb.AttributeType.STRING
                },
                SortKey = new DynamoDb.Attribute
                {
                    Name = "Date",
                    Type = DynamoDb.AttributeType.STRING
                },
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            // processor lambda 
            var processorLambdaFilename = "../src/processor-lambda/bin/Release/net6.0/processor-lambda.zip";
            var processorLambdaHash = CalculateFileHash(processorLambdaFilename);
            var processorLambda = new Lambda.Function(this, "processor", new Lambda.FunctionProps
            {
                FunctionName = "processor-lambda",
                Architecture = Lambda.Architecture.X86_64,
                Runtime = Lambda.Runtime.DOTNET_6,
                MemorySize = 256,
                Timeout = Duration.Seconds(5),
                Environment = new Dictionary<string, string>()
                {
                    {"EVENT_STORE_TABLE_NAME", "event-store"}
                },
                Handler = "processor-lambda::processor_lambda.Function::FunctionHandler",
                Code = Lambda.Code.FromAsset(processorLambdaFilename),
                CurrentVersionOptions = new Lambda.VersionOptions
                {
                    CodeSha256 = processorLambdaHash
                }
            });

            processorLambda.ApplyRemovalPolicy(RemovalPolicy.DESTROY);

            processorLambda.Role.AttachInlinePolicy(new IAM.Policy(this, "processor-lambda-policy", new IAM.PolicyProps
            {
                Statements = new IAM.PolicyStatement[]
                {
                    new IAM.PolicyStatement(new IAM.PolicyStatementProps{
                        Effect = IAM.Effect.ALLOW,
                        Actions = new string[]{"sns:*","dynamodb:*"},
                        Resources = new []{"*"}
                    })
                }
            }));

            sns.AddSubscription(new SNS.Subscriptions.LambdaSubscription(processorLambda));

            // query lambda 
            var queryLambdaFilename = "../src/query-lambda/bin/Release/net6.0/query-lambda.zip";
            var queryLambdaHash = CalculateFileHash(queryLambdaFilename);
            var queryLambda = new Lambda.Function(this, "query", new Lambda.FunctionProps
            {
                FunctionName = "query-lambda",
                Architecture = Lambda.Architecture.X86_64,
                Runtime = Lambda.Runtime.DOTNET_6,
                MemorySize = 256,
                Timeout = Duration.Seconds(15),
                Environment = new Dictionary<string, string>()
                {
                    {"EVENT_STORE_TABLE_NAME", "event-store"}
                },
                Handler = "query-lambda::query_lambda.Function::GetAsync",
                Code = Lambda.Code.FromAsset(queryLambdaFilename),
                CurrentVersionOptions = new Lambda.VersionOptions
                {
                    CodeSha256 = queryLambdaFilename
                }
            });

            queryLambda.ApplyRemovalPolicy(RemovalPolicy.DESTROY);

            queryLambda.Role.AttachInlinePolicy(new IAM.Policy(this, "query-lambda-policy", new IAM.PolicyProps
            {
                Statements = new IAM.PolicyStatement[]
                {
                    new IAM.PolicyStatement(new IAM.PolicyStatementProps{
                        Effect = IAM.Effect.ALLOW,
                        Actions = new string[]{"dynamodb:*"},
                        Resources = new []{dynamodb.TableArn}
                    })
                }
            }));

            // create api gateway and attach to lambda
            var api = new APIGateway.RestApi(this, "api-gateway", new APIGateway.RestApiProps
            {
                RestApiName = "query-api",
                Description = "query-api",
                EndpointTypes = new APIGateway.EndpointType[] { APIGateway.EndpointType.REGIONAL },
                DefaultIntegration = new APIGateway.LambdaIntegration(queryLambda)
            });

            api.Root.AddMethod("GET");
            api.ApplyRemovalPolicy(RemovalPolicy.DESTROY);
        }

        static string CalculateFileHash(string filePath)
        {
            using (var hashAlgorithmInstance = HashAlgorithm.Create("SHA256"))
            {
                using (var fileStream = File.OpenRead(filePath))
                {
                    byte[] hashBytes = hashAlgorithmInstance.ComputeHash(fileStream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}
