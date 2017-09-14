using System;
using System.Collections.Generic;

namespace CRR
{
    /// <summary>
    /// This class is a wrapper for getting a list of named arguments from command line 
    /// </summary>
    class ArgumentParser
    {
        #region Variables    ------------------------------------------------
        Dictionary<string,string> _argCollection = new Dictionary<string, string>();
        #endregion

        #region Constructors ------------------------------------------------
        public ArgumentParser()
        {
        }

        public ArgumentParser(string[] args)
        {
            int i;
            string arg;
            for (i = 0; i < args.Length; i++)
            {
                if (args.Length > i)
                {
                    arg = args[i];
                    if (arg.IndexOf("-") == 0 && arg.Length > 1)
                    {
                        if (args.Length > i + 1)
                        {
                            if (args[i + 1].IndexOf("-") != 0)
                            {
                                _argCollection.Add(args[i].Substring(1), args[i + 1]);
                                i++;
                            }
                            else
                            {
                                _argCollection.Add(args[i].Substring(1), true.ToString());
                            }
                        }
                        else
                        {
                            _argCollection.Add(args[i].Substring(1), true.ToString());
                        }
                    }
                }
            }
        }
        #endregion

        #region Methods      ------------------------------------------------
        public T GetArgValue<T>(string argName)
        {
            T _retVal = default(T);
            foreach (var arg in _argCollection)
            {
                if (argName == arg.Key)
                {
                    _retVal = (T)Convert.ChangeType(arg.Value, typeof(T));
                }
            }
            return _retVal;
        }

        public T GetArgValue<T>(string argName, T defaultValue)
        {
            T _retVal = defaultValue;
            foreach (var arg in _argCollection)
            {
                if (argName == arg.Key)
                {
                    _retVal = (T)Convert.ChangeType(arg.Value, typeof(T));
                }
            }
            return _retVal;
        }

        public T GetArgValue<T>(string [] argAliases)
        {
            T _retVal = default(T);
            foreach (var arg in _argCollection)
            {
                foreach (string argName in argAliases)
                {
                    if (argName == arg.Key)
                    {
                        _retVal = (T)Convert.ChangeType(arg.Value, typeof(T));
                    }
                }
            }
            return _retVal;
        }

        public T GetArgValue<T>(string[] argAliases, T defaultValue)
        {
            T _retVal = defaultValue;
            foreach (var arg in _argCollection)
            {
                foreach (string argName in argAliases)
                {
                    if (argName == arg.Key)
                    {
                        _retVal = (T)Convert.ChangeType(arg.Value, typeof(T));
                    }
                }
            }
            return _retVal;
        }
        #endregion
    }
}
