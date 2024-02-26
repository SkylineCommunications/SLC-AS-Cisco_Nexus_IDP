/*
****************************************************************************
*  Copyright (c) 2022,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this driver, you expressly agree with the usage terms and
conditions set out below.
This driver and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this driver is strictly for personal use only.
This driver may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
driver is forbidden.

Any modifications to this driver by the user are only allowed for
personal use and within the intended purpose of the driver,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the driver resulting from a modification
or adaptation by the user.

The content of this driver is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

	Skyline Communications NV
	Ambachtenstraat 33
	B-8870 Izegem
	Belgium
	Tel.	: +32 51 31 35 69
	Fax.	: +32 51 31 01 29
	E-mail	: info@skyline.be
	Web		: www.skyline.be
	Contact	: Ben Vandenberghe

****************************************************************************
Revision History:

DATE		VERSION		AUTHOR			COMMENT
06/04/2022	1.0.0.1		ADK, Skyline	Initial version.

*/

using System;
using System.Collections.Generic;
using System.IO;

using IDP.Common;

using Newtonsoft.Json;

using Skyline.DataMiner.Automation;
using Skyline.DataMiner.DataMinerSolutions.IDP.ConfigurationManagement;

public class Script
{
	//vrf part of the copy command. Replace 'management' with the correct vrf. Specify an empty string if vrf is not required.
	public static readonly string sVrf = "vrf management";

	private Update configurationUpdate;

	public void Run(IEngine engine)
	{
		try
		{
			// This method will communicate with the IDP solution, to provide the required feedback for the update process.
			configurationUpdate = new Update(engine);

			// When the configuration update process starts normally, this method should be called.
			configurationUpdate.NotifyProcessStarted();

			string filePath = configurationUpdate.InputData.FileLocation;

			string data = GetDataFromFile(filePath);

			// In here, the data will be sent to the element. The code within it should be customized.
			if (!SetDataOnElement(engine, data))
			{
				// On unexpected cases that are not the result of exceptions, the script should still inform IDP of the failure.
				configurationUpdate.NotifyProcessFailure("There was an issue while setting data on the element.");
				return;
			}

			// On a normal execution, IDP should be notified of its success through this method.
			configurationUpdate.NotifyProcessSuccess();
		}
		catch (ScriptAbortException)
		{
			// For any exceptions, or other exceptional/unexpected behavior, the IDP solution should be informed, if possible, of the failure.
			configurationUpdate?.NotifyProcessFailure("Update script was aborted.");
			throw;
		}
		catch (FileLoadException ex)
		{
			configurationUpdate?.NotifyProcessFailure($"There was an issue while loading the data from the file:{Environment.NewLine}{ex}");
			engine.ExitFail(ex.ToString());
		}
		catch (Exception ex)
		{
			configurationUpdate?.NotifyProcessFailure($"The main script failed due to an exception:{Environment.NewLine}{ex}");
			engine.ExitFail(ex.ToString());
		}
	}

	private static string GetDataFromFile(string filePath)
	{
		if (String.IsNullOrWhiteSpace(filePath))
		{
			throw new ArgumentException("The provided path is invalid.", nameof(filePath));
		}

		string data = File.ReadAllText(filePath);

		if (String.IsNullOrWhiteSpace(data))
		{
			throw new FileLoadException("File could not be read properly.");
		}

		return data;
	}

	private static string UpdateConfigRunning(BackupDataFolderPath backupData)
	{
		List<string> commands = new List<string>
		{
			$"copy tftp://{backupData.Tftp}/{backupData.FolderPath}/{backupData.FileName} running-config {sVrf}",
			"WR"
		};

		return string.Join(";", commands);
	}

	private bool SetDataOnElement(IEngine engine, string data)
	{
		try
		{
			var backupData = JsonConvert.DeserializeObject<BackupDataFolderPath>(data);

			var commandToSet = UpdateConfigRunning(backupData);

			Element element = engine.FindElement(configurationUpdate.InputData.Element.AgentId, configurationUpdate.InputData.Element.ElementId);
			engine.GenerateInformation(commandToSet);

			element.SetParameter(9670, commandToSet);

			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}
}