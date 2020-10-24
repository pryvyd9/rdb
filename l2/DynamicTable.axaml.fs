namespace l2

open System
open Avalonia
//open Avalonia.Logging.Serilog
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Markup.Xaml
open Avalonia.Controls
open Avalonia.Media
open Avalonia.Data



type DynamicTable(showScrollBars) as this =
    inherit UserControl()
    do AvaloniaXamlLoader.Load(this)

    let itemGrid = this.FindControl<Grid>("itemGrid")
    let headersGrid = this.FindControl<Grid>("headerGrid")
    let hsv = this.FindControl<ScrollViewer>("headerScrollViewer")
    let isv = this.FindControl<ScrollViewer>("itemScrollViewer")

    let defaultRowHeight = 18.
    let gridSplitterThickness = 1.

    do
        let p = ScrollViewer.HorizontalScrollBarValueProperty
        hsv.Bind(p, isv.GetObservable(p)) |> ignore

    let mutable onItemUpdated:(int->int->string->unit) = (fun i j v -> ())

    let createTextBox (grid:Grid) row column text =
        let a = TextBox()
        a.SetValue(Grid.ColumnProperty, column) |> ignore
        a.SetValue(Grid.RowProperty, row) |> ignore
        a.Text <- text
        a.BorderThickness <- Thickness()
        a.Padding <- Thickness()
        a.Margin <- Thickness(1.)
        a.TextInput.AddHandler(fun o b -> onItemUpdated row column b.Text)
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
            createTextBox headersGrid 1 (i*2+2) h.name |> ignore


    let createRows rowCount columnCount =
        let createRowIndex rowIndex =
            let a = createTextBox itemGrid (rowIndex*2) 0 (string rowIndex)
            a.IsReadOnly <- true
            a.CaretBrush <- Brushes.Transparent

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
        let columnCount = List.length headers + 1
        createColumns rowCount columnCount
        createHeaders headers
        createRows rowCount columnCount

    let setColumnValues (columnIndex,(def:Column,values)) =
        for rowIndex,value in List.indexed values do
            createTextBox itemGrid (rowIndex*2) (columnIndex*2+2) (def.toString value) |> ignore

    let clear() =
        itemGrid.Children.Clear()
        itemGrid.ColumnDefinitions.Clear()
        itemGrid.RowDefinitions.Clear()
        headersGrid.Children.Clear()
        headersGrid.ColumnDefinitions.Clear()
        headersGrid.RowDefinitions.Clear()

    new() = DynamicTable(true)

    member _.SetItems (columns:Column list) (items:obj list list) =
        clear() 

        if (items |> List.isEmpty |> not) && List.length columns <> (items |> List.item 0 |> List.length)
        then failwith "number of column definitions and columns don't match"

        createTable (List.length items) columns
        if (items |> List.isEmpty |> not)
        then
            items |> List.transpose |> List.zip columns |> List.indexed |> List.iter setColumnValues
            
    member _.OnItemUpdated 
        with get() = onItemUpdated 
        and set(v) = onItemUpdated <- v
    member _.VerticalScrollBarVisibility
        with get() = isv.VerticalScrollBarVisibility
        and set(v) = isv.VerticalScrollBarVisibility <- v
    member _.HorizontalScrollBarVisibility
        with get() = isv.HorizontalScrollBarVisibility
        and set(v) = isv.HorizontalScrollBarVisibility <- v
