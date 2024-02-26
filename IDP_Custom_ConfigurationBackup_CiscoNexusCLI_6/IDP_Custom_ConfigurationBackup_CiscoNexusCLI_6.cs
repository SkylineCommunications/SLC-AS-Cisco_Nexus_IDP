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
using System.Diagnostics;
using System.Text.RegularExpressions;

using IDP.Common;

using Newtonsoft.Json;

using Skyline.DataMiner.Automation;
using Skyline.DataMiner.DataMinerSolutions.IDP.ConfigurationManagement;
using Skyline.DataMiner.DataMinerSolutions.IDP.Templates.Configuration;

public class Script
{
	//SMB Folder Name
	public static readonly string baseFolderPath = "conf/_CMC/";

	//vrf part of the copy command. Replace 'management' with the correct vrf. Specify an empty string if vrf is not required.
	public static readonly string sVrf = "vrf management";

	//IP Address of FTP Server. Machine also exposes destination folder via SMB
	public static readonly string tFtp = "192.164.206.5";

	private Backup backupManager;

	private BackupInputData inputData;

	public void Run(IEngine engine)
	{
		try
		{
			// This will load the automation script's parameter data.
			inputData = new BackupInputData(engine);

			// This method will communicate with the IDP solution, to provide the required feedback for the backup process.
			backupManager = new Backup(inputData);

			// When the backup process starts normally, this method should be called.
			backupManager.NotifyProcessStarted();

			// Backup will be managed in this method.
			CreateAndSendBackup(engine);

			// On a normal execution, IDP should be notified of its success through this method.
			backupManager.NotifyProcessSuccess();
		}
		catch (ScriptAbortException)
		{
			// For any exceptions, or other unexpected behavior, the IDP solution should be informed, if possible, of the failure.
			backupManager?.NotifyProcessFailure("Backup script was aborted.");
			throw;
		}
		catch (BackupFailedException ex)
		{
			backupManager?.NotifyProcessFailure($"Custom backup code failed with the following exception:{Environment.NewLine}{ex}");
			engine.ExitFail(ex.ToString());
		}
		catch (Exception ex)
		{
			backupManager?.NotifyProcessFailure($"The main script failed due to an exception:{Environment.NewLine}{ex}");
			engine.ExitFail(ex.ToString());
		}
	}

	private static string HandleRunningConfig(string fileName, string folderPath)
	{
		List<string> commands = new List<string>
		{
			$"copy running-config tftp://{tFtp}/{folderPath}/{fileName} {sVrf}",
		};

		return string.Join(";", commands);
	}

	private static string HandleStartupConfig(string fileName, string folderPath)
	{
		List<string> commands = new List<string>
		{
			$"copy startup-config tftp://{tFtp}/{folderPath}/{fileName} {sVrf}",
		};

		return string.Join(";", commands);
	}

	private static bool Retry(Func<bool> func, TimeSpan timeout)
	{
		bool success;

		Stopwatch sw = Stopwatch.StartNew();

		do
		{
			success = func();
			if (!success)
			{
				System.Threading.Thread.Sleep(100);
			}
		}
		while (!success && sw.Elapsed <= timeout);

		return success;
	}

	/// <summary>
	/// </summary>
	/// <param name="engine">The object that will communicate with the DMA.</param>
	/// <param name="backupMethod"></param>
	/// <returns></returns>
	private string BackupDevice(IEngine engine, Func<IActionableElement, string> backupMethod)
	{
		string backup = String.Empty;

		IActionableElement element = engine.FindElement(inputData.Element.AgentId, inputData.Element.ElementId);

		const int BackupTimeoutMinutes = 15;

		bool result = Retry(
		() =>
		{
			// Run the backup method, that will provide you with the data. Depending on the method that was provided, it will return different data.
			backup = backupMethod.Invoke(element);

			return !String.IsNullOrWhiteSpace(backup);
		},
		TimeSpan.FromMinutes(BackupTimeoutMinutes));

		if (!result)
		{
			throw new BackupFailedException($"No data was obtained from the element after {BackupTimeoutMinutes} minutes.");
		}

		return backup;
	}

	/// <summary>
	/// This method is a framework of how to backup a device, and then send that same data to IDP. Depending on the goal,
	/// several options are presented inside, and only one of each should be selected.
	/// </summary>
	/// <param name="engine">The object that will communicate with the DMA.</param>
	private void CreateAndSendBackup(IEngine engine)
	{
		/* *************
		 * *Backup Code*
		 * *************
		 * IDP backup allows the user to decide how to manage the way the backups are saved.
		 * Please, carefully select the desired methods, according to your chosen method of backup, outlined below.
		 *
		 * Please, use only one of the following possibilities:
		 * > Send backup content without change detection verification;
		 *		One backup method, one send method
		 * > Send backup content with change detection verification, with the same data for full and core backup;
		 * > Send backup content with change detection verification, with different data for full and core backup;
		 * > Send backup file path without change detection verification;
		 * > Send backup file path with change detection verification, with the same path for full and core backup;
		 * > Send backup file path with change detection verification, with a different path for full and core backup;
		 */

		/*
		 * The backup itself will happen here. Depending on the method provided, you will receive back different types of data.
		 * Please, select only the backup methods required for your goals. For normal use, select only a full backup, while
		 * for change detection, depending on your goal, you may either chose a single full backup, or a full + core method.
		 *
		 * *****************************
		 * *Full Backup vs Core Backup:*
		 * *****************************
		 *
		 * Not all parameters of an element need to be considered when detecting a change in the data. For example, if you have
		 * an uptime parameter, that changes constantly, there is very little reason to include that when detecting a change
		 * in version. As such, IDP allows you to provide two sets of backup data:
		 * > Full Backup includes all parameter data that the user wishes to save. This is what's used for later restoration;
		 * > Core Backup includes all parameter data that should allow IDP to detect a change in version. Usually, only
		 * parameters that hardly change, or configuration parameters, are provided here. This allows a full backup data to
		 * change, without the version changing with it;
		 *
		 * As such, when using change detection, it may be worth considering doing a full and a core backup, and using it
		 * on the appropriate methods.
		 */
		string fullBackupData = BackupDevice(engine, GetDeviceFullBackupAsText);

		/* ******************
		 * *Send Backup Code*
		 * ******************
		 * The following will be a collection of the several ways to send the obtained data to IDP. Depending on the goal,
		 * only one of the following methods should be selected.
		 */

		/* *************************************
		 * *Send Backup Possibility: as content*
		 * *************************************
		 * When the backup is created as content, this method will send that same data to IDP to be stored as a full backup
		 * in the configuration archive. IDP will save the sent content within a file it creates.
		 */
		backupManager.SendBackupContentToIdp(fullBackupData);

		// TODO: check if we can move the file to local drive & share the data content instead of custom class details with FTP references.
		//	backupManager.SendBackupFilePathToIdp(fullBackupData);
	}

	/// <summary>
	/// </summary>
	/// <param name="element">The element object from where data can be fetched.</param>
	/// <returns>The full backup data as text.</returns>
	private string GetDeviceFullBackupAsText(IActionableElement element)
	{
		string elementName = Regex.Replace(element.ElementName, @"\s", "");
		string commandToSet;
		string fileName;

		string folderPath = baseFolderPath + elementName;

		switch (inputData.ConfigurationType)
		{
			case ConfigurationType.StartUp:
				fileName = elementName + "-" + DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss") + "-startup.cfg";
				commandToSet = HandleStartupConfig(fileName, folderPath);
				break;

			case ConfigurationType.Running:
				fileName = elementName + "-" + DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss") + "-running.cfg";
				commandToSet = HandleRunningConfig(fileName, folderPath);
				break;

			default:

				throw new BackupFailedException($"{inputData.ConfigurationType} type not supported");
		}

		// Send command
		try
		{
			element.SetParameter(9670, commandToSet);
		}
		catch
		{
		}

		// grab file from FTP to DM location
		var backup = new BackupDataFolderPath
		{
			Tftp = tFtp,
			FolderPath = folderPath,
			FileName = fileName,
		};

		return JsonConvert.SerializeObject(backup);
	}
}