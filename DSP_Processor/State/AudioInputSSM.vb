Imports DSP_Processor.AudioIO
Imports System.Management ' For WMI device monitoring

Namespace State

    ''' <summary>
    ''' AudioInput State Machine (SSM) - Controls physical input device selection
    ''' Manages: Device enumeration, selection, availability
    ''' Owner: Device selection state (NOT driver mode - that's AudioDevice SSM)
    ''' </summary>
    Public Class AudioInputSSM
        Implements IStateMachine(Of AudioInputState)

#Region "IStateMachine Implementation"

        Public ReadOnly Property CurrentState As AudioInputState Implements IStateMachine(Of AudioInputState).CurrentState
            Get
                Return _currentState
            End Get
        End Property

        Public Event StateChanged As EventHandler(Of StateChangedEventArgs(Of AudioInputState)) Implements IStateMachine(Of AudioInputState).StateChanged

        Public Function IsValidTransition(fromState As AudioInputState, toState As AudioInputState) As Boolean Implements IStateMachine(Of AudioInputState).IsValidTransition
            ' Same state is always valid
            If fromState = toState Then Return True

            Select Case fromState
                Case AudioInputState.Uninitialized
                    ' Can go to device selected or error
                    Return toState = AudioInputState.DeviceSelected OrElse
                           toState = AudioInputState.ErrorState

                Case AudioInputState.DeviceSelected
                    ' Can switch devices, become unavailable, or error
                    Return toState = AudioInputState.DeviceUnavailable OrElse
                           toState = AudioInputState.Uninitialized OrElse
                           toState = AudioInputState.ErrorState

                Case AudioInputState.DeviceUnavailable
                    ' Can recover to device selected or re-initialize
                    Return toState = AudioInputState.DeviceSelected OrElse
                           toState = AudioInputState.Uninitialized OrElse
                           toState = AudioInputState.ErrorState

                Case AudioInputState.ErrorState
                    ' Can recover to any state
                    Return True

                Case Else
                    Return False
            End Select
        End Function

#End Region

#Region "Private Fields"

        Private _currentState As AudioInputState
        Private _initialized As Boolean = False
        Private _currentDeviceIndex As Integer = -1
        
        ' USB device monitoring (WMI-based)
        Private _deviceInsertionWatcher As ManagementEventWatcher
        Private _deviceRemovalWatcher As ManagementEventWatcher
        Private _monitoringActive As Boolean = False

#End Region

#Region "Initialization"

        Public Sub New()
            _currentState = AudioInputState.Uninitialized
        End Sub

        ''' <summary>
        ''' Initialize AudioInput SSM with default device
        ''' </summary>
        Public Function Initialize() As Boolean
            If _initialized Then
                Utils.Logger.Instance.Warning("AudioInput SSM already initialized", "AudioInputSSM")
                Return False
            End If

            Try
                ' Enumerate devices and select first one
                Dim devices = AudioInputManager.Instance.GetDevices(AudioInputManager.Instance.CurrentDriver)
                
                If devices.Count > 0 Then
                    ' Select first device (index 0)
                    Dim success = TransitionTo(AudioInputState.DeviceSelected, "System initialization - default device")
                    
                    If success Then
                        _currentDeviceIndex = 0
                        _initialized = True
                        
                        ' Start USB device monitoring
                        StartUSBMonitoring()
                        
                        Utils.Logger.Instance.Info($"? AudioInput SSM initialized (Device 0) with USB monitoring", "AudioInputSSM")
                    Else
                        Utils.Logger.Instance.Error("? AudioInput SSM initialization failed", Nothing, "AudioInputSSM")
                    End If

                    Return success
                Else
                    Utils.Logger.Instance.Warning("No audio input devices found", "AudioInputSSM")
                    Return TransitionTo(AudioInputState.ErrorState, "No devices found")
                End If

            Catch ex As Exception
                Utils.Logger.Instance.Error("AudioInput SSM initialization exception", ex, "AudioInputSSM")
                Return False
            End Try
        End Function

#End Region

#Region "State Transitions"

        ''' <summary>
        ''' Attempt to transition to a new device state
        ''' </summary>
        Public Function TransitionTo(newState As AudioInputState, reason As String) As Boolean Implements IStateMachine(Of AudioInputState).TransitionTo
            ' Validate transition
            If Not IsValidTransition(_currentState, newState) Then
                Utils.Logger.Instance.Warning($"Invalid transition: {_currentState} ? {newState} (Reason: {reason})", "AudioInputSSM")
                Return False
            End If

            ' Validate preconditions (global state, armed mic, etc.)
            Dim validationMessage As String = Nothing
            If Not ValidateTransition(newState, validationMessage) Then
                Utils.Logger.Instance.Warning($"Transition validation failed: {validationMessage}", "AudioInputSSM")
                Return False
            End If

            ' Execute transition
            Dim oldState = _currentState
            Dim transitionID = GenerateTransitionID(oldState, newState)

            Try
                ' Exit actions for old state
                ExecuteExitActions(oldState)

                ' Change state
                _currentState = newState

                ' Entry actions for new state
                ExecuteEntryActions(newState)

                ' Raise state change event
                Dim args As New StateChangedEventArgs(Of AudioInputState)(oldState, newState, reason)
                RaiseEvent StateChanged(Me, args)

                Utils.Logger.Instance.Info($"?? AudioInput: {oldState} ? {newState} | {transitionID} | {reason}", "AudioInputSSM")

                Return True

            Catch ex As Exception
                Utils.Logger.Instance.Error($"Transition failed: {oldState} ? {newState}", ex, "AudioInputSSM")
                
                ' Try to recover to old state
                _currentState = oldState
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Validate preconditions for transition (GlobalState, armed mic, etc.)
        ''' </summary>
        Private Function ValidateTransition(newState As AudioInputState, ByRef message As String) As Boolean
            ' Switching devices requires system to be idle
            If _currentState = AudioInputState.DeviceSelected AndAlso newState = AudioInputState.DeviceSelected Then
                ' Check GlobalStateMachine state
                Dim coordinator = StateCoordinator.Instance
                If coordinator IsNot Nothing Then
                    Dim globalState = coordinator.GlobalState

                    ' Cannot switch during Recording or Playing
                    If globalState = GlobalState.Recording Then
                        message = "Cannot switch audio input device while recording"
                        Return False
                    End If

                    If globalState = GlobalState.Playing Then
                        message = "Cannot switch audio input device during playback"
                        Return False
                    End If

                    ' Check if microphone is armed
                    Dim recSSM = coordinator.RecordingManagerSSM
                    If recSSM IsNot Nothing AndAlso recSSM.IsArmed Then
                        message = "Cannot switch audio input device while microphone is armed"
                        Return False
                    End If
                End If
            End If

            Return True
        End Function

        ''' <summary>
        ''' Execute exit actions when leaving a state
        ''' </summary>
        Private Sub ExecuteExitActions(state As AudioInputState)
            Select Case state
                Case AudioInputState.DeviceSelected
                    ' Teardown current device (if needed)
                    Utils.Logger.Instance.Debug($"Exiting device selection (index {_currentDeviceIndex})", "AudioInputSSM")
            End Select
        End Sub

        ''' <summary>
        ''' Execute entry actions when entering a state
        ''' </summary>
        Private Sub ExecuteEntryActions(state As AudioInputState)
            Select Case state
                Case AudioInputState.DeviceSelected
                    ' Device already set by RequestDeviceChange
                    Utils.Logger.Instance.Info($"Audio input device selected (index {_currentDeviceIndex})", "AudioInputSSM")

                Case AudioInputState.DeviceUnavailable
                    Utils.Logger.Instance.Warning("Audio input device unavailable", "AudioInputSSM")

                Case AudioInputState.ErrorState
                    Utils.Logger.Instance.Error("AudioInput entered ERROR state", Nothing, "AudioInputSSM")
            End Select
        End Sub

#End Region

#Region "Public API"

        ''' <summary>
        ''' Request device change (called from UI)
        ''' </summary>
        Public Function RequestDeviceChange(deviceIndex As Integer, reason As String) As Boolean
            If deviceIndex < 0 Then
                Utils.Logger.Instance.Warning("Invalid device index", "AudioInputSSM")
                Return False
            End If

            ' Validate device exists
            Dim devices = AudioInputManager.Instance.GetDevices(AudioInputManager.Instance.CurrentDriver)
            If deviceIndex >= devices.Count Then
                Utils.Logger.Instance.Warning($"Device index {deviceIndex} out of range", "AudioInputSSM")
                Return False
            End If

            ' Store device index
            _currentDeviceIndex = deviceIndex

            ' Transition to DeviceSelected (validation will check if allowed)
            Return TransitionTo(AudioInputState.DeviceSelected, reason)
        End Function

        ''' <summary>
        ''' Refresh device list (called when driver changes)
        ''' </summary>
        Public Function RefreshDevices(reason As String) As Boolean
            ' Transition to Uninitialized, then back to DeviceSelected with default device
            If Not TransitionTo(AudioInputState.Uninitialized, reason) Then
                Return False
            End If

            ' Re-enumerate and select first device
            Return Initialize()
        End Function

        ''' <summary>
        ''' Get current device index
        ''' </summary>
        Public ReadOnly Property CurrentDeviceIndex As Integer
            Get
                Return _currentDeviceIndex
            End Get
        End Property

#End Region

#Region "Helper Methods"

        Private Function GenerateTransitionID(oldState As AudioInputState, newState As AudioInputState) As String
            Return $"AUDIOINPUT_{oldState}_{newState}"
        End Function

#End Region

#Region "USB Device Monitoring"

        ''' <summary>
        ''' Start monitoring for USB device insertion/removal
        ''' Uses WMI (Windows Management Instrumentation) to detect hardware changes
        ''' </summary>
        Private Sub StartUSBMonitoring()
            If _monitoringActive Then Return

            Try
                ' WMI query for USB device insertion (Win32_DeviceChangeEvent with EventType=2)
                Dim insertionQuery As New WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2")
                _deviceInsertionWatcher = New ManagementEventWatcher(insertionQuery)
                AddHandler _deviceInsertionWatcher.EventArrived, AddressOf OnDeviceInserted

                ' WMI query for USB device removal (Win32_DeviceChangeEvent with EventType=3)
                Dim removalQuery As New WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 3")
                _deviceRemovalWatcher = New ManagementEventWatcher(removalQuery)
                AddHandler _deviceRemovalWatcher.EventArrived, AddressOf OnDeviceRemoved

                ' Start monitoring
                _deviceInsertionWatcher.Start()
                _deviceRemovalWatcher.Start()

                _monitoringActive = True
                Utils.Logger.Instance.Info("USB device monitoring started", "AudioInputSSM")

            Catch ex As Exception
                Utils.Logger.Instance.Error("Failed to start USB monitoring", ex, "AudioInputSSM")
            End Try
        End Sub

        ''' <summary>
        ''' Stop USB device monitoring and cleanup
        ''' </summary>
        Private Sub StopUSBMonitoring()
            If Not _monitoringActive Then Return

            Try
                If _deviceInsertionWatcher IsNot Nothing Then
                    _deviceInsertionWatcher.Stop()
                    RemoveHandler _deviceInsertionWatcher.EventArrived, AddressOf OnDeviceInserted
                    _deviceInsertionWatcher.Dispose()
                    _deviceInsertionWatcher = Nothing
                End If

                If _deviceRemovalWatcher IsNot Nothing Then
                    _deviceRemovalWatcher.Stop()
                    RemoveHandler _deviceRemovalWatcher.EventArrived, AddressOf OnDeviceRemoved
                    _deviceRemovalWatcher.Dispose()
                    _deviceRemovalWatcher = Nothing
                End If

                _monitoringActive = False
                Utils.Logger.Instance.Info("USB device monitoring stopped", "AudioInputSSM")

            Catch ex As Exception
                Utils.Logger.Instance.Error("Error stopping USB monitoring", ex, "AudioInputSSM")
            End Try
        End Sub

        ''' <summary>
        ''' Called when a USB device is inserted
        ''' </summary>
        Private Sub OnDeviceInserted(sender As Object, e As EventArrivedEventArgs)
            Try
                Utils.Logger.Instance.Info("USB device inserted detected", "AudioInputSSM")
                
                ' Re-enumerate devices to see if a new audio device is available
                ' This runs on WMI event thread, so we need to be careful
                Dim devices = AudioInputManager.Instance.GetDevices(AudioInputManager.Instance.CurrentDriver)
                
                ' If we're in DeviceUnavailable state and devices are now available, offer recovery
                If _currentState = AudioInputState.DeviceUnavailable AndAlso devices.Count > 0 Then
                    Utils.Logger.Instance.Info("Audio input device became available - transitioning to DeviceSelected", "AudioInputSSM")
                    TransitionTo(AudioInputState.DeviceSelected, "USB device reconnected")
                End If

            Catch ex As Exception
                Utils.Logger.Instance.Error("Error handling device insertion", ex, "AudioInputSSM")
            End Try
        End Sub

        ''' <summary>
        ''' Called when a USB device is removed
        ''' </summary>
        Private Sub OnDeviceRemoved(sender As Object, e As EventArrivedEventArgs)
            Try
                Utils.Logger.Instance.Warning("USB device removal detected", "AudioInputSSM")
                
                ' Check if current device is still available
                Dim devices = AudioInputManager.Instance.GetDevices(AudioInputManager.Instance.CurrentDriver)
                
                ' If current device index is out of range, device was removed
                If _currentDeviceIndex >= devices.Count Then
                    Utils.Logger.Instance.Warning($"Current audio device (index {_currentDeviceIndex}) was removed", "AudioInputSSM")
                    TransitionTo(AudioInputState.DeviceUnavailable, "USB device unplugged")
                End If

            Catch ex As Exception
                Utils.Logger.Instance.Error("Error handling device removal", ex, "AudioInputSSM")
            End Try
        End Sub

        ''' <summary>
        ''' Dispose USB monitoring resources
        ''' </summary>
        Public Sub Dispose()
            StopUSBMonitoring()
        End Sub

#End Region

    End Class

    ''' <summary>
    ''' AudioInput SSM States
    ''' </summary>
    Public Enum AudioInputState
        Uninitialized
        DeviceSelected
        DeviceUnavailable
        ErrorState
    End Enum

End Namespace
