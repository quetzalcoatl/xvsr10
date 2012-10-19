// PackageDigitalSignature SDK Sample - PackageDigitalSignature.cs
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.IO;
using System.IO.Packaging;
using System.Security.Cryptography; // for CryptographicException
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Collections;
using System.Collections.Generic;
using System.Windows;               // for MessageBox

// http://msdn.microsoft.com/en-us/library/dd997171.aspx

namespace SDKSample
{
    class PackageDigitalSignatureSample
    {
        //  ------------------------------ Main -------------------------------
        public static void Main(string[] argv)
        {
            string _packageFilename = argv[0];

            using (Package package = Package.Open(_packageFilename))
            {
                SignAllParts(package);
                ValidateSignatures(package);
            }
        }

        // ------------------------ ValidateSignatures ------------------------
        /// <summary>
        ///   Validates all the digital signatures of a given package.</summary>
        /// <param name="package">
        ///   The package for validating digital signatures.</param>
        /// <returns>
        ///   true if all digital signatures are valid; otherwise false if the
        ///   package is unsigned or any of the signatures are invalid.</returns>
        private static bool ValidateSignatures(Package package)
        {
            if (package == null)
                throw new ArgumentNullException("ValidateSignatures(package)");

            // Create a PackageDigitalSignatureManager for the given Package.
            PackageDigitalSignatureManager dsm =
                new PackageDigitalSignatureManager(package);

            // Check to see if the package contains any signatures.
            if (!dsm.IsSigned)
            {
                MessageBox.Show("The package is not signed");
                return false;
            }

            // Verify that all signatures are valid.
            VerifyResult result = dsm.VerifySignatures(false);
            if (result != VerifyResult.Success)
            {
                MessageBox.Show("One or more digital signatures are invalid.");
                return false;
            }

            // else if (result == VerifyResult.Success)
            return true;        // All signatures are valid.

        }// end:ValidateSignatures()


        private static void SignAllParts(Package package)
        {
            if (package == null)
                throw new ArgumentNullException("SignAllParts(package)");

            // Create the DigitalSignature Manager
            PackageDigitalSignatureManager dsm =
                new PackageDigitalSignatureManager(package);
            dsm.CertificateOption =
                CertificateEmbeddingOption.InSignaturePart;

            // Create a list of all the part URIs in the package to sign
            // (GetParts() also includes PackageRelationship parts).
            System.Collections.Generic.List<Uri> toSign =
                new System.Collections.Generic.List<Uri>();
            foreach (PackagePart packagePart in package.GetParts())
            {
                // Add all package parts to the list for signing.
                toSign.Add(packagePart.Uri);
            }

            // Add the URI for SignatureOrigin PackageRelationship part.
            // The SignatureOrigin relationship is created when Sign() is called.
            // Signing the SignatureOrigin relationship disables counter-signatures.
            toSign.Add(PackUriHelper.GetRelationshipPartUri(dsm.SignatureOrigin));

            // Also sign the SignatureOrigin part.
            toSign.Add(dsm.SignatureOrigin);

            // Add the package relationship to the signature origin to be signed.
            toSign.Add(PackUriHelper.GetRelationshipPartUri(new Uri("/", UriKind.RelativeOrAbsolute)));

            // Sign() will prompt the user to select a Certificate to sign with.
            try
            {
                dsm.Sign(toSign);
            }

            // If there are no certificates or the SmartCard manager is
            // not running, catch the exception and show an error message.
            catch (CryptographicException ex)
            {
                MessageBox.Show(
                    "Cannot Sign\n" + ex.Message,
                    "No Digital Certificates Available",
                    MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
            }

        }// end:SignAllParts()

    }// end:class PackageDigitalSignatureSample

}// end:namespace SDKSample
