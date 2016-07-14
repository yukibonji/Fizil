﻿module Display

open ExecutionResult
open System
open System.Globalization

type Status = 
    {
        StartTime:           DateTimeOffset
        ElapsedTime:         TimeSpan
        Executions:          uint64
        Crashes:             uint64
        NonZeroExitCodes:    uint64
        Paths:               uint64
        ExecutionsPerSecond: float
        LastCrash:           string option
    }
    with 
        member this.AddExecution(result: Result) =
            let elapsedTime = DateTimeOffset.UtcNow - this.StartTime
            let executions = this.Executions + 1UL
            let executionsPerSecond = 
                if (elapsedTime.TotalMilliseconds > 0.0) 
                then Convert.ToDouble(elapsedTime.TotalMilliseconds) / Convert.ToDouble(executions) / 1000.0
                else 0.0
            {
                StartTime           = this.StartTime
                ElapsedTime         = elapsedTime
                Executions          = executions
                Crashes             = this.Crashes          + (if result.Crashed       then 1UL else 0UL)
                NonZeroExitCodes    = this.NonZeroExitCodes + (if result.ExitCode <> 0 then 1UL else 0UL)
                Paths               = this.Paths            + (if result.NewPathFound  then 1UL else 0UL)
                ExecutionsPerSecond = executionsPerSecond
                LastCrash           = 
                    match result.Crashed, result.HasStdErrOutput with
                    | true, true  -> Some result.StdErr
                    | true, false -> Some result.StdOut
                    | false, _    -> this.LastCrash
            }


let initialState() = 
    {
        StartTime           = DateTimeOffset.UtcNow
        ElapsedTime         = TimeSpan.Zero
        Executions          = 0UL
        Crashes             = 0UL
        NonZeroExitCodes    = 0UL
        Paths               = 0UL
        ExecutionsPerSecond = 0.0
        LastCrash           = None
    }


let private writeValue(title: string) (leftColumnWith: int) (formattedValue: string) =
    Console.ForegroundColor <- ConsoleColor.Gray
    Console.Write ((title.PadLeft leftColumnWith) + " : ")
    Console.ForegroundColor <- ConsoleColor.White
    Console.WriteLine formattedValue


let private writeParagraph(title: string) (leftColumnWith: int) (formattedValue: string option) =
    Console.ForegroundColor <- ConsoleColor.Gray
    match formattedValue with
    | Some value -> 
        Console.WriteLine ((title.PadLeft leftColumnWith) + " : ")
        Console.ForegroundColor <- ConsoleColor.White
        Console.WriteLine formattedValue
    | None -> Console.WriteLine ((title.PadLeft leftColumnWith) + " : <none>")


let private formatTimeSpan(span: TimeSpan) : string =
    sprintf "%d days, %d hrs, %d minutes, %d seconds" span.Days span.Hours span.Minutes span.Seconds


let toConsole(status: Status) =
    Console.Clear()
    Console.BackgroundColor <- ConsoleColor.Black
    Console.SetCursorPosition(0, 0)
    let leftColumnWidth = 19
    writeValue "Elapsed time"       leftColumnWidth (status.ElapsedTime |> formatTimeSpan)
    writeValue "Executions"         leftColumnWidth (status.Executions.ToString(CultureInfo.CurrentUICulture))
    writeValue "Crashes"            leftColumnWidth (status.Crashes.ToString(CultureInfo.CurrentUICulture))
    writeValue "Nonzero exit codes" leftColumnWidth (status.NonZeroExitCodes.ToString(CultureInfo.CurrentUICulture))
    writeValue "Paths"              leftColumnWidth (status.Paths.ToString(CultureInfo.CurrentUICulture))
    writeValue "Executions/second"  leftColumnWidth (status.ExecutionsPerSecond.ToString("G4", CultureInfo.CurrentUICulture))
    writeParagraph "Last crash"     leftColumnWidth status.LastCrash
    status


let update(previousStatus: Status, currentResult: Result) : Status =
    previousStatus.AddExecution(currentResult) 
        |> toConsole
