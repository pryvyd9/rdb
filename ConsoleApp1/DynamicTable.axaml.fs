namespace ConsoleApp1

open System
open Avalonia
open Avalonia.Logging.Serilog
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Markup.Xaml
open Avalonia.Controls
open Avalonia.Media
open Avalonia.Data


type Column = {
    name:string
    validate:string->string option
    toValue:string->obj
    toString:obj->string
}

type DynamicTable() as this =
    inherit UserControl()
    do AvaloniaXamlLoader.Load(this)

    let grid = Grid()
    let headersGrid = Grid()
    let defaultRowHeight = 18.
    let gridSplitterThickness = 3.

    do
        let sv = 
            let sv = ScrollViewer()
            sv.Content <- grid
            sv.VerticalScrollBarVisibility <- Primitives.ScrollBarVisibility.Visible
            sv.HorizontalScrollBarVisibility <- Primitives.ScrollBarVisibility.Visible
            sv.SetValue(Grid.RowProperty, 1)
            sv

        let hgi =
            // Header scrollViewer
            let hsv = ScrollViewer()
            hsv.Content <- headersGrid
            hsv.VerticalScrollBarVisibility <- Primitives.ScrollBarVisibility.Disabled
            hsv.Bind(ScrollViewer.HorizontalScrollBarValueProperty, sv.GetObservable(ScrollViewer.HorizontalScrollBarValueProperty)) |> ignore

            let scrollBarWidth = 18.

            // Header grid internal
            let hgi = Grid()
            hgi.ColumnDefinitions.Add(ColumnDefinition())
            hgi.ColumnDefinitions.Add(ColumnDefinition(scrollBarWidth, GridUnitType.Pixel))
            hgi.Children.Add(hsv)

            hgi

        let mainGrid = Grid()
        mainGrid.RowDefinitions.Add(RowDefinition(defaultRowHeight, GridUnitType.Pixel))
        mainGrid.RowDefinitions.Add(RowDefinition())
        mainGrid.Children.Add(hgi)
        mainGrid.Children.Add(sv)

        this.Content <- mainGrid

    let mutable onItemUpdated:(int->int->string->unit) = (fun i j v -> ())

    let createTextBox (grid:Grid) row column text =
        let a = TextBox()
        a.SetValue(Grid.ColumnProperty, column)
        a.SetValue(Grid.RowProperty, row)
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
        a.SetValue(Grid.ColumnProperty, column)
        a.SetValue(Grid.RowSpanProperty, rowCount)
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
        grid.ColumnDefinitions.Add(ColumnDefinition(rowIndexDefaultWidth, GridUnitType.Pixel))
        grid.ColumnDefinitions.Add(ColumnDefinition(gridSplitterThickness, GridUnitType.Pixel))
        createVerticalGridSplitter grid 1 (rowCount*2) 

        for i in 0..columnCount-2 do
            grid.ColumnDefinitions.Add(ColumnDefinition(200., GridUnitType.Pixel))
            grid.ColumnDefinitions.Add(ColumnDefinition(gridSplitterThickness, GridUnitType.Pixel))
            createVerticalGridSplitter grid (i*2+1+2) (rowCount*2)

        grid.ColumnDefinitions.Add(ColumnDefinition())

    let createHeaders (headers:Column list) =
        for cd in grid.ColumnDefinitions do
            let hcd = ColumnDefinition()
            hcd.Bind(ColumnDefinition.WidthProperty, cd.GetObservable(ColumnDefinition.WidthProperty)) |> ignore
            cd.Bind(ColumnDefinition.WidthProperty, hcd.GetObservable(ColumnDefinition.WidthProperty)) |> ignore
            headersGrid.ColumnDefinitions.Add(hcd)

        for c in grid.Children do
            createVerticalGridSplitter headersGrid (c.GetValue<int>(Grid.ColumnProperty)) 1
        
        for i,h in List.indexed headers do
            createTextBox headersGrid 1 (i*2+2) h.name |> ignore

    let createRows rowCount columnCount =
        let createRowIndex rowIndex =
            let a = createTextBox grid (rowIndex*2) 0 (string rowIndex)
            a.IsReadOnly <- true
            a.CaretBrush <- Brushes.Transparent

        for i in 0..rowCount-1 do
            grid.RowDefinitions.Add(RowDefinition(defaultRowHeight, GridUnitType.Pixel))
            createRowIndex i

            grid.RowDefinitions.Add(RowDefinition(gridSplitterThickness, GridUnitType.Pixel))
            let a = GridSplitter()
            a.SetValue(Grid.RowProperty, i*2+1)
            a.SetValue(Grid.ColumnSpanProperty, columnCount*2)
            a.HorizontalAlignment <- Layout.HorizontalAlignment.Stretch
            a.Height <- gridSplitterThickness
            a.Background <- Brushes.Black
            grid.Children.Add(a)

        if rowCount >= 1
        then
            grid.RowDefinitions.Add(RowDefinition())
          

    let createTable rowCount headers =
        let columnCount = List.length headers + 1
        createColumns rowCount columnCount
        createHeaders headers
        createRows rowCount columnCount

    let setColumnValues (columnIndex,(def:Column,values)) =
        for rowIndex,value in List.indexed values do
            createTextBox grid (rowIndex*2) (columnIndex*2+2) (def.toString value) |> ignore

    member _.OnItemUpdated 
        with get() = onItemUpdated 
        and set(v) = onItemUpdated <- v

    member _.SetItems (columns:Column list) (items:obj list list) filter =
        grid.Children.Clear()
        grid.ColumnDefinitions.Clear()
        grid.RowDefinitions.Clear()
        headersGrid.Children.Clear()
        headersGrid.ColumnDefinitions.Clear()
        headersGrid.RowDefinitions.Clear()

        let items = items |> List.filter filter

        if (items |> List.isEmpty |> not) && List.length columns <> (items |> List.item 0 |> List.length)
        then failwith "number of column definitions and columns don't match"

        createTable (List.length items) columns
        if (items |> List.isEmpty |> not)
        then
            items |> List.transpose |> List.zip columns |> List.indexed |> List.iter setColumnValues

