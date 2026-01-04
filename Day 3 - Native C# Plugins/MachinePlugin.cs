using Microsoft.SemanticKernel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Day_3___Native_C__Plugins
{
    internal class MachinePlugin
    {
        [KernelFunction]
        public string GetMachineStatus(string machine)
        {
            return $"Machine {machine} is STOPPED";
        }
    }
}
