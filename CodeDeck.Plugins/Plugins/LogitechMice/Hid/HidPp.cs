using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeDeck.Plugins.Plugins.LogitechMice.Hid;

public class HidPp
{
    public const byte SOFTWARE_ID = 0x08;
    public const byte MESSAGE_TYPE_SHORT = 0x10;
    public const byte MESSAGE_TYPE_LONG = 0x11;

    public const ushort HIDPP_FEATURE_ROOT = 0x0000;
    public const ushort HIDPP_FEATURE_BATTERY_VOLTAGE = 0x1001;
    public const ushort HIDPP_FEATURE_UNIFIED_BATTERY = 0x1004;

    public const byte HIDPP_PAGE_ROOT_IDX = 0x00;
    public const byte FUNCTION_ROOT_GET_FEATURE = 0x00;
    public const byte FUNCTION_ROOT_GET_PROTOCOL_VERSION = 0x10;
    public const byte CMD_BATTERY_VOLTAGE_GET_BATTERY_VOLTAGE = 0x00;


    public static byte[] CreateGetFeatureIndexPacket(byte device, ushort feature)
    {
        var packet = new List<byte>
        {
            MESSAGE_TYPE_LONG,
            device,
            HIDPP_PAGE_ROOT_IDX,
            (byte)(FUNCTION_ROOT_GET_FEATURE | SOFTWARE_ID)
        };
        var featureBytes = BitConverter.GetBytes(feature).Reverse();
        packet.AddRange(featureBytes);
        return packet.ToArray();
    }

    public static byte[] CreateGetBatteryInformationPacket(byte device, byte featureIndex)
    {
        return new byte[]
        {
            MESSAGE_TYPE_LONG,
            device,
            featureIndex,
            (byte)(CMD_BATTERY_VOLTAGE_GET_BATTERY_VOLTAGE | SOFTWARE_ID)
        };
    }

    public static byte[] CreateGetUnifiedBatteryPacket(byte deviceIndex, byte featureIndex)
    {
        byte funcAndId = (byte) ((1 << 4) | SOFTWARE_ID); // FuncIndex=1
        return new byte[] { MESSAGE_TYPE_LONG, deviceIndex, featureIndex, funcAndId };
    }

    public static byte[] CreatePingPacket(byte device)
    {
        return new byte[]
        {
            MESSAGE_TYPE_LONG,
            device,
            HIDPP_PAGE_ROOT_IDX,
            (byte)(FUNCTION_ROOT_GET_PROTOCOL_VERSION | SOFTWARE_ID),
            0x00,
            0x00,
            0xAA
        };
    }
}
