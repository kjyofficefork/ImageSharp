// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static SixLabors.ImageSharp.SimdUtils;

namespace SixLabors.ImageSharp;

/// <inheritdoc/>
internal interface IShuffle4Slice3 : IComponentShuffle
{
}

internal readonly struct DefaultShuffle4Slice3 : IShuffle4Slice3
{
    public DefaultShuffle4Slice3(byte control)
    {
        DebugGuard.MustBeBetweenOrEqualTo<byte>(control, 0, 3, nameof(control));
        this.Control = control;
    }

    public byte Control { get; }

    [MethodImpl(InliningOptions.ShortMethod)]
    public void ShuffleReduce(ref ReadOnlySpan<byte> source, ref Span<byte> dest)
        => HwIntrinsics.Shuffle4Slice3Reduce(ref source, ref dest, this.Control);

    [MethodImpl(InliningOptions.ShortMethod)]
    public void RunFallbackShuffle(ReadOnlySpan<byte> source, Span<byte> dest)
    {
        ref byte sBase = ref MemoryMarshal.GetReference(source);
        ref byte dBase = ref MemoryMarshal.GetReference(dest);

        Shuffle.InverseMMShuffle(this.Control, out _, out int p2, out int p1, out int p0);

        for (int i = 0, j = 0; i < dest.Length; i += 3, j += 4)
        {
            Unsafe.Add(ref dBase, i) = Unsafe.Add(ref sBase, p0 + j);
            Unsafe.Add(ref dBase, i + 1) = Unsafe.Add(ref sBase, p1 + j);
            Unsafe.Add(ref dBase, i + 2) = Unsafe.Add(ref sBase, p2 + j);
        }
    }
}

internal readonly struct XYZWShuffle4Slice3 : IShuffle4Slice3
{
    [MethodImpl(InliningOptions.ShortMethod)]
    public void ShuffleReduce(ref ReadOnlySpan<byte> source, ref Span<byte> dest)
        => HwIntrinsics.Shuffle4Slice3Reduce(ref source, ref dest, Shuffle.MMShuffle3210);

    [MethodImpl(InliningOptions.ShortMethod)]
    public void RunFallbackShuffle(ReadOnlySpan<byte> source, Span<byte> dest)
    {
        ref uint sBase = ref Unsafe.As<byte, uint>(ref MemoryMarshal.GetReference(source));
        ref Byte3 dBase = ref Unsafe.As<byte, Byte3>(ref MemoryMarshal.GetReference(dest));

        int n = source.Length / 4;
        int m = Numerics.Modulo4(n);
        int u = n - m;

        ref uint sLoopEnd = ref Unsafe.Add(ref sBase, u);
        ref uint sEnd = ref Unsafe.Add(ref sBase, n);

        while (Unsafe.IsAddressLessThan(ref sBase, ref sLoopEnd))
        {
            Unsafe.Add(ref dBase, 0) = Unsafe.As<uint, Byte3>(ref Unsafe.Add(ref sBase, 0));
            Unsafe.Add(ref dBase, 1) = Unsafe.As<uint, Byte3>(ref Unsafe.Add(ref sBase, 1));
            Unsafe.Add(ref dBase, 2) = Unsafe.As<uint, Byte3>(ref Unsafe.Add(ref sBase, 2));
            Unsafe.Add(ref dBase, 3) = Unsafe.As<uint, Byte3>(ref Unsafe.Add(ref sBase, 3));

            sBase = ref Unsafe.Add(ref sBase, 4);
            dBase = ref Unsafe.Add(ref dBase, 4);
        }

        while (Unsafe.IsAddressLessThan(ref sBase, ref sEnd))
        {
            Unsafe.Add(ref dBase, 0) = Unsafe.As<uint, Byte3>(ref Unsafe.Add(ref sBase, 0));

            sBase = ref Unsafe.Add(ref sBase, 1);
            dBase = ref Unsafe.Add(ref dBase, 1);
        }
    }
}

[StructLayout(LayoutKind.Explicit, Size = 3)]
internal readonly struct Byte3
{
}
