using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TeleSharp.Generator
{
    class Program
    {
        static List<String> keywords = new List<string>(new string[] { "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while", "add", "alias", "ascending", "async", "await", "descending", "dynamic", "from", "get", "global", "group", "into", "join", "let", "orderby", "partial", "partial", "remove", "select", "set", "value", "var", "where", "where", "yield" });
        static List<String> interfacesList = new List<string>();
        static List<String> classesList = new List<string>();


        static void Main(string[] args)
        {
            string AbsStyle = File.ReadAllText("ConstructorAbs.tmp");
            string NormalStyle = File.ReadAllText("Constructor.tmp");
            string MethodStyle = File.ReadAllText("Method.tmp");
            string Json = "";
            string url;
            if (args.Count() == 0) url = "tl-schema.json"; else url = args[0];

            Json = File.ReadAllText(url);
            Schema schema = JsonConvert.DeserializeObject<Schema>(Json);
            foreach (var c in schema.constructors)
            {
                interfacesList.Add(c.type);
                classesList.Add(c.predicate);
            }
            foreach (var c in schema.constructors)
            {
                var list = schema.constructors.Where(x => x.type == c.type);
                if (list.Count() > 1)
                {
                    string path = (GetNameSpace(c.type).Replace("TeleSharp.TL", "TL\\").Replace(".", "") + "\\" + GetNameofClass(c.type, true) + ".cs").Replace("\\\\", "\\");
                    string nspace = (GetNameSpace(c.type).Replace("TeleSharp.TL", "TL\\").Replace(".", "")).Replace("\\\\", "\\").Replace("\\", ".");
                    if (nspace.EndsWith("."))
                        nspace = nspace.Remove(nspace.Length - 1, 1);
                    string temp = AbsStyle.Replace("/* NAMESPACE */", "TeleSharp." + nspace);
                    temp = temp.Replace("/* NAME */", GetNameofClass(c.type, true));
                    SetFileContents(path, temp);
                }
                else
                {
                    interfacesList.Remove(list.First().type);
                    list.First().type = "himself";
                }
            }
            foreach (var c in schema.constructors)
            {
                string path = (GetNameSpace(c.predicate).Replace("TeleSharp.TL", "TL\\").Replace(".", "") + "\\" + GetNameofClass(c.predicate, false) + ".cs").Replace("\\\\", "\\");
                
                #region About Class
                string nspace = (GetNameSpace(c.predicate).Replace("TeleSharp.TL", "TL\\").Replace(".", "")).Replace("\\\\", "\\").Replace("\\", ".");
                if (nspace.EndsWith("."))
                    nspace = nspace.Remove(nspace.Length - 1, 1);
                string temp = NormalStyle.Replace("/* NAMESPACE */", "TeleSharp." + nspace);
                temp = (c.type == "himself") ? temp.Replace("/* PARENT */", "TLObject") : temp.Replace("/* PARENT */", GetNameofClass(c.type, true));
                temp = temp.Replace("/*Constructor*/", c.id.ToString());
                temp = temp.Replace("/* NAME */", GetNameofClass(c.predicate, false));
                #endregion
                
                #region Fields
                temp = temp.Replace("/* PARAMS */", GetFields(c.Params).GetCode());
                #endregion
                
                #region ComputeFlagFunc
                temp = temp.Replace("/* COMPUTE */", GetComputeFlagsCode(c.Params).GetCode());
                #endregion
                
                #region SerializeFunc
                temp = temp.Replace("/* SERIALIZE */", GetSerializeCode(c.Params).GetCode());
                #endregion
                
                #region DeSerializeFunc
                temp = temp.Replace("/* DESERIALIZE */", GetDeserializeCode(c.Params).GetCode());
                #endregion
                    
                SetFileContents(path, temp);
            }
            foreach (var c in schema.methods)
            {
                string path = (GetNameSpace(c.method).Replace("TeleSharp.TL", "TL\\").Replace(".", "") + "\\" + GetNameofClass(c.method, false, true) + ".cs").Replace("\\\\", "\\");

                #region About Class
                string nspace = (GetNameSpace(c.method).Replace("TeleSharp.TL", "TL\\").Replace(".", ""))
                    .Replace("\\\\", "\\").Replace("\\", ".");
                if (nspace.EndsWith("."))
                    nspace = nspace.Remove(nspace.Length - 1, 1);
                string temp = MethodStyle.Replace("/* NAMESPACE */", "TeleSharp." + nspace);
                temp = temp.Replace("/* PARENT */", "TLMethod");
                temp = temp.Replace("/*Constructor*/", c.id.ToString());
                temp = temp.Replace("/* NAME */", GetNameofClass(c.method, false, true));
                #endregion

                #region Fields
                var fields = GetFields(c.Params);
                fields += $"public {CheckForFlagBase(c.type, GetTypeName(c.type))} Response " + "{ get; set; }";
                temp = temp.Replace("/* PARAMS */", fields.GetCode());
                #endregion

                #region ComputeFlagFunc
                temp = temp.Replace("/* COMPUTE */", GetComputeFlagsCode(c.Params).GetCode());
                #endregion

                #region SerializeFunc
                temp = temp.Replace("/* SERIALIZE */", GetSerializeCode(c.Params).GetCode());
                #endregion

                #region DeSerializeFunc
                temp = temp.Replace("/* DESERIALIZE */", GetDeserializeCode(c.Params).GetCode());
                #endregion

                #region DeSerializeRespFunc
                var codeGenerator = new CodeGenerator {
                    IndentationLevel = 3
                };
                var p2 = new Param {name = "Response", type = c.type};
                GetDeserializeCode(codeGenerator, p2);
                temp = temp.Replace("/* DESERIALIZEResp */", codeGenerator.GetCode());
                #endregion

                SetFileContents(path, temp);
            }
        }

        private static CodeGenerator GetComputeFlagsCode(List<Param> parameters) {
            var cg = new CodeGenerator {
                IndentationLevel = 3
            };

            if (parameters.All(x => x.name.ToLower() != "flags")) return cg;
            
            var compute = new CodeGenerator {
                IndentationLevel = 3
            };
            compute += "Flags = 0;";

            foreach (var param in parameters.Where(x => IsFlagBase(x.type))) {
                if (IsTrueFlag(param.type)) {
                    compute +=
                        $"Flags = {CheckForKeywordAndPascalCase(param.name)} " +
                        $"? (Flags | {GetBitMask(param.type)}) " +
                        $": (Flags & ~{GetBitMask(param.type)});";
                }
                else {
                    compute +=
                        $"Flags = {CheckForKeywordAndPascalCase(param.name)} != null " +
                        $"? (Flags | {GetBitMask(param.type)}) " +
                        $": (Flags & ~{GetBitMask(param.type)});";
                }
            }

            return compute;
        }

        public static CodeGenerator GetFields(List<Param> parameters) {
            var fields = new CodeGenerator {
                IndentationLevel = 2
            };
            foreach (var tmp in parameters)
            {
                fields += $"public {CheckForFlagBase(tmp.type, GetTypeName(tmp.type))} {CheckForKeywordAndPascalCase(tmp.name)} " + "{ get; set; }";
            }
            return fields;
        }
        
        public static string FormatName(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Can not format an empty name.", nameof(input));
            if (input.IndexOf('.') != -1)
            {
                input = input.Replace(".", " ");
                var temp = "";
                foreach (var s in input.Split(' '))
                {
                    temp += FormatName(s) + " ";
                }
                input = temp.Trim();
            }
            return input.First().ToString().ToUpper() + input.Substring(1);
        }
        
        public static string CheckForKeywordAndPascalCase(string name)
        {
            name = name.Replace("_", " ");
            name = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);
            name = name.Replace(" ", "");

            if (keywords.Contains(name)) return "@" + name;
            return name;
        }
        
        public static string GetNameofClass(string type, bool isinterface = false, bool ismethod = false)
        {
            if (!ismethod)
            {
                if (type.IndexOf('.') != -1 && type.IndexOf('?') == -1)
                    return isinterface ? "TLAbs" + FormatName(type.Split('.')[1]) : "TL" + FormatName(type.Split('.')[1]);
                else if (type.IndexOf('.') != -1 && type.IndexOf('?') != -1)
                    return isinterface ? "TLAbs" + FormatName(type.Split('?')[1]) : "TL" + FormatName(type.Split('?')[1]);
                else
                    return isinterface ? "TLAbs" + FormatName(type) : "TL" + FormatName(type);
            }
            else
            {
                if (type.IndexOf('.') != -1 && type.IndexOf('?') == -1)
                    return "TLRequest" + FormatName(type.Split('.')[1]);
                else if (type.IndexOf('.') != -1 && type.IndexOf('?') != -1)
                    return "TLRequest" + FormatName(type.Split('?')[1]);
                else
                    return "TLRequest" + FormatName(type);
            }
        }
        
        private static bool IsFlagBase(string type)
        {
            return type.Contains("?");
        }
        
        private static int GetBitMask(string type)
        {
            return (int)Math.Pow((double)2, (double)int.Parse(type.Split('?')[0].Split('.')[1]));
        }
        
        private static bool IsTrueFlag(string type)
        {
            return type.Split('?')[1] == "true";
        }
        
        public static string GetNameSpace(string type)
        {
            if (type.IndexOf('.') != -1)
                return "TeleSharp.TL" + FormatName(type.Split('.')[0]);
            else
                return "TeleSharp.TL";
        }
        
        public static string CheckForFlagBase(string type, string result)
        {
            if (type.IndexOf('?') == -1)
                return result;
            else
            {
                string innerType = type.Split('?')[1];
                if (innerType == "true") return result;
                else if ((new string[] { "bool", "int", "uint", "long", "double" }).Contains(result)) return result + "?";
                else return result;
            }
        }
        
        public static string GetTypeName(string type)
        {
            switch (type.ToLower())
            {
                case "#":
                case "int":
                    return "int";
                case "uint":
                    return "uint";
                case "long":
                    return "long";
                case "double":
                    return "double";
                case "string":
                    return "string";
                case "bytes":
                    return "byte[]";
                case "true":
                case "bool":
                    return "bool";
                case "!x":
                    return "TLObject";
                case "x":
                    return "TLObject";
            }

            if (type.StartsWith("Vector"))
                return "TLVector<" + GetTypeName(type.Replace("Vector<", "").Replace(">", "")) + ">";

            if (type.ToLower().Contains("inputcontact"))
                return "TLInputPhoneContact";


            if (type.IndexOf('.') != -1 && type.IndexOf('?') == -1)
            {

                if (interfacesList.Any(x => x.ToLower() == (type).ToLower()))
                    return FormatName(type.Split('.')[0]) + "." + "TLAbs" + type.Split('.')[1];
                else if (classesList.Any(x => x.ToLower() == (type).ToLower()))
                    return FormatName(type.Split('.')[0]) + "." + "TL" + type.Split('.')[1];
                else
                    return FormatName(type.Split('.')[1]);
            }
            else if (type.IndexOf('?') == -1)
            {
                if (interfacesList.Any(x => x.ToLower() == type.ToLower()))
                    return "TLAbs" + type;
                else if (classesList.Any(x => x.ToLower() == type.ToLower()))
                    return "TL" + type;
                else
                    return type;
            }
            else
            {
                return GetTypeName(type.Split('?')[1]);
            }
        }
        
        public static string LookTypeInLists(string src)
        {
            if (interfacesList.Any(x => x.ToLower() == src.ToLower()))
                return "TLAbs" + FormatName(src);
            else if (classesList.Any(x => x.ToLower() == src.ToLower()))
                return "TL" + FormatName(src);
            else
                return src;
        }

        public static CodeGenerator GetSerializeCode(List<Param> parameters) {
            var serialize = new CodeGenerator {
                IndentationLevel = 3
            };
            if (parameters.Any(x => x.name.ToLower() == "flags")) {
                serialize += "ComputeFlags();";
                serialize += "bw.Write(Flags);";
            }

            foreach (var p in parameters.Where(x => x.name.ToLower() != "flags"))
            {
                serialize += GetSerializeCode(p);
            }
            return serialize;
        }
        
        public static string GetSerializeCode(Param p, bool flag = false)
        {
            switch (p.type.ToLower())
            {
                case "#":
                case "int":
                    return flag ? $"bw.Write({CheckForKeywordAndPascalCase(p.name)}.Value);" : $"bw.Write({CheckForKeywordAndPascalCase(p.name)});";
                case "long":
                    return flag ? $"bw.Write({CheckForKeywordAndPascalCase(p.name)}.Value);" : $"bw.Write({CheckForKeywordAndPascalCase(p.name)});";
                case "string":
                    return $"StringUtil.Serialize({CheckForKeywordAndPascalCase(p.name)}, bw);";
                case "bool":
                    return flag ? $"BoolUtil.Serialize({CheckForKeywordAndPascalCase(p.name)}.Value,bw);" : $"BoolUtil.Serialize({CheckForKeywordAndPascalCase(p.name)},bw);";
                case "true":
                    return $"BoolUtil.Serialize({CheckForKeywordAndPascalCase(p.name)},bw);";
                case "bytes":
                    return $"BytesUtil.Serialize({CheckForKeywordAndPascalCase(p.name)}, bw);";
                case "double":
                    return flag ? $"bw.Write({CheckForKeywordAndPascalCase(p.name)}.Value);" : $"bw.Write({CheckForKeywordAndPascalCase(p.name)});";
                default:
                    if (!IsFlagBase(p.type))
                        return $"ObjectUtils.SerializeObject({CheckForKeywordAndPascalCase(p.name)}, bw);";
                    else
                    {
                        if (IsTrueFlag(p.type))
                            return $"";
                        else
                        {
                            var p2 = new Param() { name = p.name, type = p.type.Split('?')[1] };
                            return $"if ((Flags & {GetBitMask(p.type).ToString()}) != 0) " + GetSerializeCode(p2, true);
                        }
                    }
            }
        }

        public static CodeGenerator GetDeserializeCode(List<Param> parameters) {
            var deserialize = new CodeGenerator {
                IndentationLevel = 3
            };
            foreach (var p in parameters)
            {
                GetDeserializeCode(deserialize, p);
            }
            return deserialize;
        }
        
        public static void GetDeserializeCode(CodeGenerator cg, Param p)
        {
            switch (p.type.ToLower())
            {
                case "#":
                case "int":
                    cg.WriteLine($"{CheckForKeywordAndPascalCase(p.name)} = br.ReadInt32();");
                    break;
                case "long":
                    cg.WriteLine($"{CheckForKeywordAndPascalCase(p.name)} = br.ReadInt64();");
                    break;
                case "string":
                    cg.WriteLine($"{CheckForKeywordAndPascalCase(p.name)} = StringUtil.Deserialize(br);");
                    break;
                case "bool":
                case "true":
                    cg.WriteLine($"{CheckForKeywordAndPascalCase(p.name)} = BoolUtil.Deserialize(br);");
                    break;
                case "bytes":
                    cg.WriteLine($"{CheckForKeywordAndPascalCase(p.name)} = BytesUtil.Deserialize(br);");
                    break;
                case "double":
                    cg.WriteLine($"{CheckForKeywordAndPascalCase(p.name)} = br.ReadDouble();");
                    break;
                default:
                    if (!IsFlagBase(p.type))
                    {
                        if (p.type.ToLower().Contains("vector"))
                        {
                            cg.WriteLine($"{CheckForKeywordAndPascalCase(p.name)} = ({GetTypeName(p.type)})ObjectUtils.DeserializeVector<{GetTypeName(p.type).Replace("TLVector<", "").Replace(">", "")}>(br);");
                        }
                        else {
                            cg.WriteLine($"{CheckForKeywordAndPascalCase(p.name)} = ({GetTypeName(p.type)})ObjectUtils.DeserializeObject(br);");
                        }
                    }
                    else
                    {
                        if (IsTrueFlag(p.type)) {
                            cg.WriteLine($"{CheckForKeywordAndPascalCase(p.name)} = (Flags & {GetBitMask(p.type).ToString()}) != 0;");
                        }
                        else
                        {
                            var p2 = new Param { name = p.name, type = p.type.Split('?')[1] };
                            cg.WriteLine($"if ((Flags & {GetBitMask(p.type).ToString()}) != 0)");
                            {
                                cg.IndentationLevel += 1;
                                GetDeserializeCode(cg, p2);
                                cg.IndentationLevel -= 1;
                            }
                            cg.WriteLine("else");
                            {
                                cg.IndentationLevel += 1;
                                cg.WriteLine($"{CheckForKeywordAndPascalCase(p.name)} = null;");
                                cg.IndentationLevel -= 1;
                            }
                        }
                    }
                    break;
            }
        }
        
        public static void SetFileContents(string filepath, string contents) {
            var directoryName = Path.GetDirectoryName(filepath);
            if (!Directory.Exists(directoryName) && directoryName != null)
                Directory.CreateDirectory(directoryName);
            File.WriteAllText(filepath, contents);
        }
    }
}