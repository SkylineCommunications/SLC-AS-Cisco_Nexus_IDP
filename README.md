# SLC-AS-Cisco_Nexus_IDP

## Cisco Nexus CLI IDP Automation Scripts
This repository contains a collection of DataMiner Infrastructure Discovery Provisioning (IDP) automation scripts tailored for Cisco Nexus CLI. 

### Scripts

Configuration Backup Script: This script communicates with IDP solution and provides the required feedback for the backup process. IDP backup allows the user to decide how to manage the way the backups are saved. For a normal use, it is possible to select full backup, while for the change detection, it is possible to choose a single full backup or a full + core method.

Configuration Update Script: This script communicates with IDP solution and provides the required feedback for the update process. IDP update allows the user to send the update data to the element.

Software Update Script: This script is used for upgrading the element's software with the desired version.

### Usage
Requirements: Ensure you have DataMinerSolutions Dynamic Link Library at: C:\Skyline DataMiner\ProtocolScripts\DataMinerSolutions.dll
