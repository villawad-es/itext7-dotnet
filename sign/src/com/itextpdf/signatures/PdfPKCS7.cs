/*
$Id$

This file is part of the iText (R) project.
Copyright (c) 1998-2016 iText Group NV
Authors: Bruno Lowagie, Paulo Soares, et al.

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License version 3
as published by the Free Software Foundation with the addition of the
following permission added to Section 15 as permitted in Section 7(a):
FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
OF THIRD PARTY RIGHTS

This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
or FITNESS FOR A PARTICULAR PURPOSE.
See the GNU Affero General Public License for more details.
You should have received a copy of the GNU Affero General Public License
along with this program; if not, see http://www.gnu.org/licenses or write to
the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
Boston, MA, 02110-1301 USA, or download the license from the following URL:
http://itextpdf.com/terms-of-use/

The interactive user interfaces in modified source and object code versions
of this program must display Appropriate Legal Notices, as required under
Section 5 of the GNU Affero General Public License.

In accordance with Section 7(b) of the GNU Affero General Public License,
a covered work must retain the producer line in every PDF that is created
or manipulated using iText.

You can be released from the requirements of the license by purchasing
a commercial license. Buying such a license is mandatory as soon as you
develop commercial activities involving the iText software without
disclosing the source code of your own applications.
These activities include: offering paid services to customers as an ASP,
serving PDFs on the fly in a web application, shipping iText with a closed
source product.

For more information, please contact iText Software Corp. at this
address: sales@itextpdf.com
*/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using com.itextpdf.kernel;
using com.itextpdf.kernel.pdf;
using java.math;
using java.security;
using java.security.cert;
using org.bouncycastle.@operator;
using org.bouncycastle.@operator.jcajce;
using org.bouncycastle.asn1;
using org.bouncycastle.asn1.cms;
using org.bouncycastle.asn1.ess;
using org.bouncycastle.asn1.ocsp;
using org.bouncycastle.asn1.pkcs;
using org.bouncycastle.asn1.tsp;
using org.bouncycastle.asn1.x509;
using org.bouncycastle.cert.jcajce;
using org.bouncycastle.cert.ocsp;
using org.bouncycastle.jce;
using org.bouncycastle.jce.provider;
using org.bouncycastle.tsp;

namespace com.itextpdf.signatures
{
	/// <summary>
	/// This class does all the processing related to signing
	/// and verifying a PKCS#7 signature.
	/// </summary>
	public class PdfPKCS7
	{
		/// <summary>Assembles all the elements needed to create a signature, except for the data.
		/// 	</summary>
		/// <param name="privKey">the private key</param>
		/// <param name="certChain">the certificate chain</param>
		/// <param name="interfaceDigest">the interface digest</param>
		/// <param name="hashAlgorithm">the hash algorithm</param>
		/// <param name="provider">the provider or <code>null</code> for the default provider
		/// 	</param>
		/// <param name="hasRSAdata"><CODE>true</CODE> if the sub-filter is adbe.pkcs7.sha1</param>
		/// <exception cref="java.security.InvalidKeyException">on error</exception>
		/// <exception cref="java.security.NoSuchProviderException">on error</exception>
		/// <exception cref="java.security.NoSuchAlgorithmException">on error</exception>
		public PdfPKCS7(PrivateKey privKey, Certificate[] certChain, String hashAlgorithm
			, String provider, IExternalDigest interfaceDigest, bool hasRSAdata)
		{
			// Constructors for creating new signatures
			this.provider = provider;
			this.interfaceDigest = interfaceDigest;
			// message digest
			digestAlgorithmOid = DigestAlgorithms.GetAllowedDigest(hashAlgorithm);
			if (digestAlgorithmOid == null)
			{
				throw new PdfException(PdfException.UnknownHashAlgorithm1).SetMessageParams(hashAlgorithm
					);
			}
			// Copy the certificates
			signCert = (X509Certificate)certChain[0];
			certs = new List<Certificate>();
			foreach (Certificate element in certChain)
			{
				certs.Add(element);
			}
			// initialize and add the digest algorithms.
			digestalgos = new HashSet<String>();
			digestalgos.Add(digestAlgorithmOid);
			// find the signing algorithm (RSA or DSA)
			if (privKey != null)
			{
				digestEncryptionAlgorithmOid = privKey.GetAlgorithm();
				if (digestEncryptionAlgorithmOid.Equals("RSA"))
				{
					digestEncryptionAlgorithmOid = SecurityIDs.ID_RSA;
				}
				else
				{
					if (digestEncryptionAlgorithmOid.Equals("DSA"))
					{
						digestEncryptionAlgorithmOid = SecurityIDs.ID_DSA;
					}
					else
					{
						throw new PdfException(PdfException.UnknownKeyAlgorithm1).SetMessageParams(digestEncryptionAlgorithmOid
							);
					}
				}
			}
			// initialize the RSA data
			if (hasRSAdata)
			{
				RSAdata = new byte[0];
				messageDigest = DigestAlgorithms.GetMessageDigest(GetHashAlgorithm(), provider);
			}
			// initialize the Signature object
			if (privKey != null)
			{
				sig = InitSignature(privKey);
			}
		}

		/// <summary>Use this constructor if you want to verify a signature using the sub-filter adbe.x509.rsa_sha1.
		/// 	</summary>
		/// <param name="contentsKey">the /Contents key</param>
		/// <param name="certsKey">the /Cert key</param>
		/// <param name="provider">the provider or <code>null</code> for the default provider
		/// 	</param>
		public PdfPKCS7(byte[] contentsKey, byte[] certsKey, String provider)
		{
			// Constructors for validating existing signatures
			try
			{
				this.provider = provider;
				X509CertParser cr = new X509CertParser();
				cr.EngineInit(new MemoryStream(certsKey));
				certs = cr.EngineReadAll();
				signCerts = certs;
				signCert = (X509Certificate)certs.GetEnumerator().Current;
				crls = new List<CRL>();
				ASN1InputStream @in = new ASN1InputStream(new MemoryStream(contentsKey));
				digest = ((ASN1OctetString)@in.ReadObject()).GetOctets();
				if (provider == null)
				{
					sig = Signature.GetInstance("SHA1withRSA");
				}
				else
				{
					sig = Signature.GetInstance("SHA1withRSA", provider);
				}
				sig.InitVerify(signCert.GetPublicKey());
				// setting the oid to SHA1withRSA
				digestAlgorithmOid = "1.2.840.10040.4.3";
				digestEncryptionAlgorithmOid = "1.3.36.3.3.1.2";
			}
			catch (Exception e)
			{
				throw new PdfException(e);
			}
		}

		/// <summary>Use this constructor if you want to verify a signature.</summary>
		/// <param name="contentsKey">the /Contents key</param>
		/// <param name="filterSubtype">the filtersubtype</param>
		/// <param name="provider">the provider or <code>null</code> for the default provider
		/// 	</param>
		public PdfPKCS7(byte[] contentsKey, PdfName filterSubtype, String provider)
		{
			this.filterSubtype = filterSubtype;
			isTsp = PdfName.ETSI_RFC3161.Equals(filterSubtype);
			isCades = PdfName.ETSI_CAdES_DETACHED.Equals(filterSubtype);
			try
			{
				this.provider = provider;
				ASN1InputStream din = new ASN1InputStream(new MemoryStream(contentsKey));
				//
				// Basic checks to make sure it's a PKCS#7 SignedData Object
				//
				ASN1Primitive pkcs;
				try
				{
					pkcs = din.ReadObject();
				}
				catch (System.IO.IOException)
				{
					throw new ArgumentException(PdfException.CantDecodePkcs7SigneddataObject);
				}
				if (!(pkcs is ASN1Sequence))
				{
					throw new ArgumentException(PdfException.NotAValidPkcs7ObjectNotASequence);
				}
				ASN1Sequence signedData = (ASN1Sequence)pkcs;
				ASN1ObjectIdentifier objId = (ASN1ObjectIdentifier)signedData.GetObjectAt(0);
				if (!objId.GetId().Equals(SecurityIDs.ID_PKCS7_SIGNED_DATA))
				{
					throw new ArgumentException(PdfException.NotAValidPkcs7ObjectNotSignedData);
				}
				ASN1Sequence content = (ASN1Sequence)((ASN1TaggedObject)signedData.GetObjectAt(1)
					).GetObject();
				// the positions that we care are:
				//     0 - version
				//     1 - digestAlgorithms
				//     2 - possible ID_PKCS7_DATA
				//     (the certificates and crls are taken out by other means)
				//     last - signerInfos
				// the version
				version = ((ASN1Integer)content.GetObjectAt(0)).GetValue();
				// the digestAlgorithms
				digestalgos = new HashSet<String>();
				IEnumerator<ASN1Sequence> e_1 = ((ASN1Set)content.GetObjectAt(1)).GetObjects();
				while (e_1.MoveNext())
				{
					ASN1Sequence s = e_1.Current;
					ASN1ObjectIdentifier o = (ASN1ObjectIdentifier)s.GetObjectAt(0);
					digestalgos.Add(o.GetId());
				}
				// the possible ID_PKCS7_DATA
				ASN1Sequence rsaData = (ASN1Sequence)content.GetObjectAt(2);
				if (rsaData.Size() > 1)
				{
					ASN1OctetString rsaDataContent = (ASN1OctetString)((ASN1TaggedObject)rsaData.GetObjectAt
						(1)).GetObject();
					RSAdata = rsaDataContent.GetOctets();
				}
				int next = 3;
				while (content.GetObjectAt(next) is ASN1TaggedObject)
				{
					++next;
				}
				// the certificates
				/*
				This should work, but that's not always the case because of a bug in BouncyCastle:
				*/
				X509CertParser cr = new X509CertParser();
				cr.EngineInit(new MemoryStream(contentsKey));
				certs = cr.EngineReadAll();
				/*
				The following workaround was provided by Alfonso Massa, but it doesn't always work either.
				
				ASN1Set certSet = null;
				ASN1Set crlSet = null;
				while (content.getObjectAt(next) instanceof ASN1TaggedObject) {
				ASN1TaggedObject tagged = (ASN1TaggedObject)content.getObjectAt(next);
				
				switch (tagged.getTagNo()) {
				case 0:
				certSet = ASN1Set.getInstance(tagged, false);
				break;
				case 1:
				crlSet = ASN1Set.getInstance(tagged, false);
				break;
				default:
				throw new IllegalArgumentException("unknown tag value " + tagged.getTagNo());
				}
				++next;
				}
				certs = new ArrayList<Certificate>(certSet.size());
				
				CertificateFactory certFact = CertificateFactory.getInstance("X.509", new BouncyCastleProvider());
				for (Enumeration en = certSet.getObjects(); en.hasMoreElements();) {
				ASN1Primitive obj = ((ASN1Encodable)en.nextElement()).toASN1Primitive();
				if (obj instanceof ASN1Sequence) {
				ByteArrayInputStream stream = new ByteArrayInputStream(obj.getEncoded());
				X509Certificate x509Certificate = (X509Certificate)certFact.generateCertificate(stream);
				stream.close();
				certs.add(x509Certificate);
				}
				}
				*/
				// the signerInfos
				ASN1Set signerInfos = (ASN1Set)content.GetObjectAt(next);
				if (signerInfos.Size() != 1)
				{
					throw new ArgumentException(PdfException.ThisPkcs7ObjectHasMultipleSignerinfosOnlyOneIsSupportedAtThisTime
						);
				}
				ASN1Sequence signerInfo = (ASN1Sequence)signerInfos.GetObjectAt(0);
				// the positions that we care are
				//     0 - version
				//     1 - the signing certificate issuer and serial number
				//     2 - the digest algorithm
				//     3 or 4 - digestEncryptionAlgorithm
				//     4 or 5 - encryptedDigest
				signerversion = ((ASN1Integer)signerInfo.GetObjectAt(0)).GetValue();
				// Get the signing certificate
				ASN1Sequence issuerAndSerialNumber = (ASN1Sequence)signerInfo.GetObjectAt(1);
				X509Principal issuer = new X509Principal(issuerAndSerialNumber.GetObjectAt(0).ToASN1Primitive
					().GetEncoded());
				BigInteger serialNumber = ((ASN1Integer)issuerAndSerialNumber.GetObjectAt(1)).GetValue
					();
				foreach (Object element in certs)
				{
					X509Certificate cert = (X509Certificate)element;
					if (cert.GetIssuerDN().Equals(issuer) && serialNumber.Equals(cert.GetSerialNumber
						()))
					{
						signCert = cert;
						break;
					}
				}
				if (signCert == null)
				{
					throw new PdfException(PdfException.CantFindSigningCertificateWithSerial1).SetMessageParams
						(issuer.GetName() + " / " + serialNumber.ToString(16));
				}
				SignCertificateChain();
				digestAlgorithmOid = ((ASN1ObjectIdentifier)((ASN1Sequence)signerInfo.GetObjectAt
					(2)).GetObjectAt(0)).GetId();
				next = 3;
				bool foundCades = false;
				if (signerInfo.GetObjectAt(next) is ASN1TaggedObject)
				{
					ASN1TaggedObject tagsig = (ASN1TaggedObject)signerInfo.GetObjectAt(next);
					ASN1Set sseq = ASN1Set.GetInstance(tagsig, false);
					sigAttr = sseq.GetEncoded();
					// maybe not necessary, but we use the following line as fallback:
					sigAttrDer = sseq.GetEncoded(ASN1Encoding.DER);
					for (int k = 0; k < sseq.Size(); ++k)
					{
						ASN1Sequence seq2 = (ASN1Sequence)sseq.GetObjectAt(k);
						String idSeq2 = ((ASN1ObjectIdentifier)seq2.GetObjectAt(0)).GetId();
						if (idSeq2.Equals(SecurityIDs.ID_MESSAGE_DIGEST))
						{
							ASN1Set set = (ASN1Set)seq2.GetObjectAt(1);
							digestAttr = ((ASN1OctetString)set.GetObjectAt(0)).GetOctets();
						}
						else
						{
							if (idSeq2.Equals(SecurityIDs.ID_ADBE_REVOCATION))
							{
								ASN1Set setout = (ASN1Set)seq2.GetObjectAt(1);
								ASN1Sequence seqout = (ASN1Sequence)setout.GetObjectAt(0);
								for (int j = 0; j < seqout.Size(); ++j)
								{
									ASN1TaggedObject tg = (ASN1TaggedObject)seqout.GetObjectAt(j);
									if (tg.GetTagNo() == 0)
									{
										ASN1Sequence seqin = (ASN1Sequence)tg.GetObject();
										FindCRL(seqin);
									}
									if (tg.GetTagNo() == 1)
									{
										ASN1Sequence seqin = (ASN1Sequence)tg.GetObject();
										FindOcsp(seqin);
									}
								}
							}
							else
							{
								if (isCades && idSeq2.Equals(SecurityIDs.ID_AA_SIGNING_CERTIFICATE_V1))
								{
									ASN1Set setout = (ASN1Set)seq2.GetObjectAt(1);
									ASN1Sequence seqout = (ASN1Sequence)setout.GetObjectAt(0);
									SigningCertificate sv2 = SigningCertificate.GetInstance(seqout);
									ESSCertID[] cerv2m = sv2.GetCerts();
									ESSCertID cerv2 = cerv2m[0];
									byte[] enc2 = signCert.GetEncoded();
									MessageDigest m2 = new BouncyCastleDigest().GetMessageDigest("SHA-1");
									byte[] signCertHash = m2.Digest(enc2);
									byte[] hs2 = cerv2.GetCertHash();
									if (!com.itextpdf.io.util.JavaUtil.ArraysEquals(signCertHash, hs2))
									{
										throw new ArgumentException("Signing certificate doesn't match the ESS information."
											);
									}
									foundCades = true;
								}
								else
								{
									if (isCades && idSeq2.Equals(SecurityIDs.ID_AA_SIGNING_CERTIFICATE_V2))
									{
										ASN1Set setout = (ASN1Set)seq2.GetObjectAt(1);
										ASN1Sequence seqout = (ASN1Sequence)setout.GetObjectAt(0);
										SigningCertificateV2 sv2 = SigningCertificateV2.GetInstance(seqout);
										ESSCertIDv2[] cerv2m = sv2.GetCerts();
										ESSCertIDv2 cerv2 = cerv2m[0];
										AlgorithmIdentifier ai2 = cerv2.GetHashAlgorithm();
										byte[] enc2 = signCert.GetEncoded();
										MessageDigest m2 = new BouncyCastleDigest().GetMessageDigest(DigestAlgorithms.GetDigest
											(ai2.GetAlgorithm().GetId()));
										byte[] signCertHash = m2.Digest(enc2);
										byte[] hs2 = cerv2.GetCertHash();
										if (!com.itextpdf.io.util.JavaUtil.ArraysEquals(signCertHash, hs2))
										{
											throw new ArgumentException("Signing certificate doesn't match the ESS information."
												);
										}
										foundCades = true;
									}
								}
							}
						}
					}
					if (digestAttr == null)
					{
						throw new ArgumentException(PdfException.AuthenticatedAttributeIsMissingTheDigest
							);
					}
					++next;
				}
				if (isCades && !foundCades)
				{
					throw new ArgumentException("CAdES ESS information missing.");
				}
				digestEncryptionAlgorithmOid = ((ASN1ObjectIdentifier)((ASN1Sequence)signerInfo.GetObjectAt
					(next++)).GetObjectAt(0)).GetId();
				digest = ((ASN1OctetString)signerInfo.GetObjectAt(next++)).GetOctets();
				if (next < signerInfo.Size() && signerInfo.GetObjectAt(next) is ASN1TaggedObject)
				{
					ASN1TaggedObject taggedObject = (ASN1TaggedObject)signerInfo.GetObjectAt(next);
					ASN1Set unat = ASN1Set.GetInstance(taggedObject, false);
					AttributeTable attble = new AttributeTable(unat);
					Attribute ts = attble.Get(PKCSObjectIdentifiers.id_aa_signatureTimeStampToken);
					if (ts != null && ts.GetAttrValues().Size() > 0)
					{
						ASN1Set attributeValues = ts.GetAttrValues();
						ASN1Sequence tokenSequence = ASN1Sequence.GetInstance(attributeValues.GetObjectAt
							(0));
						ContentInfo contentInfo = new ContentInfo(tokenSequence);
						this.timeStampToken = new TimeStampToken(contentInfo);
					}
				}
				if (isTsp)
				{
					ContentInfo contentInfoTsp = new ContentInfo(signedData);
					this.timeStampToken = new TimeStampToken(contentInfoTsp);
					TimeStampTokenInfo info = timeStampToken.GetTimeStampInfo();
					String algOID = info.GetMessageImprintAlgOID().GetId();
					messageDigest = DigestAlgorithms.GetMessageDigestFromOid(algOID, null);
				}
				else
				{
					if (RSAdata != null || digestAttr != null)
					{
						if (PdfName.Adbe_pkcs7_sha1.Equals(GetFilterSubtype()))
						{
							messageDigest = DigestAlgorithms.GetMessageDigest("SHA1", provider);
						}
						else
						{
							messageDigest = DigestAlgorithms.GetMessageDigest(GetHashAlgorithm(), provider);
						}
						encContDigest = DigestAlgorithms.GetMessageDigest(GetHashAlgorithm(), provider);
					}
					sig = InitSignature(signCert.GetPublicKey());
				}
			}
			catch (Exception e)
			{
				throw new PdfException(e);
			}
		}

		/// <summary>The encryption provider, e.g.</summary>
		/// <remarks>The encryption provider, e.g. "BC" if you use BouncyCastle.</remarks>
		private String provider;

		/// <summary>Holds value of property signName.</summary>
		private String signName;

		/// <summary>Holds value of property reason.</summary>
		private String reason;

		/// <summary>Holds value of property location.</summary>
		private String location;

		/// <summary>Holds value of property signDate.</summary>
		private Calendar signDate;

		// Encryption provider
		// Signature info
		/// <summary>Getter for property sigName.</summary>
		/// <returns>Value of property sigName.</returns>
		public virtual String GetSignName()
		{
			return this.signName;
		}

		/// <summary>Setter for property sigName.</summary>
		/// <param name="signName">New value of property sigName.</param>
		public virtual void SetSignName(String signName)
		{
			this.signName = signName;
		}

		/// <summary>Getter for property reason.</summary>
		/// <returns>Value of property reason.</returns>
		public virtual String GetReason()
		{
			return this.reason;
		}

		/// <summary>Setter for property reason.</summary>
		/// <param name="reason">New value of property reason.</param>
		public virtual void SetReason(String reason)
		{
			this.reason = reason;
		}

		/// <summary>Getter for property location.</summary>
		/// <returns>Value of property location.</returns>
		public virtual String GetLocation()
		{
			return this.location;
		}

		/// <summary>Setter for property location.</summary>
		/// <param name="location">New value of property location.</param>
		public virtual void SetLocation(String location)
		{
			this.location = location;
		}

		/// <summary>Getter for property signDate.</summary>
		/// <returns>Value of property signDate.</returns>
		public virtual Calendar GetSignDate()
		{
			Calendar dt = GetTimeStampDate();
			if (dt == null)
			{
				return this.signDate;
			}
			else
			{
				return dt;
			}
		}

		/// <summary>Setter for property signDate.</summary>
		/// <param name="signDate">New value of property signDate.</param>
		public virtual void SetSignDate(Calendar signDate)
		{
			this.signDate = signDate;
		}

		/// <summary>Version of the PKCS#7 object</summary>
		private int version = 1;

		/// <summary>Version of the PKCS#7 "SignerInfo" object.</summary>
		private int signerversion = 1;

		// version info
		/// <summary>Get the version of the PKCS#7 object.</summary>
		/// <returns>the version of the PKCS#7 object.</returns>
		public virtual int GetVersion()
		{
			return version;
		}

		/// <summary>Get the version of the PKCS#7 "SignerInfo" object.</summary>
		/// <returns>the version of the PKCS#7 "SignerInfo" object.</returns>
		public virtual int GetSigningInfoVersion()
		{
			return signerversion;
		}

		/// <summary>The ID of the digest algorithm, e.g.</summary>
		/// <remarks>The ID of the digest algorithm, e.g. "2.16.840.1.101.3.4.2.1".</remarks>
		private String digestAlgorithmOid;

		/// <summary>The object that will create the digest</summary>
		private MessageDigest messageDigest;

		/// <summary>The digest algorithms</summary>
		private ICollection<String> digestalgos;

		/// <summary>The digest attributes</summary>
		private byte[] digestAttr;

		private PdfName filterSubtype;

		// Message digest algorithm
		/// <summary>Getter for the ID of the digest algorithm, e.g.</summary>
		/// <remarks>Getter for the ID of the digest algorithm, e.g. "2.16.840.1.101.3.4.2.1"
		/// 	</remarks>
		public virtual String GetDigestAlgorithmOid()
		{
			return digestAlgorithmOid;
		}

		/// <summary>Returns the name of the digest algorithm, e.g.</summary>
		/// <remarks>Returns the name of the digest algorithm, e.g. "SHA256".</remarks>
		/// <returns>the digest algorithm name, e.g. "SHA256"</returns>
		public virtual String GetHashAlgorithm()
		{
			return DigestAlgorithms.GetDigest(digestAlgorithmOid);
		}

		/// <summary>The encryption algorithm.</summary>
		private String digestEncryptionAlgorithmOid;

		// Encryption algorithm
		/// <summary>Getter for the digest encryption algorithm</summary>
		public virtual String GetDigestEncryptionAlgorithmOid()
		{
			return digestEncryptionAlgorithmOid;
		}

		/// <summary>Get the algorithm used to calculate the message digest, e.g.</summary>
		/// <remarks>Get the algorithm used to calculate the message digest, e.g. "SHA1withRSA".
		/// 	</remarks>
		/// <returns>the algorithm used to calculate the message digest</returns>
		public virtual String GetDigestAlgorithm()
		{
			return GetHashAlgorithm() + "with" + GetEncryptionAlgorithm();
		}

		private IExternalDigest interfaceDigest;

		/// <summary>The signed digest if created outside this class</summary>
		private byte[] externalDigest;

		/// <summary>External RSA data</summary>
		private byte[] externalRSAdata;

		/*
		*	DIGITAL SIGNATURE CREATION
		*/
		// The signature is created externally
		/// <summary>Sets the digest/signature to an external calculated value.</summary>
		/// <param name="digest">the digest. This is the actual signature</param>
		/// <param name="RSAdata">the extra data that goes into the data tag in PKCS#7</param>
		/// <param name="digestEncryptionAlgorithm">
		/// the encryption algorithm. It may must be <CODE>null</CODE> if the <CODE>digest</CODE>
		/// is also <CODE>null</CODE>. If the <CODE>digest</CODE> is not <CODE>null</CODE>
		/// then it may be "RSA" or "DSA"
		/// </param>
		public virtual void SetExternalDigest(byte[] digest, byte[] RSAdata, String digestEncryptionAlgorithm
			)
		{
			externalDigest = digest;
			externalRSAdata = RSAdata;
			if (digestEncryptionAlgorithm != null)
			{
				if (digestEncryptionAlgorithm.Equals("RSA"))
				{
					this.digestEncryptionAlgorithmOid = SecurityIDs.ID_RSA;
				}
				else
				{
					if (digestEncryptionAlgorithm.Equals("DSA"))
					{
						this.digestEncryptionAlgorithmOid = SecurityIDs.ID_DSA;
					}
					else
					{
						if (digestEncryptionAlgorithm.Equals("ECDSA"))
						{
							this.digestEncryptionAlgorithmOid = SecurityIDs.ID_ECDSA;
						}
						else
						{
							throw new PdfException(PdfException.UnknownKeyAlgorithm1).SetMessageParams(digestEncryptionAlgorithm
								);
						}
					}
				}
			}
		}

		/// <summary>Class from the Java SDK that provides the functionality of a digital signature algorithm.
		/// 	</summary>
		private Signature sig;

		/// <summary>The signed digest as calculated by this class (or extracted from an existing PDF)
		/// 	</summary>
		private byte[] digest;

		/// <summary>The RSA data</summary>
		private byte[] RSAdata;

		// The signature is created internally
		// Signing functionality.
		/// <exception cref="java.security.NoSuchAlgorithmException"/>
		/// <exception cref="java.security.NoSuchProviderException"/>
		/// <exception cref="java.security.InvalidKeyException"/>
		private Signature InitSignature(PrivateKey key)
		{
			Signature signature;
			if (provider == null)
			{
				signature = Signature.GetInstance(GetDigestAlgorithm());
			}
			else
			{
				signature = Signature.GetInstance(GetDigestAlgorithm(), provider);
			}
			signature.InitSign(key);
			return signature;
		}

		/// <exception cref="java.security.NoSuchAlgorithmException"/>
		/// <exception cref="java.security.NoSuchProviderException"/>
		/// <exception cref="java.security.InvalidKeyException"/>
		private Signature InitSignature(PublicKey key)
		{
			String digestAlgorithm = GetDigestAlgorithm();
			if (PdfName.Adbe_x509_rsa_sha1.Equals(GetFilterSubtype()))
			{
				digestAlgorithm = "SHA1withRSA";
			}
			Signature signature;
			if (provider == null)
			{
				signature = Signature.GetInstance(digestAlgorithm);
			}
			else
			{
				signature = Signature.GetInstance(digestAlgorithm, provider);
			}
			signature.InitVerify(key);
			return signature;
		}

		/// <summary>Update the digest with the specified bytes.</summary>
		/// <remarks>
		/// Update the digest with the specified bytes.
		/// This method is used both for signing and verifying
		/// </remarks>
		/// <param name="buf">the data buffer</param>
		/// <param name="off">the offset in the data buffer</param>
		/// <param name="len">the data length</param>
		/// <exception cref="java.security.SignatureException">on error</exception>
		public virtual void Update(byte[] buf, int off, int len)
		{
			if (RSAdata != null || digestAttr != null || isTsp)
			{
				messageDigest.Update(buf, off, len);
			}
			else
			{
				sig.Update(buf, off, len);
			}
		}

		// adbe.x509.rsa_sha1 (PKCS#1)
		/// <summary>Gets the bytes for the PKCS#1 object.</summary>
		/// <returns>a byte array</returns>
		public virtual byte[] GetEncodedPKCS1()
		{
			try
			{
				if (externalDigest != null)
				{
					digest = externalDigest;
				}
				else
				{
					digest = sig.Sign();
				}
				MemoryStream bOut = new MemoryStream();
				ASN1OutputStream dout = new ASN1OutputStream(bOut);
				dout.WriteObject(new DEROctetString(digest));
				dout.Close();
				return bOut.ToArray();
			}
			catch (Exception e)
			{
				throw new PdfException(e);
			}
		}

		// other subfilters (PKCS#7)
		/// <summary>Gets the bytes for the PKCS7SignedData object.</summary>
		/// <returns>the bytes for the PKCS7SignedData object</returns>
		public virtual byte[] GetEncodedPKCS7()
		{
			return GetEncodedPKCS7(null, null, null, null, PdfSigner.CryptoStandard.CMS);
		}

		/// <summary>Gets the bytes for the PKCS7SignedData object.</summary>
		/// <remarks>
		/// Gets the bytes for the PKCS7SignedData object. Optionally the authenticatedAttributes
		/// in the signerInfo can also be set. If either of the parameters is <CODE>null</CODE>, none will be used.
		/// </remarks>
		/// <param name="secondDigest">the digest in the authenticatedAttributes</param>
		/// <returns>the bytes for the PKCS7SignedData object</returns>
		public virtual byte[] GetEncodedPKCS7(byte[] secondDigest)
		{
			return GetEncodedPKCS7(secondDigest, null, null, null, PdfSigner.CryptoStandard.CMS
				);
		}

		/// <summary>Gets the bytes for the PKCS7SignedData object.</summary>
		/// <remarks>
		/// Gets the bytes for the PKCS7SignedData object. Optionally the authenticatedAttributes
		/// in the signerInfo can also be set, OR a time-stamp-authority client
		/// may be provided.
		/// </remarks>
		/// <param name="secondDigest">the digest in the authenticatedAttributes</param>
		/// <param name="tsaClient">TSAClient - null or an optional time stamp authority client
		/// 	</param>
		/// <returns>byte[] the bytes for the PKCS7SignedData object</returns>
		public virtual byte[] GetEncodedPKCS7(byte[] secondDigest, ITSAClient tsaClient, 
			byte[] ocsp, ICollection<byte[]> crlBytes, PdfSigner.CryptoStandard sigtype)
		{
			try
			{
				if (externalDigest != null)
				{
					digest = externalDigest;
					if (RSAdata != null)
					{
						RSAdata = externalRSAdata;
					}
				}
				else
				{
					if (externalRSAdata != null && RSAdata != null)
					{
						RSAdata = externalRSAdata;
						sig.Update(RSAdata);
						digest = sig.Sign();
					}
					else
					{
						if (RSAdata != null)
						{
							RSAdata = messageDigest.Digest();
							sig.Update(RSAdata);
						}
						digest = sig.Sign();
					}
				}
				// Create the set of Hash algorithms
				ASN1EncodableVector digestAlgorithms = new ASN1EncodableVector();
				foreach (Object element in digestalgos)
				{
					ASN1EncodableVector algos = new ASN1EncodableVector();
					algos.Add(new ASN1ObjectIdentifier((String)element));
					algos.Add(DERNull.INSTANCE);
					digestAlgorithms.Add(new DERSequence(algos));
				}
				// Create the contentInfo.
				ASN1EncodableVector v = new ASN1EncodableVector();
				v.Add(new ASN1ObjectIdentifier(SecurityIDs.ID_PKCS7_DATA));
				if (RSAdata != null)
				{
					v.Add(new DERTaggedObject(0, new DEROctetString(RSAdata)));
				}
				DERSequence contentinfo = new DERSequence(v);
				// Get all the certificates
				//
				v = new ASN1EncodableVector();
				foreach (Object element_1 in certs)
				{
					ASN1InputStream tempstream = new ASN1InputStream(new MemoryStream(((X509Certificate
						)element_1).GetEncoded()));
					v.Add(tempstream.ReadObject());
				}
				DERSet dercertificates = new DERSet(v);
				// Create signerinfo structure.
				//
				ASN1EncodableVector signerinfo = new ASN1EncodableVector();
				// Add the signerInfo version
				//
				signerinfo.Add(new ASN1Integer(signerversion));
				v = new ASN1EncodableVector();
				v.Add(CertificateInfo.GetIssuer(signCert.GetTBSCertificate()));
				v.Add(new ASN1Integer(signCert.GetSerialNumber()));
				signerinfo.Add(new DERSequence(v));
				// Add the digestAlgorithm
				v = new ASN1EncodableVector();
				v.Add(new ASN1ObjectIdentifier(digestAlgorithmOid));
				v.Add(new DERNull());
				signerinfo.Add(new DERSequence(v));
				// add the authenticated attribute if present
				if (secondDigest != null)
				{
					signerinfo.Add(new DERTaggedObject(false, 0, GetAuthenticatedAttributeSet(secondDigest
						, ocsp, crlBytes, sigtype)));
				}
				// Add the digestEncryptionAlgorithm
				v = new ASN1EncodableVector();
				v.Add(new ASN1ObjectIdentifier(digestEncryptionAlgorithmOid));
				v.Add(new DERNull());
				signerinfo.Add(new DERSequence(v));
				// Add the digest
				signerinfo.Add(new DEROctetString(digest));
				// When requested, go get and add the timestamp. May throw an exception.
				// Added by Martin Brunecky, 07/12/2007 folowing Aiken Sam, 2006-11-15
				// Sam found Adobe expects time-stamped SHA1-1 of the encrypted digest
				if (tsaClient != null)
				{
					byte[] tsImprint = tsaClient.GetMessageDigest().Digest(digest);
					byte[] tsToken = tsaClient.GetTimeStampToken(tsImprint);
					if (tsToken != null)
					{
						ASN1EncodableVector unauthAttributes = BuildUnauthenticatedAttributes(tsToken);
						if (unauthAttributes != null)
						{
							signerinfo.Add(new DERTaggedObject(false, 1, new DERSet(unauthAttributes)));
						}
					}
				}
				// Finally build the body out of all the components above
				ASN1EncodableVector body = new ASN1EncodableVector();
				body.Add(new ASN1Integer(version));
				body.Add(new DERSet(digestAlgorithms));
				body.Add(contentinfo);
				body.Add(new DERTaggedObject(false, 0, dercertificates));
				// Only allow one signerInfo
				body.Add(new DERSet(new DERSequence(signerinfo)));
				// Now we have the body, wrap it in it's PKCS7Signed shell
				// and return it
				//
				ASN1EncodableVector whole = new ASN1EncodableVector();
				whole.Add(new ASN1ObjectIdentifier(SecurityIDs.ID_PKCS7_SIGNED_DATA));
				whole.Add(new DERTaggedObject(0, new DERSequence(body)));
				MemoryStream bOut = new MemoryStream();
				ASN1OutputStream dout = new ASN1OutputStream(bOut);
				dout.WriteObject(new DERSequence(whole));
				dout.Close();
				return bOut.ToArray();
			}
			catch (Exception e)
			{
				throw new PdfException(e);
			}
		}

		/// <summary>
		/// Added by Aiken Sam, 2006-11-15, modifed by Martin Brunecky 07/12/2007
		/// to start with the timeStampToken (signedData 1.2.840.113549.1.7.2).
		/// </summary>
		/// <remarks>
		/// Added by Aiken Sam, 2006-11-15, modifed by Martin Brunecky 07/12/2007
		/// to start with the timeStampToken (signedData 1.2.840.113549.1.7.2).
		/// Token is the TSA response without response status, which is usually
		/// handled by the (vendor supplied) TSA request/response interface).
		/// </remarks>
		/// <param name="timeStampToken">byte[] - time stamp token, DER encoded signedData</param>
		/// <returns>ASN1EncodableVector</returns>
		/// <exception cref="System.IO.IOException"/>
		private ASN1EncodableVector BuildUnauthenticatedAttributes(byte[] timeStampToken)
		{
			if (timeStampToken == null)
			{
				return null;
			}
			// @todo: move this together with the rest of the defintions
			String ID_TIME_STAMP_TOKEN = "1.2.840.113549.1.9.16.2.14";
			// RFC 3161 id-aa-timeStampToken
			ASN1InputStream tempstream = new ASN1InputStream(new MemoryStream(timeStampToken)
				);
			ASN1EncodableVector unauthAttributes = new ASN1EncodableVector();
			ASN1EncodableVector v = new ASN1EncodableVector();
			v.Add(new ASN1ObjectIdentifier(ID_TIME_STAMP_TOKEN));
			// id-aa-timeStampToken
			ASN1Sequence seq = (ASN1Sequence)tempstream.ReadObject();
			v.Add(new DERSet(seq));
			unauthAttributes.Add(new DERSequence(v));
			return unauthAttributes;
		}

		// Authenticated attributes
		/// <summary>When using authenticatedAttributes the authentication process is different.
		/// 	</summary>
		/// <remarks>
		/// When using authenticatedAttributes the authentication process is different.
		/// The document digest is generated and put inside the attribute. The signing is done over the DER encoded
		/// authenticatedAttributes. This method provides that encoding and the parameters must be
		/// exactly the same as in
		/// <see cref="GetEncodedPKCS7(byte[])"/>
		/// .
		/// <p>
		/// A simple example:
		/// <p>
		/// <pre>
		/// Calendar cal = Calendar.getInstance();
		/// PdfPKCS7 pk7 = new PdfPKCS7(key, chain, null, "SHA1", null, false);
		/// MessageDigest messageDigest = MessageDigest.getInstance("SHA1");
		/// byte[] buf = new byte[8192];
		/// int n;
		/// InputStream inp = sap.getRangeStream();
		/// while ((n = inp.read(buf)) &gt; 0) {
		/// messageDigest.update(buf, 0, n);
		/// }
		/// byte[] hash = messageDigest.digest();
		/// byte[] sh = pk7.getAuthenticatedAttributeBytes(hash, cal);
		/// pk7.update(sh, 0, sh.length);
		/// byte[] sg = pk7.getEncodedPKCS7(hash, cal);
		/// </pre>
		/// </remarks>
		/// <param name="secondDigest">the content digest</param>
		/// <returns>the byte array representation of the authenticatedAttributes ready to be signed
		/// 	</returns>
		public virtual byte[] GetAuthenticatedAttributeBytes(byte[] secondDigest, byte[] 
			ocsp, ICollection<byte[]> crlBytes, PdfSigner.CryptoStandard sigtype)
		{
			try
			{
				return GetAuthenticatedAttributeSet(secondDigest, ocsp, crlBytes, sigtype).GetEncoded
					(ASN1Encoding.DER);
			}
			catch (Exception e)
			{
				throw new PdfException(e);
			}
		}

		/// <summary>
		/// This method provides that encoding and the parameters must be
		/// exactly the same as in
		/// <see cref="GetEncodedPKCS7(byte[])"/>
		/// .
		/// </summary>
		/// <param name="secondDigest">the content digest</param>
		/// <returns>the byte array representation of the authenticatedAttributes ready to be signed
		/// 	</returns>
		private DERSet GetAuthenticatedAttributeSet(byte[] secondDigest, byte[] ocsp, ICollection
			<byte[]> crlBytes, PdfSigner.CryptoStandard sigtype)
		{
			try
			{
				ASN1EncodableVector attribute = new ASN1EncodableVector();
				ASN1EncodableVector v = new ASN1EncodableVector();
				v.Add(new ASN1ObjectIdentifier(SecurityIDs.ID_CONTENT_TYPE));
				v.Add(new DERSet(new ASN1ObjectIdentifier(SecurityIDs.ID_PKCS7_DATA)));
				attribute.Add(new DERSequence(v));
				v = new ASN1EncodableVector();
				v.Add(new ASN1ObjectIdentifier(SecurityIDs.ID_MESSAGE_DIGEST));
				v.Add(new DERSet(new DEROctetString(secondDigest)));
				attribute.Add(new DERSequence(v));
				bool haveCrl = false;
				if (crlBytes != null)
				{
					foreach (byte[] bCrl in crlBytes)
					{
						if (bCrl != null)
						{
							haveCrl = true;
							break;
						}
					}
				}
				if (ocsp != null || haveCrl)
				{
					v = new ASN1EncodableVector();
					v.Add(new ASN1ObjectIdentifier(SecurityIDs.ID_ADBE_REVOCATION));
					ASN1EncodableVector revocationV = new ASN1EncodableVector();
					if (haveCrl)
					{
						ASN1EncodableVector v2 = new ASN1EncodableVector();
						foreach (byte[] bCrl in crlBytes)
						{
							if (bCrl == null)
							{
								continue;
							}
							ASN1InputStream t = new ASN1InputStream(new MemoryStream(bCrl));
							v2.Add(t.ReadObject());
						}
						revocationV.Add(new DERTaggedObject(true, 0, new DERSequence(v2)));
					}
					if (ocsp != null)
					{
						DEROctetString doctet = new DEROctetString(ocsp);
						ASN1EncodableVector vo1 = new ASN1EncodableVector();
						ASN1EncodableVector v2 = new ASN1EncodableVector();
						v2.Add(OCSPObjectIdentifiers.id_pkix_ocsp_basic);
						v2.Add(doctet);
						ASN1Enumerated den = new ASN1Enumerated(0);
						ASN1EncodableVector v3 = new ASN1EncodableVector();
						v3.Add(den);
						v3.Add(new DERTaggedObject(true, 0, new DERSequence(v2)));
						vo1.Add(new DERSequence(v3));
						revocationV.Add(new DERTaggedObject(true, 1, new DERSequence(vo1)));
					}
					v.Add(new DERSet(new DERSequence(revocationV)));
					attribute.Add(new DERSequence(v));
				}
				if (sigtype == PdfSigner.CryptoStandard.CADES)
				{
					v = new ASN1EncodableVector();
					v.Add(new ASN1ObjectIdentifier(SecurityIDs.ID_AA_SIGNING_CERTIFICATE_V2));
					ASN1EncodableVector aaV2 = new ASN1EncodableVector();
					AlgorithmIdentifier algoId = new AlgorithmIdentifier(new ASN1ObjectIdentifier(digestAlgorithmOid
						), null);
					aaV2.Add(algoId);
					MessageDigest md = interfaceDigest.GetMessageDigest(GetHashAlgorithm());
					byte[] dig = md.Digest(signCert.GetEncoded());
					aaV2.Add(new DEROctetString(dig));
					v.Add(new DERSet(new DERSequence(new DERSequence(new DERSequence(aaV2)))));
					attribute.Add(new DERSequence(v));
				}
				return new DERSet(attribute);
			}
			catch (Exception e)
			{
				throw new PdfException(e);
			}
		}

		/// <summary>Signature attributes</summary>
		private byte[] sigAttr;

		/// <summary>Signature attributes (maybe not necessary, but we use it as fallback)</summary>
		private byte[] sigAttrDer;

		/// <summary>encrypted digest</summary>
		private MessageDigest encContDigest;

		/// <summary>Indicates if a signature has already been verified</summary>
		private bool verified;

		/// <summary>The result of the verification</summary>
		private bool verifyResult;

		/*
		*	DIGITAL SIGNATURE VERIFICATION
		*/
		// Stefan Santesson
		// verification
		/// <summary>Verify the digest.</summary>
		/// <exception cref="java.security.SignatureException">on error</exception>
		/// <returns><CODE>true</CODE> if the signature checks out, <CODE>false</CODE> otherwise
		/// 	</returns>
		/// <exception cref="java.security.GeneralSecurityException"/>
		public virtual bool Verify()
		{
			if (verified)
			{
				return verifyResult;
			}
			if (isTsp)
			{
				TimeStampTokenInfo info = timeStampToken.GetTimeStampInfo();
				MessageImprint imprint = info.ToASN1Structure().GetMessageImprint();
				byte[] md = messageDigest.Digest();
				byte[] imphashed = imprint.GetHashedMessage();
				verifyResult = com.itextpdf.io.util.JavaUtil.ArraysEquals(md, imphashed);
			}
			else
			{
				if (sigAttr != null || sigAttrDer != null)
				{
					byte[] msgDigestBytes = messageDigest.Digest();
					bool verifyRSAdata = true;
					// Stefan Santesson fixed a bug, keeping the code backward compatible
					bool encContDigestCompare = false;
					if (RSAdata != null)
					{
						verifyRSAdata = com.itextpdf.io.util.JavaUtil.ArraysEquals(msgDigestBytes, RSAdata
							);
						encContDigest.Update(RSAdata);
						encContDigestCompare = com.itextpdf.io.util.JavaUtil.ArraysEquals(encContDigest.Digest
							(), digestAttr);
					}
					bool absentEncContDigestCompare = com.itextpdf.io.util.JavaUtil.ArraysEquals(msgDigestBytes
						, digestAttr);
					bool concludingDigestCompare = absentEncContDigestCompare || encContDigestCompare;
					bool sigVerify = VerifySigAttributes(sigAttr) || VerifySigAttributes(sigAttrDer);
					verifyResult = concludingDigestCompare && sigVerify && verifyRSAdata;
				}
				else
				{
					if (RSAdata != null)
					{
						sig.Update(messageDigest.Digest());
					}
					verifyResult = sig.Verify(digest);
				}
			}
			verified = true;
			return verifyResult;
		}

		/// <exception cref="java.security.GeneralSecurityException"/>
		private bool VerifySigAttributes(byte[] attr)
		{
			Signature signature = InitSignature(signCert.GetPublicKey());
			signature.Update(attr);
			return signature.Verify(digest);
		}

		/// <summary>Checks if the timestamp refers to this document.</summary>
		/// <returns>true if it checks false otherwise</returns>
		/// <exception cref="java.security.GeneralSecurityException">on error</exception>
		public virtual bool VerifyTimestampImprint()
		{
			if (timeStampToken == null)
			{
				return false;
			}
			TimeStampTokenInfo info = timeStampToken.GetTimeStampInfo();
			MessageImprint imprint = info.ToASN1Structure().GetMessageImprint();
			String algOID = info.GetMessageImprintAlgOID().GetId();
			byte[] md = new BouncyCastleDigest().GetMessageDigest(DigestAlgorithms.GetDigest(
				algOID)).Digest(digest);
			byte[] imphashed = imprint.GetHashedMessage();
			bool res = com.itextpdf.io.util.JavaUtil.ArraysEquals(md, imphashed);
			return res;
		}

		/// <summary>All the X.509 certificates in no particular order.</summary>
		private ICollection<Certificate> certs;

		/// <summary>All the X.509 certificates used for the main signature.</summary>
		private ICollection<Certificate> signCerts;

		/// <summary>The X.509 certificate that is used to sign the digest.</summary>
		private X509Certificate signCert;

		// Certificates
		/// <summary>Get all the X.509 certificates associated with this PKCS#7 object in no particular order.
		/// 	</summary>
		/// <remarks>
		/// Get all the X.509 certificates associated with this PKCS#7 object in no particular order.
		/// Other certificates, from OCSP for example, will also be included.
		/// </remarks>
		/// <returns>the X.509 certificates associated with this PKCS#7 object</returns>
		public virtual Certificate[] GetCertificates()
		{
			return certs.ToArray(new X509Certificate[certs.Count]);
		}

		/// <summary>Get the X.509 sign certificate chain associated with this PKCS#7 object.
		/// 	</summary>
		/// <remarks>
		/// Get the X.509 sign certificate chain associated with this PKCS#7 object.
		/// Only the certificates used for the main signature will be returned, with
		/// the signing certificate first.
		/// </remarks>
		/// <returns>the X.509 certificates associated with this PKCS#7 object</returns>
		public virtual Certificate[] GetSignCertificateChain()
		{
			return signCerts.ToArray(new X509Certificate[signCerts.Count]);
		}

		/// <summary>Get the X.509 certificate actually used to sign the digest.</summary>
		/// <returns>the X.509 certificate actually used to sign the digest</returns>
		public virtual X509Certificate GetSigningCertificate()
		{
			return signCert;
		}

		/// <summary>
		/// Helper method that creates the collection of certificates
		/// used for the main signature based on the complete list
		/// of certificates and the sign certificate.
		/// </summary>
		private void SignCertificateChain()
		{
			IList<Certificate> cc = new List<Certificate>();
			cc.Add(signCert);
			IList<Certificate> oc = new List<Certificate>(certs);
			for (int k = 0; k < oc.Count; ++k)
			{
				if (signCert.Equals(oc[k]))
				{
					oc.RemoveAt(k);
					--k;
					continue;
				}
			}
			bool found = true;
			while (found)
			{
				X509Certificate v = (X509Certificate)cc[cc.Count - 1];
				found = false;
				for (int k_1 = 0; k_1 < oc.Count; ++k_1)
				{
					X509Certificate issuer = (X509Certificate)oc[k_1];
					try
					{
						if (provider == null)
						{
							v.Verify(issuer.GetPublicKey());
						}
						else
						{
							v.Verify(issuer.GetPublicKey(), provider);
						}
						found = true;
						cc.Add(oc[k_1]);
						oc.RemoveAt(k_1);
						break;
					}
					catch (Exception)
					{
					}
				}
			}
			signCerts = cc;
		}

		private ICollection<CRL> crls;

		// Certificate Revocation Lists
		/// <summary>Get the X.509 certificate revocation lists associated with this PKCS#7 object
		/// 	</summary>
		/// <returns>the X.509 certificate revocation lists associated with this PKCS#7 object
		/// 	</returns>
		public virtual ICollection<CRL> GetCRLs()
		{
			return crls;
		}

		/// <summary>Helper method that tries to construct the CRLs.</summary>
		private void FindCRL(ASN1Sequence seq)
		{
			try
			{
				crls = new List<CRL>();
				for (int k = 0; k < seq.Size(); ++k)
				{
					MemoryStream ar = new MemoryStream(seq.GetObjectAt(k).ToASN1Primitive().GetEncoded
						(ASN1Encoding.DER));
					CertificateFactory cf = CertificateFactory.GetInstance("X.509");
					X509CRL crl = (X509CRL)cf.GenerateCRL(ar);
					crls.Add(crl);
				}
			}
			catch (Exception)
			{
			}
		}

		/// <summary>BouncyCastle BasicOCSPResp</summary>
		private BasicOCSPResp basicResp;

		// ignore
		// Online Certificate Status Protocol
		/// <summary>Gets the OCSP basic response if there is one.</summary>
		/// <returns>the OCSP basic response or null</returns>
		public virtual BasicOCSPResp GetOcsp()
		{
			return basicResp;
		}

		/// <summary>Checks if OCSP revocation refers to the document signing certificate.</summary>
		/// <returns>true if it checks, false otherwise</returns>
		public virtual bool IsRevocationValid()
		{
			if (basicResp == null)
			{
				return false;
			}
			if (signCerts.Count < 2)
			{
				return false;
			}
			try
			{
				X509Certificate[] cs = (X509Certificate[])GetSignCertificateChain();
				SingleResp sr = basicResp.GetResponses()[0];
				CertificateID cid = sr.GetCertID();
				DigestCalculator digestalg = new JcaDigestCalculatorProviderBuilder().Build().Get
					(new AlgorithmIdentifier(cid.GetHashAlgOID(), DERNull.INSTANCE));
				X509Certificate sigcer = GetSigningCertificate();
				X509Certificate isscer = cs[1];
				CertificateID tis = new CertificateID(digestalg, new JcaX509CertificateHolder(isscer
					), sigcer.GetSerialNumber());
				return tis.Equals(cid);
			}
			catch (Exception)
			{
			}
			return false;
		}

		/// <summary>Helper method that creates the BasicOCSPResp object.</summary>
		/// <param name="seq"/>
		/// <exception cref="System.IO.IOException"/>
		private void FindOcsp(ASN1Sequence seq)
		{
			basicResp = (BasicOCSPResp)null;
			bool ret = false;
			while (true)
			{
				if (seq.GetObjectAt(0) is ASN1ObjectIdentifier && ((ASN1ObjectIdentifier)seq.GetObjectAt
					(0)).GetId().Equals(OCSPObjectIdentifiers.id_pkix_ocsp_basic.GetId()))
				{
					break;
				}
				ret = true;
				for (int k = 0; k < seq.Size(); ++k)
				{
					if (seq.GetObjectAt(k) is ASN1Sequence)
					{
						seq = (ASN1Sequence)seq.GetObjectAt(0);
						ret = false;
						break;
					}
					if (seq.GetObjectAt(k) is ASN1TaggedObject)
					{
						ASN1TaggedObject tag = (ASN1TaggedObject)seq.GetObjectAt(k);
						if (tag.GetObject() is ASN1Sequence)
						{
							seq = (ASN1Sequence)tag.GetObject();
							ret = false;
							break;
						}
						else
						{
							return;
						}
					}
				}
				if (ret)
				{
					return;
				}
			}
			ASN1OctetString os = (ASN1OctetString)seq.GetObjectAt(1);
			ASN1InputStream inp = new ASN1InputStream(os.GetOctets());
			BasicOCSPResponse resp = BasicOCSPResponse.GetInstance(inp.ReadObject());
			basicResp = new BasicOCSPResp(resp);
		}

		/// <summary>True if there's a PAdES LTV time stamp.</summary>
		private bool isTsp;

		/// <summary>True if it's a CAdES signature type.</summary>
		private bool isCades;

		/// <summary>BouncyCastle TimeStampToken.</summary>
		private TimeStampToken timeStampToken;

		// Time Stamps
		/// <summary>Check if it's a PAdES-LTV time stamp.</summary>
		/// <returns>true if it's a PAdES-LTV time stamp, false otherwise</returns>
		public virtual bool IsTsp()
		{
			return isTsp;
		}

		/// <summary>Gets the timestamp token if there is one.</summary>
		/// <returns>the timestamp token or null</returns>
		public virtual TimeStampToken GetTimeStampToken()
		{
			return timeStampToken;
		}

		/// <summary>Gets the timestamp date</summary>
		/// <returns>a date</returns>
		public virtual Calendar GetTimeStampDate()
		{
			if (timeStampToken == null)
			{
				return null;
			}
			Calendar cal = new GregorianCalendar();
			DateTime date = timeStampToken.GetTimeStampInfo().GetGenTime();
			cal.SetTime(date);
			return cal;
		}

		/// <summary>Returns the filter subtype.</summary>
		public virtual PdfName GetFilterSubtype()
		{
			return filterSubtype;
		}

		/// <summary>Returns the encryption algorithm</summary>
		/// <returns>the name of an encryption algorithm</returns>
		public virtual String GetEncryptionAlgorithm()
		{
			String encryptAlgo = EncryptionAlgorithms.GetAlgorithm(digestEncryptionAlgorithmOid
				);
			if (encryptAlgo == null)
			{
				encryptAlgo = digestEncryptionAlgorithmOid;
			}
			return encryptAlgo;
		}
	}
}