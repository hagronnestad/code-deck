<Query Kind="Program">
  <NuGetReference>HidSharp</NuGetReference>
  <Namespace>HidSharp</Namespace>
  <Namespace>HidSharp.Experimental</Namespace>
  <Namespace>HidSharp.Reports</Namespace>
  <Namespace>HidSharp.Reports.Encodings</Namespace>
  <Namespace>HidSharp.Reports.Input</Namespace>
  <Namespace>HidSharp.Reports.Units</Namespace>
  <Namespace>HidSharp.Utility</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

async Task Main()
{
	//DeviceList.Local.GetHidDevices().ToList().Where(x => x.Manufacturer.Contains("Logitech")) .Dump();

	var list = DeviceList.Local.GetHidDevices(1133, 50489)
	.Where(x => x.GetMaxOutputReportLength() > 0);
	//list.ToList().Dump();

	var d = list
	.Where(x => x.GetMaxInputReportLength() == 20)
	.FirstOrDefault();

	HidStream s = d.Open();
	
	
	_ = Task.Run(async () =>
	{
		while (true)
		{
			try
			{
				var rBuffer = new byte[512];

				var readBytes = await Task.Factory.FromAsync(s.BeginRead, s.EndRead, rBuffer, 0, rBuffer.Length, TaskCreationOptions.None);
				Encoding.UTF8.GetString(rBuffer[0..readBytes]).Dump();
				rBuffer[0..readBytes].Dump();

				await Task.Delay(100);
			}
			catch (Exception ex)
			{
			}
		}
	});

	var report = new List<byte>();
	report.AddRange(HidPp.CreateGetFeatureIndexPacket(0x01, HidPp.HIDPP_FEATURE_BATTERY_VOLTAGE));

	var reportArray = new byte[d.GetMaxInputReportLength()];
	Array.Copy(report.Select(x => (byte)x).ToArray(), reportArray, report.Count);
	s.Write(reportArray, 0, reportArray.Length);

	//var response = new byte[20];
	//s.Read(response, 0, response.Length);

	await Task.Delay(30000);
}

private class HidPp
{
	public const byte SOFTWARE_ID = 0x08;
	public const byte MESSAGE_TYPE_SHORT = 0x10;
	public const byte MESSAGE_TYPE_LONG = 0x11;

	public const ushort HIDPP_FEATURE_ROOT = 0x0000;
	public const ushort HIDPP_FEATURE_BATTERY_VOLTAGE = 0x1001;

	public const byte HIDPP_PAGE_ROOT_IDX = 0x00;
	public const byte FUNCTION_ROOT_GET_FEATURE = 0x00;
	public const byte FUNCTION_ROOT_GET_PROTOCOL_VERSION = 0x10;
	public const byte CMD_BATTERY_VOLTAGE_GET_BATTERY_VOLTAGE = 0x00;


	public static byte[] CreateGetFeatureIndexPacket(byte device, ushort feature)
	{
		var packet = new List<byte>();
		packet.Add(MESSAGE_TYPE_LONG);
		packet.Add(device);
		packet.Add(HIDPP_PAGE_ROOT_IDX); // Feature index
		packet.Add(FUNCTION_ROOT_GET_FEATURE | SOFTWARE_ID);
		packet.AddRange(BitConverter.GetBytes(feature).Reverse());
		return packet.ToArray();
	}

	public static int[] CreateGetBatteryInformationPacket(byte device, byte featureIndex)
	{
		return new int[] {
				MESSAGE_TYPE_LONG,
				device,
				featureIndex,
				CMD_BATTERY_VOLTAGE_GET_BATTERY_VOLTAGE | SOFTWARE_ID,
			};
	}

	public static int[] CreateGetFirmwareVersionPacket(byte device)
	{
		return new int[] {
				MESSAGE_TYPE_LONG,
				device,
				0x03, // Feature index
		        FUNCTION_ROOT_GET_PROTOCOL_VERSION | SOFTWARE_ID,
			};
	}

	public static int[] CreatePingPacket(byte device)
	{
		return new int[] {
				MESSAGE_TYPE_LONG,
				device,
				HIDPP_PAGE_ROOT_IDX, // Feature index
		        FUNCTION_ROOT_GET_PROTOCOL_VERSION | SOFTWARE_ID,
				0x00,
				0x00,
				0xAA
			};
	}
}