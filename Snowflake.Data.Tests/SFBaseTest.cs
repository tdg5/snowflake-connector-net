/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace Snowflake.Data.Tests
{
    using NUnit.Framework;
    using NUnit.Framework.Interfaces;
    using Newtonsoft.Json;

    /*
     * This is the base class for all tests that call blocking methods in the library - it uses MockSynchronizationContext to verify that 
     * there are no async deadlocks in the library
     * 
     */
    [TestFixture]
    public class SFBaseTest : SFBaseTestAsync
    {
        [SetUp]
        public static void SetUpContext()
        {
            MockSynchronizationContext.SetupContext();
        }

        [TearDown]
        public static void TearDownContext()
        {
            MockSynchronizationContext.Verify();
        }
    }

    /*
     * This is the base class for all tests that call async metodes in the library - it does not use a special SynchronizationContext
     * 
     */
    [SetUpFixture]
    public class SFBaseTestAsync
    {
        private const string connectionStringWithoutAuthFmt = "scheme={0};host={1};port={2};" +
            "account={3};role={4};db={5};schema={6};warehouse={7}";

        protected string ConnectionStringWithoutAuth
        {
            get
            {
                return String.Format(connectionStringWithoutAuthFmt,
                    testConfig.protocol,
                    testConfig.host,
                    testConfig.port,
                    testConfig.account,
                    testConfig.role,
                    testConfig.database,
                    testConfig.schema,
                    testConfig.warehouse);
            }
        }
        private const string connectionStringSnowflakeAuthFmt = ";user={0};password={1};";

        protected string ConnectionString
        {
            get {
                return ConnectionStringWithoutAuth +
                    String.Format(connectionStringSnowflakeAuthFmt,
                    testConfig.user,
                    testConfig.password);
            }
        }

        protected TestConfig testConfig { get; set; }

        [OneTimeSetUp]
        public void SFTestSetup()
        {
            log4net.GlobalContext.Properties["framework"] = "netcoreapp2.0";
            var logRepository = log4net.LogManager.GetRepository(Assembly.GetEntryAssembly());
            log4net.Config.XmlConfigurator.Configure(logRepository, new FileInfo("App.config"));
            var cloud = Environment.GetEnvironmentVariable("SNOWFLAKE_TEST_CLOUD_ENV");
            Assert.IsTrue(cloud == null || cloud == "AWS" || cloud == "AZURE" || cloud == "GCP", "{0} is not supported. Specify AWS, AZURE or GCP as cloud environment", cloud);

            var account = Environment.GetEnvironmentVariable("SNOWFLAKE_TEST_ACCOUNT");
            var database = Environment.GetEnvironmentVariable("SNOWFLAKE_TEST_DATABASE");
            var host = Environment.GetEnvironmentVariable("SNOWFLAKE_TEST_HOST");
            var password = Environment.GetEnvironmentVariable("SNOWFLAKE_TEST_PASSWORD");
            var role = Environment.GetEnvironmentVariable("SNOWFLAKE_TEST_ROLE");
            var schema = Environment.GetEnvironmentVariable("SNOWFLAKE_TEST_SCHEMA");
            var user = Environment.GetEnvironmentVariable("SNOWFLAKE_TEST_USER");
            var warehouse = Environment.GetEnvironmentVariable("SNOWFLAKE_TEST_WAREHOUSE");

            var testConfigs = new Dictionary<string, TestConfig> {
                {
                    "AZURE",
                    new TestConfig() {
                        account = account,
                        database = database,
                        host = host,
                        password = password,
                        role = role,
                        schema = schema,
                        user = user,
                        warehouse = warehouse
                    }
                }
            };

            // get key of connection. Default to "testconnection". If snowflake_cloud_env is specified, use that value as key to
            // find connection object
            String connectionKey = cloud == null ? "testconnection" : cloud;

            TestConfig testConnectionConfig;
            if (testConfigs.TryGetValue(connectionKey, out testConnectionConfig))
            {
                testConfig = testConnectionConfig;
            }
            else
            {
                Assert.Fail($"Failed to load test configuration");
            }
        }
    }

    public class TestConfig
    {
        internal string user { get; set; }

        internal string password { get; set; }

        internal string account { get; set; }

        internal string host { get; set; }

        internal string port { get; set; }

        internal string warehouse { get; set; }

        internal string database { get; set; }

        internal string schema { get; set; }

        internal string role { get; set; }

        internal string protocol { get; set; }

        internal string OktaUser { get; set; }

        internal string OktaPassword { get; set; }

        internal string OktaURL { get; set; }

        public TestConfig()
        {
            this.protocol = "https";
            this.port = "443";
        }
    }

    public class IgnoreOnEnvIsAttribute : Attribute, ITestAction
    {
        String key;

        string[] values;
        public IgnoreOnEnvIsAttribute(String key, string[] values)
        {
            this.key = key;
            this.values = values;
        }

        public void BeforeTest(ITest test)
        {
            foreach (var value in this.values)
            {
                if (Environment.GetEnvironmentVariable(key) == value)
                {
                    Assert.Ignore("Test is ignored when environment variable {0} is {1} ", key, value);
                }
            }
        }

        public void AfterTest(ITest test)
        {
        }

        public ActionTargets Targets
        {
            get { return ActionTargets.Test | ActionTargets.Suite; }
        }
    }
}
