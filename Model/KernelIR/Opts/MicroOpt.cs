using DGEMMSharp.Model.KernelIR.Nodes.ComputeNodes;
using DGEMMSharp.Model.KernelIR.Opts.CFG;
using DGEMMSharp.Model.KernelIR.Values;
using NetFabric;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Opts
{
    public class MicroOpt(KernelDef kernel)
    {
        public KernelDef Def { get; } = kernel;

        public RuntimeConfig Config => Def.Config;

        public void Invoke()
        {
            Invoke(Def.CFG!.Root);
            foreach(var bb in Def.CFG.Graph.Values)
            {
                Invoke(bb);
            }
        }

        public void Invoke(BasicBlock bb)
        {
            List<MicroOptEntry> entries = new();
            Queue<MicroOptEntry> queue = new();
            Dictionary<ISrc, MicroOptEntry> lastDef = new();

            foreach (var(index, item) in bb.Nodes.Forward.Index())
            {
                var entry = new MicroOptEntry(item, index);
                foreach(var used in item.GetUsings())
                {
                    if(lastDef.TryGetValue(used, out var def))
                    {
                        entry.Dependent(def);
                    }
                }
                lastDef[item.DestValue] = entry;
                entries.Add(entry);
            }

            List<MicroOptEntry> workset = new(entries.Where(entry =>
                entry.Prevs.Count == 0));
            lastDef.Clear();

            if (workset.Count > 0)
            {
                var head = SelectFirst();
                UpdateSelected(head);
                while (workset.Count > 0)
                {
                    head = Select();
                    UpdateSelected(head);
                }

                MicroOptEntry SelectFirst()
                {
                    var val = workset[0];
                    workset.Remove(val);
                    return val;
                }

                MicroOptEntry Select()
                {
                    var prevIsFMA = head.Node is MulAdd;
                    var candidates = workset.Where(entry =>
                    entry.Node is MulAdd != prevIsFMA).ToList();
                    if(candidates.Count == 0) candidates = workset;
                    var index = candidates.FindIndex(entry =>
                        !entry.Prevs.Contains(head));
                    if (index == -1) index = 0;
                    var val = candidates[index];
                    workset.Remove(val);
                    return val;
                }

                void UpdateSelected(MicroOptEntry entry)
                {
                    queue.Enqueue(entry);
                    workset.AddRange(entry.UpdateSchedule());
                }
            }

            bb.Nodes.Clear();
            foreach(MicroOptEntry entry in queue)
            {
                bb.Nodes.AddLast(entry.Node);
            }
        }
    }

    public class MicroOptEntry
    {
        public ComputeBase Node {  get; }

        public int Index { get; }   

        public HashSet<MicroOptEntry> Prevs { get; }

        public HashSet<MicroOptEntry> Succs { get; }

        public int Deps { get; private set; }

        public bool Scheduled { get; private set; }

        public MicroOptEntry(ComputeBase node, int index)
        {
            Node = node;
            Index = index;
            Prevs = new HashSet<MicroOptEntry>();
            Succs = new HashSet<MicroOptEntry>();
            Deps = 0;
            Scheduled = false;
        }

        public void Dependent(MicroOptEntry prev)
        {
            Prevs.Add(prev);
            prev.Succs.Add(this);
            Deps++;
        }

        public IEnumerable<MicroOptEntry> UpdateSchedule()
        {
            Scheduled = true;
            foreach (MicroOptEntry entry in Succs)
            {
                if(--entry.Deps == 0)
                    yield return entry;
            }
        }

        public override string ToString()
        {
            return $"[NumDep: {Deps}] {Node}";
        }
    }
}
