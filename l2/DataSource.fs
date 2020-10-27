namespace l2

open System
open Npgsql
open System.Collections.Generic

type Column = {
    name:string
    displayName:string
    validate:string->string option
    toValue:string->obj option
    toString:obj option->string
    dbToValue:Data.Common.DbDataReader->int->obj
    toFilter:string->string
}

module Column =
    let string = {
        name = null
        displayName = null
        validate = fun _ -> None
        toString = function Some s -> string s | _ -> String.Empty
        toValue = fun s -> if String.IsNullOrWhiteSpace s then None else s |> box |> Some
        dbToValue = fun a -> a.GetString >> box
        toFilter = sprintf "LIKE '%%%s%%'"
    }
    let int = {
        string with
            toValue = Int32.TryParse >> function true,v -> v |> box |> Some  | _ -> None
            dbToValue = fun a -> a.GetInt32 >> box
            toFilter = sprintf "= %s"
    }
    let float32 = {
        string with
            toValue = Double.TryParse >> function true,v -> v |> box |> Some  | _ -> None
            dbToValue = fun a -> a.GetDouble >> box
            toFilter = sprintf "= %s"
    }


type DataSource = {
    columns:Column list
    commands:IDictionary<string,string>
}

module DataSource =
    let warehouseTable = {
        columns = [
            { Column.string with name = "dbSource"; displayName = "Source" }
            { Column.int with name = "id" }
            { Column.int with name = "item_id" }
            { Column.float32 with name = "quantity" }
        ]
        commands = dict [
            "select", "select * from (SELECT 'db2' dbSource, * FROM public.dblink 
                   ('demodbrnd','select * from public.warehouse')
                   AS DATA(id int, item_id int, quantity float) 
                   union all
                   select 'db1' dbSource, * from public.warehouse) a"
        ]
    }

    let select (dataSource:DataSource) (filter:(Column*obj option)list) =
        let filter = 
            let nonNoneFilter = filter |> List.filter (snd >> Option.isSome)
            if List.isEmpty nonNoneFilter then String.Empty
            else " where " + String.concat "," [for c,v in nonNoneFilter -> sprintf "%s %s" c.name (v |> c.toString |> c.toFilter)] + " "

        let conn = new NpgsqlConnection("Host=localhost;Port=54320;Database=postgres;Username=postgres;Password=postpswd")
        conn.Open()
               
        use command = conn.CreateCommand()
        command.CommandText <- dataSource.commands.["select"] + filter + ";"

        use reader = command.ExecuteReader()
               
        let cols = List.indexed dataSource.columns 
        let rec read acc =
            if reader.Read()
            then [for i,c in cols -> c.dbToValue reader i]::acc |> read
            else acc

        read []
