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
                if (blocks.Any())
                {
                    var currentBlock = blocks[blocks.Count() - 1];
                    if (currentBlock.HasVariable(variable))
                        Error("Previously defined variable \"{0}\"", name);
                    else
                    {
                        currentBlock.AddVariable(variable);
                        var typeName = string.Format("array({0})", type);
                        if (definedArrays.ContainsKey(typeName) == false)
                        {
                            definedArrays.Add(typeName, arrayType);
                            stringByType.Add(arrayType, typeName);
                            DefinedTypes.Add(typeName, arrayType);
                        }


                    }
                }
                else
                {
                    if (globalSymbolTable.HasVariable(variable))
                        Error("Previously defined variable \"{0}\"", name); //Already exists 
                    else
                        globalSymbolTable.AddVariable(variable);
                } 
            }
        }

        public ArrayType GetArrayTypeFrom(string name)
        {
            if (blocks.Any())
            {
                for (var i = blocks.Count() - 1; i >= 0; i--)
                {
                    if (blocks[i].Variables.ContainsKey(name)) //Variable found!!
                    {
                        return (ArrayType) blocks[i].Variables[name].Type;
                    }
                }
            }

            if (globalSymbolTable.Variables.ContainsKey(name))
                return (ArrayType)globalSymbolTable.Variables[name].Type;
            else if (globalSymbolTable.DeviceSymbolTable.Variables.ContainsKey(name))
                return (ArrayType)globalSymbolTable.DeviceSymbolTable.Variables[name].Type;

            else return null;
        }

        public bool UseArray(string name, bool atDevice, int numberOfDimensions, out string baseType)
        {
            ISymbolTable globalTable;
            if (atDevice)
                globalTable = globalSymbolTable.DeviceSymbolTable;
            else
                globalTable = globalSymbolTable; 

            if (blocks.Any())
            {
                for (var i = blocks.Count() - 1; i >= 0; i--)
                {
                    if (blocks[i].Variables.ContainsKey(name) &&
                        blocks[i].Variables[name].Type.Parameters != TypeParameters.IsStructure && (blocks[i].Variables[name].Type.Parameters & TypeParameters.IsArray)== TypeParameters.IsArray) //Variable found!!
                    {
                        var arrayType = (blocks[i].Variables[name].Type as ArrayType);
                        if (numberOfDimensions != arrayType.Dimensions)
                        {
                            Error("Array {0} expected {1} dimensions but was used with {2}.", name, arrayType.Dimensions, numberOfDimensions);
                            baseType = "error";
                        }
                        else
                            baseType = stringByType[arrayType.BaseType];
                        
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
                    baseType = "error";
                }
                else
                    baseType = stringByType[arrayType.BaseType];
                return true;
            }
            baseType = "error";
            return false;
        }
    }
}
