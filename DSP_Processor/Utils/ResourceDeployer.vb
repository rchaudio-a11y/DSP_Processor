Imports System.IO
Imports System.Reflection
Imports DSP_Processor.Utils

Namespace Utils

    ''' <summary>
    ''' Manages embedded resource deployment to application folders
    ''' </summary>
    Public Class ResourceDeployer

#Region "Public Methods"

        ''' <summary>
        ''' Deploy all embedded test audio files to Recordings folder
        ''' </summary>
        Public Shared Sub DeployTestAudioFiles()
            Try
                Dim recordingsFolder = Path.Combine(Application.StartupPath, "Recordings")
                
                ' Ensure folder exists
                If Not Directory.Exists(recordingsFolder) Then
                    Directory.CreateDirectory(recordingsFolder)
                End If
                
                ' Deploy white noise file
                DeployEmbeddedResource("DSP_Processor.whitenoise.wav", 
                                      Path.Combine(recordingsFolder, "WhiteNoise_Test.wav"))
                
                ' Deploy frequency sweep file
                DeployEmbeddedResource("DSP_Processor.freq20-20000-20s.wav", 
                                      Path.Combine(recordingsFolder, "FreqSweep_20Hz-20kHz_Test.wav"))
                
                ' Deploy calibration signal
                DeployEmbeddedResource("DSP_Processor.Calibration signal.wav", 
                                      Path.Combine(recordingsFolder, "Calibration_Test.wav"))
                
                Logger.Instance.Info("Test audio files deployed successfully", "ResourceDeployer")
                
            Catch ex As Exception
                Logger.Instance.Error("Failed to deploy test audio files", ex, "ResourceDeployer")
            End Try
        End Sub

        ''' <summary>
        ''' Deploy an embedded resource to a target file path
        ''' </summary>
        Private Shared Sub DeployEmbeddedResource(resourceName As String, targetPath As String)
            Try
                ' Skip if file already exists (don't overwrite user recordings)
                If File.Exists(targetPath) Then
                    Logger.Instance.Debug($"Test file already exists, skipping: {Path.GetFileName(targetPath)}", "ResourceDeployer")
                    Return
                End If
                
                ' Get current assembly
                Dim asm As Assembly = Assembly.GetExecutingAssembly()
                
                ' Get embedded resource stream
                Using resourceStream = asm.GetManifestResourceStream(resourceName)
                    If resourceStream Is Nothing Then
                        Logger.Instance.Warning($"Embedded resource not found: {resourceName}", "ResourceDeployer")
                        Return
                    End If
                    
                    ' Copy to target file
                    Using fileStream As New FileStream(targetPath, FileMode.Create, FileAccess.Write)
                        resourceStream.CopyTo(fileStream)
                    End Using
                    
                    Logger.Instance.Info($"Deployed embedded resource: {Path.GetFileName(targetPath)} ({New FileInfo(targetPath).Length} bytes)", "ResourceDeployer")
                End Using
                
            Catch ex As Exception
                Logger.Instance.Error($"Failed to deploy resource: {resourceName}", ex, "ResourceDeployer")
            End Try
        End Sub

        ''' <summary>
        ''' List all embedded resources in the assembly (for debugging)
        ''' </summary>
        Public Shared Function ListEmbeddedResources() As String()
            Try
                Dim asm As Assembly = Assembly.GetExecutingAssembly()
                Return asm.GetManifestResourceNames()
            Catch ex As Exception
                Logger.Instance.Error("Failed to list embedded resources", ex, "ResourceDeployer")
                Return Array.Empty(Of String)()
            End Try
        End Function

#End Region

    End Class

End Namespace
