Namespace Managers
    
    ''' <summary>
    ''' Health status for tap point readers
    ''' Used by MonitoringController to track reader activity
    ''' </summary>
    Public Enum ReaderHealth
        ''' <summary>Reader is healthy and actively reading data</summary>
        Healthy = 0
        
        ''' <summary>Reader hasn't read in >5 seconds (warning)</summary>
        Stale = 1
        
        ''' <summary>Reader hasn't read in >30 seconds (critical)</summary>
        Dead = 2
        
        ''' <summary>Not enough data to determine health</summary>
        Unknown = 3
    End Enum
    
End Namespace
