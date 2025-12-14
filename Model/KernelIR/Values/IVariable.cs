using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Values;

public interface ISrc: IValue
{
    public void LoadValue();
}

public interface IDest: ISrc
{
    public void StoreValue();
}

public interface IScalar: ISrc
{
}

public interface IScalar<T>: IScalar
{
}

public interface IVariable : IScalar, IDest
{
    public void LoadAddr();
}

public interface IVariable<T> : IVariable, IScalar<T>
{
}
