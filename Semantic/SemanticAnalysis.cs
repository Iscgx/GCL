using System;
using System.Collections.Generic;
using System.Linq;

namespace Semantic
{
    public partial class SemanticAnalysis
    {
        private readonly GlobalSymbolTable globalSymbolTable;
        private Function actualFunction;
        private readonly List<Block> blocks;

        //HardCoded Types
        public readonly Dictionary<string, Type> DefinedTypes;
        private readonly Dictionary<string, StructureType> definedStructures; 
        private readonly Dictionary<string, ArrayType> definedArrays; 

        private readonly Dictionary<Type, string> stringByType; 
        public bool SemanticError = false;
        public ThrowErrorDelegate ThrowError;

        public string GetTypeName(Type type)
        {
            if (this.stringByType.ContainsKey(type))
                return this.stringByType[type];
            else
                return "error";
        }

        public SemanticAnalysis()
        {
            this.globalSymbolTable = new GlobalSymbolTable();
            //_actualFunction = null;
            this.blocks = new List<Block>();
            this.ThrowError += Error;

            this.DefinedTypes = new Dictionary<string, Type>
                { 
                    {"int", new Type(4, 4, TypeParameters.IsInteger)},
                    {"float", new Type(4, 6, TypeParameters.IsArithmetic)},
                    {"char", new Type(1, 2, TypeParameters.IsInteger)},
                    {"bool", new Type(1, 1, TypeParameters.None)},
                    {"double", new Type(8, 7, TypeParameters.IsArithmetic)},
                    {"long_double", new Type(16, 8, TypeParameters.IsArithmetic)},
                    {"short_int", new Type(2, 3, TypeParameters.IsInteger)},
                    {"long_int", new Type(8, 5, TypeParameters.IsInteger)},
                    {"string", new Type(-1, 9, TypeParameters.None)},
                    {"unsigned_int", new Type(4, 4, TypeParameters.IsInteger | TypeParameters.IsUnsigned)},
                    {"unsigned_short_int", new Type(2, 3, TypeParameters.IsInteger | TypeParameters.IsUnsigned)},
                    {"unsigned_long_int", new Type(8, 5, TypeParameters.IsInteger | TypeParameters.IsUnsigned)}
                };
            this.definedStructures = new Dictionary<string, StructureType>();
            this.definedArrays = new Dictionary<string, ArrayType>();
            this.stringByType = new Dictionary<Type, string>();
            foreach (var definedType in this.DefinedTypes)
                this.stringByType.Add(definedType.Value, definedType.Key);
            //HardCoded Types
        }

        public Type GetDefinedType(string name)
        {
            if (this.DefinedTypes.ContainsKey(name))
                return this.DefinedTypes[name];
            return null;
        }

        public int BlocksOpened()
        {
            return this.blocks.Count;
        }

        public void CloseFunction()
        {
            this.actualFunction = null;
        }

        public void CheckReturnType(string type)
        {
            if (this.actualFunction.ReturnType.Equals(type) == false)
                Error("Return Type Expression doesn't match Function's return type");
        }

        private void Error(string message, params object[] parameters)
        {
            Console.WriteLine(message, parameters);
            this.SemanticError = true;
        }

        public bool UseVariable(string name, bool atDevice)
        {
            ISymbolTable globalTable;
            if (atDevice)
                globalTable = this.globalSymbolTable.DeviceSymbolTable;
            else
                globalTable = this.globalSymbolTable;

            if (this.blocks.Any())
            {
                for (var i = this.blocks.Count() - 1; i >= 0; i--)
                {
                    if (this.blocks[i].Variables.ContainsKey(name) && this.blocks[i].Variables[name].Type.Parameters != TypeParameters.IsStructure) //Variable found!!
                        return true;
                }
            }
            if (globalTable.Variables.ContainsKey(name) && globalTable.Variables[name].Type.Parameters != TypeParameters.IsStructure) //Variable Found!!!
            {
                return true;
            }

            return false;
            
        }

        public string StructureInstanceExists(string structureInstanceName, Stack<string> structureField, bool atDevice)
        {
            var fieldList = structureField.ToList();
            if (this.blocks.Any())
            {
                for (var i = this.blocks.Count() - 1; i >= 0; i--)
                {
                    if (this.blocks[i].Variables.ContainsKey(structureInstanceName) &&
                        (this.blocks[i].Variables[structureInstanceName].Type.Parameters & TypeParameters.IsStructure) == TypeParameters.IsStructure)
                    {
                        var structure = this.definedStructures[this.stringByType[this.blocks[i].Variables[structureInstanceName].Type]];
                        for (var j = 0; j < fieldList.Count;j++)
                        {
                            if (!structure.Variables.ContainsKey(fieldList[j])) continue;
                            var variable = structure.Variables[fieldList[j]];
                                if (variable.Type.Parameters != TypeParameters.IsStructure && j < fieldList.Count() - 1)
                                    return null;
                                if(j < fieldList.Count - 1 && this.definedStructures.ContainsKey(this.stringByType[variable.Type]))
                                    structure = this.definedStructures[this.stringByType[variable.Type]];
                            }
                        if (structure.Variables.ContainsKey(fieldList[fieldList.Count - 1]))
                            return this.stringByType[structure.Variables[fieldList[fieldList.Count - 1]].Type];
                    }
                }
            }
            if (this.globalSymbolTable.Variables.ContainsKey(structureInstanceName) &&
                    (this.globalSymbolTable.Variables[structureInstanceName].Type.Parameters & TypeParameters.IsStructure) == TypeParameters.IsStructure)
            {
                var structure = this.definedStructures[this.stringByType[this.globalSymbolTable.Variables[structureInstanceName].Type]];
                for (var j = 0; j < fieldList.Count; j++)
                {
                    if (!structure.Variables.ContainsKey(fieldList[j])) continue;
                    var variable = structure.Variables[fieldList[j]];
                        if (variable.Type.Parameters != TypeParameters.IsStructure && j < fieldList.Count() - 1)
                            return null;
                        if (j < fieldList.Count - 1 && this.definedStructures.ContainsKey(this.stringByType[variable.Type]))
                            structure = this.definedStructures[this.stringByType[variable.Type]];
                    }
                if (structure.Variables.ContainsKey(fieldList[fieldList.Count - 1]))
                    return this.stringByType[structure.Variables[fieldList[fieldList.Count - 1]].Type];
            }

            return "error"; //Variable not found
        }

        public void AddNewStructure(string structureName, List<Tuple<string, string>> variables, bool atDevice)
        {
            //Added to the actual Scope
            var structureVariables = new Dictionary<string, Variable>();
            foreach (var variable in variables)
            {
                if (this.DefinedTypes.ContainsKey(variable.Item1))
                    structureVariables.Add(variable.Item2, new Variable(variable.Item2, this.DefinedTypes[variable.Item1], atDevice));
                else
                {
                    Error("Undefined Type {0} for variable {1} in struct declaration {2}.", variable.Item1,
                          variable.Item2, structureName);
                    return;
                }
            }
            var structure = new StructureType(structureName, structureVariables, atDevice);
            if (this.blocks.Any())
            {
                if (this.blocks[this.blocks.Count() - 1].AddVariable(structure) == false) //Already exists
                {
                    Error("Previously defined structure \"{0}\"", structureName);
                    return;
                }
            }
            else
            {
                if (this.globalSymbolTable.AddVariable(structure) == false) //Already exists
                {
                    Error("Previously defined structure \"{0}\"", structureName);
                    return;
                }
            }
            this.definedStructures.Add(structureName, structure);
            this.stringByType.Add(structure, structureName);
            this.DefinedTypes.Add(structureName, structure);
        }



        public void AddVariable(string type, string name, bool atDevice)
        {
            var variable = new Variable(name, this.DefinedTypes[type], atDevice);

            if (this.DefinedTypes.ContainsKey(type))
            {
                if (this.blocks.Any())
                {
                    var block = this.blocks[this.blocks.Count() - 1];

                    if (block.HasVariable(variable))
                        Error("Previously defined variable \"{0}\"", name); //Already exists
                    else
                        block.AddVariable(variable);
                }
                else
                {
                    if (this.globalSymbolTable.HasVariable(variable))
                        Error("Previously defined variable \"{0}\"", name); //Already exists 
                    else
                        this.globalSymbolTable.AddVariable(variable);
                }

            }
            else
                Error("Undefined Type {0} for variable {1}.", type, name);

        }

        public Function GetFunction(string name)
        {
            if (this.globalSymbolTable.Functions.ContainsKey(name))
                return this.globalSymbolTable.Functions[name];
            else if(this.globalSymbolTable.DeviceSymbolTable.Functions.ContainsKey(name))
                return this.globalSymbolTable.DeviceSymbolTable.Functions[name];
            else return null;
        }

        public void AddFunction(string name, string returnType, bool atDevice, params Type[] parameters)
        {
            var function = new Function(name, parameters.ToList(), returnType, atDevice);
            this.actualFunction = function;
            if(this.globalSymbolTable.AddFunction(name, function) == false) //Already Declared Function
            {
                Error("Already Declared Function with name {0}.", name);
            }
        }

        public void NewBlock()
        {
            this.blocks.Add(new Block());
        }

        public void CloseBlock()
        {
            var blockToRemove = this.blocks.ElementAt(this.blocks.Count() - 1);

            foreach (var structure in blockToRemove.Structures)
            {
                this.DefinedTypes.Remove(structure.Key);
                this.definedStructures.Remove(structure.Key);
                this.stringByType.Remove(structure.Value);
            }

            /*foreach (var variable in blockToRemove.Variables)
            {
                var isArray = (variable.Value.Type.Parameters & TypeParameters.IsArray) == TypeParameters.IsArray;

                DefinedTypes.Remove(variable.Key);
                _definedArrays.Remove(variable.Key);
                _stringByType.Remove(variable.Value.);
            }*/
            this.blocks.Remove(blockToRemove);
        }

        public void AddConstant(string type, string name, bool atDevice)
        {
            var variable = new Variable(name, this.DefinedTypes[type], atDevice) {IsConstant = true};

            if (this.DefinedTypes.ContainsKey(type))
            {
                if (this.blocks.Any())
                {
                    var block = this.blocks[this.blocks.Count() - 1];

                    if (block.HasVariable(variable))
                        Error("Previously defined variable \"{0}\"", name); //Already exists
                    else
                        block.AddVariable(variable);
                }
                else
                {
                    if (this.globalSymbolTable.HasVariable(variable))
                        Error("Previously defined variable \"{0}\"", name); //Already exists 
                    else
                        this.globalSymbolTable.AddVariable(variable);
                }

            }
            else
                Error("Undefined Type {0} for variable {1}.", type, name);
        }

        public string MaxType(string type1, string type2)
        {
            if (this.DefinedTypes.ContainsKey(type1) == false || this.DefinedTypes.ContainsKey(type2) == false)
            {
                this.SemanticError = true;
                return "error";
            }
            return this.DefinedTypes[type1].HierarchyPosition >= this.DefinedTypes[type2].HierarchyPosition ? type1 : type2;
        }

        public bool AreComparableTypes(string type1, string type2)
        {
            return (type1 == "string" || IsArithmeticType(type1)) && (type2 == "string" || IsArithmeticType(type2));
        }

        public bool IsBitwiseType(string type)
        {
            return this.DefinedTypes.ContainsKey(type) && (this.DefinedTypes[type].Parameters & TypeParameters.IsBitwise) == TypeParameters.IsBitwise;
        }

        public bool IsIntegerType(string type)
        {
            return this.DefinedTypes.ContainsKey(type) && (this.DefinedTypes[type].Parameters & TypeParameters.IsInteger) == TypeParameters.IsInteger;
        }

        public bool IsArithmeticType(string type)
        {
            return this.DefinedTypes.ContainsKey(type) && (this.DefinedTypes[type].Parameters & TypeParameters.IsArithmetic) == TypeParameters.IsArithmetic;
        }

        public bool IsCastableToType(string type1, string type2)
        {
            return type1 != "string" && this.DefinedTypes.ContainsKey(type1) && this.DefinedTypes[type1].Parameters != TypeParameters.IsStructure && this.DefinedTypes.ContainsKey(type2) && type2 != "string" && this.DefinedTypes[type2].Parameters != TypeParameters.IsStructure;
        }

        public string GetType(string name, bool atDevice)
        {
            ISymbolTable globalTable;
            if (atDevice)
                globalTable = this.globalSymbolTable.DeviceSymbolTable;
            else
                globalTable = this.globalSymbolTable;

            if (VariableExists(name, atDevice))
            {
                if (this.blocks.Any())
                {
                    for (int i = this.blocks.Count() - 1; i >= 0; i--)
                    {
                        if(this.blocks[i].Variables.ContainsKey(name))
                        {
                            if (this.stringByType.ContainsKey(this.blocks[i].Variables[name].Type))
                                return this.stringByType[this.blocks[i].Variables[name].Type];
                            else return "error";
                        }
                    }
                }
                if (globalTable.Variables.ContainsKey(name))
                {
                    if(this.stringByType.ContainsKey(globalTable.Variables[name].Type))
                        return this.stringByType[globalTable.Variables[name].Type];
                    else return "error";
                }
            }
            return "error";
        }

        public int GetTypeSize(string typeName)
        {
            if (this.DefinedTypes.ContainsKey(typeName) == false)
                return 0;
            return this.DefinedTypes[typeName].ByteSize;
        }

        public Type GetTypeObject(string variableName, bool atDevice)
        {
            ISymbolTable globalTable;
            if (atDevice)
                globalTable = this.globalSymbolTable.DeviceSymbolTable;
            else
                globalTable = this.globalSymbolTable;

            if (VariableExists(variableName, atDevice))
            {
                if (this.blocks.Any())
                {
                    for (int i = this.blocks.Count() - 1; i >= 0; i--)
                    {
                        if (this.blocks[i].Variables.ContainsKey(variableName))
                        {
                            if (this.stringByType.ContainsKey(this.blocks[i].Variables[variableName].Type))
                                return this.blocks[i].Variables[variableName].Type;
                            return null;
                        }
                    }
                }
                if (globalTable.Variables.ContainsKey(variableName))
                {
                    if (this.stringByType.ContainsKey(globalTable.Variables[variableName].Type))
                        return globalTable.Variables[variableName].Type;
                    return null;
                }
            }
            return null;
        }

        public bool DeclaredInDevice(string variableName)
        {
            //Host call
            if (this.globalSymbolTable.DeviceSymbolTable.Variables.ContainsKey(variableName))
                return true;
            else return false;
        }

        public bool IsArray(string typeName)
        {
            if (this.DefinedTypes.ContainsKey(typeName) == false)
                return false;
            var value = (this.DefinedTypes[typeName].Parameters & TypeParameters.IsArray) == TypeParameters.IsArray;
            return value;
        }

        public string GetNextTempName()
        {
            return this.blocks[this.blocks.Count - 1].GetNextTempName();
        }

        public bool VariableExists(string name, bool atDevice)
        {
            var returnValue = UseVariable(name, atDevice);
            return returnValue;
        }

    }
}
