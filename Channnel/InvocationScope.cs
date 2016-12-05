using System.Configuration;
using Channel;

namespace Channnel
{
    /// <summary>
    /// Invocation scope name captures the point from where it is instantiated.
    /// Always instantiate this from nearest use.
    /// </summary>
    public class InvocationScope
    {
        public string ThreadId { get; set; }
        public string InvocationScopeName { get; private set; }
        public bool ReadOnly { get; set; }
        public bool WriteOnly { get; set; }

        public InvocationScope()
        {
            //TODO : need to find a better way to handle this
            InvocationScopeName = Helpers.GetInvocationScopeMethodName(2);
        }

        public bool ValidateSettings()
        {
            if (WriteOnly && ReadOnly)
            {
                throw new ConfigurationErrorsException("Channel cannot be both readonly and write only.");
            }

            return true;
        }

    }
}