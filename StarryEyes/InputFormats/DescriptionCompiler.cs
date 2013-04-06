using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using StarryEyes.Albireo;

namespace StarryEyes.InputFormats
{
    public static class DescriptionCompiler
    {
        private static SortedDictionary<string, Type> _functionsResolver = new SortedDictionary<string, Type>();

        public static InputFormatFunctionBase FindFunction(string id)
        {
            Type type;
            if (_functionsResolver.TryGetValue(id, out type))
            {
                return (InputFormatFunctionBase)Activator.CreateInstance(type);
            }
            return null;
        }

        internal static void RegisterInternalAssembly()
        {
            RegisterAssembly(Assembly.GetExecutingAssembly());
        }

        public static void RegisterAssembly(Assembly asm)
        {
            var basetype = typeof(InputFormatFunctionBase);
            asm.GetTypes()
               .Where(t => basetype.IsAssignableFrom(t) && !t.IsAbstract && t.IsPublic)
               .ForEach(RegisterType);
        }

        public static void RegisterType<T>() where T : InputFormatFunctionBase
        {
            RegisterType(typeof(T));
        }

        public static void RegisterType(Type type)
        {
            var basetype = typeof(InputFormatFunctionBase);
            if (!basetype.IsAssignableFrom(type))
            {
                throw new ArgumentException("Type " + type.FullName + " is not assignable to InputFormatFunctionBase.");
            }
            Register((InputFormatFunctionBase)Activator.CreateInstance(type));
        }

        public static void Register(InputFormatFunctionBase expr)
        {
            _functionsResolver[expr.Name] = expr.GetType();
        }

        public static FormatDescription Compile(string text)
        {
            var desc = new FormatDescription();
            var builder = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '(' && (i == 0 || text[i - 1] != '\\'))
                {
                    var build = builder.ToString();
                    if (!build.IsNullOrEmpty())
                    {
                        desc.AddElement(new TextFormatElement(build));
                        builder.Clear();
                    }
                    desc.AddElement(new ExpressionElement(ParseBracket(desc, text, i, out i)));
                }
                else
                {
                    builder.Append(text[i]);
                }
            }
            var trail = builder.ToString();
            if (!trail.IsNullOrEmpty())
            {
                desc.AddElement(new TextFormatElement(trail));
            }
            return desc;
        }

        private static InputFormatFunctionBase ParseBracket(FormatDescription desc, string text, int index, out int next)
        {
            var providers = new List<IInputFormatValueProvider>();
            for (var i = index + 1; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case '"':
                        providers.Add(new StringValueProvider(ParseQuotes(text, i, out i)));
                        break;
                    case '\t':
                    case ' ':
                        if (i - (index + 1) > 0)
                        {
                            var ctoken = text.Substring(index + 1, (i - (index + 1)));
                            if (ctoken.Any(c => (c < '0' && c > '9') || c == '.'))
                            {
                                // raw token
                                providers.Add(new FunctionTokenProvider(ctoken));
                            }
                            else
                            {
                                // numeric value
                                switch (ctoken.Count(c => c == '.'))
                                {
                                    case 0:
                                        // int value
                                        providers.Add(new IntegerValueProvider(int.Parse(ctoken)));
                                        break;
                                    case 1:
                                        // double value
                                        providers.Add(new DoubleValueProvider(double.Parse(ctoken)));
                                        break;
                                    case 2:
                                        // invalid
                                        throw new Exception("invalid token: " + ctoken);
                                }
                            }
                        }
                        index = i;
                        break;
                    case '(':
                        providers.Add(new ProxyValueProvider(ParseBracket(desc, text, i, out i)));
                        break;
                    case ')':
                        // genereate current clause
                        next = i + 1;
                        if (providers.Count == 0)
                        {
                            throw new Exception("Bracket is empty: index " + index + "-");
                        }
                        var func = providers[0] as FunctionTokenProvider;
                        if (func == null)
                        {
                            throw new Exception("First element of this bracket is not function: " + providers[0]);
                        }
                        var funcBody = func.GetValue<InputFormatFunctionBase>();
                        funcBody.Owner = desc;
                        var input = funcBody as FunctionInput;
                        if (input != null)
                        {
                            // input special handling
                            var num = providers.Skip(1).ToArray();
                            if (num.Length == 1 && num[0] is IntegerValueProvider)
                            {
                                desc.RegisterId(num[0].GetValue<int>());
                            }
                            else
                            {
                                throw new Exception("input parameter is not matched. input accepts static integer value only.");
                            }
                        }
                        funcBody.SetArguments(providers.Skip(1).ToArray());
                        return funcBody;
                }
            }
            throw new Exception("Bracket is not end: \"" + text.Substring(index) + "\" , index " + index + "-");
        }

        private static string ParseQuotes(string text, int index, out int next)
        {
            for (var i = index + 1; i < text.Length; i++)
            {
                if (text[i] != '"' || text[i - 1] == '\\') continue;
                //return
                next = i + 1;
                return text.Substring(index + 1, i - (index + 1));
            }
            throw new Exception("String is not end: \"" + text.Substring(index) + "\" , index " + index + "-");
        }
    }
}
