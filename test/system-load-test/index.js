// Load the AWS SDK for Node.js
var AWS = require("aws-sdk");
var uuidv4 = require("uuid").v4;

// Set the region
AWS.config.update({ region: "eu-central-1" });

// Event names array
var eventNames = [
  "JobCreated",
  "PageViewed",
  "UserCreated",
  "ProductPurchased",
  "ArticleRead",
  "VideoWatched",
  "ButtonClicked",
  "UserLoggedIn",
  "ItemAddedToCart",
  "CheckoutStarted",
];

// Number of messages to send
var numberOfMessages = 5; // Change this to the desired number of messages

for (let i = 0; i < numberOfMessages; i++) {
  // Create publish parameters
  var params = {
    Message: JSON.stringify({
      Id: uuidv4(),
      EventName: eventNames[Math.floor(Math.random() * eventNames.length)],
      EventData: uuidv4(),
      Date: new Date().toISOString(),
    }),
    TopicArn: "arn:aws:sns:eu-central-1:377140207735:event-topic",
  };

  // Create promise and SNS service object
  var publishTextPromise = new AWS.SNS({ apiVersion: "2010-03-31" })
    .publish(params)
    .promise();

  // Handle promise's fulfilled/rejected states
  publishTextPromise
    .then(function (data) {
      console.log(
        `Message ${params.Message} sent to the topic ${params.TopicArn}`
      );
      console.log("MessageID is " + data.MessageId);
    })
    .catch(function (err) {
      console.error(err, err.stack);
    });
}
