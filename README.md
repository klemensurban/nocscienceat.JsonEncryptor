# nocscienceat.JsonEncryptor

A small Commandline tool to create encrypted configuration files (`.encVault`) used by the `nocscienceat.EncryptedConfigurationProvider` library.

## Overview

nocscienceat.EncryptedConfigurationProvider extends `Microsoft.Extensions.Configuration` to support encrypted configuration files (`.encVault`). - see https://github.com/klemensurban/nocscienceat.EncryptedConfigurationProvider for details.
The JsonEncryptor commandline tool allows you to create such encrypted configuration files using the same encryption mechanism as the EncryptedConfigurationProvider.
To use JsonEncryptor you need a certificate with public/private keypair installed in the certificate store of LocalMachine or CurrentUser. 
Since the the underlying AES Key, nonce and tag are not only encrypted with RSA but also signed (with the same certificate) you must have access to both the public and private key of the certificate.

## Disclaimer  ;-)
Most of the code was written by ChatGPT with the exception of the encryption/decryption logic which is part of the `nocscienceat.Aes256GcmRsaCryptoService` library.

## Installation

Download this repository and build the solution with a Visual Studio Version supporting .Net 8 or later, or via commandline using `dotnet build`.


## Usage

1. **Create json Configuration template** 

Create a json file (e.g. `appsettings.template.json`)  you want to encrypt, the sensitive values should be replaced with `<ask>` e.g.

```json
{
  "LdapAccountInfo": {
    "LdapServer": "<ask>",
    "LdapPort": 389,
    "LdapUserDn": "<ask>",
    "LdapPassword": "<ask>"
  },
  "SomeOtherConfig": {
    "Setting1": "Value1",
    "Setting2": "Value2"
  }
}
```
when JsonEncryptor encounters a value `<ask>` it will prompt you to enter the real value

2. **JsonEncryptor Commandline parameters**

```
JsonEncryptor.exe

Usage:
  JsonEncryptor -f <pathToJsonFile> [-t <certificateThumbprint>] [-u]

Options:
  -f <path>   Path to input JSON file (required).
  -t <thumb>  Certificate thumbprint .
  -u          Use CurrentUser certificate store instead of LocalMachine .
``` 


3. **Example**
```
prompt>JsonEncryptor.exe -f appsettings.template.json -t 98b11214d22b062aff19e3b55edfa0ddc6b61d18

Enter replacement for 'LdapAccountInfo.LdapServer': myldap.com
Enter replacement for 'LdapAccountInfo.LdapUserDn': CN=ldapsvc,DC=myldap,DC=com
Enter replacement for 'LdapAccountInfo.LdapPassword': P@$$w0rd

Resulting JSON:
{
  "LdapAccountInfo": {
    "LdapServer": "myldap.com",
    "LdapPort": 389,
    "LdapUserDn": "CN=ldapsvc,DC=myldap,DC=com",
    "LdapPassword": "P@$$w0rd"
  },
  "SomeOtherConfig": {
    "Setting1": "Value1",
    "Setting2": "Value2"
  }
}
Press any Key to continue or ctrl-c to terminate  // (waiting for user input, with "Enter" the Configuration is encrypted and written to encrypted configuration file 98b11214d22b062aff19e3b55edfa0ddc6b61d18.encVault)


Decrypted JSON for verification: // (after decryption of the created .encVault file)
{
  "LdapAccountInfo": {
    "LdapServer": "myldap.com",
    "LdapPort": 389,
    "LdapUserDn": "CN=ldapsvc,DC=myldap,DC=com",
    "LdapPassword": "P@$$w0rd"
  },
  "SomeOtherConfig": {
    "Setting1": "Value1",
    "Setting2": "Value2"
  }
}

prompt>dir *.encVault


 Directory of C:\....

28.11.2025  18:24             1 748 98b11214d22b062aff19e3b55edfa0ddc6b61d18.encVault
               1 File(s)          1 748 bytes

prompt>type 98b11214d22b062aff19e3b55edfa0ddc6b61d18.encVault
GFvV7NZoUgcReUhHEFlUo5J4we2O/F2Tum9IK8pt/L63K6w+rvW6HMzPp5+Agn5ot2KAKfx1oF3U
b3zNM2we9itxcPfjc3wS3QUbuqUMCwhubfOGaAjLlk3ohWDjrTmWMg+BMTejgtgWGTP0OEP89chs
GZovFbaBPHuEaLyuxrGAskhOoQ9CZcM2bGt5JE8sdJoZFllnR3icMfkAxlZ+ApzpNW/2TqmWBOI0
....
....
ZgVTSYRlLUKuSv6AO0zXn65RNSVI8eA=

```