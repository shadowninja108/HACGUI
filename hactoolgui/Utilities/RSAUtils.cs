using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Windows;

namespace CertNX
{
    static class RSAUtils
    {
        public static BigInteger GetBigInteger(byte[] In)
        {
            return new BigInteger(In);
        }

        public static BigInteger GetBigIntegerAndReverse(byte[] In)
        {
            return new BigInteger(Reverse(In));
        }

        public static byte[] Reverse(byte[] In)
        {
            byte[] B = new byte[In.Length + 1];
            Buffer.BlockCopy(In, 0, B, 1, In.Length);
            Array.Reverse(B);
            return B;
        }

        public static byte[] GetBytes(BigInteger In, int length = -1)
        {
            byte[] Bytes = In.ToByteArray();
            if (length == -1)
                length = Bytes.Length;
            Array.Resize(ref Bytes, length);
            Array.Reverse(Bytes);
            return Bytes;
        }

        public static BigInteger ModInverse(BigInteger Exp, BigInteger Mod)
        {
            BigInteger N = Mod; BigInteger E = Exp; BigInteger T = 0; BigInteger A = 1;
            while (E != 0)
            {
                BigInteger Q = N / E;
                BigInteger Val;
                Val = T;
                T = A;
                A = Val - Q * A;
                Val = N;
                N = E;
                E = Val - Q * E;
            }
            if (T < 0) T += Mod;
            return T;
        }

        public static void ImportPrivateKey(this X509Certificate2 obj, byte[] key)
        {
            CspParameters csp = new CspParameters
            {
                KeyContainerName = "KeyContainer" // keeps key persistent
            };

            RSACryptoServiceProvider provider = new RSACryptoServiceProvider(csp);
            RSAParameters publicKeyParams = obj.GetRSAPublicKey().ExportParameters(false);
            provider.ImportParameters(publicKeyParams); // have exponent ready
            byte[] publicKey = publicKeyParams.Modulus;
            RSAParameters privateKeyParams = RecoverRSAParameters(GetBigIntegerAndReverse(publicKey), GetBigInteger(publicKeyParams.Exponent), GetBigIntegerAndReverse(key.Take(0x100).ToArray()), publicKey.Length); // actually derive private key params
            provider.ImportParameters(privateKeyParams); // import params to newly created RSACryptoServiceProvider
            obj.PrivateKey = provider; // assign certificate it's new private key
        }

        public static RSAParameters RecoverRSAParameters(BigInteger n, BigInteger e, BigInteger d, int length)
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                BigInteger k = d * e - 1;
                BigInteger two = 2;
                BigInteger one = 1;
                BigInteger r = k / two;
                while (r.IsEven) { one++; r /= two; }
                byte[] rndBuf = n.ToByteArray();
                if (rndBuf[rndBuf.Length - 1] == 0) rndBuf = new byte[rndBuf.Length - 1];
                BigInteger nm1 = n - 1;
                BigInteger y = BigInteger.Zero;
                bool Done = false;
                for (int i = 0; i < 100 && !Done; i++)
                {
                    BigInteger g;
                    do { rng.GetBytes(rndBuf); g = GetBigIntegerAndReverse(rndBuf); }
                    while (g >= n);
                    y = BigInteger.ModPow(g, r, n);
                    if (y.IsOne || y == nm1) { i--; continue; }
                    for (BigInteger j = 1; j < one; j++)
                    {
                        BigInteger x = BigInteger.ModPow(y, two, n);
                        if (x.IsOne) { Done = true; break; }
                        if (x == nm1) break;
                        y = x;
                    }
                }
                BigInteger p = BigInteger.GreatestCommonDivisor(y - 1, n);
                BigInteger q = n / p;
                BigInteger dp = d % (p - 1);
                BigInteger dq = d % (q - 1);
                BigInteger inverseQ = ModInverse(q, p);
                int modLen = rndBuf.Length;
                int halfModLen = (modLen + 1) / 2;
                return new RSAParameters
                {
                    Modulus = GetBytes(n, length),
                    Exponent = GetBytes(e, 3),
                    D = GetBytes(d, length),
                    P = GetBytes(p, length/2),
                    Q = GetBytes(q, length/2),
                    DP = GetBytes(dp, length/2),
                    DQ = GetBytes(dq, length/2),
                    InverseQ = GetBytes(inverseQ, length/2)
                };
            }
        }
    }
}