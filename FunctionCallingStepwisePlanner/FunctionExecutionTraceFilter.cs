using Microsoft.SemanticKernel;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionCallingStepwisePlanner
{
    public sealed class FunctionExecutionTraceFilter : IFunctionInvocationFilter
    {
        public async Task OnFunctionInvocationAsync(
            FunctionInvocationContext context,
            Func<FunctionInvocationContext, Task> next)
        {
            // Before
            Console.WriteLine($"⚡ Calling: {context.Function.Name}");
            foreach (var arg in context.Arguments)
            {
                Console.WriteLine($"   Arg: {arg.Key} = {arg.Value}");
            }

            var stopwatch = Stopwatch.StartNew();

            await next(context); // function executes here

            stopwatch.Stop();

            // After
            Console.WriteLine($"✅ Completed: {context.Function.Name} ({stopwatch.ElapsedMilliseconds}ms)");
            Console.WriteLine($"   Result: {context.Result}");
        }
    }
}
