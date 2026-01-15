Namespace AudioIO

    ''' <summary>
    ''' Enumeration of audio driver types supported by the application.
    ''' </summary>
    ''' <remarks>
    ''' Created: Phase 1, Task 1.1
    ''' Purpose: Support multiple audio API backends for flexibility and performance
    ''' </remarks>
    Public Enum DriverType
        ''' <summary>Legacy WDM (Windows Driver Model) audio drivers</summary>
        ''' <remarks>
        ''' Most compatible option, available on all Windows systems.
        ''' Higher latency (~20-50ms) but universal support.
        ''' </remarks>
        WaveIn = 0

        ''' <summary>Windows Audio Session API (Windows Vista and later)</summary>
        ''' <remarks>
        ''' Modern Windows audio API with lower latency and better quality.
        ''' Exclusive mode: 3-10ms latency
        ''' Shared mode: 10-30ms latency
        ''' </remarks>
        WASAPI = 1

        ''' <summary>Audio Stream Input/Output (professional audio interfaces)</summary>
        ''' <remarks>
        ''' Professional audio driver standard for lowest latency (less than 5ms).
        ''' Requires ASIO-compatible hardware or ASIO4ALL driver.
        ''' Optional - only available if drivers installed.
        ''' </remarks>
        ASIO = 2

        ''' <summary>Legacy DirectSound drivers (optional, rarely used)</summary>
        ''' <remarks>
        ''' Older DirectX audio API, included for compatibility.
        ''' Generally superseded by WASAPI on modern Windows.
        ''' </remarks>
        DirectSound = 3
    End Enum

End Namespace
