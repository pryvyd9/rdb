namespace l2

open System
open Avalonia
open Avalonia.Markup.Xaml
open Avalonia.Controls
open Avalonia.Media



type DynamicTable() as this =
    inherit UserControl()
    do AvaloniaXamlLoader.Load(this)

    let itemGrid = this.FindControl<Grid>("itemGrid")
    let headersGrid = this.FindControl<Grid>("headerGrid")
    let hsv = this.FindControl<ScrollViewer>("headerScrollViewer")
    let isv = this.FindControl<ScrollViewer>("itemScrollViewer")

    let defaultRowHeight = 18.
    let gridSplitterThickness = 1.

    let mutable items:(unit->obj option) option [] List = []
    let mutable itemsString:(unit->string) [] List = []
    let mutable isReadOnly:bool = false

    do
        let p = ScrollViewer.HorizontalScrollBarValueProperty
        hsv.Bind(p, isv.GetObservable(p)) |> ignore

    let mutable onItemUpdated:(int->int->string->unit) = (fun i j v -> ())

    let createTextBox (grid:Grid) row column text isReadOnly =
        let a = TextBox()
        a.SetValue(Grid.ColumnProperty, column) |> ignore
        a.SetValue(Grid.RowProperty, row) |> ignore
        a.Text <- text
        a.BorderThickness <- Thickness()
        a.Padding <- Thickness()
        a.Margin <- Thickness(1.)
        if isReadOnly
        then 
            a.IsReadOnly <- true
            a.CaretBrush <- Brushes.Transparent
        a.GetObservable(TextBox.TextProperty).Subscribe(fun a -> onItemUpdated (row/2) (column/2) a) |> ignore
        grid.Children.Add(a)
        a

    let createVerticalGridSplitter (grid:Grid) column rowCount  =
        let rowCount = if rowCount < 1 then 1 else rowCount
        let a = GridSplitter()
        a.SetValue(Grid.ColumnProperty, column) |> ignore
        a.SetValue(Grid.RowSpanProperty, rowCount) |> ignore
        a.Width <- gridSplitterThickness
        a.Background <- Brushes.Black
        grid.Children.Add(a)

    let createColumns rowCount columnCount =
        let rowIndexDefaultWidth = 
            // Get number of digits in rowCount and multiply by width of a letter
            match rowCount |> float |> log10 |> (+) 1. |> (*) 12. with
            | a when a < 0. -> 0.
            | a -> a

        // Index column
        itemGrid.ColumnDefinitions.Add(ColumnDefinition(rowIndexDefaultWidth, GridUnitType.Pixel))
        itemGrid.ColumnDefinitions.Add(ColumnDefinition(gridSplitterThickness, GridUnitType.Pixel))
        createVerticalGridSplitter itemGrid 1 (rowCount*2) 

        for i in 0..columnCount-2 do
            itemGrid.ColumnDefinitions.Add(ColumnDefinition(200., GridUnitType.Pixel))
            itemGrid.ColumnDefinitions.Add(ColumnDefinition(gridSplitterThickness, GridUnitType.Pixel))
            createVerticalGridSplitter itemGrid (i*2+1+2) (rowCount*2)

        itemGrid.ColumnDefinitions.Add(ColumnDefinition())

    let createHeaders (headers:Column list) =
        for cd in itemGrid.ColumnDefinitions do
            let hcd = ColumnDefinition()
            hcd.Bind(ColumnDefinition.WidthProperty, cd.GetObservable(ColumnDefinition.WidthProperty)) |> ignore
            cd.Bind(ColumnDefinition.WidthProperty, hcd.GetObservable(ColumnDefinition.WidthProperty)) |> ignore
            headersGrid.ColumnDefinitions.Add(hcd)

        do
            headersGrid.RowDefinitions.Add(RowDefinition(gridSplitterThickness, GridUnitType.Pixel))
            headersGrid.RowDefinitions.Add(RowDefinition())
            headersGrid.RowDefinitions.Add(RowDefinition(gridSplitterThickness, GridUnitType.Pixel))
            let colSpan = headers |> List.length |> (+) 1 |> (*) 2
            do 
                let a = GridSplitter()
                a.SetValue(Grid.ColumnSpanProperty, colSpan) |> ignore
                a.HorizontalAlignment <- Layout.HorizontalAlignment.Stretch
                a.Height <- gridSplitterThickness
                a.Background <- Brushes.Black
                headersGrid.Children.Add(a)

            do
                let a = GridSplitter()
                a.SetValue(Grid.RowProperty, 2) |> ignore
                a.SetValue(Grid.ColumnSpanProperty, colSpan) |> ignore
                a.HorizontalAlignment <- Layout.HorizontalAlignment.Stretch
                a.Height <- gridSplitterThickness
                a.Background <- Brushes.Black
                headersGrid.Children.Add(a)

        for c in itemGrid.Children do
            createVerticalGridSplitter headersGrid (c.GetValue<int>(Grid.ColumnProperty)) 3
        
        for i,h in List.indexed headers do
            createTextBox headersGrid 1 (i*2+2) (match h.displayName with null -> h.name | a -> a) true |> ignore

    let createRows rowCount columnCount =
        let createRowIndex rowIndex =
            createTextBox itemGrid (rowIndex*2) 0 (string rowIndex) true

        for i in 0..rowCount-1 do
            itemGrid.RowDefinitions.Add(RowDefinition(defaultRowHeight, GridUnitType.Pixel))
            createRowIndex i

            itemGrid.RowDefinitions.Add(RowDefinition(gridSplitterThickness, GridUnitType.Pixel))
            let a = GridSplitter()
            a.SetValue(Grid.RowProperty, i*2+1) |> ignore
            a.SetValue(Grid.ColumnSpanProperty, columnCount*2) |> ignore
            a.HorizontalAlignment <- Layout.HorizontalAlignment.Stretch
            a.Height <- gridSplitterThickness
            a.Background <- Brushes.Black
            itemGrid.Children.Add(a)

        if rowCount >= 1
        then
            itemGrid.RowDefinitions.Add(RowDefinition())
          
    let createTable rowCount headers =
        let columnCount = List.length headers
        items <- List.init rowCount (fun _ -> Array.init columnCount (fun _ -> None)) 
        itemsString <- List.init rowCount (fun _ -> Array.init columnCount (fun _ -> fun _ -> String.Empty)) 

        let columnCount = columnCount + 1
        createColumns rowCount columnCount
        createHeaders headers
        createRows rowCount columnCount

    let setColumnValues (columnIndex,(def:Column,values)) =
        for rowIndex,value in values |> List.map Some |> List.indexed do
            let a = createTextBox itemGrid (rowIndex*2) (columnIndex*2+2) (def.toString value) isReadOnly
            items.[rowIndex].[columnIndex] <- Some (fun () -> def.toValue a.Text) 
            itemsString.[rowIndex].[columnIndex] <- fun () -> a.Text

    let clear() =
        itemGrid.Children.Clear()
        itemGrid.ColumnDefinitions.Clear()
        itemGrid.RowDefinitions.Clear()
        headersGrid.Children.Clear()
        headersGrid.ColumnDefinitions.Clear()
        headersGrid.RowDefinitions.Clear()

    member _.SetItems (columns:Column list) (items:obj list list) =
        clear() 

        if (items |> List.isEmpty |> not) && List.length columns <> (items |> List.item 0 |> List.length)
        then failwith "number of column definitions and columns don't match"

        createTable (List.length items) columns
        if (items |> List.isEmpty |> not)
        then
            items |> List.transpose |> List.zip columns |> List.indexed |> List.iter setColumnValues
            
    member _.GetItems() = items |> List.map (fun a -> a |> Array.map (function Some b -> b() | _ -> None))
    member _.GetItemsString() = itemsString |> List.map (fun a -> a |> Array.map (fun b -> b()))
    member _.IsReadOnly 
        with get() = isReadOnly 
        and set(v) = isReadOnly <- v
    member _.OnItemUpdated 
        with get() = onItemUpdated 
        and set(v) = onItemUpdated <- v
    member _.VerticalScrollBarVisibility
        with get() = isv.VerticalScrollBarVisibility
        and set(v) = isv.VerticalScrollBarVisibility <- v
    member _.HorizontalScrollBarVisibility
        with get() = isv.HorizontalScrollBarVisibility
        and set(v) = isv.HorizontalScrollBarVisibility <- v
