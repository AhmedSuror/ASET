# ASET - Egyptian Tax Authority APIs implementation

![ASET](/res/Logo-Full.png?raw=true)

## Introduction

ASET is a .NET library that implements the invoicing SDK of the Egyptian Tax Authority.

This project is in the early development stage, you're welcome to contribute.

## Disclaimer

This library is <u>**NOT**</u> an official release of [ETA](https://eta.gov.eg), we neither have a relationship with them nor with any sponsoring authority, however, this library will adhere to the official [ETA API instructions](https://sdk.preprod.invoicing.eta.gov.eg/api/).

## Usage guide

### 1. Token

A static class used for master token generation and manipulation.

#### HttpClient

The token class has a singleton `HttpClient` which serves the purpose of the whole application, it is strongly recommended not to dispose it for the lifetime of the application that uses the library.

Disposing the `HttpClient` will cause it re-initialized each time a transaction occurs against the API portal, which 
may - and possibly will - cause a [socket exhaustion](https://bit.ly/3YKFvJX), so disposing of it should be at the very end of the respective application life.

#### Token Generation

First, you need to create a class that implements the `ISecretVault` interface, which is used to get user data such as client ID and client secrets:

- [ ] `ISecretVault` definition
  
  ```cs
  public interface ISecretVault
  {
      Task<string> GetClientId();
      Task<string> GetClientSecret1();
      Task<string> GetClientSecret2();
  }
  ```
* [ ] Implementation might look like this snippet
  
  ```cs
  ///Tip: User data should be encrypted, and then decrypted and retrieved by this class.
  internal class SecretVault : ISecretVault
  {
      private FrmMain fm = Program.fm;
  
      public Task<string> GetClientId()
      {
          var c = fm.txtClientId.Text.Trim(); 
          return Task.FromResult(c);
      }
  
      public Task<string> GetClientSecret1()
      {
          var c = fm.txtClientSecret1.Text.Trim();
          return Task.FromResult(c);
      }
  
      public Task<string> GetClientSecret2()
      {
          throw new NotImplementedException();
      }
  }
  ```
  
  *The idea behind the scenes is that the user data is extremely confidential and should be encrypted, a good perspective might be encrypting it using [symmetric or asymmetric algorithms](https://bit.ly/3Z63Zx6) and using the implementation to decrypt it, thus passing it to the token securely.*
- [ ] Configuring and generating a token
  
  ```cs
  SecretVault s = new SecretVault(); //Create a SecretVault
  Token.Environment = EtEnvironment.PreProduction; //Set ETA Environment
  await Token.GenerateAsync(); //Generate a new token
  ```
  
  *Note: `EtEnvironment` is a public enum in the namespace `ASET.Core` that holds the environment modes that ETA supports:*
  
  ```cs
  public enum EtEnvironment { PreProduction, SIT, Production }
  ```

- [ ] Get token value
  
    If the token is generated without exceptions, then you can get its value like this:
  
  ```cs
  txtToken.Text = Token.Value
  ```
  
    Also, there are some helpful properties you might want to use:
  
  ```cs
  lblEnvironment.Text = Token.Environment.ToString();
  txtToken.Text = Token.Value ?? string.Empty;
  lblTokenTimestamp.Text = Token.GenerationTime?.ToString() ?? "N/A";
  lblTokenLiveTime.Text = Token.ExpireIn?.ToString() ?? "N/A";
  lblTokenExpiry.Text = Token.ExpiryTime?.ToString() ?? "N/A";
  ```

#### Token destruction

You can erase all token data by calling `Token.Destroy()`

*Note: by calling the preceding method, the related `HttpClient` is not disposed and must be disposed manually by calling `Token.HttpClient.Dispose()`.*

#### Token events

There is a bunch of useful token events you can subscribe to:

1. `TokenGenerating`: Occurs right before the token is successfully generated.
2. `TokenGenerated`:   Occurs after the token is successfully generated.
3. `TokenDestroyed`: Occurs after the token is destroyed by the user.
4. `LifeSpanChanged`: Fires each second passed from the lifetime of the generated token.

# Documentation

Documentation is currently under development, the link will be stated here once available.
