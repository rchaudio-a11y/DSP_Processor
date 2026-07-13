Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>
''' FR-011: multi-reader independence and overrun detection
''' (feature 003, US2 - written against the NEW monotonic-counter contract).
''' Oracle: data-model.md invariants 1-5 and contracts/monitor-reader-api.md.
''' </summary>
<TestClass>
Public Class MultiReaderRingBufferTests

    Private Const Cap As Integer = 64

    Private Shared Function Pattern(count As Integer, Optional seedOffset As Integer = 0) As Byte()
        Dim data(count - 1) As Byte
        For i = 0 To count - 1
            data(i) = CByte((i + seedOffset) Mod 256)
        Next
        Return data
    End Function

#Region "Reader lifecycle"

    <TestMethod>
    Public Sub CreateReader_StartsAtLiveEdge_NoPhantomBacklog()
        Using mrb As New Utils.MultiReaderRingBuffer(Cap)
            mrb.Write(Pattern(40), 0, 40) ' data written BEFORE reader exists
            mrb.CreateReader("late")

            Assert.AreEqual(0, mrb.Available("late"), "a new reader must not see pre-creation data")
            Dim stats = mrb.GetReaderStats("late")
            Assert.AreEqual(0, stats.OverrunEvents, "creation must not register a false overrun")
        End Using
    End Sub

    <TestMethod>
    Public Sub CreateReader_DuplicateName_Throws()
        Using mrb As New Utils.MultiReaderRingBuffer(Cap)
            mrb.CreateReader("dup")
            Assert.ThrowsException(Of InvalidOperationException)(Function() mrb.CreateReader("dup"))
        End Using
    End Sub

    <TestMethod>
    Public Sub RemoveReader_LeavesOtherReadersIntact()
        Using mrb As New Utils.MultiReaderRingBuffer(Cap)
            mrb.CreateReader("keep")
            mrb.CreateReader("drop")
            mrb.Write(Pattern(20), 0, 20)

            mrb.RemoveReader("drop")

            Assert.IsFalse(mrb.HasReader("drop"))
            Assert.IsTrue(mrb.HasReader("keep"))
            Assert.AreEqual(20, mrb.Available("keep"), "surviving reader's pending data must be untouched")
            Assert.AreEqual(1, mrb.ReaderCount)
        End Using
    End Sub

    <TestMethod>
    Public Sub Read_UnknownReader_Throws_AvailableReturnsZero()
        Using mrb As New Utils.MultiReaderRingBuffer(Cap)
            Dim dst(9) As Byte
            Assert.ThrowsException(Of InvalidOperationException)(Function() mrb.Read("ghost", dst, 0, 10))
            Assert.AreEqual(0, mrb.Available("ghost"))
        End Using
    End Sub

#End Region

#Region "Independence (FR-004)"

    <TestMethod>
    Public Sub TwoReaders_IndependentPositions()
        Using mrb As New Utils.MultiReaderRingBuffer(Cap)
            mrb.CreateReader("fast")
            mrb.CreateReader("slow")
            Dim src = Pattern(30)
            mrb.Write(src, 0, 30)

            ' fast consumes everything; slow consumes nothing
            Dim dst(29) As Byte
            Assert.AreEqual(30, mrb.Read("fast", dst, 0, 30))
            CollectionAssert.AreEqual(src, dst)

            Assert.AreEqual(0, mrb.Available("fast"))
            Assert.AreEqual(30, mrb.Available("slow"), "reading via one reader must not move another")

            ' slow still gets the identical bytes
            Dim dst2(29) As Byte
            Assert.AreEqual(30, mrb.Read("slow", dst2, 0, 30))
            CollectionAssert.AreEqual(src, dst2)
        End Using
    End Sub

    <TestMethod>
    Public Sub StalledReaderOverrun_DoesNotAffectHealthyReader()
        Using mrb As New Utils.MultiReaderRingBuffer(Cap)
            mrb.CreateReader("healthy")
            mrb.CreateReader("stalled")
            Dim dst(Cap - 1) As Byte

            ' Healthy reader keeps up while stalled one never reads
            For round = 0 To 3
                Dim src = Pattern(32, seedOffset:=round * 32)
                mrb.Write(src, 0, 32)
                Assert.AreEqual(32, mrb.Read("healthy", dst, 0, 32), $"round {round}")
                CollectionAssert.AreEqual(src, dst.Take(32).ToArray(), $"healthy data corrupted in round {round}")
            Next

            Dim healthyStats = mrb.GetReaderStats("healthy")
            Assert.AreEqual(0, healthyStats.OverrunEvents, "healthy reader must never see an overrun")

            Dim stalledStats = mrb.GetReaderStats("stalled")
            Assert.AreEqual(1, stalledStats.OverrunEvents)
            Assert.AreEqual(CLng(4 * 32 - Cap), stalledStats.TotalBytesLost)
        End Using
    End Sub

#End Region

#Region "Overrun detection (FR-005/006/007) - the aliasing killers"

    <TestMethod>
    Public Sub ExactSingleLap_Detected_NotAliasedToZero()
        ' THE bug this feature exists to fix: under Mod arithmetic a reader
        ' lapped by exactly one capacity showed Available = 0 (silent total loss).
        Using mrb As New Utils.MultiReaderRingBuffer(Cap)
            mrb.CreateReader("r")

            mrb.Write(Pattern(Cap), 0, Cap)          ' fills buffer: pending = capacity, NOT overrun
            Assert.AreEqual(Cap, mrb.Available("r"), "pending = capacity exactly is still fully readable")

            mrb.Write(Pattern(Cap, 100), 0, Cap)     ' second full write: pending = 2*capacity

            Dim stats = mrb.GetReaderStats("r")
            Assert.AreEqual(1, stats.OverrunEvents, "exact lap must be detected")
            Assert.AreEqual(CLng(Cap), stats.TotalBytesLost, "exactly one buffer of data was lost")
            Assert.AreEqual(CLng(Cap), stats.Pending, "the newest full buffer remains readable")
        End Using
    End Sub

    <TestMethod>
    Public Sub MultiLap_LossTotalsReflectAllLaps()
        Using mrb As New Utils.MultiReaderRingBuffer(Cap)
            mrb.CreateReader("r")

            ' Three full-buffer writes with no reads: pending = 3*capacity
            For lap = 0 To 2
                mrb.Write(Pattern(Cap, lap * 10), 0, Cap)
            Next

            Dim lost As Long
            Dim dst(Cap - 1) As Byte
            Dim bytesRead = mrb.Read("r", dst, 0, Cap, lost)

            Assert.AreEqual(CLng(2 * Cap), lost, "loss must reflect the TOTAL overwritten, not just the last lap")
            Assert.AreEqual(Cap, bytesRead)
        End Using
    End Sub

    <TestMethod>
    Public Sub OverrunRead_NeverReturnsSplicedAudio()
        ' FR-007: after a lap, a read must return ONLY the newest contiguous
        ' window - not a mixture of old and new bytes.
        Using mrb As New Utils.MultiReaderRingBuffer(Cap)
            mrb.CreateReader("r")

            Dim old = Pattern(Cap, seedOffset:=0)     ' first window
            Dim fresh = Pattern(Cap, seedOffset:=77)  ' second window, distinct values
            mrb.Write(old, 0, Cap)
            ' Lap the reader with 1.5 buffers of fresh data, in two writes
            mrb.Write(fresh, 0, Cap)
            mrb.Write(Pattern(Cap \ 2, seedOffset:=200), 0, Cap \ 2)

            Dim dst(Cap - 1) As Byte
            Dim lost As Long
            Dim bytesRead = mrb.Read("r", dst, 0, Cap, lost)

            Assert.IsTrue(lost > 0, "loss must be reported")
            Assert.AreEqual(Cap, bytesRead)
            ' Returned window must be exactly the oldest-valid contiguous bytes:
            ' last half of 'fresh' followed by the 200-seeded half buffer
            Dim expected = fresh.Skip(Cap \ 2).Concat(Pattern(Cap \ 2, seedOffset:=200)).ToArray()
            CollectionAssert.AreEqual(expected, dst, "read must be the contiguous newest window, never a splice")
        End Using
    End Sub

    <TestMethod>
    Public Sub ByRefOverload_ReportsLossAtCallSite()
        Using mrb As New Utils.MultiReaderRingBuffer(Cap)
            mrb.CreateReader("r")
            mrb.Write(Pattern(Cap), 0, Cap)
            mrb.Write(Pattern(50, 5), 0, 50) ' pending = capacity + 50

            Dim dst(Cap - 1) As Byte
            Dim lost As Long = -1
            mrb.Read("r", dst, 0, 10, lost)
            Assert.AreEqual(50L, lost, "ByRef overload must report the loss of THIS interaction")

            ' Next read: no further loss
            mrb.Read("r", dst, 0, 10, lost)
            Assert.AreEqual(0L, lost)
        End Using
    End Sub

    <TestMethod>
    Public Sub Available_DetectsAndResyncs_ClampsToCapacity()
        Using mrb As New Utils.MultiReaderRingBuffer(Cap)
            mrb.CreateReader("r")
            mrb.Write(Pattern(Cap), 0, Cap)
            mrb.Write(Pattern(Cap), 0, Cap)
            mrb.Write(Pattern(30), 0, 30)

            Dim avail = mrb.Available("r")
            Assert.AreEqual(Cap, avail, "Available must clamp to capacity after resync")

            Dim stats = mrb.GetReaderStats("r")
            Assert.IsTrue(stats.OverrunEvents >= 1, "the Available poll path must detect the lap")
        End Using
    End Sub

    <TestMethod>
    Public Sub PartialRead_ReturnsExactAvailableCount_NoPadding()
        ' Analysis C2: request more than available -> exact count, tail untouched
        Using mrb As New Utils.MultiReaderRingBuffer(Cap)
            mrb.CreateReader("r")
            mrb.Write(Pattern(10), 0, 10)

            Dim dst(Cap - 1) As Byte
            For i = 0 To Cap - 1 : dst(i) = &HEE : Next ' sentinel fill

            Assert.AreEqual(10, mrb.Read("r", dst, 0, Cap))
            CollectionAssert.AreEqual(Pattern(10), dst.Take(10).ToArray())
            Assert.IsTrue(dst.Skip(10).All(Function(b) b = &HEE), "bytes beyond the returned count must be untouched (no padding)")
        End Using
    End Sub

#End Region

#Region "Episode semantics & rate-limited logging (FR-008, analyses B1/E1)"

    <TestMethod>
    Public Sub EpisodeCount_IncrementsOnlyOnTransitionAfterCleanRead()
        Using mrb As New Utils.MultiReaderRingBuffer(Cap)
            mrb.CreateReader("r")
            Dim dst(Cap - 1) As Byte

            ' Episode 1: lap, then poll twice more while still lapped
            mrb.Write(Pattern(Cap), 0, Cap)
            mrb.Write(Pattern(Cap), 0, Cap)
            mrb.Available("r") ' detects: episode 1, event 1
            mrb.Write(Pattern(Cap), 0, Cap)
            mrb.Available("r") ' still in episode: event 2, episode count unchanged

            Dim stats1 = mrb.GetReaderStats("r")
            Assert.AreEqual(1, stats1.OverrunEpisodeCount, "continuous overrun is ONE episode")
            Assert.AreEqual(2, stats1.OverrunEvents)

            ' Clean read ends the episode
            mrb.Read("r", dst, 0, Cap)
            mrb.Read("r", dst, 0, Cap) ' clean check (nothing pending)

            ' Episode 2: lap again
            mrb.Write(Pattern(Cap), 0, Cap)
            mrb.Write(Pattern(Cap), 0, Cap)
            mrb.Available("r")

            Dim stats2 = mrb.GetReaderStats("r")
            Assert.AreEqual(2, stats2.OverrunEpisodeCount, "a lap after a clean read is a NEW episode")
        End Using
    End Sub

    <TestMethod>
    Public Sub OverrunLog_RateLimited_OncePerIntervalPerReader()
        ' LastOverrunLogEmitted is the observable seam for FR-008 (analysis E1):
        ' set on the first episode, NOT updated for a second episode within 1 s.
        Using mrb As New Utils.MultiReaderRingBuffer(Cap)
            mrb.CreateReader("r")
            Dim dst(Cap - 1) As Byte

            Assert.AreEqual(DateTime.MinValue, mrb.GetReaderStats("r").LastOverrunLogEmitted, "never logged initially")

            ' Episode 1 -> log fires, timestamp set
            mrb.Write(Pattern(Cap), 0, Cap)
            mrb.Write(Pattern(Cap), 0, Cap)
            mrb.Available("r")
            Dim firstEmit = mrb.GetReaderStats("r").LastOverrunLogEmitted
            Assert.AreNotEqual(DateTime.MinValue, firstEmit, "first episode must emit the log")

            ' Clean read, then episode 2 immediately (well within the 1 s interval)
            mrb.Read("r", dst, 0, Cap)
            mrb.Write(Pattern(Cap), 0, Cap)
            mrb.Write(Pattern(Cap), 0, Cap)
            mrb.Available("r")

            Dim stats = mrb.GetReaderStats("r")
            Assert.AreEqual(2, stats.OverrunEpisodeCount, "second episode must be counted")
            Assert.AreEqual(firstEmit, stats.LastOverrunLogEmitted, "log emission must be rate-limited to 1 s per reader")
        End Using
    End Sub

#End Region

#Region "Stats accounting"

    <TestMethod>
    Public Sub ReaderStats_PendingTracksWriteReadDelta()
        Using mrb As New Utils.MultiReaderRingBuffer(Cap)
            mrb.CreateReader("r")
            mrb.Write(Pattern(40), 0, 40)

            Assert.AreEqual(40L, mrb.GetReaderStats("r").Pending)

            Dim dst(15) As Byte
            mrb.Read("r", dst, 0, 16)
            Assert.AreEqual(24L, mrb.GetReaderStats("r").Pending)
            Assert.AreEqual(0L, mrb.GetReaderStats("r").TotalBytesLost)
        End Using
    End Sub

    <TestMethod>
    Public Sub WraparoundReads_DataIntactAcrossManyCycles()
        Using mrb As New Utils.MultiReaderRingBuffer(32)
            mrb.CreateReader("r")
            Dim dst(22) As Byte
            For cycle = 0 To 99
                Dim src = Pattern(23, seedOffset:=cycle)
                mrb.Write(src, 0, 23)
                Assert.AreEqual(23, mrb.Read("r", dst, 0, 23), $"read failed at cycle {cycle}")
                CollectionAssert.AreEqual(src, dst, $"corruption at cycle {cycle}")
            Next
            Assert.AreEqual(0, mrb.GetReaderStats("r").OverrunEvents, "kept-up reader must never overrun")
        End Using
    End Sub

#End Region

End Class
