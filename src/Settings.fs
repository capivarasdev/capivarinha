namespace Capivarinha

open FsConfig

type Settings = {
    DiscordToken: string
    GuildId: uint64
    BotChannelId: uint64

    [<DefaultValue("sqlite.db")>]
    DatabasePath: string
    [<DefaultValue("ironicamente")>]
    ForbiddenWords: string list
}


[<RequireQualifiedAccess>]
module Settings =
    open Microsoft.Data.Sqlite
    open Microsoft.Extensions.Configuration

    let databaseConnectionString settings =
        let builder = SqliteConnectionStringBuilder()
        builder.DataSource <- settings.DatabasePath
        builder.ForeignKeys <- true
        builder.Pooling <- true 
        
        builder.ConnectionString

    let load () =
        let builder = 
            ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", optional=true)
                .Build()

        let appConfig = AppConfig(builder)

        match appConfig.Get<Settings>() with
        | Ok config -> config
        | Error (NotFound envVarName) -> 
            failwithf "Environment variable %s not found" envVarName
        | Error (BadValue (envVarName, value)) -> 
            failwithf "Environment variable %s has invalid value %s" envVarName value
        | Error (NotSupported msg) -> 
            failwith msg
