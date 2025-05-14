using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model
{
    internal static class ILUtils
    {


        internal static void EmitLoadInt(this ILGenerator il, int value)
        {
            switch (value)
            {
                case -1:
                    il.Emit(OpCodes.Ldc_I4_M1);
                    break;
                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    break;
                case int shortValue when shortValue >= -128 && shortValue <= 127:
                    il.Emit(OpCodes.Ldc_I4_S, (byte)shortValue);
                    break;
                default:
                    il.Emit(OpCodes.Ldc_I4, value);
                    break;
            }
        }

        internal static void EmitStoreLocal(this ILGenerator il, LocalBuilder local)
        {
            int index = local.LocalIndex;
            switch (index)
            {
                case 0:
                    il.Emit(OpCodes.Stloc_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Stloc_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Stloc_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Stloc_3);
                    break;
                case int shortValue when shortValue <= 255:
                    il.Emit(OpCodes.Stloc_S, (byte)shortValue);
                    break;
                default:
                    il.Emit(OpCodes.Stloc, index);
                    break;
            }
        }

        internal static void EmitLoadLocal(this ILGenerator il, LocalBuilder local)
        {
            int index = local.LocalIndex;
            switch (index)
            {
                case 0:
                    il.Emit(OpCodes.Ldloc_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldloc_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldloc_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldloc_3);
                    break;
                case int shortValue when shortValue <= 255:
                    il.Emit(OpCodes.Ldloc_S, (byte)shortValue);
                    break;
                default:
                    il.Emit(OpCodes.Ldloc, index);
                    break;
            }
        }

        internal static Type IntType { get; } = typeof(int);

        internal static Type DoubleType { get; } = typeof(double);

        /// <summary>
        /// ref double result = ref Unsafe.Add(ref a, offset);
        /// </summary>
        internal static MethodInfo AddDouble { get; }
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

        /// <summary>
        /// local = ref Unsafe.Add(ref local, offset);
        /// </summary>
        internal static void EmitAddRef(this ILGenerator il, LocalBuilder local, int offset)
        => il.EmitAddRef(local, offset, local);

        /// <summary>
        /// dest = ref Unsafe.Add(ref src, offset);
        /// </summary>
        internal static void EmitAddRef(this ILGenerator il, LocalBuilder src, int offset, LocalBuilder dest)
        {
            il.EmitLoadLocal(src);
            il.EmitLoadInt(offset);
            il.EmitCall(OpCodes.Call, AddDouble, null);
            il.EmitStoreLocal(dest);
        }

        /// <summary>
        /// c = Fma.MultiplyAdd(a, b, c); <=> c += a * b;
        /// </summary>
        /// <param name="il"></param>
        /// <param name="simdType"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        internal static void EmitFMA(this ILGenerator il, Type simdType, LocalBuilder a, LocalBuilder b, LocalBuilder c)
        {
            var fma = typeof(Fma).
                GetMethods(BindingFlags.Public | BindingFlags.Static).
                First(x =>
                x.Name == "MultiplyAdd" &&
                x.ReturnType == simdType);
            var opMul = DynamicVectorOpMul(simdType);
            var opAdd = DynamicVectorOpAdd(simdType);
            il.EmitLoadLocal(a);
            il.EmitLoadLocal(b);
            il.EmitLoadLocal(c);
            il.EmitCall(OpCodes.Call, fma, null);
            il.EmitStoreLocal(c);
        }
    }
}
