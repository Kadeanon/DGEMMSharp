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

        LoadVector =
            ILUtils.DynamicLoadVectorUnsafe(StaticSIMDType);
        LoadVectorWithOffset =
            ILUtils.DynamicLoadVectorUnsafeWithOffset(StaticSIMDType);
        StoreVector = 
            ILUtils.DynamicStoreVectorUnsafe(StaticSIMDType);
        BroadcastVector =
            ILUtils.DynamicCreateVectorFromScalar(StaticSIMDType);

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
