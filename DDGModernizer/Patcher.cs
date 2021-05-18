using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;

namespace DDGModernizer
{
    class PatcherArg
    {
        public string name;
        public Type type;
        public int size;
        public object defaultVal;
        public object userVal;

        public void ConfigureType(string typeName, string typeSize)
        {
            int sz = int.Parse(typeSize);

            if (typeName == "float")
            {
                if (sz != 4)
                    throw new System.NotSupportedException("Float args must be 4 bytes in size");

                size = sz;
                type = typeof(float);
            }
            else if (typeName == "dec")
            {
                switch (sz)
                {
                    case 1:
                        type = typeof(char);
                        break;

                    case 2:
                        type = typeof(short);
                        break;

                    case 4:
                        type = typeof(int);
                        break;

                    case 8:
                        type = typeof(long);
                        break;

                    default:
                        throw new System.NotSupportedException("Decimal args must be 1, 2, 4, or 8 bytes in size");
                }

                size = sz;
            }
            else if (typeName == "str")
            {
                size = sz;
                type = typeof(string);
            }
            else
            {
                throw new System.NotSupportedException($"Unsupported arg type {typeName}");
            }
        }

        public void ConfigureDefaultVal(string val)
        {
            defaultVal = Convert.ChangeType(val, type);
        }

        public void SetUserVal(object val)
        {
            userVal = Convert.ChangeType(val, type);
        }
    }

    class PatcherInjection
    {
        public uint offset;
        public string searchPattern;
        public string injectPattern;
    }

    class PatcherModule
    {
        public string name;

        public List<PatcherArg> argList = new List<PatcherArg>();
        public List<PatcherInjection> injectionList = new List<PatcherInjection>();

        public Dictionary<string, PatcherArg> argMap = new Dictionary<string, PatcherArg>();

        public PatcherModule(string name)
        {
            this.name = name;
        }
    }

    class Patcher
    {
        private DDGVersion mVersion;

        public List<PatcherModule> moduleList = new List<PatcherModule>();
        public Dictionary<string, PatcherModule> moduleMap = new Dictionary<string, PatcherModule>();

        public const string MODULE_KEY_ASPECT = "Aspect";
        public const string MODULE_KEY_DRAW_DISTANCE = "DrawDistance";

        public static Regex REGEX_MODULE_NAME = new Regex(@"\[([A-Za-z0-9_ ]+)\]");
        public static Regex REGEX_ARG_DECL = new Regex(@"<(?<ArgName>[A-Za-z0-9-_]+),(?<ArgType>float|dec|str),(?<ArgSize>\d+),(?<DefaultVal>[^,>]+)(?:,(?<Comment>[^,>]+))?>");

        public DDGVersion Version
        {
            get { return mVersion; }
        }

        public Patcher(DDGVersion version)
        {
            if (version == DDGVersion.UNKNOWN)
            {
                throw new System.NotSupportedException("Version is required for Patcher.");
            }

            mVersion = version;

            ParsePatchConfig();
        }

        public void SetModuleParam(string moduleName, string paramName, object value)
        {
            if (moduleMap.ContainsKey(moduleName) == false)
                return;

            PatcherModule module = moduleMap[moduleName];
            if (module.argMap.ContainsKey(paramName) == false)
                return;

            module.argMap[paramName].SetUserVal(value);
        }

        public void PatchAndRun()
        {

        }

        #region Patch Config

        string ReadResource(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();

            try
            {
                string resourcePath = resourceNames.Single(str => str.EndsWith(name));

                using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch
            {
                return null;
            }
        }

        string ResourceNameForVersion(DDGVersion version)
        {
            return $"{version}.txt";
        }

        void ParsePatchConfig()
        {
            string resourceName = ResourceNameForVersion(Version);
            string config = ReadResource(resourceName);
            if (config == null)
            {
                throw new System.NotSupportedException($"No patch config found for selected Version {Version}");
            }

            // Get all lines, filtering out comments and whitespace
            char[] splitChars = new char[] { '\n' };
            List<string> lines = config.Split(splitChars)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0 )
                .Where(x => x.StartsWith(";") == false )
                .ToList();

            PatcherModule currentModule = null;
            foreach (var line in lines)
            {
                char triggerChar = line[0];
                switch (triggerChar)
                {
                    case '[':
                        {
                            // Start module

                            if (REGEX_MODULE_NAME.IsMatch(line) == false)
                                throw new System.NotSupportedException("Malformed module name line");

                            var match = REGEX_MODULE_NAME.Match(line);
                            string moduleName = match.Groups[1].Value;

                            if (moduleMap.ContainsKey(moduleName))
                                throw new System.NotSupportedException("Repeat declaration of patcher module");

                            PatcherModule module = new PatcherModule(moduleName);
                            moduleList.Add(module);
                            moduleMap[moduleName] = module;
                            currentModule = module;

                            break;
                        }

                    case '<':
                        {
                            // Start arg declaration
                            if (currentModule == null)
                                throw new System.NotSupportedException("Argument declared without patch module");

                            if (REGEX_ARG_DECL.IsMatch(line) == false)
                                throw new System.NotSupportedException("Malformed arg declaration");

                            var match = REGEX_ARG_DECL.Match(line);
                            var argName = match.Groups["ArgName"].Value;

                            if (currentModule.argMap.ContainsKey(argName) == true)
                                throw new System.NotSupportedException("Repeat declaration of argument");

                            PatcherArg arg = new PatcherArg();
                            arg.name = argName;
                            arg.ConfigureType(match.Groups["ArgType"].Value, match.Groups["ArgSize"].Value);
                            arg.ConfigureDefaultVal(match.Groups["DefaultVal"].Value);

                            currentModule.argList.Add(arg);
                            currentModule.argMap[argName] = arg;

                            break;
                        }

                    case '$':
                        {
                            // Start injection declaration
                            if (currentModule == null)
                                throw new System.NotSupportedException("Injection declared without patch module");

                            string injCmd = line.Substring(1);

                            // Split by comma
                            var parts = injCmd.Split(',').ToList();
                            if (parts.Count != 3)
                                throw new System.NotSupportedException("Malformed injection declaration");

                            string partOffset = parts[0].Trim();
                            string partSearch = parts[1].Trim();
                            string partReplace = parts[2].Trim();

                            PatcherInjection injection = new PatcherInjection();
                            injection.offset = uint.Parse(partOffset, System.Globalization.NumberStyles.HexNumber);
                            injection.searchPattern = partSearch;
                            injection.injectPattern = partReplace;

                            currentModule.injectionList.Add(injection);

                            break;
                        }

                    default:
                        {
                            throw new System.NotSupportedException("Unsupported trigger char in patch config");
                        }
                }
            }
        }

        #endregion
    }
}
