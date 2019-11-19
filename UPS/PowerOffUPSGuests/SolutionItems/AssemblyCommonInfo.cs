//Copyright (C) 2011  Kim Carter

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <http://www.gnu.org/licenses/>

using System.Reflection;
using System.Security;

// These attributes numbers are intended to be shared by all framework,
// business and application assemblies that comprise PayGlobal.
// Individual csproj files should link to this file. They should also
// remove these attributes from their own AssemblyInfo.cs files.
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("BinaryMist.net")]
[assembly: AssemblyProduct("BinaryMist")]
[assembly: AssemblyCopyright("Copyright © BinaryMist.net 2011")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: AllowPartiallyTrustedCallers]
