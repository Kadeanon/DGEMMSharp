using Sigil;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace DGEMMSharp.Model;

internal static class ILUtils
{
    internal static Type IntType { get; } = typeof(int);

    internal static Type DoubleType { get; } = typeof(double);

    /// <summary>
    /// ref double result = ref Unsafe.Add(ref a, offset);
    /// </summary>
    internal static MethodInfo AddRef { get; }
        = typeof(Unsafe).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(
            x => x.Name == "Add" &&
            x.ContainsGenericParameters &&
            x.GetParameters() is [_, var intParamInfo] && 
            intParamInfo.ParameterType == IntType)!;

    /// <summary>
    /// ref double result = ref Unsafe.Add(ref a, offset);
    /// </summary>
    internal static MethodInfo AddRefDouble { get; }
        = typeof(Unsafe).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(
            x => x.Name == "Add" &&
            x.ContainsGenericParameters &&
            x.GetParameters() is [_, var intParamInfo] && intParamInfo.ParameterType == IntType)
            !.MakeGenericMethod(typeof(double));

    /// <summary>
    /// Span<double> span = MemoryMarshal.CreateSpan(ref a, length);
    /// </summary>
    internal static MethodInfo CreateSpan { get; }
        = typeof(MemoryMarshal).GetMethod("CreateSpan", BindingFlags.Public | BindingFlags.Static)!
        .MakeGenericMethod(DoubleType);

    /// <summary>
    /// public static operator implicit ReadOnlySpan<double>(Span<double> span)
    /// </summary>
    internal static MethodInfo ConvSpanAsReadOnly { get; }
        = typeof(Span<double>).GetMethods(BindingFlags.Public | BindingFlags.Static).
        FirstOrDefault
        (x => x.Name == "op_Implicit" &&
        x.GetParameters() is [var spanParamInfo] &&
        spanParamInfo.ParameterType == typeof(Span<double>))!;

    /// <summary>
    /// VectorX{double} vec =  VectorX.Create(span);
    /// </summary>
    internal static MethodInfo DynamicCreateVectorFromSpan(Type staticSimdType)
        => staticSimdType.GetMethods(BindingFlags.Public | BindingFlags.Static)
        .FirstOrDefault(
        x => x.Name == "Create" &&
        x.ContainsGenericParameters &&
        x.MakeGenericMethod(DoubleType).GetParameters() is [var spanParamInfo]
        && spanParamInfo.ParameterType == typeof(ReadOnlySpan<double>))
        !.MakeGenericMethod(DoubleType);

    /// <summary>
    /// VectorX{double} vec =  VectorX.LoadUnsafe(ref xRef);
    /// </summary>
    internal static MethodInfo DynamicLoadVectorUnsafe(Type staticSimdType)
        => staticSimdType.GetMethods(BindingFlags.Public | BindingFlags.Static)
        .FirstOrDefault(
        x => x.Name == "LoadUnsafe" &&
        x.ContainsGenericParameters &&
        x.MakeGenericMethod(DoubleType).GetParameters() is [var refParamInfo]
        && refParamInfo.ParameterType == DoubleType.MakeByRefType())
        !.MakeGenericMethod(DoubleType);

    /// <summary>
    /// VectorX{double} vec =  VectorX.LoadUnsafe(ref xRef);
    /// </summary>
    internal static MethodInfo DynamicStoreVectorUnsafe(Type staticSimdType)
    {
        var methods = staticSimdType.GetMethods(BindingFlags.Public | BindingFlags.Static);
        return methods.FirstOrDefault(x => x.Name == "StoreUnsafe" && 
        x.GetParameters().Length == 2)!.MakeGenericMethod(DoubleType);
    }

    /// <summary>
    /// VectorX{double} vec =  VectorX.LoadUnsafe(ref xRef, offset);
    /// </summary>
    internal static MethodInfo DynamicLoadVectorUnsafeWithOffset(Type staticSimdType)
        => staticSimdType.GetMethods(BindingFlags.Public | BindingFlags.Static)
        .FirstOrDefault(
        x => x.Name == "LoadUnsafe" &&
        x.ContainsGenericParameters &&
        x.MakeGenericMethod(DoubleType).GetParameters() is [var refParamInfo, var _]
        && refParamInfo.ParameterType == DoubleType.MakeByRefType())
        !.MakeGenericMethod(DoubleType);

    /// <summary>
    /// VectorX{double} vec =  VectorX.Create(a);
    /// </summary>
    internal static MethodInfo DynamicCreateVectorFromScalar(Type staticSimdType)
        => staticSimdType.GetMethods(BindingFlags.Public | BindingFlags.Static)
        .FirstOrDefault(
        x => x.Name == "Create" &&
        x.ContainsGenericParameters &&
        x.MakeGenericMethod(DoubleType).GetParameters() is [var spanParamInfo]
        && spanParamInfo.ParameterType == typeof(double))
        !.MakeGenericMethod(DoubleType);

    /// <summary>
    /// vec.CopyTo(span);
    /// </summary>
    internal static MethodInfo DynamicCopyTo(Type staticSimdType)
        => staticSimdType.GetMethods(BindingFlags.Public | BindingFlags.Static).
            FirstOrDefault
            (x => x.Name == "CopyTo" &&
             x.ContainsGenericParameters &&
            x.MakeGenericMethod(DoubleType).GetParameters() is [_, var spanParamInfo] &&
            spanParamInfo.ParameterType == typeof(Span<double>))!.MakeGenericMethod(DoubleType);

    /// <summary>
    /// vec0 = vec0 * vec1;
    /// </summary>
    internal static MethodInfo DynamicVectorOpMul(Type simdType) =>
        simdType.GetMethods(BindingFlags.Public | BindingFlags.Static)
        .FirstOrDefault(
        x => x.Name == "op_Multiply" &&
        x.GetParameters() is [var leftParamInfo, var rightParamInfo] &&
        leftParamInfo.ParameterType == simdType &&
        rightParamInfo.ParameterType == simdType)!;

    /// <summary>
    /// vec2 = vec0 + vec1;
    /// </summary>
    internal static MethodInfo DynamicVectorOpAdd(Type simdType) =>
        simdType.GetMethods(BindingFlags.Public | BindingFlags.Static)
        .FirstOrDefault(
        x => x.Name == "op_Addition" &&
        x.GetParameters() is [var leftParamInfo, var rightParamInfo] &&
        leftParamInfo.ParameterType == simdType &&
        rightParamInfo.ParameterType == simdType)!;

}
