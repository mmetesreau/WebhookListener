module Webhooks.App

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open System.Security.Cryptography
open System.Text

// ---------------------------------
// Helpers
// ---------------------------------

let computeGithubSignature (secret: string) (payload: string) : string = 
    use hmacSh1 = new HMACSHA1(Encoding.ASCII.GetBytes(secret))
    let hash = 
        hmacSh1.ComputeHash(Encoding.ASCII.GetBytes(payload))
                |> Array.map (fun (x : byte) -> System.String.Format("{0:X2}", x))
                |> String.concat String.Empty

    sprintf "sha1=%s" hash
                
// ---------------------------------
// Handlers
// ---------------------------------

let handlePostGithub (secret: string) (next : HttpFunc) (ctx : HttpContext) = task {
    match ctx.GetRequestHeader "X-Hub-Signature" with
    | Error _ ->
        return! RequestErrors.BAD_REQUEST "Missing signature" next ctx
    | Ok signature ->
        let! payload = ctx.ReadBodyFromRequestAsync()

        let computedSignature = computeGithubSignature secret payload

        if computedSignature.Equals(signature, StringComparison.CurrentCultureIgnoreCase) |> not then
            return! RequestErrors.BAD_REQUEST "Invalid signature" next ctx
        else     
            printfn "payload %s" payload

            return! Successful.OK "" next ctx
}

// ---------------------------------
// Web app
// ---------------------------------

let webApp =
    let githubSecret = 
        match Environment.GetEnvironmentVariable("GITHUB_SECRET") with
        | null -> "mysupersecret"
        | githubSecret -> githubSecret

    choose [
        subRoute "/api"
            (choose [
                POST >=> choose [
                    route "/github"  >=> handlePostGithub githubSecret 
                ]
                GET >=> choose [
                    route "/ping" >=> text "pong"
                ]
            ])
        setStatusCode 404 >=> text "Not Found" ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IHostingEnvironment>()
    (match env.IsDevelopment() with
    | true  -> app.UseDeveloperExceptionPage()
    | false -> app.UseGiraffeErrorHandler errorHandler)
        .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    services.AddGiraffe() |> ignore

[<EntryPoint>]
let main _ =
    let port = 
        match Environment.GetEnvironmentVariable("PORT") with
        | null -> "8080"
        | port -> port

    let url = sprintf "http://0.0.0.0:%s" port
    
    WebHostBuilder()
        .UseKestrel()
        .UseUrls(url)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .Build()
        .Run()
    0