using System.Diagnostics;
using System.Reflection;

namespace Channel
{
    public static class Helpers
    {
        public static string GetInvocationScopeMethodName(int depth)
        {
            var methodBase = new StackFrame(depth).GetMethod();

            while (methodBase.IsConstructor || methodBase.MemberType == MemberTypes.Constructor)
            {
                methodBase = new StackFrame(depth++).GetMethod();
            }

            return methodBase.Name;
        }
    }
}
