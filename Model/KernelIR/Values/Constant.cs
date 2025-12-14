using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Values;

public class Constant<T>(KernelDef kernel, T value)
    : Value(kernel, value?.ToString()??"unknown"), IScalar<T>
{
    public T Value { get; } = value;

    public void LoadValue()
    {
        if (typeof(T) == typeof(int))
            Emitter.LoadConstant((int)(object)Value);
        else if (typeof(T) == typeof(bool))
            Emitter.LoadConstant((int)(object)Value);
        else if (typeof(T) == typeof(float))
            Emitter.LoadConstant((float)(object)Value);
        else if (typeof(T) == typeof(double))
            Emitter.LoadConstant((double)(object)Value);
        else if (typeof(T) == typeof(string))
            Emitter.LoadConstant((string)(object)Value);
        else
            throw new NotSupportedException();
    }
}
