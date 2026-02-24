using System;
using System.Runtime.InteropServices;

namespace KeyVaultExplorer;

public static class StringExtensions
{
    public static byte[] ToByteArray(this string s) => s.ToByteSpan().ToArray(); 

    public static ReadOnlySpan<byte> ToByteSpan(this string s) => MemoryMarshal.Cast<char, byte>(s);
}