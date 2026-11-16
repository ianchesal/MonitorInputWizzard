namespace MonitorInputWizzard.Tests;

// Only the packet builder is tested: the rest of NvApi needs nvapi64.dll and an NVIDIA GPU.
public class NvApiPayloadTests
{
    [Fact]
    public void Lg_ultragear_hdmi1_payload()
    {
        // VCP 0xF4 = 0x0090 over the 0x50 side channel (the app's defaults).
        var payload = NvApi.BuildSetVcpPayload(vcp: 0xF4, value: 0x0090, sourceAddr: 0x50);

        Assert.Equal(new byte[] { 0x84, 0x03, 0xF4, 0x00, 0x90, 0xDD }, payload);
    }

    [Fact]
    public void Standard_mccs_input_select_payload()
    {
        // VCP 0x60 = 0x0F (DisplayPort 1) from the standard 0x51 host address.
        var payload = NvApi.BuildSetVcpPayload(vcp: 0x60, value: 0x000F, sourceAddr: 0x51);

        Assert.Equal(new byte[] { 0x84, 0x03, 0x60, 0x00, 0x0F, 0xD7 }, payload);
    }

    [Fact]
    public void Value_is_split_big_endian()
    {
        var payload = NvApi.BuildSetVcpPayload(vcp: 0x60, value: 0x1234, sourceAddr: 0x51);

        Assert.Equal(0x12, payload[3]);
        Assert.Equal(0x34, payload[4]);
        Assert.Equal(0xFE, payload[5]);
    }

    [Theory]
    [InlineData(0x10, 0x0000, 0x51)]
    [InlineData(0x60, 0xFFFF, 0x50)]
    [InlineData(0xF4, 0x00D2, 0x50)]
    public void Checksum_xors_to_zero_with_display_and_source_address(byte vcp, ushort value, byte src)
    {
        var payload = NvApi.BuildSetVcpPayload(vcp, value, src);

        byte x = (byte)(0x6E ^ src);
        foreach (var b in payload) x ^= b;
        Assert.Equal(0, x);
    }
}
