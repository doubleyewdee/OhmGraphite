﻿using System;
using System.Configuration;
using System.Net;
using Xunit;

namespace OhmGraphite.Test
{
    public class ConfigTest
    {
        [Fact]
        public void CanParseGraphiteConfig()
        {
            var configMap = new ExeConfigurationFileMap { ExeConfigFilename = "assets/graphite.config" };
            var config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            var customConfig = new CustomConfig(config);
            var results = MetricConfig.ParseAppSettings(customConfig);

            Assert.NotNull(results.Graphite);
            Assert.Null(results.Influx);
            Assert.Equal("myhost", results.Graphite.Host);
            Assert.Equal(2004, results.Graphite.Port);
            Assert.Equal(TimeSpan.FromSeconds(6), results.Interval);
            Assert.True(results.Graphite.Tags);
        }

        [Fact]
        public void CanParseNullConfig()
        {
            var configMap = new ExeConfigurationFileMap { ExeConfigFilename = "assets/default.config" };
            var config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            var customConfig = new CustomConfig(config);
            var results = MetricConfig.ParseAppSettings(customConfig);

            Assert.NotNull(results.Graphite);
            Assert.Null(results.Influx);
            Assert.Equal("localhost", results.Graphite.Host);
            Assert.Equal(2003, results.Graphite.Port);
            Assert.Equal(TimeSpan.FromSeconds(5), results.Interval);
            Assert.False(results.Graphite.Tags);

            Assert.True(results.EnabledHardware.Cpu);
            Assert.True(results.EnabledHardware.Gpu);
            Assert.True(results.EnabledHardware.Motherboard);
            Assert.True(results.EnabledHardware.Controller);
            Assert.True(results.EnabledHardware.Network);
            Assert.True(results.EnabledHardware.Ram);
            Assert.True(results.EnabledHardware.Storage);
        }

        [Fact]
        public void CanParseInfluxDbConfig()
        {
            var configMap = new ExeConfigurationFileMap { ExeConfigFilename = "assets/influx.config" };
            var config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            var customConfig = new CustomConfig(config);
            var results = MetricConfig.ParseAppSettings(customConfig);

            Assert.Null(results.Graphite);
            Assert.NotNull(results.Influx);
            Assert.Equal(TimeSpan.FromSeconds(6), results.Interval);
            Assert.Equal("http://192.168.1.15:8086/", results.Influx.Address.ToString());
            Assert.Equal("mydb", results.Influx.Db);
            Assert.Equal("my_user", results.Influx.User);
            Assert.Equal("my_pass", results.Influx.Password);
        }

        [Fact]
        public void CanParsePrometheusConfig()
        {
            var configMap = new ExeConfigurationFileMap { ExeConfigFilename = "assets/prometheus.config" };
            var config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            var customConfig = new CustomConfig(config);
            var results = MetricConfig.ParseAppSettings(customConfig);

            Assert.Null(results.Graphite);
            Assert.Null(results.Influx);
            Assert.NotNull(results.Prometheus);
            Assert.Equal(4446, results.Prometheus.Port);
            Assert.Equal("127.0.0.1", results.Prometheus.Host);
        }

        [Fact]
        public void CanParseTimescaleConfig()
        {
            var configMap = new ExeConfigurationFileMap { ExeConfigFilename = "assets/timescale.config" };
            var config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            var customConfig = new CustomConfig(config);
            var results = MetricConfig.ParseAppSettings(customConfig);

            Assert.Null(results.Graphite);
            Assert.Null(results.Influx);
            Assert.Null(results.Prometheus);
            Assert.NotNull(results.Timescale);
            Assert.Equal("Host=vm-ubuntu;Username=ohm;Password=123456", results.Timescale.Connection);
            Assert.False(results.Timescale.SetupTable);
        }

        [Fact]
        public void CanParseStaticResolutionName()
        {
            var configMap = new ExeConfigurationFileMap { ExeConfigFilename = "assets/static-name.config" };
            var config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            var customConfig = new CustomConfig(config);
            var results = MetricConfig.ParseAppSettings(customConfig);

            Assert.Equal("my-cool-machine", results.LookupName());
        }

        [Fact]
        public void CanParseSensorAlias()
        {
            var configMap = new ExeConfigurationFileMap { ExeConfigFilename = "assets/rename.config" };
            var config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            var customConfig = new CustomConfig(config);
            var results = MetricConfig.ParseAppSettings(customConfig);

            Assert.True(results.TryGetAlias("/amdcpu/0/load/1", out string alias));
            Assert.Equal("CPU Core 0 T0", alias);
            Assert.True(results.TryGetAlias("/amdcpu/0/load/2", out alias));
            Assert.Equal("CPU Core 0 T1", alias);
            Assert.False(results.TryGetAlias("/amdcpu/0/load/3", out alias));
        }

        [Fact]
        public void CanParseHiddenSensors()
        {
            var configMap = new ExeConfigurationFileMap { ExeConfigFilename = "assets/hidden-sensors.config" };
            var config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            var customConfig = new CustomConfig(config);
            var results = MetricConfig.ParseAppSettings(customConfig);

            Assert.True(results.IsHidden("/amdcpu/0/load/1"));
            Assert.True(results.IsHidden("/amdcpu/0/load/2"));
            Assert.False(results.IsHidden("/amdcpu/0/load/3"));

            Assert.True(results.IsHidden("/amdcpu/0/clock/1"));
            Assert.True(results.IsHidden("/amdcpu/1/clock/100"));
            Assert.True(results.IsHidden("/amdcpu/0/power/1"));
            Assert.True(results.IsHidden("/nvidia-gpu/0/power/1"));
            Assert.False(results.IsHidden("/nvme/0/factor/power_cycles"));

            Assert.False(results.EnabledHardware.Cpu);
        }

        [Fact]
        public void CanInstallCertificateVerification()
        {
            var current = ServicePointManager.ServerCertificateValidationCallback;
            MetricConfig.InstallCertificateVerification("true");
            Assert.Equal(current, ServicePointManager.ServerCertificateValidationCallback);

            MetricConfig.InstallCertificateVerification("false");
            Assert.NotEqual(current, ServicePointManager.ServerCertificateValidationCallback);

            MetricConfig.InstallCertificateVerification("assets/influxdb-selfsigned.crt");
            Assert.NotEqual(current, ServicePointManager.ServerCertificateValidationCallback);

            ServicePointManager.ServerCertificateValidationCallback = null;
        }
    }
}
