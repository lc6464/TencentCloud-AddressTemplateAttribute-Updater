try {
	// 读取配置
	ConfigurationBuilder builder = new();

	var settingsFilePath = "";
	if (File.Exists(settingsFilePath = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? "", "appsettings.json"))) {
		builder.AddJsonFile(settingsFilePath);
	}
	if (File.Exists(settingsFilePath = Path.Combine(Environment.CurrentDirectory, "appsettings.json"))) {
		builder.AddJsonFile(settingsFilePath);
	}

	var configuration = builder.Build();

	string? secretId = configuration["Secret:Id"],
		secretKey = configuration["Secret:Key"],
		templateId = configuration["Template:Id"],
		templateDescription = configuration["Template:Description"];

	if (string.IsNullOrWhiteSpace(secretId) || string.IsNullOrWhiteSpace(secretKey)) {
		Console.WriteLine("No secret found!");
		return 1;
	}

	if (string.IsNullOrWhiteSpace(templateId) || string.IsNullOrWhiteSpace(templateDescription)) {
		Console.WriteLine("No template found!");
		return 1;
	}


	// 获取 IPv6 地址
	string? address = null;

	var addresses = Dns.GetHostAddresses(Dns.GetHostName());
	foreach (var ipAddress in addresses) {
		if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6) {
			if (!(ipAddress.IsIPv6LinkLocal || ipAddress.IsIPv6Teredo
				|| ipAddress.IsIPv4MappedToIPv6 || ipAddress.IsIPv6UniqueLocal
				|| ipAddress.IsIPv6SiteLocal || ipAddress.IsIPv6Multicast)) {
				var addressString = ipAddress.ToString();
				if (!(addressString.Split(':')[^1].Length == 1)) {
					address = addressString;
					break;
				}
			}
		}
	}

	if (address is null) {
		Console.WriteLine("No global IPv6 address found!");
		return 2;
	}


	// 请求 API
	VpcClient client = new(new() {
		SecretId = secretId,
		SecretKey = secretKey
	}, "ap-guangzhou");

	AddressInfo addressInfo = new() {
		Address = address,
		Description = templateDescription
	};

	ModifyAddressTemplateAttributeRequest request = new() {
		AddressTemplateId = templateId,
		AddressesExtra = new[] { addressInfo }
	};

	var response = client.ModifyAddressTemplateAttributeSync(request);

	Console.WriteLine("更新 IP 地址成功！Request Id: " + response.RequestId);
} catch (TencentCloudSDKException e) {
	Console.WriteLine("腾讯云 CDN SDK 抛出异常：");
	Console.WriteLine(e);
	return 3;
} catch (Exception e) {
	Console.WriteLine("程序运行时发生异常：");
	Console.WriteLine(e);
	return 4;
}

return 0;