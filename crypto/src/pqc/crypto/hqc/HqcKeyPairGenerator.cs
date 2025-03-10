﻿using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;

namespace Org.BouncyCastle.Pqc.Crypto.Hqc

{
    public class HqcKeyPairGenerator : IAsymmetricCipherKeyPairGenerator
    {
        private int n;

        private int k;

        private int delta;

        private int w;

        private int wr;

        private int we;
        private int N_BYTE;
        private HqcKeyGenerationParameters hqcKeyGenerationParameters;

        private SecureRandom random;

        public AsymmetricCipherKeyPair GenerateKeyPair()
        {
            byte[] seed = new byte[48];
            random.NextBytes(seed);
            return GenKeyPair(seed);
        }

        public AsymmetricCipherKeyPair GenerateKeyPairWithSeed(byte[] seed)
        {
            return GenKeyPair(seed);
        }

        public void Init(KeyGenerationParameters parameters)
        {
            this.hqcKeyGenerationParameters = (HqcKeyGenerationParameters)parameters;
            this.random = parameters.Random;

            // get parameters
            this.n = this.hqcKeyGenerationParameters.Parameters.N;
            this.k = this.hqcKeyGenerationParameters.Parameters.K;
            this.delta = this.hqcKeyGenerationParameters.Parameters.Delta;
            this.w = this.hqcKeyGenerationParameters.Parameters.W;
            this.wr = this.hqcKeyGenerationParameters.Parameters.Wr;
            this.we = this.hqcKeyGenerationParameters.Parameters.We;
            this.N_BYTE = (n + 7) / 8;
        }
        private AsymmetricCipherKeyPair GenKeyPair(byte[] seed)
        {
            HqcEngine engine = hqcKeyGenerationParameters.Parameters.Engine;
            byte[] pk = new byte[40 + N_BYTE];
            byte[] sk = new byte[40 + 40 + N_BYTE];

            engine.GenKeyPair(pk, sk, seed);

            // form keys
            HqcPublicKeyParameters publicKey = new HqcPublicKeyParameters(hqcKeyGenerationParameters.Parameters, pk);
            HqcPrivateKeyParameters privateKey = new HqcPrivateKeyParameters(hqcKeyGenerationParameters.Parameters, sk);

            return new AsymmetricCipherKeyPair(publicKey, privateKey);
        }

       

    }
}
