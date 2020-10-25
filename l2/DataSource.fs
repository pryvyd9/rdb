namespace l2

open System

type Column = {
    name:string
    validate:string->string option
    toValue:string->obj
    toString:obj->string
}

type DataSource = {
    columns:Column list
}

module DataSource =
    let testTable = {
        columns = [
            {
                name = "col1"
                validate = fun _ -> None
                toString = string
                toValue = fun a -> box a 
            }
            {
                name = "col2"
                validate = fun _ -> None
                toString = string
                toValue = fun a -> box a 
            }
        ]
    }

    let load (dataSource:DataSource) (filter:(Column*obj)list) =
        let items = [["0:0"; "0:1"];["1:0";"1:1"];["2:0";"2:1"]]
        let items = items |> List.map (List.map box)
        items