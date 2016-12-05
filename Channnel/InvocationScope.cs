using System.Configuration;
using System.Diagnostics;

namespace Channnel
{
    public class InvocationScope
    {
        public string ThreadId { get; set; }
        public string InvocationScopeName { get; private set; }
        public bool ReadOnly { get; set; }
        public bool WriteOnly { get; set; }

        public InvocationScope()
        {
            //TODO : need to find a better way to handle this
            InvocationScopeName = GetInvocationScopeMethodName(2);
        }

        public bool ValidateSettings()
        {
            if (WriteOnly && ReadOnly)
            {
                throw new ConfigurationErrorsException("Channel cannot be both readonly and write only.");
            }

            return true;
        }

        private static string GetInvocationScopeMethodName(int depth)
        {
            return new StackFrame(depth).GetMethod().Name;
        }

    }
}