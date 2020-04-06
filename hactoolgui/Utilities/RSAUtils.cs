using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.X509;
using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;

namespace CertNX
{
    static class RSAUtils
    {
        public static Org.BouncyCastle.Math.BigInteger ToBouncyCastleBigInteger(byte[] b)
        {
            return new Org.BouncyCastle.Math.BigInteger(b);
        }

        public static Org.BouncyCastle.Math.BigInteger ToBouncyCastleBigInteger(BigInteger i)
        {
            return ToBouncyCastleBigInteger(i.ToByteArray());
        }

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

        public static byte[] GetBytes(BigInteger Input, int Len = -1)
        {
            byte[] Bytes = Input.ToByteArray();
            Len = Bytes.Length;
            Array.Resize(ref Bytes, Len);
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

        public static AsymmetricKeyParameter RecoverPrivateParameter(this X509Certificate obj, byte[] privModulus)
        {
            RsaKeyParameters publicKeyParams = obj.GetPublicKey() as RsaKeyParameters;
            byte[] publicKey = publicKeyParams.Modulus.ToByteArray();
            return RecoverRSAParameters(
                GetBigIntegerAndReverse(publicKey),
                GetBigIntegerAndReverse(publicKeyParams.Exponent.ToByteArray()),
                GetBigIntegerAndReverse(privModulus.Take(0x100).ToArray())
            );
        }

        public static AsymmetricKeyParameter RecoverRSAParameters(BigInteger n, BigInteger e, BigInteger d)
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {

                BigInteger k = d * e - 1;
                BigInteger two = 2;
                BigInteger t = 1;
                BigInteger r = k / two;

                while (r.IsEven)
                {
                    t++;
                    r /= two;
                }

                byte[] Buf = n.ToByteArray();

                if (Buf[Buf.Length - 1] == 0)
                {
                    Buf = new byte[Buf.Length - 1];
                }

                BigInteger nMinusOne = n - 1;

                bool Done = false;
                BigInteger y = BigInteger.Zero;

                for (int i = 0; i < 100 && !Done; i++)
                {
                    BigInteger g;

                    do
                    {
                        rng.GetBytes(Buf);
                        g = GetBigInteger(Buf);
                    }
                    while (g >= n);

                    y = BigInteger.ModPow(g, r, n);

                    if (y.IsOne || y == nMinusOne)
                    {
                        i--;
                        continue;
                    }

                    for (BigInteger j = 1; j < t; j++)
                    {
                        BigInteger x = BigInteger.ModPow(y, two, n);

                        if (x.IsOne)
                        {
                            Done = true;
                            break;
                        }

                        if (x == nMinusOne)
                        {
                            break;
                        }

                        y = x;
                    }
                }

                BigInteger p = BigInteger.GreatestCommonDivisor(y - 1, n);
                BigInteger q = n / p;
                BigInteger dp = d % (p - 1);
                BigInteger dq = d % (q - 1);
                BigInteger inverseQ = ModInverse(q, p);

                int modLen = Buf.Length;
                int halfModLen = (modLen + 1) / 2;

                byte[] Modulus = GetBytes(n, modLen);
                byte[] Exponent = GetBytes(e);
                byte[] D = GetBytes(d, modLen);
                byte[] P = GetBytes(p, halfModLen);
                byte[] Q = GetBytes(q, halfModLen);
                byte[] DP = GetBytes(dp, halfModLen);
                byte[] DQ = GetBytes(dq, halfModLen);
                byte[] InverseQ = GetBytes(inverseQ, halfModLen);

                return new RsaPrivateCrtKeyParameters(
                    ToBouncyCastleBigInteger(Modulus),
                    ToBouncyCastleBigInteger(Exponent), 
                    ToBouncyCastleBigInteger(D),
                    ToBouncyCastleBigInteger(P),
                    ToBouncyCastleBigInteger(Q),
                    ToBouncyCastleBigInteger(DP),
                    ToBouncyCastleBigInteger(DQ),
                    ToBouncyCastleBigInteger(InverseQ)
                );
            }
        }
    }
}