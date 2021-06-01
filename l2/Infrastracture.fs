module Infrastracture

module List =
    let inline remove func = List.choose (fun a -> if func(a) then Some a else None)
    let inline replace index newItem = List.mapi (fun i a -> if i = index then newItem else a)
    let inline update index updateFunc = List.mapi (fun i a -> if i = index then updateFunc a else a)


