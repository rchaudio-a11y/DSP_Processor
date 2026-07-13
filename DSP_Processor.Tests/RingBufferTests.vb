Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>
''' FR-010: behavior-locking tests for the SPSC ring buffer.
''' Locks CURRENT behavior (feature 003, US1) - written before any production change.
''' </summary>
<TestClass>
Public Class RingBufferTests

    Private Shared Function Pattern(count As Integer, Optional seedOffset As Integer = 0) As Byte()
        Dim data(count - 1) As Byte
        For i = 0 To count - 1
            data(i) = CByte((i + seedOffset) Mod 256)
        Next
        Return data
    End Function

#Region "Capacity & initial state"

    <TestMethod>
    Public Sub Constructor_RoundsCapacityUpToPowerOfTwo()
        Using rb As New Utils.RingBuffer(1000)
            Assert.AreEqual(1024, rb.Capacity)
        End Using
        Using rb As New Utils.RingBuffer(1024)
            Assert.AreEqual(1024, rb.Capacity)
        End Using
        Using rb As New Utils.RingBuffer(1)
            Assert.AreEqual(1, rb.Capacity)
        End Using
    End Sub

    <TestMethod>
    Public Sub Constructor_NonPositiveSize_Throws()
        Assert.ThrowsException(Of ArgumentException)(Function() New Utils.RingBuffer(0))
        Assert.ThrowsException(Of ArgumentException)(Function() New Utils.RingBuffer(-5))
    End Sub

    <TestMethod>
    Public Sub NewBuffer_IsEmpty_FreeSpaceIsCapacityMinusOne()
        ' One byte is reserved to disambiguate full from empty (SPSC design)
        Using rb As New Utils.RingBuffer(64)
            Assert.AreEqual(0, rb.Available)
            Assert.IsTrue(rb.IsEmpty)
            Assert.IsFalse(rb.IsFull)
            Assert.AreEqual(63, rb.FreeSpace)
        End Using
    End Sub

#End Region

#Region "Round-trip & accounting (FR-010)"

    <TestMethod>
    Public Sub WriteThenRead_RoundTripsExactBytes()
        Using rb As New Utils.RingBuffer(64)
            Dim src = Pattern(40)
            Assert.AreEqual(40, rb.Write(src, 0, 40))
            Assert.AreEqual(40, rb.Available)
            Assert.AreEqual(23, rb.FreeSpace)

            Dim dst(39) As Byte
            Assert.AreEqual(40, rb.Read(dst, 0, 40))
            CollectionAssert.AreEqual(src, dst)
            Assert.AreEqual(0, rb.Available)
        End Using
    End Sub

    <TestMethod>
    Public Sub Write_ClampsToFreeSpace_BufferReportsFull()
        Using rb As New Utils.RingBuffer(64)
            Dim src = Pattern(100)
            ' Only capacity-1 = 63 bytes fit
            Assert.AreEqual(63, rb.Write(src, 0, 100))
            Assert.IsTrue(rb.IsFull)
            Assert.AreEqual(0, rb.FreeSpace)

            ' Full buffer accepts nothing more
            Assert.AreEqual(0, rb.Write(src, 0, 10))
        End Using
    End Sub

    <TestMethod>
    Public Sub Read_RequestMoreThanAvailable_ReturnsOnlyAvailable()
        Using rb As New Utils.RingBuffer(64)
            rb.Write(Pattern(10), 0, 10)

            Dim dst(63) As Byte
            Assert.AreEqual(10, rb.Read(dst, 0, 64))
            CollectionAssert.AreEqual(Pattern(10), dst.Take(10).ToArray())
            ' Read from empty returns 0
            Assert.AreEqual(0, rb.Read(dst, 0, 64))
        End Using
    End Sub

#End Region

#Region "Wraparound (FR-010)"

    <TestMethod>
    Public Sub Wraparound_DataIntegrityAcrossBoundary()
        Using rb As New Utils.RingBuffer(16)
            ' Advance positions to near the physical end, then write across it
            Dim scratch(15) As Byte
            rb.Write(Pattern(10), 0, 10)
            rb.Read(scratch, 0, 10) ' positions now at 10

            Dim src = Pattern(12, seedOffset:=100) ' 12 bytes wraps: 6 at end + 6 at start
            Assert.AreEqual(12, rb.Write(src, 0, 12))
            Assert.AreEqual(12, rb.Available)

            Dim dst(11) As Byte
            Assert.AreEqual(12, rb.Read(dst, 0, 12))
            CollectionAssert.AreEqual(src, dst)
        End Using
    End Sub

    <TestMethod>
    Public Sub Wraparound_ManyCycles_NoCorruption()
        Using rb As New Utils.RingBuffer(32)
            Dim dst(30) As Byte
            For cycle = 0 To 99
                Dim src = Pattern(23, seedOffset:=cycle) ' 23 and 32 are coprime -> every offset hit
                Assert.AreEqual(23, rb.Write(src, 0, 23), $"write failed at cycle {cycle}")
                Assert.AreEqual(23, rb.Read(dst, 0, 23), $"read failed at cycle {cycle}")
                CollectionAssert.AreEqual(src, dst.Take(23).ToArray(), $"corruption at cycle {cycle}")
            Next
        End Using
    End Sub

#End Region

#Region "Skip / Clear / argument & dispose contracts"

    <TestMethod>
    Public Sub Skip_AdvancesReadPosition_ClampsToAvailable()
        Using rb As New Utils.RingBuffer(64)
            rb.Write(Pattern(20), 0, 20)
            Assert.AreEqual(5, rb.Skip(5))
            Assert.AreEqual(15, rb.Available)

            Dim dst(14) As Byte
            rb.Read(dst, 0, 15)
            ' First byte read should be pattern index 5
            Assert.AreEqual(CByte(5), dst(0))

            rb.Write(Pattern(10), 0, 10)
            Assert.AreEqual(10, rb.Skip(64)) ' clamps to available
        End Using
    End Sub

    <TestMethod>
    Public Sub Clear_ResetsToEmpty()
        Using rb As New Utils.RingBuffer(64)
            rb.Write(Pattern(30), 0, 30)
            rb.Clear()
            Assert.AreEqual(0, rb.Available)
            Assert.IsTrue(rb.IsEmpty)
            Assert.AreEqual(63, rb.FreeSpace)
        End Using
    End Sub

    <TestMethod>
    Public Sub ArgumentValidation_Throws()
        Using rb As New Utils.RingBuffer(64)
            Dim buf(9) As Byte
            Assert.ThrowsException(Of ArgumentNullException)(Sub() rb.Write(Nothing, 0, 1))
            Assert.ThrowsException(Of ArgumentNullException)(Sub() rb.Read(Nothing, 0, 1))
            Assert.ThrowsException(Of ArgumentOutOfRangeException)(Sub() rb.Write(buf, -1, 1))
            Assert.ThrowsException(Of ArgumentOutOfRangeException)(Sub() rb.Write(buf, 0, 11))
            Assert.ThrowsException(Of ArgumentOutOfRangeException)(Sub() rb.Read(buf, 11, 1))
            Assert.ThrowsException(Of ArgumentOutOfRangeException)(Sub() rb.Skip(-1))
        End Using
    End Sub

    <TestMethod>
    Public Sub Dispose_SubsequentOperationsThrow()
        Dim rb As New Utils.RingBuffer(64)
        rb.Dispose()
        Dim buf(9) As Byte
        Assert.ThrowsException(Of ObjectDisposedException)(Sub() rb.Write(buf, 0, 10))
        Assert.ThrowsException(Of ObjectDisposedException)(Sub() rb.Read(buf, 0, 10))
        Assert.ThrowsException(Of ObjectDisposedException)(Sub() rb.Skip(1))
    End Sub

#End Region

End Class
