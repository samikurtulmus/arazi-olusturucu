using Autodesk.AutoCAD.Runtime;
using YapiLabCadTools.Plugin;

// Telling AutoCAD where the entry point and the commands live makes NETLOAD
// noticeably faster: the runtime skips scanning every type in the assembly.
[assembly: ExtensionApplication(typeof(PluginExtension))]
[assembly: CommandClass(typeof(Commands))]
