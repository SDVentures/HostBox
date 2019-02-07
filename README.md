![logo](https://cloud.githubusercontent.com/assets/888475/20529464/4e61c15a-b0e1-11e6-9fa1-cdd32af5be14.png) HostBox
========

# About

HostBox is a .net core CLI tool used for hosting .net applications.

## Installation

HostBox nuget package can be installed both as a global and local .net cli tool.

`dotnet tool install -g HostBox`

`dotnet tool install --tool-path {LOCAL_TOOL_PATH} HostBox`

## Usage

	host [options]

	Options:
		-p|--path <path>  Path to hostable component.
		-v|--version      Show version information.
		-?|-h|--help      Show help information.