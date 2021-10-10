using UnityEngine.Networking;

namespace AA.Web
{
	public class AcceptAllCertificatesSignedWithASpecificKeyPublicKey : CertificateHandler
	{
		// Encoded RSAPublicKey
		//private static string PUB_KEY = "write key here!";
		protected override bool ValidateCertificate(byte[] certificateData)
		{
			//X509Certificate2 certificate = new X509Certificate2(certificateData);
			//string pk = certificate.GetPublicKeyString();
			//if (pk.ToLower().Equals(PUB_KEY.ToLower()))
			return true;
			//return false;
		}
	}
}
