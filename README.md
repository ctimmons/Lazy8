Lazy8 (A C# Utilities Library for .Net Core and Beyond)
=====================================

(NOTE: This library supercedes the [cs_utilities](https://github.com/ctimmons/cs_utilities) library.)

License
-------

Unless otherwise noted, the code in this respository is governed by the GNU GENERAL PUBLIC LICENSE Version 3.  See the LICENSE file in the solution's root folder for the details.

Overview
--------

The repository contains Visual Studio 2022 projects which are configured to use .Net 8, but most of the code should work with earlier Core-related versions.

Most of the code in Lazy8.Core has unit tests in Lazy8.Core.Tests.  The majority of methods are short, usually less than five lines of code.  I've tried to make the method and property names clearly state what they do.  The documentation is OK, but I'm still working on it (and probably always will be).

Dependencies
------------

All dependencies are via NuGet packages, so just building the projects should automatically download the required packages.

Highlights
----------

Lazy8.Core includes abstractions for easier handling of strings and DateTimes, code for chaining assertions together (Assert.cs), and code for serializing/deserializing XML (Xml.cs).  StringScanner.cs implements a general purpose recursive-descent lexical scanner.  To see how it's used, check out GetTSqlBatchExtension.cs in the Lazy8.SqlClient project.

Lazy8.SqlClient contains extension methods making it a little easier to execute T-SQL on a connection.  Included is the above mentioned GetTSqlBatchExtension.cs file, which contains a scanner that intelligently splits a given T-SQL string on any embedded GO batch separators, and submits each batch on the given connection.  Note that this does not use Sql Server Management Objects to achieve this.

Pull Requests and Copying
-------------------------

Please note this library is licensed under the GNU GENERAL PUBLIC LICENSE Version 3.  Any pull requests will also be under that license.  If the pull request has a different license, I might not be able to acccept the request.

