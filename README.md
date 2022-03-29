# ![logo](https://cloud.githubusercontent.com/assets/888475/20529464/4e61c15a-b0e1-11e6-9fa1-cdd32af5be14.png) HostBox

## About

HostBox is a .net core CLI tool used for hosting .net applications.

## Installation

HostBox nuget package can be installed both as a global and local .net cli tool.

`dotnet tool install -g HostBox`

`dotnet tool install --tool-path {LOCAL_TOOL_PATH} HostBox`

## Usage

```cmd
host [options]

Options:
  -p|--path <path>  Path to hostable component.
  -v|--version      Show version information.
  -w|--web          Run as a web component.
  -?|-h|--help      Show help information.
```

## Configuration

### Filtering configuration files

It is possible to filter configuration files of the hosted service on loading.
For instance, a service's working directory may have 2 or more additional configuration files applicable for different instances, such as:

* `app.settings.json`
* `[instance01].app.settings.json`
* `[instance02].app.settings.json`

In this way, in case of the "instance01" service, will be loaded only the `app.settings.json` and `[instance01].app.settings.json` config files. To configure it the HostBox provides the way to set up configuration keys for accessing exact identifiers:

```json
"host": {
  "configFilter": {
    "fileRegexConfigKey": "configFilter:fileRegex",
    "fileRegexGroupsConfigKey": "configFilter:fileRegexGroups"
  }
}
```

Here `configFilter:fileRegex` is the configuration key that is used to access the regular expression that is used for filtering configuration files from the service's working directory. This expression may contain regex groups that can be used to refer to an element of the map defined in the configuration by the key defined in `configFilter:fileRegexGroups`. For instance, such the configuration can be defined in some "shared settings" which also are loaded on the host start up:

```json
"configFilter": {
  "fileRegex": "^(?:\\[(?<prefix>\\S*)\\]\\.)*[a-zA-Z\\d\\.]+.settings\\.json$",
  "fileRegexGroups": {
    "prefix": ["INSTANCE_KEY"]
  }
}
```

Here the `prefix` is the regex group. And for filtering the files by some value of the prefix, e.g. "instance01", the `INSTANCE_KEY` configuration should be defined, for instance as an environment variable. It is possible to define more than one acceptable value for the group, e.g. `["INSTANCE_KEY", "ENV_KEY"]`, so filtering will be done with OR operator.

```bash
export INSTANCE_KEY="instance01"
```

So, if the `INSTANCE_KEY` is defined with the value `instance01`, the host will load 2 files `app.settings.json` and `[instance01].app.settings.json`. The prefix is not required by the regular expression, so all the files without a prefix will be loaded too.

It is important that all these settings is optional.
