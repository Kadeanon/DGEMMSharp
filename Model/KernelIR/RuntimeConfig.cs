using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR;

public class RuntimeConfig
{
    public RuntimeConfig(VectorType vectorLevel)
    {
        VectorLevel = vectorLevel;

        switch (vectorLevel)
        {
            case VectorType.Vector512:
                StaticSIMDType = typeof(Vector512);
                SIMDType = typeof(Vector512<double>);
                break;
            case VectorType.Vector256:
                StaticSIMDType = typeof(Vector256);
                SIMDType = typeof(Vector256<double>);
                break;
            case VectorType.Vector128:
                StaticSIMDType = typeof(Vector128);
                SIMDType = typeof(Vector128<double>);
                break;
            default:
                throw new NotSupportedException("Not supported yet!");
        }

        var methods = StaticSIMDType.GetMethods(BindingFlags.Public | BindingFlags.Static);
        LoadVector = methods
            .Where(x => x.Name == "LoadUnsafe" &&
                x.ContainsGenericParameters)
            .Select(x => x.MakeGenericMethod(typeof(double)))
            .FirstOrDefault(x => x.GetParameters() is [var refParamInfo] &&
                    refParamInfo.ParameterType == typeof(double).MakeByRefType())!;
        LoadVectorWithOffset = methods
            .Where(x => x.Name == "LoadUnsafe" &&
                x.ContainsGenericParameters)
            .Select(x => x.MakeGenericMethod(typeof(double)))
            .FirstOrDefault(x => x.GetParameters() is [var refParamInfo, var _] &&
                    refParamInfo.ParameterType == typeof(double).MakeByRefType())!;
        StoreVector = methods
            .FirstOrDefault(x => x.Name == "StoreUnsafe" &&
                x.GetParameters().Length == 2)!
            .MakeGenericMethod(typeof(double));
        BroadcastVector = methods
            .Where(x => x.Name == "Create" &&
                x.ContainsGenericParameters)
            .Select(x => x.MakeGenericMethod(typeof(double)))
            .FirstOrDefault(x => x.GetParameters() is [var spanParamInfo] &&
                    spanParamInfo.ParameterType == typeof(double))!;

        MultiAdd = typeof(System.Runtime.Intrinsics.X86.Fma).
            GetMethod("MultiplyAdd",
            BindingFlags.Public | BindingFlags.Static,
            [SIMDType, SIMDType, SIMDType]) ?? 
            throw new NotSupportedException("Not supported yet!");
    }

    public VectorType VectorLevel { get; }

    public Type StaticSIMDType { get; }

    public Type SIMDType { get; }

    public MethodInfo LoadVector { get; }

    public MethodInfo LoadVectorWithOffset { get; }

    public MethodInfo StoreVector { get; }

    public MethodInfo BroadcastVector { get; }

    public MethodInfo MultiAdd { get; }
}
