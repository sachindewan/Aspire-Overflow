using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var compose = builder.AddDockerComposeEnvironment("compose")
    .WithDashboard(dashboard => dashboard.WithHostPort(8080));

var typeSenseApiKey = builder.AddParameter("TypeSense-Api-Key", secret:true);

var rabbitmq = builder.AddRabbitMQ("messaging")
                        .WithManagementPlugin(15427);

var keycloak = builder.AddKeycloak("keycloak", 6001)
    .WithRealmImport("../infra/realms")
    .WithDataVolume("keycloak-data")
    .WithEnvironment("KC_HTTP_ENABLED", "true")
    .WithEnvironment("KC_HOSTNAME_STRICT", "false")
    .WithEndpoint(6001, 8080, "keycloak", isExternal: true);

var postgres = builder.AddPostgres("postgres", port: 5432)
        .WithDataVolume("postgres-data")
        .WithPgAdmin();

var typesense = builder.AddContainer("typesense", "typesense/typesense", "29.0")
                        .WithVolume("typesense-data", "/data")
                        .WithEnvironment("TYPESENSE_DATA_DIR", "/data")
                        .WithEnvironment("TYPESENSE_ENABLES_CORS", "true")
                        .WithEnvironment("TYPESENSE_API_KEY", typeSenseApiKey)
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
