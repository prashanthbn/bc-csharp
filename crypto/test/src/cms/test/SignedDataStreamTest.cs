using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using NUnit.Framework;

using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.Utilities.IO;
using Org.BouncyCastle.X509;

namespace Org.BouncyCastle.Cms.Tests
{
	[TestFixture]
	public class SignedDataStreamTest
	{
		private const string TestMessage = "Hello World!";
		private const string SignDN = "O=Bouncy Castle, C=AU";
		private static AsymmetricCipherKeyPair signKP;
		private static X509Certificate signCert;

		private const string OrigDN = "CN=Bob, OU=Sales, O=Bouncy Castle, C=AU";
		private static AsymmetricCipherKeyPair origKP;
		private static X509Certificate origCert;

		private const string ReciDN = "CN=Doug, OU=Sales, O=Bouncy Castle, C=AU";
//		private static AsymmetricCipherKeyPair reciKP;
//		private static X509Certificate reciCert;

		private static AsymmetricCipherKeyPair origDsaKP;
		private static X509Certificate origDsaCert;

		private static X509Crl signCrl;
		private static X509Crl origCrl;

		private static readonly byte[] OcspResponseBytes = Base64.Decode(
			"MIIFnAoBAKCCBZUwggWRBgkrBgEFBQcwAQEEggWCMIIFfjCCARehgZ8wgZwx"
			+ "CzAJBgNVBAYTAklOMRcwFQYDVQQIEw5BbmRocmEgcHJhZGVzaDESMBAGA1UE"
			+ "BxMJSHlkZXJhYmFkMQwwCgYDVQQKEwNUQ1MxDDAKBgNVBAsTA0FUQzEeMBwG"
			+ "A1UEAxMVVENTLUNBIE9DU1AgUmVzcG9uZGVyMSQwIgYJKoZIhvcNAQkBFhVv"
			+ "Y3NwQHRjcy1jYS50Y3MuY28uaW4YDzIwMDMwNDAyMTIzNDU4WjBiMGAwOjAJ"
			+ "BgUrDgMCGgUABBRs07IuoCWNmcEl1oHwIak1BPnX8QQUtGyl/iL9WJ1VxjxF"
			+ "j0hAwJ/s1AcCAQKhERgPMjAwMjA4MjkwNzA5MjZaGA8yMDAzMDQwMjEyMzQ1"
			+ "OFowDQYJKoZIhvcNAQEFBQADgYEAfbN0TCRFKdhsmvOdUoiJ+qvygGBzDxD/"
			+ "VWhXYA+16AphHLIWNABR3CgHB3zWtdy2j7DJmQ/R7qKj7dUhWLSqclAiPgFt"
			+ "QQ1YvSJAYfEIdyHkxv4NP0LSogxrumANcDyC9yt/W9yHjD2ICPBIqCsZLuLk"
			+ "OHYi5DlwWe9Zm9VFwCGgggPMMIIDyDCCA8QwggKsoAMCAQICAQYwDQYJKoZI"
			+ "hvcNAQEFBQAwgZQxFDASBgNVBAMTC1RDUy1DQSBPQ1NQMSYwJAYJKoZIhvcN"
			+ "AQkBFhd0Y3MtY2FAdGNzLWNhLnRjcy5jby5pbjEMMAoGA1UEChMDVENTMQww"
			+ "CgYDVQQLEwNBVEMxEjAQBgNVBAcTCUh5ZGVyYWJhZDEXMBUGA1UECBMOQW5k"
			+ "aHJhIHByYWRlc2gxCzAJBgNVBAYTAklOMB4XDTAyMDgyOTA3MTE0M1oXDTAz"
			+ "MDgyOTA3MTE0M1owgZwxCzAJBgNVBAYTAklOMRcwFQYDVQQIEw5BbmRocmEg"
			+ "cHJhZGVzaDESMBAGA1UEBxMJSHlkZXJhYmFkMQwwCgYDVQQKEwNUQ1MxDDAK"
			+ "BgNVBAsTA0FUQzEeMBwGA1UEAxMVVENTLUNBIE9DU1AgUmVzcG9uZGVyMSQw"
			+ "IgYJKoZIhvcNAQkBFhVvY3NwQHRjcy1jYS50Y3MuY28uaW4wgZ8wDQYJKoZI"
			+ "hvcNAQEBBQADgY0AMIGJAoGBAM+XWW4caMRv46D7L6Bv8iwtKgmQu0SAybmF"
			+ "RJiz12qXzdvTLt8C75OdgmUomxp0+gW/4XlTPUqOMQWv463aZRv9Ust4f8MH"
			+ "EJh4ekP/NS9+d8vEO3P40ntQkmSMcFmtA9E1koUtQ3MSJlcs441JjbgUaVnm"
			+ "jDmmniQnZY4bU3tVAgMBAAGjgZowgZcwDAYDVR0TAQH/BAIwADALBgNVHQ8E"
			+ "BAMCB4AwEwYDVR0lBAwwCgYIKwYBBQUHAwkwNgYIKwYBBQUHAQEEKjAoMCYG"
			+ "CCsGAQUFBzABhhpodHRwOi8vMTcyLjE5LjQwLjExMDo3NzAwLzAtBgNVHR8E"
			+ "JjAkMCKgIKAehhxodHRwOi8vMTcyLjE5LjQwLjExMC9jcmwuY3JsMA0GCSqG"
			+ "SIb3DQEBBQUAA4IBAQB6FovM3B4VDDZ15o12gnADZsIk9fTAczLlcrmXLNN4"
			+ "PgmqgnwF0Ymj3bD5SavDOXxbA65AZJ7rBNAguLUo+xVkgxmoBH7R2sBxjTCc"
			+ "r07NEadxM3HQkt0aX5XYEl8eRoifwqYAI9h0ziZfTNes8elNfb3DoPPjqq6V"
			+ "mMg0f0iMS4W8LjNPorjRB+kIosa1deAGPhq0eJ8yr0/s2QR2/WFD5P4aXc8I"
			+ "KWleklnIImS3zqiPrq6tl2Bm8DZj7vXlTOwmraSQxUwzCKwYob1yGvNOUQTq"
			+ "pG6jxn7jgDawHU1+WjWQe4Q34/pWeGLysxTraMa+Ug9kPe+jy/qRX2xwvKBZ");

		private static AsymmetricCipherKeyPair SignKP
		{
			get { return signKP == null ? (signKP = CmsTestUtil.MakeKeyPair()) : signKP; }
		}

		private static AsymmetricCipherKeyPair OrigKP
		{
			get { return origKP == null ? (origKP = CmsTestUtil.MakeKeyPair()) : origKP; }
		}

//		private static AsymmetricCipherKeyPair ReciKP
//		{
//			get { return reciKP == null ? (reciKP = CmsTestUtil.MakeKeyPair()) : reciKP; }
//		}

		private static AsymmetricCipherKeyPair OrigDsaKP
		{
			get { return origDsaKP == null ? (origDsaKP = CmsTestUtil.MakeDsaKeyPair()) : origDsaKP; }
		}

		private static X509Certificate SignCert
		{
			get { return signCert == null ? (signCert = CmsTestUtil.MakeCertificate(SignKP, SignDN, SignKP, SignDN)) : signCert; }
		}

		private static X509Certificate OrigCert
		{
			get { return origCert == null ? (origCert = CmsTestUtil.MakeCertificate(OrigKP, OrigDN, SignKP, SignDN)) : origCert; }
		}

//		private static X509Certificate ReciCert
//		{
//			get { return reciCert == null ? (reciCert = CmsTestUtil.MakeCertificate(ReciKP, ReciDN, SignKP, SignDN)) : reciCert; }
//		}

		private static X509Certificate OrigDsaCert
		{
			get { return origDsaCert == null ? (origDsaCert = CmsTestUtil.MakeCertificate(OrigDsaKP, OrigDN, SignKP, SignDN)) : origDsaCert; }
		}

		private static X509Crl SignCrl
		{
			get { return signCrl == null ? (signCrl = CmsTestUtil.MakeCrl(SignKP)) : signCrl; }
		}

		private static X509Crl OrigCrl
		{
			get { return origCrl == null ? (origCrl = CmsTestUtil.MakeCrl(OrigKP)) : origCrl; }
		}

		private void VerifySignatures(
			CmsSignedDataParser	sp,
			byte[]				contentDigest)
		{
			var certStore = sp.GetCertificates();
			SignerInformationStore signers = sp.GetSignerInfos();

			foreach (SignerInformation signer in signers.GetSigners())
			{
				var certCollection = certStore.EnumerateMatches(signer.SignerID);

				var certEnum = certCollection.GetEnumerator();

				certEnum.MoveNext();
				X509Certificate	cert = certEnum.Current;

				Assert.IsTrue(signer.Verify(cert));

				if (contentDigest != null)
				{
					Assert.IsTrue(Arrays.AreEqual(contentDigest, signer.GetContentDigest()));
				}
			}
		}

		private void VerifySignatures(
			CmsSignedDataParser sp)
		{
			VerifySignatures(sp, null);
		}

		private void VerifyEncodedData(
			MemoryStream bOut)
		{
			CmsSignedDataParser sp = new CmsSignedDataParser(bOut.ToArray());

			sp.GetSignedContent().Drain();

			VerifySignatures(sp);

			sp.Close();
		}

	    private void CheckSigParseable(byte[] sig)
	    {
	        CmsSignedDataParser sp = new CmsSignedDataParser(sig);
	        sp.Version.ToString();
	        CmsTypedStream sc = sp.GetSignedContent();
	        if (sc != null)
	        {
	            sc.Drain();
	        }
	        sp.GetAttributeCertificates();
	        sp.GetCertificates();
	        sp.GetCrls();
	        sp.GetSignerInfos();
	        sp.Close();
	    }

		[Test]
		public void TestEarlyInvalidKeyException()
		{
			try
			{
				CmsSignedDataStreamGenerator gen = new CmsSignedDataStreamGenerator();
				gen.AddSigner(OrigKP.Private, OrigCert,
					"DSA", // DOESN'T MATCH KEY ALG
					CmsSignedDataStreamGenerator.DigestSha1);

				Assert.Fail("Expected InvalidKeyException in AddSigner");
			}
			catch (InvalidKeyException)
			{
				// Ignore
			}
		}

		[Test]
		public void TestEarlyNoSuchAlgorithmException()
		{
			try
			{
				CmsSignedDataStreamGenerator gen = new CmsSignedDataStreamGenerator();
				gen.AddSigner(OrigKP.Private, OrigCert,
					CmsSignedDataStreamGenerator.DigestSha1, // BAD OID!
					CmsSignedDataStreamGenerator.DigestSha1);

				Assert.Fail("Expected NoSuchAlgorithmException in AddSigner");
			}
			catch (SecurityUtilityException)
			{
				// Ignore
			}
		}

		[Test]
		public void TestSha1EncapsulatedSignature()
		{
			byte[]  encapSigData = Base64.Decode(
				"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"
				+ "n7MsJMoKN3Kd2Vzh6s10PFgeAAAAAAAA");

			CmsSignedDataParser sp = new CmsSignedDataParser(encapSigData);

			sp.GetSignedContent().Drain();

			VerifySignatures(sp);
		}

        //[Test]
        //public void TestSha1WithRsaNoAttributes()
        //{
        //    CmsProcessable msg = new CmsProcessableByteArray(Encoding.ASCII.GetBytes(TestMessage));

        //    IX509Store x509Certs = CmsTestUtil.MakeCertStore(OrigCert, SignCert);

        //    CmsSignedDataGenerator gen = new CmsSignedDataGenerator();
        //    gen.AddSigner(OrigKP.Private, OrigCert, CmsSignedDataGenerator.DigestSha1);
        //    gen.AddCertificates(x509Certs);

        //    CmsSignedData s = gen.Generate(CmsSignedDataGenerator.Data, msg, false, false);

        //    CmsSignedDataParser sp = new CmsSignedDataParser(
        //        new CmsTypedStream(new MemoryStream(Encoding.ASCII.GetBytes(TestMessage), false)), s.GetEncoded());

        //    sp.GetSignedContent().Drain();

        //    byte[] testBytes = Encoding.ASCII.GetBytes(TestMessage);

        //    // compute expected content digest
        //    byte[] hash = DigestUtilities.CalculateDigest("SHA1", testBytes);

        //    VerifySignatures(sp, hash);
        //}

        //[Test]
        //public void TestDsaNoAttributes()
        //{
        //    CmsProcessable msg = new CmsProcessableByteArray(Encoding.ASCII.GetBytes(TestMessage));

        //    IX509Store x509Certs = CmsTestUtil.MakeCertStore(OrigDsaCert, SignCert);

        //    CmsSignedDataGenerator gen = new CmsSignedDataGenerator();
        //    gen.AddSigner(OrigDsaKP.Private, OrigDsaCert, CmsSignedDataGenerator.DigestSha1);
        //    gen.AddCertificates(x509Certs);

        //    CmsSignedData s = gen.Generate(CmsSignedDataGenerator.Data, msg, false, false);

        //    CmsSignedDataParser sp = new CmsSignedDataParser(
        //        new CmsTypedStream(
        //        new MemoryStream(Encoding.ASCII.GetBytes(TestMessage), false)),
        //        s.GetEncoded());

        //    sp.GetSignedContent().Drain();

        //    byte[] testBytes = Encoding.ASCII.GetBytes(TestMessage);

        //    // compute expected content digest
        //    byte[] hash = DigestUtilities.CalculateDigest("SHA1", testBytes);

        //    VerifySignatures(sp, hash);
        //}

		[Test]
		public void TestSha1WithRsa()
		{
			MemoryStream bOut = new MemoryStream();

            var x509Certs = CmsTestUtil.MakeCertStore(OrigCert, SignCert);
            var x509Crls = CmsTestUtil.MakeCrlStore(SignCrl, OrigCrl);

            CmsSignedDataStreamGenerator gen = new CmsSignedDataStreamGenerator();
			gen.AddSigner(OrigKP.Private, OrigCert, CmsSignedDataStreamGenerator.DigestSha1);
			gen.AddCertificates(x509Certs);
			gen.AddCrls(x509Crls);

            Stream sigOut = gen.Open(bOut);

			byte[] testBytes = Encoding.ASCII.GetBytes(TestMessage);
			sigOut.Write(testBytes, 0, testBytes.Length);

			sigOut.Close();

			CheckSigParseable(bOut.ToArray());

			CmsSignedDataParser sp = new CmsSignedDataParser(
				new CmsTypedStream(new MemoryStream(testBytes, false)), bOut.ToArray());

			sp.GetSignedContent().Drain();

            // compute expected content digest
            byte[] hash = DigestUtilities.CalculateDigest("SHA1", testBytes);

            VerifySignatures(sp, hash);

			//
			// try using existing signer
			//
			gen = new CmsSignedDataStreamGenerator();
			gen.AddSigners(sp.GetSignerInfos());
			gen.AddCertificates(sp.GetCertificates());
			gen.AddCrls(sp.GetCrls());

            bOut.SetLength(0);

            sigOut = gen.Open(bOut, true);
			sigOut.Write(testBytes, 0, testBytes.Length);
			sigOut.Close();

            VerifyEncodedData(bOut);

            //
			// look for the CRLs
			//
			var col = new List<X509Crl>(x509Crls.EnumerateMatches(null));

            Assert.AreEqual(2, col.Count);
			Assert.IsTrue(col.Contains(SignCrl));
			Assert.IsTrue(col.Contains(OrigCrl));
		}

        [Test]
        public void TestCrlAndOtherRevocationInfoFormat()
        {
            MemoryStream bOut = new MemoryStream();

            var x509Certs = CmsTestUtil.MakeCertStore(OrigCert, SignCert);
            var x509Crls = CmsTestUtil.MakeCrlStore(SignCrl, OrigCrl);
			var x509OtherRevocationInfos = CmsTestUtil.MakeOtherRevocationInfoStore(OcspResponseBytes);

            CmsSignedDataStreamGenerator gen = new CmsSignedDataStreamGenerator();
            gen.AddSigner(OrigKP.Private, OrigCert, CmsSignedGenerator.DigestSha1);
            gen.AddCertificates(x509Certs);
            gen.AddCrls(x509Crls);
            gen.AddOtherRevocationInfos(CmsObjectIdentifiers.id_ri_ocsp_response, x509OtherRevocationInfos);

            Stream sigOut = gen.Open(bOut);

            byte[] testBytes = Encoding.ASCII.GetBytes(TestMessage);
            sigOut.Write(testBytes, 0, testBytes.Length);

            sigOut.Close();

            CheckSigParseable(bOut.ToArray());

            CmsSignedDataParser sp = new CmsSignedDataParser(
                new CmsTypedStream(new MemoryStream(testBytes, false)), bOut.ToArray());

            sp.GetSignedContent().Drain();

            // compute expected content digest
            byte[] hash = DigestUtilities.CalculateDigest("SHA1", testBytes);

            VerifySignatures(sp, hash);

            //
            // try using existing signer
            //
            gen = new CmsSignedDataStreamGenerator();
            gen.AddSigners(sp.GetSignerInfos());
            gen.AddCertificates(sp.GetCertificates());
            gen.AddCrls(sp.GetCrls());

            var spOtherRevocationInfos = sp.GetOtherRevInfos(CmsObjectIdentifiers.id_ri_ocsp_response);
            gen.AddOtherRevocationInfos(CmsObjectIdentifiers.id_ri_ocsp_response, spOtherRevocationInfos);

            bOut.SetLength(0);

            sigOut = gen.Open(bOut, true);
            sigOut.Write(testBytes, 0, testBytes.Length);
            sigOut.Close();

            VerifyEncodedData(bOut);

            //
            // look for the CRLs
            //
            var crls = new List<X509Crl>(x509Crls.EnumerateMatches(null));

            Assert.AreEqual(2, crls.Count);
            Assert.IsTrue(crls.Contains(SignCrl));
            Assert.IsTrue(crls.Contains(OrigCrl));

            //
            // look for OtherRevocationInfo
            //
            var x509OtherRevocationInfoList = new List<Asn1Encodable>(
				x509OtherRevocationInfos.EnumerateMatches(null));

            Assert.AreEqual(1, x509OtherRevocationInfoList.Count);

            var spOtherRevocationInfoList = new List<Asn1Encodable>(
                spOtherRevocationInfos.EnumerateMatches(null));

            Assert.AreEqual(1, spOtherRevocationInfoList.Count);
        }

        [Test]
		public void TestSha1WithRsaNonData()
		{
			MemoryStream bOut = new MemoryStream();

            var x509Certs = CmsTestUtil.MakeCertStore(OrigCert, SignCert);
            var x509Crls = CmsTestUtil.MakeCrlStore(SignCrl, OrigCrl);

            CmsSignedDataStreamGenerator gen = new CmsSignedDataStreamGenerator();
			gen.AddSigner(OrigKP.Private, OrigCert, CmsSignedDataStreamGenerator.DigestSha1);
			gen.AddCertificates(x509Certs);
			gen.AddCrls(x509Crls);

            byte[] testBytes = Encoding.ASCII.GetBytes(TestMessage);

            Stream sigOut = gen.Open(bOut, "1.2.3.4", true);
			sigOut.Write(testBytes, 0, testBytes.Length);
			sigOut.Close();

            CmsSignedDataParser sp = new CmsSignedDataParser(bOut.ToArray());

            CmsTypedStream stream = sp.GetSignedContent();

			Assert.AreEqual("1.2.3.4", stream.ContentType);

			stream.Drain();

            // compute expected content digest
            byte[] hash = DigestUtilities.CalculateDigest("SHA1", testBytes);

            VerifySignatures(sp, hash);
		}

        [Test]
		public void TestSha1AndMD5WithRsa()
		{
			MemoryStream bOut = new MemoryStream();

            var x509Certs = CmsTestUtil.MakeCertStore(OrigCert, SignCert);

            CmsSignedDataStreamGenerator gen = new CmsSignedDataStreamGenerator();
			gen.AddDigests(CmsSignedDataStreamGenerator.DigestSha1,
				CmsSignedDataStreamGenerator.DigestMD5);

            byte[] testBytes = Encoding.ASCII.GetBytes(TestMessage);

            Stream sigOut = gen.Open(bOut);
			sigOut.Write(testBytes, 0, testBytes.Length);

			gen.AddCertificates(x509Certs);
			gen.AddSigner(OrigKP.Private, OrigCert, CmsSignedDataStreamGenerator.DigestSha1);
			gen.AddSigner(OrigKP.Private, OrigCert, CmsSignedDataStreamGenerator.DigestMD5);

			sigOut.Close();

			CheckSigParseable(bOut.ToArray());

			CmsSignedDataParser sp = new CmsSignedDataParser(
				new CmsTypedStream(new MemoryStream(testBytes, false)), bOut.ToArray());

			sp.GetSignedContent().Drain();

			VerifySignatures(sp);
		}

		[Test]
		public void TestSha1WithRsaEncapsulatedBufferedStream()
		{
			MemoryStream bOut = new MemoryStream();

            var x509Certs = CmsTestUtil.MakeCertStore(OrigCert, SignCert);

            //
			// find unbuffered length
			//
			CmsSignedDataStreamGenerator gen = new CmsSignedDataStreamGenerator();
			gen.AddSigner(OrigKP.Private, OrigCert, CmsSignedDataStreamGenerator.DigestSha1);
			gen.AddCertificates(x509Certs);

			Stream sigOut = gen.Open(bOut, true);
			for (int i = 0; i != 2000; i++)
			{
				sigOut.WriteByte((byte)(i & 0xff));
			}
			sigOut.Close();

			CmsSignedDataParser sp = new CmsSignedDataParser(bOut.ToArray());

            sp.GetSignedContent().Drain();

            VerifySignatures(sp);

			int unbufferedLength = bOut.ToArray().Length;

			//
			// find buffered length with buffered stream - should be equal
			//
			bOut.SetLength(0);

			gen = new CmsSignedDataStreamGenerator();
			gen.AddSigner(OrigKP.Private, OrigCert, CmsSignedDataStreamGenerator.DigestSha1);
			gen.AddCertificates(x509Certs);

			sigOut = gen.Open(bOut, true);

			byte[] data = new byte[2000];
			for (int i = 0; i != 2000; i++)
			{
				data[i] = (byte)(i & 0xff);
			}

            Streams.PipeAll(new MemoryStream(data, false), sigOut);
			sigOut.Close();

            VerifyEncodedData(bOut);

			Assert.AreEqual(unbufferedLength, bOut.ToArray().Length);
		}

		[Test]
		public void TestSha1WithRsaEncapsulatedBuffered()
		{
			MemoryStream bOut = new MemoryStream();

            var x509Certs = CmsTestUtil.MakeCertStore(OrigCert, SignCert);

			//
			// find unbuffered length
			//
			CmsSignedDataStreamGenerator gen = new CmsSignedDataStreamGenerator();
			gen.AddSigner(OrigKP.Private, OrigCert, CmsSignedDataStreamGenerator.DigestSha1);
			gen.AddCertificates(x509Certs);

			Stream sigOut = gen.Open(bOut, true);

			for (int i = 0; i != 2000; i++)
			{
				sigOut.WriteByte((byte)(i & 0xff));
			}

			sigOut.Close();

			CmsSignedDataParser sp = new CmsSignedDataParser(bOut.ToArray());

			sp.GetSignedContent().Drain();

			VerifySignatures(sp);

			int unbufferedLength = bOut.ToArray().Length;

			//
			// find buffered length - buffer size less than default
			//
			bOut.SetLength(0);

			gen = new CmsSignedDataStreamGenerator();
			gen.SetBufferSize(300);
            gen.AddSigner(OrigKP.Private, OrigCert, CmsSignedDataStreamGenerator.DigestSha1);
			gen.AddCertificates(x509Certs);

            sigOut = gen.Open(bOut, true);

			for (int i = 0; i != 2000; i++)
			{
				sigOut.WriteByte((byte)(i & 0xff));
			}

			sigOut.Close();

			VerifyEncodedData(bOut);

			Assert.IsTrue(unbufferedLength < bOut.ToArray().Length);
		}

		[Test]
		public void TestSha1WithRsaEncapsulated()
		{
			MemoryStream bOut = new MemoryStream();

            var x509Certs = CmsTestUtil.MakeCertStore(OrigCert, SignCert);

            CmsSignedDataStreamGenerator gen = new CmsSignedDataStreamGenerator();
			gen.AddSigner(OrigKP.Private, OrigCert, CmsSignedDataStreamGenerator.DigestSha1);
			gen.AddCertificates(x509Certs);

            byte[] testBytes = Encoding.ASCII.GetBytes(TestMessage);

            Stream sigOut = gen.Open(bOut, true);
			sigOut.Write(testBytes, 0, testBytes.Length);
			sigOut.Close();

            CmsSignedDataParser sp = new CmsSignedDataParser(bOut.ToArray());

			sp.GetSignedContent().Drain();

			VerifySignatures(sp);

            byte[] contentDigest = (byte[])gen.GetGeneratedDigests()[CmsSignedGenerator.DigestSha1];

            var signers = sp.GetSignerInfos().GetSigners();

			AttributeTable table = ((SignerInformation) signers[0]).SignedAttributes;
			Asn1.Cms.Attribute hash = table[CmsAttributes.MessageDigest];

			Assert.IsTrue(Arrays.AreEqual(contentDigest, ((Asn1OctetString)hash.AttrValues[0]).GetOctets()));

			//
			// try using existing signer
			//
			gen = new CmsSignedDataStreamGenerator();
			gen.AddSigners(sp.GetSignerInfos());
			gen.AddCertificates(sp.GetCertificates());
			gen.AddCrls(sp.GetCrls());

            bOut.SetLength(0);

            sigOut = gen.Open(bOut, true);
			sigOut.Write(testBytes, 0, testBytes.Length);
			sigOut.Close();

            CmsSignedData sd = new CmsSignedData(
				new CmsProcessableByteArray(testBytes), bOut.ToArray());

            Assert.AreEqual(1, sd.GetSignerInfos().GetSigners().Count);

            VerifyEncodedData(bOut);
		}

		private static readonly DerObjectIdentifier dummyOid1 = new DerObjectIdentifier("1.2.3");
		private static readonly DerObjectIdentifier dummyOid2 = new DerObjectIdentifier("1.2.3.4");

		private class SignedGenAttributeTableGenerator
			: DefaultSignedAttributeTableGenerator
		{
			public override AttributeTable GetAttributes(IDictionary<CmsAttributeTableParameter, object> parameters)
			{
				var table = CreateStandardAttributeTable(parameters);

				DerOctetString val = new DerOctetString((byte[])parameters[CmsAttributeTableParameter.Digest]);
				Asn1.Cms.Attribute attr = new Asn1.Cms.Attribute(dummyOid1, new DerSet(val));

				table[attr.AttrType] = attr;

				return new AttributeTable(table);
			}
		};

		private class UnsignedGenAttributeTableGenerator
			: CmsAttributeTableGenerator
		{
			public AttributeTable GetAttributes(IDictionary<CmsAttributeTableParameter, object> parameters)
			{
				DerOctetString val = new DerOctetString((byte[])parameters[CmsAttributeTableParameter.Signature]);
				Asn1.Cms.Attribute attr = new Asn1.Cms.Attribute(dummyOid2, new DerSet(val));

				return new AttributeTable(new DerSet(attr));
			}
		};

		[Test]
	    public void TestSha1WithRsaEncapsulatedSubjectKeyID()
	    {
	        MemoryStream bOut = new MemoryStream();

            var x509Certs = CmsTestUtil.MakeCertStore(OrigCert, SignCert);

            CmsSignedDataStreamGenerator gen = new CmsSignedDataStreamGenerator();
	        gen.AddSigner(OrigKP.Private,
				CmsTestUtil.CreateSubjectKeyId(OrigCert.GetPublicKey()).GetKeyIdentifier(),
				CmsSignedDataStreamGenerator.DigestSha1);
			gen.AddCertificates(x509Certs);

            byte[] testBytes = Encoding.ASCII.GetBytes(TestMessage);

            Stream sigOut = gen.Open(bOut, true);
            sigOut.Write(testBytes, 0, testBytes.Length);
			sigOut.Close();

            CmsSignedDataParser sp = new CmsSignedDataParser(bOut.ToArray());

			sp.GetSignedContent().Drain();

			VerifySignatures(sp);

			byte[] contentDigest = (byte[])gen.GetGeneratedDigests()[CmsSignedGenerator.DigestSha1];

			var signers = sp.GetSignerInfos().GetSigners();

			AttributeTable table = ((SignerInformation) signers[0]).SignedAttributes;
			Asn1.Cms.Attribute hash = table[CmsAttributes.MessageDigest];

			Assert.IsTrue(Arrays.AreEqual(contentDigest, ((Asn1OctetString)hash.AttrValues[0]).GetOctets()));

			//
			// try using existing signer
			//
			gen = new CmsSignedDataStreamGenerator();
			gen.AddSigners(sp.GetSignerInfos());
			gen.AddCertificates(sp.GetCertificates());

            bOut.SetLength(0);

            sigOut = gen.Open(bOut, true);
			sigOut.Write(testBytes, 0, testBytes.Length);
			sigOut.Close();

            CmsSignedData sd = new CmsSignedData(new CmsProcessableByteArray(testBytes), bOut.ToArray());

			Assert.AreEqual(1, sd.GetSignerInfos().GetSigners().Count);

	        VerifyEncodedData(bOut);
	    }

        [Test]
		public void TestAttributeGenerators()
		{
			MemoryStream bOut = new MemoryStream();

            var x509Certs = CmsTestUtil.MakeCertStore(OrigCert, SignCert);

			CmsAttributeTableGenerator signedGen = new SignedGenAttributeTableGenerator();
			CmsAttributeTableGenerator unsignedGen = new UnsignedGenAttributeTableGenerator();

            CmsSignedDataStreamGenerator gen = new CmsSignedDataStreamGenerator();
            gen.AddSigner(OrigKP.Private, OrigCert,
				CmsSignedDataStreamGenerator.DigestSha1, signedGen, unsignedGen);
			gen.AddCertificates(x509Certs);

            byte[] testBytes = Encoding.ASCII.GetBytes(TestMessage);

			Stream sigOut = gen.Open(bOut, true);
			sigOut.Write(testBytes, 0, testBytes.Length);
			sigOut.Close();

			CmsSignedDataParser sp = new CmsSignedDataParser(bOut.ToArray());

			sp.GetSignedContent().Drain();

			VerifySignatures(sp);

			//
			// check attributes
			//
			SignerInformationStore signers = sp.GetSignerInfos();

			foreach (SignerInformation signer in signers.GetSigners())
			{
				CheckAttribute(signer.GetContentDigest(), signer.SignedAttributes[dummyOid1]);
				CheckAttribute(signer.GetSignature(), signer.UnsignedAttributes[dummyOid2]);
			}
		}

		private void CheckAttribute(byte[] expected, Asn1.Cms.Attribute	attr)
		{
			DerOctetString value = (DerOctetString)attr.AttrValues[0];

            Assert.AreEqual(new DerOctetString(expected), value);
		}

        [Test]
		public void TestWithAttributeCertificate()
		{
            var x509Certs = CmsTestUtil.MakeCertStore(SignCert);

            CmsSignedDataStreamGenerator gen = new CmsSignedDataStreamGenerator();
			gen.AddSigner(OrigKP.Private, OrigCert, CmsSignedDataGenerator.DigestSha1);
			gen.AddCertificates(x509Certs);

            var attrCert = CmsTestUtil.GetAttributeCertificate();

            var store = CmsTestUtil.MakeAttrCertStore(attrCert);

            gen.AddAttributeCertificates(store);

            MemoryStream bOut = new MemoryStream();

            byte[] testBytes = Encoding.ASCII.GetBytes(TestMessage);

			Stream sigOut = gen.Open(bOut, true);
			sigOut.Write(testBytes, 0, testBytes.Length);
			sigOut.Close();

            CmsSignedDataParser sp = new CmsSignedDataParser(bOut.ToArray());

            sp.GetSignedContent().Drain();

			Assert.AreEqual(4, sp.Version);

			store = sp.GetAttributeCertificates();

			var coll = new List<X509V2AttributeCertificate>(store.EnumerateMatches(null));

			Assert.AreEqual(1, coll.Count);

			Assert.IsTrue(coll.Contains(attrCert));
		}

		[Test]
		public void TestSignerStoreReplacement()
		{
			MemoryStream bOut = new MemoryStream();
			byte[] data = Encoding.ASCII.GetBytes(TestMessage);

            var x509Certs = CmsTestUtil.MakeCertStore(OrigCert, SignCert);

			CmsSignedDataStreamGenerator gen = new CmsSignedDataStreamGenerator();
			gen.AddSigner(OrigKP.Private, OrigCert, CmsSignedDataStreamGenerator.DigestSha1);
			gen.AddCertificates(x509Certs);

			Stream sigOut = gen.Open(bOut, false);
			sigOut.Write(data, 0, data.Length);
			sigOut.Close();

			CheckSigParseable(bOut.ToArray());

			//
			// create new Signer
			//
			MemoryStream original = new MemoryStream(bOut.ToArray(), false);

			bOut.SetLength(0);

			gen = new CmsSignedDataStreamGenerator();
			gen.AddSigner(OrigKP.Private, OrigCert, CmsSignedDataStreamGenerator.DigestSha224);

            gen.AddCertificates(x509Certs);

			sigOut = gen.Open(bOut);
			sigOut.Write(data, 0, data.Length);
			sigOut.Close();

            CheckSigParseable(bOut.ToArray());

			CmsSignedData sd = new CmsSignedData(bOut.ToArray());

			//
			// replace signer
			//
			MemoryStream newOut = new MemoryStream();

			CmsSignedDataParser.ReplaceSigners(original, sd.GetSignerInfos(), newOut);

			sd = new CmsSignedData(new CmsProcessableByteArray(data), newOut.ToArray());

			var signerEnum = sd.GetSignerInfos().GetSigners().GetEnumerator();
			signerEnum.MoveNext();
			SignerInformation signer = signerEnum.Current;

			Assert.AreEqual(signer.DigestAlgOid, CmsSignedDataStreamGenerator.DigestSha224);

			CmsSignedDataParser sp = new CmsSignedDataParser(new CmsTypedStream(
				new MemoryStream(data, false)), newOut.ToArray());

			sp.GetSignedContent().Drain();

			VerifySignatures(sp);
		}

		[Test]
		public void TestEncapsulatedSignerStoreReplacement()
		{
			MemoryStream bOut = new MemoryStream();

            var x509Certs = CmsTestUtil.MakeCertStore(OrigCert, SignCert);

            CmsSignedDataStreamGenerator gen = new CmsSignedDataStreamGenerator();
			gen.AddSigner(OrigKP.Private, OrigCert, CmsSignedDataStreamGenerator.DigestSha1);
			gen.AddCertificates(x509Certs);

            byte[] testBytes = Encoding.ASCII.GetBytes(TestMessage);

			Stream sigOut = gen.Open(bOut, true);
			sigOut.Write(testBytes, 0, testBytes.Length);
			sigOut.Close();

			//
			// create new Signer
			//
			MemoryStream  original = new MemoryStream(bOut.ToArray(), false);

			bOut.SetLength(0);

			gen = new CmsSignedDataStreamGenerator();
			gen.AddSigner(OrigKP.Private, OrigCert, CmsSignedDataStreamGenerator.DigestSha224);
			gen.AddCertificates(x509Certs);

            sigOut = gen.Open(bOut, true);
			sigOut.Write(testBytes, 0, testBytes.Length);
			sigOut.Close();

            CmsSignedData sd = new CmsSignedData(bOut.ToArray());

            //
			// replace signer
			//
			MemoryStream newOut = new MemoryStream();

			CmsSignedDataParser.ReplaceSigners(original, sd.GetSignerInfos(), newOut);

			sd = new CmsSignedData(newOut.ToArray());

			var signerEnum = sd.GetSignerInfos().GetSigners().GetEnumerator();
			signerEnum.MoveNext();
			SignerInformation signer = signerEnum.Current;

			Assert.AreEqual(signer.DigestAlgOid, CmsSignedDataStreamGenerator.DigestSha224);

			CmsSignedDataParser sp = new CmsSignedDataParser(newOut.ToArray());

			sp.GetSignedContent().Drain();

			VerifySignatures(sp);
		}

		[Test]
		public void TestCertStoreReplacement()
		{
			MemoryStream bOut = new MemoryStream();
			byte[] data = Encoding.ASCII.GetBytes(TestMessage);

            var x509Certs = CmsTestUtil.MakeCertStore(OrigDsaCert);

            CmsSignedDataStreamGenerator gen = new CmsSignedDataStreamGenerator();
			gen.AddSigner(OrigKP.Private, OrigCert, CmsSignedDataStreamGenerator.DigestSha1);
			gen.AddCertificates(x509Certs);

            Stream sigOut = gen.Open(bOut);
			sigOut.Write(data, 0, data.Length);
			sigOut.Close();

            CheckSigParseable(bOut.ToArray());

            //
			// create new certstore with the right certificates
			//
            x509Certs = CmsTestUtil.MakeCertStore(OrigCert, SignCert);

			//
			// replace certs
			//
			MemoryStream original = new MemoryStream(bOut.ToArray(), false);
			MemoryStream newOut = new MemoryStream();

			CmsSignedDataParser.ReplaceCertificatesAndCrls(original, x509Certs, null, null, newOut);

			CmsSignedDataParser sp = new CmsSignedDataParser(new CmsTypedStream(new MemoryStream(data, false)), newOut.ToArray());

			sp.GetSignedContent().Drain();

			VerifySignatures(sp);
		}

		[Test]
		public void TestEncapsulatedCertStoreReplacement()
		{
			MemoryStream bOut = new MemoryStream();

            var x509Certs = CmsTestUtil.MakeCertStore(OrigDsaCert);

			CmsSignedDataStreamGenerator gen = new CmsSignedDataStreamGenerator();

			gen.AddSigner(OrigKP.Private, OrigCert, CmsSignedDataStreamGenerator.DigestSha1);

			gen.AddCertificates(x509Certs);

			Stream sigOut = gen.Open(bOut, true);

			byte[] testBytes = Encoding.ASCII.GetBytes(TestMessage);
			sigOut.Write(testBytes, 0, testBytes.Length);

			sigOut.Close();

			//
			// create new certstore with the right certificates
			//
            x509Certs = CmsTestUtil.MakeCertStore(OrigCert, SignCert);

            //
			// replace certs
			//
			MemoryStream original = new MemoryStream(bOut.ToArray(), false);
			MemoryStream newOut = new MemoryStream();

			CmsSignedDataParser.ReplaceCertificatesAndCrls(original, x509Certs, null, null, newOut);

			CmsSignedDataParser sp = new CmsSignedDataParser(newOut.ToArray());

			sp.GetSignedContent().Drain();

			VerifySignatures(sp);
		}

		[Test]
		public void TestCertOrdering1()
		{
			MemoryStream bOut = new MemoryStream();

            var x509Certs = CmsTestUtil.MakeCertStore(OrigCert, SignCert);

            CmsSignedDataStreamGenerator gen = new CmsSignedDataStreamGenerator();
			gen.AddSigner(OrigKP.Private, OrigCert, CmsSignedDataStreamGenerator.DigestSha1);
			gen.AddCertificates(x509Certs);

            byte[] testBytes = Encoding.ASCII.GetBytes(TestMessage);

			Stream sigOut = gen.Open(bOut, true);
			sigOut.Write(testBytes, 0, testBytes.Length);
			sigOut.Close();

            CmsSignedDataParser sp = new CmsSignedDataParser(bOut.ToArray());

			sp.GetSignedContent().Drain();
			x509Certs = sp.GetCertificates();
			var a = new List<X509Certificate>(x509Certs.EnumerateMatches(null));

			Assert.AreEqual(2, a.Count);
			Assert.AreEqual(OrigCert, a[0]);
			Assert.AreEqual(SignCert, a[1]);
		}

		[Test]
		public void TestCertOrdering2()
		{
			MemoryStream bOut = new MemoryStream();

            var x509Certs = CmsTestUtil.MakeCertStore(SignCert, OrigCert);

            CmsSignedDataStreamGenerator gen = new CmsSignedDataStreamGenerator();
			gen.AddSigner(OrigKP.Private, OrigCert, CmsSignedDataStreamGenerator.DigestSha1);
			gen.AddCertificates(x509Certs);

            byte[] testBytes = Encoding.ASCII.GetBytes(TestMessage);

            Stream sigOut = gen.Open(bOut, true);
			sigOut.Write(testBytes, 0, testBytes.Length);
			sigOut.Close();

            CmsSignedDataParser sp = new CmsSignedDataParser(bOut.ToArray());

            sp.GetSignedContent().Drain();
			x509Certs = sp.GetCertificates();
			var a = new List<X509Certificate>(x509Certs.EnumerateMatches(null));

			Assert.AreEqual(2, a.Count);
			Assert.AreEqual(SignCert, a[0]);
			Assert.AreEqual(OrigCert, a[1]);
		}

        [Test]
        public void TestCertsOnly()
        {
            var x509Certs = CmsTestUtil.MakeCertStore(OrigCert, SignCert);

            MemoryStream bOut = new MemoryStream();

            CmsSignedDataStreamGenerator gen = new CmsSignedDataStreamGenerator();
            gen.AddCertificates(x509Certs);
            gen.Open(bOut).Close();

            CheckSigParseable(bOut.ToArray());
        }
	}
}
