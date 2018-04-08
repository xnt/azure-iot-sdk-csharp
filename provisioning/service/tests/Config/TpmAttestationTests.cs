﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    public class TpmAttestationTests
    {
        private const string Key = "AToAAQALAAMAsgAgg3GXZ0SEs/gakMyNRqXXJP1S124GUgtk8qHaGzMUaaoABgCAAEMAEAgAAAAAAAEAxsj" +
           "2gUScTk1UjuioeTlfGYZrrimExB+bScH75adUMRIi2UOMxG1kw4y+9RW/IVoMl4e620VxZad0ARX2gUqVjYO7KPVt3dyKhZS3dkcvfBisB" +
           "hP1XH9B33VqHG9SHnbnQXdBUaCgKAfxome8UmBKfe+naTsE5fkvjb/do3/dD6l4sGBwFCnKRdln4XpM03zLpoHFao8zOwt8l/uP3qUIxmC" +
           "Yv9A7m69Ms+5/pCkTu/rK4mRDsfhZ0QLfbzVI6zQFOKF/rwsfBtFeWlWtcuJMKlXdD8TXWElTzgh7JS4qhFzreL0c1mI0GCj+Aws0usZh7" +
           "dLIVPnlgZcBhgy1SSDQMQ==";

        /* SRS_TPM_ATTESTATION_21_001: [The EndorsementKey setter shall throws ArgumentNullException if the provided 
         *                              endorsementKey is null or white space.] */
        [TestMethod]
        [TestCategory("DevService")]
        public void TpmAttestation_ThrowsOnNullEndorsementKey()
        {
            // arrange
            string endorsementKey = null;

            // act - assert
            TestAssert.Throws<ProvisioningServiceClientException>(() => new TpmAttestation(endorsementKey));
        }

        [TestMethod]
        [TestCategory("DevService")]
        public void TpmAttestation_ThrowsOnEmptyEndorsementKey()
        {
            // arrange
            string endorsementKey = "";

            // act - assert
            TestAssert.Throws<ProvisioningServiceClientException>(() => new TpmAttestation(endorsementKey));
        }

        /* SRS_TPM_ATTESTATION_21_002: [The StorageRootKey setter shall store the storageRootKey passed.] */
        [TestMethod]
        [TestCategory("DevService")]
        public void TpmAttestation_SucceedOnNullStorageRootKey()
        {
            // arrange
            string endorsementKey = Key;
            string storageRootKey = null;

            // act - assert
            TpmAttestation tpmAttestation = new TpmAttestation(endorsementKey, storageRootKey);

            // assert
            Assert.AreEqual(endorsementKey, tpmAttestation.EndorsementKey);
            Assert.IsNull(tpmAttestation.StorageRootKey);
        }

        [TestMethod]
        [TestCategory("DevService")]
        public void TpmAttestation_SucceedOnEmptyStorageRootKey()
        {
            // arrange
            string endorsementKey = Key;
            string storageRootKey = "";

            // act - assert
            TpmAttestation tpmAttestation = new TpmAttestation(endorsementKey, storageRootKey);

            // assert
            Assert.AreEqual(endorsementKey, tpmAttestation.EndorsementKey);
            Assert.AreEqual(storageRootKey, tpmAttestation.StorageRootKey);
        }

        /* SRS_TPM_ATTESTATION_21_003: [The constructor shall store the provided endorsementKey and storageRootKey.] */
        [TestMethod]
        [TestCategory("DevService")]
        public void TpmAttestation_SucceedOnValidEndorsementKey()
        {
            // arrange
            string endorsementKey = Key;

            // act
            TpmAttestation tpmAttestation = new TpmAttestation(endorsementKey);

            // assert
            Assert.AreEqual(endorsementKey, tpmAttestation.EndorsementKey);
            Assert.IsNull(tpmAttestation.StorageRootKey);
        }

        [TestMethod]
        [TestCategory("DevService")]
        public void TpmAttestation_SucceedOnValidEndorsementKeyAndStorageRootKey()
        {
            // arrange
            string endorsementKey = Key;
            string storageRootKey = Key;

            // act
            TpmAttestation tpmAttestation = new TpmAttestation(endorsementKey, storageRootKey);

            // assert
            Assert.AreEqual(endorsementKey, tpmAttestation.EndorsementKey);
            Assert.AreEqual(storageRootKey, tpmAttestation.StorageRootKey);
        }

        /* SRS_TPM_ATTESTATION_21_004: [The TpmAttestation shall provide means to serialization and deserialization.] */
        [TestMethod]
        [TestCategory("DevService")]
        public void TpmAttestation_SucceedOnSerialization()
        {
            // arrange
            string endorsementKey = Key;
            string storageRootKey = Key;
            string expectedJson = 
                "{" +
                "  \"endorsementKey\":\""+endorsementKey+"\"," +
                "  \"storageRootKey\":\"" + storageRootKey + "\"" +
                "}";
            TpmAttestation tpmAttestation = new TpmAttestation(endorsementKey, storageRootKey);

            // act
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(tpmAttestation);

            // assert
            TestAssert.AreEqualJson(expectedJson, json);
        }

        [TestMethod]
        [TestCategory("DevService")]
        public void TpmAttestation_SucceedOnSerializationWithoutStorageRootKey()
        {
            // arrange
            string endorsementKey = Key;
            string expectedJson =
                "{" +
                "  \"endorsementKey\":\"" + endorsementKey + "\"" +
                "}";
            TpmAttestation tpmAttestation = new TpmAttestation(endorsementKey);

            // act
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(tpmAttestation);

            // assert
            TestAssert.AreEqualJson(expectedJson, json);
        }

        [TestMethod]
        [TestCategory("DevService")]
        public void TpmAttestation_SucceedOnSerializationWithEmptyStorageRootKey()
        {
            // arrange
            string endorsementKey = Key;
            string storageRootKey = "";
            string expectedJson =
                "{" +
                "  \"endorsementKey\":\"" + endorsementKey + "\"," +
                "  \"storageRootKey\":\"" + storageRootKey + "\"" +
                "}";
            TpmAttestation tpmAttestation = new TpmAttestation(endorsementKey, storageRootKey);

            // act
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(tpmAttestation);

            // assert
            TestAssert.AreEqualJson(expectedJson, json);
        }

        [TestMethod]
        [TestCategory("DevService")]
        public void TpmAttestation_SucceedOnDeserialization()
        {
            // arrange
            string endorsementKey = Key;
            string storageRootKey = Key;
            string json =
                "{" +
                "  \"endorsementKey\":\"" + endorsementKey + "\"," +
                "  \"storageRootKey\":\"" + storageRootKey + "\"" +
                "}";

            // act
            TpmAttestation tpmAttestation = Newtonsoft.Json.JsonConvert.DeserializeObject<TpmAttestation>(json);

            // assert
            Assert.AreEqual(endorsementKey, tpmAttestation.EndorsementKey);
            Assert.AreEqual(storageRootKey, tpmAttestation.StorageRootKey);
        }

        [TestMethod]
        [TestCategory("DevService")]
        public void TpmAttestation_ThrowsOnDeserializationWithoutEndorsementKey()
        {
            // arrange
            string storageRootKey = Key;
            string json =
                "{" +
                "  \"storageRootKey\":\"" + storageRootKey + "\"" +
                "}";

            // act - assert
            TestAssert.Throws<ProvisioningServiceClientException>(() => Newtonsoft.Json.JsonConvert.DeserializeObject<TpmAttestation>(json));
        }

        [TestMethod]
        [TestCategory("DevService")]
        public void TpmAttestation_ThrowsOnDeserializationWithEmptyEndorsementKey()
        {
            // arrange
            string storageRootKey = Key;
            string json =
                "{" +
                "  \"endorsementKey\":\"\"," +
                "  \"storageRootKey\":\"" + storageRootKey + "\"" +
                "}";

            // act - assert
            TestAssert.Throws<ProvisioningServiceClientException>(() => Newtonsoft.Json.JsonConvert.DeserializeObject<TpmAttestation>(json));
        }

        [TestMethod]
        [TestCategory("DevService")]
        public void TpmAttestation_SucceedOnDeserializationWithoutStorageRootKey()
        {
            // arrange
            string endorsementKey = Key;
            string json =
                "{" +
                "  \"endorsementKey\":\"" + endorsementKey + "\"," +
                "}";

            // act
            TpmAttestation tpmAttestation = Newtonsoft.Json.JsonConvert.DeserializeObject<TpmAttestation>(json);

            // assert
            Assert.AreEqual(endorsementKey, tpmAttestation.EndorsementKey);
        }

        [TestMethod]
        [TestCategory("DevService")]
        public void TpmAttestation_SucceedOnDeserializationWithEmptyStorageRootKey()
        {
            // arrange
            string endorsementKey = Key;
            string storageRootKey = "";
            string json =
                "{" +
                "  \"endorsementKey\":\"" + endorsementKey + "\"," +
                "  \"storageRootKey\":\"" + storageRootKey + "\"" +
                "}";

            // act
            TpmAttestation tpmAttestation = Newtonsoft.Json.JsonConvert.DeserializeObject<TpmAttestation>(json);

            // assert
            Assert.AreEqual(endorsementKey, tpmAttestation.EndorsementKey);
            Assert.AreEqual(storageRootKey, tpmAttestation.StorageRootKey);
        }
    }
}
