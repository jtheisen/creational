# ADO.NET / SQL Server Issue reproduction case

This branch holds a reproduction for a bug in ADO.NET, [filed here](https://github.com/dotnet/efcore/issues/30871).

To reproduce:

1. Have a SQL Server ready in the following version: Microsoft SQL Server Developer (64-bit) 16.0.1050.5 on Windows
2. Make sure to checkout the branch `testcase-for-utf8-issue`.
3. Open the solution with VS and set the console project as the start project
4. Adjust the hard-coded connection string; the database will be created on running the console project
5. Start the project

Or with command line and assuming a SQL Server default instance with appropriate permissions for the current user:

```
git clone --single-branch --branch testcase-for-utf8-issue https://github.com/jtheisen/creational.git testcase-for-utf8-issue
cd .\testcase-for-utf8-issue\
dotnet run --project Creational.Console
```

You will get this exception:

```
SqlException: The incoming tabular data stream (TDS) remote procedure call (RPC) protocol stream is incorrect. Parameter 7 ("@p8"): Data type 0xA7 has an invalid data length or metadata length.
```
This happens only when the collation is set to the UTF8 collation and all the columns to `varchar` (rather than `nvarchar`) as it should be with UTF8 collations.

Even then, it happens only in very specific circumstances, which is why the test case looks a bit more specific than one might expect.

