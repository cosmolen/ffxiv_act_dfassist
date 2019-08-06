﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Advanced_Combat_Tracker;

namespace DFAssist
{
    public class AssemblyResolver
    {
        private static AssemblyResolver _instance;
        public static AssemblyResolver Instance => _instance ?? (_instance = new AssemblyResolver());

        private bool _attached;
        private string _librariesPath;
        private ActPluginData _pluginData;

        public bool Attach(IActPluginV1 plugin)
        {
            if(_attached)
                return true;

            try
            {
                _pluginData = ActGlobals.oFormActMain.PluginGetSelfData(plugin);
                var enviroment = Path.GetDirectoryName(_pluginData.pluginFile.ToString());
                if(string.IsNullOrWhiteSpace(enviroment))
                    return  false;
            
                _librariesPath = Path.Combine(enviroment, "libs");
                if(!Directory.Exists(_librariesPath))
                    return false;

                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }
            catch (Exception)
            {
                Debug.WriteLine("There was an error when attaching to AssemblyResolve!");
                throw;
            }

            _attached = true;
            return true;
        }

        public void Detach()
        {
            if(!_attached)
                return;

            _attached = false;

            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            _librariesPath = null;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if(args.Name.Contains(".resources")
                || args.RequestingAssembly == null 
                || GetAssemblyName(args.RequestingAssembly.FullName) != nameof(DFAssist))
                return null;

            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if(assembly != null)
                return assembly;

            var filename = GetAssemblyName(args.Name) + ".dll".ToLower();
            var asmFile = Path.Combine(_librariesPath, filename);

            if (File.Exists(asmFile))
            {
                try 
                {
                    return Assembly.LoadFrom(asmFile);
                }
                catch(Exception) 
                {
                    _pluginData.lblPluginStatus.Text = $"Unable to load {args.Name} library, it may needs to be 'Unblocked'.";
                    return null;
                }
            }

            _pluginData.lblPluginStatus.Text = $"Unable to find {asmFile}, the plugin cannot be starterd.";
            return null;
        }

        private static string GetAssemblyName(string fullAssemblyName)
        {
            return fullAssemblyName.IndexOf(",", StringComparison.Ordinal) > -1
                ? fullAssemblyName.Substring(0, fullAssemblyName.IndexOf(",", StringComparison.Ordinal))
                : fullAssemblyName;
        }
    }
}
