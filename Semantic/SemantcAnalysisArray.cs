using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semantic
{
    public partial class SemanticAnalysis
    {
        public void AddArray(string type, string name, bool atDevice, int dimensions, int firstDimensionWidth, params int[] extraDimensionsWidth)
        {
            
            
            if (DefinedTypes.ContainsKey(type) == false)
                Error("Undefined Type {0} for variable {1}.", type, name);
            else
            {
                var arrayPrimitive = DefinedTypes[type];
                var arrayType = new ArrayType(arrayPrimitive, dimensions, firstDimensionWidth, extraDimensionsWidth);
                var variable = new Variable(name, arrayType, atDevice);
                if (_blocks.Any())
                {
                    var currentBlock = _blocks[_blocks.Count() - 1];
                    if (currentBlock.HasVariable(variable))
                        Error("Previously defined variable \"{0}\"", name);
                    else
                    {
                        currentBlock.AddVariable(variable);
                        var typeName = string.Format("array({0})", type);
                        if (_definedArrays.ContainsKey(typeName) == false)
                        {
                            _definedArrays.Add(typeName, arrayType);
                            _stringByType.Add(arrayType, typeName);
                            DefinedTypes.Add(typeName, arrayType);
                        }


                    }
                }
                else
                {
                    if (_globalSymbolTable.HasVariable(variable))
                        Error("Previously defined variable \"{0}\"", name); //Already exists 
                    else
                        _globalSymbolTable.AddVariable(variable);
                } 
            }
        }

        public ArrayType GetArrayTypeFrom(string name)
        {
            if (_blocks.Any())
            {
                for (var i = _blocks.Count() - 1; i >= 0; i--)
                {
                    if (_blocks[i].Variables.ContainsKey(name)) //Variable found!!
                    {
                        return (ArrayType) _blocks[i].Variables[name].Type;
                    }
                }
            }

            if (_globalSymbolTable.Variables.ContainsKey(name))
                return (ArrayType)_globalSymbolTable.Variables[name].Type;
            else if (_globalSymbolTable.DeviceSymbolTable.Variables.ContainsKey(name))
                return (ArrayType)_globalSymbolTable.DeviceSymbolTable.Variables[name].Type;

            else return null;
        }

        public bool UseArray(string name, bool atDevice, int numberOfDimensions, out string BaseType)
        {
            ISymbolTable globalTable;
            if (atDevice)
                globalTable = _globalSymbolTable.DeviceSymbolTable;
            else
                globalTable = _globalSymbolTable; 

            if (_blocks.Any())
            {
                for (var i = _blocks.Count() - 1; i >= 0; i--)
                {
                    if (_blocks[i].Variables.ContainsKey(name) &&
                        _blocks[i].Variables[name].Type.Parameters != TypeParameters.IsStructure && (_blocks[i].Variables[name].Type.Parameters & TypeParameters.IsArray)== TypeParameters.IsArray) //Variable found!!
                    {
                        var arrayType = (_blocks[i].Variables[name].Type as ArrayType);
                        if (numberOfDimensions != arrayType.Dimensions)
                        {
                            Error("Array {0} expected {1} dimensions but was used with {2}.", name, arrayType.Dimensions, numberOfDimensions);
                            BaseType = "error";
                        }
                        else
                            BaseType = _stringByType[arrayType.BaseType];
                        
                        return true;
                    }
                }
            }
            else if (globalTable.Variables.ContainsKey(name) && globalTable.Variables[name].Type.Parameters != TypeParameters.IsStructure && (globalTable.Variables[name].Type.Parameters & TypeParameters.IsArray) == TypeParameters.IsArray) //Variable Found!!!
            {
                var arrayType = (globalTable.Variables[name].Type as ArrayType);
                if (numberOfDimensions != arrayType.Dimensions)
                {
                    Error("Array {0} expected {1} dimensions but was used with {2}.", name, arrayType.Dimensions, numberOfDimensions);
                    BaseType = "error";
                }
                else
                    BaseType = _stringByType[arrayType.BaseType];
                return true;
            }
            BaseType = "error";
            return false;
        }
    }
}
