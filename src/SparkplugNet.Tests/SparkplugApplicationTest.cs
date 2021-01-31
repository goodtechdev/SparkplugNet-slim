﻿namespace SparkplugNet.Tests
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using SparkplugNet.Application;
    using SparkplugNet.Enumerations;

    [TestClass]
    public class SparkplugApplicationTest
    {
        private SparkplugApplicationOptions options = new SparkplugApplicationOptions(
            "localhost",
            "testApplication",
            "test",
            "password",
            false,
            "scala1",
            TimeSpan.FromSeconds(5),
            true);

        /// <summary>
        /// Tests the Sparkplug application with the version A namespace.
        /// </summary>
        [TestMethod]
        public async Task TestNamespaceA()
        {
            var application = new SparkplugApplication(SparkplugVersion.V22, SparkplugNamespace.VersionA);
            await application.Start(this.options);
        }

        /// <summary>
        /// Tests the Sparkplug application with the version B namespace.
        /// </summary>
        [TestMethod]
        public async Task TestNamespaceB()
        {
            var application = new SparkplugApplication(SparkplugVersion.V22, SparkplugNamespace.VersionB);
            await application.Start(this.options);
        }
    }
}