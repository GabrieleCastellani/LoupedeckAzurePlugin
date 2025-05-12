# Loupedeck Azure Plugin
This plugin allow control of a Virtual Machine on Azure with Loupedeck

## Installation
- Download the latest version from releases
- Install the plugin 
- create a text file under C:\Users\[your user]\\.loupedeck\azure\azure.json

content:
```
{
  "AzureConfigs": {
    "subsciptionid": {
      "ClientId": "",
      "ClientSecret": "",
      "TenantId": ""
    }
  }
}
```
example:
```
{
  "AzureConfigs": {
    "bac73c0d-9c70-4325-a716-ee4558afb6cd": {
      "ClientId": "051b6fb6-d9aa-4d32-a1a8-b206b6044eeb",
      "ClientSecret": "6451c39b-e30e-4046-b5a3-a98399c1848b",
      "TenantId": "7ef663ff-aac4-4128-8315-1e402dd2559a"
    }
  }
}
```
