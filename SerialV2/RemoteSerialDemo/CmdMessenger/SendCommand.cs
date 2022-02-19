#region CmdMessenger - MIT - (c) 2013 Thijs Elenbaas.
/*
  CmdMessenger - library that provides command based messaging

  Permission is hereby granted, free of charge, to any person obtaining
  a copy of this software and associated documentation files (the
  "Software"), to deal in the Software without restriction, including
  without limitation the rights to use, copy, modify, merge, publish,
  distribute, sublicense, and/or sell copies of the Software, and to
  permit persons to whom the Software is furnished to do so, subject to
  the following conditions:

  The above copyright notice and this permission notice shall be
  included in all copies or substantial portions of the Software.

  Copyright 2013 - Thijs Elenbaas
*/
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CommandMessenger
{
    /// <summary> A command to be send by CmdMessenger </summary>
    public class SendCommand : Command
    {
        private readonly List<Action> _lazyArguments = new List<Action>();


        /// <summary> Indicates if we want to wait for an acknowledge command. </summary>
        /// <value> true if request acknowledge, false if not. </value>
        public bool ReqAc { get; set; }

        /// <summary> Gets or sets the acknowledge command ID. </summary>
        /// <value> the acknowledge command ID. </value>
        public int AckCmdId { get; set; }

        /// <summary> Gets or sets the time we want to wait for the acknowledge command. </summary>
        /// <value> The timeout on waiting for an acknowledge</value>
        public int Timeout { get; set; }

        /// <summary> Constructor. </summary>
        /// <param name="cmdId"> the command ID. </param>
        public SendCommand(int cmdId)
        {
            Init(cmdId, false, 0, 0);
        }

        /// <summary> Constructor. </summary>
        /// <param name="cmdId">    Command ID </param>
        /// <param name="argument"> The argument. </param>
        public SendCommand(int cmdId, string argument)
        {
            Init(cmdId, false, 0, 0);
            AddArgument(argument);
        }

        /// <summary> Constructor. </summary>
        /// <param name="cmdId">     Command ID </param>
        /// <param name="arguments"> The arguments. </param>
        public SendCommand(int cmdId, params object[] arguments)
        {
            Init(cmdId, false, 0, 0);
            foreach (var arg in arguments)
            {
                var typeCode = arg.GetTypeCode();
                var argType = arg.GetType();
                switch (typeCode)
                {
                    case TypeCode.Empty:
                    case TypeCode.DBNull: // Not supported
                        break;
                    case TypeCode.Object:
                        {
                            if (arg is IConvertible || argType == typeof(Version) || argType == typeof(Guid))
                                AddArgument(Escaping.Escape(Convert.ToString(arg, CultureInfo.InvariantCulture)));
                            else
                                throw new ArgumentException($"ArgumentType '{argType}' not supported.");
                        }
                        break;
                    case TypeCode.Boolean:
                        AddArgument((bool)arg ? "1" : "0"); break;
                    case TypeCode.Char:
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal: AddArgument(Escaping.Escape(Convert.ToString(arg,CultureInfo.InvariantCulture)));
                        break;
                    case TypeCode.DateTime:
                        AddArgument(Escaping.Escape(Convert.ToString(((DateTime)arg).ToFileTime(), CultureInfo.InvariantCulture)));
                        break;
                    case TypeCode.String:
                        AddArgument(Escaping.Escape((string)arg));
                        break;
                    default:
                        break;
                }

            }
        }

        /// <summary> Initializes this object. </summary>
        /// <param name="cmdId">    Command ID </param>
        /// <param name="reqAc">    true to request ac. </param>
        /// <param name="ackCmdId"> Acknowledge command ID. </param>
        /// <param name="timeout">  The timeout on waiting for an acknowledge</param>
        private void Init(int cmdId, bool reqAc, int ackCmdId, int timeout)
        {
            ReqAc = reqAc;
            CmdId = cmdId;
            AckCmdId = ackCmdId;
            Timeout = timeout;
        }

        // ***** String based **** /

        /// <summary> Adds a command argument.  </summary>
        /// <param name="argument"> The argument. </param>
        public void AddArgument(string argument)
        {
            if (argument != null)
                _lazyArguments.Add(() => CmdArgs.Add(argument));
        }

        /// <summary> Adds command arguments.  </summary>
        /// <param name="arguments"> The arguments. </param>
        public void AddArguments(string[] arguments)
        {
            if (arguments != null)
                _lazyArguments.Add(() => CmdArgs.AddRange(arguments));
        }

        /// <summary> Adds a command argument. </summary>
        /// <param name="argument"> The argument. </param>
        public void AddArgument(float argument)
        {
            _lazyArguments.Add(() => CmdArgs.Add(argument.ToString("R", CultureInfo.InvariantCulture)));
        }

        /// <summary> Adds a command argument. </summary>
        /// <param name="argument"> The argument. </param>
        public void AddArgument(Double argument)
        {
            _lazyArguments.Add(() =>
            {
                    // Not completely sure if this is needed for plain text sending.
                    var floatArg = (float) argument;
                    CmdArgs.Add(floatArg.ToString("R", CultureInfo.InvariantCulture));
            });
        }

        /// <summary> Adds a command argument. </summary>
        /// <param name="argument"> The argument. </param>
        public void AddArgument(Int16 argument)
        {
            _lazyArguments.Add(() => CmdArgs.Add(argument.ToString(CultureInfo.InvariantCulture)));
        }

        /// <summary> Adds a command argument. </summary>
        /// <param name="argument"> The argument. </param>
        public void AddArgument(UInt16 argument)
        {
            _lazyArguments.Add(() => CmdArgs.Add(argument.ToString(CultureInfo.InvariantCulture)));
        }

        /// <summary> Adds a command argument. </summary>
        /// <param name="argument"> The argument. </param>
        public void AddArgument(Int32 argument)
        {
            // Make sure the other side can read this: on a 16 processor, read as Long
            _lazyArguments.Add(() => CmdArgs.Add(argument.ToString(CultureInfo.InvariantCulture)));
        }

        /// <summary> Adds a command argument. </summary>
        /// <param name="argument"> The argument. </param>
        public void AddArgument(UInt32 argument)
        {
            // Make sure the other side can read this: on a 16 processor, read as Long
            _lazyArguments.Add(() => CmdArgs.Add(argument.ToString(CultureInfo.InvariantCulture)));
        }

        /// <summary> Adds a command argument. </summary>
        /// <param name="argument"> The argument. </param>
        public void AddArgument(bool argument)
        {
            AddArgument((Int16) (argument ? 1 : 0));
        }

        // ***** Binary **** /

        /// <summary> Adds a binary command argument.  </summary>
        /// <param name="argument"> The argument. </param>
        public void AddBinArgument(string argument)
        {
            _lazyArguments.Add(() => CmdArgs.Add(Escaping.Escape(argument)));
        }

        /// <summary> Adds a binary command argument. </summary>
        /// <param name="argument"> The argument. </param>
        public void AddBinArgument(float argument)
        {
            _lazyArguments.Add(() => CmdArgs.Add(BinaryConverter.ToString(argument)));
        }

        /// <summary> Adds a binary command argument. </summary>
        /// <param name="argument"> The argument. </param>
        public void AddBinArgument(Double argument)
        {
            _lazyArguments.Add(() => CmdArgs.Add(BinaryConverter.ToString((float)argument)));
        }

        /// <summary> Adds a binary command argument. </summary>
        /// <param name="argument"> The argument. </param>
        public void AddBinArgument(Int16 argument)
        {
            _lazyArguments.Add(() => CmdArgs.Add(BinaryConverter.ToString(argument)));
        }

        /// <summary> Adds a binary command argument. </summary>
        /// <param name="argument"> The argument. </param>
        public void AddBinArgument(UInt16 argument)
        {
            _lazyArguments.Add(() => CmdArgs.Add(BinaryConverter.ToString(argument)));
        }

        /// <summary> Adds a binary command argument. </summary>
        /// <param name="argument"> The argument. </param>
        public void AddBinArgument(Int32 argument)
        {
            _lazyArguments.Add(() => CmdArgs.Add(BinaryConverter.ToString(argument)));
        }

        /// <summary> Adds a binary command argument. </summary>
        /// <param name="argument"> The argument. </param>
        public void AddBinArgument(UInt32 argument)
        {
            _lazyArguments.Add(() => CmdArgs.Add(BinaryConverter.ToString(argument)));
        }

        /// <summary> Adds a binary command argument. </summary>
        /// <param name="argument"> The argument. </param>
        public void AddBinArgument(bool argument)
        {
            _lazyArguments.Add(() => CmdArgs.Add(BinaryConverter.ToString(argument ? (byte) 1 : (byte) 0)));
        }

        internal void InitArguments()
        {
            CmdArgs.Clear();
            foreach (var action in _lazyArguments)
            {
                action.Invoke();
            }
        }

        public override string ToString()
        {
            var commandString = new StringBuilder(CmdId.ToString(CultureInfo.InvariantCulture));
            InitArguments();
            foreach (var argument in Arguments)
            {
                commandString.Append(',').Append(argument);
            }
            commandString.Append(';');

            return commandString.ToString();
        }
    }
}

namespace System
{
    public static class TypeExtensions
    {
        private static readonly Dictionary<Type, TypeCode> TypeCodeMap =
            new Dictionary<Type, TypeCode>
            {
                {typeof(bool), TypeCode.Boolean},
                {typeof(byte), TypeCode.Byte},
                {typeof(sbyte), TypeCode.SByte},
                {typeof(char), TypeCode.Char},
                {typeof(DateTime), TypeCode.DateTime},
                {typeof(decimal), TypeCode.Decimal},
                {typeof(double), TypeCode.Double},
                {typeof(float), TypeCode.Single},
                {typeof(short), TypeCode.Int16},
                {typeof(int), TypeCode.Int32},
                {typeof(long), TypeCode.Int64},
                {typeof(ushort), TypeCode.UInt16},
                {typeof(uint), TypeCode.UInt32},
                {typeof(ulong), TypeCode.UInt64},
                {typeof(string), TypeCode.String}
            };
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static TypeCode GetTypeCode(this object obj)
        {
            if (obj == null)
                return TypeCode.Empty;

            TypeCode tc;
            var type = obj.GetType();
            if (!TypeCodeMap.TryGetValue(type, out tc))
            {
                tc = TypeCode.Object;
            }

            return tc;
        }
    }
}
