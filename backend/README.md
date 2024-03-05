## LexBox backend

The LexBox backend is a dotnet core api. With a proxy to route requests for hg and resumable.

### ASP.NET Routes

| Route | Description | Notes |
|---|---|---|
| **Send/Receive** | | These routes have the lowest precedence, because they're the most generic. |
| `/{project-code}`<sup>1</sup> OR `/hg/{project-code}` | hg S/R  | <sup>1</sup>Obviously it would be a bit problematic if a `{project-code}` were `api`, but that's the URL baked into Chorus clients around the world 🤷.|
| `/api/v03` | hg-resumable S/R |   |
| `/api/user/{userName}/projects` | Returns a list of the user's projects  |  See `LegacyProjectApiController`. <br>Used by [Chorus/FLEx](https://github.com/sillsdev/chorus/blob/04eda7903f3fe09d92cfc3edf91acea598c92744/src/LibChorus/Model/ServerSettingsModel.cs#L336) & [Language Forge](https://github.com/sillsdev/web-languageforge/blob/b2123ad2ca45a67bbd68381152e98b6f2bb5334a/src/Api/Model/Languageforge/Lexicon/Command/SendReceiveCommands.php#L91). |
| **Frontend API** | |
| `/api/graphql` | GraphQL API |  |
| `/api/project/upload-zip/{project-code}` | TUS upload  | Expects a zip with an .hg folder  |
| `/api/**` | Other REST controllers  |  |
| **Tools** | |
| `/api/healthz` | Health check  |
| `/api/swagger` | Swagger UI | |
| `/api/graphql/ui` | GraphQL UI | [Banana Cake Pop](https://chillicream.com/docs/bananacakepop/v2/explore-the-ui) |
| `/api/quartz`  | Quartz.NET UI (job scheduler)  | [CrystalQuartz](https://github.com/guryanovev/CrystalQuartz) |
| `/security.txt` or `/.well-known/security.txt`  | Vulnerability reporting info  | [Security.txt standard](https://securitytxt.org/) |
