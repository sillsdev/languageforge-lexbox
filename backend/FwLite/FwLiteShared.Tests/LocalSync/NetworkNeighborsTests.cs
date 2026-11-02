using System.Net;
using FwLiteShared.LocalSync.Hosting;

namespace FwLiteShared.Tests.LocalSync;

public class NetworkNeighborsTests
{
    [Fact]
    public void LinuxsArpTable_ListsOnlyCompleteEntries()
    {
        const string table = """
            IP address       HW type     Flags       HW address            Mask     Device
            10.42.0.12       0x1         0x2         3a:1f:9c:40:22:7e     *        wlp2s0
            10.42.0.13       0x1         0x0         00:00:00:00:00:00     *        wlp2s0
            192.168.1.1      0x1         0x6         b4:fb:e4:12:9a:01     *        enp3s0
            """;

        SystemNetworkNeighbors.ParseProcNetArp(table).Should().Equal(IPAddress.Parse("10.42.0.12"), IPAddress.Parse("192.168.1.1"));
    }

    [Fact]
    public void WindowsDhcpNames_AreReadFromTheHostsFile()
    {
        const string hosts = "# Copyright (c) Microsoft Corp.\r\n"
                             + "192.168.137.45 field-laptop # 2026 10 29 16:45 \r\n"
                             + "\r\n"
                             + "192.168.137.46\tGalaxy-A15.mshome.net\r\n";

        SystemNetworkNeighbors.ParseHostsFile(hosts).Should().BeEquivalentTo(new Dictionary<IPAddress, string>
        {
            [IPAddress.Parse("192.168.137.45")] = "field-laptop",
            [IPAddress.Parse("192.168.137.46")] = "Galaxy-A15.mshome.net",
        });
    }

    [Theory]
    [InlineData("192.168.137.45", true)]
    [InlineData("192.168.137.1", false)] // the host itself
    [InlineData("192.168.137.0", false)] // the network
    [InlineData("192.168.137.255", false)] // broadcast
    [InlineData("192.168.138.45", false)]
    public void ASubnet_ContainsTheOtherDevicesAddresses(string address, bool contains)
    {
        var subnet = new Subnet(IPAddress.Parse("192.168.137.1"), 24);

        subnet.Contains(IPAddress.Parse(address)).Should().Be(contains);
    }

    [Fact]
    public void ASweep_CoversEveryOtherAddressOnTheSubnet()
    {
        var subnet = new Subnet(IPAddress.Parse("192.168.137.1"), 24);

        var addresses = subnet.Addresses().ToList();

        addresses.Should().HaveCount(253)
            .And.NotContain(IPAddress.Parse("192.168.137.1"))
            .And.Contain([IPAddress.Parse("192.168.137.2"), IPAddress.Parse("192.168.137.254")]);
    }
}
