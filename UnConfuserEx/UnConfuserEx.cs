using System;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using System.Collections.Generic;
using UnConfuserEx.Protections;
using log4net;
using log4net.Config;
using UnConfuserEx.Protections.Delegates;
using UnConfuserEx.Protections.AntiDebug;
using UnConfuserEx.Protections.AntiDump;

namespace UnConfuserEx
{
    internal class UnConfuserEx
    {
        static ILog Logger = LogManager.GetLogger("UnConfuserEx");

        static int Main(string[] args)
        {
            XmlConfigurator.Configure(typeof(UnConfuserEx).Assembly.GetManifestResourceStream("UnConfuserEx.log4net.xml"));

            if (args.Length < 1 || args.Length > 2)
            {
                Logger.Error("Usage: unconfuser.exe <module path> <output path>");
                return 1;
            }

            var path = args[0];
            if (!File.Exists(path))
            {
                Logger.Error($"File {path} does not exist");
                return 1;
            }

            // Load the module
            ModuleContext context = new();
            ModuleDefMD module = ModuleDefMD.Load(path, context);

            var pipeline = new List<IProtection>
            {
                // If this is present, it MUST be removed first
                new AntiTamperRemover(),

                new ControlFlowRemover(),
                new ResourcesRemover(),
                new ConstantsRemover(),
                new RefProxyRemover(),
                new AntiDumpRemover(),
                new AntiDebugRemover(),
                new UnicodeRemover(),

            };

            foreach (var p in pipeline)
            {
                if (p.IsPresent(ref module))
                {
                    Logger.Info($"{p.Name} detected, attempting to remove");
                    try
                    {
                        if (p.Remove(ref module))
                        {
                            Logger.Info($"Successfully removed {p.Name} protection");
                        }
                        else
                        {
                            Logger.Error($"Failed to remove {p.Name} protection");
                            return 1;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Fatal($"Caught exception when trying to remove {p.Name} protection");
                        Logger.Error(ex.ToString());
                        return 1;
                    }
                }
            }

            var newPath = $"{Path.GetDirectoryName(path)}\\{Path.GetFileNameWithoutExtension(path)}-deobfuscated{Path.GetExtension(path)}";
            if (args.Length == 2)
            {
                // Use the user supplied path
                newPath = args[1];
            }

            // Write the module back
            Logger.Info($"All detected protections removed. Writing new module to {newPath}");

            try
            {
                if (module.IsILOnly)
                {
                    ModuleWriterOptions writerOptions = new ModuleWriterOptions(module);
                    module.Write(newPath, writerOptions);
                }
                else
                {
                    NativeModuleWriterOptions writerOptions = new NativeModuleWriterOptions(module, true);
                    //writerOptions.MetadataOptions.Flags = MetadataFlags.PreserveAll;
                    module.NativeWrite(newPath, writerOptions);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to write module");
                Logger.Error(ex.ToString());
                return 1;
            }
            Logger.Info("Deobfuscated module successfully written");

            // Done
            return 0;
        }

    }
}