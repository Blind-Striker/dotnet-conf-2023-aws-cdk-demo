using System.Net;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace query_lambda;

public class Function
{
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
    /// A Lambda function to respond to HTTP Get methods from API Gateway
    /// </summary>
    /// <param name="request"></param>
    /// <returns>The API Gateway response.</returns>
    public async Task<APIGatewayProxyResponse> GetAsync(APIGatewayProxyRequest request, ILambdaContext context)
    {
        context.Logger.LogInformation("Get Request\n");
        
        var conditions = new List<ScanCondition>();
        var items = await _dynamoDBContext.ScanAsync<EventModel>(conditions, _operationConfig).GetRemainingAsync();

        var response = new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.OK,
            Body = JsonSerializer.Serialize(items),
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };

        return response;
    }
}