namespace l2

open System

type Column = {
    name:string
    validate:string->string option
    toValue:string->obj
    toString:obj->string
}

//type DataSource = {
//    columns:Column list
//    getItems:
//}