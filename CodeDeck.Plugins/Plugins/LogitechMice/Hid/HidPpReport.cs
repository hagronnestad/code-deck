using System.IO;

namespace CodeDeck.Plugins.Plugins.LogitechMice.Hid;

public class HidPpReport
{
    public byte ReportId;
    public byte DeviceIndex;
    public byte FeatureIndex;
    public byte FuncIndexAndSoftwareId;
    public byte[] Params = new byte[20 - 4];

    public byte FuncIndex => (byte) (FuncIndexAndSoftwareId >> 4);
    public byte SoftwareId => (byte) (FuncIndexAndSoftwareId & 0x0F);

    public static HidPpReport? FromBytes(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 20) return null;

        using var ms = new MemoryStream(bytes);
        using var r = new BinaryReader(ms);

        var report = new HidPpReport
        {
            ReportId = r.ReadByte(),
            DeviceIndex = r.ReadByte(),
            FeatureIndex = r.ReadByte(),
            FuncIndexAndSoftwareId = r.ReadByte(),
            Params = r.ReadBytes(20 - 4)
        };
        return report;
    }
}
