using System.Configuration;

namespace Channnel
{
    public class ClientConfig
    {
        public bool ReadOnly { get; set; }
        public bool WriteOnly { get; set; }

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