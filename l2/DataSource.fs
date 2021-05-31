namespace l2

open System
open Npgsql
open System.Collections.Generic

type Condition = 
    | Greater 
    | GreaterOrEqual
    | Less
    | LessOrEqual
    | Equal
    | Like

module Condition =
    //let all = [Greater;Less;Equal;Like]
    let allMap = [
        Greater,">"
        GreaterOrEqual,">="
        Less,"<"
        LessOrEqual,"<="
        Equal,"="
        Like,"LIKE"
    ]

    let toString a = 
        List.tryFind (fst >> (=) a) allMap
        |> function 
        | None -> String.Empty
        | Some s -> s |> snd

    let fromString a =
        List.tryFind (snd >> (=) a) allMap
        |> function
        | None -> None
        | Some s -> s |> fst |> Some

    let getClause field condition value =
        match condition with
        | Greater | GreaterOrEqual | Less | LessOrEqual | Equal -> $"({field} {toString condition} {value})"
        | Like -> $"({field} {toString condition} '%%{value}%%')"


type Column = {
    name:string
    displayName:string
    validate:string->string option
    toValue:string->obj option
    toString:obj option->string
    dbToValue:Data.Common.DbDataReader->int->obj
    typename:string

    toFilter:string->string
    condition:Condition option
    conditionValue:obj option
    supportedConditions:Condition list

    shouldSelect:bool
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
        typename = "text"
        condition = None
        conditionValue = None
        supportedConditions = [Like;Equal]
        shouldSelect = true
    }
    let int = {
        string with
            toValue = Int32.TryParse >> function true,v -> v |> box |> Some  | _ -> None
            dbToValue = fun a -> a.GetInt32 >> box
            toFilter = sprintf "= %s"
            typename = "int"
            supportedConditions = [Greater;GreaterOrEqual;Less;LessOrEqual;Equal]
    }
    let float32 = {
        int with
            toValue = Double.TryParse >> function true,v -> v |> box |> Some  | _ -> None
            dbToValue = fun a -> a.GetDouble >> box
            typename = "float"
    }

module Command =
    let create columns =
        let columnsToSelect = columns |> List.filter (fun a -> a.shouldSelect)
        let columns = String.concat "," (columnsToSelect |> List.map (fun a -> a.name))
        let start = $"select {columns} from (SELECT 'db2' dbSource, * FROM public.dblink 
        ('demodbrnd','select * from public.warehouse')
        AS DATA(id int, item_id int, quantity float) 
        union all
        select 'db1' dbSource, * from public.warehouse) a"
        let columnsToFilter = 
            columnsToSelect 
            |> List.filter (fun a -> Option.isSome a.condition && Option.isSome a.conditionValue)
            |> List.map (fun a -> a.name, a.condition.Value, a.toString, a.conditionValue)
        let filter = String.concat "AND" (columnsToFilter |> List.map (fun (name, condition, toString, conditionValue) -> Condition.getClause name condition (toString conditionValue)))
        
        if String.IsNullOrWhiteSpace filter
        then $"{start};"
        else $"{start} WHERE {filter};"


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

        let conn = new NpgsqlConnection("Host=localhost;Port=54321;Database=postgres;Username=postgres;Password=postpswd")
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

        
type DataView = {
    columns:Column list
    name:string
}

module DataView =
    let warehouseTable = {
        columns = [
            { Column.string with name = "dbSource"; displayName = "Source" }
            { Column.int with name = "id" }
            { Column.int with name = "item_id" }
            { Column.float32 with name = "quantity" }

        ]
        name = "warehouse"
    }

    let select (dataSource:DataView) =
        let query = Command.create(dataSource.columns)

        let conn = new NpgsqlConnection("Host=localhost;Port=54321;Database=postgres;Username=postgres;Password=postpswd")
        conn.Open()
               
        use command = conn.CreateCommand()
        command.CommandText <- query

        use reader = command.ExecuteReader()
               
        let cols = List.indexed (dataSource.columns |> List.filter (fun a -> a.shouldSelect))
        let rec read acc =
            if reader.Read()
            then [for i,c in cols -> c.dbToValue reader i]::acc |> read
            else acc

        read []

        