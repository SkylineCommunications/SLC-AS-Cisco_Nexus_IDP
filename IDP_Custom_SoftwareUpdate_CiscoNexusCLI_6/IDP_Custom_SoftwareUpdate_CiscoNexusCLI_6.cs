/*
****************************************************************************
*  Copyright (c) 2022,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
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

DATE		VERSION		AUTHOR			COMMENTS
06/04/2022	1.0.0.1		ADK, Skyline	Initial version

*/

using System;

using Skyline.DataMiner.Automation;
using Skyline.DataMiner.DataMinerSolutions.DataMinerSystem;
using Skyline.DataMiner.DataMinerSolutions.IDP.Software;

/// <summary>
/// DataMiner Script Class.
/// </summary>
public class Script
{
	private SoftwareUpdate softwareUpdate;

	/// <summary>
	/// The Script entry point.
	/// </summary>
	/// <param name="engine">Link with SLAutomation process.</param>
	public void Run(IEngine engine)
	{
		try
		{
			softwareUpdate = new SoftwareUpdate(engine);
			softwareUpdate.NotifyProcessStarted();

			PerformUpgrade(engine);
		}
		catch (ScriptAbortException)
		{
			softwareUpdate?.NotifyProcessFailure("Script aborted");
			throw;
		}
		catch (Exception e)
		{
			softwareUpdate?.NotifyProcessFailure(e.ToString());
			engine.ExitFail($"Exception thrown{Environment.NewLine}{e}");
		}
	}

	private void PerformUpgrade(IEngine engine)
	{
		InputData inputParameters = softwareUpdate.InputData;
		IElement element = inputParameters.Element;

		IActionableElement dataMinerElement = engine.FindElement(element.AgentId, element.ElementId);

		PushUpgradeToDevice(dataMinerElement, inputParameters.ImageFileLocation);

		ValidateResult(engine, dataMinerElement);
	}

	private void PushUpgradeToDevice(IActionableElement element, string imageFileLocation)
	{
		try
		{
			//install new image
			string commandUpdate = "install all nxos " + imageFileLocation + " non-interruptive";
			//string commandUpdate = "sh install all progress";
			element.SetParameter(9670, commandUpdate);
		}
		catch (Exception e)
		{
			softwareUpdate.NotifyProcessFailure(
				$"Failed to issue software update command to element{Environment.NewLine}{e}");
		}
	}

	private void ValidateResult(IEngine engine, IActionableElement dataMinerElement)
	{
		string[] primaryKeys = dataMinerElement.GetTablePrimaryKeys(9700);
		int startIndex = 0;

		if (primaryKeys.Length == 1)
		{
			startIndex = Convert.ToInt32(primaryKeys[0]);
		}
		else
		{
			throw new UpdateFailedException("Install command failed");
		}

		bool restarting = false;

		for (int i = 0; i < 7; i++)
		{
			string shProgress = "sh install all progress";
			dataMinerElement.SetParameter(9670, shProgress);
			engine.Sleep(15000);
			int index = (startIndex + i + 1);
			string progress = dataMinerElement.GetParameterDisplayByPrimaryKey(9703, "" + index);
			engine.GenerateInformation("Progress " + index + "::" + progress);
			engine.Sleep(45000);

			Skyline.DataMiner.Automation.Element[] elements = engine.FindElements(new ElementFilter { DataMinerID = dataMinerElement.DmaId, ElementID = dataMinerElement.ElementId, TimeoutOnly = true });

			if (elements.Length == 1)
			{
				restarting = true;
				break;
			}

			//read progress update
		}

		if (!restarting)
		{
			engine.GenerateInformation("ERR:ISSU Time out (> 7 min)");
			throw new UpdateFailedException("ISSU Unsuccesful");
		}

		for (int i = 0; i < 6; i++)
		{
			Skyline.DataMiner.Automation.Element[] elements = engine.FindElements(new ElementFilter { DataMinerID = dataMinerElement.DmaId, ElementID = dataMinerElement.ElementId, TimeoutOnly = true });
			engine.GenerateInformation("Check Element state");

			if (elements.Length == 0)
			{
				engine.GenerateInformation("Active again");
				restarting = false;

				break;
			}

			engine.GenerateInformation("Element in Timeout");

			engine.Sleep(60000);
		}

		if (restarting)
		{
			engine.GenerateInformation("ERR:Element remains in timeout (> 5 min)");
			throw new UpdateFailedException("Element remains in timeout");
		}
		else
		{
			engine.Sleep(60000); //wait for sysdescription to be updated.
		}

		softwareUpdate.NotifyProcessSuccess();
	}
}