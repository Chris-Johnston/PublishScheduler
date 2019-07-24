using System;
using System.Security.Cryptography;

namespace PublishScheduler
{
    public class RSAKeyValue
    {
        public string Modulus { get; set; }
        public string Exponent { get; set; }
        public string P { get; set; }
        public string Q { get; set; }
        public string DP { get; set; }
        public string DQ { get; set; }
        public string InverseQ { get; set; }
        public string D { get; set; }

        // makes a RSAParameters from a RSAKeyValue. RSAKeyValue is used for serialization
        // and each of the properties are Base64 strings.
        public RSAParameters ToRSAParameters()
            => new RSAParameters()
            {
                Modulus = Convert.FromBase64String(this.Modulus),
                Exponent = Convert.FromBase64String(this.Exponent),
                P = Convert.FromBase64String(this.P),
                Q = Convert.FromBase64String(this.Q),
                DQ = Convert.FromBase64String(this.DQ),
                DP = Convert.FromBase64String(this.DP),
                InverseQ = Convert.FromBase64String(this.InverseQ),
                D = Convert.FromBase64String(this.D)
            };
    }
}