using Aspire.Hosting;
using Aspire.Hosting.Yarp.Transforms;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

//for local and docker based deployment
//var compose = builder.AddDockerComposeEnvironment("compose")
//    .WithDashboard(dashboard => dashboard.WithHostPort(8080));

var typeSenseApiKey = builder.AddParameter("TypeSense-Api-Key", secret:true);

var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin(15427);

var keycloak = builder.AddKeycloak("keycloak", 6001)
    .WithRealmImport("../infra/realms")
    .WithDataVolume("keycloak-data")
    .WithEnvironment("KC_HTTP_ENABLED", "true")
    .WithEnvironment("KC_HOSTNAME_STRICT", "false")
    .WithEndpoint("http",e=>e.IsExternal = true);
// .WithEnvironment("VIRTUAL_HOST", "id.overflow.local")
// .WithEnvironment("VIRTUAL_PORT", "8080");

var pgUser = builder.AddParameter("pg-username",secret:true);
var pgPassword = builder.AddParameter("pg-password",secret:true);
var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
    .WithPasswordAuthentication(pgUser, pgPassword);

var typesense = builder.AddContainer("typesense", "typesense/typesense", "29.0")
                        .WithVolume("typesense-data", "/data")
                        .WithEnvironment("TYPESENSE_DATA_DIR", "/data")
                        .WithEnvironment("TYPESENSE_ENABLES_CORS", "true")
                        .WithEnvironment("TYPESENSE_API_KEY", typeSenseApiKey)
                        .WithHttpEndpoint(8108, 8108, "typesense");
var typeSenseContainer = typesense.GetEndpoint("typesense");

var questionDb = postgres.AddDatabase("questionDb", "question");
var profileDb = postgres.AddDatabase("profileDb", "profile");
var statDb = postgres.AddDatabase("statDb", "stat");
var voteDb = postgres.AddDatabase("voteDb", "vote");

var questionService = builder.AddProject<Projects.QuestionService>("question-svc")
                      .WithReference(keycloak)
                      .WithReference(questionDb)
                      .WithReference(rabbitmq)
                      .WaitFor(keycloak)
                      .WaitFor(questionDb)
                      .WaitFor(rabbitmq);

var searchService = builder.AddProject<Projects.SearchService>("search-svc")
                      .WithReference(typeSenseContainer)
                      .WithReference(rabbitmq)
                      .WaitFor(rabbitmq)
                      .WaitFor(typesense)
                      .WithEnvironment("typesense-api-key", typeSenseApiKey);

var profileService = builder.AddProject<Projects.ProfileService>("profile-svc")
                      .WithReference(keycloak)
                      .WithReference(profileDb)
                      .WithReference(rabbitmq)
                      .WaitFor(keycloak)
                      .WaitFor(profileDb)
                      .WaitFor(rabbitmq);

var statService = builder.AddProject<Projects.StatsService>("stat-svc")
                      .WithReference(statDb)
                      .WithReference(rabbitmq)
                      .WaitFor(statDb)
                      .WaitFor(rabbitmq);

var voteService = builder.AddProject<Projects.VoteService>("voteservice")
                      .WithReference(voteDb)
                      .WithReference(rabbitmq)
                      .WaitFor(voteDb)
                      .WaitFor(rabbitmq);

var yarp = builder.AddYarp("gateway")
      .WithConfiguration(yarpBuilder =>
      {
          yarpBuilder.AddRoute("/questions/{**catch-all}", questionService);
          yarpBuilder.AddRoute("/tags/{**catch-all}", questionService);
          yarpBuilder.AddRoute("/search/{**catch-all}", searchService);
          yarpBuilder.AddRoute("/profiles/{**catch-all}", profileService);
          yarpBuilder.AddRoute("/stats/{**catch-all}", statService);
          yarpBuilder.AddRoute("/vote/{**catch-all}", statService);
      })
      .WithEnvironment("ASPNETCORE_URLS", "http://*:8001")
      .WithEndpoint(port: 8001, targetPort: 8001, "http", name: "gateway", isExternal: true);
//.WithEnvironment("VIRTUAL_HOST", "api.overflow.local")
//.WithEnvironment("VIRTUAL_PORT", "8001");

var nodeApp = builder.AddJavaScriptApp("webapp", "../webapp", "dev")
    .WithHttpEndpoint(port: 3000, env: "PORT", targetPort: 4000)
    .WithReference(keycloak).PublishAsDockerFile();

if (builder.ExecutionContext.IsPublishMode)
{
    rabbitmq.WithVolume("rabbitmq-data","var/lib/rabbitmq/mnesia");
}
else
{
    rabbitmq.WithDataVolume("rabbitmq-data");
    postgres.RunAsContainer();
}


builder.Build().Run();
