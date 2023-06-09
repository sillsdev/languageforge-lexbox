#addin nuget:?package=Cake.Docker&version=1.2.0
#addin nuget:?package=Cake.Pnpm&version=1.0.0
#addin nuget:?package=Cake.DoInDirectory&version=6.0.0

var target = Argument("target", "test");

Task("back")
	.Does(() => {
		DockerComposeUp(new DockerComposeUpSettings { DetachedMode = true },
            "db", "hasura", "otel-collector", "maildev", "hgweb");
    });

Task("front")
    .IsDependentOn("back")
	.Does(() => DoInDirectory("./frontend", () => PnpmRun("dev")));

Task("api")
	.Does(() => DoInDirectory("./backend/LexBoxApi", () =>
        StartAndReturnProcess("dotnet", new ProcessSettings {
            Arguments = ProcessArgumentBuilder.FromString("watch")
        })));

Task("test-sr")
    .IsDependentOn("back")
	.Does(() => DotNetTest("./backend/Testing/Testing.csproj",
        new DotNetTestSettings {Filter = "SendReceive"}));

Task("back-down")
	.Does(() => {
		DockerComposeDown();
    });

RunTarget(target);
