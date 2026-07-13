Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>
''' Assembly-level test setup (feature 003-tap-consolidation, research R4).
''' Switches Utils.Logger into null-logger mode BEFORE any test code can touch
''' Logger.Instance, so the suite performs zero file I/O.
''' </summary>
<TestClass>
Public Class TestSetup

    <AssemblyInitialize>
    Public Shared Sub AssemblyInit(context As TestContext)
        Utils.Logger.SuppressForTesting = True
    End Sub

    ''' <summary>
    ''' Canary: touching Logger.Instance under suppress mode must not create a
    ''' Logs directory (or any log file) in the test working directory.
    ''' </summary>
    <TestMethod>
    Public Sub Logger_SuppressMode_CreatesNoFiles()
        Dim baseDir = AppContext.BaseDirectory

        ' Force singleton construction and a log call
        Utils.Logger.Instance.Info("canary message - must go nowhere", "TestSetup")
        Utils.Logger.Instance.Warning("canary warning - must go nowhere", "TestSetup")

        Dim logDirs = IO.Directory.GetDirectories(baseDir, "*log*", IO.SearchOption.TopDirectoryOnly)
        Assert.AreEqual(0, logDirs.Length, $"Suppress mode must not create log directories, found: {String.Join(", ", logDirs)}")

        Dim logFiles = IO.Directory.GetFiles(baseDir, "*.log", IO.SearchOption.AllDirectories)
        Assert.AreEqual(0, logFiles.Length, $"Suppress mode must not create log files, found: {String.Join(", ", logFiles)}")
    End Sub

End Class
