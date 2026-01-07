using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var compose = builder.AddDockerComposeEnvironment("compose")
    .WithDashboard(dashboard => dashboard.WithHostPort(8080));

var typeSenseApiKey = builder.AddParameter("TypeSense-Api-Key", secret:true);

var rabbitmq = builder.AddRabbitMQ("messaging")
                        .WithManagementPlugin(15427);

var keycloak = builder.AddKeycloak("keycloak", 6001)
    .WithDataVolume("keycloak-data");

var postgres = builder.AddPostgres("postgres", port: 5432)
        .WithDataVolume("postgres-data")
        .WithPgAdmin();

var typesense = builder.AddContainer("typesense", "typesense/typesense", "29.0")
                        .WithArgs("--data-dir", "data", "--api-key", typeSenseApiKey, "--enable-cors")
                        .WithVolume("typesense-data", "/data")
                        .WithHttpEndpoint(8108, 8108, "typesense");
var typeSenseContainer = typesense.GetEndpoint("typesense");

var questionDb = postgres.AddDatabase("questionDb", "question");

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

var yarp = builder.AddYarp("gateway")
      .WithConfiguration(yarpBuilder =>
      {
          yarpBuilder.AddRoute("/questions/{**catch-all}", questionService);
          yarpBuilder.AddRoute("/tags/{**catch-all}", questionService);
          yarpBuilder.AddRoute("/search/{**catch-all}", searchService);
      })
      .WithEnvironment("ASPNETCORE_URLS", "http://*:8001")
      .WithEndpoint(port:8001, targetPort:8001,"http",name:"gateway",isExternal:true);

builder.Build().Run();
