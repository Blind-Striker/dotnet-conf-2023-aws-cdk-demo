using System.Text.Json.Serialization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace processor_lambda;

public class Function
{
    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>

    private readonly IAmazonDynamoDB _dynamoDBClient;
    private readonly IDynamoDBContext _dynamoDBContext;
    private readonly string _tableName;
    private readonly DynamoDBOperationConfig _operationConfig;

    public Function()
    {
        _dynamoDBClient = new AmazonDynamoDBClient();
        _dynamoDBContext = new DynamoDBContext(_dynamoDBClient);
        _tableName = Environment.GetEnvironmentVariable("EVENT_STORE_TABLE_NAME");
        _operationConfig = new DynamoDBOperationConfig
        {
            OverrideTableName = _tableName
        };
    }


    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an SNS event object and can be used 
    /// to respond to SNS messages.
    /// </summary>
    /// <param name="evnt"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task FunctionHandler(SNSEvent evnt, ILambdaContext context)
    {
        foreach (var record in evnt.Records)
        {
            await ProcessRecordAsync(record, context);
        }
    }

    private async Task ProcessRecordAsync(SNSEvent.SNSRecord record, ILambdaContext context)
    {
        context.Logger.LogInformation($"Processed record {record.Sns.Message}");

        var item = System.Text.Json.JsonSerializer.Deserialize<EventModel>(record.Sns.Message);
        await _dynamoDBContext.SaveAsync(item, _operationConfig, default(CancellationToken));
    }
}