using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Values;

public interface IScalar
{
    public void LoadValue();
}

public interface IVariable: IScalar
{
    public void LoadAddr();

    public void StoreValue();
}
