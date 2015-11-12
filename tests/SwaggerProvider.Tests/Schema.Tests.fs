﻿module SwaggerProvider.Tests

open SwaggerProvider.Internal.Schema
open SwaggerProvider.Internal.Schema.Parsers
open FSharp.Data
open NUnit.Framework
open System.IO

[<Test>]
let ``Schema parse of PetStore.Swagger.json sample`` () =
    let schema =
        "Schemas/PetStore.Swagger.json"
        |> File.ReadAllText
        |> JsonValue.Parse
        |> JsonParser.parseSwaggerObject
    Assert.AreEqual(6, schema.Definitions.Length)

    let expectedInfo = {
        Title = "Swagger Petstore"
        Version = "1.0.0"
        Description = "This is a sample server Petstore server.  You can find out more about Swagger at [http://swagger.io](http://swagger.io) or on [irc.freenode.net, #swagger](http://swagger.io/irc/).  For this sample, you can use the api key `special-key` to test the authorization filters."
    }
    if schema.Info <> expectedInfo
        then Assert.Fail (sprintf "\n%A \n<> \n%A" schema.Info expectedInfo)


// Test that provider can parse real-word Swagger 2.0 schemas

type ApisGuru = FSharp.Data.JsonProvider<"https://apis-guru.github.io/api-models/apis.json">

let ApisGuruSchemaUrls =
    ApisGuru.GetSample().Apis
    |> Array.choose (fun x ->
        x.Properties
        |> Array.tryFind (fun y ->
            y.Type = "Swagger")
       )
    |> Array.map (fun x -> x.Url)

let ManualSchemaUrls =
    [|//"http://netflix.github.io/genie/docs/rest/swagger.json" // This schema is incorrect
      //"https://www.expedia.com/static/mobile/swaggerui/swagger.json" // This schema is incorrect
      "https://graphhopper.com/api/1/vrp/swagger.json"|]

let SchemaUrls =
    Array.concat [ManualSchemaUrls; ApisGuruSchemaUrls]

[<Test; TestCaseSource("SchemaUrls")>]
let ``Schema Parse`` url =
    let json =
        try
            Http.RequestString url
        with
        | :? System.Net.WebException ->
            printfn "Schema is unaccessible %s" url
            ""
    if not <| System.String.IsNullOrEmpty(json) then
        let schema = json |> JsonValue.Parse |> Parsers.JsonParser.parseSwaggerObject
        Assert.Greater(schema.Paths.Length + schema.Definitions.Length, 0)